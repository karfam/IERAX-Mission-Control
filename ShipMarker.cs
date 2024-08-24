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
    using System.Drawing.Drawing2D;
    using System.Collections.Generic;
    using System;

    public class ShipMarker : GMapMarker
    {
        private readonly Bitmap shipIcon;

        // Additional properties
        public string ShipName { get; set; }
        public string MMSI { get; set; }

        private double heading;
        public double Heading
        {
            get => heading;
            set
            {
                if (heading != value)
                {
                    heading = value;
                    HeadingChanged?.Invoke();
                }
            }
        }

        private double speed;
        public double Speed
        {
            get => speed;
            set
            {
                if (speed != value)
                {
                    speed = value;
                    SpeedChanged?.Invoke();
                }
            }
        }
        public string VesselType { get; set; }

        public event Action PositionChanged;
        public event Action SpeedChanged;
        public event Action HeadingChanged;


        // List to store historical positions
        public List<HistoryMarker> HistoryMarkers { get; private set; } = new List<HistoryMarker>();

        // List to store projected positions
        public List<HistoryMarker> ProjectedMarkers { get; private set; } = new List<HistoryMarker>();

        // Add a property to store the previous position
        public PointLatLng? PreviousPosition { get; set; }

        // Property to store the previous position
        private PointLatLng? shipPosition;
        public PointLatLng? ShipPosition
        {
            get => shipPosition;
            set
            {
                if (shipPosition != value)
                {
                    shipPosition = value;
                    PositionChanged?.Invoke();
                }
            }
        }

        public ShipMarker(PointLatLng p) : base(p)
        {
            Bitmap originalIcon = new Bitmap(Resources.container); // Assuming the image is named ShipMarker in Resources
            shipIcon = ResizeBitmap(originalIcon, 50, 50); // Adjust the size as needed
        }

        public override void OnRender(Graphics g)
        {
            if (shipIcon != null)
            {
                // Rotate the ship icon based on the heading
                Bitmap rotatedIcon = RotateImage(shipIcon, (float)Heading - 90.0f);

                // Draw the rotated image at the ship's position
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

        public void UpdateProjectedMarkers()
        {
            // Clear existing projected markers
            ProjectedMarkers.Clear();

            double distancePerMinute = Speed / 60.0; // Nautical miles per minute

            for (int i = 1; i <= 10; i++) // Next 10 minutes
            {
                // Calculate the new position based on heading and speed
                PointLatLng newPosition = CalculateNewPosition(Position, Heading, distancePerMinute * i);
                HistoryMarker projectedMarker = new HistoryMarker(newPosition, Brushes.Blue)
                {
                    // Use blue for projected markers
                    ToolTipMode = MarkerTooltipMode.OnMouseOver
                };
                ProjectedMarkers.Add(projectedMarker);
            }
        }


        private PointLatLng CalculateNewPosition(PointLatLng startPosition, double heading, double distance)
        {
            double radHeading = heading * Math.PI / 180.0;
            double latDistance = distance * Math.Cos(radHeading);
            double lngDistance = distance * Math.Sin(radHeading);

            // Earth's radius in nautical miles
            const double earthRadius = 3440.065;

            double newLat = startPosition.Lat + (latDistance / earthRadius) * (180.0 / Math.PI);
            double newLng = startPosition.Lng + (lngDistance / (earthRadius * Math.Cos(startPosition.Lat * Math.PI / 180.0))) * (180.0 / Math.PI);

            return new PointLatLng(newLat, newLng);
        }


    }


}
