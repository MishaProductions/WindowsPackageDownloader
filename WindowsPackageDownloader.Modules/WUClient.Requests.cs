using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WindowsPackageDownloader.Core.Products;

namespace WindowsPackageDownloader.Core
{
    public partial class WUClient
    {
        private static readonly Random random = new Random();
        private const string WUEndpoint = "https://fe3cr.delivery.mp.microsoft.com/ClientWebService/client.asmx";
        private const string WUEndpointSecured = "https://fe3cr.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured";
        private async Task<string> GetToken()
        {
            if (!File.Exists("wutok.json"))
            {
                var req = CreateGetCookieRequest();
                var x = await SendRequest(req);
                if (!x.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to get cookie");
                }
                var responseText = await x.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseText);
                Console.WriteLine(responseText);
                var body = doc["s:Envelope"]["s:Body"];
                var cookieResponse = body["GetCookieResponse"];
                string token = "";
                if (cookieResponse != null)
                {
                    var r = cookieResponse["GetCookieResult"];
                    if (r != null)
                    {
                        dynamic j = new JObject();
                        j.expirationDate = r["Expiration"].InnerText;
                        j.data = token = r["EncryptedData"].InnerText;
                        File.WriteAllText("wutok.json", (string)j.ToString());
                    }
                }
                if (token == "")
                {
                    throw new Exception("failed to get cookie");
                }
                return token;
            }
            else
            {
                dynamic x = JsonConvert.DeserializeObject(File.ReadAllText("wutok.json"));
                return x.data;
            }
        }
        internal async Task<HttpResponseMessage> SendRequest(string request)
        {
            //Create the request
            var content = new StringContent(request);
            if (content.Headers.ContentType != null)
                content.Headers.ContentType.MediaType = "application/soap+xml";
            else
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soup+xml");
            var r = await client.PostAsync(WUEndpoint, content);

            return r;
        }

        protected string FetchBuildInfo(string guid, string uupEncryptedData, string created, string expires, string callerAttrib, string deviceAttributes, string products, bool syncCurrentVersion = false)
        {
            return $"<s:Envelope xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Header><a:Action s:mustUnderstand=\"1\">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncUpdates</a:Action><a:MessageID>urn:uuid:{guid}</a:MessageID><a:To s:mustUnderstand=\"1\">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx</a:To><o:Security s:mustUnderstand=\"1\" xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\"><Timestamp xmlns=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\"><Created>{created}</Created><Expires>{expires}</Expires></Timestamp><wuws:WindowsUpdateTicketsToken wsu:id=\"ClientMSA\" xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns:wuws=\"http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization\"><TicketType Name=\"MSA\" Version=\"1.0\" Policy=\"MBI_SSL\"></TicketType></wuws:WindowsUpdateTicketsToken></o:Security></s:Header><s:Body><SyncUpdates xmlns=\"http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService\"><cookie><Expiration>2046-02-18T21:29:10Z</Expiration><EncryptedData>{uupEncryptedData}</EncryptedData></cookie><parameters><ExpressQuery>false</ExpressQuery><InstalledNonLeafUpdateIDs><int>1</int><int>2</int><int>3</int><int>10</int><int>11</int><int>17</int><int>19</int><int>2359974</int><int>2359977</int><int>5143990</int><int>5169043</int><int>5169044</int><int>5169047</int><int>8788830</int><int>8806526</int><int>9125350</int><int>9154769</int><int>10809856</int><int>23110993</int><int>23110994</int><int>23110995</int><int>23110996</int><int>23110999</int><int>23111000</int><int>23111001</int><int>23111002</int><int>23111003</int><int>23111004</int><int>24513870</int><int>28880263</int><int>30077688</int><int>30486944</int><int>59830006</int><int>59830007</int><int>59830008</int><int>60484010</int><int>62450018</int><int>62450019</int><int>62450020</int><int>98959022</int><int>98959023</int><int>98959024</int><int>98959025</int><int>98959026</int><int>105939029</int><int>105995585</int><int>106017178</int><int>107825194</int><int>117765322</int><int>129905029</int><int>130040030</int><int>130040031</int><int>130040032</int><int>130040033</int><int>133399034</int><int>138372035</int><int>138372036</int><int>139536037</int><int>139536038</int><int>139536039</int><int>139536040</int><int>142045136</int><int>158941041</int><int>158941042</int><int>158941043</int><int>158941044</int><int>159776047</int><int>160733048</int><int>160733049</int><int>160733050</int><int>160733051</int><int>160733055</int><int>160733056</int><int>161870057</int><int>161870058</int><int>161870059</int></InstalledNonLeafUpdateIDs><OtherCachedUpdateIDs></OtherCachedUpdateIDs><SkipSoftwareSync>false</SkipSoftwareSync><NeedTwoGroupOutOfScopeUpdates>true</NeedTwoGroupOutOfScopeUpdates><AlsoPerformRegularSync>true</AlsoPerformRegularSync><ComputerSpec/><ExtendedUpdateInfoParameters><XmlUpdateFragmentTypes><XmlUpdateFragmentType>Extended</XmlUpdateFragmentType><XmlUpdateFragmentType>LocalizedProperties</XmlUpdateFragmentType><XmlUpdateFragmentType>Eula</XmlUpdateFragmentType></XmlUpdateFragmentTypes><Locales><string>en-US</string></Locales></ExtendedUpdateInfoParameters><ClientPreferredLanguages></ClientPreferredLanguages><ProductsParameters><SyncCurrentVersionOnly>{syncCurrentVersion.ToString().ToLower()}</SyncCurrentVersionOnly><DeviceAttributes>{deviceAttributes}</DeviceAttributes><CallerAttributes>{callerAttrib}</CallerAttributes><Products>{products}</Products></ProductsParameters></parameters></SyncUpdates></s:Body></s:Envelope>";
        }
        protected string CreateDownloadUrl(string guid, string uupDevice, string created, string expires, string uuid, string rev, string deviceAttributes)
        {
            return $"<s:Envelope xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Header><a:Action s:mustUnderstand=\"1\">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtendedUpdateInfo2</a:Action><a:MessageID>urn:uuid:{guid}</a:MessageID><a:To s:mustUnderstand=\"1\">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured</a:To><o:Security s:mustUnderstand=\"1\" xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\"><Timestamp xmlns=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\"><Created>{created}</Created><Expires>{expires}</Expires></Timestamp><wuws:WindowsUpdateTicketsToken wsu:id=\"ClientMSA\" xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns:wuws=\"http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization\"><TicketType Name=\"MSA\" Version=\"1.0\" Policy=\"MBI_SSL\"><Device>{uupDevice}</Device></TicketType></wuws:WindowsUpdateTicketsToken></o:Security></s:Header><s:Body><GetExtendedUpdateInfo2 xmlns=\"http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService\"><updateIDs><UpdateIdentity><UpdateID>{uuid}</UpdateID><RevisionNumber>{rev}</RevisionNumber></UpdateIdentity></updateIDs><infoTypes><XmlUpdateFragmentType>FileUrl</XmlUpdateFragmentType><XmlUpdateFragmentType>FileDecryption</XmlUpdateFragmentType><XmlUpdateFragmentType>EsrpDecryptionInformation</XmlUpdateFragmentType><XmlUpdateFragmentType>PiecesHashUrl</XmlUpdateFragmentType><XmlUpdateFragmentType>BlockMapUrl</XmlUpdateFragmentType></infoTypes><deviceAttributes>{deviceAttributes}</deviceAttributes></GetExtendedUpdateInfo2></s:Body></s:Envelope>";
        }
        private string CreateGetCookieRequest()
        {
            return $"<s:Envelope xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Header><a:Action s:mustUnderstand=\"1\">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookie</a:Action><a:To s:mustUnderstand=\"1\">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx</a:To><o:Security s:mustUnderstand=\"1\" xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\"><wuws:WindowsUpdateTicketsToken wsu:id=\"ClientMSA\" xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns:wuws=\"http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization\"><TicketType Name=\"MSA\" Version=\"1.0\" Policy=\"MBI_SSL\"></TicketType></wuws:WindowsUpdateTicketsToken></o:Security></s:Header><s:Body><GetCookie xmlns=\"http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService\"><protocolVersion>2.0</protocolVersion></GetCookie></s:Body></s:Envelope>"; ;
        }

        public async Task GetUpdateInfo(string build, WUArch arch, string ring, string branch, string flight)
        {
            var info = new DesktopProduct(arch, build, branch, flight, ring, "Client.OS.rs2");

            string token = await GetToken();
            var time = DateTime.Now;
            var expire = time.AddSeconds(120);

            var callAtribs = info.GetCallerAttributes();
            var deviceAttribs = info.GetDeviceAttributes();
            var product = info.GetProductString();
            var xml = FetchBuildInfo(Guid.NewGuid().ToString(), token,
                XmlConvert.ToString(time, XmlDateTimeSerializationMode.Local),
                XmlConvert.ToString(expire, XmlDateTimeSerializationMode.Local),
               callAtribs,
                deviceAttribs,
                product);

            var req = await SendRequest(xml);
            var content = await req.Content.ReadAsStringAsync();
            ;
        }
    }
}
