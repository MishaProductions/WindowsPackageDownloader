using WindowsPackageDownloader.Core;

namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var client = new WUClient();
            var build = await client.FetchBuild("10.0.16299.15", WUArch.amd64, "Retail", "rs3_release", "Active", "48");
            foreach (var item in build.DownloadInfo)
            {
                if(item.Size> 324288000)
                Console.WriteLine(item.Name + ", " + item.Size + ", " + item.Url);
            }
            Console.Write("Press any key to exit . . .");
            Console.ReadKey();
        }
    }
}
