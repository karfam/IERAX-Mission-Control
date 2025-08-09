using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMap.NET;
using GMap.NET.WindowsForms;

namespace IERAX_MissionControl
{
    public class MagnitudeCircleMarker : GMapMarker
    {
        public double Magnitude { get; }
        public int BaseRadius { get; set; } = 6;    // pixels
        public float OutlineWidth { get; set; } = 1.5f;

        public MagnitudeCircleMarker(PointLatLng pos, double magnitude) : base(pos)
        {
            Magnitude = magnitude;
            var r = RadiusPx;
            Size = new Size(r * 2, r * 2);
            Offset = new Point(-r, -r); // center the circle on the position
            IsHitTestVisible = true;
        }

        private int RadiusPx => BaseRadius + (int)Math.Round(Math.Max(0, Magnitude) * 2.0); // scale with M

        public override void OnRender(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int r = RadiusPx;
            var rect = new Rectangle(LocalPosition.X - r + Size.Width / 2, LocalPosition.Y - r + Size.Height / 2, r * 2, r * 2);

            var fill = GetColorForMagnitude(Magnitude);
            using var fillBrush = new SolidBrush(Color.FromArgb(170, fill));          // semi-transparent fill
            using var outlinePen = new Pen(Darken(fill, 0.35f), OutlineWidth);

            g.FillEllipse(fillBrush, rect);
            g.DrawEllipse(outlinePen, rect);
        }

        private static Color GetColorForMagnitude(double m)
        {
            // 0→green, 2.5→yellow, 5+→red (clamped)
            double t = Math.Max(0, Math.Min(1, m / 5.0));
            // Green→Yellow
            if (t <= 0.5)
                return Lerp(Color.FromArgb(0, 180, 0), Color.FromArgb(255, 200, 0), t / 0.5);
            // Yellow→Red
            return Lerp(Color.FromArgb(255, 200, 0), Color.FromArgb(220, 0, 0), (t - 0.5) / 0.5);
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            int R = (int)Math.Round(a.R + (b.R - a.R) * t);
            int G = (int)Math.Round(a.G + (b.G - a.G) * t);
            int B = (int)Math.Round(a.B + (b.B - a.B) * t);
            return Color.FromArgb(R, G, B);
        }

        private static Color Darken(Color c, float amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));
            return Color.FromArgb(
                (int)(c.R * (1 - amount)),
                (int)(c.G * (1 - amount)),
                (int)(c.B * (1 - amount)));
        }
    }
}
