using System.Runtime.InteropServices;
using Bloxstrap.AppData;
using Microsoft.Win32;

namespace Bloxstrap
{
    public static class RobloxRuntimeOptimizer
    {
        private const string LOG_IDENT_BASE = "RobloxRuntimeOptimizer";

        private static class Native
        {
            // processthreadsapi.h — second enum member after ProcessMemoryExhaustionInfo
            internal const int ProcessInformationClassProcessPowerThrottling = 1;

            internal const uint ProcessPowerThrottlingCurrentVersion = 1;
            internal const uint ProcessPowerThrottlingExecutionSpeed = 0x1;

            [StructLayout(LayoutKind.Sequential)]
            internal struct PROCESS_POWER_THROTTLING_STATE
            {
                internal uint Version;
                internal uint ControlMask;
                internal uint StateMask;
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool SetProcessInformation(
                IntPtr hProcess,
                int processInformationClass,
                ref PROCESS_POWER_THROTTLING_STATE processInformation,
                uint processInformationSize);
        }

        private static readonly Regex s_fullscreenOptLayer = new(
            @"~\s*DISABLEDXMAXIMIZEDWINDOWEDVSYNC",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static string GetCpuModel()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key?.GetValue("ProcessorNameString") is string model && !String.IsNullOrWhiteSpace(model))
                    return Regex.Replace(model.Trim(), @"\s+", " ");
            }
            catch { }

            return "Unknown CPU";
        }

        public static int SuggestAutoCoreCount()
        {
            int logicalCores = Environment.ProcessorCount;
            string model = GetCpuModel();

            if (logicalCores <= 8)
                return logicalCores;

            bool lowPowerIntel = model.Contains("Intel", StringComparison.OrdinalIgnoreCase)
                && (model.Contains(" U", StringComparison.OrdinalIgnoreCase)
                    || model.Contains(" Y", StringComparison.OrdinalIgnoreCase));

            if (model.Contains("Ryzen 9", StringComparison.OrdinalIgnoreCase) || model.Contains("Core(TM) i9", StringComparison.OrdinalIgnoreCase))
                return Math.Clamp(logicalCores - 2, 8, logicalCores);

            if (lowPowerIntel)
                return Math.Clamp(logicalCores - 4, 4, logicalCores);

            if (logicalCores >= 24)
                return 16;
            if (logicalCores >= 16)
                return 12;
            if (logicalCores >= 12)
                return 10;

            return Math.Clamp(logicalCores - 2, 4, logicalCores);
        }

        public static int GetTargetCoreCount(RobloxAffinityMode mode)
        {
            int logicalCores = Math.Max(1, Environment.ProcessorCount);

            return mode switch
            {
                RobloxAffinityMode.AllCores => logicalCores,
                RobloxAffinityMode.Percent75 => Math.Clamp((int)Math.Ceiling(logicalCores * 0.75), 1, logicalCores),
                RobloxAffinityMode.Percent50 => Math.Clamp((int)Math.Ceiling(logicalCores * 0.50), 1, logicalCores),
                _ => Math.Clamp(SuggestAutoCoreCount(), 1, logicalCores)
            };
        }

        /// <summary>
        /// Applies DirectX GPU preference and compatibility Layers for known Player and Studio paths when those files exist.
        /// </summary>
        public static void ApplyShellPreferencesFromSettings()
        {
            const string LOG_IDENT = $"{LOG_IDENT_BASE}::ApplyShellPreferencesFromSettings";

            try
            {
                ApplyShellPreferencesToPath(new RobloxPlayerData().ExecutablePath, LOG_IDENT);
                ApplyShellPreferencesToPath(new RobloxStudioData().ExecutablePath, LOG_IDENT);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to apply shell preferences");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        /// <summary>
        /// Applies shell preferences for the executable about to launch (correct version folder).
        /// </summary>
        public static void ApplyShellPreferencesForExecutable(string executablePath)
        {
            ApplyShellPreferencesToPath(executablePath, $"{LOG_IDENT_BASE}::ApplyShellPreferencesForExecutable");
        }

        private static void ApplyShellPreferencesToPath(string executablePath, string logIdent)
        {
            if (String.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                return;

            bool wantGpu = App.Settings.Prop.RobloxShellGpuHighPerformance;
            bool wantFs = App.Settings.Prop.RobloxShellDisableFullscreenOptimizations;

            try
            {
                if (wantGpu)
                    ApplyGpuPreference(executablePath, logIdent);
                else
                    RemoveGpuPreferenceIfOurs(executablePath, logIdent);

                if (wantFs)
                    MergeFullscreenOptimizationsLayer(executablePath, enable: true, logIdent);
                else
                    MergeFullscreenOptimizationsLayer(executablePath, enable: false, logIdent);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(logIdent, $"Shell preference failed for {executablePath}");
                App.Logger.WriteException(logIdent, ex);
            }
        }

        private const string GpuPreferenceValueHigh = "GpuPreference=2;";

        private static void ApplyGpuPreference(string exePath, string logIdent)
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences");
            if (key is null)
                return;

            key.SetValue(exePath, GpuPreferenceValueHigh, RegistryValueKind.String);
            App.Logger.WriteLine(logIdent, $"Set GPU preference to high performance for {exePath}");
        }

        private static void RemoveGpuPreferenceIfOurs(string exePath, string logIdent)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences", writable: true);
            if (key is null)
                return;

            var existing = key.GetValue(exePath) as string;
            if (String.IsNullOrEmpty(existing))
                return;

            if (existing.Trim().Equals(GpuPreferenceValueHigh.Trim(), StringComparison.OrdinalIgnoreCase)
                || existing.Trim() == "GpuPreference=2")
            {
                key.DeleteValue(exePath, throwOnMissingValue: false);
                App.Logger.WriteLine(logIdent, $"Removed GPU preference override for {exePath}");
            }
        }

        private static void MergeFullscreenOptimizationsLayer(string exePath, bool enable, string logIdent)
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");
            if (key is null)
                return;

            string? existing = key.GetValue(exePath) as string;

            if (enable)
            {
                if (String.IsNullOrEmpty(existing))
                {
                    key.SetValue(exePath, "~ DISABLEDXMAXIMIZEDWINDOWEDVSYNC", RegistryValueKind.String);
                    App.Logger.WriteLine(logIdent, $"Set compatibility layer (disable fullscreen optimizations) for {exePath}");
                }
                else if (!existing.Contains("DISABLEDXMAXIMIZEDWINDOWEDVSYNC", StringComparison.OrdinalIgnoreCase))
                {
                    key.SetValue(exePath, existing.TrimEnd() + " ~ DISABLEDXMAXIMIZEDWINDOWEDVSYNC", RegistryValueKind.String);
                    App.Logger.WriteLine(logIdent, $"Appended compatibility layer for {exePath}");
                }

                return;
            }

            if (String.IsNullOrEmpty(existing))
                return;

            string trimmed = s_fullscreenOptLayer.Replace(existing, " ").Trim();
            trimmed = Regex.Replace(trimmed, @"\s+", " ").Trim();

            if (String.IsNullOrEmpty(trimmed))
                key.DeleteValue(exePath, throwOnMissingValue: false);
            else
                key.SetValue(exePath, trimmed, RegistryValueKind.String);

            App.Logger.WriteLine(logIdent, $"Removed fullscreen-optimization compatibility flag from {exePath} (if present)");
        }

        public static void ApplyToProcess(int processId)
        {
            const string LOG_IDENT = $"{LOG_IDENT_BASE}::ApplyToProcess";

            try
            {
                using var process = Process.GetProcessById(processId);

                var priority = App.Settings.Prop.RobloxProcessPriority switch
                {
                    RobloxProcessPriority.High => ProcessPriorityClass.High,
                    RobloxProcessPriority.AboveNormal => ProcessPriorityClass.AboveNormal,
                    _ => ProcessPriorityClass.Normal
                };

                try
                {
                    process.PriorityClass = priority;
                    App.Logger.WriteLine(LOG_IDENT, $"Set process priority to {priority} (pid={processId})");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to set process priority (pid={processId})");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }

                try
                {
                    process.PriorityBoostEnabled = App.Settings.Prop.RobloxRuntimePriorityBoost;
                    App.Logger.WriteLine(LOG_IDENT, $"Set PriorityBoostEnabled={process.PriorityBoostEnabled} (pid={processId})");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to set priority boost (pid={processId})");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }

                if (App.Settings.Prop.RobloxRuntimeDisablePowerThrottling)
                {
                    try
                    {
                        var state = new Native.PROCESS_POWER_THROTTLING_STATE
                        {
                            Version = Native.ProcessPowerThrottlingCurrentVersion,
                            ControlMask = Native.ProcessPowerThrottlingExecutionSpeed,
                            StateMask = 0
                        };

                        bool ok = Native.SetProcessInformation(
                            process.Handle,
                            Native.ProcessInformationClassProcessPowerThrottling,
                            ref state,
                            (uint)Marshal.SizeOf<Native.PROCESS_POWER_THROTTLING_STATE>());

                        if (ok)
                            App.Logger.WriteLine(LOG_IDENT, $"Disabled process power throttling (EcoQoS execution speed) (pid={processId})");
                        else
                            App.Logger.WriteLine(LOG_IDENT, $"SetProcessInformation failed (pid={processId}, win32={Marshal.GetLastWin32Error()})");
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to disable power throttling (pid={processId})");
                        App.Logger.WriteException(LOG_IDENT, ex);
                    }
                }

                int logicalCores = Math.Max(1, Environment.ProcessorCount);
                int maxMaskBits = Math.Min(logicalCores, 63);
                int targetCores = Math.Clamp(GetTargetCoreCount(App.Settings.Prop.RobloxAffinityMode), 1, maxMaskBits);

                if (targetCores >= logicalCores || App.Settings.Prop.RobloxAffinityMode == RobloxAffinityMode.AllCores)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Skipping affinity change (mode={App.Settings.Prop.RobloxAffinityMode}, logical={logicalCores})");
                    return;
                }

                long mask = 0;
                for (int i = 0; i < targetCores; i++)
                    mask |= 1L << i;

                try
                {
                    process.ProcessorAffinity = (IntPtr)mask;
                    App.Logger.WriteLine(LOG_IDENT, $"Set processor affinity to first {targetCores}/{logicalCores} logical cores (pid={processId}, mask=0x{mask:X})");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to set processor affinity (pid={processId})");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Could not access process {processId}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
