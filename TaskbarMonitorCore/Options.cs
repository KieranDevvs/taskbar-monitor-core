using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TaskbarMonitorCore.Counters;

namespace TaskbarMonitorCore;

public class Options
{
    public enum ThemeList
    {
        AUTOMATIC,
        DARK,
        LIGHT,
        CUSTOM
    }

    public static readonly int LATESTOPTIONSVERSION = 2;

    public int OptionsVersion = LATESTOPTIONSVERSION;

    public Dictionary<string, CounterOptions> CounterOptions { get; set; } = new Dictionary<string, CounterOptions>();

    public int HistorySize { get; set; } = 50;

    public int PollTime { get; set; } = 3;

    public ThemeList ThemeType { get; set; } = ThemeList.DARK;

    public void CopyTo(Options opt)
    {
        opt.HistorySize = HistorySize;
        opt.PollTime = PollTime;
        opt.ThemeType = ThemeType;

        foreach (var item in CounterOptions)
        {
            if (!opt.CounterOptions.ContainsKey(item.Key))
            {
                opt.CounterOptions.Add(item.Key, new CounterOptions());
            }

            opt.CounterOptions[item.Key].ShowTitle = item.Value.ShowTitle;
            opt.CounterOptions[item.Key].Enabled = item.Value.Enabled;
            opt.CounterOptions[item.Key].TitlePosition = item.Value.TitlePosition;
            opt.CounterOptions[item.Key].ShowTitleShadowOnHover = item.Value.ShowTitleShadowOnHover;
            opt.CounterOptions[item.Key].ShowCurrentValue = item.Value.ShowCurrentValue;
            opt.CounterOptions[item.Key].ShowCurrentValueShadowOnHover = item.Value.ShowCurrentValueShadowOnHover;
            opt.CounterOptions[item.Key].CurrentValueAsSummary = item.Value.CurrentValueAsSummary;
            opt.CounterOptions[item.Key].SummaryPosition = item.Value.SummaryPosition;
            opt.CounterOptions[item.Key].InvertOrder = item.Value.InvertOrder;
            opt.CounterOptions[item.Key].SeparateScales = item.Value.SeparateScales;
            opt.CounterOptions[item.Key].GraphType = item.Value.GraphType;
        }
    }
    public static Options DefaultOptions()
    {
        return new Options
        {
            CounterOptions = new Dictionary<string, CounterOptions>
            {
                { "CPU", new CounterOptions { GraphType = CounterType.SINGLE } },
                { "MEM", new CounterOptions { GraphType = CounterType.SINGLE } },
                { "DISK", new CounterOptions { GraphType = CounterType.SINGLE } },
                { "NET", new CounterOptions { GraphType = CounterType.SINGLE } }
            },
            HistorySize = 50,
            PollTime = 3,
            ThemeType = ThemeList.DARK
        };
    }

    public static Options ReadFromDisk()
    {
        var defaultOptions = DefaultOptions();

        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "taskbar-monitor");
        var origin = Path.Combine(folder, "config.json");
        if (!File.Exists(origin))
        {
            return defaultOptions;
        }

        var file = File.ReadAllText(origin);
        var deserializedOptions = JsonSerializer.Deserialize<Options>(file);
        if (deserializedOptions is null)
        {
            return defaultOptions;
        }

        return deserializedOptions;
    }

    public bool Upgrade(GraphTheme graphTheme)
    {
        if (_Upgrade(graphTheme)) // do a inplace upgrade
        {
            return SaveToDisk();
        }

        return false;
    }

    public bool SaveToDisk()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "taskbar-monitor");
        var origin = Path.Combine(folder, "config.json");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(origin, JsonSerializer.Serialize(this));
        return true;
    }
    private bool _Upgrade(GraphTheme graphTheme)
    {
        if (LATESTOPTIONSVERSION > OptionsVersion)
        {
            switch (OptionsVersion)
            {
                case 0:
                //this.OptionsVersion = LATESTOPTIONSVERSION;
                //return true;
                case 1:
                    if (GraphTheme.IsCustom(graphTheme))
                        ThemeType = ThemeList.CUSTOM;
                    else
                        ThemeType = ThemeList.AUTOMATIC;
                    OptionsVersion = LATESTOPTIONSVERSION;
                    return true;
                default:
                    break;
            }
        }
        return false;
    }
}

public class CounterOptions
{
    public enum DisplayType
    {
        HIDDEN,
        SHOW,
        HOVER
    }
    public enum DisplayPosition
    {
        TOP,
        BOTTOM,
        MIDDLE
    }
    public bool Enabled { get; set; } = true;
    public DisplayType ShowTitle { get; set; } = DisplayType.HOVER;
    public DisplayPosition TitlePosition { get; set; } = DisplayPosition.MIDDLE;
    public bool ShowTitleShadowOnHover { get; set; } = true;
    public DisplayType ShowCurrentValue { get; set; } = DisplayType.SHOW;
    public bool ShowCurrentValueShadowOnHover { get; set; } = true;
    public bool CurrentValueAsSummary { get; set; } = true;
    public DisplayPosition SummaryPosition { get; set; } = DisplayPosition.TOP;
    public bool InvertOrder { get; set; } = false;
    public bool SeparateScales { get; set; } = true;
    public CounterType GraphType { get; set; }
}
