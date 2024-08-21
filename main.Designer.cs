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
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            this.gMapControl1 = new GMap.NET.WindowsForms.GMapControl();
            this.AltHoldButton = new System.Windows.Forms.Button();
            this.GuidedModeButton = new System.Windows.Forms.Button();
            this.StopFollowingShipButton = new System.Windows.Forms.Button();
            this.GSpeedLabel = new System.Windows.Forms.Label();
            this.ArmStatusBox = new System.Windows.Forms.TextBox();
            this.TakeOffButton = new System.Windows.Forms.Button();
            this.but_armdisarm = new System.Windows.Forms.Button();
            this.but_mission = new System.Windows.Forms.Button();
            this.ShipFollowingModeLabel = new System.Windows.Forms.Label();
            this.but_connect = new System.Windows.Forms.Button();
            this.cmb_baudrate = new System.Windows.Forms.ComboBox();
            this.CMB_comport = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.ShipInfoLabel = new System.Windows.Forms.Label();
            this.LandButton = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.AltimeterBox = new System.Windows.Forms.Label();
            this.DroneMode = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // gMapControl1
            // 
            this.gMapControl1.AutoSize = true;
            this.gMapControl1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.gMapControl1.Bearing = 0F;
            this.gMapControl1.CanDragMap = true;
            this.gMapControl1.EmptyTileColor = System.Drawing.Color.Navy;
            this.gMapControl1.GrayScaleMode = false;
            this.gMapControl1.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            this.gMapControl1.LevelsKeepInMemory = 5;
            this.gMapControl1.Location = new System.Drawing.Point(0, 150);
            this.gMapControl1.Margin = new System.Windows.Forms.Padding(6);
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
            this.gMapControl1.Size = new System.Drawing.Size(0, 0);
            this.gMapControl1.TabIndex = 0;
            this.gMapControl1.Zoom = 0D;
            this.gMapControl1.Load += new System.EventHandler(this.gMapControl1_Load);
            // 
            // AltHoldButton
            // 
            this.AltHoldButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AltHoldButton.Location = new System.Drawing.Point(494, 77);
            this.AltHoldButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.AltHoldButton.Name = "AltHoldButton";
            this.AltHoldButton.Size = new System.Drawing.Size(237, 62);
            this.AltHoldButton.TabIndex = 11;
            this.AltHoldButton.Text = "AltHold Mode";
            this.AltHoldButton.UseVisualStyleBackColor = true;
            this.AltHoldButton.Click += new System.EventHandler(this.AltHoldButton_Click);
            // 
            // GuidedModeButton
            // 
            this.GuidedModeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GuidedModeButton.Location = new System.Drawing.Point(494, 5);
            this.GuidedModeButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.GuidedModeButton.Name = "GuidedModeButton";
            this.GuidedModeButton.Size = new System.Drawing.Size(237, 62);
            this.GuidedModeButton.TabIndex = 12;
            this.GuidedModeButton.Text = "Guided Mode";
            this.GuidedModeButton.UseVisualStyleBackColor = true;
            this.GuidedModeButton.Click += new System.EventHandler(this.GuidedModeButton_Click);
            // 
            // StopFollowingShipButton
            // 
            this.StopFollowingShipButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.StopFollowingShipButton.Location = new System.Drawing.Point(1347, 75);
            this.StopFollowingShipButton.Name = "StopFollowingShipButton";
            this.StopFollowingShipButton.Size = new System.Drawing.Size(243, 66);
            this.StopFollowingShipButton.TabIndex = 14;
            this.StopFollowingShipButton.Text = "Stop Following";
            this.StopFollowingShipButton.UseVisualStyleBackColor = true;
            this.StopFollowingShipButton.Click += new System.EventHandler(this.StopFollowingShipButton_Click);
            // 
            // GSpeedLabel
            // 
            this.GSpeedLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GSpeedLabel.Location = new System.Drawing.Point(150, 0);
            this.GSpeedLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this.GSpeedLabel.Name = "GSpeedLabel";
            this.GSpeedLabel.Size = new System.Drawing.Size(150, 194);
            this.GSpeedLabel.TabIndex = 15;
            this.GSpeedLabel.Text = "Ground Speed";
            this.GSpeedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ArmStatusBox
            // 
            this.ArmStatusBox.AllowDrop = true;
            this.ArmStatusBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ArmStatusBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ArmStatusBox.Location = new System.Drawing.Point(738, 75);
            this.ArmStatusBox.Name = "ArmStatusBox";
            this.ArmStatusBox.Size = new System.Drawing.Size(239, 27);
            this.ArmStatusBox.TabIndex = 6;
            this.ArmStatusBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ArmStatusBox.TextChanged += new System.EventHandler(this.ArmStatusBox_TextChanged);
            // 
            // TakeOffButton
            // 
            this.TakeOffButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TakeOffButton.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.TakeOffButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.TakeOffButton.Cursor = System.Windows.Forms.Cursors.Default;
            this.TakeOffButton.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.TakeOffButton.Location = new System.Drawing.Point(984, 5);
            this.TakeOffButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.TakeOffButton.Name = "TakeOffButton";
            this.TakeOffButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.TakeOffButton.Size = new System.Drawing.Size(356, 62);
            this.TakeOffButton.TabIndex = 7;
            this.TakeOffButton.Text = "TAKE OFF";
            this.TakeOffButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.TakeOffButton.UseVisualStyleBackColor = false;
            this.TakeOffButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // but_armdisarm
            // 
            this.but_armdisarm.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.but_armdisarm.BackColor = System.Drawing.Color.Yellow;
            this.but_armdisarm.Location = new System.Drawing.Point(741, 6);
            this.but_armdisarm.Margin = new System.Windows.Forms.Padding(6);
            this.but_armdisarm.Name = "but_armdisarm";
            this.but_armdisarm.Size = new System.Drawing.Size(233, 60);
            this.but_armdisarm.TabIndex = 3;
            this.but_armdisarm.Text = "Arm/Disarm";
            this.but_armdisarm.UseVisualStyleBackColor = false;
            this.but_armdisarm.Click += new System.EventHandler(this.but_armdisarm_Click);
            // 
            // but_mission
            // 
            this.but_mission.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.but_mission.Location = new System.Drawing.Point(251, 78);
            this.but_mission.Margin = new System.Windows.Forms.Padding(6);
            this.but_mission.Name = "but_mission";
            this.but_mission.Size = new System.Drawing.Size(233, 60);
            this.but_mission.TabIndex = 4;
            this.but_mission.Text = "Send Mission";
            this.but_mission.UseVisualStyleBackColor = true;
            this.but_mission.Click += new System.EventHandler(this.but_mission_Click);
            // 
            // ShipFollowingModeLabel
            // 
            this.ShipFollowingModeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ShipFollowingModeLabel.Location = new System.Drawing.Point(1347, 0);
            this.ShipFollowingModeLabel.Name = "ShipFollowingModeLabel";
            this.ShipFollowingModeLabel.Size = new System.Drawing.Size(243, 72);
            this.ShipFollowingModeLabel.TabIndex = 13;
            this.ShipFollowingModeLabel.Text = "Ship Following";
            this.ShipFollowingModeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // but_connect
            // 
            this.but_connect.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.but_connect.Location = new System.Drawing.Point(251, 6);
            this.but_connect.Margin = new System.Windows.Forms.Padding(6);
            this.but_connect.Name = "but_connect";
            this.but_connect.Size = new System.Drawing.Size(233, 60);
            this.but_connect.TabIndex = 2;
            this.but_connect.Text = "Connect";
            this.but_connect.UseVisualStyleBackColor = true;
            this.but_connect.Click += new System.EventHandler(this.but_connect_Click);
            // 
            // cmb_baudrate
            // 
            this.cmb_baudrate.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmb_baudrate.FormattingEnabled = true;
            this.cmb_baudrate.ItemHeight = 25;
            this.cmb_baudrate.Items.AddRange(new object[] {
            "9600",
            "14400",
            "19200",
            "28800",
            "38400",
            "57600",
            "115200"});
            this.cmb_baudrate.Location = new System.Drawing.Point(6, 78);
            this.cmb_baudrate.Margin = new System.Windows.Forms.Padding(6);
            this.cmb_baudrate.Name = "cmb_baudrate";
            this.cmb_baudrate.Size = new System.Drawing.Size(233, 33);
            this.cmb_baudrate.TabIndex = 1;
            this.cmb_baudrate.SelectedIndexChanged += new System.EventHandler(this.cmb_baudrate_SelectedIndexChanged);
            // 
            // CMB_comport
            // 
            this.CMB_comport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CMB_comport.FormattingEnabled = true;
            this.CMB_comport.Location = new System.Drawing.Point(6, 6);
            this.CMB_comport.Margin = new System.Windows.Forms.Padding(6);
            this.CMB_comport.Name = "CMB_comport";
            this.CMB_comport.Size = new System.Drawing.Size(233, 33);
            this.CMB_comport.TabIndex = 0;
            this.CMB_comport.SelectedIndexChanged += new System.EventHandler(this.CMB_comport_SelectedIndexChanged);
            this.CMB_comport.Click += new System.EventHandler(this.CMB_comport_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.tableLayoutPanel1.ColumnCount = 9;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.66456F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.66455F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.66455F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.66455F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18.77743F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.878F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17.68635F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.tableLayoutPanel1.Controls.Add(this.but_connect, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.AltHoldButton, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.TakeOffButton, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.GuidedModeButton, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.StopFollowingShipButton, 6, 1);
            this.tableLayoutPanel1.Controls.Add(this.ShipFollowingModeLabel, 6, 0);
            this.tableLayoutPanel1.Controls.Add(this.ArmStatusBox, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.CMB_comport, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.cmb_baudrate, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.but_mission, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.ShipInfoLabel, 7, 0);
            this.tableLayoutPanel1.Controls.Add(this.but_armdisarm, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.LandButton, 4, 1);
            this.tableLayoutPanel1.Controls.Add(this.pictureBox1, 8, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(2241, 144);
            this.tableLayoutPanel1.TabIndex = 16;
            this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel2.Controls.Add(this.GSpeedLabel, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.AltimeterBox, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.DroneMode, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Right;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(1935, 144);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.tableLayoutPanel2.RowCount = 6;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66666F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(306, 1170);
            this.tableLayoutPanel2.TabIndex = 17;
            this.tableLayoutPanel2.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel2_Paint);
            // 
            // ShipInfoLabel
            // 
            this.ShipInfoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ShipInfoLabel.AutoSize = true;
            this.ShipInfoLabel.BackColor = System.Drawing.SystemColors.InfoText;
            this.ShipInfoLabel.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ShipInfoLabel.Location = new System.Drawing.Point(1596, 0);
            this.ShipInfoLabel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.ShipInfoLabel.Name = "ShipInfoLabel";
            this.tableLayoutPanel1.SetRowSpan(this.ShipInfoLabel, 2);
            this.ShipInfoLabel.Size = new System.Drawing.Size(340, 144);
            this.ShipInfoLabel.TabIndex = 15;
            this.ShipInfoLabel.Text = "AIS Ship Information";
            this.ShipInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.ShipInfoLabel.Click += new System.EventHandler(this.label1_Click);
            // 
            // LandButton
            // 
            this.LandButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LandButton.BackColor = System.Drawing.Color.Lime;
            this.LandButton.Location = new System.Drawing.Point(983, 75);
            this.LandButton.Name = "LandButton";
            this.LandButton.Size = new System.Drawing.Size(358, 66);
            this.LandButton.TabIndex = 16;
            this.LandButton.Text = "LAND";
            this.LandButton.UseVisualStyleBackColor = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pictureBox1.Image = global::IERAX_MissionControl.Properties.Resources.ieraxlogo;
            this.pictureBox1.Location = new System.Drawing.Point(1936, 0);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBox1.MaximumSize = new System.Drawing.Size(300, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.tableLayoutPanel1.SetRowSpan(this.pictureBox1, 2);
            this.pictureBox1.Size = new System.Drawing.Size(300, 144);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // AltimeterBox
            // 
            this.AltimeterBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AltimeterBox.AutoSize = true;
            this.AltimeterBox.Location = new System.Drawing.Point(0, 0);
            this.AltimeterBox.Margin = new System.Windows.Forms.Padding(0);
            this.AltimeterBox.Name = "AltimeterBox";
            this.AltimeterBox.Size = new System.Drawing.Size(150, 194);
            this.AltimeterBox.TabIndex = 16;
            this.AltimeterBox.Text = "Altimeter";
            this.AltimeterBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // DroneMode
            // 
            this.DroneMode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DroneMode.AutoSize = true;
            this.DroneMode.Location = new System.Drawing.Point(3, 194);
            this.DroneMode.Name = "DroneMode";
            this.DroneMode.Size = new System.Drawing.Size(144, 195);
            this.DroneMode.TabIndex = 17;
            this.DroneMode.Text = "Flight Controller Mode";
            this.DroneMode.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MPIeraxMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2241, 1314);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.gMapControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "MPIeraxMain";
            this.Text = "IERAX MISSION CONTROL";
            this.Load += new System.EventHandler(this.MPIeraxMain_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.IO.Ports.SerialPort serialPort1;
        private GMap.NET.WindowsForms.GMapControl gMapControl1;
        private System.Windows.Forms.Button AltHoldButton;
        private System.Windows.Forms.Button GuidedModeButton;
        private System.Windows.Forms.Button StopFollowingShipButton;
        private System.Windows.Forms.Label GSpeedLabel;
        private System.Windows.Forms.TextBox ArmStatusBox;
        private System.Windows.Forms.Button TakeOffButton;
        private System.Windows.Forms.Button but_armdisarm;
        private System.Windows.Forms.Button but_mission;
        private System.Windows.Forms.Label ShipFollowingModeLabel;
        private System.Windows.Forms.Button but_connect;
        private System.Windows.Forms.ComboBox cmb_baudrate;
        private System.Windows.Forms.ComboBox CMB_comport;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label ShipInfoLabel;
        private System.Windows.Forms.Button LandButton;
        private System.Windows.Forms.Label AltimeterBox;
        private System.Windows.Forms.Label DroneMode;
        public System.Windows.Forms.PictureBox pictureBox1;
    }
}

