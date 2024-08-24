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
            this.but_armdisarm = new System.Windows.Forms.Button();
            this.but_mission = new System.Windows.Forms.Button();
            this.ShipFollowingModeLabel = new System.Windows.Forms.Label();
            this.but_connect = new System.Windows.Forms.Button();
            this.cmb_baudrate = new System.Windows.Forms.ComboBox();
            this.CMB_comport = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.TakeOffButton = new System.Windows.Forms.Button();
            this.ShipInfoLabel = new System.Windows.Forms.Label();
            this.LandButton = new System.Windows.Forms.Button();
            this.RTLButton = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.HeadingLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.DistanceToWPLabel = new System.Windows.Forms.Label();
            this.TimeToWPLabel = new System.Windows.Forms.Label();
            this.DroneMode = new System.Windows.Forms.Label();
            this.AltimeterBox = new System.Windows.Forms.Label();
            this.ASpeedLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.BatteryRemainingLabel = new System.Windows.Forms.Label();
            this.WindDirectionLabel = new System.Windows.Forms.Label();
            this.WindSpeedLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
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
            this.gMapControl1.Location = new System.Drawing.Point(0, 96);
            this.gMapControl1.Margin = new System.Windows.Forms.Padding(4);
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
            this.AltHoldButton.Location = new System.Drawing.Point(370, 49);
            this.AltHoldButton.Name = "AltHoldButton";
            this.AltHoldButton.Size = new System.Drawing.Size(178, 40);
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
            this.GuidedModeButton.Location = new System.Drawing.Point(370, 3);
            this.GuidedModeButton.Name = "GuidedModeButton";
            this.GuidedModeButton.Size = new System.Drawing.Size(178, 40);
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
            this.StopFollowingShipButton.Location = new System.Drawing.Point(921, 48);
            this.StopFollowingShipButton.Margin = new System.Windows.Forms.Padding(2);
            this.StopFollowingShipButton.Name = "StopFollowingShipButton";
            this.StopFollowingShipButton.Size = new System.Drawing.Size(180, 42);
            this.StopFollowingShipButton.TabIndex = 14;
            this.StopFollowingShipButton.Text = "Stop Following";
            this.StopFollowingShipButton.UseVisualStyleBackColor = true;
            this.StopFollowingShipButton.Click += new System.EventHandler(this.StopFollowingShipButton_Click);
            // 
            // GSpeedLabel
            // 
            this.GSpeedLabel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.GSpeedLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GSpeedLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.GSpeedLabel.Location = new System.Drawing.Point(100, 50);
            this.GSpeedLabel.Margin = new System.Windows.Forms.Padding(0);
            this.GSpeedLabel.Name = "GSpeedLabel";
            this.GSpeedLabel.Size = new System.Drawing.Size(100, 74);
            this.GSpeedLabel.TabIndex = 15;
            this.GSpeedLabel.Text = "Ground Speed";
            this.GSpeedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.GSpeedLabel.Click += new System.EventHandler(this.GSpeedLabel_Click);
            // 
            // ArmStatusBox
            // 
            this.ArmStatusBox.AllowDrop = true;
            this.ArmStatusBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ArmStatusBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ArmStatusBox.Location = new System.Drawing.Point(553, 48);
            this.ArmStatusBox.Margin = new System.Windows.Forms.Padding(2);
            this.ArmStatusBox.Name = "ArmStatusBox";
            this.ArmStatusBox.Size = new System.Drawing.Size(180, 27);
            this.ArmStatusBox.TabIndex = 6;
            this.ArmStatusBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ArmStatusBox.TextChanged += new System.EventHandler(this.ArmStatusBox_TextChanged);
            // 
            // but_armdisarm
            // 
            this.but_armdisarm.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.but_armdisarm.BackColor = System.Drawing.Color.Yellow;
            this.but_armdisarm.Location = new System.Drawing.Point(555, 4);
            this.but_armdisarm.Margin = new System.Windows.Forms.Padding(4);
            this.but_armdisarm.Name = "but_armdisarm";
            this.but_armdisarm.Size = new System.Drawing.Size(176, 38);
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
            this.but_mission.Location = new System.Drawing.Point(187, 50);
            this.but_mission.Margin = new System.Windows.Forms.Padding(4);
            this.but_mission.Name = "but_mission";
            this.but_mission.Size = new System.Drawing.Size(176, 38);
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
            this.ShipFollowingModeLabel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.ShipFollowingModeLabel.Location = new System.Drawing.Point(102, 422);
            this.ShipFollowingModeLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.ShipFollowingModeLabel.Name = "ShipFollowingModeLabel";
            this.ShipFollowingModeLabel.Size = new System.Drawing.Size(96, 74);
            this.ShipFollowingModeLabel.TabIndex = 13;
            this.ShipFollowingModeLabel.Text = "Ship Following";
            this.ShipFollowingModeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // but_connect
            // 
            this.but_connect.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.but_connect.Location = new System.Drawing.Point(187, 4);
            this.but_connect.Margin = new System.Windows.Forms.Padding(4);
            this.but_connect.Name = "but_connect";
            this.but_connect.Size = new System.Drawing.Size(176, 38);
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
            this.cmb_baudrate.ItemHeight = 16;
            this.cmb_baudrate.Items.AddRange(new object[] {
            "9600",
            "14400",
            "19200",
            "28800",
            "38400",
            "57600",
            "115200"});
            this.cmb_baudrate.Location = new System.Drawing.Point(4, 50);
            this.cmb_baudrate.Margin = new System.Windows.Forms.Padding(4);
            this.cmb_baudrate.Name = "cmb_baudrate";
            this.cmb_baudrate.Size = new System.Drawing.Size(175, 24);
            this.cmb_baudrate.TabIndex = 1;
            this.cmb_baudrate.SelectedIndexChanged += new System.EventHandler(this.cmb_baudrate_SelectedIndexChanged);
            // 
            // CMB_comport
            // 
            this.CMB_comport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CMB_comport.FormattingEnabled = true;
            this.CMB_comport.Location = new System.Drawing.Point(4, 4);
            this.CMB_comport.Margin = new System.Windows.Forms.Padding(4);
            this.CMB_comport.Name = "CMB_comport";
            this.CMB_comport.Size = new System.Drawing.Size(175, 24);
            this.CMB_comport.TabIndex = 0;
            this.CMB_comport.SelectedIndexChanged += new System.EventHandler(this.CMB_comport_SelectedIndexChanged);
            this.CMB_comport.Click += new System.EventHandler(this.CMB_comport_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.tableLayoutPanel1.ColumnCount = 8;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28572F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28572F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28572F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28572F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28572F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28572F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 206F));
            this.tableLayoutPanel1.Controls.Add(this.but_connect, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.AltHoldButton, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.TakeOffButton, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.GuidedModeButton, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.StopFollowingShipButton, 5, 1);
            this.tableLayoutPanel1.Controls.Add(this.ArmStatusBox, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.CMB_comport, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.cmb_baudrate, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.but_mission, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.ShipInfoLabel, 6, 0);
            this.tableLayoutPanel1.Controls.Add(this.but_armdisarm, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.LandButton, 4, 1);
            this.tableLayoutPanel1.Controls.Add(this.RTLButton, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.pictureBox1, 7, 1);
            this.tableLayoutPanel1.Controls.Add(this.pictureBox2, 7, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 0F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1494, 92);
            this.tableLayoutPanel1.TabIndex = 16;
            this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
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
            this.TakeOffButton.Location = new System.Drawing.Point(738, 3);
            this.TakeOffButton.Name = "TakeOffButton";
            this.TakeOffButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.TakeOffButton.Size = new System.Drawing.Size(178, 40);
            this.TakeOffButton.TabIndex = 7;
            this.TakeOffButton.Text = "TAKE OFF";
            this.TakeOffButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.TakeOffButton.UseVisualStyleBackColor = false;
            this.TakeOffButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // ShipInfoLabel
            // 
            this.ShipInfoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ShipInfoLabel.AutoSize = true;
            this.ShipInfoLabel.BackColor = System.Drawing.SystemColors.InfoText;
            this.ShipInfoLabel.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ShipInfoLabel.Location = new System.Drawing.Point(1105, 0);
            this.ShipInfoLabel.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.ShipInfoLabel.Name = "ShipInfoLabel";
            this.tableLayoutPanel1.SetRowSpan(this.ShipInfoLabel, 2);
            this.ShipInfoLabel.Size = new System.Drawing.Size(182, 92);
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
            this.LandButton.Location = new System.Drawing.Point(737, 48);
            this.LandButton.Margin = new System.Windows.Forms.Padding(2);
            this.LandButton.Name = "LandButton";
            this.LandButton.Size = new System.Drawing.Size(180, 42);
            this.LandButton.TabIndex = 16;
            this.LandButton.Text = "LAND";
            this.LandButton.UseVisualStyleBackColor = false;
            // 
            // RTLButton
            // 
            this.RTLButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.RTLButton.BackColor = System.Drawing.SystemColors.Highlight;
            this.RTLButton.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.RTLButton.Location = new System.Drawing.Point(922, 3);
            this.RTLButton.Name = "RTLButton";
            this.RTLButton.Size = new System.Drawing.Size(178, 40);
            this.RTLButton.TabIndex = 17;
            this.RTLButton.Text = "RETURN TO LAUNCH";
            this.RTLButton.UseVisualStyleBackColor = false;
            this.RTLButton.Click += new System.EventHandler(this.RTLButton_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pictureBox1.Image = global::IERAX_MissionControl.Properties.Resources.ieraxlogo;
            this.pictureBox1.Location = new System.Drawing.Point(0, 92);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBox1.MaximumSize = new System.Drawing.Size(200, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(24, 0);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox2.Image = global::IERAX_MissionControl.Properties.Resources.ieraxlogo;
            this.pictureBox2.Location = new System.Drawing.Point(1290, 3);
            this.pictureBox2.Name = "pictureBox2";
            this.tableLayoutPanel1.SetRowSpan(this.pictureBox2, 2);
            this.pictureBox2.Size = new System.Drawing.Size(201, 86);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 18;
            this.pictureBox2.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel2.Controls.Add(this.WindSpeedLabel, 0, 11);
            this.tableLayoutPanel2.Controls.Add(this.WindDirectionLabel, 0, 11);
            this.tableLayoutPanel2.Controls.Add(this.label8, 1, 6);
            this.tableLayoutPanel2.Controls.Add(this.label7, 0, 6);
            this.tableLayoutPanel2.Controls.Add(this.label6, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.label5, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.HeadingLabel, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.label4, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.ShipFollowingModeLabel, 1, 7);
            this.tableLayoutPanel2.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.label2, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.GSpeedLabel, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.DistanceToWPLabel, 0, 5);
            this.tableLayoutPanel2.Controls.Add(this.TimeToWPLabel, 1, 5);
            this.tableLayoutPanel2.Controls.Add(this.DroneMode, 0, 7);
            this.tableLayoutPanel2.Controls.Add(this.AltimeterBox, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.ASpeedLabel, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label9, 1, 8);
            this.tableLayoutPanel2.Controls.Add(this.label, 0, 8);
            this.tableLayoutPanel2.Controls.Add(this.label11, 0, 10);
            this.tableLayoutPanel2.Controls.Add(this.label12, 1, 10);
            this.tableLayoutPanel2.Controls.Add(this.BatteryRemainingLabel, 0, 9);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Right;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(1290, 92);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.tableLayoutPanel2.RowCount = 12;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66666F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66666F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66666F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66666F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66666F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(204, 749);
            this.tableLayoutPanel2.TabIndex = 17;
            this.tableLayoutPanel2.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel2_Paint);
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label8.Location = new System.Drawing.Point(103, 372);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(94, 50);
            this.label8.TabIndex = 29;
            this.label8.Text = "EMPTY";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label7.Location = new System.Drawing.Point(3, 372);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(94, 50);
            this.label7.TabIndex = 28;
            this.label7.Text = "FLIGHT MODE";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label6.Location = new System.Drawing.Point(103, 248);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(94, 50);
            this.label6.TabIndex = 27;
            this.label6.Text = "TIME TO WP";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label5.Location = new System.Drawing.Point(3, 248);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(94, 50);
            this.label5.TabIndex = 26;
            this.label5.Text = "DISTANCE TO WP";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // HeadingLabel
            // 
            this.HeadingLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.HeadingLabel.AutoSize = true;
            this.HeadingLabel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.HeadingLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.HeadingLabel.Location = new System.Drawing.Point(100, 174);
            this.HeadingLabel.Margin = new System.Windows.Forms.Padding(0);
            this.HeadingLabel.Name = "HeadingLabel";
            this.HeadingLabel.Size = new System.Drawing.Size(100, 74);
            this.HeadingLabel.TabIndex = 25;
            this.HeadingLabel.Text = "Heading";
            this.HeadingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label4.Location = new System.Drawing.Point(103, 124);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(94, 50);
            this.label4.TabIndex = 24;
            this.label4.Text = "HEADING";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label3.Location = new System.Drawing.Point(3, 124);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(94, 50);
            this.label3.TabIndex = 23;
            this.label3.Text = "ALTIMETER";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label2.Location = new System.Drawing.Point(103, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 50);
            this.label2.TabIndex = 22;
            this.label2.Text = "GROUND SPEED";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // DistanceToWPLabel
            // 
            this.DistanceToWPLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DistanceToWPLabel.AutoSize = true;
            this.DistanceToWPLabel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.DistanceToWPLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.DistanceToWPLabel.Location = new System.Drawing.Point(2, 298);
            this.DistanceToWPLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.DistanceToWPLabel.Name = "DistanceToWPLabel";
            this.DistanceToWPLabel.Size = new System.Drawing.Size(96, 74);
            this.DistanceToWPLabel.TabIndex = 18;
            this.DistanceToWPLabel.Text = "Distance to WP";
            this.DistanceToWPLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TimeToWPLabel
            // 
            this.TimeToWPLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TimeToWPLabel.AutoSize = true;
            this.TimeToWPLabel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.TimeToWPLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.TimeToWPLabel.Location = new System.Drawing.Point(102, 298);
            this.TimeToWPLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.TimeToWPLabel.Name = "TimeToWPLabel";
            this.TimeToWPLabel.Size = new System.Drawing.Size(96, 74);
            this.TimeToWPLabel.TabIndex = 19;
            this.TimeToWPLabel.Text = "Time To WP";
            this.TimeToWPLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // DroneMode
            // 
            this.DroneMode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DroneMode.AutoSize = true;
            this.DroneMode.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.DroneMode.Location = new System.Drawing.Point(2, 422);
            this.DroneMode.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.DroneMode.Name = "DroneMode";
            this.DroneMode.Size = new System.Drawing.Size(96, 74);
            this.DroneMode.TabIndex = 17;
            this.DroneMode.Text = "Flight Controller Mode";
            this.DroneMode.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // AltimeterBox
            // 
            this.AltimeterBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AltimeterBox.AutoSize = true;
            this.AltimeterBox.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.AltimeterBox.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.AltimeterBox.Location = new System.Drawing.Point(0, 174);
            this.AltimeterBox.Margin = new System.Windows.Forms.Padding(0);
            this.AltimeterBox.Name = "AltimeterBox";
            this.AltimeterBox.Size = new System.Drawing.Size(100, 74);
            this.AltimeterBox.TabIndex = 16;
            this.AltimeterBox.Text = "Altimeter";
            this.AltimeterBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ASpeedLabel
            // 
            this.ASpeedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ASpeedLabel.AutoSize = true;
            this.ASpeedLabel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.ASpeedLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ASpeedLabel.Location = new System.Drawing.Point(2, 50);
            this.ASpeedLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.ASpeedLabel.Name = "ASpeedLabel";
            this.ASpeedLabel.Size = new System.Drawing.Size(96, 74);
            this.ASpeedLabel.TabIndex = 20;
            this.ASpeedLabel.Text = "AirSpeed";
            this.ASpeedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 50);
            this.label1.TabIndex = 21;
            this.label1.Text = "AIRSPEED";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label9.Location = new System.Drawing.Point(103, 496);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(94, 50);
            this.label9.TabIndex = 31;
            this.label9.Text = "EMPTY";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label
            // 
            this.label.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label.AutoSize = true;
            this.label.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label.Location = new System.Drawing.Point(3, 496);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(94, 50);
            this.label.TabIndex = 32;
            this.label.Text = "Battery";
            this.label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label.Click += new System.EventHandler(this.label10_Click);
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label11.Location = new System.Drawing.Point(3, 620);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(94, 50);
            this.label11.TabIndex = 33;
            this.label11.Text = "EMPTY";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label12.Location = new System.Drawing.Point(103, 620);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(94, 50);
            this.label12.TabIndex = 34;
            this.label12.Text = "EMPTY";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BatteryRemainingLabel
            // 
            this.BatteryRemainingLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BatteryRemainingLabel.AutoSize = true;
            this.BatteryRemainingLabel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.BatteryRemainingLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.BatteryRemainingLabel.Location = new System.Drawing.Point(0, 546);
            this.BatteryRemainingLabel.Margin = new System.Windows.Forms.Padding(0);
            this.BatteryRemainingLabel.Name = "BatteryRemainingLabel";
            this.BatteryRemainingLabel.Size = new System.Drawing.Size(100, 74);
            this.BatteryRemainingLabel.TabIndex = 35;
            this.BatteryRemainingLabel.Text = "Battery";
            this.BatteryRemainingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // WindDirectionLabel
            // 
            this.WindDirectionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.WindDirectionLabel.AutoSize = true;
            this.WindDirectionLabel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.WindDirectionLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.WindDirectionLabel.Location = new System.Drawing.Point(100, 670);
            this.WindDirectionLabel.Margin = new System.Windows.Forms.Padding(0);
            this.WindDirectionLabel.Name = "WindDirectionLabel";
            this.WindDirectionLabel.Size = new System.Drawing.Size(100, 79);
            this.WindDirectionLabel.TabIndex = 36;
            this.WindDirectionLabel.Text = "Wind Direction";
            this.WindDirectionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // WindSpeedLabel
            // 
            this.WindSpeedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.WindSpeedLabel.AutoSize = true;
            this.WindSpeedLabel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.WindSpeedLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.WindSpeedLabel.Location = new System.Drawing.Point(0, 670);
            this.WindSpeedLabel.Margin = new System.Windows.Forms.Padding(0);
            this.WindSpeedLabel.Name = "WindSpeedLabel";
            this.WindSpeedLabel.Size = new System.Drawing.Size(100, 79);
            this.WindSpeedLabel.TabIndex = 37;
            this.WindSpeedLabel.Text = "Wind Speed";
            this.WindSpeedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MPIeraxMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1494, 841);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.gMapControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MPIeraxMain";
            this.Text = "IERAX MISSION CONTROL";
            this.Load += new System.EventHandler(this.MPIeraxMain_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
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
        private System.Windows.Forms.Button but_armdisarm;
        private System.Windows.Forms.Button but_mission;
        private System.Windows.Forms.Label ShipFollowingModeLabel;
        private System.Windows.Forms.Button but_connect;
        private System.Windows.Forms.ComboBox cmb_baudrate;
        private System.Windows.Forms.ComboBox CMB_comport;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label AltimeterBox;
        private System.Windows.Forms.Label DroneMode;
        private System.Windows.Forms.Label DistanceToWPLabel;
        private System.Windows.Forms.Label TimeToWPLabel;
        private System.Windows.Forms.Label ASpeedLabel;
        public System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label HeadingLabel;
        private System.Windows.Forms.Button RTLButton;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button TakeOffButton;
        private System.Windows.Forms.Label ShipInfoLabel;
        private System.Windows.Forms.Button LandButton;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Label BatteryRemainingLabel;
        private System.Windows.Forms.Label WindSpeedLabel;
        private System.Windows.Forms.Label WindDirectionLabel;
    }
}

