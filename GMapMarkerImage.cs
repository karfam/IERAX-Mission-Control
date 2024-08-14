using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IERAX_MissionControl
{
    public class GMapMarkerImage : GMapMarker
    {
        private readonly Bitmap _image;

        public GMapMarkerImage(PointLatLng p, Bitmap image) : base(p)
        {

            _image = ResizeBitmap(image, 50, 50); // Adjust the size as needed
        }


        public override void OnRender(Graphics g)
        {
            if (_image != null)
            {
                // Draw the image centered on the marker's position
                g.DrawImage(_image, LocalPosition.X - _image.Width / 2, LocalPosition.Y - _image.Height / 2);
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
