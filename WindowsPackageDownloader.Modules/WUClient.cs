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
    /// <summary>
    /// Windows Update Client
    /// </summary>
    public partial class WUClient
    {
     
        private readonly HttpClient client;

        public WUClient()
        {
            //setup the http client
            client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Windows-Update-Agent/10.0.10011.16384 Client-Protocol/2.50");
        }
    }
}
