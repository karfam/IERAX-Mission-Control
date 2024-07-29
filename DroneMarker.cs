using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using IERAX_MissionControl.Properties;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace IERAX_MissionControl
{
    using GMap.NET;
    using GMap.NET.WindowsForms;
    using System.Drawing;

    public class DroneMarker : GMapMarker
    {
        private readonly Bitmap droneIcon;

        public DroneMarker(PointLatLng p) : base(p)
        {
            // Load and resize the drone icon
            Bitmap originalIcon = new Bitmap(Resources.droneMarker);
            droneIcon = ResizeBitmap(originalIcon, 50, 50); // Adjust the size as needed
        }

        public override void OnRender(Graphics g)
        {
            if (droneIcon != null)
            {
                g.DrawImage(droneIcon, new PointF(LocalPosition.X - droneIcon.Width / 2, LocalPosition.Y - droneIcon.Height / 2));
            }
        }

        private Bitmap ResizeBitmap(Bitmap originalBitmap, int width, int height)
        {
            Bitmap resizedBitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resizedBitmap))
            {
                g.DrawImage(originalBitmap, 0, 0, width, height);
            }
            return resizedBitmap;
        }
    }
}
