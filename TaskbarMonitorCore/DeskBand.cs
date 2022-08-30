using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CSDeskBand;
using TaskbarMonitor;

namespace TaskbarMonitorCore;

[ComVisible(true)]
[Guid("13790826-15fa-46d0-9814-c2a5c6c11f32")]
[CSDeskBandRegistration(Name = "taskbar-monitor")]
public class Deskband : CSDeskBandWin
{
    protected override Control Control { get; }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    public Deskband()
    {
        try
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }

            Application.EnableVisualStyles();
            // Application.SetCompatibleTextRenderingDefault(false);

            var opt = TaskbarMonitorCore.Options.ReadFromDisk();


            var ctl = new SystemWatcherControl(opt);
            Options.MinHorizontalSize = new Size((ctl.Options.HistorySize + 10) * ctl.CountersCount, 30);
            ctl.OnChangeSize += Ctl_OnChangeSize;
            Control = ctl;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error intializing Deskband: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private void Ctl_OnChangeSize(Size size)
    {
        Options.MinHorizontalSize = new Size(size.Width, 30);
    }


}
