using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using WindowsPackageDownloader.Core;

namespace WindowsPackageDownloader
{
    /// <summary>
    /// This class uses json files to store build info. The format of the json files is similar to UUPDump
    /// </summary>
    public static class Cache
    {
        private static object SyncRoot = new object();
        public static string CacheDir { get { return AppContext.BaseDirectory + "/Cache/"; } }
        public static CacheDB Database;
        public static void Init()
        {
            Directory.CreateDirectory(CacheDir);
            Directory.CreateDirectory(CacheDir + "/full/");
            if (!File.Exists(CacheDir + "cache.json"))
            {
                File.WriteAllText(CacheDir + "cache.json", "{\"version\": 1, \"database\": {}}");

            }

            var file = File.ReadAllText(CacheDir + "cache.json");
           
            Database = JsonConvert.DeserializeObject<CacheDB>(file);
         

            SaveDB();
        }

        public static void SaveDB()
        {
            lock (SyncRoot)
            {
               
                File.WriteAllText(CacheDir + "cache.json", JsonConvert.SerializeObject(Database));
            }
        }
        public static CacheEntry? GetEntry(string updateid)
        {
            foreach (var item in Database.database)
            {
                if(item.Value.UpdateID == updateid)
                {
                    return item.Value;
                }
            }
            return null;
        }
        public static CacheEntry? GetEntry(string build, string ring, WUArch arch, string sku)
        {
            foreach (var item in Database.database)
            {
                if (item.Value.Build == build && item.Value.Ring == ring && item.Value.arch == arch && item.Value.Sku == sku)
                {
                    return item.Value;
                }
            }
            return null;
        }

        public static void InsertBuild(string id, string title, WUArch arch, string build, string sku, string flight, string ring)
        {
            var entry = new CacheEntry() { UpdateID = id, Title = title, arch = arch, Build = build, Sku = sku, Flight = flight, Ring = ring, Created = DateTime.Now.Ticks };
            if (Database.database.ContainsKey(id))
            {
                entry.Created = Database.database[id].Created;
                Database.database[id] = entry;
            }
            else
            {
                Database.database.Add(id, entry);
            }
            SaveDB();
        }

        public static void InsertBuildInfo(BuildInfo bld)
        {
            string file = CacheDir + "/full/" + bld.UpdateID + ".json";
            var json = JsonConvert.SerializeObject(bld);
            File.WriteAllText(file, json);
        }
        public static BuildInfo? GetFullBuildInfo(string id)
        {
            string file = CacheDir + "/full/" + id + ".json";
            if (File.Exists(file))
            {
                BuildInfo? obj = JsonConvert.DeserializeObject<BuildInfo>(File.ReadAllText(file));
                if(obj == null)
                {
                    return null;
                }

                return obj;
            }
            else
            {
                return null;
            }
        }

     
    }
    [DataContract]

    public class CacheEntry
    {
        [DataMember]
        public string UpdateID;
        [DataMember]
        public string Title;
        [DataMember]
        public WUArch arch;
        [DataMember]
        public long Created;
        [DataMember]
        public string Build;
        [DataMember]
        public string Ring;
        [DataMember]
        public string Flight;
        [DataMember]
        public string Sku;
    }
    [DataContract]
    public class CacheDB
    {
        [DataMember]
        public int version { get; set; }
        [DataMember]
        public Dictionary<string, CacheEntry> database = new Dictionary<string, CacheEntry>();
    }
    [DataContract]
    public class CacheFileEntry
    {
        [DataMember]
        public string name { get; set; } = "";
        [DataMember]
        public long size { get; set; } = 0;
        [DataMember]
        public string sha256 { get; set; } = "";
        [DataMember]
        public string url { get; set; } = "";
        [DataMember]
        public DateTime expires { get; set; }
    }

    [DataContract]
    public class BuildInfo
    {
        [DataMember]
        public string Title { get; set; } = "";
        [DataMember]
        public string Ring { get; set; } = "";
        [DataMember]
        public string Flight { get; set; } = "";
        [DataMember]
        public WUArch Arch { get; set; }
        [DataMember]
        public string Build { get; set; } = "";
        [DataMember]
        public string CheckBuild { get; set; } = "";
        [DataMember]
        public string Sku { get; set; }
        [DataMember]
        public ulong Created { get; set; }
        [DataMember]
        public string UpdateID;
        [DataMember]
        public List<CacheFileEntry> Files = new List<CacheFileEntry>();
    }
}