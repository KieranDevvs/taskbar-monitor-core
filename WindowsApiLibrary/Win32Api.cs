using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WindowsApiLibrary;

internal struct APPBARDATA
{
    public int cbSize;
    public IntPtr hWnd;
    public int uCallbackMessage;
    public int uEdge;
    public RECT rc;
    public IntPtr lParam;
}

enum ABEdge : int
{
    ABE_LEFT = 0,
    ABE_TOP,
    ABE_RIGHT,
    ABE_BOTTOM
}

internal struct RECT
{
    public int left, top, right, bottom;
}

enum ABMsg : int
{
    ABM_NEW = 0,
    ABM_REMOVE = 1,
    ABM_QUERYPOS = 2,
    ABM_SETPOS = 3,
    ABM_GETSTATE = 4,
    ABM_GETTASKBARPOS = 5,
    ABM_ACTIVATE = 6,
    ABM_GETAUTOHIDEBAR = 7,
    ABM_SETAUTOHIDEBAR = 8,
    ABM_WINDOWPOSCHANGED = 9,
    ABM_SETSTATE = 10
}

public class Win32Api
{
    [DllImport("shell32.dll")]
    private static extern IntPtr SHAppBarMessage(int msg, ref APPBARDATA data);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    private static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

    public static Rectangle GetTaskbarPosition()
    {
        var data = new APPBARDATA();
        data.cbSize = Marshal.SizeOf(data);

        var retval = SHAppBarMessage((int)ABMsg.ABM_GETTASKBARPOS, ref data);
        if (retval == IntPtr.Zero)
        {
            throw new Win32Exception("Please re-install Windows");
        }

        return new Rectangle(data.rc.left, data.rc.top, data.rc.right - data.rc.left, data.rc.bottom - data.rc.top);
    }

    [SupportedOSPlatform("Windows")]
    public static Color GetColourAt(Point location)
    {
        using (var screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
        using (var gdest = Graphics.FromImage(screenPixel))
        {
            using (var gsrc = Graphics.FromHwnd(IntPtr.Zero))
            {
                var hSrcDC = gsrc.GetHdc();
                var hDC = gdest.GetHdc();
                var retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                gdest.ReleaseHdc();
                gsrc.ReleaseHdc();
            }

            return screenPixel.GetPixel(0, 0);
        }
    }
}
