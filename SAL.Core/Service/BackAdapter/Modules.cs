using System;
using System.Collections.Generic;
using Autofac;
using SAL.API;
using SAL.API.Monad;
using SAL.Core.Config;

namespace SAL.Core.Service
{
    internal partial class BackAdapter
    {
        protected virtual void ConfigureModules(ContainerBuilder builder)
        {
            //   Log.Trace("InitModules...");
            var modules = new List<IModule>();
            var modulesTypeStr = ConfigWatcher.GetSection(ConfigurationSectionNames.Modules).ConvertValue<string[]>();
            modulesTypeStr?.ForEach(mn =>
            {

                var moduleType = Type.GetType(mn, false);
                if (moduleType == null)
                {

                    throw new Exception($"Не найден тип модуля {mn}");
                }
                var imodule = (IModule)Activator.CreateInstance(moduleType);
                //      Log.Trace($"Модуль {moduleType.FullName} - активирован");
                modules.Add(imodule);

            });

            //    Log.Trace("ConfigureModules...");
            modules?.ForEach(m =>
            {
                m.Configure(builder);
                //   Log.Trace($"{m.GetType().FullName}.Configure();");
            });

            //    Log.Trace("ConfigureModules Done.");
        }
    }
}