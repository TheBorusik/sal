using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.Core.Configuration.Logging;
using SAL.Core.DTO.Transport;

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
        }

        public void LogIncoming(CommandPayload commandPayload)
        {
            var loggerSettings = GetHandlerLogger(commandPayload);
            if (loggerSettings.Ignore)
                return;
            var pt = DateTime.UtcNow - commandPayload.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append("[CP  <- BUS] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            sb.Append($"| CID:{commandPayload.Descriptor.CorrelationId} ");
            sb.Append($"| P:{commandPayload.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandPayload.Descriptor.PublishTimeStamp:O} ");
            sb.Append($"| IS:{commandPayload.Descriptor.IsSync} ");
            if (commandPayload.Descriptor.TTL.HasValue)
                sb.Append($"| TTL:{commandPayload.Descriptor.TTL} ");

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
                sb.Append(body.Substring(0, loggerSettings.CropSize));
            }
            loggerSettings.logger.LogInformation(sb.ToString());
        }
        public void LogIncoming(CommandResultPayload commandResultPayload)
        {
            var loggerSettings = GetHandlerLogger(commandResultPayload);
            if (loggerSettings.Ignore)
                return;

            var pt = DateTime.UtcNow - commandResultPayload.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append("[CRP <- BUS] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            sb.Append($"| CID:{commandResultPayload.Descriptor.CorrelationId} ");
            sb.Append($"| P:{commandResultPayload.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandResultPayload.Descriptor.PublishTimeStamp:O} ");
            sb.Append($"| HTS:{commandResultPayload.Descriptor.HandlerTimeStamp:O} ");
            sb.Append($"| IS:{commandResultPayload.Descriptor.IsSync} ");

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
                sb.Append(body.Substring(0, loggerSettings.CropSize));
            }
            loggerSettings.logger.LogInformation(sb.ToString());
        }
        public void LogIncoming(EventPayload eventPayload)
        {
            var loggerSettings = GetHandlerLogger(eventPayload);
            if (loggerSettings.Ignore)
                return;
            var pt = DateTime.UtcNow - eventPayload.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append("[EP  <- BUS] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            sb.Append($"| CID:{eventPayload.Descriptor.CorrelationId} ");

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
                sb.Append(body.Substring(0, loggerSettings.CropSize));
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
            sb.Append($"| CID:{commandPayload.Descriptor.CorrelationId} ");
            sb.Append($"| P:{commandPayload.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandPayload.Descriptor.PublishTimeStamp:O} ");
            sb.Append($"| IS:{commandPayload.Descriptor.IsSync} ");
            if (commandPayload.Descriptor.TTL.HasValue)
                sb.Append($"| TTL:{commandPayload.Descriptor.TTL} ");
            if (string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterType))
                sb.Append($"| DST:{commandPayload.Descriptor.DestinationAdapterType}.{commandPayload.Descriptor.DestinationAdapterName}");

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
                sb.Append(body.Substring(0, loggerSettings.CropSize));
            }
            loggerSettings.logger.LogInformation(sb.ToString());
        }
        public void LogOutgoing(CommandResultPayload commandResultPayload)
        {
            var loggerSettings = GetHandlerLogger(commandResultPayload);
            if (loggerSettings.Ignore)
                return;

            var sb = new StringBuilder();
            sb.Append("[RES -> BUS] ");
            if (commandResultPayload.Descriptor.HandlerTimeStamp.HasValue)
            {
                var pt = DateTime.UtcNow - commandResultPayload.Descriptor.HandlerTimeStamp.Value;
                sb.Append($"({pt.TotalSeconds:F3} c) ");
            }

            sb.Append($"| CID:{commandResultPayload.Descriptor.CorrelationId} ");
            sb.Append($"| P:{commandResultPayload.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandResultPayload.Descriptor.PublishTimeStamp:O} ");
            sb.Append($"| IS:{commandResultPayload.Descriptor.IsSync} ");

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
                sb.Append(body.Substring(0, loggerSettings.CropSize));
            }
            loggerSettings.logger.LogInformation(sb.ToString());
        }

        public void LogNullOutgoing(CommandDescriptor commandDescriptor)
        {
            var loggerSettings = GetHandlerLogger(commandDescriptor);
            if (loggerSettings.Ignore)
                return;

            var sb = new StringBuilder();
            sb.Append("[RES -> NUL] ");
            sb.Append($"| CID:{commandDescriptor.CorrelationId} ");
            sb.Append($"| P:{commandDescriptor.Priority} ");
            sb.Append($"| PTS:{commandDescriptor.PublishTimeStamp:O} ");
            sb.Append($"| HTS:{commandDescriptor.HandlerTimeStamp:O} ");
            sb.Append($"| IS:{commandDescriptor.IsSync} ");

            loggerSettings.logger.LogInformation(sb.ToString());
        }

        public void LogNullHandler(CommandResultPayload commandResultPayload)
        {
            var loggerSettings = GetHandlerLogger(commandResultPayload);
            if (loggerSettings.Ignore)
                return;

            var pt = DateTime.UtcNow - commandResultPayload.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append("[CRP -> NUL] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            sb.Append($"| CID:{commandResultPayload.Descriptor.CorrelationId} ");
            sb.Append($"| P:{commandResultPayload.Descriptor.Priority} ");
            sb.Append($"| PTS:{commandResultPayload.Descriptor.PublishTimeStamp:O} ");
            sb.Append($"| HTS:{commandResultPayload.Descriptor.HandlerTimeStamp:O} ");
            sb.Append($"| IS:{commandResultPayload.Descriptor.IsSync} ");

            loggerSettings.logger.LogInformation(sb.ToString());
        }

        public void LogHandler(CommandResultPayload commandResultPayload, string handlerName, bool handled)
        {
            var loggerSettings = GetHandlerLogger(commandResultPayload);
            if (loggerSettings.Ignore)
                return;

            var pt = DateTime.UtcNow - commandResultPayload.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append($"[CRP -> {handlerName}] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            sb.Append($" = {handled} ");
            sb.Append($"| CID:{commandResultPayload.Descriptor.CorrelationId} ");

            loggerSettings.logger.LogInformation(sb.ToString());

        }

        public void LogHandler(CommandPayload commandPayload, string handlerName)
        {
            var loggerSettings = GetHandlerLogger(commandPayload);
            if (loggerSettings.Ignore)
                return;

            var pt = DateTime.UtcNow - commandPayload.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append($"[CP  -> {handlerName}] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            sb.Append($"| CID:{commandPayload.Descriptor.CorrelationId} ");

            loggerSettings.logger.LogInformation(sb.ToString());

        }

        public void LogHandler(EventPayload eventPayload, string handlerName)
        {
            var loggerSettings = GetHandlerLogger(eventPayload);
            if (loggerSettings.Ignore)
                return;
            var pt = DateTime.UtcNow - eventPayload.Descriptor.PublishTimeStamp;
            var sb = new StringBuilder();
            sb.Append($"[EP  -> {handlerName}] ");
            sb.Append($"({pt.TotalSeconds:F3} c) ");
            sb.Append($"| CID:{eventPayload.Descriptor.CorrelationId} ");

            loggerSettings.logger.LogInformation(sb.ToString());
        }

        public void LogOutgoing(EventPayload eventPayload)
        {
            var loggerSettings = GetHandlerLogger(eventPayload);
            if (loggerSettings.Ignore)
                return;

            var sb = new StringBuilder();
            sb.Append("[EVN -> BUS] ");
            sb.Append($"| CID:{eventPayload.Descriptor.CorrelationId} ");


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
                sb.Append(body.Substring(0, loggerSettings.CropSize));
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
            var loggerName = $"{commandPayload.Descriptor.CommandName}.Command";

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
                        if (Regex.IsMatch(commandPayload.Descriptor.CommandName, loggingConfigItem.Key, RegexOptions.IgnoreCase))
                        {
                            return new HandlerLogger
                            {
                                Ignore = loggingConfigItem.Value.Ignore,
                                CropSize = loggingConfigItem.Value.СropSize,
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
                                CropSize = loggingConfigItem.Value.СropSize,
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

            var loggerName = $"{commandResultPayload.Descriptor.CommandName}.Command";

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
                        if (Regex.IsMatch(commandResultPayload.Descriptor.CommandName, loggingConfigItem.Key, RegexOptions.IgnoreCase))
                        {
                            return new HandlerLogger
                            {
                                Ignore = loggingConfigItem.Value.Ignore,
                                CropSize = loggingConfigItem.Value.СropSize,
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
            var eventName = $"{eventPayload.Descriptor.EventName}";
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
                                CropSize = loggingConfigItem.Value.СropSize,
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

    }

    public class HandlerLogger
    {
        public bool Ignore;
        public ILogger logger;
        public int CropSize;
        public bool Formatting;
    }
}
