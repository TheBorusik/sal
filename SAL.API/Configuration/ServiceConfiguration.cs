namespace SAL.API
{
    public static class ServiceConfiguration
    {
        public static string AdapterName { get; internal set; }
        public static string AdapterType { get; internal set; }
        public static string AdapterVersion { get; internal set; }
        public static string AdapterHostName { get; internal set; }
        public static string[] AdapterHostIp { get; internal set; }

        public static string LogRootPath { get; internal set; }

        public static string RootPath { get; internal set; }
        public static string ConfigPath { get; internal set; }

        public static string DiskStorePath { get; internal set; }

        public static int SalVersion { get; internal set; }
        public static int Revision { get; internal set; }

        public static string Contour { get; internal set; }
    }
}