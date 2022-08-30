using System.Collections.Generic;
using TaskbarMonitorCore;

namespace TaskbarMonitorCore.Counters;

public class CounterInfo
{
    public string Name { get; set; }
    public float MaximumValue { get; set; }
    public float CurrentValue { get; set; }
    public string CurrentStringValue { get; set; }
    public List<float> History { get; set; }
}

public abstract class BaseCounter
{
    public Options Options { get; private set; }

    public CounterInfo InfoSummary { get; protected set; }

    public List<CounterInfo> Infos { get; protected set; }

    public object ThreadLock { get; protected set; }


    public BaseCounter(Options options)
    {
        Options = options;
        ThreadLock = new object();
    }

    public abstract string GetName();

    public abstract void Initialize();

    public abstract void Update();

    public abstract CounterType GetCounterType();

    //public abstract List<CounterInfo> GetValues();
}
