using System;

namespace SAL.Core.Helpers
{
    public static class VersionHelper
    {
        public static int CalculateVersion(this Version version)
        {
            return version.Major * 1000000 + version.Minor * 100000 + version.Build;
        }
    }
}
