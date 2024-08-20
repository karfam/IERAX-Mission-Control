using GMap.NET;
using IERAX_MissionControl.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IERAX_MissionControl
{
    public class InterceptMarker : GMap.NET.WindowsForms.GMapMarker
    {
        private Bitmap markerIcon;

        public InterceptMarker(PointLatLng p) : base(p)
        {
            // Load and resize the icon
            markerIcon = new Bitmap(Resources.triangle_outline_256); // Use your custom icon here
            markerIcon = ResizeBitmap(markerIcon, 50, 50); // Adjust the size as needed
        
        }

        public override void OnRender(Graphics g)
        {
            if (markerIcon != null)
            {
                g.DrawImage(markerIcon, new PointF(LocalPosition.X - markerIcon.Width / 2, LocalPosition.Y - markerIcon.Height / 2));
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
