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

    public class ShipMarker : GMapMarker
    {
        private readonly Bitmap shipIcon;
        public string ShipName { get; set; }
        public string MMSI { get; set; }

        public ShipMarker(PointLatLng p) : base(p)
        {
            // Load and resize the ship icon
            Bitmap originalIcon = new Bitmap(Resources.container); // Assuming the image is named ShipMarker in Resources
            shipIcon = ResizeBitmap(originalIcon, 50, 50); // Adjust the size as needed
        }

        public override void OnRender(Graphics g)
        {
            if (shipIcon != null)
            {
                g.DrawImage(shipIcon, new PointF(LocalPosition.X - shipIcon.Width / 2, LocalPosition.Y - shipIcon.Height / 2));
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

