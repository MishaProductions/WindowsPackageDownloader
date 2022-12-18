using WindowsPackageDownloader.Modules.Utils;

namespace WindowsPackageDownloader.Modules
{
    public class Modules
    {
        public static bool GetIfVersionAvailable(Version reqVer) {
            bool success = false;

            if (reqVer.RequestVersion == null) {
                success = false;
            }

            return success;
        }
    }
}
