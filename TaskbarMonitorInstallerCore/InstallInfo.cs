using System.Collections.Generic;

namespace TaskbarMonitorInstallerCore;

public class InstallInfo
{
    public Dictionary<string, byte[]> FilesToCopy { get; }
    public List<string> FilesToRegister { get; }
    public string TargetPath { get; }

    public InstallInfo(Dictionary<string, byte[]> filesToCopy, List<string> filesToRegister, string targetPath)
    {
        FilesToCopy = filesToCopy;
        FilesToRegister = filesToRegister;
        TargetPath = targetPath;
    }
}