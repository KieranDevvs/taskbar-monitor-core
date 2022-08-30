using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TaskbarMonitorInstallerCore.Windows;

class RestartExplorer : Win32Api
{
    public event Action<string>? ReportProgress;

    public event Action<uint>? ReportPercentage;

    public void Execute() => Execute();

    public void Execute(Action? action)
    {
        var key = Guid.NewGuid().ToString();

        var res = RmStartSession(out var handle, 0, key);
        if (res == 0)
        {
            ReportProgress?.Invoke($"Restart Manager session created with ID {key}");

            var processes = GetProcesses("explorer");
            res = RmRegisterResources(handle, 0, null, (uint)processes.Length, processes, 0, null);
            if (res == 0)
            {
                ReportProgress?.Invoke("Successfully registered resources.");

                res = RmShutdown(handle, RM_SHUTDOWN_TYPE.RmForceShutdown, (percent) => ReportPercentage?.Invoke(percent));
                if (res == 0)
                {
                    ReportProgress?.Invoke("Applications stopped successfully.");
                    if (action is not null)
                    {
                        action();
                    }

                    res = RmRestart(handle, 0, (percent) => ReportPercentage?.Invoke(percent));
                    if (res == 0)
                    {
                        ReportProgress?.Invoke("Applications restarted successfully.");
                    }
                }
            }

            res = RmEndSession(handle);
            if (res == 0)
            {
                ReportProgress?.Invoke("Restart Manager session ended.");
            }
        }
    }

    private static RM_UNIQUE_PROCESS[] GetProcesses(string name)
    {
        var processes = new List<RM_UNIQUE_PROCESS>();

        foreach (var p in Process.GetProcessesByName(name))
        {
            var rp = new RM_UNIQUE_PROCESS
            {
                dwProcessId = p.Id
            };

            GetProcessTimes(p.Handle, out var creationTime, out _, out _, out _);
            rp.ProcessStartTime = creationTime;
            processes.Add(rp);
        }

        return processes.ToArray();
    }
}
