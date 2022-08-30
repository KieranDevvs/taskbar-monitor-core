using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TaskbarMonitorCore.WPF.Controls;
/// <summary>
/// Interaction logic for Graph.xaml
/// </summary>
public partial class Graph : UserControl
{
    private Timer _timer;

    public Graph()
    {
        InitializeComponent();

        for(var i = 0; i < 40; i++)
        {
            var y = new Random().Next(0, 40);
            var line = new Rectangle()
            {
                Stroke = new SolidColorBrush(Color.FromArgb(255, 27, 86, 140)),
                Height = y,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            Grid.SetColumn(line, i);

            //Deskband is too small to anti-alias as the pixels combine and blur.
            RenderOptions.SetEdgeMode(line, EdgeMode.Aliased);

            GraphCanvas.Children.Add(line);
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        _timer = new Timer(UpdateGraph, null, 0, 1000);
    }


    public void UpdateGraph(object? sender)
    {
        // Is Click_RefreshStatus being called from the main UI thread or
        // from the timer's thread?
        if (!GraphCanvas.Dispatcher.CheckAccess())
        {
            // Called from timer's thread. Need to re-invoke on the UI main thread
            GraphCanvas.Dispatcher.Invoke(DispatcherPriority.Normal, UpdateGraph, sender, Array.Empty<object>());
            return;
        }
        else
        {
            GraphCanvas.Children.RemoveAt(0);

            for (var i = 0; i < 39; i++)
            {
                var child = GraphCanvas.Children[i];
                Grid.SetColumn(child, i);
            }

            var line = new Rectangle()
            {
                Stroke = new SolidColorBrush(Color.FromArgb(255, 27, 86, 140)),
                Height = new Random().Next(0, 40),
                VerticalAlignment = VerticalAlignment.Bottom
            };

            Grid.SetColumn(line, 39);

            GraphCanvas.Children.Add(line);
        }
    }

}
