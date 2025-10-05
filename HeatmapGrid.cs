using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Drawing;                  // Color, Pen, SolidBrush
using GMap.NET;
using GMap.NET.WindowsForms;           // GMapControl, GMapOverlay
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsForms.ToolTips;

namespace IERAX_MissionControl
{
    public sealed class HeatmapGrid
    {
        public readonly double CenterLat, CenterLon;
        public readonly double SizeMeters;
        public readonly double CellMeters;
        public readonly int Nx, Ny;

        private HeatAggregation _aggMode = HeatAggregation.Average;
        private readonly double[,] _max;      // for Max (raw)
        private readonly double[,] _maxW;     // for MaxWeighted (gaussian)


        private readonly double[,] Sum;
        private readonly int[,] Count;
        private readonly double[,] MaxVal;   // max of raw value per cell (binning)
        private readonly double[,] MaxWVal;  // max of value*weight per cell (gaussian)

        public readonly GMapOverlay Overlay;
        public readonly GMapPolygon[,] CellPoly;
        public enum HeatAggregation { Average, Max, MaxWeighted }


        public HeatmapGrid(double centerLat, double centerLon, double sizeMeters, double cellMeters, GMapControl map)
        {
            CenterLat = centerLat;
            CenterLon = centerLon;
            SizeMeters = sizeMeters;
            CellMeters = cellMeters;

   
            Nx = (int)Math.Ceiling(sizeMeters / cellMeters);
            Ny = Nx;

            Sum = new double[Nx, Ny];
            Count = new int[Nx, Ny];



        MaxVal = new double[Nx, Ny];
            MaxWVal = new double[Nx, Ny];

            for (int ix = 0; ix < Nx; ix++)
                for (int iy = 0; iy < Ny; iy++)
                {
                    MaxVal[ix, iy] = double.NegativeInfinity;
                    MaxWVal[ix, iy] = double.NegativeInfinity;
                }

            Overlay = new GMapOverlay("heatmap");
            CellPoly = new GMapPolygon[Nx, Ny];

            for (int ix = 0; ix < Nx; ix++)
            {
                for (int iy = 0; iy < Ny; iy++)
                {
                    double x0 = -SizeMeters / 2 + ix * CellMeters;
                    double y0 = SizeMeters / 2 - iy * CellMeters;
                    double x1 = x0 + CellMeters;
                    double y1 = y0 - CellMeters;

                    var pNW = ToLatLon(CenterLat, CenterLon, x0, y0);
                    var pNE = ToLatLon(CenterLat, CenterLon, x1, y0);
                    var pSE = ToLatLon(CenterLat, CenterLon, x1, y1);
                    var pSW = ToLatLon(CenterLat, CenterLon, x0, y1);

                    var points = new List<PointLatLng>
                    {
                        new PointLatLng(pNW.Lat, pNW.Lng),
                        new PointLatLng(pNE.Lat, pNE.Lng),
                        new PointLatLng(pSE.Lat, pSE.Lng),
                        new PointLatLng(pSW.Lat, pSW.Lng)
                    };

                    var poly = new GMapPolygon(points, $"cell_{ix}_{iy}");
                    poly.Stroke = new Pen(Color.FromArgb(30, 0, 0, 0), 1f);       // faint grid
                    poly.Fill = new SolidBrush(Color.FromArgb(0, 0, 0, 0));     // fully transparent initially

                    CellPoly[ix, iy] = poly;
                    Overlay.Polygons.Add(poly);
                }
            }

            // WinForms control exposes Overlays
            map.Overlays.Add(Overlay);
            map.Refresh();
        }

        public void Clear()
        {
            for (int ix = 0; ix < Nx; ix++)
                for (int iy = 0; iy < Ny; iy++)
                {
                    Sum[ix, iy] = 0;
                    Count[ix, iy] = 0;
                    MaxVal[ix, iy] = double.NegativeInfinity;
                    MaxWVal[ix, iy] = double.NegativeInfinity;

                    ReplaceFill(CellPoly[ix, iy], System.Drawing.Color.FromArgb(0, 0, 0, 0));
                }
        }

        public void SetAggregation(HeatAggregation mode) => _aggMode = mode;


        /// <summary>
        /// Add one sample. If gaussianRadiusMeters <= 0, bins to the single cell.
        /// Otherwise, applies a Gaussian “splat” to neighboring cells.
        /// </summary>
        public void AddSample(double lat, double lon, double value, double gaussianRadiusMeters)
        {
            if (gaussianRadiusMeters <= 0)
            {
                int ix, iy;
                if (TryCellIndex(lat, lon, out ix, out iy))
                {
                    // Average path (keep existing behavior available)
                    Sum[ix, iy] += value;
                    Count[ix, iy]++;

                    // Max path (raw value win)
                    if (value > MaxVal[ix, iy]) MaxVal[ix, iy] = value;
                }
                return;
            }

            // --- Gaussian mode ---
            double sx, sy;
            ToLocalXY(CenterLat, CenterLon, lat, lon, out sx, out sy);
            int cx = (int)Math.Floor((sx + SizeMeters / 2) / CellMeters);
            int cy = (int)Math.Floor((SizeMeters / 2 - sy) / CellMeters);
            int rCells = (int)Math.Ceiling(gaussianRadiusMeters / CellMeters);

            for (int ix = Math.Max(0, cx - rCells); ix <= Math.Min(Nx - 1, cx + rCells); ix++)
            {
                for (int iy = Math.Max(0, cy - rCells); iy <= Math.Min(Ny - 1, cy + rCells); iy++)
                {
                    double x0 = -SizeMeters / 2 + (ix + 0.5) * CellMeters;
                    double y0 = SizeMeters / 2 - (iy + 0.5) * CellMeters;
                    double dx = x0 - sx;
                    double dy = y0 - sy;
                    double d2 = dx * dx + dy * dy;
                    double w = Math.Exp(-d2 / (2 * gaussianRadiusMeters * gaussianRadiusMeters));

                    // Average path (optional smoothing)
                    Sum[ix, iy] += value * w;
                    Count[ix, iy] += (int)Math.Max(1, Math.Round(w * 10));

                    // MaxWeighted path: track maximum of value*weight
                    double vw = value * w;
                    if (vw > MaxWVal[ix, iy]) MaxWVal[ix, iy] = vw;
                }
            }
        }



        /// <summary>
        /// Recolor all cells according to avg(cell) mapped to Blue→Cyan→Green→Yellow→Red.
        /// </summary>
        public void Recolor(double minValue, double maxValue, byte maxAlpha)
        {
            for (int ix = 0; ix < Nx; ix++)
                for (int iy = 0; iy < Ny; iy++)
                {
                    double v;

                    switch (_aggMode)
                    {
                        case HeatAggregation.Max:
                            v = MaxVal[ix, iy];
                            if (double.IsNegativeInfinity(v)) { ReplaceFill(CellPoly[ix, iy], System.Drawing.Color.FromArgb(0, 0, 0, 0)); continue; }
                            break;

                        case HeatAggregation.MaxWeighted:
                            v = MaxWVal[ix, iy];
                            if (double.IsNegativeInfinity(v)) { ReplaceFill(CellPoly[ix, iy], System.Drawing.Color.FromArgb(0, 0, 0, 0)); continue; }
                            break;

                        default: // Average
                            if (Count[ix, iy] == 0) { ReplaceFill(CellPoly[ix, iy], System.Drawing.Color.FromArgb(0, 0, 0, 0)); continue; }
                            v = Sum[ix, iy] / Count[ix, iy];
                            break;
                    }

                    var c = ColorLerp(minValue, maxValue, v, maxAlpha);
                    ReplaceFill(CellPoly[ix, iy], c);
                }
        }



        private static void ReplaceFill(GMapPolygon poly, System.Drawing.Color c)
        {
            if (poly.Fill != null) poly.Fill.Dispose();
            poly.Fill = new System.Drawing.SolidBrush(c);
        }



        public bool TryCellIndex(double lat, double lon, out int ix, out int iy)
        {
            double x, y;
            ToLocalXY(CenterLat, CenterLon, lat, lon, out x, out y);
            ix = (int)Math.Floor((x + SizeMeters / 2) / CellMeters);
            iy = (int)Math.Floor((SizeMeters / 2 - y) / CellMeters);
            return ix >= 0 && ix < Nx && iy >= 0 && iy < Ny;
        }

        // --- helpers ---


        public static void ToLocalXY(double refLat, double refLon, double lat, double lon, out double x, out double y)
        {
            double R = 6378137.0;
            double dLat = (lat - refLat) * Math.PI / 180.0;
            double dLon = (lon - refLon) * Math.PI / 180.0;
            double m = Math.Cos(refLat * Math.PI / 180.0);
            x = R * dLon * m;   // east
            y = R * dLat;       // north
        }

        public static (double Lat, double Lng) ToLatLon(double refLat, double refLon, double xEast, double yNorth)
        {
            double R = 6378137.0;
            double dLat = yNorth / R;
            double dLon = xEast / (R * Math.Cos(refLat * Math.PI / 180.0));
            return (refLat + dLat * 180.0 / Math.PI, refLon + dLon * 180.0 / Math.PI);
        }

        private static Color ColorLerp(double vmin, double vmax, double v, byte maxAlpha)
        {
            v = Clamp01((v - vmin) / Math.Max(1e-9, (vmax - vmin)));

            // 5-stop gradient
            Color[] stops = new Color[]
            {
                Color.Blue,
                Color.Cyan,
                Color.Lime,
                Color.Yellow,
                Color.Red
            };

            double t = v * (stops.Length - 1);
            int i = (int)Math.Floor(t);
            int j = Math.Min(stops.Length - 1, i + 1);
            double f = t - i;

            byte r = (byte)Math.Round(stops[i].R + (stops[j].R - stops[i].R) * f);
            byte g = (byte)Math.Round(stops[i].G + (stops[j].G - stops[i].G) * f);
            byte b = (byte)Math.Round(stops[i].B + (stops[j].B - stops[i].B) * f);

            return Color.FromArgb(maxAlpha, r, g, b);
        }

        private static double Clamp01(double x)
        {
            if (x < 0.0) return 0.0;
            if (x > 1.0) return 1.0;
            return x;
        }

    }

}
