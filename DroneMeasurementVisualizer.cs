



namespace IERAX_MissionControl
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;



    public class DroneMeasurementVisualizer
    {
        // Visualizes CO2 readings by drawing a grid representation in the provided PictureBox.
        // The shipPosition is used as a reference for generating the ordered grid points.
        public void VisualizeCO2Readings(PointLatLng shipPosition, Dictionary<PointLatLng, int> co2Readings, PictureBox pictureBox1)
        {
            // Grid dimensions: 5 columns x 5 rows
            int gridCols = 5;
            int gridRows = 5;
            int cellSize = 50; // pixels per cell
            int imageWidth = gridCols * cellSize;
            int imageHeight = gridRows * cellSize;

            // Create a new bitmap for drawing.
            Bitmap bitmap = new Bitmap(imageWidth, imageHeight);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Clear the background.
                g.Clear(Color.White);

                // Draw the grid lines.
                using (Pen gridPen = new Pen(Color.Black, 2))
                {
                    // Draw vertical lines.
                    for (int i = 0; i <= gridCols; i++)
                    {
                        g.DrawLine(gridPen, i * cellSize, 0, i * cellSize, imageHeight);
                    }
                    // Draw horizontal lines.
                    for (int j = 0; j <= gridRows; j++)
                    {
                        g.DrawLine(gridPen, 0, j * cellSize, imageWidth, j * cellSize);
                    }
                }

                // Generate an ordered list of grid points based on the shipPosition.
                // This ordering should match the one used during measurement (column-first, then row).
                List<PointLatLng> gridPoints = GetOrderedGridPoints(shipPosition);

                // Determine the range of CO2 values for color scaling.
                int minCO2 = co2Readings.Values.Min();
                int maxCO2 = co2Readings.Values.Max();

                // Draw each cell in the grid with the corresponding CO2 reading.
                for (int i = 0; i < gridPoints.Count; i++)
                {
                    // Compute the row and column for this cell.
                    int col = i / gridRows;  // Outer loop is columns.
                    int row = i % gridRows;  // Inner loop is rows.

                    // Retrieve the CO2 reading for this grid point.
                    int reading = co2Readings[gridPoints[i]];

                    // Get the cell color based on the reading (green for low, red for high).
                    Color cellColor = GetColorForReading(reading, minCO2, maxCO2);

                    // Fill the cell with the determined color.
                    using (SolidBrush brush = new SolidBrush(cellColor))
                    {
                        g.FillRectangle(brush, col * cellSize, row * cellSize, cellSize, cellSize);
                    }

                    // Draw the CO2 reading text in the center of the cell.
                    string readingText = reading.ToString();
                    SizeF textSize = g.MeasureString(readingText, SystemFonts.DefaultFont);
                    float textX = col * cellSize + (cellSize - textSize.Width) / 2;
                    float textY = row * cellSize + (cellSize - textSize.Height) / 2;
                    g.DrawString(readingText, SystemFonts.DefaultFont, Brushes.Black, textX, textY);
                }
            }

            // Assign the generated bitmap to the PictureBox for display.
            pictureBox1.Image = bitmap;
        }

        // Helper: Generates an ordered list of grid center points based on the ship's position.
        // This assumes that the measurement was done in a column-first order.
        private List<PointLatLng> GetOrderedGridPoints(PointLatLng shipPosition)
        {
            List<PointLatLng> points = new List<PointLatLng>();

            // Define the offsets for the grid (in meters) relative to the ship position.
            // For columns (left to right): -20, -10, 0, 10, 20.
            double[] colCenters = new double[] { -20, -10, 0, 10, 20 };

            // For rows (top to bottom): 15, 5, -5, -15, -25.
            double[] rowCenters = new double[] { 15, 5, -5, -15, -25 };

            // Iterate in a column-first (outer loop) then row (inner loop) manner.
            foreach (double colOffset in colCenters)
            {
                foreach (double rowOffset in rowCenters)
                {
                    PointLatLng pt = ConvertOffsetToLatLng(shipPosition, colOffset, rowOffset);
                    points.Add(pt);
                }
            }

            return points;
        }

        // Helper: Convert an offset in meters from an origin point to a new geographic coordinate.
        public static PointLatLng ConvertOffsetToLatLng(PointLatLng origin, double offsetX, double offsetY)
        {
            // Earth's radius in meters (WGS84)
            double earthRadius = 6378137;

            // Change in latitude in degrees.
            double deltaLat = (offsetY / earthRadius) * (180 / Math.PI);

            // Change in longitude in degrees, adjusted by the cosine of the latitude.
            double deltaLng = (offsetX / earthRadius) * (180 / Math.PI) / Math.Cos(origin.Lat * Math.PI / 180);

            return new PointLatLng(origin.Lat + deltaLat, origin.Lng + deltaLng);
        }

        // Helper: Maps a CO2 reading to a color gradient (green for low, red for high).
        private Color GetColorForReading(int reading, int minReading, int maxReading)
        {
            double ratio = (maxReading - minReading) != 0
                ? (double)(reading - minReading) / (maxReading - minReading)
                : 0.5;

            int red = (int)(ratio * 255);
            int green = (int)((1 - ratio) * 255);

            return Color.FromArgb(red, green, 0);
        }
    }

    // Example class representing a geographic coordinate.
    public class PointLatLng
    {
        public double Lat { get; set; }
        public double Lng { get; set; }

        public PointLatLng(double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
        }
    }

}
