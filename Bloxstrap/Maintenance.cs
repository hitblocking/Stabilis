using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Bloxstrap
{
    public static class Maintenance
    {
        private static Timer? _trimTimer;

        public static void StartMemoryTrimmer(int minutes)
        {
            StopMemoryTrimmer();

            if (minutes <= 0)
                return;

            var due = TimeSpan.FromMinutes(minutes);
            _trimTimer = new Timer(_ => TrimMemory(), null, due, TimeSpan.FromMinutes(minutes));
        }

        public static void StopMemoryTrimmer()
        {
            try
            {
                _trimTimer?.Dispose();
                _trimTimer = null;
            }
            catch { }
        }

        public static void TrimMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // attempt to trim working set
                try
                {
                    EmptyWorkingSet(Process.GetCurrentProcess().Handle);
                }
                catch { }

                App.Logger.WriteLine("Maintenance::TrimMemory", "Trimmed memory");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("Maintenance::TrimMemory", ex);
            }
        }

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        // retentionDays: 0 = Never; otherwise delete files older than retentionDays
        public static void CleanTempAndLogs(int retentionDays, bool cleanCache, bool cleanLogs, bool cleanAppLogs)
        {
            try
            {
                // delete app temp updates/logs completely if requested
                if (cleanAppLogs)
                {
                    TryDeleteDirectory(Paths.TempUpdates);
                    TryDeleteDirectory(Paths.TempLogs);
                }

                // delete files in Path.Temp (app-specific) root - preserve root
                if (cleanAppLogs)
                    TryDeleteDirectory(Paths.Temp, preserveRoot: true);

                // if retentionDays == 0, user selected 'Never'
                if (retentionDays <= 0)
                {
                    App.Logger.WriteLine("Maintenance::CleanTempAndLogs", "Cleaner enabled but retention is 'Never' - nothing to delete based on age");
                    return;
                }

                DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);

                // clean downloads/cache
                if (cleanCache && Directory.Exists(Paths.Downloads))
                {
                    foreach (var f in Directory.GetFiles(Paths.Downloads))
                    {
                        try
                        {
                            var info = new FileInfo(f);
                            if (info.LastWriteTimeUtc < cutoff)
                                File.Delete(f);
                        }
                        catch { }
                    }
                }

                // clean logs
                if (cleanLogs && Directory.Exists(Paths.Logs))
                {
                    foreach (var f in Directory.GetFiles(Paths.Logs))
                    {
                        try
                        {
                            var info = new FileInfo(f);
                            if (info.LastWriteTimeUtc < cutoff)
                                File.Delete(f);
                        }
                        catch { }
                    }
                }

                // clean temp app logs (if not deleted entirely)
                if (!cleanAppLogs && Directory.Exists(Paths.TempLogs))
                {
                    foreach (var f in Directory.GetFiles(Paths.TempLogs))
                    {
                        try
                        {
                            var info = new FileInfo(f);
                            if (info.LastWriteTimeUtc < cutoff)
                                File.Delete(f);
                        }
                        catch { }
                    }
                }

                App.Logger.WriteLine("Maintenance::CleanTempAndLogs", $"Cleaned files older than {retentionDays} days (Cache:{cleanCache}, Logs:{cleanLogs}, AppLogs:{cleanAppLogs})");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("Maintenance::CleanTempAndLogs", ex);
            }
        }

        private static void TryDeleteDirectory(string path, bool preserveRoot = false)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                foreach (var file in Directory.GetFiles(path))
                {
                    try { File.Delete(file); } catch { }
                }

                foreach (var dir in Directory.GetDirectories(path))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }

                if (!preserveRoot)
                {
                    try { Directory.Delete(path, false); } catch { }
                }
            }
            catch { }
        }
    }
}
