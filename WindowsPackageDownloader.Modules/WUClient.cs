using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WindowsPackageDownloader.Core
{
    public class WUClient
    {
        private static Random random = new Random();
        private HttpClient client;
        private const string WUEndpoint = "https://fe3cr.delivery.mp.microsoft.com/ClientWebService/client.asmx";
        public WUClient()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Windows-Update-Agent/10.0.10011.16384 Client-Protocol/2.50");
            //   client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Windows-Update-Agent/10.0.10011.16384 Client-Protocol/2.50"));
            client.Timeout = new TimeSpan(0, 0, 15);
        }

        public async Task FetchUpdate(string arch, string ring, string flight, int build, int minor, int sku, string type = "Production")
        {
            arch=arch.ToLower();
            ring = ring.ToUpper();
            flight = char.ToUpper(flight[0]) + flight.Substring(1).ToLower();
            flight = "Active";
            

            if(!(arch == "amd64" || arch == "x86" || arch == "arm64" || arch=="arm" || arch == "all"))
            {
                throw new Exception("Invaild arch");
            }
            if(!(ring == "DEV" || ring == "BETA" || ring == "RELEASEPREVIEW" || ring=="WIF" ||ring=="WIS"||ring=="RP"||ring == "RETAIL" || ring == "MSIT"))
            {
                throw new Exception("Invaild ring: " + ring);
            }
            if(!(flight == "Mainline" || flight =="Active" || flight == "Skip"))
            {
                throw new Exception("Invaild flight: " + flight);
            }
            if(flight=="Skip" && ring != "WIF")
            {
                throw new Exception("Cannot have SKIP flight, and not WIF ring");
            }
            if (ring == "DEV") ring = "WIF";
            if (ring == "BETA") ring = "WIS";
            if (ring == "RELEASEPREVIEW") ring = "RP";

            if (flight == "Active" && ring == "RP") flight = "Current";

            var buildstr = $"10.0.{build}.{minor}";
            type = char.ToUpper(type[0]) + type.Substring(1).ToLower();
            if(!(type=="Production" || type == "Test"))
            {
                type = "Production";
            }
            var device = GenerateDeviceString();
            await EncryptData(device);
            var data = await CreateFetchUpdateRequest(device, arch, ring, flight, buildstr, sku, type);
            var outData = await SendWuPostRequest(data, device);
            ;
        }
        private bool IsSkuServer(int sku)
        {
            var serverSkus = new int[] { 7, 8, 12, 13, 79, 80, 120, 145, 146,
        147, 148, 159, 160, 406, 407, 408};

            if (serverSkus.Contains(sku))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private async Task<string> CreateFetchUpdateRequest(string device, string arch, string ring, string flight, string build, int sku, string type = "Production")
        {
            var uuid = GenerateUUID();
            var createdTime = DateTime.Now;
            var expireTime = DateTime.Now.AddSeconds(120);
            var cookieExpireTime = createdTime.AddSeconds(604800);

            var created = XmlConvert.ToString(createdTime);
            var expire = XmlConvert.ToString(expireTime);
            var cookieExpire = XmlConvert.ToString(cookieExpireTime);

            var branch = BranchFromBuild(build);
            var product = "Client.OS.rs2";
            if (IsSkuServer(sku))
            {
                product = "Server.OS";
            }

            //HubOS
            if (sku == 180)
            {
                product = "WCOSDevice2.OS";
            }
            //Andromeda
            else if (sku == 184)
            {
                product = "WCOSDevice1.OS";
            }
            else if (sku == 189)
            {
                product = "WCOSDevice0.OS";
            }

            string[] archlist;
            if (arch == "all")
            {
                archlist = new string[] { "amd64", "x86", "arm64", "arm" };
            }
            else
            {
                archlist = new string[] { arch };
            }
            List<string> products = new List<string>();
            foreach (var aarch in archlist)
            {
                products.Add($"PN={product}.{aarch}&Branch={branch}&PrimaryOSProduct=1&Repairable=1&V={build}&ReofferUpdate=1");
                products.Add($"PN=Adobe.Flash.{aarch}&Repairable=1&V=0.0.0.0");
                products.Add($"PN=Microsoft.Edge.Stable.{aarch}&Repairable=1&V=0.0.0.0");
                products.Add($"PN=Microsoft.NETFX.{aarch}&V=2018.12.2.0");
                products.Add($"PN=Windows.Appraiser.{aarch}&Repairable=1&V={build}");
                products.Add($"PN=Windows.AppraiserData.{aarch}&Repairable=1&V={build}");
                products.Add($"PN=Windows.EmergencyUpdate.{aarch}&V={build}");
                products.Add($"PN=Windows.FeatureExperiencePack.{aarch}&Repairable=1&V=0.0.0.0");
                products.Add($"PN=Windows.ManagementOOBE.{aarch}&IsWindowsManagementOOBE=1&Repairable=1&V={build}");
                products.Add($"PN=Windows.OOBE.{aarch}&IsWindowsOOBE=1&Repairable=1&V={build}");
                products.Add($"PN=Windows.UpdateStackPackage.{aarch}&Name=Update Stack Package&Repairable=1&V={build}");
                products.Add($"PN=Hammer.{aarch}&Source=UpdateOrchestrator&V=0.0.0.0");
                products.Add($"PN=MSRT.{aarch}&Source=UpdateOrchestrator&V=0.0.0.0");
                products.Add($"PN=SedimentPack.{aarch}&Source=UpdateOrchestrator&V=0.0.0.0");
                products.Add($"PN=UUS.{aarch}&Source=UpdateOrchestrator&V=0.0.0.0");
            }

            string[] callerAttrib = new string[] {  "Profile=AUv2",
        "Acquisition=1",
        "Interactive=1",
        "IsSeeker=1",
        "SheddingAware=1",
        "Id=MoUpdateOrchestrator" };
            var productsh = EscapeString(string.Join(";", products.ToArray()));
            var callerAttribh = "E:" + EscapeString( string.Join("&", callerAttrib));
            var syncCurrentStr = "false";
            // $syncCurrent = uupApiConfigIsTrue('fetch_sync_current_only');
            //$syncCurrentStr = $syncCurrent ? 'true' : 'false';
            var deviceatrrib = CreateDeviceAttributes(flight, ring, build, archlist, sku, type);

            return $@"<s:Envelope xmlns:a=""http://www.w3.org/2005/08/addressing"" xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
    <s:Header>
        <a:Action s:mustUnderstand=""1"">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncUpdates</a:Action>
        <a:MessageID>urn:uuid:{uuid}</a:MessageID>
        <a:To s:mustUnderstand=""1"">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx</a:To>
        <o:Security s:mustUnderstand=""1"" xmlns:o=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
            <Timestamp xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
                <Created>{created}</Created>
                <Expires>{expire}</Expires>
            </Timestamp>
            <wuws:WindowsUpdateTicketsToken wsu:id=""ClientMSA"" xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"" xmlns:wuws=""http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization"">
                <TicketType Name=""MSA"" Version=""1.0"" Policy=""MBI_SSL"">
                    <Device>{device}</Device>
                </TicketType>
            </wuws:WindowsUpdateTicketsToken>
        </o:Security>
    </s:Header>
    <s:Body>
        <SyncUpdates xmlns=""http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService"">
            <cookie>
                <Expiration>{cookieExpire}</Expiration>
                <EncryptedData>{await EncryptData(device)}</EncryptedData>
            </cookie>
            <parameters>
                <ExpressQuery>false</ExpressQuery>
                <InstalledNonLeafUpdateIDs>
                    <int>1</int>
                    <int>10</int>
                    <int>105939029</int>
                    <int>105995585</int>
                    <int>106017178</int>
                    <int>107825194</int>
                    <int>10809856</int>
                    <int>11</int>
                    <int>117765322</int>
                    <int>129905029</int>
                    <int>130040030</int>
                    <int>130040031</int>
                    <int>130040032</int>
                    <int>130040033</int>
                    <int>133399034</int>
                    <int>138372035</int>
                    <int>138372036</int>
                    <int>139536037</int>
                    <int>139536038</int>
                    <int>139536039</int>
                    <int>139536040</int>
                    <int>142045136</int>
                    <int>158941041</int>
                    <int>158941042</int>
                    <int>158941043</int>
                    <int>158941044</int>
                    <int>159776047</int>
                    <int>160733048</int>
                    <int>160733049</int>
                    <int>160733050</int>
                    <int>160733051</int>
                    <int>160733055</int>
                    <int>160733056</int>
                    <int>161870057</int>
                    <int>161870058</int>
                    <int>161870059</int>
                    <int>17</int>
                    <int>19</int>
                    <int>2</int>
                    <int>23110993</int>
                    <int>23110994</int>
                    <int>23110995</int>
                    <int>23110996</int>
                    <int>23110999</int>
                    <int>23111000</int>
                    <int>23111001</int>
                    <int>23111002</int>
                    <int>23111003</int>
                    <int>23111004</int>
                    <int>2359974</int>
                    <int>2359977</int>
                    <int>24513870</int>
                    <int>28880263</int>
                    <int>3</int>
                    <int>30077688</int>
                    <int>30486944</int>
                    <int>5143990</int>
                    <int>5169043</int>
                    <int>5169044</int>
                    <int>5169047</int>
                    <int>59830006</int>
                    <int>59830007</int>
                    <int>59830008</int>
                    <int>60484010</int>
                    <int>62450018</int>
                    <int>62450019</int>
                    <int>62450020</int>
                    <int>69801474</int>
                    <int>8788830</int>
                    <int>8806526</int>
                    <int>9125350</int>
                    <int>9154769</int>
                    <int>98959022</int>
                    <int>98959023</int>
                    <int>98959024</int>
                    <int>98959025</int>
                    <int>98959026</int>
                </InstalledNonLeafUpdateIDs>
                <OtherCachedUpdateIDs/>
                <SkipSoftwareSync>false</SkipSoftwareSync>
                <NeedTwoGroupOutOfScopeUpdates>true</NeedTwoGroupOutOfScopeUpdates>
                <AlsoPerformRegularSync>true</AlsoPerformRegularSync>
                <ComputerSpec/>
                <ExtendedUpdateInfoParameters>
                    <XmlUpdateFragmentTypes>
                        <XmlUpdateFragmentType>Extended</XmlUpdateFragmentType>
                        <XmlUpdateFragmentType>LocalizedProperties</XmlUpdateFragmentType>
                    </XmlUpdateFragmentTypes>
                    <Locales>
                        <string>en-US</string>
                    </Locales>
                </ExtendedUpdateInfoParameters>
                <ClientPreferredLanguages/>
                <ProductsParameters>
                    <SyncCurrentVersionOnly>{syncCurrentStr}</SyncCurrentVersionOnly>
                    <DeviceAttributes>{deviceatrrib}</DeviceAttributes>
                    <CallerAttributes>{callerAttribh}</CallerAttributes>
                    <Products>{productsh}</Products>
                </ProductsParameters>
            </parameters>
        </SyncUpdates>
    </s:Body>
</s:Envelope>";
        }

        private string CreateDeviceAttributes(string flight, string ring, string build, string[] archlist, int sku, string type)
        {
            var branch = BranchFromBuild(build);

            var blockUpdates = 0;
            var flightEnabled = 1;
            var isRetail = 0;

            var arch = archlist[0];
            if (sku == 125 || sku == 126)
            {
                blockUpdates = 1;
            }

            string dvcFamily = "Windows.Desktop";
            var insType = "Client";
            if (sku == 119)
            {
                dvcFamily = "Windows.Team";
            }
            if (IsSkuServer(sku))
            {
                dvcFamily = "Windows.Server";
                insType = "Server";
                blockUpdates = 1;
            }
            // HubOS Andromeda Lite
            if (sku == 180 || sku == 184 || sku == 189)
            {
                dvcFamily = "Windows.Core";
                insType = "FactoryOS";
            }

            var fltContent = "Mainline";
            var fltRing = "External";
            flight = "Active";
            string fltBranch = "";


            if (ring == "RETAIL")
            {
                fltBranch = "";
                fltContent = flight;
                fltRing = "Retail";
                flightEnabled = 0;
                isRetail = 1;
            }

            if (ring == "WIF")
            {
                fltBranch = "Dev";
            }

            if (ring == "WIS")
            {
                fltBranch = "Beta";
            }

            if (ring == "RP")
            {
                fltBranch = "ReleasePreview";
            }

            if (ring == "DEV")
            {
                fltBranch = "Dev";
                ring = "WIF";
            }

            if (ring == "BETA")
            {
                fltBranch = "Beta";
                ring = "WIS";
            }

            if (ring == "RELEASEPREVIEW")
            {
                fltBranch = "ReleasePreview";
                ring = "RP";
            }

            if (ring == "MSIT")
            {
                fltBranch = "MSIT";
                fltRing = "Internal";
            }

            var bldnums = build.Split(".");
            var bldnum = int.Parse(bldnums[2]);

            if (bldnum < 17763)
            {
                if (ring == "RP")
                {
                    flight = "current";

                }
                fltBranch = "external";
                fltContent = flight;
                fltRing = ring;
            }

            string[] attrib = new string[] {
                 "App=WU_OS",
        "AppVer="+build,
        "AttrDataVer=177",
        "AllowInPlaceUpgrade=1",
        "AllowUpgradesWithUnsupportedTPMOrCPU=1",
        "BlockFeatureUpdates="+blockUpdates,
        "BranchReadinessLevel=CB",
        "CurrentBranch="+branch,
        "DataExpDateEpoch_CO21H2="+ XmlConvert.ToString(DateTime.Now.AddSeconds(82800)),
        "DataExpDateEpoch_CO21H2Setup="+ XmlConvert.ToString(DateTime.Now.AddSeconds(82800)),
        "DataExpDateEpoch_21H2="+ XmlConvert.ToString(DateTime.Now.AddSeconds(82800)),
        "DataExpDateEpoch_21H1="+ XmlConvert.ToString(DateTime.Now.AddSeconds(82800)),
        "DataExpDateEpoch_20H1="+ XmlConvert.ToString(DateTime.Now.AddSeconds(82800)),
        "DataExpDateEpoch_19H1="+ XmlConvert.ToString(DateTime.Now.AddSeconds(82800)),
        "DataVer_RS5=2000000000",
        "DefaultUserRegion=191",
        "DeviceFamily="+dvcFamily,
        "EKB19H2InstallCount=1",
        "EKB19H2InstallTimeEpoch=1255000000",
        "FlightingBranchName="+fltBranch,
        //"FlightContent="+fltContent,
        "FlightRing="+fltRing,
        "Free=gt64",
        "GStatus_CO21H2=2",
        "GStatus_CO21H2Setup=2",
        "GStatus_21H2=2",
        "GStatus_21H1=2",
        "GStatus_20H1=2",
        "GStatus_20H1Setup=2",
        "GStatus_19H1=2",
        "GStatus_19H1Setup=2",
        "GStatus_RS5=2",
        "GenTelRunTimestamp_19H1="+ XmlConvert.ToString(DateTime.Now.AddSeconds(-3600)),
        "InstallDate=1438196400",
        "InstallLanguage=en-US",
        "InstallationType="+insType,
        "IsDeviceRetailDemo=0",
        "IsFlightingEnabled="+flightEnabled,
        "IsRetailOS="+isRetail,
        "MediaBranch=",
        "MediaVersion="+build,
        "CloudPBR=1",
        "DUScan=1",
        "OEMModel=Asus ROG Maximus Z690 Extreme",
        "OEMModelBaseBoard=ROG MAXIMUS Z690 EXTREME",
        "OEMName_Uncleaned=ASUSTeK COMPUTER INC.",
        "OSArchitecture="+arch,
        "OSSkuId="+sku,
        "OSUILocale=en-US",
        "OSVersion="+build,
        "ProcessorIdentifier=Intel64 Family 6 Model 151 Stepping 2",
        "ProcessorManufacturer=GenuineIntel",
        "ProcessorModel=12th Gen Intel(R) Core(TM) i9-12900K",
        "ReleaseType="+type,
        "SdbVer_20H1=2000000000",
        "SdbVer_19H1=2000000000",
        "SecureBootCapable=1",
        "TelemetryLevel=3",
        "TimestampEpochString_CO21H2="+ XmlConvert.ToString(DateTime.Now.AddSeconds(-3600)),
        "TimestampEpochString_CO21H2Setup="+ XmlConvert.ToString(DateTime.Now.AddSeconds(-3600)),
        "TimestampEpochString_21H2="+ XmlConvert.ToString(DateTime.Now.AddSeconds(-3600)),
        "TimestampEpochString_21H1=" + XmlConvert.ToString(DateTime.Now.AddSeconds(-3600)),
        "TimestampEpochString_20H1=" + XmlConvert.ToString(DateTime.Now.AddSeconds(-3600)),
        "TimestampEpochString_19H1=" + XmlConvert.ToString(DateTime.Now.AddSeconds(-3600)),
        "TPMVersion=2",
        "UpdateManagementGroup=2",
        "UpdateOfferedDays=0",
        "UpgEx_NI22H2=Green",
        "UpgEx_CO21H2=Green",
        "UpgEx_21H2=Green",
        "UpgEx_21H1=Green",
        "UpgEx_20H1=Green",
        "UpgEx_19H1=Green",
        "UpgEx_RS5=Green",
        "UpgradeEligible=1",
        "Version_RS5=2000000000",
        "WuClientVer=" + build
            };

            return "E:" + EscapeString(string.Join("&", attrib));
        }

        private string BranchFromBuild(string build)
        {
            var b = build.Split(".");
            var build2 = int.Parse(b[2]);
            string branch = "";

            switch (build2)
            {
                case 15063:
                    branch = "rs2_release";
                    break;

                case 16299:
                    branch = "rs3_release";
                    break;

                case 17134:
                    branch = "rs4_release";
                    break;

                case 17763:
                    branch = "rs5_release";
                    break;

                case 17784:
                    branch = "rs5_release_svc_hci";
                    break;

                case 18362:
                case 18363:
                    branch = "19h1_release";
                    break;

                case 19041:
                case 19042:
                case 19043:
                case 19044:
                case 19045:
                case 19046:
                    branch = "vb_release";
                    break;

                case 20279:
                    branch = "fe_release_10x";
                    break;

                case 20348:
                case 20349:
                case 20350:
                    branch = "fe_release";
                    break;

                case 22000:
                    branch = "co_release";
                    break;

                case 22621:
                    branch = "ni_release";
                    break;

                default:
                    branch = "rs_prerelease";
                    break;
            }
            return branch;
        }

        private string GenerateDeviceString()
        {
            var header = "13003002c377040014d5bcac7a66de0d50beddf9bba16c87edb9e019898000";
            var random = RandomString(1054);
            var end = "b401";

            var value = header + random + end;
            var data = $"t={Convert.ToBase64String(Encoding.Unicode.GetBytes(value))}&p=";
            return Convert.ToBase64String(Encoding.Unicode.GetBytes(string.Join("", data.Split("\0"))));

        }
        private string CreateGetCookieRequest(string device)
        {
            var uuid = GenerateUUID();
            var createdtime = DateTime.Now;
            var expiretime = createdtime.AddSeconds(120);

            var created = XmlConvert.ToString(createdtime);
            var expire = XmlConvert.ToString(expiretime);


            return $@"<s:Envelope xmlns:a=""http://www.w3.org/2005/08/addressing"" xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
    <s:Header>
        <a:Action s:mustUnderstand=""1"">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookie</a:Action>
        <a:MessageID>urn:uuid:{uuid}</a:MessageID>
        <a:To s:mustUnderstand=""1"">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx</a:To>
        <o:Security s:mustUnderstand=""1"" xmlns:o=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
            <Timestamp xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
                <Created>{created}</Created>
                <Expires>{expire}</Expires>
            </Timestamp>
            <wuws:WindowsUpdateTicketsToken wsu:id=""ClientMSA"" xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"" xmlns:wuws=""http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization"">
                <TicketType Name=""MSA"" Version=""1.0"" Policy=""MBI_SSL"">
                    <Device>{device}</Device>
                </TicketType>
            </wuws:WindowsUpdateTicketsToken>
        </o:Security>
    </s:Header>
    <s:Body>
        <GetCookie xmlns=""http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService"">
            <oldCookie>
                <Expiration>{created}</Expiration>
            </oldCookie>
            <lastChange>{created}</lastChange>
            <currentTime>{created}</currentTime>
            <protocolVersion>2.0</protocolVersion>
        </GetCookie>
    </s:Body>
</s:Envelope>";
        }
        private string GenerateUUID()
        {
            string result = "";
            result += random.Next(0, 0xffff).ToString("x4");
            result += random.Next(0, 0xffff).ToString("x4") + "-";

            result += random.Next(0, 0xffff).ToString("x4") + "-";

            result += (random.Next(0, 0xffff) | 0x4000).ToString("x4") + "-";
            result += (random.Next(0, 0x3fff) | 0x8000).ToString("x4") + "-";
            result += random.Next(0, 0xffff).ToString("x4");
            result += random.Next(0, 0xffff).ToString("x4");
            result += random.Next(0, 0xffff).ToString("x4");
            return result;
        }
        private async Task<string> EncryptData(string device)
        {
            if (!File.Exists("wutok.json"))
            {
                var req = CreateGetCookieRequest(device);
                await SendWuPostRequest(req, device);
                return await EncryptData(device);
            }
            else
            {
                dynamic x = JsonConvert.DeserializeObject(File.ReadAllText("wutok.json"));
                return x.data;
            }
        }
        private async Task<XmlDocument> SendWuPostRequest(string req, string device)
        {
            var content = new StringContent(req);
            content.Headers.ContentType.MediaType = "application/soap+xml";

            var response = await client.PostAsync(WUEndpoint, content);
            var responseText = await response.Content.ReadAsStringAsync();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(responseText);
            Console.WriteLine(responseText);
            if (response.StatusCode == HttpStatusCode.InternalServerError && responseText.Contains("CookieExpired"))
            {
                File.Delete("wutok.json");
                var x= await EncryptData(device);
                return await SendWuPostRequest(req, device);
            }
            try
            {
                var body = doc["s:Envelope"]["s:Body"];
                var cookieResponse = body["GetCookieResponse"];
                if (cookieResponse != null)
                {
                    var r = cookieResponse["GetCookieResult"];
                    if (r != null)
                    {
                        dynamic j = new JObject();
                        j.expirationDate = r["Expiration"].InnerText;
                        j.data = r["EncryptedData"].InnerText;
                        File.WriteAllText("wutok.json", (string)j.ToString());
                    }
                }
            }
            catch
            {

            }
            return doc;
        }
        private static string EscapeString(string str)
        {
            return Uri.EscapeDataString(str);//str.Replace("&", "&amp;");
        }
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
