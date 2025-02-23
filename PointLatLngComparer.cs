using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMap.NET;

namespace IERAX_MissionControl
{
    public class PointLatLngComparer : IEqualityComparer<PointLatLng>
    {
        public bool Equals(PointLatLng p1, PointLatLng p2)
        {
            if (p1 == null && p2 == null)
                return true;
            if (p1 == null || p2 == null)
                return false;
            return Math.Abs(p1.Lat - p2.Lat) < 1e-6 && Math.Abs(p1.Lng - p2.Lng) < 1e-6;
        }

        public int GetHashCode(PointLatLng p)
        {
            // Combine hash codes for the two coordinates.
            return p.Lat.GetHashCode() ^ p.Lng.GetHashCode();
        }
    }
}
