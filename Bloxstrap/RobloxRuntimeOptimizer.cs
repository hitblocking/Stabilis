using Microsoft.Win32;

namespace Bloxstrap
{
    public static class RobloxRuntimeOptimizer
    {
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

        public static void ApplyToProcess(int processId)
        {
            const string LOG_IDENT = "RobloxRuntimeOptimizer::ApplyToProcess";

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
