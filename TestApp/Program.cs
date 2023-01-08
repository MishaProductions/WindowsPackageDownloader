using WindowsPackageDownloader.Core;

namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var client = new WUClient();
            var build = await client.FetchBuild("10.0.16299.15", WUArch.amd64, "Retail", "rs3_release", "Active");
           
            Console.Write("Press any key to exit . . .");
            Console.ReadKey();
        }
    }
}
