using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaskbarMonitorCore;

namespace TaskbarMonitorCore.Counters;

class CounterNetwork : BaseCounter
{
    private List<PerformanceCounter> _netCountersSent;
    private List<PerformanceCounter> _netCountersReceived;

    public CounterNetwork(Options options) : base(options)
    {
        _netCountersSent = new List<PerformanceCounter>();
        _netCountersReceived = new List<PerformanceCounter>();

        InfoSummary = new CounterInfo()
        {
            Name = "summary",
            History = new List<float>(),
            MaximumValue = 1
        };

        Infos = new List<CounterInfo>
        {
            new CounterInfo() { Name = "D", History = new List<float>(), MaximumValue = 1 },
            new CounterInfo() { Name = "U", History = new List<float>(), MaximumValue = 1 }
        };
    }

    public override void Initialize()
    {
        ReadCounters();
    }

    private void ReadCounters()
    {
        var pcg = new PerformanceCounterCategory("Network Interface");
        var instances = pcg.GetInstanceNames();

        foreach (var instance in instances)
        {
            _netCountersSent.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance));
            _netCountersReceived.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", instance));
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

            if (info.CurrentValue > 1024 * 1024)
                info.CurrentStringValue = (info.CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
            else
                info.CurrentStringValue = (info.CurrentValue / 1024).ToString("0.0") + "KB/s";
        }

        var success = false;
        var maxRetries = 1;
        var retries = 0;

        while (!success && retries <= maxRetries)
        {
            try
            {
                var currentSent = 0f;
                var currentReceived = 0f;

                foreach (var netCounter in _netCountersSent)
                {
                    currentSent += netCounter.NextValue();
                }

                foreach (var netCounter in _netCountersReceived)
                {
                    currentReceived += netCounter.NextValue();
                }

                lock (ThreadLock)
                {
                    addValue(InfoSummary, currentSent + currentReceived);
                    addValue(Infos.Where(x => x.Name == "D").Single(), currentReceived);
                    addValue(Infos.Where(x => x.Name == "U").Single(), currentSent);

                    // if locks down same scale for both counters is on
                    if (!Options.CounterOptions["NET"].SeparateScales)
                    {
                        var info1 = Infos.Where(x => x.Name == "D").Single();
                        var info2 = Infos.Where(x => x.Name == "U").Single();

                        var max = info1.MaximumValue > info2.MaximumValue ? info1.MaximumValue : info2.MaximumValue;
                        info1.MaximumValue = info2.MaximumValue = max;
                    }
                    success = true;
                }
            }
            catch
            {
                // in this case we have to reevaluate the counters
                ReadCounters();
                retries++;
            }
        }
    }

    public override string GetName()
    {
        return "NET";
    }

    public override CounterType GetCounterType()
    {
        return Options.CounterOptions["NET"].GraphType;
    }
}
