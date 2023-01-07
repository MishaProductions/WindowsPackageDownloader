using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WindowsPackageDownloader.Core.Products
{
    public class DesktopProduct : Product
    {
        public DesktopProduct(WUArch arch, string build, string branch, string flight, string ring, string productstr) : base(arch, build, branch, flight, ring, productstr, "48")
        {
        }

        public override string GetDeviceAttributes()
        {
            var attributes = new string[]
            {
                $"AppVer={Build}",
                $"AttrDataVer=98",
                $"ReleaseType=Production",
                $"BranchReadinessLevel=CB",
                $"CurrentBranch={Branch}",
                $"FlightingBranchName={Ring}",
                $"FlightContent={Flight}",
                $"FlightRing=External",
                $"IsFlightingEnabled={(Ring.ToUpper() == "RETAIL" ? "0" : "1")}",
                $"IsRetailOS={(Ring.ToUpper() == "RETAIL" ? "1" : "0")}",
                $"OSSkuId={Sku}",
                $"OSVersion={Build}",
                $"ProcessorManufacturer=GenuineIntel",
                $"TPMVersion=2",
                $"UpgEx_20H1=Green",
                $"UpgEx_21H1=Green",
                $"UpgEx_22H1=Green",
                $"DataExpDateEpoch_20H1=0",
                $"GStatus_20H1=2"
            };

            return string.Join(";", attributes);
        }

        public override string GetProductString()
        {
            var productsArray = new string[]
           {
                $"PN=Client.OS.rs2.{Arch}&amp;Branch={Branch}&amp;V={Build}",
           };

            return string.Join(';', productsArray);
        }
    }
}
