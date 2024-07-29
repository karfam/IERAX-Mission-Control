using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IERAX_MissionControl
{
    public class AisMessage
    {
        public PositionReport PositionReport { get; set; }
        public StandardClassBPositionReport StandardClassBPositionReport { get; set; }
    }

    public class PositionReport
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int UserID { get; set; }
        public string ShipName { get; set; }
    }

    public class StandardClassBPositionReport
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int UserID { get; set; }
        public string ShipName { get; set; }
    }

    public class MetaData
    {
        public string MMSI { get; set; }
        public string ShipName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class AisData
    {
        public AisMessage Message { get; set; }
        public string MessageType { get; set; }
        public MetaData MetaData { get; set; }
    }

}
