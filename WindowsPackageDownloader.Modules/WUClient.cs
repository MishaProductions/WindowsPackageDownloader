using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
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
        }

        public async Task FetchVersionInfo()
        {
            await EncryptData();
        }
        private string GenerateDeviceString()
        {
            var header = "13003002c377040014d5bcac7a66de0d50beddf9bba16c87edb9e019898000";
            var random = RandomString(1054);
            var end = "b401";

            var value = header + random + end;
            var data = $"t={value}&p=";
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Join("", data.Split("\0"))));

        }
        private string CreateGetCookieRequest(string device)
        {
            var uuid = GenerateUUID();
            var createdtime = DateTime.Now;
            var expiretime = createdtime.AddSeconds(120);

            var created= XmlConvert.ToString(createdtime);
            var expire = XmlConvert.ToString(expiretime);


            return $@"<s:Envelope xmlns:a=""http://www.w3.org/2005/08/addressing"" xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
    <s:Header>
        <a:Action s:mustUnderstand=""1"">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookie</a:Action>
        <a:MessageID>urn:uuid:$uuid</a:MessageID>
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
            result += random.Next(0, 0xffff);
            result += random.Next(0, 0xffff);   

            result += random.Next(0, 0xffff) + "-";

            result += (random.Next(0, 0xffff)| 0x4000) + "-";
            result += (random.Next(0, 0x3fff) | 0x8000) + "-";
            result += random.Next(0, 0xffff);
            result += random.Next(0, 0xffff);
            result += random.Next(0, 0xffff);
            return result;
        }
        private async Task<string> EncryptData()
        {
            if (!File.Exists("wutok.json"))
            {
                var req = CreateGetCookieRequest(GenerateDeviceString());
                sendWuPostRequest(req);
                return await EncryptData();
            }
            else
            {
                return File.ReadAllText("wutok.json");
            }
        }

        private async void sendWuPostRequest(string req)
        {
            var content = new StringContent(req);
            content.Headers.ContentType.MediaType = "application/soap+xml";
       
            var response = await client.PostAsync(WUEndpoint, content);
            var responseText = response.Content.ReadAsStringAsync();
            ;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
