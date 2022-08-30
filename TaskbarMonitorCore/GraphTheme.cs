using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;

namespace TaskbarMonitorCore;

public class GraphTheme
{
    public static readonly int LATESTTHEMEVERSION = 1;

    public int ThemeVersion = LATESTTHEMEVERSION;
    public Color BarColor { get; set; }
    public Color TextColor { get; set; }
    public Color TextShadowColor { get; set; }

    public Color TitleColor { get; set; }
    public Color TitleShadowColor { get; set; }
    public string TitleFont { get; set; } = "Arial";
    public FontStyle TitleFontStyle { get; set; } = FontStyle.Bold;
    public float TitleSize { get; set; } = 7f;

    public string CurrentValueFont { get; set; } = "Arial";
    public FontStyle CurrentValueFontStyle { get; set; } = FontStyle.Bold;
    public float CurrentValueSize { get; set; } = 7f;

    public List<Color> StackedColors { get; set; } = new List<Color>();

    public Color GetNthColor(int total, int n)
    {
        var colors = GetColorGradient(StackedColors.First(), StackedColors.Last(), total);
        return colors.ElementAt(n);
    }

    public static IEnumerable<Color> GetColorGradient(Color from, Color to, int totalNumberOfColors)
    {
        if (totalNumberOfColors < 2)
        {
            throw new ArgumentException("Gradient cannot have less than two colors.", nameof(totalNumberOfColors));
        }

        double diffA = to.A - from.A;
        double diffR = to.R - from.R;
        double diffG = to.G - from.G;
        double diffB = to.B - from.B;

        var steps = totalNumberOfColors - 1;

        var stepA = diffA / steps;
        var stepR = diffR / steps;
        var stepG = diffG / steps;
        var stepB = diffB / steps;

        yield return from;

        for (var i = 1; i < steps; ++i)
        {
            yield return Color.FromArgb(
                c(from.A, stepA),
                c(from.R, stepR),
                c(from.G, stepG),
                c(from.B, stepB));

            int c(int fromC, double stepC)
            {
                return (int)Math.Round(fromC + stepC * i);
            }
        }

        yield return to;
    }

    public void CopyTo(GraphTheme theme)
    {
        theme.BarColor = BarColor;
        theme.TextColor = TextColor;
        theme.TextShadowColor = TextShadowColor;
        theme.TitleColor = TitleColor;
        theme.TitleShadowColor = TitleShadowColor;
        theme.TitleFont = TitleFont;
        theme.TitleFontStyle = TitleFontStyle;
        theme.TitleSize = TitleSize;
        theme.CurrentValueFont = CurrentValueFont;
        theme.CurrentValueFontStyle = CurrentValueFontStyle;
        theme.CurrentValueSize = CurrentValueSize;

        theme.StackedColors = new List<Color>();

        foreach (var item in StackedColors)
        {
            theme.StackedColors.Add(item);
        }
    }
    public static GraphTheme DefaultDarkTheme()
    {
        return new GraphTheme
        {
            BarColor = Color.FromArgb(255, 176, 222, 255),
            TextColor = Color.FromArgb(200, 185, 255, 70),
            TextShadowColor = Color.FromArgb(255, 0, 0, 0),
            TitleColor = Color.FromArgb(255, 255, 255, 255),
            TitleShadowColor = Color.FromArgb(255, 0, 0, 0),
            TitleFont = "Arial",
            TitleFontStyle = FontStyle.Bold,
            TitleSize = 7f,
            CurrentValueFont = "Arial",
            CurrentValueFontStyle = FontStyle.Bold,
            CurrentValueSize = 7f,
            StackedColors = new List<Color>
            {
                Color.FromArgb(255, 37, 84, 142) ,
                Color.FromArgb(255, 65, 144, 242)
            }
        };
    }
    public static GraphTheme DefaultLightTheme()
    {
        return new GraphTheme
        {
            BarColor = Color.FromArgb(255, 0, 0, 0),
            TextColor = Color.FromArgb(200, 255, 0, 128),
            TextShadowColor = Color.FromArgb(255, 234, 234, 234),
            TitleColor = Color.FromArgb(255, 0, 0, 0),
            TitleShadowColor = Color.FromArgb(255, 214, 214, 214),
            TitleFont = "Arial",
            TitleFontStyle = FontStyle.Bold,
            TitleSize = 7f,
            CurrentValueFont = "Arial",
            CurrentValueFontStyle = FontStyle.Bold,
            CurrentValueSize = 7f,
            StackedColors = new List<Color>
            {
                Color.FromArgb(255, 102, 102, 102) ,
                Color.FromArgb(255, 145, 145, 145)
            }
        };
    }
    public static GraphTheme ReadFromDisk()
    {
        var theme = DefaultDarkTheme();

        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "taskbar-monitor");
        var origin = Path.Combine(folder, "theme.json");
        if (!File.Exists(origin))
        {
            return theme;
        }

        var deserializedGraphTheme = JsonSerializer.Deserialize<GraphTheme>(File.ReadAllText(origin));
        if (deserializedGraphTheme is null)
        {
            return theme;
        }

        theme = deserializedGraphTheme;
        if (theme.Upgrade()) // do a inplace upgrade
        {
            theme.SaveToDisk();
        }

        return theme;
    }
    public bool SaveToDisk()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "taskbar-monitor");
        var origin = Path.Combine(folder, "theme.json");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(origin, JsonSerializer.Serialize(this));
        return true;
    }
    private bool Upgrade()
    {
        if (LATESTTHEMEVERSION > ThemeVersion)
        {
            switch (ThemeVersion)
            {
                case 0:
                    ThemeVersion = LATESTTHEMEVERSION;
                    return true;
                case 1:
                default:
                    break;
            }
        }
        return false;
    }

    public static bool IsCustom(GraphTheme theme)
    {
        var light = DefaultLightTheme();
        var dark = DefaultDarkTheme();
        if (theme.BarColor.ToArgb() != light.BarColor.ToArgb() && theme.BarColor.ToArgb() != dark.BarColor.ToArgb())
            return true;
        if (theme.TextColor.ToArgb() != light.TextColor.ToArgb() && theme.TextColor.ToArgb() != dark.TextColor.ToArgb())
            return true;
        if (theme.TextShadowColor.ToArgb() != light.TextShadowColor.ToArgb() && theme.TextShadowColor.ToArgb() != dark.TextShadowColor.ToArgb())
            return true;
        if (theme.TitleColor.ToArgb() != light.TitleColor.ToArgb() && theme.TitleColor.ToArgb() != dark.TitleColor.ToArgb())
            return true;
        if (theme.TitleShadowColor.ToArgb() != light.TitleShadowColor.ToArgb() && theme.TitleShadowColor.ToArgb() != dark.TitleShadowColor.ToArgb())
            return true;
        if (!theme.TitleFont.Equals(light) && !theme.TitleFont.Equals(dark))
            return true;
        if (!theme.TitleSize.Equals(light) && !theme.TitleSize.Equals(dark))
            return true;

        var i = 0;

        foreach (var item in theme.StackedColors)
        {
            if (light.StackedColors.Count <= i) return true;
            if (dark.StackedColors.Count <= i) return true;
            if (item.ToArgb() != light.StackedColors[i].ToArgb() && item.ToArgb() != dark.StackedColors[i].ToArgb())
                return true;
            i++;
        }

        return false;
    }

}
