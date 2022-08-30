using System.Windows;
using System.Windows.Controls;

namespace TaskbarMonitorCore.WPF.Controls;

/// <summary>
/// Interaction logic for NumericCounter.xaml
/// </summary>
public partial class NumericCounter : UserControl
{
    public string Title
    {
        get => (string)GetValue(TitleDepdendency);
        set => SetValue(TitleDepdendency, value); 
    }

    public string Value
    {
        get => (string)GetValue(ValueDepdendency);
        set => SetValue(ValueDepdendency, value);
    }

    public static readonly DependencyProperty TitleDepdendency = DependencyProperty.Register(nameof(Title), typeof(string), typeof(NumericCounter), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty ValueDepdendency = DependencyProperty.Register(nameof(Value), typeof(string), typeof(NumericCounter), new PropertyMetadata(default(string)));

    public NumericCounter()
    {
        InitializeComponent();
    }
}
