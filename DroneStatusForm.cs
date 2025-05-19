using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;


namespace IERAX_MissionControl
{
    public partial class DroneStatusForm : Form
    {
        public DroneStatusForm()
        {
            InitializeComponent();
        }

        private void DroneStatusForm_Load(object sender, EventArgs e)
        {
            textBoxStatus.Multiline = true;
            textBoxStatus.ScrollBars = ScrollBars.Vertical;
            textBoxStatus.ReadOnly = true;
            pictureBox1.Paint += PictureBox1_Paint;
        }

        // Function to update status text
        public void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
            }
            else
            {
                textBoxStatus.AppendText(message + Environment.NewLine);
                textBoxStatus.SelectionStart = textBoxStatus.Text.Length;
                textBoxStatus.ScrollToCaret();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }



        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // Load the ship layout image (ideally cache it so it isn't loaded on every paint)
            using (Image shipImage = Properties.Resources.container)
            {
                // Rotate if needed.
                shipImage.RotateFlip(RotateFlipType.Rotate90FlipNone);

                // Define desired size.
                int desiredWidth = 100;  // 100 pixels wide
                int desiredHeight = 100; // 100 pixels tall

                // Calculate center position then shift one grid row up (assumed 100 pixels per row)
                int x = (pictureBox1.Width - desiredWidth) / 2;
                int gridRowHeight = 100;
                int y = ((pictureBox1.Height - desiredHeight) / 2) - gridRowHeight;

                // Destination rectangle for drawing the image.
                Rectangle destRect = new Rectangle(x, y, desiredWidth, desiredHeight);

                // Create a ColorMatrix and set its alpha value to 0.5 (50% opacity)
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = 0.5f; // Alpha component

                // Create ImageAttributes and set the ColorMatrix.
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    // Draw the image using the destination rectangle and the ImageAttributes for transparency.
                    e.Graphics.DrawImage(
                        shipImage,
                        destRect,         // Destination rectangle.
                        0, 0,             // Source X and Y.
                        shipImage.Width,  // Source width.
                        shipImage.Height, // Source height.
                        GraphicsUnit.Pixel,
                        attributes);      // Apply the transparency.
                }
            }
        }






        public PictureBox VisualizerPictureBox
        {
            get { return this.pictureBox1; }
        }


        private void textBoxStatus_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
