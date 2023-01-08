using WindowsPackageDownloader.Core;

namespace WindowsPackageDownloader.Website
{
    public class UUP
    {
        public static async Task<BuildInfo?> FetchUpdate(string build, WUArch arch, string ring, string branch, string flight, string sku, bool EnableCache = true)
        {
            var client = new WUClient();
            var cache = Cache.GetEntry(build, ring, arch, sku);
            if (cache != null && EnableCache)
            {
                var x = Cache.GetFullBuildInfo(cache.UpdateID);


                //we need to make sure that the download links did not expire
                var file = x.Files[0].expires;
                if(DateTime.Now > file)
                {
                    //it expired, we need to regenerate the download links
                    return await FetchUpdate(build, arch, ring, branch, flight, sku, false);
                }
                return x;
            }
            else
            {
                Console.WriteLine($"Build {build} {arch} {branch} {ring} not found, adding it to the cache");
                var data = await client.FetchBuildAndDownloadlinks("10.0." + build, WUArch.amd64, ring, branch, flight, sku);
                if (data != null)
                {
                    Cache.InsertBuild(data.UpdateID, data.Title, arch, build, sku, flight, ring);
                    Console.WriteLine($"Build {build} {arch} {branch} {ring} added!!");

                    BuildInfo bld = new BuildInfo() { Arch = arch, Build = build, Created = 0, Flight = flight, Ring = ring, Sku = sku, Title = data.Title, UpdateID = data.UpdateID };
                    foreach (var item in data.DownloadInfo)
                    {
                        bld.Files.Add(new CacheFileEntry() { name = item.Name, sha256 = item.SHA256Hash, size = item.Size, expires = DateTime.Now.AddHours(10), url = item.Url });
                    }
                    Cache.InsertBuildInfo(bld);
                    return bld;
                }
                else
                {
                    Console.WriteLine($"Build {build} {arch} {branch} {ring} not found on WU servers!");
                    return null;
                }
            }
        }
    }
}
