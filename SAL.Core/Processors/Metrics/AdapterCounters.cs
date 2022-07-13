using System.Collections.Generic;
using Prometheus;

namespace SAL.Core.Processors
{
    public class AdapterCounters
    {
        public ICounter CommandPositive;
        public ICounter CommandFatal;
        public ICounter CommandTotal;

        public IGauge CommandTotalL;
        public IGauge CommandFatalL;
        public IGauge CommandPositiveL;
        public IGauge CommandAverageElapsedL;

        public LinkedList<MetricElapsedData> CommandGaugeData = new();


        public ICounter CommandResultPositive;
        public ICounter CommandResultFatal;
        public ICounter CommandResultTotal;

        public IGauge CommandResultPositiveL;
        public IGauge CommandResultFatalL;
        public IGauge CommandResultTotalL;
        public IGauge CommandResultAverageElapsedL;

        public LinkedList<MetricElapsedData> CommandResultGaugeData = new();
        
        
        public ICounter SyncCommandResultPositive;
        public ICounter SyncCommandResultFatal;
        public ICounter SyncCommandResultTotal;

        public IGauge SyncCommandResultPositiveL;
        public IGauge SyncCommandResultFatalL;
        public IGauge SyncCommandResultTotalL;
        public IGauge SyncCommandResultAverageElapsedL;

        public LinkedList<MetricElapsedData> SyncCommandResultGaugeData = new();
        
        
        public ICounter SharedCommandResultPositive;
        public ICounter SharedCommandResultFatal;
        public ICounter SharedCommandResultTotal;

        public IGauge SharedCommandResultPositiveL;
        public IGauge SharedCommandResultFatalL;
        public IGauge SharedCommandResultTotalL;
        public IGauge SharedCommandResultAverageElapsedL;

        public LinkedList<MetricElapsedData> SharedCommandResultGaugeData = new();
        


        public ICounter EventPositive;
        public ICounter EventFatal;
        public ICounter EventTotal;

        public IGauge EventPositiveL;
        public IGauge EventFatalL;
        public IGauge EventTotalL;
        public IGauge EventAverageElapsedL;

        public LinkedList<MetricElapsedData> EventGaugeData = new();
    }
}