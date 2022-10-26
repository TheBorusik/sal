using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.Core.Configuration.Logging;

namespace SAL.Core.Processors
{
    public class SalHandlerLogger : ISalLogger
    {
        private ILoggerProvider loggerProvider;
        private IConfigWatcher configWatcher;

        private const string LoggingSettingsSectionName = "SystemLogging";
        private const string EventsSectionName = "Events";
        private const string CommandsSectionName = "Commands";
        

        private ConcurrentDictionary<string, HandlerLogger> loggers = new ConcurrentDictionary<string, HandlerLogger>();

        public SalHandlerLogger(ILoggerProvider loggerProvider, IConfigWatcher configWatcher)
        {
            this.loggerProvider = loggerProvider;
            this.configWatcher = configWatcher;
            configWatcher.Subscribe(LoggingSettingsSectionName, (s, arg) =>
            {
                loggers.Clear();
            });
        }

        public void LogIncoming(CommandPayload commandPayload)
        {
            var loggerSettings = GetHandlerLogger(commandPayload);
            if (loggerSettings.Ignore)
                return;
            var pt = DateTime.UtcNow - commandPayload.Context.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append("[CP  <- BUS] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            if(!string.IsNullOrEmpty(commandPayload.Context.Descriptor.Version)) 
                sb.Append($"| V:{commandPayload.Context.Descriptor.Version} ");
            sb.Append($"| P:{commandPayload.Context.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandPayload.Context.Descriptor.PublishTimeStamp:O} ");
            if (commandPayload.Context.Descriptor.TTL.HasValue)
                sb.Append($"| TTL:{commandPayload.Context.Descriptor.TTL} ");

            if (loggerSettings.CropSize == 0)
            {
                loggerSettings.logger.LogInformation(sb.ToString());
                return;
            }

            sb.AppendLine();
            var body = loggerSettings.Formatting ? commandPayload.Payload.ToIndentedJson() : commandPayload.Payload.ToJson();

            if (loggerSettings.CropSize < 0)
                sb.Append(body);
            else
            {
                sb.Append(CropString(body, loggerSettings.CropSize));
            }
            loggerSettings.logger.LogInformation(sb.ToString());
        }
        public void LogIncoming(CommandResultPayload commandResultPayload)
        {
            var loggerSettings = GetHandlerLogger(commandResultPayload);
            if (loggerSettings.Ignore)
                return;


            var sb = new StringBuilder();
            sb.Append("[CRP <- BUS] ");
            sb.Append($"({commandResultPayload.Context.Descriptor.ProcessingDuration?.TotalSeconds:F3} c) ");
            sb.Append($"| P:{commandResultPayload.Context.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandResultPayload.Context.Descriptor.PublishTimeStamp:O} ");
            sb.Append($"| HTS:{commandResultPayload.Context.Descriptor.HandlerTimeStamp:O} ");

            if (loggerSettings.CropSize == 0)
            {
                loggerSettings.logger.LogInformation(sb.ToString());
                return;
            }

            sb.AppendLine();
            var body = loggerSettings.Formatting ? commandResultPayload.Payload.ToIndentedJson() : commandResultPayload.Payload.ToJson();

            if (loggerSettings.CropSize < 0)
                sb.Append(body);
            else
            {
                sb.Append(CropString(body, loggerSettings.CropSize));
            }
            loggerSettings.logger.LogInformation(sb.ToString());
        }
        public void LogIncoming(EventPayload eventPayload)
        {
            var loggerSettings = GetHandlerLogger(eventPayload);
            if (loggerSettings.Ignore)
                return;
            var pt = DateTime.UtcNow - eventPayload.Context.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append("[EP  <- BUS] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");

            if (loggerSettings.CropSize == 0)
            {
                loggerSettings.logger.LogInformation(sb.ToString());
                return;
            }

            sb.AppendLine();
            var body = loggerSettings.Formatting ? eventPayload.Payload.ToIndentedJson() : eventPayload.Payload.ToJson();

            if (loggerSettings.CropSize < 0)
                sb.Append(body);
            else
            {
                sb.Append(CropString(body, loggerSettings.CropSize));
            }
            loggerSettings.logger.LogInformation(sb.ToString());
        }

        public void LogOutgoing(CommandPayload commandPayload)
        {
            var loggerSettings = GetHandlerLogger(commandPayload);
            if (loggerSettings.Ignore)
                return;

            var sb = new StringBuilder();
            sb.Append("[CMD -> BUS] ");
            if(!string.IsNullOrEmpty(commandPayload.Context.Descriptor.Version)) 
                sb.Append($"| V:{commandPayload.Context.Descriptor.Version} ");
            sb.Append($"| P:{commandPayload.Context.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandPayload.Context.Descriptor.PublishTimeStamp:O} ");
            if (commandPayload.Context.Descriptor.TTL.HasValue)
                sb.Append($"| TTL:{commandPayload.Context.Descriptor.TTL} ");
            sb.Append($"| Route:{commandPayload.Context.Descriptor.CommandExchangeName}->{commandPayload.Context.Descriptor.CommandRoutingKey}");

            if (loggerSettings.CropSize == 0)
            {
                loggerSettings.logger.LogInformation(sb.ToString());
                return;
            }

            sb.AppendLine();
            var body = loggerSettings.Formatting ? commandPayload.Payload.ToIndentedJson() : commandPayload.Payload.ToJson();

            if (loggerSettings.CropSize < 0)
                sb.Append(body);
            else
            {
                sb.Append(CropString(body, loggerSettings.CropSize));
            }

            var old = HandlerContext.CorrelationId;
            HandlerContext.UpdateCorrelationId(commandPayload.Context.Descriptor.CorrelationId);
            loggerSettings.logger.LogInformation(sb.ToString());
            HandlerContext.UpdateCorrelationId(old);
        }
        public void LogOutgoing(CommandResultPayload commandResultPayload)
        {
            var loggerSettings = GetHandlerLogger(commandResultPayload);
            if (loggerSettings.Ignore)
                return;

            var sb = new StringBuilder();
            sb.Append("[RES -> BUS] ");
            if (commandResultPayload.Context.Descriptor.HandlerTimeStamp.HasValue)
            {
                var pt = DateTime.UtcNow - commandResultPayload.Context.Descriptor.HandlerTimeStamp.Value;
                sb.Append($"({pt.TotalSeconds:F3} c) ");
            }
            
            sb.Append($"| P:{commandResultPayload.Context.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandResultPayload.Context.Descriptor.PublishTimeStamp:O} ");

            if (loggerSettings.CropSize == 0)
            {
                loggerSettings.logger.LogInformation(sb.ToString());
                return;
            }

            sb.AppendLine();
            var body = loggerSettings.Formatting ? commandResultPayload.Payload.ToIndentedJson() : commandResultPayload.Payload.ToJson();

            if (loggerSettings.CropSize < 0)
                sb.Append(body);
            else
            {
                sb.Append(CropString(body, loggerSettings.CropSize));
            }
            
            var old = HandlerContext.CorrelationId;
            HandlerContext.UpdateCorrelationId(commandResultPayload.Context.Descriptor.CorrelationId);
            loggerSettings.logger.LogInformation(sb.ToString());
            HandlerContext.UpdateCorrelationId(old);
            
        }

        public void LogNullOutgoing(CommandDescriptor commandDescriptor)
        {
            var loggerSettings = GetHandlerLogger(commandDescriptor);
            if (loggerSettings.Ignore)
                return;

            var sb = new StringBuilder();
            sb.Append("[RES -> NUL] ");
            sb.Append($"| P:{commandDescriptor.Priority} ");
            sb.Append($"| PTS:{commandDescriptor.PublishTimeStamp:O} ");
            sb.Append($"| HTS:{commandDescriptor.HandlerTimeStamp:O} ");

            var old = HandlerContext.CorrelationId;
            HandlerContext.UpdateCorrelationId(commandDescriptor.CorrelationId);
            loggerSettings.logger.LogInformation(sb.ToString());
            HandlerContext.UpdateCorrelationId(old);
        }

        public void LogNullHandler(CommandResultPayload commandResultPayload)
        {
            var loggerSettings = GetHandlerLogger(commandResultPayload);
            if (loggerSettings.Ignore)
                return;

            var pt = DateTime.UtcNow - commandResultPayload.Context.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append("[CRP -> NUL] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            sb.Append($"| P:{commandResultPayload.Context.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandResultPayload.Context.Descriptor.PublishTimeStamp:O} ");
            sb.Append($"| HTS:{commandResultPayload.Context.Descriptor.HandlerTimeStamp:O} ");

            var old = HandlerContext.CorrelationId;
            HandlerContext.UpdateCorrelationId(commandResultPayload.Context.Descriptor.CorrelationId);
            loggerSettings.logger.LogInformation(sb.ToString());
            HandlerContext.UpdateCorrelationId(old);
        }

        public void LogHandler(CommandResultPayload commandResultPayload, string handlerName, bool handled)
        {
            var loggerSettings = GetHandlerLogger(commandResultPayload);
            if (loggerSettings.Ignore)
                return;

            var pt = DateTime.UtcNow - commandResultPayload.Context.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append($"[CRP -> {handlerName}] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            sb.Append($" = {handled} ");


            loggerSettings.logger.LogInformation(sb.ToString());

        }

        public void LogHandler(CommandPayload commandPayload, string handlerName)
        {
            var loggerSettings = GetHandlerLogger(commandPayload);
            if (loggerSettings.Ignore)
                return;

            var pt = DateTime.UtcNow - commandPayload.Context.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append($"[CP  -> {handlerName}] ");
            if(!string.IsNullOrEmpty(commandPayload.Context.Descriptor.Version)) 
                sb.Append($"V:{commandPayload.Context.Descriptor.Version} ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");


            loggerSettings.logger.LogInformation(sb.ToString());

        }

        public void LogHandler(EventPayload eventPayload, string handlerName)
        {
            var loggerSettings = GetHandlerLogger(eventPayload);
            if (loggerSettings.Ignore)
                return;
            var pt = DateTime.UtcNow - eventPayload.Context.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append($"[EP  -> {handlerName}] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");

            loggerSettings.logger.LogInformation(sb.ToString());
        }

        public void LogOutgoing(EventPayload eventPayload)
        {
            var loggerSettings = GetHandlerLogger(eventPayload);
            if (loggerSettings.Ignore)
                return;

            var sb = new StringBuilder();
            sb.Append("[EVN -> BUS] ");
            
            if (loggerSettings.CropSize == 0)
            {
                loggerSettings.logger.LogInformation(sb.ToString());
                return;
            }

            sb.AppendLine();
            var body = loggerSettings.Formatting ? eventPayload.Payload.ToIndentedJson() : eventPayload.Payload.ToJson();

            if (loggerSettings.CropSize < 0)
                sb.Append(body);
            else
            {
                sb.Append(CropString(body, loggerSettings.CropSize));
            }
            loggerSettings.logger.LogInformation(sb.ToString());
        }

        public ILogger GetLogger(CommandPayload commandPayload)
        {
            var handlerLogger = GetHandlerLogger(commandPayload);
            return handlerLogger.logger;
        }

        public ILogger GetLogger(CommandResultPayload commandResultPayload)
        {
            var handlerLogger = GetHandlerLogger(commandResultPayload);
            return handlerLogger.logger;
        }


        public ILogger GetLogger(EventPayload eventPayload)
        {
            var handlerLogger = GetHandlerLogger(eventPayload);
            return handlerLogger.logger;
        }



        private HandlerLogger GetHandlerLogger(CommandPayload commandPayload)
        {
            var loggerName = $"{commandPayload.Context.Descriptor.CommandName}.Command";

            return loggers.GetOrAdd(loggerName, s =>
            {
                var section = configWatcher.GetSection(LoggingSettingsSectionName);
                if (section == null)
                {
                    return GetDefaultHandlerLogger(s);
                }
                else
                {
                    var loggingItems = section.GetSafeValue<Dictionary<string, LoggingItem>>(CommandsSectionName, null);
                    if (loggingItems == null)
                    {
                        return GetDefaultHandlerLogger(s);
                    }


                    foreach (var loggingConfigItem in loggingItems)
                    {
                        if (Regex.IsMatch(commandPayload.Context.Descriptor.CommandName, loggingConfigItem.Key, RegexOptions.IgnoreCase))
                        {
                            return new HandlerLogger
                            {
                                Ignore = loggingConfigItem.Value.Ignore,
                                CropSize = loggingConfigItem.Value.CropSize,
                                Formatting = loggingConfigItem.Value.Formatting,
                                logger = loggerProvider.CreateLogger(s)
                            };

                        }
                    }
                    return GetDefaultHandlerLogger(s);

                }
            });

        }

        private HandlerLogger GetHandlerLogger(CommandDescriptor commandDescriptor)
        {
            var loggerName = $"{commandDescriptor.CommandName}.Command";

            return loggers.GetOrAdd(loggerName, s =>
            {
                var section = configWatcher.GetSection(LoggingSettingsSectionName);
                if (section == null)
                {
                    return GetDefaultHandlerLogger(s);
                }
                else
                {
                    var loggingItems = section.GetSafeValue<Dictionary<string, LoggingItem>>(CommandsSectionName, null);
                    if (loggingItems == null)
                    {
                        return GetDefaultHandlerLogger(s);
                    }


                    foreach (var loggingConfigItem in loggingItems)
                    {
                        if (Regex.IsMatch(commandDescriptor.CommandName, loggingConfigItem.Key, RegexOptions.IgnoreCase))
                        {
                            return new HandlerLogger
                            {
                                Ignore = loggingConfigItem.Value.Ignore,
                                CropSize = loggingConfigItem.Value.CropSize,
                                Formatting = loggingConfigItem.Value.Formatting,
                                logger = loggerProvider.CreateLogger(s)
                            };

                        }
                    }
                    return GetDefaultHandlerLogger(s);

                }
            });

        }
        private HandlerLogger GetHandlerLogger(CommandResultPayload commandResultPayload)
        {

            var loggerName = $"{commandResultPayload.Context.Descriptor.CommandName}.Command";

            return loggers.GetOrAdd(loggerName, s =>
            {
                var section = configWatcher.GetSection(LoggingSettingsSectionName);
                if (section == null)
                {
                    return GetDefaultHandlerLogger(s);
                }
                else
                {
                    var loggingItems = section.GetSafeValue<Dictionary<string, LoggingItem>>(CommandsSectionName, null);
                    if (loggingItems == null)
                    {
                        return GetDefaultHandlerLogger(s);
                    }


                    foreach (var loggingConfigItem in loggingItems)
                    {
                        if (Regex.IsMatch(commandResultPayload.Context.Descriptor.CommandName, loggingConfigItem.Key, RegexOptions.IgnoreCase))
                        {
                            return new HandlerLogger
                            {
                                Ignore = loggingConfigItem.Value.Ignore,
                                CropSize = loggingConfigItem.Value.CropSize,
                                Formatting = loggingConfigItem.Value.Formatting,
                                logger = loggerProvider.CreateLogger(s)
                            };

                        }
                    }
                    return GetDefaultHandlerLogger(s);

                }
            });
        }

        private HandlerLogger GetHandlerLogger(EventPayload eventPayload)
        {
            var eventName = $"{eventPayload.Context.Descriptor.EventName}";
            var loggerName = $"{eventName}.Event";

            return loggers.GetOrAdd(loggerName, s =>
            {
                var section = configWatcher.GetSection(LoggingSettingsSectionName);
                if (section == null)
                {
                    return GetDefaultHandlerLogger(s);
                }
                else
                {
                    var loggingItems = section.GetSafeValue<Dictionary<string, LoggingItem>>(EventsSectionName, null);
                    if (loggingItems == null)
                    {
                        return GetDefaultHandlerLogger(s);
                    }

                    foreach (var loggingConfigItem in loggingItems)
                    {
                        if (Regex.IsMatch(eventName, loggingConfigItem.Key, RegexOptions.IgnoreCase))
                        {
                            return new HandlerLogger
                            {
                                Ignore = loggingConfigItem.Value.Ignore,
                                CropSize = loggingConfigItem.Value.CropSize,
                                Formatting = loggingConfigItem.Value.Formatting,
                                logger = loggerProvider.CreateLogger(s)
                            };

                        }
                    }
                    return GetDefaultHandlerLogger(s);

                }
            });
        }
        private HandlerLogger GetDefaultHandlerLogger(string name)
        {
            return new HandlerLogger
            {
                logger = loggerProvider.CreateLogger(name),
                Ignore = false,
                Formatting = false,
                CropSize = -1

            };
        }

        string CropString(string data, int cropSize)
        {
            if (data.Length < cropSize)
                return data;
            return $"{data.Substring(0, cropSize)}...[{data.Length}]";
        }

    }

    public class HandlerLogger
    {
        public bool Ignore;
        public ILogger logger;
        public int CropSize;
        public bool Formatting;
    }
}
