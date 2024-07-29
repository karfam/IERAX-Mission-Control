using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IERAX_MissionControl
{
    public static class GeoUtils
    {
        private const double EarthRadiusNm = 3440.065; // Earth's radius in nautical miles

        public static (double MinLat, double MaxLat, double MinLon, double MaxLon) CalculateBoundingBox(double lat, double lon, double radiusNm)
        {
            double latRadians = DegreeToRadian(lat);
            double lonRadians = DegreeToRadian(lon);
            double radiusRadians = radiusNm / EarthRadiusNm;

            double minLat = lat - RadianToDegree(radiusRadians);
            double maxLat = lat + RadianToDegree(radiusRadians);

            double minLon = lon - RadianToDegree(radiusRadians / Math.Cos(latRadians));
            double maxLon = lon + RadianToDegree(radiusRadians / Math.Cos(latRadians));

            return (minLat, maxLat, minLon, maxLon);
        }

        public static string FormatBoundingBox(double minLat, double maxLat, double minLon, double maxLon)
        {
            return $"[[{minLat.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}, {minLon.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}], [{maxLat.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}, {maxLon.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}]]";
        }

        private static double DegreeToRadian(double degree)
        {
            return degree * Math.PI / 180.0;
        }

        private static double RadianToDegree(double radian)
        {
            return radian * 180.0 / Math.PI;
        }
    }

}

