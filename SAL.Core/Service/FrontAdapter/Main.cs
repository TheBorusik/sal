using System;
using System.Collections.Generic;
using System.Text;
using SAL.API;

namespace SAL.Core.Service
{
    public partial class FrontAdapter : BackAdapter
    {
        protected override void ShowStartupInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine()
                .AppendLine("-------------------------------------------------------------")
                .AppendLine($"RunnerType      : {GetType().Name}")
                .AppendLine($"AdapterName     : {ServiceConfiguration.AdapterName}")
                .AppendLine($"AdapterType     : {ServiceConfiguration.AdapterType}")
                .AppendLine($"AdapterVersion  : {ServiceConfiguration.AdapterVersion}")
                .AppendLine($"AdapterHostName : {ServiceConfiguration.AdapterHostName}")
                .AppendLine($"AdapterHostIp   : {string.Join(", ", ServiceConfiguration.AdapterHostIp)}")
                .AppendLine($"FrontContour    : {ServiceConfiguration.FrontContour}")
                .AppendLine($"BackContour     : {ServiceConfiguration.Contour}")
                .AppendLine($"SalVersion      : {ServiceConfiguration.SalVersion} ({ServiceConfiguration.Revision})")
                .AppendLine($"RootPath        : {ServiceConfiguration.RootPath}")
                .AppendLine($"ConfigPath      : {ServiceConfiguration.ConfigPath}")
                .AppendLine($"LogRootPath     : {ServiceConfiguration.LogRootPath}")
                .AppendLine($"DiskStorePath   : {ServiceConfiguration.DiskStorePath}")
                .AppendLine("-------------------------------------------------------------");
            logger.Info(sb.ToString());
        }
    }
}
