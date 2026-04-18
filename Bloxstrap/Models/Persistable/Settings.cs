using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Bloxstrap.Models.Persistable
{
    public class Settings
    {
        // bloxstrap configuration
        public BootstrapperStyle BootstrapperStyle { get; set; } = BootstrapperStyle.FluentDialog;
        public BootstrapperIcon BootstrapperIcon { get; set; } = BootstrapperIcon.IconBloxstrap;
        public string BootstrapperTitle { get; set; } = App.ProjectName;
        public string BootstrapperIconCustomLocation { get; set; } = "";
        public Theme Theme { get; set; } = Theme.Default;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DeveloperMode { get; set; } = false;
        public bool CheckForUpdates { get; set; } = false;
        public bool ConfirmLaunches { get; set; } = false;
        public string Locale { get; set; } = "nil";
        public bool UseFastFlagManager { get; set; } = true;
        public bool WPFSoftwareRender { get; set; } = false;
        public bool EnableAnalytics { get; set; } = true;
        public bool BackgroundUpdatesEnabled { get; set; } = false;
        public bool DebugDisableVersionPackageCleanup { get; set; } = false;
        public string? SelectedCustomTheme { get; set; } = null;
        public WebEnvironment WebEnvironment { get; set; } = WebEnvironment.Production;

        // integration configuration
        public bool EnableActivityTracking { get; set; } = true;
        public bool UseDiscordRichPresence { get; set; } = true;
        public bool HideRPCButtons { get; set; } = true;
        public bool ShowAccountOnRichPresence { get; set; } = false;
        public bool ShowServerDetails { get; set; } = false;
        public ObservableCollection<CustomIntegration> CustomIntegrations { get; set; } = new();

        // mod preset configuration
        public bool UseDisableAppPatch { get; set; } = false;

        // performance profiles
        public string SelectedPerformanceProfile { get; set; } = "Balanced";
        public int? PerformanceFPSCap { get; set; } = null; // null = auto
        public bool PerformanceManualFPSOverride { get; set; } = false;
        // memory trimmer
        public bool MemoryTrimEnabled { get; set; } = false;
        public int MemoryTrimIntervalMinutes { get; set; } = 0; // 0 = disabled

        // performance maintenance
        public bool PerformanceAutoCleanTempLogs { get; set; } = false;
        // cleaner options
        // 0 = Never, otherwise number of days after which files will be deleted
        public int PerformanceCleanerRetentionDays { get; set; } = 0;
        public bool PerformanceCleanerCache { get; set; } = false;
        public bool PerformanceCleanerLogs { get; set; } = false;
        public bool PerformanceCleanerAppLogs { get; set; } = false;

        // advanced Roblox-specific performance toggles
        public bool PerformanceAdvPreferD3D11 { get; set; } = false;
        public bool PerformanceAdvDisableGrass { get; set; } = false;
        public bool PerformanceAdvPauseVoxelizer { get; set; } = false;
        public bool PerformanceAdvLowTextureQuality { get; set; } = false;
        /// <summary>Forces a low automatic graphics quality tier (in-game quality slider–like behavior).</summary>
        public bool PerformanceAdvCapGraphicsQuality { get; set; } = false;
        /// <summary>Disables MSAA (FIntDebugForceMSAASamples = 0).</summary>
        public bool PerformanceAdvDisableMsaa { get; set; } = false;

        // Roblox runtime process tuning
        public RobloxProcessPriority RobloxProcessPriority { get; set; } = RobloxProcessPriority.AboveNormal;
        public RobloxAffinityMode RobloxAffinityMode { get; set; } = RobloxAffinityMode.Auto;
        /// <summary>Windows: allow temporary priority boost when the Roblox window has focus.</summary>
        public bool RobloxRuntimePriorityBoost { get; set; } = true;
        /// <summary>Windows: disable EcoQoS / execution-speed power throttling for the Roblox process (no FastFlags).</summary>
        public bool RobloxRuntimeDisablePowerThrottling { get; set; } = true;
        /// <summary>Windows: set per-exe GPU preference to high performance (DirectX UserGpuPreferences).</summary>
        public bool RobloxShellGpuHighPerformance { get; set; } = false;
        /// <summary>Windows: add compatibility layer to disable fullscreen optimizations for the Roblox executable.</summary>
        public bool RobloxShellDisableFullscreenOptimizations { get; set; } = false;
    }
}
