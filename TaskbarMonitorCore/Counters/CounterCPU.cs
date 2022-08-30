using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TaskbarMonitorCore.Counters;

public class CounterCPU : BaseCounter
{
    private PerformanceCounter _cpuCounter;
    private List<PerformanceCounter> _cpuCounterCores;
    private float _currentValue = 0;

    public CounterCPU(Options options) : base(options)
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _cpuCounterCores = new List<PerformanceCounter>();

        Infos = new List<CounterInfo>();
        InfoSummary = new CounterInfo()
        {
            Name = "summary",
            History = new List<float>(),
            MaximumValue = 100.0f
        };
    }

    public override void Initialize()
    {
        var cat = new PerformanceCounterCategory("Processor");
        var instances = cat.GetInstanceNames();

        lock (ThreadLock)
        {
            foreach (var item in instances.OrderBy(x => x))
            {
                if (item.ToLower().Contains("_total")) continue;

                var counterInfo = new CounterInfo()
                {
                    Name = item,
                    History = new List<float>(),
                    MaximumValue = 100.0f
                };

                Infos.Add(counterInfo);
                _cpuCounterCores.Add(new PerformanceCounter("Processor", "% Processor Time", item));
            }
        }

    }
    public override void Update()
    {
        _currentValue = _cpuCounter.NextValue();

        lock (ThreadLock)
        {
            InfoSummary.CurrentValue = _currentValue;
            InfoSummary.History.Add(_currentValue);
            if (InfoSummary.History.Count > Options.HistorySize) InfoSummary.History.RemoveAt(0);
            InfoSummary.CurrentStringValue = InfoSummary.CurrentValue.ToString("0") + "%";

            foreach (var item in _cpuCounterCores)
            {
                var ct = Infos.Where(x => x.Name == item.InstanceName).Single();

                ct.CurrentValue = item.NextValue();
                ct.History.Add(ct.CurrentValue);
                if (ct.History.Count > Options.HistorySize) ct.History.RemoveAt(0);

                ct.CurrentStringValue = InfoSummary.CurrentStringValue;// same string value from summary
            }
        }

    }

    public override string GetName()
    {
        return "CPU";
    }

    public override CounterType GetCounterType()
    {
        return Options.CounterOptions["CPU"].GraphType;
    }
}
