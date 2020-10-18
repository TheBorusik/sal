using System.Text;
using SAL.API;

namespace SAL.Core.Service
{
    internal partial class FrontAdapter : BackAdapter
    {
        protected override void ShowStartupInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine()
                .AppendLine("-------------------------------------------------------------")
                .AppendLine($"RunnerType      : {GetType().Name}")
                .AppendLine($"AdapterName     : {AdapterConfiguration.AdapterName}")
                .AppendLine($"AdapterType     : {AdapterConfiguration.AdapterType}")
                .AppendLine($"AdapterVersion  : {AdapterConfiguration.AdapterVersion}")
                .AppendLine($"AdapterHostName : {AdapterConfiguration.AdapterHostName}")
                .AppendLine($"AdapterHostIp   : {string.Join(", ", AdapterConfiguration.AdapterHostIp)}")
                .AppendLine($"FrontContour    : {AdapterConfiguration.Contour}")
                .AppendLine($"BackContour     : {AdapterConfiguration.BackContour}")
                .AppendLine($"SalVersion      : {AdapterConfiguration.SalVersion} ({AdapterConfiguration.Revision})")
                .AppendLine($"RootPath        : {AdapterConfiguration.RootPath}")
                .AppendLine($"ConfigPath      : {AdapterConfiguration.ConfigPath}")
                .AppendLine($"LogRootPath     : {AdapterConfiguration.LogRootPath}")
                .AppendLine($"DiskStorePath   : {AdapterConfiguration.DiskStorePath}")
                .AppendLine("-------------------------------------------------------------");
            logger.Info(sb.ToString());
        }
    }
}
