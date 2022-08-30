using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using TaskbarMonitorCore;

namespace TaskbarMonitorCore.Counters;

class CounterMemory : BaseCounter
{

    public CounterMemory(Options options) : base(options)
    {
        _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

    private PerformanceCounter _ramCounter;
    private long totalMemory = 0;

    public override void Initialize()
    {
        GetPhysicallyInstalledSystemMemory(out totalMemory);
        lock (ThreadLock)
        {
            InfoSummary = new CounterInfo() { Name = "summary", History = new List<float>(), MaximumValue = totalMemory / 1024 };
            Infos = new List<CounterInfo>
            {
                new CounterInfo() { Name = "U", History = new List<float>(), MaximumValue = totalMemory / 1024 }
            };
        }
    }
    public override void Update()
    {
        var currentValue = totalMemory / 1024 - _ramCounter.NextValue();

        lock (ThreadLock)
        {
            InfoSummary.CurrentValue = currentValue;
            InfoSummary.History.Add(currentValue);
            if (InfoSummary.History.Count > Options.HistorySize) InfoSummary.History.RemoveAt(0);

            InfoSummary.CurrentStringValue = (InfoSummary.CurrentValue / 1024).ToString("0.0") + "GB";

            {
                var info = Infos.Where(x => x.Name == "U").Single();
                info.CurrentValue = currentValue;
                info.History.Add(currentValue);
                if (info.History.Count > Options.HistorySize) info.History.RemoveAt(0);

                info.CurrentStringValue = (info.CurrentValue / 1024).ToString("0.0") + "GB";
            }

        }


    }


    public override string GetName()
    {
        return "MEM";
    }

    public override CounterType GetCounterType()
    {
        return CounterType.SINGLE;
    }
}
