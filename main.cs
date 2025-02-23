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
using static MAVLink;
using System.IO;





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

        private DroneStatusForm statusForm;

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
        private bool isConnected = false;
        // Variables to track maximum values and timestamps
        private float maxCO2 = float.MinValue;
        private string maxCO2Timestamp = string.Empty;

        private float maxHDCO2 = float.MinValue;
        private string maxHDCO2Timestamp = string.Empty;

        private readonly List<MAVLinkData> paramValueBatch = new List<MAVLinkData>(); // For batch storage
        private const int BatchSize = 10; // Adjust batch size as needed

        private Dictionary<string, AisData> cachedAisData = new Dictionary<string, AisData>();

        private CancellationTokenSource cts;

        private MavlinkMessageHandler mavlinkMessageHandler;

        private System.Windows.Forms.Timer flyToShipTimer;
        private ShipMarker targetShipMarker;

        public static ShipMarker GlobalClickedMarker { get; set; } // Global variable for the clicked marker
        public static float InstantCO2 { get; set; }
        public static float InstantHDCO2 { get; set; }

       
        // In Main.cs
        private CameraForm cameraForm;
        private Boolean shipFollowingMode = false;
        private const double EarthRadius = 6378137.0;  // Earth's radius in meters

        public MPIeraxMain()
        {
            InitializeMavlinkHandler();
            InitializeComponent();
            InitializeMap();
            InitializeWebSocket();
            this.AutoScaleMode = AutoScaleMode.Dpi;
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
            droneMarker = new DroneMarker(new PointLatLng(37.7128, 21.0060),mavlinkMessageHandler); // Example coordinates
            markersOverlay.Markers.Add(droneMarker);

            // Refresh the map to ensure the marker is displayed
            gMapControl1.Refresh();

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
                            //Console.WriteLine($"Received {message}");
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

            try
            {
                if (selectedConnection.Contains(":")) // TCP Connection Handling
                {
                    if (isConnected)
                    {
                        DisconnectTCP();
                    }
                    else
                    {
                        string[] parts = selectedConnection.Split(':');
                        string ip = parts[0];
                        int port = int.Parse(parts[1]);

                        ConnectViaTCP(ip, port);
                    }
                }
                else // Serial Port Connection Handling
                {
                    if (serialPort1.IsOpen)
                    {
                        DisconnectSerial();
                    }
                    else
                    {
                        ConnectSerial(selectedConnection);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetConnectButton(); // Reset button in case of failure
            }
        }

        // Function to connect via Serial
        private void ConnectSerial(string portName)
        {
            serialPort1.PortName = portName;
            serialPort1.BaudRate = int.Parse(cmb_baudrate.Text);
            serialPort1.Open();
            serialPort1.ReadTimeout = 2000;

            this.Invoke(new Action(() =>
            {
                but_connect.Text = "Disconnect";
                but_connect.BackColor = Color.Green;
                but_connect.ForeColor = Color.White;
            }));

            // Start background worker to handle connection
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += bgw_DoWork;
            bgw.RunWorkerAsync();
        }

        // Function to disconnect Serial
        private void DisconnectSerial()
        {
            serialPort1.Close();

            this.Invoke(new Action(() =>
            {
                but_connect.Text = "Connect";
                but_connect.BackColor = Color.Red;
                but_connect.ForeColor = Color.White;
            }));
        }

        // Function to reset button on failure
        private void ResetConnectButton()
        {
            this.Invoke(new Action(() =>
            {
                but_connect.Text = "Connect";
                but_connect.BackColor = Color.Gray;
                but_connect.ForeColor = Color.Black;
                isConnected = false;
            }));
        }

        private void DisconnectTCP()
        {
            Console.WriteLine("❌ Disconnected from SITL");

            this.Invoke(new Action(() =>
            {
                but_connect.Text = "Connect";
                but_connect.BackColor = Color.Red;
                but_connect.ForeColor = Color.White;
                isConnected = false;
            }));
        }



        private async Task ConnectViaTCP(string ip, int port)
        {
            try
            {
                TcpClient client = new TcpClient(ip, port);
                tcpStream = client.GetStream();
                isTcpConnection = true;

                this.Invoke(new Action(() =>
                {
                    but_connect.Text = "Connected";
                    but_connect.BackColor = Color.Green;
                    but_connect.ForeColor = Color.White;
                    isConnected = true;
                }));

                Console.WriteLine($"✅ Connected to SITL at {ip}:5762");

                // Send heartbeat and wait for SYSID response
                bool sysIdReceived = await SendMavlinkHeartbeatAsync();
                if (!sysIdReceived)
                {
                    Console.WriteLine("❌ Connection failed: No SYSID received.");
                    return;
                }
                await Task.Delay(1000);

                // Request autopilot capabilities (Wakes up SITL)
                  //RequestAutopilotCapabilities();
                 // await Task.Delay(1000);

                // Request system parameters
              RequestParameters();
               await Task.Delay(2000);


                // Request telemetry data streams
                RequestDataStream();
                await Task.Delay(1000);

                // Start handling MAVLink messages
                await Task.Run(() => HandleMavlinkMessages(tcpStream));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to {ip}:{port} - {ex.Message}");
            }
        }

        private async Task<bool> SendMavlinkHeartbeatAsync()
        {
            if (tcpStream == null)
            {
                Console.WriteLine("TCP stream is null. Cannot send heartbeat.");
                return false;
            }

            try
            {
                MAVLink.mavlink_heartbeat_t heartbeat = new MAVLink.mavlink_heartbeat_t()
                {
                    type = (byte)MAVLink.MAV_TYPE.GCS,  // Ground Control Station
                    autopilot = (byte)MAVLink.MAV_AUTOPILOT.INVALID,
                    base_mode = (byte)MAVLink.MAV_MODE_FLAG.MANUAL_INPUT_ENABLED,
                    system_status = (byte)MAVLink.MAV_STATE.ACTIVE,
                    mavlink_version = 3  // Ensure MAVLink 2.0
                };

                byte[] packet = mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.HEARTBEAT, heartbeat);

           
                    tcpStream.Write(packet, 0, packet.Length);
                    tcpStream.Flush();
                    Console.WriteLine($"✅ Sent MAVLink heartbeat over TCP ");
                await Task.Delay(500);

                // Now wait for a response for up to 2.2 seconds
                return await WaitForSysIdCompIdAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending heartbeat over TCP: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> WaitForSysIdCompIdAsync()
        {
            DateTime timeout = DateTime.Now.AddMilliseconds(2200);

            while (DateTime.Now < timeout)
            {
                MAVLink.MAVLinkMessage packet;
                lock (readlock)
                {
                    packet = mavlink.ReadPacket(tcpStream);
                    if (packet == null || packet.data == null)
                        continue;
                }

                // Check if the received packet is a heartbeat
                if (packet.data.GetType() == typeof(MAVLink.mavlink_heartbeat_t))
                {
                    var hb = (MAVLink.mavlink_heartbeat_t)packet.data;
                    sysid = packet.sysid;
                    compid = packet.compid;

                    Console.WriteLine($"✅ Received SYSID={sysid}, COMPID={compid}");
                    return true;
                }

                await Task.Delay(50); // Small delay to avoid excessive CPU usage
            }

            Console.WriteLine("❌ Timeout: No SYSID/COMPID received.");
            return false;
        }




        private void SendMavlinkHeartbeat()
        {
            if (tcpStream == null)
            {
                Console.WriteLine("TCP stream is null. Cannot send heartbeat.");
                return;
            }

            try
            {
                MAVLink.mavlink_heartbeat_t heartbeat = new MAVLink.mavlink_heartbeat_t()
                {
                    type = (byte)MAVLink.MAV_TYPE.GCS,  // Ground Control Station
                    autopilot = (byte)MAVLink.MAV_AUTOPILOT.INVALID,
                    base_mode = (byte)MAVLink.MAV_MODE_FLAG.MANUAL_INPUT_ENABLED,
                    system_status = (byte)MAVLink.MAV_STATE.ACTIVE,
                    mavlink_version = 3
                };

                byte[] packet = mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.HEARTBEAT, heartbeat);

                for (int i = 0; i < 5; i++) // Send 5 times
                {
                    tcpStream.Write(packet, 0, packet.Length);
                    tcpStream.Flush();
                    Console.WriteLine($"✅ Sent MAVLink heartbeat over TCP (Attempt {i + 1}/5)");
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending heartbeat over TCP: {ex.Message}");
            }
        }




        private void RequestDataStream()
        {
            if (sysid == 0 || compid == 0)
            {
                Console.WriteLine("❌ System ID or Component ID not received yet. Cannot request data.");
                return;
            }

            try
            {
                byte[][] requests = new byte[][]
                {
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = sysid, target_component = compid, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.EXTENDED_STATUS, req_message_rate = 2, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = sysid, target_component = compid, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.POSITION, req_message_rate = 2, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = sysid, target_component = compid, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.EXTRA1, req_message_rate = 4, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = sysid, target_component = compid, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.EXTRA2, req_message_rate = 4, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = sysid, target_component = compid, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.EXTRA3, req_message_rate = 2, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = sysid, target_component = compid, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.RAW_SENSORS, req_message_rate = 2, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = sysid, target_component = compid, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.RC_CHANNELS, req_message_rate = 2, start_stop = 1
            })
                };

                foreach (var packet in requests)
                {
                    tcpStream.Write(packet, 0, packet.Length);
                    tcpStream.Flush();
                    Thread.Sleep(500);
                }

                Console.WriteLine("📡 Requested multiple data streams from SITL.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting data streams: {ex.Message}");
            }
        }

        private void RequestAutopilotCapabilities()
        {
            if (sysid == 0 || compid == 0)
            {
                Console.WriteLine("❌ System ID or Component ID not received yet. Cannot request capabilities.");
                return;
            }

            try
            {
                MAVLink.mavlink_command_long_t cmd = new MAVLink.mavlink_command_long_t()
                {
                    target_system = sysid,
                    target_component = compid,
                    command = (ushort)MAVLink.MAV_CMD.REQUEST_AUTOPILOT_CAPABILITIES
                };

                byte[] packet = mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd);

                tcpStream.Write(packet, 0, packet.Length);
                tcpStream.Flush();

                Console.WriteLine("📡 Requested Autopilot Capabilities.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting autopilot capabilities: {ex.Message}");
            }
        }

        private void RequestParameters()
        {
            if (sysid == 0 || compid == 0)
            {
                Console.WriteLine("❌ System ID or Component ID not received yet. Cannot request parameters.");
                return;
            }

            try
            {
                MAVLink.mavlink_param_request_list_t paramRequest = new MAVLink.mavlink_param_request_list_t()
                {
                    target_system = sysid,
                    target_component = compid
                };

                byte[] packet = mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.PARAM_REQUEST_LIST, paramRequest);

                tcpStream.Write(packet, 0, packet.Length);
                tcpStream.Flush();

                Console.WriteLine("🔄 Requested SITL parameters.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting parameters: {ex.Message}");
            }
        }






        private void HandleMavlinkMessages(Stream stream)
        {
            MAVLink.MavlinkParse mavlink = new MAVLink.MavlinkParse();

            while (true)
            {
                try
                {
                    MAVLink.MAVLinkMessage message = mavlink.ReadPacket(stream);

                    if (message == null || message.data == null)
                        continue;
                    //Console.WriteLine($"Received MAVLink message: sysid={message.sysid}, msgid={message.msgid}, msgtypename={message.msgtypename}");
                    // Handle messages from sysid=1 (ardupilot cube)
                    if (message.sysid == 1)
                    {
                        // Delegate handling to the message handler
                        mavlinkMessageHandler.HandleMavlinkMessage(message);
                    }
                    // Handle messages from sysid=10 (CO2 sensors)
                    else if (message.sysid == 10)
                    {
                        HandleSensorMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling MAVLink message: {ex.Message}");
                    break;
                }
            }
        }


        private void HandleSensorMessage(MAVLink.MAVLinkMessage message)
        {
            if (message == null || message.data == null)
            {
                Console.WriteLine("Warning: Received null MAVLink message or data.");
                return;
            }

            if (message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.PARAM_VALUE)
            {
                var paramValue = (MAVLink.mavlink_param_value_t)message.data;

                // Extract parameter name and value
                string paramName = ExtractParamName(paramValue.param_id);
                if (string.IsNullOrEmpty(paramName))
                {
                    Console.WriteLine("Warning: Extracted an empty parameter name.");
                    return;
                }

                float paramValueFloat = paramValue.param_value;
                string currentTimestamp = DateTime.Now.ToString("HH:mm:ss");

                // CO2 Handling
                if (paramName == "CO2")
                {
                    InstantCO2 = paramValueFloat;

                    if (paramValueFloat > maxCO2)
                    {
                        maxCO2 = paramValueFloat;
                        maxCO2Timestamp = currentTimestamp;

                        if (txtCO2max != null)
                            UpdateMaxValueTextBox(txtCO2max, maxCO2, maxCO2Timestamp);
                    }

                    if (txtCO2 != null)
                        UpdateTextBox(txtCO2, paramValueFloat.ToString("F2"));
                }
                // HDCO2 Handling
                else if (paramName == "HDCO2")
                {
                    InstantHDCO2 = paramValueFloat;

                    if (paramValueFloat > maxHDCO2)
                    {
                        maxHDCO2 = paramValueFloat;
                        maxHDCO2Timestamp = currentTimestamp;

                        if (txtHDCO2max != null)
                            UpdateMaxValueTextBox(txtHDCO2max, maxHDCO2, maxHDCO2Timestamp);
                    }

                    if (txtHDCO2 != null)
                        UpdateTextBox(txtHDCO2, paramValueFloat.ToString("F2"));
                }
            }
        }


        // Helper method to update the maximum value TextBox
        private void UpdateMaxValueTextBox(Label textBox, float maxValue, string timestamp)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new Action(() =>
                {
                    textBox.Text = $"{maxValue:F2} ({timestamp})";
                    textBox.ForeColor = Color.Red; // Set text color to red
                }));
            }
            else
            {
                textBox.Text = $"{maxValue:F2} ({timestamp})";
                textBox.ForeColor = Color.Red; // Set text color to red
            }
        }

        // Helper method to update real-time value TextBox
        private void UpdateTextBox(Label textBox, string value)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new Action(() => textBox.Text = value));
            }
            else
            {
                textBox.Text = value;
            }
        }

        // Helper method to extract and clean parameter names
        private string ExtractParamName(byte[] paramId)
        {
            int length = Array.IndexOf(paramId, (byte)0); // Find the first null character
            if (length == -1) length = paramId.Length;   // If no null, use full length

            return new string(paramId.Take(length)
                                      .Select(b => (char)b)
                                      .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c))
                                      .ToArray());
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
                    droneMarker = new DroneMarker(position, mavlinkMessageHandler);
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

                // Get the drone's speed
                double droneSpeedMps = GetDroneGroundSpeed();

                // If speed is greater than 5 m/s, draw a straight green line 1000m ahead
                if (droneSpeedMps > 5.0)
                {
                    DrawProjectedFlightPath(position, droneSpeedMps);
                }
            }
        }

        private void DrawProjectedFlightPath(PointLatLng currentPosition, double speed)
        {
            double projectionDistanceMeters = 3000.0; // 1000 meters ahead
            double bearing = GetDroneCurrentHeading(); // Function to get the drone's heading in degrees

            // Calculate the new projected position 1000m ahead
            PointLatLng projectedPosition = GetDestinationPoint(currentPosition, projectionDistanceMeters, bearing);

            // Remove any existing projected path lines before adding a new one
            markersOverlay.Routes.Clear();

            // Create a new route (polyline) from the current position to the projected position
            GMapRoute projectedPath = new GMapRoute(new List<PointLatLng> { currentPosition, projectedPosition }, "FlightPath");
            projectedPath.Stroke = new Pen(Color.Green, 3); // Green line with thickness 3

            // Add the projected path to the overlay
            markersOverlay.Routes.Add(projectedPath);
        }

        // Function to calculate the destination point given a starting point, distance, and bearing
        private PointLatLng GetDestinationPoint(PointLatLng startPoint, double distanceMeters, double bearingDegrees)
        {
            double radiusEarth = 6371000.0; // Radius of Earth in meters
            double latRad = startPoint.Lat * (Math.PI / 180.0);
            double lonRad = startPoint.Lng * (Math.PI / 180.0);
            double bearingRad = bearingDegrees * (Math.PI / 180.0);

            double angularDistance = distanceMeters / radiusEarth;

            double newLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(angularDistance) +
                                         Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

            double newLonRad = lonRad + Math.Atan2(Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
                                                   Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad));

            double newLat = newLatRad * (180.0 / Math.PI);
            double newLon = newLonRad * (180.0 / Math.PI);

            return new PointLatLng(newLat, newLon);
        }






        //EDO EXOUME TON ASYNC WORKER
        void bgw_DoWork(object sender, DoWorkEventArgs e)
        {

            HandleMavlinkMessages(serialPort1.BaseStream);
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
            serialPorts.Add("172.23.130.102:5760");
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

                GlobalClickedMarker = clickedMarker; //
                // If the right-click was on a ship marker, add the "Fly to [ShipName]" option
                contextMenu.Items.Add($"Fly to {clickedMarker.ShipName}", null, (s, e) => FlyToShip(clickedMarker));

                // Add "Intercept [ShipName]" option
                contextMenu.Items.Add($"Intercept {clickedMarker.ShipName}", null, (s, e) => InterceptShip(clickedMarker));

                // Optionally, you can still show the coordinates of the ship
                contextMenu.Items.Add($"Latitude: {clickedMarker.Position.Lat:F6}, Longitude: {clickedMarker.Position.Lng:F6}", null);
            }
            else
            {
                GlobalClickedMarker = null; // Reset the global clickedMarker
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
            //SwitchToGuidedMode();

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

            //Console.WriteLine(labelName);
      
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
                if (shipFollowingMode)
                {
                    FlyToLocation(latestPosition);
                    ShipFollowingModeLabel.Text = $"Following {targetShipMarker.ShipName}";
                    ShipFollowingModeLabel.BackColor = Color.Red; // Set the label background to red
                }
          

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
            shipFollowingMode = disabled;

            if (targetShipMarker != null)
            {
                targetShipMarker.PositionChanged -= OnShipMarkerUpdated;
                targetShipMarker.SpeedChanged -= OnShipMarkerUpdated;
                targetShipMarker.HeadingChanged -= OnShipMarkerUpdated;
            }

            ShipFollowingModeLabel.BackColor = Color.Green; // Set the label background to green
            targetShipMarker = null;
        }

        private void InterceptShip(ShipMarker shipMarker)

        {
            // Unsubscribe from previous shipMarker events, if any
            if (targetShipMarker != null)
            {
                shipFollowingMode = true;
                targetShipMarker.PositionChanged -= OnShipMarkerUpdated;
                targetShipMarker.SpeedChanged -= OnShipMarkerUpdated;
                targetShipMarker.HeadingChanged -= OnShipMarkerUpdated;
            }

            // Store the target ship marker
            targetShipMarker = shipMarker;

            // Subscribe to the shipMarker events
            targetShipMarker.PositionChanged += OnShipMarkerUpdated;
            targetShipMarker.SpeedChanged += OnShipMarkerUpdated;
            targetShipMarker.HeadingChanged += OnShipMarkerUpdated;

            // Update the label to indicate that intercepting is enabled
            ShipFollowingModeLabel.Text = $"Intercepting {shipMarker.ShipName}";
            ShipFollowingModeLabel.BackColor = Color.Red;

            // Calculate the initial intercept position
            UpdateInterceptPosition();
        }

        // Method to handle ShipMarker updates
        private void OnShipMarkerUpdated()
        {
            UpdateInterceptPosition();
        }

        // Method to update the intercept position
        private void UpdateInterceptPosition()
        {
            double droneSpeedMps = GetDroneGroundSpeed();
            droneSpeedMps = (droneSpeedMps < 5.0) ? 10.0 : droneSpeedMps;
            double shipSpeedMps = targetShipMarker.Speed * 0.514444;
            double droneHeadingDegrees = GetDroneCurrentHeading();

            var dronePosition = GetDroneCurrentPosition();
            double distanceToShip = GetDistance(dronePosition, targetShipMarker.Position);
            double timeToIntercept = distanceToShip / droneSpeedMps;

            PointLatLng interceptPosition = CalculateInterceptPosition(targetShipMarker.Position, targetShipMarker.Heading, shipSpeedMps, timeToIntercept);

            PointLatLng interceptLatLng = CalculateIntercept(
           dronePosition, droneSpeedMps, droneHeadingDegrees,
           targetShipMarker.Position, targetShipMarker.Speed, targetShipMarker.Heading
       );

            Console.WriteLine($"Intercept Coordinates: Latitude {interceptLatLng.Lat:F6}, Longitude {interceptLatLng.Lng:F6}");

            // Add or update the intercept marker

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

            if (shipFollowingMode)
            {
                FlyToLocation(interceptPosition);

                Console.WriteLine($"Intercepting {targetShipMarker.ShipName} at updated position: Lat {interceptPosition.Lat}, Lng {interceptPosition.Lng}");
            }

           

            // Refresh the overlay
            gMapControl1.Refresh();
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

        private Double GetDroneGroundSpeed()
        {
           Double currentSpeed = mavlinkMessageHandler.DroneGroundSpeed;
            return currentSpeed;
        }

        private Double GetDroneCurrentHeading()
            {
            Double currentHeading = mavlinkMessageHandler.DroneHeading;
            return currentHeading;
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

                if (targetShipMarker != null)
            {
                targetShipMarker.PositionChanged -= OnShipMarkerUpdated;
                targetShipMarker.SpeedChanged -= OnShipMarkerUpdated;
                targetShipMarker.HeadingChanged -= OnShipMarkerUpdated;
            }
         
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

        private void GSpeedLabel_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void RTLButton_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        public static PointLatLng CalculateIntercept(
        PointLatLng dronePos, double droneSpeed, double droneHeadingDegrees,
        PointLatLng shipPos, double shipSpeed, double shipHeadingDegrees)
        {
            // Convert drone and ship positions to ENU coordinates relative to drone's initial position
            var droneENU = LatLonToENU(dronePos, dronePos);
            var shipENU = LatLonToENU(shipPos, dronePos);

            // Convert headings to radians
            double droneHeadingRadians = DegreesToRadians(droneHeadingDegrees);
            double shipHeadingRadians = DegreesToRadians(shipHeadingDegrees);

            // Calculate the velocity components of the drone and ship
            double droneSpeedX = droneSpeed * Math.Cos(droneHeadingRadians);
            double droneSpeedY = droneSpeed * Math.Sin(droneHeadingRadians);
            double shipSpeedX = shipSpeed * Math.Cos(shipHeadingRadians);
            double shipSpeedY = shipSpeed * Math.Sin(shipHeadingRadians);

            // Calculate the relative velocity components
            double relativeSpeedX = droneSpeedX - shipSpeedX;
            double relativeSpeedY = droneSpeedY - shipSpeedY;

            // Calculate the relative position
            double relativePosX = shipENU.x - droneENU.x;
            double relativePosY = shipENU.y - droneENU.y;

            // Calculate the time to intercept
            double tIntercept = (relativePosX * relativeSpeedX + relativePosY * relativeSpeedY) /
                                (relativeSpeedX * relativeSpeedX + relativeSpeedY * relativeSpeedY);

            // Calculate the intercept coordinates in ENU
            double xIntercept = droneENU.x + droneSpeedX * tIntercept;
            double yIntercept = droneENU.y + droneSpeedY * tIntercept;

            // Convert the intercept ENU coordinates back to latitude and longitude
            return ENUToLatLng(xIntercept, yIntercept, dronePos);
        }

        private static (double x, double y) LatLonToENU(PointLatLng point, PointLatLng reference)
        {
            double dLat = DegreesToRadians(point.Lat - reference.Lat);
            double dLng = DegreesToRadians(point.Lng - reference.Lng);

            double x = EarthRadius * dLng * Math.Cos(DegreesToRadians(reference.Lat)); // East
            double y = EarthRadius * dLat; // North

            return (x, y);
        }

        private static PointLatLng ENUToLatLng(double x, double y, PointLatLng reference)
        {
            double dLat = y / EarthRadius;
            double dLng = x / (EarthRadius * Math.Cos(DegreesToRadians(reference.Lat)));

            double lat = reference.Lat + RadiansToDegrees(dLat);
            double lng = reference.Lng + RadiansToDegrees(dLng);

            return new PointLatLng(lat, lng);
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        private void txtHDCO2_TextChanged(object sender, EventArgs e)
        {

        }

        private void BottomPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txtCO2_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtCO2_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // Change the port number to the one your drone camera is streaming on.
            int cameraPort = 5600;
            CameraForm camForm = new CameraForm(cameraPort);
            camForm.Show();  // Opens the camera feed in a new window.
        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void analyzerBT_Click(object sender, EventArgs e)
        {
            AnalyzerForm analyzer = new AnalyzerForm();
            analyzer.Show();
        }


        private async void StartShipMeasurementPattern()
        {
            if (droneMarker == null || targetShipMarker == null)
            {
                MessageBox.Show("Drone or ship marker is not available.");
                return;
            }

            // Open the status window
            statusForm = new DroneStatusForm();
            statusForm.Show();

            statusForm.UpdateStatus("Starting Measurement Pattern...");

            // Take the current drone position as the ship's center
            PointLatLng shipCenter = droneMarker.Position;
            double shipHeading = targetShipMarker.Heading; // Ship's heading

            // Define movement distance
            double moveDistance = 100.0; // 100 meters
            int waitTimeMs = 60000; // 60 seconds

            // Define measurement points
            List<PointLatLng> measurementPoints = new List<PointLatLng>
    {
        GetDestinationPoint(shipCenter, moveDistance, shipHeading),         // Front of the ship
        GetDestinationPoint(shipCenter, moveDistance, shipHeading - 90),    // Left of the ship
        GetDestinationPoint(shipCenter, moveDistance, shipHeading + 180),   // Back of the ship
        GetDestinationPoint(shipCenter, moveDistance, shipHeading + 90)     // Right of the ship
    };

            int pointIndex = 1;
            foreach (var point in measurementPoints)
            {
                statusForm.UpdateStatus($"Proceeding to point {pointIndex} with heading {shipHeading}°...");
                FlyToLocation(point); // Move to target position
                await WaitForDroneToReach(point); // Wait until the drone reaches the point
                statusForm.UpdateStatus($"Arrived at point {pointIndex}, waiting {waitTimeMs / 1000} seconds...");

                SetLoiterMode(); // Activate Loiter mode to hold position

                // Countdown Timer
                for (int i = waitTimeMs / 1000; i > 0; i--)
                {
                    statusForm.UpdateStatus($"Waiting... {i} sec remaining.");
                    await Task.Delay(1000);
                }

                pointIndex++;
            }

            statusForm.UpdateStatus("Ship measurement pattern completed.");
            MessageBox.Show("Measurement pattern completed.");
        }

        // Function to switch to Loiter mode
        private void SetLoiterMode()
        {
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();

            req.target_system = 1; // Drone system ID
            req.target_component = 1; // Drone component ID

            req.command = (ushort)MAVLink.MAV_CMD.DO_SET_MODE; // Command to set flight mode
            req.param1 = 1; // Base mode (Auto)
            req.param2 = 3; // Custom mode: Loiter (3)

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            SendPacket(packet);
            System.Threading.Thread.Sleep(100);
        }

        // Function to wait until the drone reaches the target point (optional)
        private async Task WaitForDroneToReach(PointLatLng targetPoint)
        {
            double threshold = 5.0; // 5 meters tolerance

            while (true)
            {
                PointLatLng currentPos = droneMarker.Position; // Get drone's current position
                double distance = GetDistanceBetweenPoints(currentPos, targetPoint);

                if (distance <= threshold)
                    break; // Drone reached the target

                statusForm.UpdateStatus($"Moving... {distance:F2} meters remaining.");
                await Task.Delay(1000); // Wait 1 second before checking again
            }
        }

        // Function to calculate the distance between two points (Haversine Formula approximation)
        private double GetDistanceBetweenPoints(PointLatLng p1, PointLatLng p2)
        {
            double lat1 = p1.Lat * (Math.PI / 180.0);
            double lon1 = p1.Lng * (Math.PI / 180.0);
            double lat2 = p2.Lat * (Math.PI / 180.0);
            double lon2 = p2.Lng * (Math.PI / 180.0);

            double dlat = lat2 - lat1;
            double dlon = lon2 - lon1;

            double a = Math.Pow(Math.Sin(dlat / 2), 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dlon / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double earthRadius = 6371000.0; // meters
            return earthRadius * c;
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            StartShipMeasurementPattern();
            shipFollowingMode = false;
        }
    }

}

