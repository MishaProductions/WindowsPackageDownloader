using WindowsPackageDownloader.Modules.Utils;
using WindowsPackageDownloader.Modules.Exception;

namespace WindowsPackageDownloader.Modules
{
    public class Modules
    {
        public static string GetIfVersionAvailable(Version reqVer) {

            if (reqVer.RequestVersion == null) {
                throw new VersionNotFoundException("Version is not found");
            }

            return "";
        }
    }
}
