using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using GMap.NET;

namespace IERAX_MissionControl
{
    public partial class CameraForm : Form
    {
        private PictureBox pictureBox;
        private Button screenshotButton; // Screenshot button
        private UdpClient udpClient;
        private Thread receiveThread;
        private volatile bool running;
        private int listenPort;
        private byte[] buffer = new byte[0]; // Buffer for reassembling images
        private PointLatLng shipPosition=null;




        public CameraForm(int port)
        {
            listenPort = port;
            SetupForm();
        }

        public void UpdateShipPosition(PointLatLng position)
        {
            shipPosition = position; // Update the stored ship position
        }


        private void SetupForm()
        {
            this.Text = "Drone Camera Feed (UDP)";
            this.Size = new Size(800, 600);

            // PictureBox to display video
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(pictureBox);

            // Screenshot Button
            screenshotButton = new Button
            {
                Text = "Take Screenshot",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            screenshotButton.Click += ScreenshotButton_Click;
            this.Controls.Add(screenshotButton);

            this.Load += CameraForm_Load;
            this.FormClosing += CameraForm_FormClosing;
        }

        private void CameraForm_Load(object sender, EventArgs e)
        {
            StartReceiving();
        }

        private void CameraForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopReceiving();
        }

        private void StartReceiving()
        {
            running = true;
            udpClient = new UdpClient(listenPort);
            udpClient.Client.ReceiveTimeout = 1000; // 1s timeout to allow smooth exit

            receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true
            };
            receiveThread.Start();
        }

        private void StopReceiving()
        {
            running = false;
            udpClient?.Close();
            udpClient = null;

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(2000);
            }
        }

        private void ReceiveLoop()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);
            while (running)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref remoteEP);
                    AppendToBuffer(data);
                    ProcessBufferForJPEG();
                }
                catch (SocketException se)
                {
                    if (!running) break;
                    if (se.SocketErrorCode == SocketError.TimedOut) continue;
                    Console.WriteLine("SocketException: " + se.Message);
                }
                catch (Exception ex)
                {
                    if (!running) break;
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
        }

        private void AppendToBuffer(byte[] newData)
        {
            byte[] newBuffer = new byte[buffer.Length + newData.Length];
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
            Buffer.BlockCopy(newData, 0, newBuffer, buffer.Length, newData.Length);
            buffer = newBuffer;
        }

        private void ProcessBufferForJPEG()
        {
            int start = FindMarker(buffer, new byte[] { 0xFF, 0xD8 });
            int end = FindMarker(buffer, new byte[] { 0xFF, 0xD9 });

            if (start != -1 && end != -1 && end > start)
            {
                int jpegLength = end - start + 2;
                byte[] jpegData = new byte[jpegLength];
                Buffer.BlockCopy(buffer, start, jpegData, 0, jpegLength);

                int remaining = buffer.Length - (start + jpegLength);
                byte[] newBuffer = new byte[remaining];
                Buffer.BlockCopy(buffer, start + jpegLength, newBuffer, 0, remaining);
                buffer = newBuffer;

                try
                {
                    using (MemoryStream ms = new MemoryStream(jpegData))
                    {
                        Image image = Image.FromStream(ms);
                        UpdatePictureBox(image);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("JPEG decoding exception: " + ex.Message);
                }
            }
        }

        private int FindMarker(byte[] data, byte[] marker)
        {
            for (int i = 0; i <= data.Length - marker.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < marker.Length; j++)
                {
                    if (data[i + j] != marker[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    return i;
            }
            return -1;
        }

        private void UpdatePictureBox(Image image)
        {
            if (pictureBox.InvokeRequired)
            {
                pictureBox.Invoke(new MethodInvoker(() => UpdatePictureBox(image)));
            }
            else
            {
                pictureBox.Image?.Dispose();
                pictureBox.Image = (Image)image.Clone();
            }
        }

        // Screenshot Button Click Event
        private void ScreenshotButton_Click(object sender, EventArgs e)
        {
            ShipMarker currentMarker = MPIeraxMain.GlobalClickedMarker; // Access the global variable
            float instantCO2 = MPIeraxMain.InstantCO2; // Access the global variable
            float instantHDCO2 = MPIeraxMain.InstantHDCO2; // Access the global variable
            String instantAnalyzer = AnalyzerForm.InstantAnalyzerCO2; // Access the global variable

            if (currentMarker != null && currentMarker.Position != null)
            {
                SaveScreenshot(
                    currentMarker.Position,
                    currentMarker.ShipName,
                    currentMarker.MMSI,
                    currentMarker.Speed,
                    currentMarker.Heading,
                    instantCO2,
                    instantHDCO2,
                    instantAnalyzer
            );// Pass the position to the screenshot logic
            }
            else
            {
                MessageBox.Show("No ship selected!", "Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Saves the current image from the PictureBox with a timestamped filename.
        /// </summary>
        private void SaveScreenshot(GMap.NET.PointLatLng shipPosition, string shipName, string mmsi, double speed, double heading, float instantCO2, float instantHDCO2, string instantAnalyzer)
        {
            if (pictureBox.Image == null)
            {
                MessageBox.Show("No image to save!", "Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DroneScreenshots");
                Directory.CreateDirectory(directory); // Ensure directory exists

                string filename = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                string fullPath = Path.Combine(directory, filename);

                using (Image originalImage = (Image)pictureBox.Image.Clone())
                {
                    // Resize the image to reduce memory usage
                    int newWidth = 800; // Adjust as needed
                    int newHeight = (int)((double)originalImage.Height / originalImage.Width * newWidth);
                    using (Bitmap resizedBitmap = new Bitmap(originalImage, new Size(newWidth, newHeight)))
                    using (Graphics graphics = Graphics.FromImage(resizedBitmap))
                    {
                        // Prepare text content
                        string positionText = $"Lat: {shipPosition.Lat:F6}, Lng: {shipPosition.Lng:F6}";
                        string detailsText = $"Name: {shipName}\nMMSI: {mmsi}\nSpeed: {speed:F2} knots\nHeading: {heading:F2}°";
                        string sensorText = $"CO2: {instantCO2:F2} ppm\nHDCO2: {instantHDCO2:F2} ppm\nAnalyzer: {instantAnalyzer}"; // Display string as is

                        // Configure fonts and brushes
                        Font font = new Font("Arial", 16, FontStyle.Bold);
                        Brush textBrush = new SolidBrush(Color.Black);
                        Brush backgroundBrush = new SolidBrush(Color.White);

                        // Measure the size of the text
                        SizeF positionSize = graphics.MeasureString(positionText, font);
                        SizeF detailsSize = graphics.MeasureString(detailsText, font);
                        SizeF sensorSize = graphics.MeasureString(sensorText, font);

                        // Calculate text box dimensions
                        float textBoxWidth = Math.Max(positionSize.Width, Math.Max(detailsSize.Width, sensorSize.Width)) + 20; // Add padding
                        float textBoxHeight = positionSize.Height + detailsSize.Height + sensorSize.Height + 30; // Add padding

                        // Define the text box position
                        PointF textBoxPosition = new PointF(10, 10); // Top-left corner of the image

                        // Draw the background rectangle
                        graphics.FillRectangle(backgroundBrush, textBoxPosition.X, textBoxPosition.Y, textBoxWidth, textBoxHeight);

                        // Draw the position text
                        graphics.DrawString(positionText, font, textBrush, textBoxPosition.X + 10, textBoxPosition.Y + 10);

                        // Draw the details text below the position text
                        graphics.DrawString(detailsText, font, textBrush, textBoxPosition.X + 10, textBoxPosition.Y + 10 + positionSize.Height);

                        // Draw the sensor values below the details text
                        graphics.DrawString(sensorText, font, textBrush, textBoxPosition.X + 10, textBoxPosition.Y + 10 + positionSize.Height + detailsSize.Height);

                        // Save the resized bitmap with the overlay
                        resizedBitmap.Save(fullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }

                MessageBox.Show($"Screenshot saved:\n{fullPath}", "Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving screenshot: {ex.Message}", "Screenshot Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }





        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
