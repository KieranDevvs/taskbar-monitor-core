using CSDeskBand.ContextMenu;
using CSDeskBand;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using WindowsApiLibrary;

namespace TaskbarMonitorCore.WPF;

[ComVisible(true)]
[Guid("AA8211E7-6F66-4E76-88C3-19A6FF90CB00")]
[CSDeskBandRegistration(Name = "Sample wpf")]
public class Deskband : CSDeskBandWpf
{
    private DeskBandUI _root;

    public Deskband()
    {
        _root = new DeskBandUI();

        _root.SizeChanged += (sender, args) =>
        {
            var width = (int)args.NewSize.Width;
            var height = (int)args.NewSize.Height;

            //The taskbar has to be resized to force a re-render so that the deskband object appears.
            //TODO: look into an API call to force an update of the taskbar without resizing it.
            var taskbarDimesions = Win32Api.GetTaskbarPosition();
            Options.MinHorizontalSize = new DeskBandSize(width, taskbarDimesions.Height + 1);

            Options.MinHorizontalSize = new DeskBandSize(width, 40);
        };

        Options.ContextMenuItems = ContextMenuItems;
    }

    protected override UIElement UIElement => _root;

    private static List<DeskBandMenuItem> ContextMenuItems
    {
        get
        {
            var action = new DeskBandMenuAction("Action");
            return new List<DeskBandMenuItem>() { action };
        }
    }
}
