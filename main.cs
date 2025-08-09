using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using IERAX_MissionControl.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static MAVLink;





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

        private GMapOverlay quakesOverlay;
        private System.Windows.Forms.Timer quakeTimer;

        private const double MinLat = 35.0, MaxLat = 37.0;   // Santorini area (tweak)
        private const double MinLon = 24, MaxLon = 27.0;


        // In Main.cs
        private CameraForm cameraForm;
        private Boolean shipFollowingMode = false;
        private const double EarthRadius = 6378137.0;  // Earth's radius in meters

        private System.Windows.Forms.Timer droneNavigationTimer;
        private DroneFlightMode currentMode = DroneFlightMode.None;

        public MPIeraxMain()
        {
            InitializeMavlinkHandler();
            InitializeComponent();
            InitializeMap();
            InitializeWebSocket();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.TopMost = false;

            // Initialize the timer
            droneNavigationTimer = new System.Windows.Forms.Timer();
            droneNavigationTimer.Interval = 1000; // 1 second
            droneNavigationTimer.Tick += DroneNavigationTimer_Tick;

            // Wire up the LandButton click event
            this.LandButton.Click += new System.EventHandler(this.LandButton_Click);
        }

        private class EqEvent
        {
            public DateTimeOffset Time { get; set; }
            public double Mag { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
            public string Place { get; set; }
        }

        private List<EqEvent> _eqEvents = new List<EqEvent>();

        private static int CountInRange(IEnumerable<EqEvent> src, DateTimeOffset start, DateTimeOffset end) =>
            src.Count(e => e.Time >= start && e.Time < end);

        private static (string arrow, string pct) Trend(int current, int prev)
        {
            if (prev <= 0) return current > 0 ? ("▲", "new") : ("•", "—");
            var pct = (current - prev) * 100.0 / prev;
            return (pct > 0 ? "▲" : pct < 0 ? "▼" : "•", (pct >= 0 ? "+" : "") + pct.ToString("0.#") + "%");
        }

        private void UpdateEarthquakeStatsLabel()
        {
            const double volcLat = 36.4044;
            const double volcLon = 25.3975;
            const double radius = 10.0; // km

            var now = DateTimeOffset.UtcNow;

            // --- Global time windows ---
            var d1Start = now.AddDays(-1);
            var d1Prev = now.AddDays(-2);
            var w1Start = now.AddDays(-7);
            var w1Prev = now.AddDays(-14);
            var m1Start = now.AddDays(-30);
            var m1Prev = now.AddDays(-60);

            // --- Global counts ---
            int dayNow = CountInRange(_eqEvents, d1Start, now);
            int dayPrev = CountInRange(_eqEvents, d1Prev, d1Start);
            int weekNow = CountInRange(_eqEvents, w1Start, now);
            int weekPrev = CountInRange(_eqEvents, w1Prev, w1Start);
            int monthNow = CountInRange(_eqEvents, m1Start, now);
            int monthPrev = CountInRange(_eqEvents, m1Prev, m1Start);

            (string a1, string p1) = Trend(dayNow, dayPrev);
            (string a7, string p7) = Trend(weekNow, weekPrev);
            (string a30, string p30) = Trend(monthNow, monthPrev);

            // --- Volcano counts ---
            int volcDay = CountInRangeWithRadius(_eqEvents, d1Start, now, volcLat, volcLon, radius);
            int volcWeek = CountInRangeWithRadius(_eqEvents, w1Start, now, volcLat, volcLon, radius);
            int volcMonth = CountInRangeWithRadius(_eqEvents, m1Start, now, volcLat, volcLon, radius);

            // --- Build label text ---
            var txt = string.Join(Environment.NewLine,
                $"24h: {dayNow} ({a1} {p1})   <{radius}km: {volcDay}",
                $"7d:  {weekNow} ({a7} {p7})   <{radius}km: {volcWeek}",
                $"30d: {monthNow} ({a30} {p30})   <{radius}km: {volcMonth}"
            );

            if (EarthquakeInfoLabel.InvokeRequired)
                EarthquakeInfoLabel.Invoke(new Action(() => EarthquakeInfoLabel.Text = txt));
            else
                EarthquakeInfoLabel.Text = txt;
        }




        public enum DroneFlightMode
        {
            None,
            Follow,
            Intercept
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

            markersOverlay = new GMapOverlay("markers");
            gMapControl1.Overlays.Add(markersOverlay);

            quakesOverlay = new GMapOverlay("quakes");
            gMapControl1.Overlays.Add(quakesOverlay);

            droneMarker = new DroneMarker(new PointLatLng(36.44, 25.40), mavlinkMessageHandler);
            markersOverlay.Markers.Add(droneMarker);

            gMapControl1.Refresh();

            // start periodic sync
            quakeTimer = new System.Windows.Forms.Timer { Interval = 5 * 60 * 1000 };
            quakeTimer.Tick += async (_, __) => await RefreshQuakesFromEMSCAsync();
            _ = RefreshQuakesFromEMSCAsync();   // fire once now
            quakeTimer.Start();
        }



        private async Task RefreshQuakesFromEMSCAsync(int daysBack = 60)
        {
            try
            {
                var startUtc = DateTime.UtcNow.AddDays(-daysBack).ToString("yyyy-MM-dd'T'HH:mm:ss");
                var endUtc = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss");

                var url =
                    "https://www.seismicportal.eu/fdsnws/event/1/query" +
                    $"?format=json&starttime={startUtc}&endtime={endUtc}" +
                    $"&minlatitude={MinLat}&maxlatitude={MaxLat}" +
                    $"&minlongitude={MinLon}&maxlongitude={MaxLon}" +
                    $"&orderby=time";

                using var http = new HttpClient();
                var json = await http.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("features", out var features))
                    return;

                var newCache = new List<EqEvent>();

                if (gMapControl1.InvokeRequired)
                    gMapControl1.Invoke(new Action(() => quakesOverlay.Markers.Clear()));
                else
                    quakesOverlay.Markers.Clear();

                foreach (var f in features.EnumerateArray())
                {
                    var coords = f.GetProperty("geometry").GetProperty("coordinates").EnumerateArray().ToArray();
                    double lon = coords[0].GetDouble();
                    double lat = coords[1].GetDouble();

                    var props = f.GetProperty("properties");
                    double mag = props.TryGetProperty("mag", out var mEl) && mEl.ValueKind == JsonValueKind.Number
                                 ? mEl.GetDouble() : 0.0;

                    DateTimeOffset when;
                    if (props.TryGetProperty("time", out var tEl) && tEl.ValueKind == JsonValueKind.Number)
                        when = DateTimeOffset.FromUnixTimeMilliseconds(tEl.GetInt64()).ToUniversalTime();
                    else if (props.TryGetProperty("time", out var tStr) && tStr.ValueKind == JsonValueKind.String
                             && DateTimeOffset.TryParse(tStr.GetString(), out var parsed))
                        when = parsed.ToUniversalTime();
                    else
                        when = DateTimeOffset.UtcNow;

                    string place = props.TryGetProperty("place", out var plc) && plc.ValueKind == JsonValueKind.String
                                   ? plc.GetString() ?? "" : "";

                    // Add to cache
                    newCache.Add(new EqEvent
                    {
                        Time = when,
                        Mag = mag,
                        Lat = lat,
                        Lon = lon,
                        Place = place
                    });

                    // Marker with magnitude circle
                    var marker = new MagnitudeCircleMarker(new PointLatLng(lat, lon), mag)
                    {
                        BaseRadius = 6,
                        ToolTipText = $"M {mag:F1} — {place}\n{when.LocalDateTime:g}",
                        ToolTipMode = MarkerTooltipMode.OnMouseOver
                    };

                    if (gMapControl1.InvokeRequired)
                        gMapControl1.Invoke(new Action(() => quakesOverlay.Markers.Add(marker)));
                    else
                        quakesOverlay.Markers.Add(marker);
                }

                _eqEvents = newCache;

                if (gMapControl1.InvokeRequired)
                    gMapControl1.Invoke(new Action(gMapControl1.Refresh));
                else
                    gMapControl1.Refresh();

                UpdateEarthquakeStatsLabel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EMSC quake sync failed: " + ex.Message);
            }
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
            gMapControl1.Position = new PointLatLng(37.7128, 21.0060); 
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
                    GlobalSensorStats.CO2Instant = InstantCO2; // Update global CO2 value

                    if (paramValueFloat > maxCO2)
                    {
                        maxCO2 = paramValueFloat;
                        maxCO2Timestamp = currentTimestamp;
                        GlobalSensorStats.MaxCO2 = maxCO2; // Update global max CO2 value
                        GlobalSensorStats.MaxCO2Timestamp = maxCO2Timestamp; // Update global max CO2 timestamp

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
                    GlobalSensorStats.HDCO2Instant = InstantHDCO2; // Update global HDCO2 value

                    if (paramValueFloat > maxHDCO2)
                    {
                        maxHDCO2 = paramValueFloat;
                        maxHDCO2Timestamp = currentTimestamp;
                        GlobalSensorStats.MaxHDCO2 = maxHDCO2; // Update global max HDCO2 value
                        GlobalSensorStats.MaxHDCO2Timestamp = maxHDCO2Timestamp; // Update global max HDCO2 timestamp

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
                flyToMenuItem.Click += (sender, e) => FlyToLocation(point,25);
                contextMenu.Items.Add(flyToMenuItem);

                // Add "Land at this location" item
                ToolStripMenuItem landAtMenuItem = new ToolStripMenuItem("Land at this location");
                landAtMenuItem.Click += (sender, e) => LandAtLocation(point);
                contextMenu.Items.Add(landAtMenuItem);
            }

            // Show the context menu at the mouse position
            contextMenu.Show(gMapControl1, location);
        }

       



        private void FlyToLocation(PointLatLng point, int altimeter)

        {
            // Ensure the drone is in Guided mode
            //SwitchToGuidedMode();

            // Convert latitude and longitude from double to int format required by FlyToWaypoint
            int lat_int = (int)(point.Lat * 1e7);
            int lon_int = (int)(point.Lng * 1e7);

            // Call the method to send the drone to the specified waypoint
            FlyToWaypoint(lat_int, lon_int, altimeter);
        }




        private async void LandAtLocation(PointLatLng point)
        {
            // Step 1: Fly to the specified location at a safe altitude (e.g., 10m or current altitude)
            int approachAltitude = 10; // You can adjust this value or make it a parameter
            FlyToLocation(point, approachAltitude);

            // Step 2: Wait until the drone is within 3 meters of the target point
            double threshold = 3.0; // meters
            while (true)
            {
                PointLatLng currentPos = GetDroneCurrentPosition();
                double distance = GetDistance(currentPos, point);
                if (distance <= threshold)
                    break;
                await Task.Delay(1000); // Wait 1 second before checking again
            }

            // Step 3: Send the LAND command at the current location
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
                EarthquakeInfoLabel.Text = info;
                EarthquakeInfoLabel.Visible = true;

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
            Bitmap circleImage = new Bitmap(Properties.Resources.selection); // Replace with your actual image resource or path

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
            currentMode = DroneFlightMode.Follow;
            droneNavigationTimer.Start();
        }

        private void DroneNavigationTimer_Tick(object sender, EventArgs e)
        {
            if (targetShipMarker == null || currentMode == DroneFlightMode.None)
            {
                // No active mode or no ship to target.
                return;
            }

            // Get the drone's current position
            var dronePosition = GetDroneCurrentPosition();

            // Decide what the target point is, based on the mode.
            PointLatLng targetPoint;

            if (currentMode == DroneFlightMode.Follow)
            {
                // The target is the ship's latest position
                targetPoint = targetShipMarker.Position;
            }
            else if (currentMode == DroneFlightMode.Intercept)
            {
                // Compute the intercept position
                //targetPoint = CalculateIntercept(dronePosition, targetShipMarker);
                targetPoint = UpdateInterceptPosition();
            }
            else
            {
                return;
            }

            // Check distance
            double distance = GetDistance(dronePosition, targetPoint);
            // Optional: Update arrival time label
            double droneSpeedMps = GetDroneGroundSpeed();
            // if speed < some threshold, assume a default speed to avoid dividing by zero
            if (droneSpeedMps < 0.5) droneSpeedMps = 0.5;

            // approximate time to arrival
            double timeToArrival = distance / droneSpeedMps; // in seconds
                                                             // Update label
            ShipFollowingModeLabel.Text = $"{currentMode} {targetShipMarker.ShipName} - {timeToArrival:F1}s to arrival";

            // If within 3 meters, stop
            if (distance <= 3.0)
            {
                StopFlight($"Arrived at target within 3m (mode: {currentMode})");
            }
            else
            {
                // Otherwise, command the drone to fly to the target point
                FlyToLocation(targetPoint,50);
            }


        }

        private void StopFlight(string reason = "")
        {
            droneNavigationTimer.Stop();
            currentMode = DroneFlightMode.None;

            ShipFollowingModeLabel.Text = $"Stopped: {reason}";
            ShipFollowingModeLabel.BackColor = Color.Green;

            shipFollowingMode = false; // if you still rely on this flag
                                       // Unsubscribe from events, if necessary
            if (targetShipMarker != null)
            {
                targetShipMarker.PositionChanged -= OnShipMarkerUpdated;
                targetShipMarker.SpeedChanged -= OnShipMarkerUpdated;
                targetShipMarker.HeadingChanged -= OnShipMarkerUpdated;
            }

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
            currentMode = DroneFlightMode.Intercept;

            // Subscribe to the shipMarker events
            targetShipMarker.PositionChanged += OnShipMarkerUpdated;
            targetShipMarker.SpeedChanged += OnShipMarkerUpdated;
            targetShipMarker.HeadingChanged += OnShipMarkerUpdated;

            // Update the label to indicate that intercepting is enabled
            ShipFollowingModeLabel.Text = $"Intercepting {shipMarker.ShipName}";
            ShipFollowingModeLabel.BackColor = Color.Red;

            // Calculate the initial intercept position
            droneNavigationTimer.Start();
            UpdateInterceptPosition();
        }

        // Method to handle ShipMarker updates
        private void OnShipMarkerUpdated()
        {
            UpdateInterceptPosition();
        }

        // Method to update the intercept position
        private PointLatLng UpdateInterceptPosition()
        {
            double droneSpeedMps = GetDroneGroundSpeed();
            // Ensure a minimum drone speed for calculation purposes
            droneSpeedMps = (droneSpeedMps < 5.0) ? 10.0 : droneSpeedMps;
            double shipSpeedMps = targetShipMarker.Speed * 0.514444; // Convert knots to m/s if needed
            double droneHeadingDegrees = GetDroneCurrentHeading();

            var dronePosition = GetDroneCurrentPosition();
            double distanceToShip = GetDistance(dronePosition, targetShipMarker.Position);
            double timeToIntercept = distanceToShip / droneSpeedMps;

            // Calculate intercept position based on ship's current position and heading
            PointLatLng interceptPosition = CalculateInterceptPosition(targetShipMarker.Position, targetShipMarker.Heading, shipSpeedMps, timeToIntercept);

            // Optionally, compute a refined intercept position using your other method:
            PointLatLng interceptLatLng = CalculateIntercept(
                dronePosition, droneSpeedMps, droneHeadingDegrees,
                targetShipMarker.Position, targetShipMarker.Speed, targetShipMarker.Heading
            );

            Console.WriteLine($"Intercept Coordinates: Latitude {interceptLatLng.Lat:F6}, Longitude {interceptLatLng.Lng:F6}");

            // Update the intercept marker on the map using the intercept position.
            AddInterceptMarker(interceptPosition);

            // Return the intercept position as the target point for intercept
            return interceptPosition;
        }


        private void AddInterceptMarker(PointLatLng interceptPosition)
        {
            // Remove the previous intercept marker if it exists
            var existingInterceptMarkers = markersOverlay.Markers
                .Where(m => m is InterceptMarker)
                .ToList();

            foreach (var marker in existingInterceptMarkers)
            {
                markersOverlay.Markers.Remove(marker);
            }

            // Add the new intercept marker
            InterceptMarker interceptMarker = new InterceptMarker(interceptPosition);
            markersOverlay.Markers.Add(interceptMarker);

            // Check the distance between the drone and the intercept position.
            var dronePosition = GetDroneCurrentPosition();
            double distanceToIntercept = GetDistance(dronePosition, interceptPosition);

            // If the drone is within 3 meters, stop intercepting.
            if (distanceToIntercept <= 3)
            {
                shipFollowingMode = false;
                // Unsubscribe from ship marker events:
                targetShipMarker.PositionChanged -= OnShipMarkerUpdated;
                targetShipMarker.SpeedChanged -= OnShipMarkerUpdated;
                targetShipMarker.HeadingChanged -= OnShipMarkerUpdated;

                ShipFollowingModeLabel.Text = $"Intercept complete";
                ShipFollowingModeLabel.BackColor = Color.Green;

                Console.WriteLine("Intercept complete: Drone is within 3 meters of the intercept point.");
            }
            else if (shipFollowingMode)
            {
                // Command the drone to fly to the intercept point if not yet within 3 meters.
                FlyToLocation(interceptPosition,50);
                Console.WriteLine($"Intercepting {targetShipMarker.ShipName} at updated position: Lat {interceptPosition.Lat}, Lng {interceptPosition.Lng}");
            }

            // Refresh the overlay.
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
            StopFlight("User requested stop following ship");

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
            // Show confirmation dialog before RTL
            var result = MessageBox.Show("Are you sure you want the drone to return to launch (RTL)?", "Confirm Return to Launch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                // Create the MAVLink command for RTL
                MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();
                req.target_system = 1; // Set to your drone's system ID
                req.target_component = 1; // Set to your drone's component ID
                req.command = (ushort)MAVLink.MAV_CMD.RETURN_TO_LAUNCH; // RTL command
                req.param1 = 0;
                req.param2 = 0;
                req.param3 = 0;
                req.param4 = 0;
                req.param5 = 0;
                req.param6 = 0;
                req.param7 = 0;

                byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
                SendPacket(packet);
                System.Threading.Thread.Sleep(100);
            }
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



        public static PointLatLng ConvertOffsetToLatLng(PointLatLng origin, double offsetX, double offsetY)
        {
            // Earth's radius in meters (WGS84 standard)
            double earthRadius = 6378137;

            // Calculate the change in latitude in degrees
            double deltaLat = (offsetY / earthRadius) * (180 / Math.PI);

            // Calculate the change in longitude in degrees (adjusted for current latitude)
            double deltaLng = (offsetX / earthRadius) * (180 / Math.PI) / Math.Cos(origin.Lat * Math.PI / 180);

            // Return the new geographic coordinate
            return new PointLatLng(origin.Lat + deltaLat, origin.Lng + deltaLng);
        }


        private async void StartShipMeasurementPattern()
        {
            if (droneMarker == null || targetShipMarker == null)
            {
                MessageBox.Show("Drone or ship marker is not available.");
                return;
            }

            // Open the status window.
            statusForm = new DroneStatusForm();
            statusForm.Show();
            statusForm.UpdateStatus("Starting Measurement Pattern...");

            // Get the ship's heading.
            double shipHeading = targetShipMarker.Heading; // in degrees

            statusForm.UpdateStatus("Drone altitude set to 25m for plume readings.");

            // Shift the grid center 20 meters to the back of the ship.
            // "Back" means a bearing that is shipHeading + 180°.
            PointLatLng gridCenter = GetPositionAtDistanceAndBearing(droneMarker.Position, shipHeading + 180, 20);

            // Create a visualizer instance.
            DroneMeasurementVisualizer visualizer = new DroneMeasurementVisualizer();

            // Define grid dimensions:
            // Overall area: 25 m left, 25 m right (50 m width) and 20 m front, 30 m behind (50 m height)
            // split into a 5x5 grid (each cell is 10x10 m)
            double[] colCenters = new double[] { -20, -10, 0, 10, 20 };  // horizontal offsets (east-west)
            double[] rowCenters = new double[] { 15, 5, -5, -15, -25 };   // vertical offsets (north-south)

            // Dictionary to store CO₂ readings for each grid point.
            Dictionary<PointLatLng, float> co2Readings = new Dictionary<PointLatLng, float>();
            int pointIndex = 1;

            // Precompute the grid points using a snake pattern.
            List<PointLatLng> gridPoints = new List<PointLatLng>();
            for (int row = 0; row < rowCenters.Length; row++)
            {
                if (row % 2 == 0)
                {
                    // Even row: left-to-right.
                    for (int col = 0; col < colCenters.Length; col++)
                    {
                        double offsetX = colCenters[col];
                        double offsetY = rowCenters[row];
                        PointLatLng targetPoint = ConvertOffsetToLatLng(gridCenter, offsetX, offsetY);
                        gridPoints.Add(targetPoint);
                    }
                }
                else
                {
                    // Odd row: right-to-left.
                    for (int col = colCenters.Length - 1; col >= 0; col--)
                    {
                        double offsetX = colCenters[col];
                        double offsetY = rowCenters[row];
                        PointLatLng targetPoint = ConvertOffsetToLatLng(gridCenter, offsetX, offsetY);
                        gridPoints.Add(targetPoint);
                    }
                }
            }

            // Iterate through each precomputed grid point.
            foreach (PointLatLng targetPoint in gridPoints)
            {
                statusForm.UpdateStatus($"Proceeding to grid point {pointIndex}");
                // Command the drone to fly to the target point at 25m altitude.
                FlyToLocation(targetPoint, 25);
                // Wait 5 seconds for the drone to reach the destination and for sensor stabilization.
                await Task.Delay(7000);
                // Read the CO₂ sensor value.
                float currentCO2 = InstantCO2;
                // Save the reading.
                co2Readings.Add(targetPoint, currentCO2);
                // Update the visualizer.
                visualizer.VisualizeCO2Readings(gridCenter, co2Readings, statusForm.VisualizerPictureBox);
                pointIndex++;
            }

            statusForm.UpdateStatus("Initial grid measurement pattern completed.");

            // Determine the grid point with the highest CO₂ reading.
            PointLatLng bestGridPoint = co2Readings.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            statusForm.UpdateStatus("Highest CO₂ grid point determined.");

            // Now perform additional measurements at the best grid point at different altitudes.
            int[] altimeterLevels = new int[] { 20, 25, 30 };
            Dictionary<int, float> altitudeReadings = new Dictionary<int, float>();

            foreach (int altimeter in altimeterLevels)
            {
                statusForm.UpdateStatus($"Measuring CO₂ at {altimeter}m altitude.");
                // Command the drone to fly to the best grid point at the specified altimeter.
                FlyToLocation(bestGridPoint, altimeter);
                // Wait for the drone to reach the altitude and for sensor stabilization.
                await Task.Delay(5000);
                float reading = InstantCO2;
                altitudeReadings.Add(altimeter, reading);
            }

            // Choose the altitude with the highest CO₂ reading.
            int bestAltitude = altitudeReadings.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            statusForm.UpdateStatus($"Best CO₂ reading at {bestAltitude}m altitude. Loitering...");

            // Fly to the best altitude (if not already there) and loiter for 1 minute.
            FlyToLocation(bestGridPoint, bestAltitude);
            await Task.Delay(60000); // Loiter for 60 seconds

            statusForm.UpdateStatus("Ship measurement pattern completed.");
            MessageBox.Show("Measurement pattern completed.");
        }



        /// <summary>
        /// Computes a new geographic position from an origin, given a bearing (degrees) and a distance (meters).
        /// </summary>
        public static PointLatLng GetPositionAtDistanceAndBearing(PointLatLng origin, double bearingDegrees, double distance)
        {
            double earthRadius = 6378137; // in meters (WGS84)
            double bearingRad = bearingDegrees * Math.PI / 180.0;
            double latRad = origin.Lat * Math.PI / 180.0;
            double lngRad = origin.Lng * Math.PI / 180.0;

            double newLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(distance / earthRadius) +
                                         Math.Cos(latRad) * Math.Sin(distance / earthRadius) * Math.Cos(bearingRad));
            double newLngRad = lngRad + Math.Atan2(Math.Sin(bearingRad) * Math.Sin(distance / earthRadius) * Math.Cos(latRad),
                                                   Math.Cos(distance / earthRadius) - Math.Sin(latRad) * Math.Sin(newLatRad));

            double newLat = newLatRad * 180.0 / Math.PI;
            double newLng = newLngRad * 180.0 / Math.PI;
            return new PointLatLng(newLat, newLng);
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

        private void pictureBox4_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        // Handler for LandButton click
        private void LandButton_Click(object sender, EventArgs e)
        {
            // Show confirmation dialog before landing
            var result = MessageBox.Show("Are you sure you want to land the drone at its current position?", "Confirm Landing", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                // Get the drone's current position
                PointLatLng currentPosition = GetDroneCurrentPosition();
                // Initiate landing at the current position
                LandAtLocation(currentPosition);
            }
        }

        private void txtCO2_Click(object sender, EventArgs e)
        {

        }

        private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Earth radius in km
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180.0) *
                    Math.Cos(lat2 * Math.PI / 180.0) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static int CountInRangeWithRadius(IEnumerable<EqEvent> src, DateTimeOffset start, DateTimeOffset end,
                                          double centerLat, double centerLon, double radiusKm)
        {
            return src.Count(e => e.Time >= start && e.Time < end &&
                                  DistanceKm(centerLat, centerLon, e.Lat, e.Lon) <= radiusKm);
        }




    }

}

