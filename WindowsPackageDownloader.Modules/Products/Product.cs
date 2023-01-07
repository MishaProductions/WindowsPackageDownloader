using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

 
    }
}
