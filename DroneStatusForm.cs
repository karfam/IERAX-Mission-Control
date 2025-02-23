using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

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
    }
}
