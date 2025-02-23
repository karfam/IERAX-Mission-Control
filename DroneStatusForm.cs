using System;
using System.Drawing;
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

     

// Then, implement the event handler:
        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
 
            // Load the ship layout image (consider caching this image instead of loading on every paint)
            using (Image shipImage = Properties.Resources.container)
            {
                shipImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                // Define desired size for the ship layout (change these values as needed)
                int desiredWidth = 100;  // e.g., 100 pixels wide
                int desiredHeight = 100; // e.g., 100 pixels tall

                // Calculate the position to center the image in the PictureBox
                int x = (pictureBox1.Width - desiredWidth) / 2;
                int y = (pictureBox1.Height - desiredHeight) / 2;

                // Create a destination rectangle
                Rectangle destRect = new Rectangle(x, y, desiredWidth, desiredHeight);

                // Draw the image
                e.Graphics.DrawImage(shipImage, destRect);
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
