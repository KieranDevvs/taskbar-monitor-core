using System.Collections.Generic;

namespace TaskbarMonitorCore.WPF.Data;

public class GraphData
{
    public string Fill { get; }

    public List<DataPoint> Data { get; }

    public GraphData(string fill, List<DataPoint> data)
    {
        Fill = fill;
        Data = data;
    }
}
