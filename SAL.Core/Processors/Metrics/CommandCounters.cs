using System.Collections.Generic;
using Prometheus;

namespace SAL.Core.Processors
{
    public class CommandCounters
    {
        public ICounter Positive;
        public ICounter Fatal;
        public ICounter Total;

        public IGauge PositiveL;
        public IGauge FatalL;
        public IGauge TotalL;
        public IGauge AverageElapsedL;

        public LinkedList<MetricElapsedData> GaugeData = new();
    }
}