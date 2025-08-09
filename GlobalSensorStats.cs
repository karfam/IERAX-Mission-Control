using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IERAX_MissionControl
{
    public static class GlobalSensorStats
    {
        
        public static double CO2Instant { get; set; } = 0;
        public static double MaxCO2 { get; set; } = 0;
        public static string MaxCO2Timestamp { get; set; } = "";
        public static double HDCO2Instant { get; set; } = 0;
        public static double MaxHDCO2 { get; set; } = 0;
        public static string MaxHDCO2Timestamp { get; set; } = "";
    }
}
