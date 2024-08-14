using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IERAX_MissionControl
{
    public class ShipStaticData
    {
        public int Type { get; set; } // Assuming Type is an int, adjust as needed
        public string Name { get; set; }
        public string CallSign { get; set; }
        public int ImoNumber { get; set; }
        // Add other relevant properties as needed
    }

    public class AisMessage
    {
        public PositionReport PositionReport { get; set; }
        public StandardClassBPositionReport StandardClassBPositionReport { get; set; }
        public ShipStaticData ShipStaticData { get; set; } // Add this line to include ShipStaticData
    }




    public class PositionReport
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int UserID { get; set; }
        public int NavigationalStatus { get; set; }
        public double RateOfTurn { get; set; }
        public double Sog { get; set; } // Speed Over Ground
        public double Cog { get; set; } // Course Over Ground (should be double)
        public int TrueHeading { get; set; }
        public int Timestamp { get; set; }
        public bool PositionAccuracy { get; set; }
        public bool Raim { get; set; }
        public int CommunicationState { get; set; }
    }

    public class StandardClassBPositionReport
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int UserID { get; set; }
        public double Cog { get; set; } // Course Over Ground (should be double)
        public double Sog { get; set; } // Speed Over Ground
        public int TrueHeading { get; set; }
        public int Timestamp { get; set; }
        public bool PositionAccuracy { get; set; }
        public bool Raim { get; set; }
        public bool ClassBUnit { get; set; }
        public bool ClassBDisplay { get; set; }
        public bool ClassBDsc { get; set; }
        public bool ClassBBand { get; set; }
        public bool ClassBMsg22 { get; set; }
        public bool AssignedMode { get; set; }
        public int CommunicationState { get; set; }
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
