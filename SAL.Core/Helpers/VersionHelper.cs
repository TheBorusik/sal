using System;

namespace SAL.Core.Helpers
{
    public static class VersionHelper
    {
        public static int CalculateVersion(this Version version)
        {
            return version.Major * 100000 + version.Minor * 1000 + version.Build;
        }
    }
}
