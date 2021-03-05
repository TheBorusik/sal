using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using SAL.API;

namespace SAL.Core.NLogEx.Adapter
{
    internal class JTokenLoggingConfiguration : LoggingConfiguration
    {
        private readonly LogFactory logFactory;
        public bool AutoReload { get; private set; }
        public bool InitializeSucceeded { get; private set; }

        private ConfigurationItemFactory ConfigurationItemFactory => ConfigurationItemFactory.Default;

        public JTokenLoggingConfiguration(JToken jnlog, LogFactory logFactory)
        {
            this.logFactory = logFactory;
            Initialize(jnlog);
        }

        private void Initialize(JToken jn)
        {
            InitializeSucceeded = false;

            ParseNLogElement(jn);

            InitializeSucceeded = true;
        }

        private void ParseNLogElement(JToken jn)
        {
            InternalLogger.Trace(nameof(ParseNLogElement));
            InternalLogger.LogLevel = LogLevel.FromString(jn.GetSafeValue("internalLogLevel", "Info"));
            AutoReload = jn.GetSafeValue("autoReload", false);

            logFactory.ThrowExceptions = jn.GetSafeValue("throwExceptions", logFactory.ThrowExceptions);
            logFactory.ThrowConfigExceptions = jn.GetSafeValue("throwConfigExceptions", logFactory.ThrowConfigExceptions);
            logFactory.KeepVariablesOnReload = jn.GetSafeValue("keepVariablesOnReload", this.logFactory.KeepVariablesOnReload);
            InternalLogger.LogToConsole = jn.GetSafeValue("internalLogToConsole", InternalLogger.LogToConsole);
            InternalLogger.LogToConsoleError = jn.GetSafeValue("internalLogToConsoleError", InternalLogger.LogToConsoleError);
            InternalLogger.LogFile = jn.GetSafeValue("internalLogFile", InternalLogger.LogFile);
            InternalLogger.LogToTrace = jn.GetSafeValue("internalLogToTrace", InternalLogger.LogToTrace);
            InternalLogger.IncludeTimestamp = jn.GetSafeValue("internalLogIncludeTimestamp", InternalLogger.IncludeTimestamp);
            logFactory.GlobalThreshold = LogLevel.FromString(jn.GetSafeValue("globalThreshold", logFactory.GlobalThreshold.ToString()));


            var token = jn.GetValueIC("appenders");
            if (token != null)
                ParseTargetsSection(token);

            token = jn.GetValueIC("targets");
            if (token != null)
                ParseTargetsSection(token);

            token = jn.GetValueIC("variable");
            if (token != null)
                ParseVariableElement(token);

            token = jn.GetValueIC("time");
            if (token != null)
                ParseTimeElement(token);

            token = jn.GetValueIC("rules");
            if (token != null)
                ParseRulesElement(token, LoggingRules);
        }

        private void ParseTargetsSection(JToken jt)
        {
            var isAsync = jt.GetSafeValue("@async", false);

            jt.ForEach(t =>
            {
                if (t is JProperty prop)
                {
                    var localName = prop.Name;
                    if (localName.Equals("@async", StringComparison.InvariantCultureIgnoreCase))
                        return;


                    var target = ParseTargetsElement(prop.Value, localName);
                    if(target == null)
                        return;


                    if (isAsync)
                        target = WrapWithAsyncTargetWrapper(target);

                    InternalLogger.Info("Adding target {0}", target);
                    AddTarget(target.Name, target);
                }
            });
        }

        private Target ParseTargetsElement(JToken tTarget,string localName)
        {
            if (tTarget is JObject oTarget)
            {
                var itemType = oTarget.GetSafeValue("type", "");

                if (string.IsNullOrWhiteSpace(itemType))
                {
                    if (string.IsNullOrWhiteSpace(localName))
                        return null;
                    throw new NLogConfigurationException($"Missing \"type\" property on \"{localName}\" target.");
                }

                var target = this.ConfigurationItemFactory.Targets.CreateInstance(itemType);
                if (!string.IsNullOrWhiteSpace(localName))
                    target.Name = localName;

                ConfigureObjectFrom(target, tTarget, true);

                if (target is WrapperTargetBase wrapperTarget)
                {
                    var wrappedJTarget = oTarget.GetValueIC("target");
                    if (wrappedJTarget != null)
                    {
                        var wrappedTarget =  ParseTargetsElement(wrappedJTarget, null);
                        if (wrappedTarget != null)
                            wrapperTarget.WrappedTarget = wrappedTarget;
                    }
                }

                return target;

            }

            return null;
        }

        private void ConfigureObjectFrom(object obj, JToken jToken, bool ignoreType)
        {
            jToken.ForEach(p =>
            {
                if (p is JProperty token)
                {
                    var propertyName = token.Name;

                    if (propertyName.Equals("type", StringComparison.InvariantCultureIgnoreCase))
                        return;

                    if (TryGetPropertyInfo(propertyName, out var property))
                    {
                        if (SetArrayItemFromElement(property, obj, token.Value) || SetLayoutFromElement(property, obj, token.Value))
                            return;

                        InternalLogger.Debug($"Setting '{obj.GetType().Name}.{propertyName}' to '{token}'");
                        property.SetValue(obj, token.Value.ConvertValue(property.PropertyType), null);
                    }
                }
            });


            bool TryGetPropertyInfo(string propertyName, out PropertyInfo result)
            {
                result = null;
                var property = obj.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                if (property != null)
                {
                    result = property;
                    return true;
                }


                return false;
            }
        }

        private bool SetArrayItemFromElement(PropertyInfo pi, object o, JToken jToken)
        {
            if (pi.IsDefined(typeof(ArrayParameterAttribute), false))
            {
                if (jToken is JArray jArray)
                {
                    var listType = pi.PropertyType.GetGenericArguments().First();
                    var list = (IList)pi.GetValue(o, null);

                    jArray.ForEach(j => list.Add(j.ToObject(listType)));
                    return true;
                }


                //throw new NotSupportedException("Parameter " + pi.Name + " of " + o.GetType().Name + " is an array and cannot be assigned a scalar value.");
            }
            return false;
        }

        private bool SetLayoutFromElement(PropertyInfo pi, object o, JToken jToken)
        {
            if (jToken is JObject jLayout)
            {
                var layoutType = jLayout.GetSafeValue("type", "");
                if (string.IsNullOrWhiteSpace(layoutType))
                    return false;
                var layoutInstance = this.ConfigurationItemFactory.Layouts.CreateInstance(layoutType);
                if (layoutInstance == null)
                    return false;

                ConfigureObjectFrom(layoutInstance, jLayout, false);

                pi.SetValue(o, layoutInstance, null);
                return true;
            }

            return false;
        }

        private void ParseVariableElement(JToken token)
        {
        }

        private void ParseTimeElement(JToken token)
        {
        }

        private void ParseRulesElement(JToken token, IList<LoggingRule> rulesCollection)
        {
            token.ForEach(r =>
            {
                if (r is JObject jr)
                {
                    if (!jr.GetSafeValue("enabled", true))
                        return;
                    var rule = new LoggingRule();
                    rule.LoggerNamePattern = jr.GetSafeValue("Pattern", "*");
                    var appendTo = jr.GetSafeValue<string>("appendTo", null) ?? jr.GetSafeValue<string>("writeTo", null);
                    if (!string.IsNullOrWhiteSpace(appendTo))
                    {
                        appendTo.Split(',').ForEach(s =>
                        {
                            var traget = FindTargetByName(s.Trim());
                            if (traget == null)
                                throw new NLogConfigurationException($"Target {s} not found.");
                            rule.Targets.Add(traget);
                        });
                    }

                    rule.Final = jr.GetSafeValue("final", false);

                    if (jr.TryGetValue<string>("level", out var levelStr))
                    {
                        rule.EnableLoggingForLevel(LogLevel.FromString(levelStr));
                    }
                    else if (jr.TryGetValue<string>("levels", out var levelsStr))
                    {
                        levelsStr.Split(',').ForEach(s =>
                        {
                            rule.EnableLoggingForLevel(LogLevel.FromString(s.Trim()));
                        });
                    }
                    else
                    {
                        var min = 0;
                        var max = LogLevel.Fatal.Ordinal;

                        if (jr.TryGetValue<string>("minLevel", out var minLevel))
                            min = NLog.LogLevel.FromString(minLevel).Ordinal;

                        if (jr.TryGetValue<string>("maxLevel", out var maxLevel))
                            max = NLog.LogLevel.FromString(maxLevel).Ordinal;

                        rule.EnableLoggingForLevels(LogLevel.FromOrdinal(min), LogLevel.FromOrdinal(max));
                    }

                    token = jr.GetValueIC("Filters");
                    if (token != null)
                        ParseFilters(rule, token);

                    token = jr.GetValueIC("Loggers");
                    if (token != null)
                        ParseRulesElement(token, rule.ChildRules);


                    rulesCollection.Add(rule);
                }
            });
        }

        private void ParseFilters(LoggingRule rule, JToken token)
        {

        }

        private static Target WrapWithAsyncTargetWrapper(Target target)
        {
            AsyncTargetWrapper asyncTargetWrapper = new AsyncTargetWrapper();
            asyncTargetWrapper.WrappedTarget = target;
            asyncTargetWrapper.Name = target.Name;
            target.Name += "_sync";
            InternalLogger.Debug<string, string>("Wrapping target '{0}' with AsyncTargetWrapper and renaming to '{1}", asyncTargetWrapper.Name, target.Name);
            target = (Target)asyncTargetWrapper;
            return target;
        }

    }
}