using System;
using System.Drawing;
using System.Windows.Forms;

namespace IERAX_MissionControl
{
    /// <summary>
    /// A form to manually enter background concentrations for SO₂ and CO₂.
    /// </summary>
    public partial class manualDataInputForm : Form
    {
        // Public properties to expose the entered values
        public double C_SO2_bkg { get; private set; }
        public double C_CO2_bkg { get; private set; }

        // UI controls
        private Label lblSO2;
        private TextBox txtSO2_bkg;
        private Label lblCO2;
        private TextBox txtCO2_bkg;
        private Button btnSave;

        // Constructor
        public manualDataInputForm()
        {
            InitializeComponent();
        }

        // Initialize UI components
        private void InitializeComponent()
        {
            this.lblSO2 = new System.Windows.Forms.Label();
            this.txtSO2_bkg = new System.Windows.Forms.TextBox();
            this.lblCO2 = new System.Windows.Forms.Label();
            this.txtCO2_bkg = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblSO2
            // 
            this.lblSO2.AutoSize = true;
            this.lblSO2.Font = new System.Drawing.Font("Airborne", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.lblSO2.Location = new System.Drawing.Point(10, 20);
            this.lblSO2.Name = "lblSO2";
            this.lblSO2.Size = new System.Drawing.Size(254, 31);
            this.lblSO2.TabIndex = 0;
            this.lblSO2.Text = "SO₂ Background (ppb):";
            // 
            // txtSO2_bkg
            // 
            this.txtSO2_bkg.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.txtSO2_bkg.Location = new System.Drawing.Point(270, 14);
            this.txtSO2_bkg.Name = "txtSO2_bkg";
            this.txtSO2_bkg.Size = new System.Drawing.Size(169, 38);
            this.txtSO2_bkg.TabIndex = 1;
            this.txtSO2_bkg.TextChanged += new System.EventHandler(this.txtSO2_bkg_TextChanged);
            // 
            // lblCO2
            // 
            this.lblCO2.AutoSize = true;
            this.lblCO2.Font = new System.Drawing.Font("Airborne", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.lblCO2.Location = new System.Drawing.Point(10, 85);
            this.lblCO2.Name = "lblCO2";
            this.lblCO2.Size = new System.Drawing.Size(254, 31);
            this.lblCO2.TabIndex = 2;
            this.lblCO2.Text = "CO₂ Background (ppm):";
            // 
            // txtCO2_bkg
            // 
            this.txtCO2_bkg.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.txtCO2_bkg.Location = new System.Drawing.Point(270, 79);
            this.txtCO2_bkg.Name = "txtCO2_bkg";
            this.txtCO2_bkg.Size = new System.Drawing.Size(169, 38);
            this.txtCO2_bkg.TabIndex = 3;
            // 
            // btnSave
            // 
            this.btnSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.btnSave.Location = new System.Drawing.Point(10, 201);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(429, 100);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save Data";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // manualDataInputForm
            // 
            this.ClientSize = new System.Drawing.Size(443, 313);
            this.Controls.Add(this.lblSO2);
            this.Controls.Add(this.txtSO2_bkg);
            this.Controls.Add(this.lblCO2);
            this.Controls.Add(this.txtCO2_bkg);
            this.Controls.Add(this.btnSave);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "manualDataInputForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manual Data Input";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        // Save button click handler
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(txtSO2_bkg.Text, out var so2) ||
                !double.TryParse(txtCO2_bkg.Text, out var co2))
            {
                MessageBox.Show("Please enter valid numeric values for both fields.",
                                "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            C_SO2_bkg = so2;
            C_CO2_bkg = co2;

            this.Close();
        }

        private void txtSO2_bkg_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

