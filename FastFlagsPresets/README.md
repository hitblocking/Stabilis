Performance profile presets for Bloxstrap

Place JSON files in this folder (or in your installation's FastFlagsPresets folder) to add custom performance profiles.

Schema (example):
{
  "Name": "Performance",
  "Description": "Reduce visual load",
  "FPSCap": 120,
  "FastFlags": {
    // You can specify either preset keys (e.g. "Rendering.DisableScaling") which map to
    // internal fastflags via FastFlagManager.PresetFlags, or the raw fastflag id (e.g. "DFFlagDisableDPIScale").
    "Rendering.DisableScaling": "True",
    "Rendering.TextureQuality.Level": "0",

    // To delete a flag, set the value to null
    // "SomeFastFlag": null
  }
}

Notes:
- FPSCap is optional (null = auto). If omitted, manager will suggest a cap based on your primary monitor.
- FastFlags values are stored as strings. Use "True"/"False" or numeric strings as appropriate.
- Files must be valid UTF-8 JSON and have a top-level object matching the sample above.
