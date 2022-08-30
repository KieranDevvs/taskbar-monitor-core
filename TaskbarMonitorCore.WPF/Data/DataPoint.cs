namespace TaskbarMonitorCore.WPF.Data;

public class DataPoint
{
    public DataPoint(double value)
    {
        Value = value;
    }

    public double Value { get; set; }
}
