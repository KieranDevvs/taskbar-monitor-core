using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaskbarMonitorCore;

namespace TaskbarMonitorCore.Counters;

public class CounterDisk : BaseCounter
{
    private PerformanceCounter _diskReadCounter;
    private PerformanceCounter _diskWriteCounter;

    public CounterDisk(Options options) : base(options)
    {
        _diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
        _diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
    }

    public override void Initialize()
    {
        lock (ThreadLock)
        {
            InfoSummary = new CounterInfo() { Name = "summary", History = new List<float>(), MaximumValue = 1 };
            Infos = new List<CounterInfo>
            {
                new CounterInfo() { Name = "R", History = new List<float>(), MaximumValue = 1 },
                new CounterInfo() { Name = "W", History = new List<float>(), MaximumValue = 1 }
            };
        }
    }
    public override void Update()
    {
        void addValue(CounterInfo info, float value)
        {
            info.CurrentValue = value;
            info.History.Add(value);
            if (info.History.Count > Options.HistorySize) info.History.RemoveAt(0);
            info.MaximumValue = Convert.ToInt64(info.History.Max()) + 1;

            info.CurrentStringValue = (info.CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
        }

        var currentRead = _diskReadCounter.NextValue();
        var currentWritten = _diskWriteCounter.NextValue();

        lock (ThreadLock)
        {
            addValue(InfoSummary, currentRead + currentWritten);
            addValue(Infos.Where(x => x.Name == "R").Single(), currentRead);
            addValue(Infos.Where(x => x.Name == "W").Single(), currentWritten);

            // if locks down same scale for both counters is on
            if (!Options.CounterOptions["DISK"].SeparateScales)
            {
                var info1 = Infos.Where(x => x.Name == "R").Single();
                var info2 = Infos.Where(x => x.Name == "W").Single();

                var max = info1.MaximumValue > info2.MaximumValue ? info1.MaximumValue : info2.MaximumValue;
                info1.MaximumValue = info2.MaximumValue = max;
            }
        }
    }

    public override string GetName()
    {
        return "DISK";
    }

    public override CounterType GetCounterType()
    {
        return Options.CounterOptions["DISK"].GraphType;// ? CounterType.SINGLE : CounterType.MIRRORED;
    }
}
