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
        private Panel infoPanel;
        private Label infoLabel;

        private Dictionary<string, AisData> cachedAisData = new Dictionary<string, AisData>();

        private CancellationTokenSource cts;

        public MPIeraxMain()
        {
            InitializeComponent();
            InitializeMap();
            InitializeInfoPanel();
            InitializeWebSocket();
           
            // Attach resize event
            this.Resize += Form1_Resize;

        }


        private void gMapControl1_Load(object sender, EventArgs e)
        {
    
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

        private void Form1_Resize(object sender, EventArgs e)
        {
            PositionInfoPanel();
        }

        private void InitializeInfoPanel()
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
        }

        private void PositionInfoPanel()
        {
            // Position the panel in the top-right corner of the gMapControl
            infoPanel.Location = new Point(gMapControl1.Width - infoPanel.Width - 10, 50);
            infoPanel.BringToFront();
        }

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
                            Console.WriteLine($"Received {message}");
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
            // if the port is open close it
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                return;
            }

            // set the comport options
            serialPort1.PortName = CMB_comport.Text;
            serialPort1.BaudRate = int.Parse(cmb_baudrate.Text);

            // open the comport
            serialPort1.Open();

            // set timeout to 2 seconds
            serialPort1.ReadTimeout = 2000;

            BackgroundWorker bgw = new BackgroundWorker();

            bgw.DoWork += bgw_DoWork;

            bgw.RunWorkerAsync();
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

                    //Console.WriteLine(packet.msgtypename);
                    
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

        T readsomedata<T>(byte sysid,byte compid,int timeout = 2000)
        {
            DateTime deadline = DateTime.Now.AddMilliseconds(timeout);

            lock (readlock)
            {
                // read the current buffered bytes
                while (DateTime.Now < deadline)
                {
                    var packet = mavlink.ReadPacket(serialPort1.BaseStream);

                    // check its not null, and its addressed to us
                    if (packet == null || sysid != packet.sysid || compid != packet.compid)
                        continue;

                  //  Console.WriteLine(packet);

                    if (packet.data.GetType() == typeof (T))
                    {
                        return (T) packet.data;
                    }
                }
            }

            throw new Exception("No packet match found");
        }

        private void but_armdisarm_Click(object sender, EventArgs e)
        {
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();

            req.target_system = 1;
            req.target_component = 1;

            req.command = (ushort)MAVLink.MAV_CMD.COMPONENT_ARM_DISARM;

            req.param1 = armed ? 0 : 1;
            armed = !armed;
            /*
            req.param2 = p2;
            req.param3 = p3;
            req.param4 = p4;
            req.param5 = p5;
            req.param6 = p6;
            req.param7 = p7;
            */

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);

            serialPort1.Write(packet, 0, packet.Length);

            try
            {
                var ack = readsomedata<MAVLink.mavlink_command_ack_t>(sysid, compid);
                if (ack.result == (byte)MAVLink.MAV_RESULT.ACCEPTED) 
                {

                }
            }
            catch 
            { 
            }
        }

        private void CMB_comport_Click(object sender, EventArgs e)
        {
            CMB_comport.DataSource = SerialPort.GetPortNames();
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

                req2.y = (int) (115 * 1.0e7);
                req2.x = (int) (-35 * 1.0e7);

                req2.z = (float) (2.34);

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
                    if ((MAVLink.MAV_MISSION_RESULT) ack2.type != MAVLink.MAV_MISSION_RESULT.MAV_MISSION_ACCEPTED)
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
            if (e.Button == MouseButtons.Left)
            {
                Console.WriteLine("Pressed Left Click");

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
                        Console.WriteLine("Pressed Left Click on marker");
                        if (marker is ShipMarker shipMarker)
                        {
                            ShowShipInfo(shipMarker);
                        }
                        break; // Exit the loop after finding the clicked marker
                    }
                }
            }

            if (e.Button == MouseButtons.Right)
            {
                // Get the latitude and longitude of the point clicked
                PointLatLng point = gMapControl1.FromLocalToLatLng(e.X, e.Y);

                // Optionally, you can show a context menu or perform some other action
                ShowRightClickMenu(point, e.Location);
            }
        }

        private void ShowRightClickMenu(PointLatLng point, Point location)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            // Add items to the context menu
            contextMenu.Items.Add($"Latitude: {point.Lat:F6}, Longitude: {point.Lng:F6}", null);
            // Add other menu items and handle their Click events as needed

            // Show the context menu at the mouse position
            contextMenu.Show(gMapControl1, location);
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

                        Console.WriteLine($"Parsed COG for ship {mmsi}: {heading} degrees.");
                        Console.WriteLine($"Parsed SOG for ship {mmsi}: {speed} knots.");

                        // Handle TrueHeading if it's not 511 (unavailable)
                        if (report.TrueHeading != 511)
                        {
                            heading = report.TrueHeading;
                            Console.WriteLine($"Parsed TrueHeading for ship {mmsi}: {heading} degrees.");
                        }
                    }
                    // Check for PositionReport (or any other relevant message type)
                    else if (aisData.Message.PositionReport != null)
                    {
                        var report = aisData.Message.PositionReport;

                        // Parse the COG, SOG, and TrueHeading
                        heading = report.Cog;
                        speed = report.Sog;

                        Console.WriteLine($"Parsed COG for ship {mmsi}: {heading} degrees.");
                        Console.WriteLine($"Parsed SOG for ship {mmsi}: {speed} knots.");

                        // Handle TrueHeading if it's not 511 (unavailable)
                        if (report.TrueHeading != 511)
                        {
                            heading = report.TrueHeading;
                            Console.WriteLine($"Parsed TrueHeading for ship {mmsi}: {heading} degrees.");
                        }
                    }
                    else
                    {
                        // If no relevant position report is found, log a message
                        Console.WriteLine($"No recognized position report found for ship {mmsi}.");
                        return; // Exit the method early since there's no position data to update
                    }

                    // Proceed with the marker update as before
                    if (cachedAisData.ContainsKey(mmsi))
                    {
                        var shipMarker = shipMarkers[mmsi] as ShipMarker;
                        if (shipMarker != null && shipMarker.PreviousPosition.HasValue)
                        {
                            heading = CalculateBearing(shipMarker.PreviousPosition.Value, currentPosition);
                            Console.WriteLine($"Calculating heading for {shipName} (MMSI: {mmsi}). Previous Position: Lat {shipMarker.PreviousPosition.Value.Lat}, Lng {shipMarker.PreviousPosition.Value.Lng}. Current Position: Lat {currentPosition.Lat}, Lng {currentPosition.Lng}. Calculated Heading: {heading} degrees");

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
                    Console.WriteLine($"Updating ship {mmsi} marker at: {latitude}, {longitude}");

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
                infoLabel.Text = info;
                infoPanel.Visible = true;

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





    }
}
