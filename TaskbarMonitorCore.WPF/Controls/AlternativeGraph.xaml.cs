using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using TaskbarMonitorCore.WPF.Data;

namespace TaskbarMonitorCore.WPF.Controls;
/// <summary>
/// Interaction logic for AlternativeGraph.xaml
/// </summary>
public partial class AlternativeGraph : UserControl
{
    public AlternativeGraph()
    {
        InitializeComponent();

        var data = new List<DataPoint>
        {
            new DataPoint(50),
            new DataPoint(0),
            new DataPoint(20),
            new DataPoint(125),
            new DataPoint(65),
            new DataPoint(15),
            new DataPoint(90),
            new DataPoint(45),
            new DataPoint(130),
            new DataPoint(75)
        };

        DataPoints.ItemsSource = data;
    }
}
