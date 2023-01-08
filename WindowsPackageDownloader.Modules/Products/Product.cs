using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WindowsPackageDownloader.Core.Products
{
    public abstract class Product
    {
        /// <summary>
        /// The branch
        /// </summary>
        public string Branch { get; set; } = "";
        /// <summary>
        /// Build string such as 10.0.19045.2364
        /// </summary>
        public string Build { get; set; } = "";
        public WUArch Arch { get; set; }
        public string Flight { get; set; } = "";
        public string Ring { get; set; } = "";
        public string Sku { get; set; } = "";
        /// <summary>
        /// Product string such as Client.OS.rs2. Used internally
        /// </summary>
        public string ProductString { get; set; } = "";
        public Product(WUArch arch, string build, string branch, string flight, string ring, string productstr, string sku)
        {
            Arch = arch;
            Build = build ?? throw new ArgumentNullException(nameof(build));
            Branch = branch ?? throw new ArgumentNullException(nameof(branch));
            Flight = flight ?? throw new ArgumentNullException(nameof(flight));
            Ring = ring ?? throw new ArgumentNullException(nameof(ring));
            ProductString = productstr ?? throw new ArgumentNullException(nameof(productstr));
            Sku = sku ?? throw new ArgumentNullException(nameof(sku));  
        }

        /// <summary>
        /// Returns the caller attributes to the windows update client
        /// </summary>
        /// <returns></returns>
        public virtual string GetCallerAttributes()
        {
            var attribs = new string[]
            {
                "Id=UpdateOrchestrator",
                "SheddingAware=1",
                "Interactive=1",
                "IsSeeker=1"
            };

            return string.Join(";", attribs);
        }

        public abstract string GetDeviceAttributes();
        public abstract string GetProductString();
        protected string CreateDownloadUrl(string guid, string uupDevice, string created, string expires, string uuid, string rev, string deviceAttributes)
        {
            return $"<s:Envelope xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Header><a:Action s:mustUnderstand=\"1\">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtendedUpdateInfo2</a:Action><a:MessageID>urn:uuid:{guid}</a:MessageID><a:To s:mustUnderstand=\"1\">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured</a:To><o:Security s:mustUnderstand=\"1\" xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\"><Timestamp xmlns=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\"><Created>{created}</Created><Expires>{expires}</Expires></Timestamp><wuws:WindowsUpdateTicketsToken wsu:id=\"ClientMSA\" xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns:wuws=\"http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization\"><TicketType Name=\"MSA\" Version=\"1.0\" Policy=\"MBI_SSL\"><Device>{uupDevice}</Device></TicketType></wuws:WindowsUpdateTicketsToken></o:Security></s:Header><s:Body><GetExtendedUpdateInfo2 xmlns=\"http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService\"><updateIDs><UpdateIdentity><UpdateID>{uuid}</UpdateID><RevisionNumber>{rev}</RevisionNumber></UpdateIdentity></updateIDs><infoTypes><XmlUpdateFragmentType>FileUrl</XmlUpdateFragmentType><XmlUpdateFragmentType>FileDecryption</XmlUpdateFragmentType><XmlUpdateFragmentType>EsrpDecryptionInformation</XmlUpdateFragmentType><XmlUpdateFragmentType>PiecesHashUrl</XmlUpdateFragmentType><XmlUpdateFragmentType>BlockMapUrl</XmlUpdateFragmentType></infoTypes><deviceAttributes>{deviceAttributes}</deviceAttributes></GetExtendedUpdateInfo2></s:Body></s:Envelope>";
        }
        public virtual string BuildFileGetRequest(string uuid, string rev, string uupEncryptedData)
        {
            var guid = Guid.NewGuid().ToString();
            var created = XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Local);
            var expires = XmlConvert.ToString(DateTime.Now + TimeSpan.FromSeconds(120), XmlDateTimeSerializationMode.Local);

            var deviceAttributes = GetDeviceAttributes();
            return CreateDownloadUrl(guid, null, created, expires, uuid, rev, deviceAttributes);
        }
    }
}
