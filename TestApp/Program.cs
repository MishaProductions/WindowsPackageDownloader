using WindowsPackageDownloader.Core;

namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var client = new WUClient();
            await client.FetchUpdate("All", "ReleasePreview", "Mainline", 20349, 1, 126);
            Console.Write("Press any key to exit . . .");
            Console.ReadKey();
        }
    }
}