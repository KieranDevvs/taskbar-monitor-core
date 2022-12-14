using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using TaskbarMonitorCore;
using TaskbarMonitorCore.Counters;
using WindowsApiLibrary;

// control architecture

// Deskband
//      Options (class holding all options loaded from disk)
//      SystemWatcherControl(Options) (main control that displays graph and has context menu)
//      Settings dialog window (receives copy of options)
//          SystemWatcherControl(CopyOfOptions) (another instance for preview)        
namespace TaskbarMonitor;

public partial class SystemWatcherControl : UserControl
{
    public delegate void SizeChangeHandler(Size size);

    public event SizeChangeHandler OnChangeSize;

    public Version Version { get; set; } = new Version("1.0.0");

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Options Options { get; set; }

    private bool _previewMode = false;

    private ContextMenuStrip? _contextMenu = null;

    private bool _verticalTaskbarMode = false;

    private System.Timers.Timer _pollingTimer;

    public bool PreviewMode
    {
        get
        {
            return _previewMode;
        }
        set
        {
            _previewMode = value;
            ContextMenuStrip = _previewMode ? null : _contextMenu;
        }
    }
    public int CountersCount
    {
        get
        {
            if (Counters == null) return 0;
            return Options.CounterOptions.Where(x => x.Value.Enabled == true).Count();
            //return Counters.Count;
        }
    }
    List<BaseCounter> Counters;
    Font fontCounter;
    Font fontTitle;
    int lastSize = 30;
    bool mouseOver = false;
    GraphTheme defaultTheme;

    public SystemWatcherControl(Options opt)//CSDeskBand.CSDeskBandWin w, 
    {
        try
        {
            var theme = GetTheme(opt);
            opt.Upgrade(theme);

            Initialize(opt, theme);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading SystemWatcherControl: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public SystemWatcherControl()
    {
        try
        {
            Options opt = Options.ReadFromDisk();
            var theme = GetTheme(opt);
            opt.Upgrade(theme);

            Initialize(opt, theme);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading SystemWatcherControl: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static GraphTheme GetTheme(Options opt)
    {
        var theme = GraphTheme.DefaultDarkTheme();

        if (opt.ThemeType == Options.ThemeList.LIGHT)
            theme = GraphTheme.DefaultLightTheme();
        else if (opt.ThemeType == Options.ThemeList.CUSTOM)
            theme = GraphTheme.ReadFromDisk();
        else if (opt.ThemeType == Options.ThemeList.AUTOMATIC)
        {
            Color taskBarColour = Win32Api.GetColourAt(Win32Api.GetTaskbarPosition().Location);
            if (taskBarColour.R + taskBarColour.G + taskBarColour.B > 382)
                theme = GraphTheme.DefaultLightTheme();
            else
                theme = GraphTheme.DefaultDarkTheme();
        }
        return theme;
    }

    private void PollingTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        UpdateGraphs();

        if (Options.ThemeType == Options.ThemeList.AUTOMATIC)
        {
            defaultTheme = GetTheme(Options);
        }

        Invalidate();
    }

    public bool IsCustomTheme()
    {
        return GraphTheme.IsCustom(defaultTheme);
    }
    public void ApplyOptions(Options Options)
    {
        ApplyOptions(Options, GetTheme(Options));
    }

    public void ApplyOptions(Options Options, GraphTheme theme)
    {
        this.Options = Options;
        defaultTheme = theme;

        fontTitle = new Font(defaultTheme.TitleFont, defaultTheme.TitleSize, defaultTheme.TitleFontStyle);
        fontCounter = new Font(defaultTheme.CurrentValueFont, defaultTheme.CurrentValueSize, defaultTheme.CurrentValueFontStyle);

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add(new ToolStripButton("Settings...", null, MenuItem_Settings_onClick));
        _contextMenu.Items.Add(new ToolStripButton("Open Resource Monitor...", null, (e, a) =>
        {
            System.Diagnostics.Process.Start("resmon.exe");
        }));
        _contextMenu.Items.Add(new ToolStripButton($"About taskbar-monitor (v{Version.ToString(3)})...", null, MenuItem_About_onClick));
        ContextMenuStrip = _contextMenu;


        if (PreviewMode)
        {
            Color taskBarColour = Win32Api.GetColourAt(Win32Api.GetTaskbarPosition().Location);
            BackColor = taskBarColour;

        }

        /*
            float dpiX, dpiY;
            using (Graphics graphics = this.CreateGraphics())
            {
                dpiX = graphics.DpiX;
                dpiY = graphics.DpiY;
            }
            float fontSize = 7f;
            if (dpiX > 96)
                fontSize = 6f;
            */

        AdjustControlSize();
        UpdateGraphs();
        Invalidate();

    }
    private void Initialize(Options opt, GraphTheme theme)
    {

        Counters = new List<BaseCounter>();
        if (opt.CounterOptions.ContainsKey("CPU"))
        {
            var ct = new CounterCPU(opt);
            ct.Initialize();
            Counters.Add(ct);
        }
        if (opt.CounterOptions.ContainsKey("MEM"))
        {
            var ct = new CounterMemory(opt);
            ct.Initialize();
            Counters.Add(ct);
        }
        if (opt.CounterOptions.ContainsKey("DISK"))
        {
            var ct = new CounterDisk(opt);
            ct.Initialize();
            Counters.Add(ct);
        }
        if (opt.CounterOptions.ContainsKey("NET"))
        {
            var ct = new CounterNetwork(opt);
            ct.Initialize();
            Counters.Add(ct);
        }

        ApplyOptions(opt, theme);
        //Initialize();
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.DoubleBuffer, true);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.UserPaint, true);

        InitializeComponent();
        AdjustControlSize();

        _pollingTimer = new System.Timers.Timer(opt.PollTime * 1000);
        _pollingTimer.Enabled = true;
        _pollingTimer.Elapsed += PollingTimer_Elapsed;
        _pollingTimer.Start();

    }

    private void AdjustControlSize()
    {
        int taskbarWidth = GetTaskbarWidth();
        int taskbarHeight = GetTaskbarHeight();
        int minimumHeight = taskbarHeight;
        if (minimumHeight < 30)
            minimumHeight = 30;

        if (taskbarWidth > 0 && taskbarHeight == 0)
            _verticalTaskbarMode = true;

        int counterSize = (Options.HistorySize + 10);
        int controlWidth = counterSize * CountersCount;
        int controlHeight = minimumHeight;

        if (_verticalTaskbarMode && taskbarWidth < controlWidth)
        {
            int countersPerLine = Convert.ToInt32(Math.Floor((float)taskbarWidth / (float)counterSize));
            controlWidth = counterSize * countersPerLine;
            controlHeight = Convert.ToInt32(Math.Ceiling((float)CountersCount / (float)countersPerLine)) * (30 + 10);
        }
        if (Size.Width != controlWidth || Size.Height != controlHeight)
        {
            Size = new Size(controlWidth, controlHeight);
            if (OnChangeSize != null)
                OnChangeSize(new Size(controlWidth, controlHeight));
        }
    }

    private void UpdateGraphs()
    {
        foreach (var ct in Counters)
        {
            ct.Update();
        }
        if (_pollingTimer != null && _pollingTimer.Interval != Options.PollTime * 1000)
            _pollingTimer.Interval = Options.PollTime * 1000;
    }

    private void SystemWatcherControl_Paint(object sender, PaintEventArgs e)
    {
        int maximumHeight = GetTaskbarHeight();
        if (maximumHeight <= 0)
            maximumHeight = 30;

        if (lastSize != maximumHeight)
        {
            Height = maximumHeight;
            lastSize = maximumHeight;
        }

        int graphPosition = 0;
        int graphPositionY = 0;


        Graphics formGraphics = e.Graphics;// this.CreateGraphics();
        formGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

        //formGraphics.Clear(Color.Transparent);
        foreach (var pair in Options.CounterOptions.Where(x => x.Value.Enabled == true))
        {
            var name = pair.Key;
            var opt = pair.Value;
            var ct = Counters.Where(x => x.GetName() == name).Single();
            var infos = ct.Infos;
            //var opt = Options.CounterOptions[ct.GetName()];
            //if (!opt.Enabled) continue;
            var showCurrentValue = !opt.CurrentValueAsSummary &&
                (opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW || (opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver));

            lock (ct.ThreadLock)
            {
                if (ct.GetCounterType() == CounterType.SINGLE)
                {
                    var info = infos[0];
                    DrawGraph(formGraphics, graphPosition, 0 + graphPositionY, maximumHeight, false, info, defaultTheme, opt);

                }
                else if (ct.GetCounterType() == CounterType.MIRRORED)
                {


                    for (int z = 0; z < infos.Count; z++)
                    {
                        var info = opt.InvertOrder ? infos[infos.Count - 1 - z] : infos[z];
                        DrawGraph(formGraphics, graphPosition, z * (maximumHeight / 2) + graphPositionY, maximumHeight / 2, z == 1, info, defaultTheme, opt);
                    }


                }
                else if (ct.GetCounterType() == CounterType.STACKED)
                {
                    DrawStackedGraph(formGraphics, graphPosition, 0 + graphPositionY, maximumHeight, opt.InvertOrder, infos, defaultTheme, opt);


                }
            }

            var sizeTitle = formGraphics.MeasureString(ct.GetName(), fontTitle);
            Dictionary<CounterOptions.DisplayPosition, float> positions = new Dictionary<CounterOptions.DisplayPosition, float>();

            positions.Add(CounterOptions.DisplayPosition.MIDDLE, (maximumHeight / 2 - sizeTitle.Height / 2) + 1 + graphPositionY);
            positions.Add(CounterOptions.DisplayPosition.TOP, graphPositionY);
            positions.Add(CounterOptions.DisplayPosition.BOTTOM, (maximumHeight - sizeTitle.Height + 1) + graphPositionY);

            CounterOptions.DisplayPosition? usedPosition = null;
            if (opt.ShowTitle == CounterOptions.DisplayType.SHOW
             || opt.ShowTitle == CounterOptions.DisplayType.HOVER)
            {

                usedPosition = opt.TitlePosition;
                var titleShadow = defaultTheme.TitleShadowColor;
                var titleColor = defaultTheme.TitleColor;

                if (opt.ShowTitle == CounterOptions.DisplayType.HOVER && !mouseOver)
                {
                    titleColor = Color.FromArgb(40, titleColor.R, titleColor.G, titleColor.B);
                }


                SolidBrush brushShadow = new SolidBrush(titleShadow);
                SolidBrush brushTitle = new SolidBrush(titleColor);


                if (
                    (opt.ShowTitleShadowOnHover && opt.ShowTitle == CounterOptions.DisplayType.HOVER && !mouseOver)
                    || (opt.ShowTitle == CounterOptions.DisplayType.HOVER && mouseOver)
                    || opt.ShowTitle == CounterOptions.DisplayType.SHOW
                   )
                {
                    if ((opt.ShowTitle == CounterOptions.DisplayType.HOVER && mouseOver) || opt.ShowTitle == CounterOptions.DisplayType.SHOW)
                    {
                        formGraphics.DrawString(ct.GetName(), fontTitle, brushShadow, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2) + 1, positions[opt.TitlePosition] + 1, sizeTitle.Width, maximumHeight), new StringFormat());
                    }
                    formGraphics.DrawString(ct.GetName(), fontTitle, brushTitle, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2), positions[opt.TitlePosition], sizeTitle.Width, maximumHeight), new StringFormat());
                }


                brushShadow.Dispose();
                brushTitle.Dispose();
            }

            if (opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW
             || opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER)
            {
                Dictionary<CounterOptions.DisplayPosition, string> texts = new Dictionary<CounterOptions.DisplayPosition, string>();

                if (opt.CurrentValueAsSummary || infos.Count > 2)
                {
                    texts.Add(opt.SummaryPosition, ct.InfoSummary.CurrentStringValue);

                }
                else
                {
                    List<CounterOptions.DisplayPosition> positionsAvailable = new List<CounterOptions.DisplayPosition> { CounterOptions.DisplayPosition.TOP, CounterOptions.DisplayPosition.MIDDLE, CounterOptions.DisplayPosition.BOTTOM };
                    if (usedPosition.HasValue)
                        positionsAvailable.Remove(usedPosition.Value);
                    var showName = infos.Count > 1;
                    for (int i = 0; i < infos.Count && i < 2; i++)
                    {
                        texts.Add(positionsAvailable[i], (showName ? infos[i].Name + " " : "") + infos[i].CurrentStringValue);
                    }
                }
                foreach (var item in texts)
                {
                    string text = item.Value;

                    var sizeString = formGraphics.MeasureString(text, fontCounter);
                    float ypos = positions[item.Key];

                    var titleShadow = defaultTheme.TextShadowColor;
                    var titleColor = defaultTheme.TextColor;

                    if (opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && !mouseOver)
                    {
                        titleColor = Color.FromArgb(40, titleColor.R, titleColor.G, titleColor.B);
                        //titleShadow = Color.FromArgb(40, titleShadow.R, titleShadow.G, titleShadow.B);
                    }

                    SolidBrush BrushText = new SolidBrush(titleColor);
                    SolidBrush BrushTextShadow = new SolidBrush(titleShadow);

                    if (
                    (opt.ShowCurrentValueShadowOnHover && opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && !mouseOver)
                    || (opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver)
                    || opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW
                   )
                    {
                        if ((opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver) || opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW)
                        {
                            formGraphics.DrawString(text, fontCounter, BrushTextShadow, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeString.Width / 2) + 1, ypos + 1, sizeString.Width, maximumHeight), new StringFormat());
                        }
                        formGraphics.DrawString(text, fontCounter, BrushText, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeString.Width / 2), ypos, sizeString.Width, maximumHeight), new StringFormat());
                    }
                    BrushText.Dispose();
                    BrushTextShadow.Dispose();
                }
            }


            graphPosition += Options.HistorySize + 10;
            if (graphPosition >= Size.Width)
            {
                graphPosition = 0;
                graphPositionY += (maximumHeight + 10);
            }
        }
        AdjustControlSize();

    }


    private void DrawGraph(Graphics formGraphics, int x, int y, int maxH, bool invertido, CounterInfo info, GraphTheme theme, CounterOptions opt)
    {
        var pos = maxH - ((info.CurrentValue * maxH) / info.MaximumValue);
        if (pos > int.MaxValue) pos = int.MaxValue;
        int posInt = Convert.ToInt32(pos) + y;

        var height = (info.CurrentValue * maxH) / info.MaximumValue;
        if (height > int.MaxValue) height = int.MaxValue;
        int heightInt = Convert.ToInt32(height);

        using (SolidBrush BrushBar = new SolidBrush(theme.BarColor))
        {
            if (invertido)
                formGraphics.FillRectangle(BrushBar, new Rectangle(x + Options.HistorySize, maxH, 4, heightInt));
            else
                formGraphics.FillRectangle(BrushBar, new Rectangle(x + Options.HistorySize, posInt, 4, heightInt));
        }

        var initialGraphPosition = x + Options.HistorySize - info.History.Count;
        Point[] points = new Point[info.History.Count + 2];
        int i = 0;
        int inverter = invertido ? -1 : 1;
        foreach (var item in info.History)
        {
            var heightItem = (item * maxH) / info.MaximumValue;
            if (heightItem > int.MaxValue) height = int.MaxValue;
            var convertido = Convert.ToInt32(heightItem);


            if (invertido)
                points[i] = new Point(initialGraphPosition + i, 0 + convertido + y);
            else
                points[i] = new Point(initialGraphPosition + i, maxH - convertido + y);
            i++;
        }
        if (invertido)
        {
            points[i] = new Point(initialGraphPosition + i, 0 + y);
            points[i + 1] = new Point(initialGraphPosition, 0 + y);
        }
        else
        {
            points[i] = new Point(initialGraphPosition + i, maxH + y);
            points[i + 1] = new Point(initialGraphPosition, maxH + y);
        }
        using (SolidBrush BrushGraph = new SolidBrush(theme.GetNthColor(2, invertido ? 1 : 0)))
        {
            formGraphics.FillPolygon(BrushGraph, points);
        }

    }

    private void DrawStackedGraph(Graphics formGraphics, int x, int y, int maxH, bool invertido, List<CounterInfo> infos, GraphTheme theme, CounterOptions opt)
    {
        float absMax = 0;
        var lastValue = new List<float>();

        // accumulate values for stacked effect
        var values = new List<List<float>>();
        foreach (var info in infos.AsEnumerable().Reverse())
        {
            absMax += info.MaximumValue;
            var value = new List<float>();
            int z = 0;
            foreach (var item in info.History)
            {
                value.Add(item + (lastValue.Count > 0 ? lastValue.ElementAt(z) : 0));
                z++;
            }
            values.Add(value);
            lastValue = value;
        }
        var historySize = values.Count > 0 ? values[0].Count : 0;
        // now we draw it

        var colors = GraphTheme.GetColorGradient(theme.StackedColors[0], theme.StackedColors[1], values.Count);
        int w = 0;
        if (!invertido)
            values.Reverse();
        foreach (var info in values)
        {
            float currentValue = info.Count > 0 ? info.Last() : 0;
            var pos = maxH - ((currentValue * maxH) / absMax);
            if (pos > int.MaxValue) pos = int.MaxValue;
            int posInt = Convert.ToInt32(pos) + y;

            var height = (currentValue * maxH) / absMax;
            if (height > int.MaxValue) height = int.MaxValue;
            int heightInt = Convert.ToInt32(height);

            SolidBrush BrushBar = new SolidBrush(theme.BarColor);
            formGraphics.FillRectangle(BrushBar, new Rectangle(x + Options.HistorySize, posInt, 4, heightInt));
            BrushBar.Dispose();

            int i = 0;
            var initialGraphPosition = x + Options.HistorySize - historySize;
            Point[] points = new Point[historySize + 2];
            foreach (var item in info)
            {
                var heightItem = (item * maxH) / absMax;
                if (heightItem > int.MaxValue) heightItem = int.MaxValue;
                var convertido = Convert.ToInt32(heightItem);

                points[i] = new Point(initialGraphPosition + i, maxH - convertido + y);
                i++;
            }
            points[i] = new Point(initialGraphPosition + i, maxH + y);
            points[i + 1] = new Point(initialGraphPosition, maxH + y);

            Brush brush = new SolidBrush(colors.ElementAt(w));
            w++;
            formGraphics.FillPolygon(brush, points);
            brush.Dispose();


        }
    }

    public static int GetTaskbarWidth()
    {
        return Screen.PrimaryScreen.Bounds.Width - Screen.PrimaryScreen.WorkingArea.Width;
    }

    public static int GetTaskbarHeight()
    {
        return Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;
    }

    private void SystemWatcherControl_MouseEnter(object sender, EventArgs e)
    {
        mouseOver = true;

        Invalidate();
    }

    private void SystemWatcherControl_MouseLeave(object sender, EventArgs e)
    {
        mouseOver = false;
        Invalidate();
    }

    private void OpenSettings(int activeIndex = 0)
    {
        var openOptionForms = Application.OpenForms.OfType<OptionForm>();

        OptionForm optionForm;

        if (openOptionForms.Any())
        {
            optionForm = openOptionForms.First();
            optionForm.Focus();
        }
        else
        {
            try
            {
                optionForm = new OptionForm(Options, defaultTheme, Version, this);
                optionForm.Show();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error loading Options: {e.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        optionForm.OpenTab(activeIndex);
    }

    private void MenuItem_Settings_onClick(object? sender, EventArgs e)
    {
        OpenSettings();
    }

    private void MenuItem_About_onClick(object? sender, EventArgs e)
    {
        OpenSettings(2);

    }


}
