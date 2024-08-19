namespace IERAX_MissionControl
{
    partial class MPIeraxMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MPIeraxMain));
            this.CMB_comport = new System.Windows.Forms.ComboBox();
            this.cmb_baudrate = new System.Windows.Forms.ComboBox();
            this.but_connect = new System.Windows.Forms.Button();
            this.but_armdisarm = new System.Windows.Forms.Button();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            this.but_mission = new System.Windows.Forms.Button();
            this.gMapControl1 = new GMap.NET.WindowsForms.GMapControl();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.ArmStatusBox = new System.Windows.Forms.TextBox();
            this.TakeOffButton = new System.Windows.Forms.Button();
            this.listView1 = new System.Windows.Forms.ListView();
            this.AltimeterBox = new System.Windows.Forms.TextBox();
            this.DroneMode = new System.Windows.Forms.TextBox();
            this.AltHoldButton = new System.Windows.Forms.Button();
            this.GuidedModeButton = new System.Windows.Forms.Button();
            this.ShipFollowingModeLabel = new System.Windows.Forms.Label();
            this.StopFollowingShipButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // CMB_comport
            // 
            this.CMB_comport.FormattingEnabled = true;
            this.CMB_comport.Location = new System.Drawing.Point(26, 25);
            this.CMB_comport.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.CMB_comport.Name = "CMB_comport";
            this.CMB_comport.Size = new System.Drawing.Size(238, 33);
            this.CMB_comport.TabIndex = 0;
            this.CMB_comport.SelectedIndexChanged += new System.EventHandler(this.CMB_comport_SelectedIndexChanged);
            this.CMB_comport.Click += new System.EventHandler(this.CMB_comport_Click);
            // 
            // cmb_baudrate
            // 
            this.cmb_baudrate.FormattingEnabled = true;
            this.cmb_baudrate.Items.AddRange(new object[] {
            "9600",
            "14400",
            "19200",
            "28800",
            "38400",
            "57600",
            "115200"});
            this.cmb_baudrate.Location = new System.Drawing.Point(280, 23);
            this.cmb_baudrate.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.cmb_baudrate.Name = "cmb_baudrate";
            this.cmb_baudrate.Size = new System.Drawing.Size(238, 33);
            this.cmb_baudrate.TabIndex = 1;
            this.cmb_baudrate.SelectedIndexChanged += new System.EventHandler(this.cmb_baudrate_SelectedIndexChanged);
            // 
            // but_connect
            // 
            this.but_connect.Location = new System.Drawing.Point(536, 23);
            this.but_connect.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.but_connect.Name = "but_connect";
            this.but_connect.Size = new System.Drawing.Size(150, 44);
            this.but_connect.TabIndex = 2;
            this.but_connect.Text = "Connect";
            this.but_connect.UseVisualStyleBackColor = true;
            this.but_connect.Click += new System.EventHandler(this.but_connect_Click);
            // 
            // but_armdisarm
            // 
            this.but_armdisarm.Location = new System.Drawing.Point(904, 23);
            this.but_armdisarm.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.but_armdisarm.Name = "but_armdisarm";
            this.but_armdisarm.Size = new System.Drawing.Size(150, 44);
            this.but_armdisarm.TabIndex = 3;
            this.but_armdisarm.Text = "Arm/Disarm";
            this.but_armdisarm.UseVisualStyleBackColor = true;
            this.but_armdisarm.Click += new System.EventHandler(this.but_armdisarm_Click);
            // 
            // but_mission
            // 
            this.but_mission.Location = new System.Drawing.Point(698, 23);
            this.but_mission.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.but_mission.Name = "but_mission";
            this.but_mission.Size = new System.Drawing.Size(194, 44);
            this.but_mission.TabIndex = 4;
            this.but_mission.Text = "Send Mission";
            this.but_mission.UseVisualStyleBackColor = true;
            this.but_mission.Click += new System.EventHandler(this.but_mission_Click);
            // 
            // gMapControl1
            // 
            this.gMapControl1.Bearing = 0F;
            this.gMapControl1.CanDragMap = true;
            this.gMapControl1.EmptyTileColor = System.Drawing.Color.Navy;
            this.gMapControl1.GrayScaleMode = false;
            this.gMapControl1.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            this.gMapControl1.LevelsKeepInMemory = 5;
            this.gMapControl1.Location = new System.Drawing.Point(26, 77);
            this.gMapControl1.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.gMapControl1.MarkersEnabled = true;
            this.gMapControl1.MaxZoom = 2;
            this.gMapControl1.MinZoom = 2;
            this.gMapControl1.MouseWheelZoomEnabled = true;
            this.gMapControl1.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            this.gMapControl1.Name = "gMapControl1";
            this.gMapControl1.NegativeMode = false;
            this.gMapControl1.PolygonsEnabled = true;
            this.gMapControl1.RetryLoadTile = 0;
            this.gMapControl1.RoutesEnabled = true;
            this.gMapControl1.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            this.gMapControl1.SelectedAreaFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(65)))), ((int)(((byte)(105)))), ((int)(((byte)(225)))));
            this.gMapControl1.ShowTileGridLines = false;
            this.gMapControl1.Size = new System.Drawing.Size(2210, 1217);
            this.gMapControl1.TabIndex = 0;
            this.gMapControl1.Zoom = 0D;
            this.gMapControl1.Load += new System.EventHandler(this.gMapControl1_Load);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::IERAX_MissionControl.Properties.Resources.ieraxlogo;
            this.pictureBox1.Location = new System.Drawing.Point(1970, 1142);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(204, 134);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // ArmStatusBox
            // 
            this.ArmStatusBox.Location = new System.Drawing.Point(1064, 30);
            this.ArmStatusBox.Name = "ArmStatusBox";
            this.ArmStatusBox.Size = new System.Drawing.Size(100, 31);
            this.ArmStatusBox.TabIndex = 6;
            // 
            // TakeOffButton
            // 
            this.TakeOffButton.Location = new System.Drawing.Point(1282, 30);
            this.TakeOffButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.TakeOffButton.Name = "TakeOffButton";
            this.TakeOffButton.Size = new System.Drawing.Size(190, 36);
            this.TakeOffButton.TabIndex = 7;
            this.TakeOffButton.Text = "TAKE OFF";
            this.TakeOffButton.UseVisualStyleBackColor = true;
            this.TakeOffButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // listView1
            // 
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(1990, 77);
            this.listView1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(180, 1054);
            this.listView1.TabIndex = 8;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // AltimeterBox
            // 
            this.AltimeterBox.Location = new System.Drawing.Point(1990, 77);
            this.AltimeterBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.AltimeterBox.Name = "AltimeterBox";
            this.AltimeterBox.Size = new System.Drawing.Size(180, 31);
            this.AltimeterBox.TabIndex = 9;
            this.AltimeterBox.Text = "Altimeter";
            // 
            // DroneMode
            // 
            this.DroneMode.Location = new System.Drawing.Point(1990, 120);
            this.DroneMode.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.DroneMode.Name = "DroneMode";
            this.DroneMode.Size = new System.Drawing.Size(180, 31);
            this.DroneMode.TabIndex = 10;
            this.DroneMode.Text = "Drone Mode";
            // 
            // AltHoldButton
            // 
            this.AltHoldButton.Location = new System.Drawing.Point(1990, 1050);
            this.AltHoldButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.AltHoldButton.Name = "AltHoldButton";
            this.AltHoldButton.Size = new System.Drawing.Size(182, 36);
            this.AltHoldButton.TabIndex = 11;
            this.AltHoldButton.Text = "AltHold Mode";
            this.AltHoldButton.UseVisualStyleBackColor = true;
            this.AltHoldButton.Click += new System.EventHandler(this.AltHoldButton_Click);
            // 
            // GuidedModeButton
            // 
            this.GuidedModeButton.Location = new System.Drawing.Point(1990, 1097);
            this.GuidedModeButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.GuidedModeButton.Name = "GuidedModeButton";
            this.GuidedModeButton.Size = new System.Drawing.Size(183, 36);
            this.GuidedModeButton.TabIndex = 12;
            this.GuidedModeButton.Text = "Guided Mode";
            this.GuidedModeButton.UseVisualStyleBackColor = true;
            this.GuidedModeButton.Click += new System.EventHandler(this.GuidedModeButton_Click);
            // 
            // ShipFollowingModeLabel
            // 
            this.ShipFollowingModeLabel.AutoSize = true;
            this.ShipFollowingModeLabel.Location = new System.Drawing.Point(2002, 457);
            this.ShipFollowingModeLabel.Name = "ShipFollowingModeLabel";
            this.ShipFollowingModeLabel.Size = new System.Drawing.Size(152, 25);
            this.ShipFollowingModeLabel.TabIndex = 13;
            this.ShipFollowingModeLabel.Text = "Ship Following";
            // 
            // StopFollowingShipButton
            // 
            this.StopFollowingShipButton.Location = new System.Drawing.Point(1990, 485);
            this.StopFollowingShipButton.Name = "StopFollowingShipButton";
            this.StopFollowingShipButton.Size = new System.Drawing.Size(180, 39);
            this.StopFollowingShipButton.TabIndex = 14;
            this.StopFollowingShipButton.Text = "Stop Following";
            this.StopFollowingShipButton.UseVisualStyleBackColor = true;
            this.StopFollowingShipButton.Click += new System.EventHandler(this.StopFollowingShipButton_Click);
            // 
            // MPIeraxMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2241, 1314);
            this.Controls.Add(this.StopFollowingShipButton);
            this.Controls.Add(this.ShipFollowingModeLabel);
            this.Controls.Add(this.GuidedModeButton);
            this.Controls.Add(this.AltHoldButton);
            this.Controls.Add(this.DroneMode);
            this.Controls.Add(this.AltimeterBox);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.TakeOffButton);
            this.Controls.Add(this.ArmStatusBox);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.gMapControl1);
            this.Controls.Add(this.but_mission);
            this.Controls.Add(this.but_armdisarm);
            this.Controls.Add(this.but_connect);
            this.Controls.Add(this.cmb_baudrate);
            this.Controls.Add(this.CMB_comport);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "MPIeraxMain";
            this.Text = "IERAX MISSION CONTROL";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox CMB_comport;
        private System.Windows.Forms.ComboBox cmb_baudrate;
        private System.Windows.Forms.Button but_connect;
        private System.Windows.Forms.Button but_armdisarm;
        private System.IO.Ports.SerialPort serialPort1;
        private System.Windows.Forms.Button but_mission;
        private GMap.NET.WindowsForms.GMapControl gMapControl1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox ArmStatusBox;
        private System.Windows.Forms.Button TakeOffButton;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.TextBox AltimeterBox;
        private System.Windows.Forms.TextBox DroneMode;
        private System.Windows.Forms.Button AltHoldButton;
        private System.Windows.Forms.Button GuidedModeButton;
        private System.Windows.Forms.Label ShipFollowingModeLabel;
        private System.Windows.Forms.Button StopFollowingShipButton;
    }
}

