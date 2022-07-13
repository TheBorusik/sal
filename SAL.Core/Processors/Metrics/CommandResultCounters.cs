using System.Collections.Generic;
using Prometheus;

namespace SAL.Core.Processors
{
    public class CommandResultCounters
    {
        public ICounter Positive;
        public ICounter Fatal;
        public ICounter Total;


        public IGauge TotalL;
        public IGauge FatalL;
        public IGauge PositiveL;
        public IGauge AverageElapsedL;

        public LinkedList<MetricElapsedData> GaugeData = new();
    }
}