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
    public class HistoryMarker : GMapMarker
    {
        private readonly Brush _brush;

        public HistoryMarker(PointLatLng p, Brush brush = null) : base(p)
        {
            _brush = brush ?? Brushes.Red; // Default to red if no brush is provided
        }


        public override void OnRender(Graphics g)
        {
            // Draw a small ellipse (dot) with the specified color
            g.FillEllipse(_brush, new Rectangle(LocalPosition.X - 2, LocalPosition.Y - 2, 4, 4));
        }
    }


}
