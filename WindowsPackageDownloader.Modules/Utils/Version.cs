namespace WindowsPackageDownloader.Modules.Utils
{
    public class Version
    {
        public string? RequestVersion { get; set; }
        
        private string SiteUrl = "site.com";

        public static string StringToVersion(string reqVer)
        {
            return "https://"+reqVer+"/download/"+reqVer+"/";
        }
    }
}