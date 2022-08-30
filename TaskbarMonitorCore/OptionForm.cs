using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TaskbarMonitorCore;
using TaskbarMonitorCore.Counters;
using WindowsApiLibrary;

namespace TaskbarMonitor;

public partial class OptionForm : Form
{
    private readonly Options _originalOptions;
    private readonly GraphTheme _originalTheme;

    private readonly Options _options;
    private readonly GraphTheme _theme;

    private readonly Version _version;

    private CounterOptions _activeCounter;
    private bool _initializing = true;
    private readonly Dictionary<string, IList<CounterType>> _availableGraphTypes;

    private readonly SystemWatcherControl _originalControl;

    private Font _chosenTitleFont;
    private Font _chosenCurrentValueFont;

    public OptionForm(Options opt, GraphTheme theme, Version version, SystemWatcherControl originalControl)
    {
        _version = version;

        _theme = new GraphTheme();
        _originalTheme = theme;
        theme.CopyTo(_theme);

        _options = new Options();
        _originalOptions = opt;
        opt.CopyTo(_options);

        _originalControl = originalControl;

        _availableGraphTypes = new Dictionary<string, IList<CounterType>>
        {
            {
                "CPU",
                new List<CounterType>
                {
                    CounterType.SINGLE,
                    CounterType.STACKED
                }
            },
            {
                "MEM", 
                new List<CounterType>
                {
                    CounterType.SINGLE
                }
            },
            {
                "DISK",
                new List<CounterType>
                {
                    CounterType.SINGLE,
                    CounterType.STACKED,
                    CounterType.MIRRORED
                }
            },
            {
                "NET",
                new List<CounterType>
                {
                    CounterType.SINGLE,
                    CounterType.STACKED,
                    CounterType.MIRRORED
                }
            }
        };

        _activeCounter = _options.CounterOptions.First().Value;
        _chosenTitleFont = new Font(_theme.TitleFont, _theme.TitleSize, FontStyle.Bold);
        _chosenCurrentValueFont = new Font(_theme.CurrentValueFont, _theme.CurrentValueSize, FontStyle.Bold);

        InitializeComponent();

        Initialize();
    }

    private void Initialize()
    {
        _initializing = true;
        editHistorySize.Value = _options.HistorySize;
        editPollTime.Value = _options.PollTime;
        listThemeType.Text = _options.ThemeType.ToString();
        listCounters.DataSource = _options.CounterOptions.Keys.AsEnumerable().ToList();
        listShowTitle.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayType));
        listShowCurrentValue.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayType));
        listSummaryPosition.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayPosition));
        listTitlePosition.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayPosition));

        lblVersion.Text = "v" + _version.ToString(3);

        _activeCounter = _options.CounterOptions.First().Value;
        UpdateForm();
        UpdateReplicateSettingsMenu();
        btnColorBar.BackColor = _theme.BarColor;
        btnColorCurrentValue.BackColor = _theme.TextColor;
        btnColorCurrentValueShadow.BackColor = _theme.TextShadowColor;
        btnColorTitle.BackColor = _theme.TitleColor;
        btnColorTitleShadow.BackColor = _theme.TitleShadowColor;
        
        _chosenTitleFont = new Font(_theme.TitleFont, _theme.TitleSize, FontStyle.Bold);
        linkTitleFont.Text = _chosenTitleFont.Name + ", " + _chosenTitleFont.Size + "pt";

        _chosenCurrentValueFont = new Font(_theme.CurrentValueFont, _theme.CurrentValueSize, FontStyle.Bold);
        linkCurrentValueFont.Text = _chosenCurrentValueFont.Name + ", " + Math.Round(_chosenCurrentValueFont.Size) + "pt";

        btnColor1.BackColor = _theme.StackedColors[0];
        btnColor2.BackColor = _theme.StackedColors[1];

        UpdatePreview();

        _initializing = false;
    }

    private void UpdatePreview()
    {
        var previewOptions = new Options();
        _options.CopyTo(previewOptions);

        GraphTheme previewTheme = GraphTheme.DefaultDarkTheme();

        if (previewOptions.ThemeType == Options.ThemeList.LIGHT)
        {
            previewTheme = GraphTheme.DefaultLightTheme();
        }
        else if (previewOptions.ThemeType == Options.ThemeList.CUSTOM)
        {
            previewTheme = new GraphTheme();
            _theme.CopyTo(previewTheme);
        }
        else if (previewOptions.ThemeType == Options.ThemeList.AUTOMATIC)
        {
            Color taskBarColour = Win32Api.GetColourAt(Win32Api.GetTaskbarPosition().Location);

            var isTaskbarDark = taskBarColour.R + taskBarColour.G + taskBarColour.B > 382;
            previewTheme = isTaskbarDark ? GraphTheme.DefaultLightTheme() : GraphTheme.DefaultDarkTheme();
        }

        swcPreview.ApplyOptions(previewOptions, previewTheme);
    }

    private void EditHistorySize_ValueChanged(object sender, EventArgs e)
    {
        _options.HistorySize = Convert.ToInt32(editHistorySize.Value);
        UpdatePreview();
    }

    private void EditPollTime_ValueChanged(object sender, EventArgs e)
    {
        _options.PollTime = Convert.ToInt32(editPollTime.Value);
        UpdatePreview();
    }

    private void ListCounters_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_initializing) return;
        _activeCounter = _options.CounterOptions[listCounters.Text];
        UpdateReplicateSettingsMenu();
        UpdateForm();
        UpdatePreview();
    }

    private void UpdateReplicateSettingsMenu()
    {
        contextMenuStripReplicateSettings.Items.Clear();
        contextMenuStripReplicateSettings.Items.Add(new ToolStripMenuItem("All other graphs", null, ContextMenuStripReplicateSettings_OnClick));
        contextMenuStripReplicateSettings.Items.Add(new ToolStripSeparator());
        foreach (var item in _options.CounterOptions.Keys.AsEnumerable().ToList())
        {
            if (item != listCounters.Text)
            {
                contextMenuStripReplicateSettings.Items.Add(new ToolStripMenuItem(item, null, ContextMenuStripReplicateSettings_OnClick));
            }
        }
    }
    private void ContextMenuStripReplicateSettings_OnClick(object? sender, EventArgs e)
    {
        if(sender is not ToolStripMenuItem senderToolStripMenuItem)
        {
            return;
        }

        var destiny = new List<string>();
        if(senderToolStripMenuItem.Text == "All other graphs")
        {
            foreach (var item in _options.CounterOptions.Keys.AsEnumerable().ToList())
            {
                if (item != listCounters.Text)
                {
                    destiny.Add(item);
                }
            }
        }
        else
        {
            destiny.Add(senderToolStripMenuItem.Text);
        }

        foreach (var item in destiny)
        {
            _options.CounterOptions[item].ShowTitle = _activeCounter.ShowTitle;
            _options.CounterOptions[item].ShowCurrentValue = _activeCounter.ShowCurrentValue;
            _options.CounterOptions[item].CurrentValueAsSummary = _activeCounter.CurrentValueAsSummary;
            _options.CounterOptions[item].SummaryPosition = _activeCounter.SummaryPosition;
            _options.CounterOptions[item].ShowTitleShadowOnHover = _activeCounter.ShowTitleShadowOnHover;
            _options.CounterOptions[item].ShowCurrentValueShadowOnHover = _activeCounter.ShowCurrentValueShadowOnHover;
            _options.CounterOptions[item].TitlePosition = _activeCounter.TitlePosition;                

        }
    }

    private void UpdateForm()
    {
        _initializing = true;
        listGraphType.DataSource = _availableGraphTypes[listCounters.Text];
        _initializing = false;
        listGraphType.Text = _activeCounter.GraphType.ToString();
        checkEnabled.Checked = _activeCounter.Enabled;
        listShowTitle.Text = _activeCounter.ShowTitle.ToString();
        listShowCurrentValue.Text = _activeCounter.ShowCurrentValue.ToString();
        checkShowSummary.Checked = _activeCounter.CurrentValueAsSummary;            
        listSummaryPosition.Text = _activeCounter.SummaryPosition.ToString();
        checkInvertOrder.Checked = _activeCounter.InvertOrder;
        checkSeparateScales.Checked = _activeCounter.SeparateScales;
        checkTitleShadowHover.Checked = _activeCounter.ShowTitleShadowOnHover;
        checkValueShadowHover.Checked = _activeCounter.ShowCurrentValueShadowOnHover;
        listTitlePosition.Text = _activeCounter.TitlePosition.ToString();
        UpdateFormScales();
        UpdateFormOrder();
         
        UpdateFormShow();
    }

    

    private void UpdateFormScales()
    {
        checkSeparateScales.Enabled = listGraphType.Text == "MIRRORED";
    }

    private void UpdateFormOrder()
    {
        checkInvertOrder.Enabled = listGraphType.Text != "SINGLE";
    }

    private void UpdateFormShow()
    {
        checkTitleShadowHover.Enabled = listShowTitle.Text == "HOVER";
        checkValueShadowHover.Enabled = listShowCurrentValue.Text == "HOVER";
        listTitlePosition.Enabled = listShowTitle.Text != "HIDDEN";
        listSummaryPosition.Enabled = checkShowSummary.Checked && listShowCurrentValue.Text != "HIDDEN";
        checkShowSummary.Enabled = listShowCurrentValue.Text != "HIDDEN";
    }

    private void ListShowTitle_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.ShowTitle = Enum.Parse<CounterOptions.DisplayType>(listShowTitle.Text);
        UpdateFormShow();
        UpdatePreview();
    }

    private void ListShowCurrentValue_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_initializing) return;
        _activeCounter.ShowCurrentValue = Enum.Parse<CounterOptions.DisplayType>(listShowCurrentValue.Text);
        UpdateFormShow();
        UpdatePreview();
    }

    private void ListGraphType_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.GraphType = Enum.Parse<CounterType>(listGraphType.Text);
        UpdateFormScales();
        UpdateFormOrder();
        UpdatePreview();
    }

    private void BtnMenu_Click(object sender, EventArgs e)
    {
        var buttons = panelMenu.Controls.OfType<Button>().OrderBy(x => x.Top).ToArray();

        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];

            if (button == sender)
            {
                tabControl1.SelectedIndex = i;
                UpdateMenuColors(button);
            }
        }
    } 
    private void UpdateMenuColors(Button activeButton)
    {
        activeButton.BackColor = Color.SteelBlue;

        foreach (var menuButton in panelMenu.Controls.OfType<Button>())
        {
            if (menuButton != activeButton)
            {
                menuButton.BackColor = Color.FromArgb(255, 64, 64, 64);
            }
        }
    }

    public void OpenTab(int i)
    {
        tabControl1.SelectedIndex = i;
        UpdateMenuColors(panelMenu.Controls.OfType<Button>().ToList().OrderBy(x => x.Top).ElementAt(i));
    }

    private void CheckShowSummary_CheckedChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.CurrentValueAsSummary = checkShowSummary.Checked;
        UpdateFormShow();
        UpdatePreview();
    }

    private void CheckInvertOrder_CheckedChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.InvertOrder = checkInvertOrder.Checked;
        UpdatePreview();
    }

    private static bool ChooseColor(Button sender)
    {
        var MyDialog = new ColorDialog
        {
            // Keeps the user from selecting a custom color.
            AllowFullOpen = true,
            // Allows the user to get help. (The default is false.)
            ShowHelp = false,
            // Sets the initial color select to the current text color.
            Color = sender.BackColor
        };

        // Update the text box color if the user clicks OK 
        if (MyDialog.ShowDialog() == DialogResult.OK)
        {
            sender.BackColor = MyDialog.Color;
            return true;
        }

        return false;
    }

    private static bool ChooseFont(LinkLabel sender, ref Font font)
    {
        var MyDialog = new FontDialog
        {
            ShowColor = false,
            ShowEffects = false,
            ShowApply = false,
            ShowHelp = false,
            FontMustExist = true,
            MaxSize = 16,
            Font = font
        };

        // Update the text box color if the user clicks OK 
        if (MyDialog.ShowDialog() == DialogResult.OK)
        {
            font = MyDialog.Font;
            sender.Text = $"{font.Name}, {Math.Round(font.Size)}pt";
            return true;
        }

        return false;
    }

    private void BtnColorBar_Click(object sender, EventArgs e)
    {
        if (sender is not Button senderButton)
        {
            return;
        }

        if (ChooseColor(senderButton))
        {
            _theme.BarColor = senderButton.BackColor;
        }

        UpdatePreview();
    }

    private void BtnColorCurrentValue_Click(object sender, EventArgs e)
    {
        if(sender is not Button senderButton)
        {
            return;
        }

        if (ChooseColor(senderButton))
        {
            _theme.TextColor = senderButton.BackColor;
        }

        UpdatePreview();
    }

    private void BtnColorCurrentValueShadow_Click(object sender, EventArgs e)
    {
        if (sender is not Button senderButton)
        {
            return;
        }

        if (ChooseColor(senderButton))
        {
            _theme.TextShadowColor = senderButton.BackColor;
        }

        UpdatePreview();
    }

    private void BtnColorTitle_Click(object sender, EventArgs e)
    {
        if (sender is not Button senderButton)
        {
            return;
        }

        if (ChooseColor(senderButton))
        {
            _theme.TitleColor = senderButton.BackColor;
        }

        UpdatePreview();
    }

    private void BtnColorTitleShadow_Click(object sender, EventArgs e)
    {
        if (sender is not Button senderButton)
        {
            return;
        }

        if (ChooseColor(senderButton))
        {
            _theme.TitleShadowColor = senderButton.BackColor;
        }

        UpdatePreview();
    }

    private void BtnColor1_Click(object sender, EventArgs e)
    {
        if (sender is not Button senderButton)
        {
            return;
        }

        if (ChooseColor(senderButton))
        {
            _theme.StackedColors[0] = senderButton.BackColor;
        }

        UpdatePreview();
    }

    private void BtnColor2_Click(object sender, EventArgs e)
    {
        if (sender is not Button senderButton)
        {
            return;
        }

        if (ChooseColor(senderButton))
        {
            _theme.StackedColors[1] = senderButton.BackColor;
        }

        UpdatePreview();
    }

    private void CheckSeparateScales_CheckedChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.SeparateScales = checkSeparateScales.Checked;
        UpdatePreview();
    }

    private void CheckTitleShadowHover_CheckedChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.ShowTitleShadowOnHover = checkTitleShadowHover.Checked;
        UpdatePreview();
    }

    private void ListTitlePosition_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.TitlePosition = Enum.Parse<CounterOptions.DisplayPosition>(listTitlePosition.Text);
        if (_activeCounter.SummaryPosition == _activeCounter.TitlePosition)
        {
            var vals = Enum
                .GetValues<CounterOptions.DisplayPosition>()
                .Where(x => x != _activeCounter.TitlePosition);

            listSummaryPosition.Text = vals.First().ToString();
        }

        UpdatePreview();
    }

    private void CheckValueShadowHover_CheckedChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.ShowCurrentValueShadowOnHover = checkValueShadowHover.Checked;
        UpdatePreview();
    }

    private void ListSummaryPosition_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.SummaryPosition = Enum.Parse<CounterOptions.DisplayPosition>(listSummaryPosition.Text);
        if (_activeCounter.SummaryPosition == _activeCounter.TitlePosition)
        {
            var vals = Enum
                .GetValues<CounterOptions.DisplayPosition>()
                .Where(x => x != _activeCounter.SummaryPosition)
                .ToList();

            listTitlePosition.Text = vals.First().ToString();
        }

        UpdatePreview();
    }

    private void Button3_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void ButtonResetDefaults_Click(object sender, EventArgs e)
    {
        Options.DefaultOptions().CopyTo(_options);
        GraphTheme.DefaultDarkTheme().CopyTo(_theme);
        Initialize();
    }

    private void CheckEnabled_CheckedChanged(object sender, EventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _activeCounter.Enabled = checkEnabled.Checked;
        UpdatePreview();
    }

    private void ButtonApply_Click(object sender, EventArgs e)
    {
        _options.CopyTo(_originalOptions);
        _options.SaveToDisk();

        _theme.CopyTo(_originalTheme);
        _theme.SaveToDisk();
        Close();

        _originalControl.ApplyOptions(_originalOptions);
    }

    private void LinkTitleFont_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        if (sender is not LinkLabel senderLinkLabel)
        {
            return;
        }

        if (ChooseFont(senderLinkLabel, ref _chosenTitleFont))
        {                
            _theme.TitleFont = _chosenTitleFont.Name;
            _theme.TitleFontStyle = _chosenTitleFont.Style;
            _theme.TitleSize = _chosenTitleFont.Size;
        }

        UpdatePreview();
    }

    private void LinkCurrentValueFont_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        if (sender is not LinkLabel senderLinkLabel)
        {
            return;
        }

        if (ChooseFont(senderLinkLabel, ref _chosenCurrentValueFont))
        {
            _theme.CurrentValueFont = _chosenCurrentValueFont.Name;
            _theme.CurrentValueFontStyle = _chosenCurrentValueFont.Style;
            _theme.CurrentValueSize = _chosenCurrentValueFont.Size;
        }

        UpdatePreview();
    }

    private void ListThemeType_SelectedIndexChanged(object sender, EventArgs e)
    {
        _options.ThemeType = Enum.Parse<Options.ThemeList>(listThemeType.Text);
        UpdatePreview();
        UpdateThemeOptions();
    }

    private void UpdateThemeOptions()
    {
        btnColorBar.Enabled = _options.ThemeType == Options.ThemeList.CUSTOM;
        btnColor1.Enabled = _options.ThemeType == Options.ThemeList.CUSTOM;
        btnColor2.Enabled = _options.ThemeType == Options.ThemeList.CUSTOM;
        btnColorTitle.Enabled = _options.ThemeType == Options.ThemeList.CUSTOM;
        btnColorTitleShadow.Enabled = _options.ThemeType == Options.ThemeList.CUSTOM;
        btnColorCurrentValue.Enabled = _options.ThemeType == Options.ThemeList.CUSTOM;
        btnColorCurrentValueShadow.Enabled = _options.ThemeType == Options.ThemeList.CUSTOM;
        linkTitleFont.Enabled = _options.ThemeType == Options.ThemeList.CUSTOM;
        linkCurrentValueFont.Enabled = _options.ThemeType == Options.ThemeList.CUSTOM;
    }
}
