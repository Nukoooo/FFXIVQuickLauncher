﻿using System.IO;

namespace XIVLauncher.Common.Unix.Compatibility;

public enum WineStartupType
{
    [SettingsDescription("Managed by XIVLauncher", "The game installation and wine setup is managed by XIVLauncher - you can leave it up to us.")]
    Managed,

    [SettingsDescription("Custom", "Point XIVLauncher to a custom location containing wine binaries to run the game with.")]
    Custom,
}

public class WineSettings
{
    public WineStartupType StartupType { get; private set; }
    public string CustomBinPath { get; private set; }

    public string EsyncOn { get; private set; }
    public string FsyncOn { get; private set; }
    public string MsyncOn { get; private set; }

    public string MoltenVk { get; private set; }

    public string DebugVars { get; private set; }
    public string Env { get; private set; }
    public FileInfo LogFile { get; private set; }

    public DirectoryInfo Prefix { get; private set; }

    public WineSettings(WineStartupType? startupType, string customBinPath, string debugVars, FileInfo logFile, DirectoryInfo prefix, bool? esyncOn, bool? fsyncOn, bool? msyncOn, bool? modernMvkOn, string? env)
    {
        this.StartupType = startupType ?? WineStartupType.Custom;
        this.CustomBinPath = customBinPath;
        this.EsyncOn = (esyncOn ?? false) ? "1" : "0";
        this.FsyncOn = (fsyncOn ?? false) ? "1" : "0";
        this.MsyncOn = (msyncOn ?? false) ? "1" : "0";
        this.MoltenVk = (modernMvkOn ?? false) ? "modern" : "stable";
        this.DebugVars = debugVars;
        this.Env = env ?? "";
        this.LogFile = logFile;
        this.Prefix = prefix;
    }
}
