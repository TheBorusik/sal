using SAL.Infrastructure;

namespace SAL.API
{
    public static class AdapterConfiguration
    {
        public static string AdapterName { get; internal set; }
        public static string AdapterType { get; internal set; }
        public static string AdapterFullName => $"{AdapterType}.{AdapterName}";
        public static string AdapterVersion { get; internal set; }

        public static string LogRootPath { get; internal set; }

        public static string RootPath { get; internal set; }
        public static string ConfigPath { get; internal set; }

        public static string DiskStorePath { get; internal set; }

        public static int SalVersion { get; internal set; }
        public static int Revision { get; internal set; }

        public static Contour AdapterContour { get; internal set; }
        public static string ContourName { get; internal set; }
        public static string BackContourName { get; internal set; }

        public static bool InDocker { get; internal set; }
        public static string MachineName { get; internal set; }
    }
}