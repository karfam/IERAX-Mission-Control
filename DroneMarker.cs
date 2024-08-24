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
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    public class DroneMarker : GMapMarker
    {
        private readonly Bitmap droneIcon;
        private readonly MavlinkMessageHandler mavlinkMessageHandler;

        // Modified constructor to accept MavlinkMessageHandler
        public DroneMarker(PointLatLng p, MavlinkMessageHandler handler) : base(p)
        {
            // Store the reference to the MavlinkMessageHandler instance
            mavlinkMessageHandler = handler;

            // Load and resize the drone icon
            Bitmap originalIcon = new Bitmap(Resources.DroneHeadingIcon);
            droneIcon = ResizeBitmap(originalIcon, 50, 50); // Adjust the size as needed
        }


        public override void OnRender(Graphics g)
        {
            if (droneIcon != null)
            {
                double orientation = GetDroneCurrentHeading();
                // Rotate the ship icon based on the heading
                Bitmap rotatedIcon = RotateImage(droneIcon, (float)orientation);

                g.DrawImage(rotatedIcon, new PointF(LocalPosition.X - rotatedIcon.Width / 2, LocalPosition.Y - rotatedIcon.Height / 2));
                // Dispose of the rotated image after use
                rotatedIcon.Dispose();
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

        private Bitmap RotateImage(Bitmap bmp, float angle)
        {
            Bitmap rotatedImage = new Bitmap(bmp.Width, bmp.Height);
            rotatedImage.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                g.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, new Point(0, 0));
            }

            return rotatedImage;
        }

        private double GetDroneCurrentHeading()
        {

            double heading = mavlinkMessageHandler.DroneHeading;  
            return heading;
        }
    }
}
