using WindowsPackageDownloader.Core;

namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var client = new WUClient();
            await client.FetchUpdate("All", "Dev", "Mainline", 25267, 1000, 8);
            Console.Write("Press any key to exit . . .");
            Console.ReadKey();
        }
    }
}