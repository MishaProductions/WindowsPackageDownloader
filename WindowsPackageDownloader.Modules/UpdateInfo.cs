using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.Collections;
using System.Diagnostics;

namespace WindowsPackageDownloader.Core
{
    public sealed class UpdateInfo
    {
        public static UpdateInfo? Parse(Envelope cont)
        {
            UpdateInfo info = new UpdateInfo();
           
            return info;
        }
       
        public static UpdateIdentity ExtractUpdateIdentity(Envelope cont)
        {
            if (!(cont.Body.SyncUpdatesResponse?.SyncUpdatesResult?.NewUpdates?.UpdateInfo != null)) return null;

            UpdateIdentity srlzd = null;

            foreach (var update in cont.Body.SyncUpdatesResponse.SyncUpdatesResult.NewUpdates.UpdateInfo)
            {
                if (update.Deployment.Action == "Install")
                {
                    var serIden = new XmlSerializer(typeof(UpdateIdentity));
                    using var strReader = new StringReader(update.Xml);
                    using var reader = XmlReader.Create(strReader, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });

                    srlzd = (UpdateIdentity)serIden.Deserialize(reader);
                    srlzd.LastChangeTime = update.Deployment.LastChangeTime;
                    srlzd.ID = update.ID;
                    srlzd.FlightID = update.Deployment.FlightId;
                    break;
                }
            }

            if (srlzd != null)
            {
                foreach (var extUpdate in cont.Body.SyncUpdatesResponse.SyncUpdatesResult.ExtendedUpdateInfo.Updates.Update)
                {
                    if (extUpdate.ID == srlzd.ID && extUpdate.Xml.StartsWith("<LocalizedProperties"))
                    {
                        srlzd.LocalizedProperties = DeserializeLocalizedProperties(extUpdate.Xml);
                        break;
                    }
                }
            }

            return srlzd;
        }
        [XmlRoot(ElementName = "UpdateIdentity")]
        public class UpdateIdentity
        {
            [XmlAttribute(AttributeName = "UpdateID")]
            public string UpdateID { get; set; }
            [XmlAttribute(AttributeName = "RevisionNumber")]
            public string RevisionNumber { get; set; }

            public string ID { get; set; }
            public string LastChangeTime { get; set; }
            public string FlightID { get; set; }

            public LocalizedProperties LocalizedProperties { get; set; }

        }
        private static LocalizedProperties DeserializeLocalizedProperties(string content)
        {
            var serFiles = new XmlSerializer(typeof(LocalizedProperties));
            using var strReader = new StringReader(content);
            using XmlReader reader = XmlReader.Create(strReader, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
            return (LocalizedProperties)serFiles.Deserialize(reader);
        }
    }
}
