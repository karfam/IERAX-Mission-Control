using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IERAX_MissionControl
{
    public class MAVLinkData
    {
        public int SysId { get; set; }
        public int CompId { get; set; }
        public string ParamName { get; set; }
        public float ParamValue { get; set; }
        public DateTime Timestamp { get; set; }

   
        public MAVLinkData(int sysId, int compId, string paramName, float paramValue)
        {
            SysId = sysId;
            CompId = compId;
            ParamName = paramName;
            ParamValue = paramValue;
            Timestamp = DateTime.UtcNow;
        }
    }
}
