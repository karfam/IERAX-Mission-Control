using GMap.NET.MapProviders;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Net.WebSockets;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IERAX_MissionControl;
using static GMap.NET.Entity.OpenStreetMapGraphHopperRouteEntity;
using IERAX_MissionControl.Properties;
using System.Net.Sockets;
using System.Data.Entity.Core.Metadata.Edm;
using System.Net;






namespace IERAX_MissionControl
{
    public partial class MPIeraxMain : Form
    {
        MAVLink.MavlinkParse mavlink = new MAVLink.MavlinkParse();
        bool armed = false;
        // locking to prevent multiple reads on serial port
        object readlock = new object();
        // our target sysid
        byte sysid;
        // our target compid
        byte compid;

        private GMapOverlay markersOverlay;
        private DroneMarker droneMarker;
        private ShipMarker shipMarker;
        private bool isMapCentered = false; // Flag to track if the map has been centered
        private ClientWebSocket ws;
        private Dictionary<string, GMapMarker> shipMarkers = new Dictionary<string, GMapMarker>();
       // private Label infoLabel;
        private bool isTcpConnection = false; // Set this based on the connection type
        private NetworkStream tcpStream; // Store the TCP stream for the connection
        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
       

        private Dictionary<string, AisData> cachedAisData = new Dictionary<string, AisData>();

        private CancellationTokenSource cts;

        private MavlinkMessageHandler mavlinkMessageHandler;

        private System.Windows.Forms.Timer flyToShipTimer;
        private ShipMarker targetShipMarker;
     


        public MPIeraxMain()
        {
            InitializeMavlinkHandler();
            InitializeComponent();
            InitializeMap();
            //InitializeInfoPanel();
            InitializeWebSocket();

            // Attach resize event
            //this.Resize += Form1_Resize;

        }


        private void gMapControl1_Load(object sender, EventArgs e)
        {

        }

        // Initialize the MavlinkMessageHandler with delegates to update the drone marker and arm status box
        private void InitializeMavlinkHandler()
        {
            // Initialize the MavlinkMessageHandler with delegates for updating the drone marker, arm status, and altimeter
            mavlinkMessageHandler = new MavlinkMessageHandler(UpdateDroneMarker, UpdateArmStatusBox, UpdateAltimeterBox,UpdateDroneModeTextBox,UpdateTextLabelGUI);
        }


        private void InitializeMap()
        {
            ConfigureMap();

            // Create a marker overlay and add it to the map
            markersOverlay = new GMapOverlay("markers");
            gMapControl1.Overlays.Add(markersOverlay);

            // Create the drone marker at a default position
            droneMarker = new DroneMarker(new PointLatLng(37.7128, 21.0060)); // Example coordinates
            markersOverlay.Markers.Add(droneMarker);

            // Refresh the map to ensure the marker is displayed
            gMapControl1.Refresh();

        }

     /*   private void Form1_Resize(object sender, EventArgs e)
        {
            PositionInfoPanel();
        }*/

 /*       private void InitializeInfoPanel()
        {
            // Initialize the panel
            infoPanel = new Panel
            {
                Size = new Size(200, 100),
                BackColor = Color.FromArgb(200, 255, 255, 255), // Semi-transparent white
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            // Initialize the label to display information
            infoLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10),
            };

            infoPanel.Controls.Add(infoLabel);

            // Add the panel to the form or gMapControl
            this.Controls.Add(infoPanel);

            // Position the panel in the top-right corner of the map
            PositionInfoPanel();
        }*/

   /*     private void PositionInfoPanel()
        {
            // Position the panel in the top-right corner of the gMapControl
            infoPanel.Location = new Point(gMapControl1.Width - infoPanel.Width - 10, 50);
            infoPanel.BringToFront();
        }*/

        private async void InitializeWebSocket()
        {
            ws = new ClientWebSocket();
            cts = new CancellationTokenSource();
            await ConnectWebSocket();
        }

        private async Task ConnectWebSocket()
        {
            try
            {

                double patrasLat = 38.2466;
                double patrasLon = 21.7346;
                double radiusNm = 50;

                var boundingBox = GeoUtils.CalculateBoundingBox(patrasLat, patrasLon, radiusNm);
                string boundingBoxString = GeoUtils.FormatBoundingBox(boundingBox.MinLat, boundingBox.MaxLat, boundingBox.MinLon, boundingBox.MaxLon);
                Console.WriteLine($"Bounding box: {boundingBoxString}");


                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;
                using (var ws = new ClientWebSocket())
                {
                    await ws.ConnectAsync(new Uri("wss://stream.aisstream.io/v0/stream"), token);
                    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"{{ \"APIKey\": \"beed537a556c9f2d4ad315da0381c2f5ee168aa1\", \"BoundingBoxes\": [{boundingBoxString}] }}")), WebSocketMessageType.Text, true, token);
                    byte[] buffer = new byte[4096];
                    while (ws.State == WebSocketState.Open)
                    {
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                        }
                        else
                        {
                            var message = Encoding.Default.GetString(buffer, 0, result.Count);
                            // Console.WriteLine($"Received {message}");
                            var aisData = JsonConvert.DeserializeObject<AisData>(message);
                            if (aisData != null)
                            {
                                UpdateShipPosition(aisData);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
        }
        private void ConfigureMap()
        {
            gMapControl1.MapProvider = GMapProviders.GoogleSatelliteMap;
            gMapControl1.Position = new PointLatLng(37.7128, 21.0060); // Example coordinates
            gMapControl1.MinZoom = 1;
            gMapControl1.MaxZoom = 20;
            gMapControl1.Zoom = 10;
            gMapControl1.Manager.Mode = AccessMode.ServerAndCache;
            gMapControl1.DragButton = MouseButtons.Left;

            // Set the GMapControl to fill the parent form
            gMapControl1.Dock = DockStyle.Fill;

            gMapControl1.MouseUp += new MouseEventHandler(gMapControl1_MouseUp);


            // Refresh the map to ensure the marker is displayed
            gMapControl1.Refresh();

        }

        private void MarkersOverlay_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (item is ShipMarker shipMarker)
            {
                ShowShipInfo(shipMarker);
            }
        }

        private void but_connect_Click(object sender, EventArgs e)
        {
            string selectedConnection = CMB_comport.Text;

            // Check if the selected option is an IP:Port format (indicating a TCP connection)
            if (selectedConnection.Contains(":"))
            {
                // Parse the IP and port
                string[] parts = selectedConnection.Split(':');
                string ip = parts[0];
                int port = int.Parse(parts[1]);

                // Handle TCP connection
                ConnectViaTCP(ip, port);
            }
            else
            {
                // Handle serial port connection as before

                // If the port is open, close it
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                    return;
                }

                // Set the comport options
                serialPort1.PortName = selectedConnection;
                serialPort1.BaudRate = int.Parse(cmb_baudrate.Text);

                // Open the comport
                serialPort1.Open();

                // Set timeout to 2 seconds
                serialPort1.ReadTimeout = 2000;

                // Start a background worker for handling the connection
                BackgroundWorker bgw = new BackgroundWorker();
                bgw.DoWork += bgw_DoWork;
                bgw.RunWorkerAsync();
            }
        }

        private void ConnectViaTCP(string ip, int port)
        {
            try
            {
                TcpClient client = new TcpClient(ip, port);
                tcpStream = client.GetStream();
                isTcpConnection = true;

                Task.Run(() => HandleMavlinkMessages(tcpStream));

                Console.WriteLine($"Connected to drone at {ip}:{port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to {ip}:{port} - {ex.Message}");
            }
        }


        private void HandleMavlinkMessages(NetworkStream stream)
        {
            MAVLink.MavlinkParse mavlink = new MAVLink.MavlinkParse();

            while (true)
            {
                try
                {
                    MAVLink.MAVLinkMessage message = mavlink.ReadPacket(stream);

                    if (message != null)
                    {
                        // Check if the message's sysid is 1
                        if (message.sysid == 1)
                        {
                           //Console.WriteLine($"Received MAVLink message: msgid={message.msgid}, sysid={message.sysid}, compid={message.compid}, msgtypename={message.msgtypename}");

                            // Handle known messages
                            mavlinkMessageHandler.HandleMavlinkMessage(message);
                        }
                        else
                        {
                           // Console.WriteLine($"Ignored message from sysid={message.sysid}, expected sysid=1");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Received null message.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving MAVLink message: {ex.Message}");
                    break;
                }
            }
        }





        private void UpdateDroneMarker(PointLatLng position)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<PointLatLng>(UpdateDroneMarker), position);
            }
            else
            {
                if (droneMarker == null)
                {
                    // Create the drone marker if it doesn't exist
                    droneMarker = new DroneMarker(position);
                    markersOverlay.Markers.Add(droneMarker);
                }
                else
                {
                    // Update the position of the existing drone marker
                    droneMarker.Position = position;
                }

                // Optionally, refresh the overlay to ensure the marker is rendered correctly
                markersOverlay.IsVisibile = false;
                markersOverlay.IsVisibile = true;
            }
        }





        //EDO EXOUME TON ASYNC WORKER
        void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (serialPort1.IsOpen)
            {
                try
                {
                    MAVLink.MAVLinkMessage packet;
                    lock (readlock)
                    {
                        // read any valid packet from the port
                        packet = mavlink.ReadPacket(serialPort1.BaseStream);

                        // check its valid
                        if (packet == null || packet.data == null)
                            continue;
                    }

                    // check to see if its a hb packet from the comport
                    if (packet.data.GetType() == typeof(MAVLink.mavlink_heartbeat_t))
                    {
                        var hb = (MAVLink.mavlink_heartbeat_t)packet.data;

                        // save the sysid and compid of the seen MAV
                        sysid = packet.sysid;
                        compid = packet.compid;

                        // request streams at 2 hz
                        var buffer = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM,
                            new MAVLink.mavlink_request_data_stream_t()
                            {
                                req_message_rate = 2,
                                req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.ALL,
                                start_stop = 1,
                                target_component = compid,
                                target_system = sysid
                            });

                        serialPort1.Write(buffer, 0, buffer.Length);

                        buffer = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.HEARTBEAT, hb);

                        serialPort1.Write(buffer, 0, buffer.Length);
                    }

                    //AN PARO THESI TOTE KANO UPDATE TON XARTI
                    if (packet.msgid == (byte)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT)
                    {
                        var position = (MAVLink.mavlink_global_position_int_t)packet.data;
                        UpdateMapPosition(position);
                    }

                    // from here we should check the the message is addressed to us
                    if (sysid != packet.sysid || compid != packet.compid)
                        continue;

                    Console.WriteLine(packet.msgtypename);

                    if (packet.msgid == (byte)MAVLink.MAVLINK_MSG_ID.ATTITUDE)
                    //or
                    //if (packet.data.GetType() == typeof(MAVLink.mavlink_attitude_t))
                    {
                        var att = (MAVLink.mavlink_attitude_t)packet.data;

                        //Console.WriteLine(att.pitch*57.2958 + " " + att.roll*57.2958);
                    }
                }
                catch
                {
                }

                System.Threading.Thread.Sleep(1);
            }
        }

        T readsomedata<T>(byte sysid, byte compid, int timeout = 2000)
        {
            DateTime deadline = DateTime.Now.AddMilliseconds(timeout);

            lock (readlock)
            {
                // read the current buffered bytes
                while (DateTime.Now < deadline)
                {
                    var packet = mavlink.ReadPacket(tcpStream);

                    // check its not null, and its addressed to us
                    if (packet == null || sysid != packet.sysid || compid != packet.compid)
                        continue;

                    if (packet.data.GetType() == typeof(T))
                    {
                        return (T)packet.data;
                    }
                }
            }

            throw new Exception("No packet match found");
        }


        private void HandleUnexpectedPacket(MAVLink.MAVLinkMessage packet)
        {
            // Log or process unexpected packets
            Console.WriteLine($"Handling unexpected packet with msgid={packet.msgid}, msgtypename={packet.msgtypename}");
            // Depending on your needs, you could add more specific handling here
        }




        private void but_armdisarm_Click(object sender, EventArgs e)
        {
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();

            // Set the correct target system and component IDs
            req.target_system = 1; // Assuming sysid=1 is the correct ID
            req.target_component = 1; // Assuming compid=1 is the correct ID

            // ARM or DISARM command
            req.command = (ushort)MAVLink.MAV_CMD.COMPONENT_ARM_DISARM;
            req.param1 = armed ? 0 : 1; // 0 to disarm, 1 to arm
            armed = !armed;

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            // Send the packet using the appropriate connection type
            SendPacket(packet);
            System.Threading.Thread.Sleep(100);
  
        }

        private void UpdateArmStatusBox(bool isArmed)
        {
            if (ArmStatusBox.InvokeRequired)
            {   

                ArmStatusBox.Invoke(new Action(() =>
                {
                    ArmStatusBox.Text = isArmed ? "Armed" : "Disarmed";
                    ArmStatusBox.BackColor = isArmed ? Color.Red : Color.Green;
                }));
            }
            else
            {
                ArmStatusBox.Text = isArmed ? "Armed" : "Disarmed";
                ArmStatusBox.BackColor = isArmed ? Color.Red : Color.Green;
            }
        }





        private void CMB_comport_Click(object sender, EventArgs e)
        {
            PopulateConnectionList();
        }

        private void PopulateConnectionList()
        {
            // Get available serial ports
            var serialPorts = SerialPort.GetPortNames().ToList();

            // Add the TCP connection option
            serialPorts.Add("192.168.3.224:5760");
            serialPorts.Add("127.0.0.1:5760");
            serialPorts.Add("127.0.0.1:5762");

            // Set the DataSource of the ComboBox to the updated list
            CMB_comport.DataSource = serialPorts;

            // Set default selection if needed
            if (CMB_comport.Items.Count > 0)
            {
                CMB_comport.SelectedIndex = 0;
            }
        }

        private void but_mission_Click(object sender, EventArgs e)
        {
            MAVLink.mavlink_mission_count_t req = new MAVLink.mavlink_mission_count_t();

            req.target_system = 1;
            req.target_component = 1;

            // set wp count
            req.count = 1;

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_COUNT, req);
            Console.WriteLine("MISSION_COUNT send");
            serialPort1.Write(packet, 0, packet.Length);

            var ack = readsomedata<MAVLink.mavlink_mission_request_t>(sysid, compid);
            if (ack.seq == 0)
            {
                MAVLink.mavlink_mission_item_int_t req2 = new MAVLink.mavlink_mission_item_int_t();

                req2.target_system = sysid;
                req2.target_component = compid;

                req2.command = (byte)MAVLink.MAV_CMD.WAYPOINT;

                req2.current = 1;
                req2.autocontinue = 0;

                req2.frame = (byte)MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT;

                req2.y = (int)(115 * 1.0e7);
                req2.x = (int)(-35 * 1.0e7);

                req2.z = (float)(2.34);

                req2.param1 = 0;
                req2.param2 = 0;
                req2.param3 = 0;
                req2.param4 = 0;

                req2.seq = 0;

                packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT, req2);
                Console.WriteLine("MISSION_ITEM_INT send");
                lock (readlock)
                {
                    serialPort1.Write(packet, 0, packet.Length);

                    var ack2 = readsomedata<MAVLink.mavlink_mission_ack_t>(sysid, compid);
                    if ((MAVLink.MAV_MISSION_RESULT)ack2.type != MAVLink.MAV_MISSION_RESULT.MAV_MISSION_ACCEPTED)
                    {

                    }
                }


                MAVLink.mavlink_mission_ack_t req3 = new MAVLink.mavlink_mission_ack_t();
                req3.target_system = 1;
                req3.target_component = 1;
                req3.type = 0;

                packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ACK, req3);
                Console.WriteLine("MISSION_ACK send");
                serialPort1.Write(packet, 0, packet.Length);
            }
        }


        private void UpdateMapPosition(MAVLink.mavlink_global_position_int_t position)
        {
            Invoke(new Action(() =>
            {
                double lat = position.lat / 1e7;
                double lng = position.lon / 1e7;
                PointLatLng point = new PointLatLng(lat, lng);

                // Center the map only the first time
                if (!isMapCentered)
                {
                    gMapControl1.Position = point;
                    isMapCentered = true;

                    Console.WriteLine("Initializing Web Socket");
                    Console.WriteLine($"Map centered at lat: {lat}, lng: {lng}");
                }

                // Update the drone marker's position
                droneMarker.Position = point;

                // Refresh the overlay to ensure the marker is rendered on top
                markersOverlay.IsVisibile = true;
                markersOverlay.IsVisibile = false;
                markersOverlay.IsVisibile = true;

            }));
        }



        private void gMapControl1_MouseUp(object sender, MouseEventArgs e)
        {
            ShipMarker clickedShipMarker = null;

            // Convert the mouse click position to the map's coordinate system
            var clickLocation = gMapControl1.FromLocalToLatLng(e.X, e.Y);

            foreach (var marker in markersOverlay.Markers)
            {
                // Calculate the size of the marker on the map
                var markerSize = new Size(50, 50); // Assuming the marker's size is 50x50
                var markerScreenPosition = gMapControl1.FromLatLngToLocal(marker.Position);

                Rectangle markerRect = new Rectangle(
                    (int)(markerScreenPosition.X - markerSize.Width / 2),
                    (int)(markerScreenPosition.Y - markerSize.Height / 2),
                    markerSize.Width,
                    markerSize.Height
                );

                // Check if the click was within the marker's area
                if (markerRect.Contains(e.Location))
                {
                    if (marker is ShipMarker shipMarker)
                    {
                        clickedShipMarker = shipMarker;
                    }
                    break; // Exit the loop after finding the clicked marker
                }
            }

            if (e.Button == MouseButtons.Left)
            {
                // Handle left click
                if (clickedShipMarker != null)
                {
                    ShowShipInfo(clickedShipMarker);
                }
            }

            if (e.Button == MouseButtons.Right)
            {
                // Handle right click
                if (clickedShipMarker != null)
                {
                    // Show context menu with "Fly to [ShipName]" option
                    ShowRightClickMenu(clickedShipMarker.Position, e.Location, clickedShipMarker);

                    // Show ship info as well
                    ShowShipInfo(clickedShipMarker);
                }
                else
                {
                    // Get the latitude and longitude of the point clicked
                    PointLatLng point = gMapControl1.FromLocalToLatLng(e.X, e.Y);

                    // Show the default right-click menu
                    ShowRightClickMenu(point, e.Location);
                }
            }
        }


        private void ShowRightClickMenu(PointLatLng point, Point location, ShipMarker clickedMarker = null)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            if (clickedMarker != null)
            {
                // If the right-click was on a ship marker, add the "Fly to [ShipName]" option
                contextMenu.Items.Add($"Fly to {clickedMarker.ShipName}", null, (s, e) => FlyToShip(clickedMarker));

                // Add "Intercept [ShipName]" option
                contextMenu.Items.Add($"Intercept {clickedMarker.ShipName}", null, (s, e) => InterceptShip(clickedMarker));

                // Optionally, you can still show the coordinates of the ship
                contextMenu.Items.Add($"Latitude: {clickedMarker.Position.Lat:F6}, Longitude: {clickedMarker.Position.Lng:F6}", null);
            }
            else
            {
                // Add a menu item for showing the coordinates
                contextMenu.Items.Add($"Latitude: {point.Lat:F6}, Longitude: {point.Lng:F6}", null);

                // Add "Fly to this location" item
                ToolStripMenuItem flyToMenuItem = new ToolStripMenuItem("Fly to this location");
                flyToMenuItem.Click += (sender, e) => FlyToLocation(point);
                contextMenu.Items.Add(flyToMenuItem);

                // Add "Land at this location" item
                ToolStripMenuItem landAtMenuItem = new ToolStripMenuItem("Land at this location");
                landAtMenuItem.Click += (sender, e) => LandAtLocation(point);
                contextMenu.Items.Add(landAtMenuItem);
            }

            // Show the context menu at the mouse position
            contextMenu.Show(gMapControl1, location);
        }




        private void FlyToLocation(PointLatLng point)
        {
            // Ensure the drone is in Guided mode
            SwitchToGuidedMode();

            // Convert latitude and longitude from double to int format required by FlyToWaypoint
            int lat_int = (int)(point.Lat * 1e7);
            int lon_int = (int)(point.Lng * 1e7);
            int alt_int = 50; // Set your desired altitude here (in centimeters)

            // Call the method to send the drone to the specified waypoint
            FlyToWaypoint(lat_int, lon_int, alt_int);
        }




        private void LandAtLocation(PointLatLng point)
        {
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();

            req.target_system = 1; // Set to your drone's system ID
            req.target_component = 1; // Set to your drone's component ID

            req.command = (ushort)MAVLink.MAV_CMD.LAND; // Command to land at a specific location
            req.param5 = (float)point.Lat; // Latitude
            req.param6 = (float)point.Lng; // Longitude
            req.param7 = 0.0f; // Final landing altitude (typically set to 0)

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            SendPacket(packet);
            System.Threading.Thread.Sleep(100); // Add a small delay if necessary
        }





        private void UpdateShipPosition(AisData aisData)
        {
            Invoke(new Action(() =>
            {
                try
                {
                    string mmsi = aisData.MetaData.MMSI;
                    string shipName = aisData.MetaData.ShipName;
                    double latitude = aisData.MetaData.Latitude;
                    double longitude = aisData.MetaData.Longitude;
                    double heading = 0;
                    double speed = 0;
                    string vesselType = "Unknown";

                    PointLatLng currentPosition = new PointLatLng(latitude, longitude);

                    // Check for StandardClassBPositionReport
                    if (aisData.Message.StandardClassBPositionReport != null)
                    {
                        var report = aisData.Message.StandardClassBPositionReport;

                        // Parse the COG, SOG, and TrueHeading
                        heading = report.Cog;
                        speed = report.Sog;

                        //Console.WriteLine($"Parsed COG for ship {mmsi}: {heading} degrees.");
                        // Console.WriteLine($"Parsed SOG for ship {mmsi}: {speed} knots.");

                        // Handle TrueHeading if it's not 511 (unavailable)
                        if (report.TrueHeading != 511)
                        {
                            heading = report.TrueHeading;
                            // Console.WriteLine($"Parsed TrueHeading for ship {mmsi}: {heading} degrees.");
                        }
                    }
                    // Check for PositionReport (or any other relevant message type)
                    else if (aisData.Message.PositionReport != null)
                    {
                        var report = aisData.Message.PositionReport;

                        // Parse the COG, SOG, and TrueHeading
                        heading = report.Cog;
                        speed = report.Sog;

                        // Console.WriteLine($"Parsed COG for ship {mmsi}: {heading} degrees.");
                        // Console.WriteLine($"Parsed SOG for ship {mmsi}: {speed} knots.");

                        // Handle TrueHeading if it's not 511 (unavailable)
                        if (report.TrueHeading != 511)
                        {
                            heading = report.TrueHeading;
                            //   Console.WriteLine($"Parsed TrueHeading for ship {mmsi}: {heading} degrees.");
                        }
                    }
                    else
                    {
                        // If no relevant position report is found, log a message
                        //   Console.WriteLine($"No recognized position report found for ship {mmsi}.");
                        return; // Exit the method early since there's no position data to update
                    }

                    // Proceed with the marker update as before
                    if (cachedAisData.ContainsKey(mmsi))
                    {
                        var shipMarker = shipMarkers[mmsi] as ShipMarker;
                        if (shipMarker != null && shipMarker.PreviousPosition.HasValue)
                        {
                            heading = CalculateBearing(shipMarker.PreviousPosition.Value, currentPosition);
                            // Console.WriteLine($"Calculating heading for {shipName} (MMSI: {mmsi}). Previous Position: Lat {shipMarker.PreviousPosition.Value.Lat}, Lng {shipMarker.PreviousPosition.Value.Lng}. Current Position: Lat {currentPosition.Lat}, Lng {currentPosition.Lng}. Calculated Heading: {heading} degrees");

                            shipMarker.PreviousPosition = currentPosition;
                        }
                    }
                    else
                    {
                        // If no cached data exists, this is the first position update
                        shipMarkers[mmsi] = new ShipMarker(currentPosition)
                        {
                            ToolTipText = shipName,
                            Tag = mmsi,
                            ShipName = shipName,
                            MMSI = mmsi,
                            Heading = heading,
                            Speed = speed,
                            VesselType = vesselType,
                            PreviousPosition = currentPosition
                        };
                        shipMarkers[mmsi].ToolTipMode = MarkerTooltipMode.OnMouseOver;
                        markersOverlay.Markers.Add(shipMarkers[mmsi]);
                    }

                    // Cache the latest AISData for future use
                    cachedAisData[mmsi] = aisData;

                    PointLatLng point = new PointLatLng(latitude, longitude);
                    // Console.WriteLine($"Updating ship {mmsi} marker at: {latitude}, {longitude}");

                    if (shipMarkers.ContainsKey(mmsi))
                    {
                        var shipMarker = shipMarkers[mmsi] as ShipMarker;
                        if (shipMarker != null)
                        {
                            HistoryMarker historyMarker = new HistoryMarker(shipMarker.Position);
                            shipMarker.HistoryMarkers.Add(historyMarker);

                            markersOverlay.Markers.Add(historyMarker);

                            DateTime tenMinutesAgo = DateTime.Now.AddMinutes(-10);
                            shipMarker.HistoryMarkers.RemoveAll(h => (DateTime.Now - tenMinutesAgo).TotalMinutes > 10);

                            shipMarker.Position = point;
                            shipMarker.Heading = heading;
                            shipMarker.Speed = speed;
                            shipMarker.VesselType = vesselType;

                            // Clear all existing projected markers from the overlay
                            foreach (var projectedMarker in shipMarker.ProjectedMarkers)
                            {
                                markersOverlay.Markers.Remove(projectedMarker);
                            }

                            // Update projected markers with new positions
                            shipMarker.UpdateProjectedMarkers();

                            // Add the new set of projected markers to the overlay
                            foreach (var projectedMarker in shipMarker.ProjectedMarkers)
                            {
                                markersOverlay.Markers.Add(projectedMarker);
                            }
                        }
                    }

                    markersOverlay.IsVisibile = true;
                    markersOverlay.IsVisibile = false;
                    markersOverlay.IsVisibile = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception encountered: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                }
            }));
        }


        private void ShowShipInfo(ShipMarker marker)
        {
            if (marker != null)
            {
                string info = $"Ship Name: {marker.ShipName}\n" +
                              $"MMSI: {marker.MMSI}\n" +
                              $"Heading: {(marker.Heading == 511 ? "Not Available" : $"{marker.Heading}°")}\n" +
                              $"Speed: {marker.Speed} knots\n" +
                              $"Vessel Type: {marker.VesselType}";

                // Display ship information in the info panel
                ShipInfoLabel.Text = info;
                ShipInfoLabel.Visible = true;

                // Draw a circle around the ship marker using an image
                DrawCircleImageAroundMarker(marker);
            }
        }

        private void DrawCircleImageAroundMarker(ShipMarker marker)
        {
            // Remove any existing circle markers before adding a new one
            foreach (var overlayMarker in markersOverlay.Markers.ToList())
            {
                if (overlayMarker.Tag != null && overlayMarker.Tag.ToString() == "Circle")
                {
                    markersOverlay.Markers.Remove(overlayMarker);
                }
            }

            // Load the circle image from resources or file
            Bitmap circleImage = new Bitmap(Resources.selection); // Replace with your actual image resource or path

            // Create a new marker using the circle image
            GMapMarker circleMarker = new GMapMarkerImage(marker.Position, circleImage)
            {
                Tag = "Circle"
            };

            // Add the circle marker to the overlay
            markersOverlay.Markers.Add(circleMarker);
        }

        private double CalculateBearing(PointLatLng start, PointLatLng end)
        {
            double lat1 = start.Lat * Math.PI / 180.0;
            double lat2 = end.Lat * Math.PI / 180.0;
            double dLon = (end.Lng - start.Lng) * Math.PI / 180.0;

            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
            double bearing = Math.Atan2(y, x) * 180.0 / Math.PI;

            // Normalize to 0-360 degrees
            return (bearing + 360.0) % 360.0;
        }

        private void CMB_comport_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cmb_baudrate_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            float targetAltitude = 50.0f; // Example altitude in meters
            SendTakeoffCommand(targetAltitude);
        }

        private void UpdateAltimeterBox(double altitude)
        {
            if (AltimeterBox.InvokeRequired)
            {
                AltimeterBox.Invoke(new Action(() => AltimeterBox.Text = $"{altitude:F2} m"));
            }
            else
            {
                AltimeterBox.Text = $"{altitude:F2} m";
            }
        }

        private void UpdateDroneModeTextBox(string flightMode)
        {
            if (this.DroneMode.InvokeRequired)
            {
                // If called from a different thread, use Invoke to switch to the UI thread
                this.DroneMode.Invoke(new Action(() => UpdateDroneModeTextBox(flightMode)));
            }
            else
            {
                // Update the TextBox with the flight mode
                this.DroneMode.Text = flightMode;
            }
        }

        public void UpdateTextLabelGUI (string labelName, string value)
        {
            // Find the label by name
            Label label = FindLabelByName(labelName);

            // Update the label with the provided value
            UpdateTextLabelAction(label, value);
        }

        public void UpdateTextLabelAction(Control control, string value)
        {
            if (control.InvokeRequired)
            {
                // If called from a different thread, use Invoke to switch to the UI thread
                control.Invoke(new Action(() => UpdateTextLabelAction(control, value)));
            }
            else
            {
                // Update the control with the provided value
                control.Text = value;
            }
        }


        private Label FindLabelByName(string labelName)
        {
            // Logic to find the label by name
            // This is just a placeholder; replace with your actual logic
            return this.Controls.Find(labelName, true).FirstOrDefault() as Label;
        }





        private void SendPacket(byte[] packet)
        {
            if (isTcpConnection && tcpStream != null)
            {
                tcpStream.Write(packet, 0, packet.Length);
                tcpStream.Flush();
                Console.WriteLine("Packet sent via TCP.");
            }
            else if (!isTcpConnection && serialPort1.IsOpen)
            {
                serialPort1.Write(packet, 0, packet.Length);
                Console.WriteLine("Packet sent via Serial.");
            }
            else
            {
                Console.WriteLine("No valid connection available. Cannot send packet.");
            }
        }



        private void SendTakeoffCommand(float targetAltitude)
        {
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();

            req.target_system = 1; // Set to your drone's system ID
            req.target_component = 1; // Set to your drone's component ID

            req.command = 22; // Numeric value for MAV_CMD_NAV_TAKEOFF
            req.param1 = 0; // Minimum pitch (degrees), ignored by most flight stacks
            req.param2 = 0; // Empty
            req.param3 = 0; // Empty
            req.param4 = float.NaN; // Yaw angle (optional), NaN for default heading
            req.param5 = float.NaN; // Latitude (optional), NaN to stay at current position
            req.param6 = float.NaN; // Longitude (optional), NaN to stay at current position
            req.param7 = targetAltitude; // Target altitude in meters above ground

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);

            // Send the packet using the appropriate connection type
            SendPacket(packet);
            System.Threading.Thread.Sleep(100);

        }

        private void AltHoldButton_Click(object sender, EventArgs e)
        {
            // Define the MAVLink command for changing the flight mode
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();

            req.target_system = 1; // Set to your drone's system ID
            req.target_component = 1; // Set to your drone's component ID

            req.command = (ushort)MAVLink.MAV_CMD.DO_SET_MODE; // Command to set flight mode
            req.param1 = 1; // Base mode (1: Auto mode)
            req.param2 = 2; // Custom mode: AltHold (this value might differ depending on the autopilot firmware)

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            SendPacket(packet);
            System.Threading.Thread.Sleep(100);
        }

        private void GuidedModeButton_Click(object sender, EventArgs e)
        {
            SwitchToGuidedMode();
        }


        private void SwitchToGuidedMode()
        {
            // Define the MAVLink command for changing the flight mode
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();

            req.target_system = 1; // Set to your drone's system ID
            req.target_component = 1; // Set to your drone's component ID

            req.command = (ushort)MAVLink.MAV_CMD.DO_SET_MODE; // Command to set flight mode
            req.param1 = 1; // Base mode (1: Auto mode)
            req.param2 = 4; // Custom mode: Guided (this value might differ depending on the autopilot firmware)

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            SendPacket(packet);
            System.Threading.Thread.Sleep(100);
        }

        private void FlyToWaypoint(int lat_int, int lon_int, int alt_int)
        {
            // Create the MAVLink message
            MAVLink.mavlink_mission_item_int_t missionItem = new MAVLink.mavlink_mission_item_int_t();

            missionItem.target_system = 1;  // Drone's system ID
            missionItem.target_component = 1;  // Drone's component ID
            missionItem.seq = 0;  // Waypoint sequence number (0 if it's the first/only waypoint)
            missionItem.frame = (byte)MAVLink.MAV_FRAME.GLOBAL;  // Use global coordinates
            missionItem.command = (ushort)MAVLink.MAV_CMD.WAYPOINT;  // Command to navigate to waypoint
            missionItem.current = 2;  // Set to 2 to indicate a guided mode command
            missionItem.autocontinue = 1;  // Autocontinue to the next waypoint (1 = yes, 0 = no)
            missionItem.param1 = 0;  // Hold time at waypoint
            missionItem.param2 = 0;  // Acceptance radius in meters
            missionItem.param3 = 0;  // Pass through waypoint (0 = yes)
            missionItem.param4 = float.NaN;  // Desired yaw angle, NaN for default
            missionItem.x = lat_int;  // Latitude
            missionItem.y = lon_int;  // Longitude
            missionItem.z = alt_int;  // Altitude in meters above MSL
            missionItem.mission_type = (byte)MAVLink.MAV_MISSION_TYPE.MISSION;  // Mission type (standard mission)

            // Pack the message into a byte array
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT, missionItem);

            // Send the packet using the appropriate connection type (TCP/Serial)
            SendPacket(packet);
            System.Threading.Thread.Sleep(100);  // Add a small delay if necessary
        }

        private void FlyToShip(ShipMarker shipMarker)
        {
            // Store the target ship marker
            targetShipMarker = shipMarker;

            // Start a timer to continuously update the drone's target position
            if (flyToShipTimer == null)
            {
                flyToShipTimer = new System.Windows.Forms.Timer();
                flyToShipTimer.Interval = 1000; // Update every second
                flyToShipTimer.Tick += FlyToShipTimer_Tick;
            }

            flyToShipTimer.Start();
        }

        private void FlyToShipTimer_Tick(object sender, EventArgs e)
        {
            if (targetShipMarker != null)
            {
                // Get the latest position of the ship
                PointLatLng latestPosition = targetShipMarker.Position;

                // Command the drone to fly to the ship's latest position
                FlyToLocation(latestPosition);
                ShipFollowingModeLabel.Text = $"Following {targetShipMarker.ShipName}";
                ShipFollowingModeLabel.BackColor = Color.Red; // Set the label background to red

                Console.WriteLine($"Flying to {targetShipMarker.ShipName} at updated position: Lat {latestPosition.Lat}, Lng {latestPosition.Lng}");
            }
        }

        private void StopFlyToShip()
        {
            if (flyToShipTimer != null)
            {
                flyToShipTimer.Stop();
            }
            // Update the label to indicate that ship following is disabled
            ShipFollowingModeLabel.Text = "SHIP FOLLOWING DISABLED";
        
            ShipFollowingModeLabel.BackColor = Color.Green; // Set the label background to green
            targetShipMarker = null;
        }

        private void InterceptShip(ShipMarker shipMarker)
        {
            // Store the target ship marker
            targetShipMarker = shipMarker;

            // Start a timer to continuously update the drone's intercept position
            if (flyToShipTimer == null)
            {
                flyToShipTimer = new System.Windows.Forms.Timer();
                flyToShipTimer.Interval = 1000; // Update every second
                flyToShipTimer.Tick += InterceptShipTimer_Tick;
            }

            flyToShipTimer.Start();

            // Update the label to indicate that intercepting is enabled
            ShipFollowingModeLabel.Text = $"Intercepting {shipMarker.ShipName}";
            ShipFollowingModeLabel.BackColor = Color.Red;

            // Calculate the initial intercept position
            double droneSpeedKmh = 40.0;
            double droneSpeedMps = droneSpeedKmh / 3.6;
            double shipSpeedMps = shipMarker.Speed * 0.514444;

            var dronePosition = GetDroneCurrentPosition();
            double distanceToShip = GetDistance(dronePosition, shipMarker.Position);
            double timeToIntercept = distanceToShip / droneSpeedMps;

            PointLatLng interceptPosition = CalculateInterceptPosition(shipMarker.Position, shipMarker.Heading, shipSpeedMps, timeToIntercept);

            // Add the intercept marker
            AddInterceptMarker(interceptPosition);
        }

        private void AddInterceptMarker(PointLatLng interceptPosition)
        {
            // Remove the previous intercept marker if it exists
            var existingInterceptMarkers = markersOverlay.Markers
                .Where(m => m is InterceptMarker)
                .ToList(); // ToList() is important to avoid modifying the collection while iterating

            foreach (var marker in existingInterceptMarkers)
            {
                markersOverlay.Markers.Remove(marker);
            }

            // Add the new intercept marker
            InterceptMarker interceptMarker = new InterceptMarker(interceptPosition);
            markersOverlay.Markers.Add(interceptMarker);

            // Refresh the overlay
            gMapControl1.Refresh();
        }





        private void InterceptShipTimer_Tick(object sender, EventArgs e)
        {
            if (targetShipMarker != null && targetShipMarker.Speed > 0)
            {
                double droneSpeedKmh = 40.0;
                double droneSpeedMps = droneSpeedKmh / 3.6;
                double shipSpeedMps = targetShipMarker.Speed * 0.514444;

                var dronePosition = GetDroneCurrentPosition();
                double distanceToShip = GetDistance(dronePosition, targetShipMarker.Position);
                double timeToIntercept = distanceToShip / droneSpeedMps;

                PointLatLng interceptPosition = CalculateInterceptPosition(targetShipMarker.Position, targetShipMarker.Heading, shipSpeedMps, timeToIntercept);

                // Update the intercept marker position
                AddInterceptMarker(interceptPosition);

                // Command the drone to fly to the intercept position
                FlyToLocation(interceptPosition);

                Console.WriteLine($"Intercepting {targetShipMarker.ShipName} at updated position: Lat {interceptPosition.Lat}, Lng {interceptPosition.Lng}");
            }
            else
            {
                // Stop following if the ship stops or the speed is zero
                StopFlyToShip();
            }
        }




        private PointLatLng CalculateInterceptPosition(PointLatLng shipPosition, double shipHeading, double shipSpeedMps, double timeInSeconds)
        {
            // Calculate the distance the ship will travel in the given time
            double distance = shipSpeedMps * timeInSeconds;

            // Convert heading to radians
            double headingRad = shipHeading * (Math.PI / 180);

            // Calculate the ship's future position
            double deltaLat = (distance / 6371000.0) * Math.Cos(headingRad); // Earth's radius in meters
            double deltaLng = (distance / 6371000.0) * Math.Sin(headingRad) / Math.Cos(shipPosition.Lat * (Math.PI / 180));

            double futureLat = shipPosition.Lat + (deltaLat * (180 / Math.PI));
            double futureLng = shipPosition.Lng + (deltaLng * (180 / Math.PI));

            return new PointLatLng(futureLat, futureLng);
        }

        private PointLatLng GetDroneCurrentPosition()
        {
            PointLatLng currentPosition = mavlinkMessageHandler.DroneCurrentPosition;
            return currentPosition;
        }



        private double GetDistance(PointLatLng point1, PointLatLng point2)
        {
            double R = 6371000; // Earth's radius in meters
            double lat1 = point1.Lat * (Math.PI / 180);
            double lat2 = point2.Lat * (Math.PI / 180);
            double deltaLat = (point2.Lat - point1.Lat) * (Math.PI / 180);
            double deltaLng = (point2.Lng - point1.Lng) * (Math.PI / 180);

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Distance in meters
        }






        private void StopFollowingShipButton_Click(object sender, EventArgs e)
        {
            StopFlyToShip();
        }

        private void ArmStatusBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void MPIeraxMain_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void AltimeterBox_TextChanged(object sender, EventArgs e)
        {

        }
    }

}

