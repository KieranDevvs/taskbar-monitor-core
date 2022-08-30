using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using TaskbarMonitorInstallerCore.Windows;

namespace TaskbarMonitorInstallerCore;

public class Program
{
    private static readonly Guid UninstallGuid = new Guid(@"F85D2114-BEE6-49E7-9D1F-E637CDB40180");

    public static void Main(string[] args)
    {
        Console.Title = "Taskbar Monitor Installer";

        var filesToCopy = new Dictionary<string, byte[]> {
            { "TaskbarMonitorCore.comhost.dll", Properties.Resources.TaskbarMonitorCore_comhost },
            { "TaskbarMonitorCore.dll", Properties.Resources.TaskbarMonitorCore },
            { "TaskbarMonitorCore.runtimeconfig.json", Properties.Resources.TaskbarMonitorCore_runtimeconfig },
        };

        var filesToRegister = new List<string> { "TaskbarMonitorCore.comhost.dll" };

        var targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TaskbarMonitor");

        var info = new InstallInfo(filesToCopy, filesToRegister, targetPath);

        if (args.Length > 0 && args[0].ToLower() == "/uninstall")
        {
            RollBack(info);
        }
        else
        {
            Install(info);
        }

        // pause
        Console.WriteLine("Press any key to close this window...");
        Console.ReadKey();
    }

    private static void Install(InstallInfo info)
    {
        Console.WriteLine("Installing taskbar-monitor on your computer, please wait.");
        var restartExplorer = new RestartExplorer();

        // Create directory
        if (!Directory.Exists(info.TargetPath))
        {
            Console.WriteLine("Creating target directory... ");
            Directory.CreateDirectory(info.TargetPath);
            Console.WriteLine("OK.");

            // First copy files to program files folder          
            foreach (var file in info.FilesToCopy)
            {
                var item = file.Key;

                var targetFilePath = Path.Combine(info.TargetPath, item);
                Console.WriteLine(string.Format("Copying {0}... ", item));
                File.WriteAllBytes(targetFilePath, file.Value);
                //File.Copy(item, targetFilePath, true);
                Console.WriteLine("OK.");
            }
            // copy the uninstaller too
            File.Copy("TaskbarMonitorInstallerCore.exe", Path.Combine(info.TargetPath, "TaskbarMonitorInstallerCore.exe"));
        }
        else
        {

            restartExplorer.Execute(() =>
            {
                // First copy files to program files folder          
                foreach (var file in info.FilesToCopy)
                {
                    var item = file.Key;

                    var targetFilePath = Path.Combine(info.TargetPath, item);
                    Console.WriteLine($"Copying {item}... ");
                    File.WriteAllBytes(targetFilePath, file.Value);
                    //File.Copy(item, targetFilePath, true);
                    Console.WriteLine("OK.");
                }
                // copy the uninstaller too
                File.Copy("TaskbarMonitorInstallerCore.exe", Path.Combine(info.TargetPath, "TaskbarMonitorInstallerCore.exe"), true);
            });
        }

        // Register assemblies
        //RegistrationServices regAsm = new RegistrationServices();
        foreach (var item in info.FilesToRegister)
        {
            var targetFilePath = Path.Combine(info.TargetPath, item);
            Console.WriteLine($"Registering {item}... ");
            COMRegistration.RegSvr32(targetFilePath, true);
            Console.WriteLine("OK.");
        }

        Console.WriteLine("Registering uninstaller... ");
        CreateUninstaller(Path.Combine(info.TargetPath, "TaskbarMonitorInstallerCore.exe"));
        Console.WriteLine("OK.");

        // remove pending delete operations
        {
            Console.WriteLine("Cleaning up previous pending uninstalls... ");
            if (CleanUpPendingDeleteOperations(info.TargetPath, out var errorMessage))
            {
                Console.WriteLine("OK.");
            }
            else
            {
                Console.WriteLine("ERROR: " + errorMessage);
            }
        }
    }

    private static bool RollBack(InstallInfo info)
    {
        // Unregister assembly
        //RegistrationServices regAsm = new RegistrationServices();
        foreach (var item in info.FilesToRegister)
        {
            var targetFilePath = Path.Combine(info.TargetPath, item);
            COMRegistration.RegSvr32(targetFilePath, false);
        }

        // Delete files
        var restartExplorer = new RestartExplorer();
        restartExplorer.Execute(() =>
        {
            // First copy files to program files folder          
            foreach (var file in info.FilesToCopy)
            {
                var item = file.Key;
                var targetFilePath = Path.Combine(info.TargetPath, item);
                if (File.Exists(targetFilePath))
                {
                    Console.WriteLine($"Deleting {item}... ");
                    try
                    {
                        File.Delete(targetFilePath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    Console.WriteLine("OK.");
                }
            }

        });

        {
            var item = "TaskbarMonitorInstallerCore.exe";
            Console.WriteLine($"Deleting {item}... ");
            try
            {
                if (Win32Api.DeleteFile(Path.Combine(info.TargetPath, item)))
                {
                    Console.WriteLine("OK.");
                }
                else
                {
                    Win32Api.MoveFileEx(Path.Combine(info.TargetPath, item), null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                    Console.WriteLine("Scheduled for deletion after next reboot.");
                }
            }
            catch
            {
                Win32Api.MoveFileEx(Path.Combine(info.TargetPath, item), null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                Console.WriteLine("Scheduled for deletion after next reboot.");
            }

        }

        if (Directory.Exists(info.TargetPath))
        {
            Console.WriteLine("Deleting target directory... ");
            try
            {
                Directory.Delete(info.TargetPath);
                Console.WriteLine("OK.");
            }
            catch
            {
                Win32Api.MoveFileEx(info.TargetPath, null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                Console.WriteLine("Scheduled for deletion after next reboot.");
            }
        }
        Console.WriteLine("Removing uninstall info from registry... ");
        DeleteUninstaller();
        Console.WriteLine("OK.");

        return true;
    }

    private static void DeleteUninstaller()
    {
        var UninstallRegKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        using (var parent = Registry.LocalMachine.OpenSubKey(UninstallRegKeyPath, true))
        {
            if (parent is null)
            {
                throw new Exception("Uninstall registry key not found.");
            }

            var guidText = UninstallGuid.ToString("B");
            parent.DeleteSubKeyTree(guidText, false);
        }

    }

    static private void CreateUninstaller(string pathToUninstaller)
    {
        var UninstallRegKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        using (var parent = Registry.LocalMachine.OpenSubKey(UninstallRegKeyPath, true))
        {
            if (parent is null)
            {
                throw new Exception("Uninstall registry key not found.");
            }
            try
            {
                RegistryKey? key = null;

                try
                {
                    var guidText = UninstallGuid.ToString("B");
                    key = parent.OpenSubKey(guidText, true) ?? parent.CreateSubKey(guidText);

                    if (key == null)
                    {
                        throw new Exception(string.Format("Unable to create uninstaller '{0}\\{1}'", UninstallRegKeyPath, guidText));
                    }

                    var v = new Version("1.0.0");

                    var exe = pathToUninstaller;

                    key.SetValue("DisplayName", "taskbar-monitor");
                    key.SetValue("ApplicationVersion", v.ToString());
                    key.SetValue("Publisher", "taskbar-monitor-publisher");
                    key.SetValue("DisplayIcon", exe);
                    key.SetValue("DisplayVersion", v.ToString(3));
                    key.SetValue("URLInfoAbout", "About Text");
                    key.SetValue("Contact", "Contact Info");
                    key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                    key.SetValue("UninstallString", exe + " /uninstall");
                }
                finally
                {
                    if (key != null)
                    {
                        key.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var message = "An error occurred writing uninstall information to the registry.  The service is fully installed but can only be uninstalled manually through the command line.";
                throw new Exception(message, ex);
            }
        }
    }

    static bool CleanUpPendingDeleteOperations(string basepath, out string errorMessage)
    {
        // here we check the registry for pending operations on the program files (previous pending uninstall)
        try
        {
            var subKeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\";
            using (var key = Registry.LocalMachine.OpenSubKey(subKeyPath, true))
            {
                if (key is null)
                {
                    errorMessage = $"Sub key path: {subKeyPath} could not be opened.";
                    return false;
                }

                var keyName = "PendingFileRenameOperations";
                if (key.GetValue(keyName) is not string[] values)
                {
                    errorMessage = "Key: {keyName} is not a string[] or does not exist.";
                    return false;
                }

                var dest = new List<string>();
                for (var i = 0; i < values.Length; i += 2)
                {
                    if (!values[i].Contains(basepath))
                    {
                        dest.Add(values[i]);
                        dest.Add(values[i + 1]);
                    }
                }

                key.SetValue("PendingFileRenameOperations", dest.ToArray());
            }

            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            errorMessage = "An error occurred cleaning up previous uninstall information to the registry. The program might be partially uninstalled on the next reboot.";
            return false;
        }
    }
}
