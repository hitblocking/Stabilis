using Microsoft.Win32;

namespace Bloxstrap
{
    /// <summary>
    /// Optional HKLM performance tweaks (multimedia scheduling, TCP). Requires Administrator for writes; failures are logged only.
    /// </summary>
    public static class WindowsPerformanceTweaks
    {
        private const string LOG_IDENT = "WindowsPerformanceTweaks";

        private const string SystemProfileKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        private const string GamesTaskKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
        private const string TcpipParametersKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
        private const string GraphicsDriversKey = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        private const string FileSystemKey = @"SYSTEM\CurrentControlSet\Control\FileSystem";
        private const string PriorityControlKey = @"SYSTEM\CurrentControlSet\Control\PriorityControl";

        /// <summary>Seconds before TDR; default is often 2.</summary>
        private const int TdrDelayExtendedSeconds = 10;

        private const int TdrDelayDefaultSeconds = 2;

        /// <summary>Common client default for Win32PrioritySeparation.</summary>
        private const int Win32PrioritySeparationDefault = 2;

        /// <summary>Short variable quantum (typical “foreground boost” value in guides).</summary>
        private const int Win32PrioritySeparationForegroundBoost = 38;

        /// <summary>Disables MMCSS network throttling for foreground multimedia (common gaming tweak).</summary>
        private const int NetworkThrottlingDisabled = unchecked((int)0xFFFFFFFF);

        /// <summary>Default when throttling is active (decimal 10).</summary>
        private const int NetworkThrottlingDefaultIndexed = 10;

        /// <summary>Snappier foreground; default Windows value is 20.</summary>
        private const int SystemResponsivenessAggressive = 10;

        private const int SystemResponsivenessDefault = 20;

        public static void ApplyFromSettings()
        {
            var s = App.Settings.Prop;
            ApplyMultimediaSystemProfile(s.PerformanceWinTweakMultimediaSystemProfile);
            ApplyTcpLatency(s.PerformanceWinTweakTcpLatency);
            ApplyMmcssGames(s.PerformanceWinTweakMmcssGames);
            ApplyGraphicsTdrDelay(s.PerformanceWinTweakGraphicsTdrDelay);
            ApplyNtfsNoLastAccess(s.PerformanceWinTweakNtfsNoLastAccess);
            ApplyWin32PriorityForeground(s.PerformanceWinTweakWin32PriorityForeground);
        }

        /// <summary>
        /// NetworkThrottlingIndex + SystemResponsiveness under Multimedia SystemProfile.
        /// </summary>
        private static void ApplyMultimediaSystemProfile(bool enable)
        {
            const string sub = $"{LOG_IDENT}::MultimediaSystemProfile";
            try
            {
                using var k = Registry.LocalMachine.CreateSubKey(SystemProfileKey, writable: true);
                if (k is null)
                    return;

                if (enable)
                {
                    TrySetDword(k, sub, "NetworkThrottlingIndex", NetworkThrottlingDisabled);
                    TrySetDword(k, sub, "SystemResponsiveness", SystemResponsivenessAggressive);
                }
                else
                {
                    TrySetDword(k, sub, "NetworkThrottlingIndex", NetworkThrottlingDefaultIndexed);
                    TrySetDword(k, sub, "SystemResponsiveness", SystemResponsivenessDefault);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(sub, "Failed to open Multimedia SystemProfile key");
                App.Logger.WriteException(sub, ex);
            }
        }

        /// <summary>
        /// TCP ACK / Nagle-style latency (system-wide). TcpAckFrequency=1 disables delayed ACKs; TCPNoDelay=1 disables Nagle.
        /// </summary>
        private static void ApplyTcpLatency(bool enable)
        {
            const string sub = $"{LOG_IDENT}::TcpLatency";
            try
            {
                using var k = Registry.LocalMachine.CreateSubKey(TcpipParametersKey, writable: true);
                if (k is null)
                    return;

                if (enable)
                {
                    TrySetDword(k, sub, "TcpAckFrequency", 1);
                    TrySetDword(k, sub, "TCPNoDelay", 1);
                }
                else
                {
                    TryDeleteValue(k, sub, "TcpAckFrequency");
                    TryDeleteValue(k, sub, "TCPNoDelay");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(sub, "Failed to open Tcpip\\Parameters key");
                App.Logger.WriteException(sub, ex);
            }
        }

        /// <summary>
        /// MMCSS "Games" task: higher scheduling priority for processes classified as games.
        /// </summary>
        private static void ApplyMmcssGames(bool enable)
        {
            const string sub = $"{LOG_IDENT}::MmcssGames";
            try
            {
                using var k = Registry.LocalMachine.CreateSubKey(GamesTaskKey, writable: true);
                if (k is null)
                    return;

                if (enable)
                {
                    TrySetDword(k, sub, "GPU Priority", 8);
                    TrySetDword(k, sub, "Priority", 6);
                    TrySetString(k, sub, "Scheduling Category", "High");
                    TrySetString(k, sub, "SFIO Priority", "High");
                    TrySetString(k, sub, "Background Only", "False");
                }
                else
                {
                    TrySetDword(k, sub, "GPU Priority", 8);
                    TrySetDword(k, sub, "Priority", 2);
                    TrySetString(k, sub, "Scheduling Category", "Medium");
                    TrySetString(k, sub, "SFIO Priority", "Normal");
                    TrySetString(k, sub, "Background Only", "False");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(sub, "Failed to open MMCSS Games key");
                App.Logger.WriteException(sub, ex);
            }
        }

        /// <summary>
        /// GPU TDR: more time before Windows resets the display driver on a hung GPU (can reduce stability issues under sustained load).
        /// </summary>
        private static void ApplyGraphicsTdrDelay(bool enable)
        {
            const string sub = $"{LOG_IDENT}::GraphicsTdrDelay";
            try
            {
                using var k = Registry.LocalMachine.CreateSubKey(GraphicsDriversKey, writable: true);
                if (k is null)
                    return;

                if (enable)
                    TrySetDword(k, sub, "TdrDelay", TdrDelayExtendedSeconds);
                else
                    TrySetDword(k, sub, "TdrDelay", TdrDelayDefaultSeconds);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(sub, "Failed to open GraphicsDrivers key");
                App.Logger.WriteException(sub, ex);
            }
        }

        /// <summary>
        /// NTFS: stop maintaining last-access timestamps on files (fewer writes; can smooth disk-heavy workloads).
        /// </summary>
        private static void ApplyNtfsNoLastAccess(bool enable)
        {
            const string sub = $"{LOG_IDENT}::NtfsLastAccess";
            try
            {
                using var k = Registry.LocalMachine.CreateSubKey(FileSystemKey, writable: true);
                if (k is null)
                    return;

                if (enable)
                    TrySetDword(k, sub, "NtfsDisableLastAccessUpdate", 1);
                else
                    TryDeleteValue(k, sub, "NtfsDisableLastAccessUpdate");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(sub, "Failed to open FileSystem key");
                App.Logger.WriteException(sub, ex);
            }
        }

        /// <summary>
        /// Scheduler: favor foreground threads with a shorter variable quantum (classic workstation tuning).
        /// </summary>
        private static void ApplyWin32PriorityForeground(bool enable)
        {
            const string sub = $"{LOG_IDENT}::Win32PrioritySeparation";
            try
            {
                using var k = Registry.LocalMachine.CreateSubKey(PriorityControlKey, writable: true);
                if (k is null)
                    return;

                if (enable)
                    TrySetDword(k, sub, "Win32PrioritySeparation", Win32PrioritySeparationForegroundBoost);
                else
                    TrySetDword(k, sub, "Win32PrioritySeparation", Win32PrioritySeparationDefault);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(sub, "Failed to open PriorityControl key");
                App.Logger.WriteException(sub, ex);
            }
        }

        private static void TrySetDword(RegistryKey key, string logSub, string name, int value)
        {
            try
            {
                key.SetValue(name, value, RegistryValueKind.DWord);
                App.Logger.WriteLine(logSub, $"Set HKLM\\...\\{name}={value} (DWORD)");
            }
            catch (UnauthorizedAccessException)
            {
                App.Logger.WriteLine(logSub, $"Access denied setting {name} (run Stabilis as Administrator to apply HKLM tweaks)");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(logSub, $"Failed setting {name}");
                App.Logger.WriteException(logSub, ex);
            }
        }

        private static void TrySetString(RegistryKey key, string logSub, string name, string value)
        {
            try
            {
                key.SetValue(name, value, RegistryValueKind.String);
                App.Logger.WriteLine(logSub, $"Set HKLM\\...\\{name}={value}");
            }
            catch (UnauthorizedAccessException)
            {
                App.Logger.WriteLine(logSub, $"Access denied setting {name} (run Stabilis as Administrator to apply HKLM tweaks)");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(logSub, $"Failed setting {name}");
                App.Logger.WriteException(logSub, ex);
            }
        }

        private static void TryDeleteValue(RegistryKey key, string logSub, string name)
        {
            try
            {
                key.DeleteValue(name, throwOnMissingValue: false);
                App.Logger.WriteLine(logSub, $"Removed {name} (restore OS default)");
            }
            catch (UnauthorizedAccessException)
            {
                App.Logger.WriteLine(logSub, $"Access denied removing {name} (run as Administrator)");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(logSub, $"Failed removing {name}");
                App.Logger.WriteException(logSub, ex);
            }
        }
    }
}
