using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Logging;
using Prometheus;
using SAL.API;
using SAL.Infrastructure;


namespace SAL.Core.Processors
{
    public class MetricsProcessor : IProcessor, IMetricProvider
    {
        private readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
        
        
        private readonly ILifetimeScope lifetimeScope;
        private readonly ILogger logger;

        private readonly Dictionary<string, CommandCounters> commandsCounter = new();
        private readonly Dictionary<string, EventCounters> eventsCounter = new();
        private readonly AdapterCounters adapterCounters = new();

        private readonly Channel<IMetricAction> metricActionChannel;
        private readonly CancellationTokenSource metricActionChannelCancellationTokenSource = new();
        private Task metricActionLoop;





        private Timer timer = null;


        public MetricsProcessor(ILoggerProvider loggerProvider, ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
            this.logger = loggerProvider.CreateLogger("MetricsProcessor");

            metricActionChannel = Channel.CreateUnbounded<IMetricAction>();
        }


        public void Start()
        {
            var metricDir = Path.Combine(AdapterConfiguration.RootPath, "metric");
            if (!Directory.Exists(metricDir))
                Directory.CreateDirectory(metricDir);
            var metricConfigFileName = Path.Combine(metricDir, "config.json");


            var dnsName = System.Net.Dns.GetHostName();
            logger.Info($"DNSName : {dnsName}");

            var ipAddr = System.Net.Dns.GetHostAddresses(dnsName)
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString()).ToArray();

            var endPoint = $"http://{ipAddr.First()}/metrics";

            logger.Info($"Metric endPoint: {endPoint}");

            File.WriteAllText(metricConfigFileName, new { endpoint = endPoint }.ToIndentedJson());
            
            var counterConfig = new CounterConfiguration
            {
                StaticLabels = new Dictionary<string, string>(),
                SuppressInitialValue = false
            };
            counterConfig.StaticLabels.Add("adapterName",AdapterConfiguration.AdapterName );

            var gaugeConfiguration = new GaugeConfiguration
            {
                StaticLabels = new Dictionary<string, string>(),
                SuppressInitialValue = false
            };
            gaugeConfiguration.StaticLabels.Add("adapterName",AdapterConfiguration.AdapterName );
            
            

            //commands
            adapterCounters.CommandPositive = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_CommandPositive","", counterConfig);
            adapterCounters.CommandFatal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_CommandFatal", $"", counterConfig);
            adapterCounters.CommandTotal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_CommandTotal", $"", counterConfig);

            
            adapterCounters.CommandTotalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_CommandTotalL", $"", gaugeConfiguration);
            adapterCounters.CommandFatalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_CommandFatalL", $"", gaugeConfiguration);
            adapterCounters.CommandPositiveL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_CommandPositiveL", $"", gaugeConfiguration);
            adapterCounters.CommandAverageElapsedL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_CommandAverageElapsedL", $"",gaugeConfiguration);

            //comandResults
            adapterCounters.CommandResultPositive = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_CommandResultPositive", $"",counterConfig );
            adapterCounters.CommandResultFatal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_CommandResultFatal", $"", counterConfig);
            adapterCounters.CommandResultTotal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_CommandResultTotal", $"", counterConfig);
            
            adapterCounters.CommandResultPositiveL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_CommandResultPositiveL", $"", gaugeConfiguration);
            adapterCounters.CommandResultFatalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_CommandResultFatalL", $"",gaugeConfiguration);
            adapterCounters.CommandResultTotalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_CommandResultTotalL", $"", gaugeConfiguration);
            adapterCounters.CommandResultAverageElapsedL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_CommandResultAverageElapsedL", $"",gaugeConfiguration);

            
            adapterCounters.SyncCommandResultPositive = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_SyncCommandResultPositive", $"",counterConfig );
            adapterCounters.SyncCommandResultFatal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_SyncCommandResultFatal", $"", counterConfig);
            adapterCounters.SyncCommandResultTotal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_SyncCommandResultTotal", $"", counterConfig);
            
            adapterCounters.SyncCommandResultPositiveL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_SyncCommandResultPositiveL", $"", gaugeConfiguration);
            adapterCounters.SyncCommandResultFatalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_SyncCommandResultFatalL", $"",gaugeConfiguration);
            adapterCounters.SyncCommandResultTotalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_SyncCommandResultTotalL", $"", gaugeConfiguration);
            adapterCounters.SyncCommandResultAverageElapsedL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_SyncCommandResultAverageElapsedL", $"",gaugeConfiguration);

            
            adapterCounters.SharedCommandResultPositive = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_SharedCommandResultPositive", $"",counterConfig );
            adapterCounters.SharedCommandResultFatal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_SharedCommandResultFatal", $"", counterConfig);
            adapterCounters.SharedCommandResultTotal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_SharedCommandResultTotal", $"", counterConfig);
            
            adapterCounters.SharedCommandResultPositiveL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_SharedCommandResultPositiveL", $"", gaugeConfiguration);
            adapterCounters.SharedCommandResultFatalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_SharedCommandResultFatalL", $"",gaugeConfiguration);
            adapterCounters.SharedCommandResultTotalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_SharedCommandResultTotalL", $"", gaugeConfiguration);
            adapterCounters.SharedCommandResultAverageElapsedL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_SharedCommandResultAverageElapsedL", $"",gaugeConfiguration);

            
            
            //events
            adapterCounters.EventPositive = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_EventPositive", $"", counterConfig );
            adapterCounters.EventFatal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_EventFatal", $"", counterConfig);
            adapterCounters.EventTotal = Metrics.CreateCounter($"{AdapterConfiguration.AdapterType}_EventTotal", $"", counterConfig);
            
            adapterCounters.EventPositiveL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_EventPositiveL", $"", gaugeConfiguration);
            adapterCounters.EventFatalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_EventFatalL", $"", gaugeConfiguration);
            adapterCounters.EventTotalL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_EventTotalL", $"", gaugeConfiguration);
            adapterCounters.EventAverageElapsedL = Metrics.CreateGauge($"{AdapterConfiguration.AdapterType}_EventAverageElapsedL", $"",gaugeConfiguration);
            
            
            timer = new Timer(TimerRoutine, null, Interval, Interval);
            metricActionLoop = Task.Run(async () => await MetricActionLoop());
        }


        public void Online()
        {
        }

        public void Offline()
        {
        }

        public void Stop()
        {
            metricActionChannelCancellationTokenSource.Cancel();
        }


        public void IncCommand(string commandName, TimeSpan elapsed, bool isFailure)
        {
            metricActionChannel.Writer.WriteAsync(new CommandMetric
            {
                CommandName = commandName,
                Data = new MetricElapsedData
                {
                    Dt = DateTime.UtcNow,
                    Elapsed = elapsed,
                    IsFailure = isFailure
                }
            });
        }
        public void IncEvent(string eventName, TimeSpan elapsed, bool isFailure)
        {
            metricActionChannel.Writer.WriteAsync(new EventMetric
            {
                EventName = eventName,
                Data = new MetricElapsedData
                {
                    Dt = DateTime.UtcNow,
                    Elapsed = elapsed,
                    IsFailure = isFailure
                }
            });
        }

        public void IncCommandResult(TimeSpan elapsed, bool isFailure)
        {
            metricActionChannel.Writer.WriteAsync(new CommandResultMetric
            {
                Type = "Handler",
                Data = new MetricElapsedData
                {
                    Dt = DateTime.UtcNow,
                    Elapsed = elapsed,
                    IsFailure = isFailure
                }
            });
        }
        public void IncSyncCommandResult(TimeSpan elapsed, bool isFailure)
        {
            metricActionChannel.Writer.WriteAsync(new CommandResultMetric
            {
                Type = "Sync",
                Data = new MetricElapsedData
                {
                    Dt = DateTime.UtcNow,
                    Elapsed = elapsed,
                    IsFailure = isFailure
                }
            });
        }
        public void IncSharedCommandResult(TimeSpan elapsed, bool isFailure)
        {
            metricActionChannel.Writer.WriteAsync(new CommandResultMetric
            {
                Type = "Shared",
                Data = new MetricElapsedData
                {
                    Dt = DateTime.UtcNow,
                    Elapsed = elapsed,
                    IsFailure = isFailure
                }
            });
        }
        public void RegisterCommand(string commandName)
        {
            if(commandsCounter.ContainsKey(commandName))
                return;
            
            var counterConfig = new CounterConfiguration
            {
                StaticLabels = new Dictionary<string, string>(),
                SuppressInitialValue = false
            };
            counterConfig.StaticLabels.Add("adapterType",AdapterConfiguration.AdapterType );
            counterConfig.StaticLabels.Add("adapterName",AdapterConfiguration.AdapterName );


            var gaugeConfiguration = new GaugeConfiguration
            {
                StaticLabels = new Dictionary<string, string>(),
                SuppressInitialValue = false
            };
            gaugeConfiguration.StaticLabels.Add("adapterType",AdapterConfiguration.AdapterType );
            gaugeConfiguration.StaticLabels.Add("adapterName",AdapterConfiguration.AdapterName );


            var metricName = commandName.Replace('.', '_');
            
            commandsCounter.Add(commandName, new CommandCounters
            {
                Total = Metrics.CreateCounter($"Command_{metricName}_Total","", counterConfig),
                Positive = Metrics.CreateCounter($"Command_{metricName}_Positive","", counterConfig),
                Fatal = Metrics.CreateCounter($"Command_{metricName}_Fatal","", counterConfig),
                TotalL = Metrics.CreateGauge($"Command_{metricName}_TotalL", $"", gaugeConfiguration),
                PositiveL = Metrics.CreateGauge($"Command_{metricName}_PositiveL", $"", gaugeConfiguration),
                FatalL = Metrics.CreateGauge($"Command_{metricName}_FatalL", $"", gaugeConfiguration),
                AverageElapsedL = Metrics.CreateGauge($"Command_{metricName}_AverageElapsedL", $"", gaugeConfiguration),
            });
        }
        public void RegisterEvent(string eventName)
        {
            if(string.IsNullOrEmpty(eventName))
                return;
            
            if(eventsCounter.ContainsKey(eventName))
                return;
            
            var counterConfig = new CounterConfiguration
            {
                StaticLabels = new Dictionary<string, string>(),
                SuppressInitialValue = false
            };
            counterConfig.StaticLabels.Add("adapterType",AdapterConfiguration.AdapterType );
            counterConfig.StaticLabels.Add("adapterName",AdapterConfiguration.AdapterName );


            var gaugeConfiguration = new GaugeConfiguration
            {
                StaticLabels = new Dictionary<string, string>(),
                SuppressInitialValue = false
            };
            gaugeConfiguration.StaticLabels.Add("adapterType",AdapterConfiguration.AdapterType );
            gaugeConfiguration.StaticLabels.Add("adapterName",AdapterConfiguration.AdapterName );
            

            var metricName = eventName.Replace('.', '_');
            
            eventsCounter.Add(eventName, new EventCounters
            {
                Total = Metrics.CreateCounter($"Event_{metricName}_Total","", counterConfig),
                Positive = Metrics.CreateCounter($"Event_{metricName}_Positive","", counterConfig),
                Fatal = Metrics.CreateCounter($"Event_{metricName}_Fatal","", counterConfig),
                TotalL = Metrics.CreateGauge($"Event_{metricName}_TotalL", $"", gaugeConfiguration),
                PositiveL = Metrics.CreateGauge($"Event_{metricName}_PositiveL", $"", gaugeConfiguration),
                FatalL = Metrics.CreateGauge($"Event_{metricName}_FatalL", $"", gaugeConfiguration),
                AverageElapsedL = Metrics.CreateGauge($"Event_{metricName}_AverageElapsedL", $"", gaugeConfiguration),
            });
        }
        private void TimerRoutine(object state)
        {
            metricActionChannel.Writer.WriteAsync(new RecalculateMetrics
            {
                Dt = DateTime.Now
            });
        }
        private async Task MetricActionLoop()
        {
            var reader = metricActionChannel.Reader;
            while (await reader.WaitToReadAsync(metricActionChannelCancellationTokenSource.Token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var processItem))
                {
                    try
                    {
                        switch (processItem)
                        {
                            case CommandMetric cm:
                                Inc(cm);
                                break;
                            case CommandResultMetric crm:
                                Inc(crm);
                                break;
                            case EventMetric em:
                                Inc(em);
                                break;
                            case RecalculateMetrics rm:
                                RecalculateMetrics(rm);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error("При обработке метрик оштбка", e);
                    }

                }
            }
        }


        public void Inc(CommandMetric commandMetric)
        {
            if (commandsCounter.TryGetValue(commandMetric.CommandName, out var commandCounter))
            {
                commandCounter.Total.Inc();
                if (commandMetric.Data.IsFailure)
                    commandCounter.Fatal.Inc();
                else
                    commandCounter.Positive.Inc();

                commandCounter.GaugeData.AddLast(commandMetric.Data);
            }

            adapterCounters.CommandTotal.Inc();
            if (commandMetric.Data.IsFailure)
                adapterCounters.CommandFatal.Inc();
            else
                adapterCounters.CommandPositive.Inc();

            adapterCounters.CommandGaugeData.AddLast(commandMetric.Data);
        }

        public void Inc(CommandResultMetric commandResultMetric)
        {

            if (commandResultMetric.Type == "Handler")
            {
                adapterCounters.CommandResultTotal.Inc();
                if (commandResultMetric.Data.IsFailure)
                    adapterCounters.CommandResultFatal.Inc();
                else
                    adapterCounters.CommandResultPositive.Inc();

                adapterCounters.CommandResultGaugeData.AddLast(commandResultMetric.Data);
            }
            
            if (commandResultMetric.Type == "Sync")
            {
                adapterCounters.SyncCommandResultTotal.Inc();
                if (commandResultMetric.Data.IsFailure)
                    adapterCounters.SyncCommandResultFatal.Inc();
                else
                    adapterCounters.SyncCommandResultPositive.Inc();

                adapterCounters.SyncCommandResultGaugeData.AddLast(commandResultMetric.Data);
            }

            if (commandResultMetric.Type == "Shared")
            {
                adapterCounters.SharedCommandResultTotal.Inc();
                if (commandResultMetric.Data.IsFailure)
                    adapterCounters.SharedCommandResultFatal.Inc();
                else
                    adapterCounters.SharedCommandResultPositive.Inc();

                adapterCounters.SharedCommandResultGaugeData.AddLast(commandResultMetric.Data);
            }
            
            
        }

        public void Inc(EventMetric eventMetric)
        {
            if (eventsCounter.TryGetValue(eventMetric.EventName, out var eventCounter))
            {
                eventCounter.Total.Inc();
                if (eventMetric.Data.IsFailure)
                    eventCounter.Fatal.Inc();
                else
                    eventCounter.Positive.Inc();

                eventCounter.GaugeData.AddLast(eventMetric.Data);
            }

            adapterCounters.EventTotal.Inc();
            if (eventMetric.Data.IsFailure)
                adapterCounters.EventFatal.Inc();
            else
                adapterCounters.EventPositive.Inc();

            adapterCounters.EventGaugeData.AddLast(eventMetric.Data);
        }
        
        private void RecalculateMetrics(RecalculateMetrics recalculateMetrics)
        {
            (double, double, double, double) GetMetricsFromDataAndClear(LinkedList<MetricElapsedData> data)
            {
                var total = data.Count;
                var positive = data.Count(x => !x.IsFailure);
                var fatal = data.Count(x => x.IsFailure);
                
                
                var average = 0.0;
                if (data.Any())
                    average = data.Select(x => x.Elapsed.TotalSeconds).Average();
                
                data.Clear();
                return (total, positive, fatal, average);
            }

            var (total, positive, fatal, avEl) = GetMetricsFromDataAndClear(adapterCounters.CommandGaugeData); 

            adapterCounters.CommandTotalL.Set(total);
            adapterCounters.CommandFatalL.Set(fatal);
            adapterCounters.CommandPositiveL.Set(positive);
            adapterCounters.CommandAverageElapsedL.Set(avEl);
        
            (total, positive, fatal, avEl) = GetMetricsFromDataAndClear(adapterCounters.CommandResultGaugeData);
            adapterCounters.CommandResultTotalL.Set(total);
            adapterCounters.CommandResultPositiveL.Set(positive);
            adapterCounters.CommandResultFatalL.Set(fatal);
            adapterCounters.CommandResultAverageElapsedL.Set(avEl);
            
            (total, positive, fatal, avEl) = GetMetricsFromDataAndClear(adapterCounters.SyncCommandResultGaugeData);
            adapterCounters.SyncCommandResultTotalL.Set(total);
            adapterCounters.SyncCommandResultPositiveL.Set(positive);
            adapterCounters.SyncCommandResultFatalL.Set(fatal);
            adapterCounters.SyncCommandResultAverageElapsedL.Set(avEl);
            
            (total, positive, fatal, avEl) = GetMetricsFromDataAndClear(adapterCounters.SharedCommandResultGaugeData);
            adapterCounters.SharedCommandResultTotalL.Set(total);
            adapterCounters.SharedCommandResultPositiveL.Set(positive);
            adapterCounters.SharedCommandResultFatalL.Set(fatal);
            adapterCounters.SharedCommandResultAverageElapsedL.Set(avEl);
            
            
            (total, positive, fatal, avEl) = GetMetricsFromDataAndClear(adapterCounters.EventGaugeData);
            adapterCounters.EventTotalL.Set(total);
            adapterCounters.EventPositiveL.Set(positive);
            adapterCounters.EventFatalL.Set(fatal);
            adapterCounters.EventAverageElapsedL.Set(avEl);
            
            
            foreach(var commandCounter in commandsCounter.Values)
            {
                (total, positive, fatal, avEl) = GetMetricsFromDataAndClear(commandCounter.GaugeData);
                commandCounter.TotalL.Set(total);
                commandCounter.PositiveL.Set(positive);
                commandCounter.FatalL.Set(fatal);
                commandCounter.AverageElapsedL.Set(avEl);
            }


            foreach(var eventCounter in eventsCounter.Values)
            {
                (total, positive, fatal, avEl) = GetMetricsFromDataAndClear(eventCounter.GaugeData);
                eventCounter.TotalL.Set(total);
                eventCounter.PositiveL.Set(positive);
                eventCounter.FatalL.Set(fatal);
                eventCounter.AverageElapsedL.Set(avEl);
            }


        }
    }
}