using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace TaskbarMonitorCore.Controls;

public class MenuButton : Button
{
    [DefaultValue(null)]
    public ContextMenuStrip? Menu { get; set; }

    [DefaultValue(false)]
    public bool ShowMenuUnderCursor { get; set; }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);

        if (Menu is not null && mevent.Button == MouseButtons.Left)
        {
            Point menuLocation;

            if (ShowMenuUnderCursor)
            {
                menuLocation = mevent.Location;
            }
            else
            {
                menuLocation = new Point(0, Height);
            }

            Menu.Show(this, menuLocation);
        }
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        base.OnPaint(pevent);

        if (Menu is not null)
        {
            var arrowX = ClientRectangle.Width - 14;
            var arrowY = ClientRectangle.Height / 2 - 1;

            var brush = Enabled ? SystemBrushes.ControlText : SystemBrushes.ControlDark;
            var arrows = new Point[] { new Point(arrowX, arrowY), new Point(arrowX + 7, arrowY), new Point(arrowX + 3, arrowY + 4) };
            pevent.Graphics.FillPolygon(brush, arrows);
        }
    }
}
