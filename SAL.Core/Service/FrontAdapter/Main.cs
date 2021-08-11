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
                .AppendLine($"InDocker        : {AdapterConfiguration.InDocker}")
                .AppendLine($"MachineName     : {AdapterConfiguration.MachineName}")
                .AppendLine($"AdapterContour  : {AdapterConfiguration.AdapterContour}")
                .AppendLine($"AdapterType     : {AdapterConfiguration.AdapterType}")
                .AppendLine($"AdapterName     : {AdapterConfiguration.AdapterName}")
                .AppendLine($"AdapterVersion  : {AdapterConfiguration.AdapterVersion}")
                .AppendLine($"FrontContourName: {AdapterConfiguration.ContourName}")
                .AppendLine($"BackContourName : {AdapterConfiguration.BackContourName}")
                .AppendLine($"SalVersion      : {AdapterConfiguration.SalVersion} ({AdapterConfiguration.Revision})")
                .AppendLine($"RootPath        : {AdapterConfiguration.RootPath}")
                .AppendLine($"ConfigPath      : {AdapterConfiguration.ConfigPath}")
                .AppendLine($"LogRootPath     : {AdapterConfiguration.LogRootPath}")
                .AppendLine($"RootStorePath   : {AdapterConfiguration.DiskStorePath}")
                .AppendLine("-------------------------------------------------------------");
            logger.Info(sb.ToString());
        }
    }
}
