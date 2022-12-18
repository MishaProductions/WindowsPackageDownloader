using WindowsPackageDownloader.Modules.Utils;
using WindowsPackageDownloader.Modules.Exception;
using Version = WindowsPackageDownloader.Modules.Utils.Version;

namespace WindowsPackageDownloader.Modules
{
    public class Modules
    {
        public string VersionToDownload(Version reqVer) {

            if (reqVer == null) {
                throw new VersionNotFoundException("Version is not found");
            }

            return Version.StringToVersion(reqVer.RequestVersion);
        }
    }
}
