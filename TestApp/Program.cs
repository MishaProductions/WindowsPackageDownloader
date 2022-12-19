using WindowsPackageDownloader.Core;

namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var client = new WUClient();
            await client.FetchUpdate("All", "Retail", "Active", 20348, 1, 8);
            Console.Write("Press any key to exit . . .");
            Console.ReadKey();
        }
    }
}