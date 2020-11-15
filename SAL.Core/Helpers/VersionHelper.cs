using System;

namespace SAL.Core.Helpers
{
    public static class VersionHelper
    {
        public static int CalculateVersion(this Version version)
        {
            return version.Major * 10000 + version.Minor * 100 + version.Build;
        }
    }
}
