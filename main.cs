using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
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
using static IERAX_MissionControl.HeatmapGrid;
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
        // our target compid;
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

        private GMapOverlay plansOverlay;
        private GMapOverlay _debugOverlay;


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

        // Heatmap runtime
        private System.Windows.Forms.Timer _heatTimer;
        private bool _heatEnabled;
        private double _heatMinPpm = 400, _heatMaxPpm = 15000;
        private double _gaussianRadiusM = 0.0; // 0 = hard binning, >0 = smoothing


        private GMapOverlay quakesOverlay;
        private System.Windows.Forms.Timer quakeTimer;

        private const double MinLat = 36.0, MaxLat = 37.0;   // Santorini area (tweak)
        private const double MinLon = 25, MaxLon = 26.0;
        private GMapOverlay lzOverlay;
        private Bitmap lzIcon;

        private GMapOverlay gridOverlay;


        // In Main.cs
        private CameraForm cameraForm;
        private Boolean shipFollowingMode = false;
        private const double EarthRadius = 6378137.0;  // Earth's radius in meters

        private System.Windows.Forms.Timer droneNavigationTimer;
        private DroneFlightMode currentMode = DroneFlightMode.None;

        private HeatmapGrid _heat;

        private Task<bool> SetModeGuidedAsyncForActiveCtx() => SendDoSetModeAsync(4); // GUIDED
        private Task<bool> SetModeAutoAsyncForActiveCtx() => SendDoSetModeAsync(3); // AUTO


        public enum Corner { NE, NW, SE, SW }

        private readonly Dictionary<Corner, List<PointLatLng>> _quadrantPaths =
         new Dictionary<Corner, List<PointLatLng>>();
        private const float DEFAULT_ALT_AGL_M = 120f;        // adjust to your op limits
        private const float CORNER_REACH_THRESH_M = 8f;      // how close is "at corner"
        private const int NAV_POLL_MS = 500;
        private readonly object _txLock = new object();
        private bool? _armUiLast;
        private DateTime _armUiLastChangeUtc = DateTime.MinValue;
        private static readonly TimeSpan ArmUiMinDwell = TimeSpan.FromMilliseconds(400);


        // ================= MULTI-DRONE SUPPORT =================
        // Each connection/drone context tracked separately
        private class DroneConnectionContext
        {
            public string ConnectionId { get; set; }
            public bool IsTcp { get; set; }
            public TcpClient TcpClient { get; set; }
            public System.IO.Stream Stream { get; set; }
            public SerialPort SerialPort { get; set; }
            // NEW: cache IDs from incoming MAVLink messages
            public byte SysId { get; set; } = 0;
            public byte CompId { get; set; } = 0;
            public MavlinkMessageHandler Handler { get; set; }
            public DroneMarker Marker { get; set; }
            public CancellationTokenSource Cts { get; set; }
            public bool IsConnected { get; set; }
            public double LastRelAltMeters { get; set; } = double.NaN;
            public DateTime LastRelAltTimeUtc { get; set; } = DateTime.MinValue;

            // Last known flight status (from HEARTBEAT / handler)
            public bool IsArmed { get; set; }
            public bool? LastIsArmedFromHb { get; set; } // optional dedupe
            public string LastModeName { get; set; }     // e.g., "GUIDED", "AUTO"
            public DateTime LastHeartbeatUtc { get; set; } = DateTime.MinValue;

      

        }

        // All active drone connections by connectionId
        private readonly Dictionary<string, DroneConnectionContext> _connections = new Dictionary<string, DroneConnectionContext>();
        // Currently active/selected connection id (used by default in UI actions)
        private string _activeConnectionId;
        private int _connCounter = 0;

        // Put in your class
        private readonly object _ackLock = new object();
        private readonly Dictionary<ushort, TaskCompletionSource<MAVLink.mavlink_command_ack_t>> _pendingAcks
            = new Dictionary<ushort, TaskCompletionSource<MAVLink.mavlink_command_ack_t>>();


        private string GenerateConnectionId(string kind, string hint)
        {
            // kind: "tcp" or "serial", hint: ip:port or COM name
            _connCounter++;
            return $"{kind}:{hint}#{_connCounter}";
        }

        private DroneConnectionContext GetActiveConnection()
        {
            if (!string.IsNullOrEmpty(_activeConnectionId) && _connections.TryGetValue(_activeConnectionId, out var ctx))
                return ctx;
            // fallback: first available
            var first = _connections.Values.FirstOrDefault();
            return first;
        }
        // =======================================================

        private void GenerateAndDrawQuadrantPaths()
        {
            double overallCenterLat = 36.4044;
            double overallCenterLon = 25.3975;
            double quadSize = 1000;    // meters
            double laneSpacing = 250;  // meters

            var quadCenters = new Dictionary<Corner, PointLatLng>
    {
        { Corner.NW, ToLatLon(overallCenterLat, overallCenterLon, -quadSize/2,  quadSize/2) },
        { Corner.NE, ToLatLon(overallCenterLat, overallCenterLon,  quadSize/2,  quadSize/2) },
        { Corner.SE, ToLatLon(overallCenterLat, overallCenterLon,  quadSize/2, -quadSize/2) },
        { Corner.SW, ToLatLon(overallCenterLat, overallCenterLon, -quadSize/2, -quadSize/2) }
    };

            var pathAlpha = PlanFromCorner(new QuadrantPlan
            {
                CenterLatDeg = quadCenters[Corner.NE].Lat,
                CenterLonDeg = quadCenters[Corner.NE].Lng,
                Width_m = quadSize,
                Height_m = quadSize,
                LaneSpacing_m = laneSpacing,
                StartCorner = Corner.NE
            });

            var pathBravo = PlanFromCorner(new QuadrantPlan
            {
                CenterLatDeg = quadCenters[Corner.SE].Lat,
                CenterLonDeg = quadCenters[Corner.SE].Lng,
                Width_m = quadSize,
                Height_m = quadSize,
                LaneSpacing_m = laneSpacing,
                StartCorner = Corner.SE
            });

            var pathCharlie = PlanFromCorner(new QuadrantPlan
            {
                CenterLatDeg = quadCenters[Corner.SW].Lat,
                CenterLonDeg = quadCenters[Corner.SW].Lng,
                Width_m = quadSize,
                Height_m = quadSize,
                LaneSpacing_m = laneSpacing,
                StartCorner = Corner.SW
            });

            var pathDelta = PlanFromCorner(new QuadrantPlan
            {
                CenterLatDeg = quadCenters[Corner.NW].Lat,
                CenterLonDeg = quadCenters[Corner.NW].Lng,
                Width_m = quadSize,
                Height_m = quadSize,
                LaneSpacing_m = laneSpacing,
                StartCorner = Corner.NW
            });

            // Cache by corner (so we can pick the correct one later)
            _quadrantPaths[Corner.NE] = pathAlpha;
            _quadrantPaths[Corner.SE] = pathBravo;
            _quadrantPaths[Corner.SW] = pathCharlie;
            _quadrantPaths[Corner.NW] = pathDelta;

            // Your existing draw
            DrawAllQuadrantPlans(pathAlpha, pathBravo, pathCharlie, pathDelta);
        }


        public async Task StartVolcanoHeatmapAsync()
        {
            
            try
            {
                if (_quadrantPaths.Count == 0)
                {
                    Console.WriteLine("[HEATMAP] Generating quadrant paths...");
                    GenerateAndDrawQuadrantPaths();
                }

                // Validate active link
                if (string.IsNullOrEmpty(_activeConnectionId) ||
                    !_connections.TryGetValue(_activeConnectionId, out var ctx) ||
                    !ctx.IsConnected || ctx.Stream == null || !ctx.Stream.CanWrite)
                {
                    Console.WriteLine("[HEATMAP] No active MAVLink connection.");
                    MessageBox.Show("No active MAVLink connection.", "Volcano Heatmap",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var dronePos = GetDroneCurrentPosition();

                Console.WriteLine($"[HEATMAP] Drone position: {dronePos.Lat:F6}, {dronePos.Lng:F6}");
                Console.WriteLine($"[HEATMAP] InstantCO2: {InstantCO2:F1} ppm");

                // Update heatmap immediately
                if (_heat == null)
                {
                    Console.WriteLine("[HEATMAP] Heatmap not initialized — skipping update.");
                }
               
                EnsureHeatmapTimerRunning();
              

                if (double.IsNaN(dronePos.Lat) || double.IsNaN(dronePos.Lng) ||
                    (Math.Abs(dronePos.Lat) < 1e-6 && Math.Abs(dronePos.Lng) < 1e-6))
                {
                    Console.WriteLine("[HEATMAP] Invalid or missing drone position.");
                    MessageBox.Show("Active drone position not available.", "Volcano Heatmap",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Compute closest corner
                double overallCenterLat = 36.4044;
                double overallCenterLon = 25.3975;
                double quadSize = 1000.0; // meters

                var refs = new Dictionary<Corner, PointLatLng>
        {
            { Corner.NW, ToLatLon(overallCenterLat, overallCenterLon, -quadSize/2,  quadSize/2) },
            { Corner.NE, ToLatLon(overallCenterLat, overallCenterLon,  quadSize/2,  quadSize/2) },
            { Corner.SE, ToLatLon(overallCenterLat, overallCenterLon,  quadSize/2, -quadSize/2) },
            { Corner.SW, ToLatLon(overallCenterLat, overallCenterLon, -quadSize/2, -quadSize/2) },
        };

                var chosenCorner = refs
                    .Select(kvp => new { kvp.Key, Dist = GetDistance(dronePos, kvp.Value) })
                    .OrderBy(x => x.Dist)
                    .First().Key;

                Console.WriteLine($"[HEATMAP] Closest corner: {chosenCorner}");

                if (!_quadrantPaths.TryGetValue(chosenCorner, out var chosenPath) ||
                    chosenPath == null || chosenPath.Count == 0)
                {
                    Console.WriteLine($"[HEATMAP] No path cached for {chosenCorner}.");
                    MessageBox.Show($"No path cached for {chosenCorner}.", "Volcano Heatmap",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var startWp = chosenPath[0];
                Console.WriteLine($"[HEATMAP] Starting waypoint: {startWp.Lat:F6}, {startWp.Lng:F6}");

                // Ensure GUIDED + armed
                Console.WriteLine("[HEATMAP] Setting mode GUIDED...");
                await SendDoSetModeAsync(4); // GUIDED
                await WaitUntilModeIsAsync("GUIDED", 3000);

                if (!ctx.IsArmed)
                {
                    Console.WriteLine("[HEATMAP] Arming drone...");
                    await ArmIfNeededAsyncForActiveCtx(true);
                    await WaitUntilArmedAsync(true, 6000);
                }

                // Takeoff if on ground
                double relAlt = GetLastRelativeAltitudeMeters();
                Console.WriteLine($"[HEATMAP] Relative altitude: {relAlt:F1} m");

                if (double.IsNaN(relAlt) || relAlt < 2.0)
                {
                    Console.WriteLine("[HEATMAP] Taking off to 15 m...");
                    var okTo = await GuidedTakeoffAsync(altRelM: 15f);
                    if (!okTo)
                    {
                        Console.WriteLine("[HEATMAP] Aborting: takeoff failed.");
                        return;
                    }
                }

                // Fly to start corner using streaming set-position
                Console.WriteLine("[HEATMAP] Flying to start corner...");
                bool reached = await FlyGuidedToAsyncStreaming(
                    startWp.Lat, startWp.Lng, altRelM: 120f, stopMeters: 5, hz: 3, timeoutSec: 90);

                if (!reached)
                    Console.WriteLine("[HEATMAP] WARN: did not reach start corner within timeout; continuing anyway.");

                // Upload mission and start AUTO
                Console.WriteLine("[HEATMAP] Uploading mission...");
                await UploadMissionAsyncForActiveCtx(chosenPath, altRelMeters: 120f);
                Console.WriteLine("[HEATMAP] Starting mission AUTO...");
                await StartMissionAsyncForActiveCtx(startIndex: 0);
                await SetModeAutoAsyncForActiveCtx();
                await WaitUntilModeIsAsync("AUTO", 3000);

                Console.WriteLine($"[HEATMAP] Mission started from {chosenCorner} corner.");
                MessageBox.Show($"Starting heatmap from {chosenCorner} corner.", "Volcano Heatmap",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HEATMAP] ERROR: {ex}");
                MessageBox.Show($"Failed to start volcano heatmap: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private double GetLastRelativeAltitudeMeters()
        {
            var ctx = GetActiveConnection();
            if (ctx == null) return double.NaN;

            // If we never got any altitude
            if (ctx.LastRelAltTimeUtc == DateTime.MinValue)
                return double.NaN;

            // Reject values older than 5 seconds
            if ((DateTime.UtcNow - ctx.LastRelAltTimeUtc).TotalSeconds > 5.0)
                return double.NaN;

            return ctx.LastRelAltMeters;
        }

        private DroneConnectionContext GetActiveCtxOrNull()
        {
            if (string.IsNullOrEmpty(_activeConnectionId)) return null;
            return _connections.TryGetValue(_activeConnectionId, out var ctx) ? ctx : null;
        }

        private bool IsCtxConnected(DroneConnectionContext ctx)
        {
            return ctx != null && ctx.IsConnected && ctx.Stream != null && ctx.Stream.CanWrite;
        }

        // ====================== Target IDs helpers (per active connection) ======================

        private (byte sys, byte comp) GetActiveTargetIds()
        {
            if (string.IsNullOrEmpty(_activeConnectionId)) return (1, 1);

            if (_connections.TryGetValue(_activeConnectionId, out var ctx) && ctx != null)
            {
                var sys = ctx.SysId != 0 ? ctx.SysId : (byte)1;
                var comp = ctx.CompId != 0 ? ctx.CompId : (byte)1;
                return (sys, comp);
            }

            return (1, 1);
        }

        
        // ====================== SET MODE (GUIDED / AUTO) ======================
        // ArduCopter custom modes: AUTO=3, GUIDED=4
        // Use MAV_CMD_DO_SET_MODE with param1=MAV_MODE_FLAG_CUSTOM_MODE_ENABLED, param2=custom_mode


        private async Task<bool> SendDoSetModeAsync(uint customMode)
        {
            var (sys, comp) = GetActiveTargetIds();

            var cmd = new MAVLink.mavlink_command_long_t
            {
                target_system = sys,
                target_component = comp,
                command = (ushort)MAVLink.MAV_CMD.DO_SET_MODE, // 176
                param1 = 1f, // MAV_MODE_FLAG_CUSTOM_MODE_ENABLED
                param2 = customMode
            };

            var waiter = RegisterAckWaiter((ushort)MAVLink.MAV_CMD.DO_SET_MODE, timeoutMs: 3000);
            var pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd);
            SendPacket(pkt, $"SET_MODE custom={customMode}");

            try
            {
                var ack = await waiter.Task;
                Console.WriteLine($"SET_MODE ACK: {(MAVLink.MAV_RESULT)ack.result}");
                return ack.result == (byte)MAVLink.MAV_RESULT.ACCEPTED;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SET_MODE ACK ERROR: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ArmIfNeededAsyncForActiveCtx(bool arm = true)
        {
            var (sys, comp) = GetActiveTargetIds();

            var cmd = new MAVLink.mavlink_command_long_t
            {
                target_system = sys,
                target_component = comp,
                command = (ushort)MAVLink.MAV_CMD.COMPONENT_ARM_DISARM, // 400
                param1 = arm ? 1f : 0f
            };

            var waiter = RegisterAckWaiter((ushort)MAVLink.MAV_CMD.COMPONENT_ARM_DISARM, timeoutMs: 3000);
            var pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd);
            SendPacket(pkt, $"ARM={arm}");

            try
            {
                var ack = await waiter.Task;
                Console.WriteLine($"ARM ACK: {(MAVLink.MAV_RESULT)ack.result}");
                return ack.result == (byte)MAVLink.MAV_RESULT.ACCEPTED;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ARM ACK ERROR: {ex.Message}");
                return false;
            }
        }

        


        public async Task<bool> GuidedTakeoffAsync(float altRelM)
        {
            await SendDoSetModeAsync(4);
            await WaitUntilModeIsAsync("GUIDED", 3000);

            var armOk = await ArmIfNeededAsyncForActiveCtx(true);
            var armed = await WaitUntilArmedAsync(true, 6000);
            if (!armOk || !armed)
            {
                Console.WriteLine("TAKEOFF aborted: not armed.");
                return false;
            }

            var (sys, comp) = GetActiveTargetIds();
            var takeoff = new MAVLink.mavlink_command_long_t
            {
                target_system = sys,
                target_component = comp,
                command = (ushort)MAVLink.MAV_CMD.TAKEOFF, // 22
                param7 = altRelM
            };

            var waiter = RegisterAckWaiter((ushort)MAVLink.MAV_CMD.TAKEOFF, 8000);
            var pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, takeoff);
            SendPacket(pkt, $"TAKEOFF {altRelM}m");

            try
            {
                var ack = await waiter.Task;
                Console.WriteLine($"TAKEOFF ACK: {(MAVLink.MAV_RESULT)ack.result}");
                return ack.result == (byte)MAVLink.MAV_RESULT.ACCEPTED;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TAKEOFF ACK ERROR: {ex.Message}");
                return false;
            }
        }



        public async Task<bool> GuidedGoto_RepositionAsync(double latDeg, double lonDeg, float altRelM, float accRadiusM = 3f)
        {
            var (sys, comp) = GetActiveTargetIds();
            float mask = 1f + 2f; // change pos + alt

            var cmd = new MAVLink.mavlink_command_long_t
            {
                target_system = sys,
                target_component = comp,
                command = (ushort)MAVLink.MAV_CMD.DO_REPOSITION, // 512
                param1 = 0f,
                param2 = mask,
                param3 = accRadiusM,
                param4 = float.NaN,
                param5 = (float)latDeg,
                param6 = (float)lonDeg,
                param7 = altRelM
            };

            var waiter = RegisterAckWaiter((ushort)MAVLink.MAV_CMD.DO_REPOSITION, timeoutMs: 3000);
            var pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd);
            SendPacket(pkt, $"REPOSITION to {latDeg:F6},{lonDeg:F6}@{altRelM:F1}m");

            try
            {
                var ack = await waiter.Task;
                Console.WriteLine($"REPOSITION ACK: {(MAVLink.MAV_RESULT)ack.result}");
                return ack.result == (byte)MAVLink.MAV_RESULT.ACCEPTED;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"REPOSITION ACK ERROR: {ex.Message}");
                return false;
            }
        }


 

        public void GuidedGoto_DO_REPOSITION(double latDeg, double lonDeg, float altRelMeters,
                                             float acceptRadiusM = 3f, float yawDeg = float.NaN)
        {
            var (sys, comp) = GetActiveTargetIds();

            // param2 bitmask: 1=change pos, 2=change alt, 4=change yaw
            float mask = 1f + 2f + (float.IsNaN(yawDeg) ? 0f : 4f);

            var cmd = new MAVLink.mavlink_command_long_t
            {
                target_system = sys,
                target_component = comp,
                command = (ushort)MAVLink.MAV_CMD.DO_REPOSITION, // 512
                confirmation = 0,
                param1 = 0f,                 // ground speed m/s (0 = unchanged)
                param2 = mask,               // what to change
                param3 = acceptRadiusM,      // acceptance radius
                param4 = yawDeg,             // yaw deg (NaN ignored)
                param5 = (float)latDeg,      // lat
                param6 = (float)lonDeg,      // lon
                param7 = altRelMeters        // alt (relative)
            };
            var pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd);
            SendPacket(pkt);
        }



        // Optional: poll until close enough
        private async Task WaitUntilNearAsync(PointLatLng target, double meters = 5, int pollMs = 500, int timeoutSec = 45)
        {
            var stop = DateTime.UtcNow.AddSeconds(timeoutSec);
            while (DateTime.UtcNow < stop)
            {
                var pos = GetDroneCurrentPosition();
                if (GetDistance(pos, target) <= meters) return;
                await Task.Delay(pollMs);
            }
        }

        // ====================== Mission upload & start (proper handshake) ======================

        private async Task UploadMissionAsyncForActiveCtx(IReadOnlyList<PointLatLng> wps, float altRelMeters)
        {
            var (sys, comp) = GetActiveTargetIds();

            // 1) Send MISSION_COUNT
            var count = new MAVLink.mavlink_mission_count_t
            {
                target_system = sys,
                target_component = comp,
                count = (ushort)wps.Count,
                mission_type = (byte)MAVLink.MAV_MISSION_TYPE.MISSION
            };
            SendPacket(mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_COUNT, count));
            await Task.Delay(100);

            // 2) Send items as MISSION_ITEM_INT (GLOBAL_RELATIVE_ALT_INT)
            for (ushort i = 0; i < wps.Count; i++)
            {
                var p = wps[i];
                var item = new MAVLink.mavlink_mission_item_int_t
                {
                    target_system = sys,
                    target_component = comp,
                    seq = i,
                    frame = (byte)MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT_INT,
                    command = (ushort)MAVLink.MAV_CMD.WAYPOINT, // NAV_WAYPOINT
                    current = (byte)(i == 0 ? 1 : 0),
                    autocontinue = 1,
                    param1 = 0,            // hold time
                    param2 = 3,            // acceptance radius (m)
                    param3 = 0,            // pass-through
                    param4 = float.NaN,    // yaw (NaN = default)
                    x = (int)Math.Round(p.Lat * 1e7),
                    y = (int)Math.Round(p.Lng * 1e7),
                    z = altRelMeters,
                    mission_type = (byte)MAVLink.MAV_MISSION_TYPE.MISSION
                };
                SendPacket(mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT, item));
                await Task.Delay(50);
            }

            // (Optionally wait for MISSION_ACK here if you handle incoming acks)
            await Task.Delay(150);
        }

        private async Task StartMissionAsyncForActiveCtx(int startIndex = 0)
        {
            var (sys, comp) = GetActiveTargetIds();

            // Option A: MAV_CMD_MISSION_START
            var cmd = new MAVLink.mavlink_command_long_t
            {
                target_system = sys,
                target_component = comp,
                command = (ushort)MAVLink.MAV_CMD.MISSION_START, // 300
                param1 = startIndex,
                param2 = 0, // end index (0 = last)
                confirmation = 0
            };
            SendPacket(mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd));
            await Task.Delay(150);

            // Ensure AUTO mode
            await SetModeAutoAsyncForActiveCtx();
        }


        private void DrawAllQuadrantPlans(
      List<PointLatLng> pathNE,
      List<PointLatLng> pathSE,
      List<PointLatLng> pathSW,
      List<PointLatLng> pathNW)
        {
            if (gMapControl1.InvokeRequired)
            {
                gMapControl1.Invoke(new Action(() => DrawAllQuadrantPlans(pathNE, pathSE, pathSW, pathNW)));
                return;
            }

            EnsurePlansOverlay();
            ClearPlansOverlay();

            DrawPath("Alpha (NE)", pathNE, Color.DeepSkyBlue);
            DrawPath("Bravo (SE)", pathSE, Color.Orange);
            DrawPath("Charlie (SW)", pathSW, Color.MediumSeaGreen);
            DrawPath("Delta (NW)", pathNW, Color.MediumOrchid);

            gMapControl1.Refresh();
        }

        private void EnsurePlansOverlay()
        {
            if (plansOverlay == null)
            {
                plansOverlay = new GMapOverlay("plans_overlay");
                gMapControl1.Overlays.Add(plansOverlay);
            }
        }

        private void ClearPlansOverlay()
        {
            if (plansOverlay == null) return;
            plansOverlay.Routes.Clear();
            plansOverlay.Markers.Clear();
        }

        private void DrawPath(string name, IList<PointLatLng> path, Color color)
        {
            if (path == null || path.Count == 0) return;
            EnsurePlansOverlay();

            // Route line
            var route = new GMapRoute(path, name)
            {
                Stroke = new Pen(color, 3f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Solid }
            };
            plansOverlay.Routes.Add(route);

            // Start / End markers
            var start = path[0];
            var end = path[path.Count - 1];

            var startMk = new GMarkerGoogle(start, GMarkerGoogleType.green_small)
            {
                ToolTipText = $"{name} — START",
                ToolTipMode = MarkerTooltipMode.OnMouseOver
            };
            var endMk = new GMarkerGoogle(end, GMarkerGoogleType.red_small)
            {
                ToolTipText = $"{name} — END",
                ToolTipMode = MarkerTooltipMode.OnMouseOver
            };

            plansOverlay.Markers.Add(startMk);
            plansOverlay.Markers.Add(endMk);
        }

        public sealed class QuadrantPlan
        {
            public double CenterLatDeg;
            public double CenterLonDeg;
            public double Width_m;
            public double Height_m;
            public double LaneSpacing_m;
            public Corner StartCorner;
            public double HeadingDeg = 0; // optional rotation
        }


        public MPIeraxMain()
        {
            InitializeMavlinkHandler();
            InitializeComponent();
            InitializeMap();
            AddLandingZones();
            GenerateAndDrawQuadrantPaths();
            // Fine grid (250 m):
            AddGridOverNeaKameni(cellKm: 0.25);
            InitializeWebSocket();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.TopMost = false;

            // Initialize the timer
            droneNavigationTimer = new System.Windows.Forms.Timer();
            droneNavigationTimer.Interval = 1000; // 1 second
            droneNavigationTimer.Tick += DroneNavigationTimer_Tick;

            // Wire up the LandButton click event
            this.LandButton.Click += new System.EventHandler(this.LandButton_Click);

            // Rewire main Connect button to open the Connections window
            try
            {
                this.but_connect.Click -= new System.EventHandler(this.but_connect_Click);
                this.but_connect.Click += new System.EventHandler(this.OpenConnectionsWindow_Click);
            }
            catch { /* ignore if not wired yet */ }

            // Initialize Active Drone dropdown
            RefreshActiveDroneComboItems();
        }

        private class EqEvent
        {
            public DateTimeOffset Time { get; set; }
            public double Mag { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
            public string Place { get; set; }
        }

        private sealed class LandingZone
        {
            public string Name { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
            public string Description { get; set; }
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

        private void AddLandingZones()
        {
            // Create overlay if not already
            if (lzOverlay == null)
            {
                lzOverlay = new GMapOverlay("landing_zones");
                gMapControl1.Overlays.Add(lzOverlay);
            }

            // Load icon from Resources\Images in output folder
            if (lzIcon == null)
            {
                string iconPath = Path.Combine(Application.StartupPath, "Resources", "Images", "landingzone.png");
                if (!File.Exists(iconPath))
                {
                    MessageBox.Show($"Landing zone icon not found:\n{iconPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                lzIcon = new Bitmap(iconPath);
                lzIcon = new Bitmap(lzIcon, new Size(32, 32)); // scale down
            }

            // Define LZs
            var zones = new List<LandingZone>
    {
        new LandingZone {
            Name = "LZ Alpha",
            Lat = 36.415797, Lon = 25.427891,
            Description = "Northeast of volcano, near port facilities and road access."
        },
        new LandingZone {
            Name = "LZ Bravo",
            Lat = 36.386587, Lon = 25.427945,
            Description = "East side, adjacent to utility infrastructure for sustained ops."
        },
        new LandingZone {
            Name = "LZ Charlie",
            Lat = 36.364377, Lon = 25.383140,
            Description = "South, low/flat terrain near sea level; sheltered in N winds."
        },
        new LandingZone {
            Name = "LZ Delta",
            Lat = 36.412694, Lon = 25.350605,
            Description = "West/NW sector position."
        }
    };

            // Add markers to overlay
            foreach (var z in zones)
            {
                var pos = new PointLatLng(z.Lat, z.Lon);
                var marker = new GMarkerGoogle(pos, lzIcon)
                {
                    ToolTipText = $"{z.Name}\n{z.Description}",
                    ToolTipMode = MarkerTooltipMode.OnMouseOver,
                    Offset = new Point(-lzIcon.Width / 2, -lzIcon.Height) // tip at coordinates
                };
                lzOverlay.Markers.Add(marker);
            }

            gMapControl1.Refresh();
        }

        private void AddGridOverNeaKameni(
    double centerLat = 36.4044,
    double centerLon = 25.3975,
    double widthKm = 2.0,
    double heightKm = 2.0,
    double cellKm = 0.25)   // 0.25 = 250 m, or try 0.5 for coarser
        {
            if (gridOverlay == null)
            {
                gridOverlay = new GMapOverlay("nea_kameni_grid");
                gMapControl1.Overlays.Add(gridOverlay);
            }
            else
            {
                gridOverlay.Polygons.Clear();
                gridOverlay.Routes.Clear();
                gridOverlay.Markers.Clear();
            }

            // km -> degrees (use center lat for longitude scale)
            double kmToLatDeg = 1.0 / 111.32; // ~deg per km
            double kmToLonDeg = 1.0 / (111.32 * Math.Cos(centerLat * Math.PI / 180.0));

            double halfWkm = widthKm / 2.0;
            double halfHkm = heightKm / 2.0;

            // grid counts
            int cols = (int)Math.Round(widthKm / cellKm);
            int rows = (int)Math.Round(heightKm / cellKm);

            // re-snap step to fit exactly
            double stepLatDeg = cellKm * kmToLatDeg;
            double stepLonDeg = cellKm * kmToLonDeg;

            double minLat = centerLat - (halfHkm * kmToLatDeg);
            double minLon = centerLon - (halfWkm * kmToLonDeg);

            // Style
            var stroke = new Pen(Color.FromArgb(160, 255, 255, 255), 1f);   // light outline
            var fill = Color.FromArgb(40, 0, 150, 255);                   // faint fill

            for (int r = 0; r < rows; r++)
            {
                double lat0 = minLat + r * stepLatDeg;
                double lat1 = lat0 + stepLatDeg;
                for (int c = 0; c < cols; c++)
                {
                    double lon0 = minLon + c * stepLonDeg;
                    double lon1 = lon0 + stepLonDeg;

                    var rect = new List<PointLatLng>
            {
                new PointLatLng(lat0, lon0),
                new PointLatLng(lat0, lon1),
                new PointLatLng(lat1, lon1),
                new PointLatLng(lat1, lon0)
            };

                    var poly = new GMap.NET.WindowsForms.GMapPolygon(rect, $"cell_{r}_{c}")
                    {
                        Stroke = stroke,
                        Fill = new SolidBrush(fill),
                        IsHitTestVisible = false
                    };

                    gridOverlay.Polygons.Add(poly);
                }
            }

            gMapControl1.Refresh();
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
                $"30d: {monthNow} ({a30} {p30})   <{radius}km: {volcMonth}")
            ;

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
            // Initialize a default MavlinkMessageHandler (for legacy single-drone flow)
            mavlinkMessageHandler = new MavlinkMessageHandler(
                pos => UpdateDroneMarkerForConnection(_activeConnectionId ?? "default", pos),
                UpdateArmStatusBox,
                UpdateAltimeterBox,
                UpdateDroneModeTextBox,
                UpdateTextLabelGUI);
        }

        private void RefreshActiveDroneComboItems()
        {
            if (ActiveDroneComboBox == null)
                return;

            if (ActiveDroneComboBox.InvokeRequired)
            {
                ActiveDroneComboBox.Invoke(new Action(RefreshActiveDroneComboItems));
                return;
            }

            var selected = _activeConnectionId;
            ActiveDroneComboBox.Items.Clear();
            foreach (var id in _connections.Keys)
            {
                ActiveDroneComboBox.Items.Add(id);
            }

            if (!string.IsNullOrEmpty(selected) && _connections.ContainsKey(selected))
            {
                ActiveDroneComboBox.SelectedItem = selected;
            }
            else if (ActiveDroneComboBox.Items.Count > 0)
            {
                ActiveDroneComboBox.SelectedIndex = 0;
            }
        }

        private void ActiveDroneComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ActiveDroneComboBox.SelectedItem is string id && _connections.ContainsKey(id))
            {
                _activeConnectionId = id;
                // Clear the projected path when active drone changes
                if (markersOverlay != null)
                {
                    markersOverlay.Routes.Clear();
                    gMapControl1?.Refresh();
                }
            }
        }


        private void InitializeMap()
        {
            ConfigureMap();

           

            markersOverlay = new GMapOverlay("markers");
            gMapControl1.Overlays.Add(markersOverlay);

            quakesOverlay = new GMapOverlay("quakes");
            gMapControl1.Overlays.Add(quakesOverlay);

            // Do not create a single global drone marker here; markers are created per-connection
            // and added when connections are established.

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

        private static AisData MakeTestAisData(
         string mmsi, string name,
         double lat, double lon,
         double cogDeg, double sogKnots, int trueHeadingDeg,
         bool useClassB = true)
            {
                var data = new AisData
                {
                    MetaData = new MetaData
                    {
                        MMSI = mmsi,
                        ShipName = name,
                        Latitude = lat,
                        Longitude = lon
                    },
                    MessageType = useClassB ? "StandardClassBPositionReport" : "PositionReport",
                    Message = new AisMessage()
                };

                if (useClassB)
                {
                    data.Message.StandardClassBPositionReport = new StandardClassBPositionReport
                    {
                        Latitude = lat,
                        Longitude = lon,
                        Cog = cogDeg,
                        Sog = sogKnots,
                        TrueHeading = trueHeadingDeg, // use 511 if unknown
                        Timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        PositionAccuracy = true
                    };
                }
                else
                {
                    data.Message.PositionReport = new PositionReport
                    {
                        Latitude = lat,
                        Longitude = lon,
                        Cog = cogDeg,
                        Sog = sogKnots,
                        TrueHeading = trueHeadingDeg, // use 511 if unknown
                        Timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        PositionAccuracy = true
                    };
                }

                // Optional: static info (your UpdateShipPosition doesn't use it yet)
                data.Message.ShipStaticData = new ShipStaticData
                {
                    Type = 70,          // e.g., Cargo (adjust as needed)
                    Name = name,
                    CallSign = "TEST",
                    ImoNumber = 0
                };

                return data;
        }

        private void SendTestShipsOnce()
        {
            // Santorini caldera center
            double calderaLat = 36.4044;
            double calderaLon = 25.3975;

            // ~0.01° lat ≈ ~1.1 km. Tweak offsets as you like.
            var testShips = new List<AisData>
    {
        MakeTestAisData("240000001", "CALDERA STAR",
            calderaLat + 0.010, calderaLon + 0.022,  30, 1.5,  32, useClassB: true),

        MakeTestAisData("240000002", "THIRA PRINCESS",
            calderaLat - 0.008, calderaLon + 0.024, 270,  1.0, 268, useClassB: false),

        MakeTestAisData("240000003", "NEA KAMENI",
            calderaLat + 0.006, calderaLon + 0.016, 180, 1.2, 183, useClassB: true),

        MakeTestAisData("240000004", "ATLANTIS II",
            calderaLat - 0.012, calderaLon + 0.012,  45,  2.5,  47, useClassB: false),
    };

            foreach (var s in testShips)
            {
                UpdateShipPosition(s); // your existing method
            }
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
                    SendTestShipsOnce();
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
            gMapControl1.Position = new PointLatLng(36.41, 25.42);
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

            double centerLat = 36.4044;
            double centerLon = 25.3975;

            // Create once
            if (_heat == null)
            {
                _heat = new HeatmapGrid(centerLat, centerLon, sizeMeters: 2000.0, cellMeters: 250.0, map: gMapControl1); // <-- your GMapControl name
                _heat.SetAggregation(HeatAggregation.Max);
            }

            if (_debugOverlay == null)
            {
                _debugOverlay = new GMapOverlay("heat_debug");
                gMapControl1.Overlays.Add(_debugOverlay);
            }


        }

        private void MarkersOverlay_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (item is ShipMarker shipMarker)
            {
                ShowShipInfo(shipMarker);
            }
        }

        // Opens the connections window instead of directly connecting
        private void OpenConnectionsWindow_Click(object sender, EventArgs e)
        {
            using (var dlg = new ConnectionsForm(this))
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ShowDialog(this);
            }
        }

        // Legacy direct connect method kept for programmatic use
        public async Task<bool> TryConnectToAsync(string selectedConnection)
        {
            if (string.IsNullOrWhiteSpace(selectedConnection))
                return false;

            try
            {
                // Also reflect selection in the existing combo, for backward-compat with existing code
                if (CMB_comport.InvokeRequired)
                    CMB_comport.Invoke(new Action(() => CMB_comport.Text = selectedConnection));
                else
                    CMB_comport.Text = selectedConnection;

                if (selectedConnection.Contains(":")) // TCP Connection Handling
                {
                    // Multiple TCP connections supported; connect without disconnecting others
                    string[] parts = selectedConnection.Split(':');
                    string ip = parts[0];
                    int port = int.Parse(parts[1]);
                    var ok = await ConnectViaTCP(ip, port);
                    return ok;
                }
                else // Serial Port Connection Handling
                {
                    // For now, maintain single serialPort1 behavior (legacy). Multi-serial can be added similarly.
                    if (serialPort1.IsOpen)
                    {
                        DisconnectSerial();
                        return false;
                    }
                    else
                    {
                        var ok = ConnectSerial(selectedConnection);
                        return ok;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetConnectButton(); // Reset button in case of failure
                return false;
            }
        }

        private void but_connect_Click(object sender, EventArgs e)
        {
            // Old behavior replaced by a connections window. Kept here for compatibility if called elsewhere.
            OpenConnectionsWindow_Click(sender, e);
        }

        // Function to connect via Serial
        private bool ConnectSerial(string portName)
        {
            try
            {
                serialPort1.PortName = portName;
                //serialPort1.BaudRate = int.Parse(cmb_baudrate.Text);
                serialPort1.BaudRate = 9600;
                serialPort1.Open();
                serialPort1.ReadTimeout = 2000;

                this.Invoke(new Action(() =>
                {
                    but_connect.Text = "Disconnect";
                    but_connect.BackColor = Color.Green;
                    but_connect.ForeColor = Color.White;
                }));

                // Create connection context for serial
                string id = GenerateConnectionId("serial", portName);
                var ctx = new DroneConnectionContext
                {
                    ConnectionId = id,
                    IsTcp = false,
                    SerialPort = serialPort1,
                    Stream = serialPort1.BaseStream,
                    Cts = new CancellationTokenSource(),
                    IsConnected = true
                };
                // per-connection handler and marker
                ctx.Handler = new MavlinkMessageHandler(
                    pos => UpdateDroneMarkerForConnection(id, pos),
                    isArmed => { if (_activeConnectionId == id) UpdateArmStatusBox(isArmed); },
                    alt => { if (_activeConnectionId == id) UpdateAltimeterBox(alt); },
                    mode => { if (_activeConnectionId == id) UpdateDroneModeTextBox(mode); },
                    (name, val) => { if (_activeConnectionId == id) UpdateTextLabelGUI(name, val); });

                var initialPos = new PointLatLng(36.415797, 25.427891);
                ctx.Marker = new DroneMarker(initialPos, ctx.Handler);
                markersOverlay.Markers.Add(ctx.Marker);

                _connections[id] = ctx;
                if (string.IsNullOrEmpty(_activeConnectionId))
                    _activeConnectionId = id;

                // update active drones dropdown
                RefreshActiveDroneComboItems();

                isTcpConnection = false;
                isConnected = true;

                // Start handling MAVLink messages
                _ = Task.Run(() => HandleMavlinkMessages(ctx));

                // Optional: send heartbeat and setup streams for serial as well
                _ = Task.Run(async () =>
                {
                    bool ok = await SendMavlinkHeartbeatAsync(ctx);
                    if (ok)
                    {
                        await Task.Delay(1000);
                        RequestParameters(ctx);
                        await Task.Delay(2000);
                        RequestDataStream(ctx);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Serial connect failed: {ex.Message}");
                return false;
            }
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
                isConnected = false;
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



        private async Task<bool> ConnectViaTCP(string ip, int port)
        {
            try
            {
                var client = new TcpClient(ip, port);
                var stream = client.GetStream();

                string id = GenerateConnectionId("tcp", $"{ip}:{port}");
                var ctx = new DroneConnectionContext
                {
                    ConnectionId = id,
                    IsTcp = true,
                    TcpClient = client,
                    Stream = stream,
                    Cts = new CancellationTokenSource(),
                    IsConnected = true,

                    // sensible defaults
                    SysId = 1,
                    CompId = 1,
                    LastRelAltMeters = double.NaN,
                    LastRelAltTimeUtc = DateTime.MinValue,
                    LastHeartbeatUtc = DateTime.MinValue
                };

                // Per-connection handler:
                //   - ONLY position goes to UI
                //   - arm/mode/alt just update context; UI is driven centrally from receiver
                ctx.Handler = new MavlinkMessageHandler(
                     pos => UpdateDroneMarkerForConnection(id, pos),

                     // Arm state: store only (UI will be updated centrally/debounced if you prefer)
                     isArmed => { ctx.IsArmed = isArmed; ctx.LastIsArmedFromHb = isArmed; },

                     // Altitude cache
                     altM => { ctx.LastRelAltMeters = altM; ctx.LastRelAltTimeUtc = DateTime.UtcNow; },

                     // Mode name string (no uint pattern matching)
                     modeName =>
                     {
                         ctx.LastModeName = modeName;
                         // If you still want immediate UI update for the active drone:
                         if (_activeConnectionId == id) UpdateDroneModeTextBox(modeName);
                     },

                     (name, val) =>
                     {
                         if (_activeConnectionId == id) UpdateTextLabelGUI(name, val);
                     });

                // Create marker; position will be updated on first GPS msg
                var initialPos = new PointLatLng(36.415797, 25.427891);
                ctx.Marker = new DroneMarker(initialPos, ctx.Handler);
                markersOverlay.Markers.Add(ctx.Marker);

                _connections[id] = ctx;

                if (string.IsNullOrEmpty(_activeConnectionId))
                    _activeConnectionId = id;

                // Legacy fields (if you still need them)
                tcpStream = stream;
                isTcpConnection = true;
                isConnected = true;

                this.Invoke(new Action(() =>
                {
                    but_connect.Text = "Connected";
                    but_connect.BackColor = Color.Green;
                    but_connect.ForeColor = Color.White;
                    RefreshActiveDroneComboItems();
                }));

                Console.WriteLine($"✅ Connected to SITL at {ip}:{port} (id={id})");

                // Handshake & telemetry
                bool sysIdReceived = await SendMavlinkHeartbeatAsync(ctx);
                if (!sysIdReceived)
                {
                    Console.WriteLine("❌ Connection failed: No SYSID received.");
                    return false;
                }
                await Task.Delay(1000);

                RequestParameters(ctx);
                await Task.Delay(2000);

                RequestDataStream(ctx);
                await Task.Delay(1000);

                _ = Task.Run(() => HandleMavlinkMessages(ctx));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to {ip}:{port} - {ex.Message}");
                return false;
            }
        }

        private async Task<bool> WaitUntilModeIsAsync(string expectedName, int timeoutMs = 3000)
        {
            var ctx = GetActiveConnection(); if (ctx == null) return false;
            var end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < end)
            {
                if (!string.IsNullOrEmpty(ctx.LastModeName) &&
                    string.Equals(ctx.LastModeName, expectedName, StringComparison.OrdinalIgnoreCase))
                    return true;

                await Task.Delay(100);
            }
            return false;
        }

        private async Task<bool> WaitUntilArmedAsync(bool desired, int timeoutMs = 5000)
        {
            var ctx = GetActiveConnection(); if (ctx == null) return false;
            var end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < end)
            {
                if (ctx.IsArmed == desired) return true;
                await Task.Delay(100);
            }
            return false;
        }


        private async Task<bool> SendMavlinkHeartbeatAsync(DroneConnectionContext ctx)
        {
            if (ctx == null || ctx.Stream == null)
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

                ctx.Stream.Write(packet, 0, packet.Length);
                ctx.Stream.Flush();
                Console.WriteLine($"✅ Sent MAVLink heartbeat over TCP to {ctx.ConnectionId}");
                await Task.Delay(500);

                // Now wait for a response for up to 2.2 seconds
                return await WaitForSysIdCompIdAsync(ctx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending heartbeat over TCP: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> WaitForSysIdCompIdAsync(DroneConnectionContext ctx)
        {
            DateTime timeout = DateTime.Now.AddMilliseconds(2200);

            while (DateTime.Now < timeout)
            {
                MAVLink.MAVLinkMessage packet;
                lock (readlock)
                {
                    packet = mavlink.ReadPacket(ctx.Stream);
                    if (packet == null || packet.data == null)
                        continue;
                }

                // Check if the received packet is a heartbeat
                if (packet.data.GetType() == typeof(MAVLink.mavlink_heartbeat_t))
                {
                    var hb = (MAVLink.mavlink_heartbeat_t)packet.data;
                    ctx.SysId = packet.sysid;
                    ctx.CompId = packet.compid;

                    // keep legacy for active
                    sysid = ctx.SysId;
                    compid = ctx.CompId;

                    Console.WriteLine($"✅ Received SYSID={ctx.SysId}, COMPID={ctx.CompId} on {ctx.ConnectionId}");
                    return true;
                }

                await Task.Delay(50); // Small delay to avoid excessive CPU usage
            }

            Console.WriteLine("❌ Timeout: No SYSID/COMPID received.");
            return false;
        }




        private void SendMavlinkHeartbeat()
        {
            var ctx = GetActiveConnection();
            if (ctx == null || ctx.Stream == null)
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
                    ctx.Stream.Write(packet, 0, packet.Length);
                    ctx.Stream.Flush();
                    Console.WriteLine($"✅ Sent MAVLink heartbeat over TCP (Attempt {i + 1}/5) to {ctx.ConnectionId}");
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending heartbeat over TCP: {ex.Message}");
            }
        }




        private void RequestDataStream(DroneConnectionContext ctx)
        {
            if (ctx == null || ctx.SysId == 0 || ctx.CompId == 0)
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
                target_system = ctx.SysId, target_component = ctx.CompId, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.EXTENDED_STATUS, req_message_rate = 2, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = ctx.SysId, target_component = ctx.CompId, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.POSITION, req_message_rate = 2, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = ctx.SysId, target_component = ctx.CompId, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.EXTRA1, req_message_rate = 4, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = ctx.SysId, target_component = ctx.CompId, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.EXTRA2, req_message_rate = 4, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = ctx.SysId, target_component = ctx.CompId, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.EXTRA3, req_message_rate = 2, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = ctx.SysId, target_component = ctx.CompId, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.RAW_SENSORS, req_message_rate = 2, start_stop = 1
            }),
            mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, new MAVLink.mavlink_request_data_stream_t()
            {
                target_system = ctx.SysId, target_component = ctx.CompId, req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.RC_CHANNELS, req_message_rate = 2, start_stop = 1
            })
                };

                foreach (var packet in requests)
                {
                    ctx.Stream.Write(packet, 0, packet.Length);
                    ctx.Stream.Flush();
                    Thread.Sleep(500);
                }

                Console.WriteLine($"📡 Requested multiple data streams from {ctx.ConnectionId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting data streams: {ex.Message}");
            }
        }

        // Backward-compatible wrappers
        private void RequestDataStream()
        {
            var ctx = GetActiveConnection();
            if (ctx != null)
                RequestDataStream(ctx);
        }

        private void RequestAutopilotCapabilities()
        {
            var ctx = GetActiveConnection();
            if (ctx == null || ctx.SysId == 0 || ctx.CompId == 0)
            {
                Console.WriteLine("❌ System ID or Component ID not received yet. Cannot request capabilities.");
                return;
            }

            try
            {
                MAVLink.mavlink_command_long_t cmd = new MAVLink.mavlink_command_long_t()
                {
                    target_system = ctx.SysId,
                    target_component = ctx.CompId,
                    command = (ushort)MAVLink.MAV_CMD.REQUEST_AUTOPILOT_CAPABILITIES
                };

                byte[] packet = mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd);

                ctx.Stream.Write(packet, 0, packet.Length);
                ctx.Stream.Flush();

                Console.WriteLine("📡 Requested Autopilot Capabilities.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting autopilot capabilities: {ex.Message}");
            }
        }

        private void RequestParameters(DroneConnectionContext ctx)
        {
            if (ctx == null || ctx.SysId == 0 || ctx.CompId == 0)
            {
                Console.WriteLine("❌ System ID or Component ID not received yet. Cannot request parameters.");
                return;
            }

            try
            {
                MAVLink.mavlink_param_request_list_t paramRequest = new MAVLink.mavlink_param_request_list_t()
                {
                    target_system = ctx.SysId,
                    target_component = ctx.CompId
                };

                byte[] packet = mavlink.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.PARAM_REQUEST_LIST, paramRequest);

                ctx.Stream.Write(packet, 0, packet.Length);
                ctx.Stream.Flush();

                Console.WriteLine("🔄 Requested SITL parameters.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting parameters: {ex.Message}");
            }
        }

        private void RequestParameters()
        {
            var ctx = GetActiveConnection();
            if (ctx != null)
                RequestParameters(ctx);
        }

        private void HandleMavlinkMessages(DroneConnectionContext ctx)
        {
            if (ctx == null || ctx.Stream == null) return;

            var parser = new MAVLink.MavlinkParse();

            while (ctx.IsConnected && ctx.Stream.CanRead && (ctx.Cts == null || !ctx.Cts.IsCancellationRequested))
            {
                try
                {
                    MAVLink.MAVLinkMessage message = parser.ReadPacket(ctx.Stream);
                    if (message == null || message.data == null) continue;

                    // --- SENSOR STREAM (sysid 10) stays global
                    if (message.sysid == 10)
                    {
                        HandleSensorMessage(message);
                        continue;
                    }

                    // --- Cache autopilot sys/comp on first HEARTBEAT only
                    if (message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.HEARTBEAT)
                    {
                        var hb = (MAVLink.mavlink_heartbeat_t)message.data;

                        // Ignore GCS heartbeats (sysid=255 or MAV_TYPE_GCS), but DO NOT return from the method
                        bool isGcs = message.sysid == 255 ||
                                     hb.type == (byte)MAVLink.MAV_TYPE.GCS;
                        if (isGcs)
                        {
                            // Optional: Console.WriteLine("[HB] (GCS) ignored");
                            continue; // <-- skip this message, keep the loop running
                        }

                        // Bind IDs ONCE when unknown (make sure ctx.SysId defaults to 0 at creation)
                        if (ctx.SysId == 0)
                        {
                            ctx.SysId = message.sysid;
                            ctx.CompId = message.compid;
                            Console.WriteLine($"[HB] bind ctx {ctx.ConnectionId} -> SYSID={ctx.SysId} COMPID={ctx.CompId}");
                        }

                        // Only process arm state from the bound autopilot
                        if (message.sysid == ctx.SysId)
                        {
                            bool isArmed = (hb.base_mode & (byte)MAVLink.MAV_MODE_FLAG.SAFETY_ARMED) != 0;
                            ctx.IsArmed = isArmed;
                            ctx.LastHeartbeatUtc = DateTime.UtcNow;

                            if (ctx.LastIsArmedFromHb != isArmed)
                            {
                                ctx.LastIsArmedFromHb = isArmed;
                                if (_activeConnectionId == ctx.ConnectionId)
                                    UpdateArmStatusBox(isArmed);
                            }

                            //Console.WriteLine($"[HB] sys={message.sysid} armed={isArmed} base_mode={hb.base_mode} custom_mode={hb.custom_mode}");
                        }

                        // Done with HEARTBEAT; move to next message
                        continue;
                    }


                    // --- LOG position-ish messages to prove they're flowing
                    if (message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT ||
                        message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.GPS_RAW_INT)
                    {
                        try
                        {
                            if (message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT)
                            {
                                var gp = (MAVLink.mavlink_global_position_int_t)message.data;
                      
                                ctx.LastRelAltMeters = gp.relative_alt / 1000.0; // mm->m
                                ctx.LastRelAltTimeUtc = DateTime.UtcNow;
                                double lat = gp.lat / 1e7;
                                double lon = gp.lon / 1e7;
                                double alt = gp.relative_alt / 1000.0;
                                double relAltM = gp.relative_alt / 1000.0; // mm -> m
                  
                                //Console.WriteLine($"[RX] GLOBAL_POSITION_INT sys={message.sysid} lat={lat:F6} lon={lon:F6} relAlt={alt:F1}m");
                            }
                            else
                            {
                                var gps = (MAVLink.mavlink_gps_raw_int_t)message.data;
                                double lat = gps.lat / 1e7;
                                double lon = gps.lon / 1e7;
                                //Console.WriteLine($"[RX] GPS_RAW_INT sys={message.sysid} lat={lat:F6} lon={lon:F6} sat={gps.satellites_visible}");
                            }
                        }
                        catch { /* swallow logging errors */ }
                    }

                    // --- Always deliver non-sensor messages to the connection's handler
                    ctx.Handler?.HandleMavlinkMessage(message);

                    // --- ACK/Mission diagnostics (keep these here too)
                    if (message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.COMMAND_ACK)
                    {
                        var ack = (MAVLink.mavlink_command_ack_t)message.data;
                        Console.WriteLine($"RX ACK cmd={ack.command} result={(MAVLink.MAV_RESULT)ack.result}");
                        CompleteAck(ack.command, ack);
                    }
                    else if (message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST_INT)
                    {
                        var req = (MAVLink.mavlink_mission_request_int_t)message.data;
                       // Console.WriteLine($"RX MISSION_REQUEST_INT seq={req.seq}");
                    }
                    else if (message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.MISSION_ACK)
                    {
                        var mack = (MAVLink.mavlink_mission_ack_t)message.data;
                       // Console.WriteLine($"RX MISSION_ACK type={(MAVLink.MAV_MISSION_RESULT)mack.type}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling MAVLink message on {ctx?.ConnectionId}: {ex.Message}");
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



        private void UpdateDroneMarkerForConnection(string connectionId, PointLatLng position)
        {

           // Console.WriteLine($"[UI] Update marker for {connectionId} -> {position.Lat:F6},{position.Lng:F6}");

            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, PointLatLng>(UpdateDroneMarkerForConnection), connectionId, position);
            }
            else
            {
                if (string.IsNullOrEmpty(connectionId) || !_connections.TryGetValue(connectionId, out var ctx))
                {
                    return;
                }

                if (ctx.Marker == null)
                {
                    ctx.Marker = new DroneMarker(position, ctx.Handler);
                    markersOverlay.Markers.Add(ctx.Marker);
                }
                else
                {
                    ctx.Marker.Position = position;
                }

                // Keep legacy global field pointing to active marker for backward compatibility
                if (_activeConnectionId == connectionId)
                {
                    droneMarker = ctx.Marker;
                }

                // Optionally, refresh the overlay to ensure the marker is rendered correctly
                markersOverlay.IsVisibile = false;
                markersOverlay.IsVisibile = true;

                // Get the drone's speed
                double droneSpeedMps = GetDroneGroundSpeed();

                // If this is the active drone and speed is greater than 5 m/s, draw projected path
                if (_activeConnectionId == connectionId && droneSpeedMps > 5.0)
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

        // NEW: Show distances from active drone to each quadrant reference (NW, NE, SE, SW)
        public void startVolcanoHeatmap()
        {
            _heat.Clear();
            _ = StartVolcanoHeatmapAsync();
        }

        private void EnsureHeatmapTimerRunning()
        {
            if (_heatTimer == null)
            {
                _heatTimer = new System.Windows.Forms.Timer();
                _heatTimer.Interval = 500; // ~2 Hz. Use 200ms for 5 Hz if you like
                _heatTimer.Tick += HeatTimer_Tick;
            }
            if (!_heatTimer.Enabled)
            {
                _heatEnabled = true;
                _heatTimer.Start();
                Console.WriteLine("[HEATMAP] Timer started.");
            }
        }

        private void StopHeatmapTimer()
        {
            if (_heatTimer != null && _heatTimer.Enabled)
            {
                _heatTimer.Stop();
                _heatEnabled = false;
                Console.WriteLine("[HEATMAP] Timer stopped.");
            }
        }



        private void HeatTimer_Tick(object sender, EventArgs e)
        {
            if (!_heatEnabled || _heat == null) return;

            var pos = GetDroneCurrentPosition();
            if (double.IsNaN(pos.Lat) || double.IsNaN(pos.Lng)) return;

            double value = InstantCO2; // your live sensor ppm

            // Ingest and recolor
            _heat.AddSample(pos.Lat, pos.Lng, value, _gaussianRadiusM);
            _heat.Recolor(minValue: _heatMinPpm, maxValue: _heatMaxPpm, maxAlpha: 150);

            // Force redraw
            gMapControl1.Refresh();

            // Debug
            Console.WriteLine($"[HEATMAP] Tick: lat={pos.Lat:F6}, lon={pos.Lng:F6}, CO2={value:F1}");
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
            if (ArmStatusBox == null || ArmStatusBox.IsDisposed) return;

            // Deduplicate + debounce UI repaints
            var now = DateTime.UtcNow;
            if (_armUiLast.HasValue && _armUiLast.Value == isArmed &&
                (now - _armUiLastChangeUtc) < ArmUiMinDwell)
            {
                return;
            }
            _armUiLast = isArmed;
            _armUiLastChangeUtc = now;

            void Apply()
            {
                // Optional: skip if text already matches (extra dedupe)
                var newText = isArmed ? "Armed" : "Disarmed";
                if (ArmStatusBox.Text == newText) return;

                ArmStatusBox.Text = newText;
                ArmStatusBox.BackColor = isArmed ? Color.Red : Color.Green; // keep your color scheme
            }

            if (ArmStatusBox.InvokeRequired)
                ArmStatusBox.BeginInvoke((Action)Apply);
            else
                Apply();
        }





        private void CMB_comport_Click(object sender, EventArgs e)
        {
            PopulateConnectionList();
        }

        // Helper to get available connections list for reuse by UI dialogs
        public List<string> GetAvailableConnections()
        {
            var serialPorts = SerialPort.GetPortNames().ToList();

            // Add the TCP connection options
            serialPorts.Add("172.23.130.102:5760");
            serialPorts.Add("127.0.0.1:5760");
            serialPorts.Add("127.0.0.1:5762");
            serialPorts.Add("127.0.0.1:5772");

            return serialPorts;
        }

        private void PopulateConnectionList()
        {
            // Get available serial ports and TCP endpoints
            var connections = GetAvailableConnections();

            // Set the DataSource of the ComboBox to the updated list
            CMB_comport.DataSource = connections;

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
                //Console.WriteLine("MISSION_ACK send");
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
                if (droneMarker != null)
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

        // --- ACK registry (keep what you have, add this helper) ---
        private TaskCompletionSource<MAVLink.mavlink_command_ack_t> RegisterAckWaiter(ushort command, int timeoutMs = 2000)
        {
            lock (_ackLock)
            {
                if (_pendingAcks.TryGetValue(command, out var existing))
                    return existing;

                var tcs = new TaskCompletionSource<MAVLink.mavlink_command_ack_t>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingAcks[command] = tcs;

                var cts = new CancellationTokenSource(timeoutMs);
                cts.Token.Register(() =>
                {
                    if (!tcs.Task.IsCompleted)
                        tcs.TrySetException(new TimeoutException($"COMMAND_ACK timeout for cmd {command}"));
                });
                return tcs;
            }
        }






        private void SendPacket(byte[] packet, string note = null)
        {
            try
            {
                var ctx = GetActiveConnection();
                if (ctx != null && ctx.IsTcp && ctx.Stream != null && ctx.Stream.CanWrite)
                {
                    lock (_txLock)
                    {
                        ctx.Stream.Write(packet, 0, packet.Length);
                        ctx.Stream.Flush();
                    }
                    byte msgid = (packet.Length > 6 && packet[0] == 0xFE) ? packet[5] : (byte)0xFF;
                   // Console.WriteLine($"TX [{ctx.ConnectionId}] msgid={msgid} len={packet.Length} {note ?? ""}");
                }
                else if (!isTcpConnection && serialPort1.IsOpen)
                {
                    lock (_txLock)
                    {
                        serialPort1.Write(packet, 0, packet.Length);
                    }
                    byte msgid = (packet.Length > 6 && packet[0] == 0xFE) ? packet[5] : (byte)0xFF;
                   // Console.WriteLine($"TX [Serial] msgid={msgid} len={packet.Length} {note ?? ""}");
                }
                else
                {
                  //  Console.WriteLine("TX FAIL: No valid connection available.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TX ERROR: {ex.Message}");
            }
        }

        private Task<MAVLink.mavlink_command_ack_t> WaitForAckAsync(ushort command, int timeoutMs = 2000)
        {
            lock (_ackLock)
            {
                if (_pendingAcks.TryGetValue(command, out var existing))
                    return existing.Task;

                var tcs = new TaskCompletionSource<MAVLink.mavlink_command_ack_t>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingAcks[command] = tcs;

                // timeout
                var cts = new CancellationTokenSource(timeoutMs);
                cts.Token.Register(() =>
                {
                    if (!tcs.Task.IsCompleted)
                        tcs.TrySetException(new TimeoutException($"COMMAND_ACK timeout for cmd {command}"));
                });
                return tcs.Task;
            }
        }

        private void CompleteAck(ushort command, MAVLink.mavlink_command_ack_t ack)
        {
            lock (_ackLock)
            {
                if (_pendingAcks.TryGetValue(command, out var tcs))
                {
                    _pendingAcks.Remove(command);
                    tcs.TrySetResult(ack);
                }
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

        private const ushort TYPE_MASK_POS_ONLY =
         (1 << 3) | // vx
         (1 << 4) | // vy
         (1 << 5) | // vz
         (1 << 6) | // ax
         (1 << 7) | // ay
         (1 << 8) | // az
         (1 << 9) | // yaw
         (1 << 10);  // yaw_rate

        private byte _targetSystem = 1; // TODO: set from heartbeat
        private byte _targetComp = 1; // TODO: set from heartbeat


        private void SendSetPositionTargetGlobalInt(DroneConnectionContext ctx, double latDeg, double lonDeg, float altRelM)
        {
            var msg = new MAVLink.mavlink_set_position_target_global_int_t
            {
                time_boot_ms = 0,
                target_system = ctx.SysId == 0 ? (byte)1 : ctx.SysId,
                target_component = ctx.CompId == 0 ? (byte)1 : ctx.CompId,
                coordinate_frame = (byte)MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT_INT,
                type_mask = TYPE_MASK_POS_ONLY,
                lat_int = (int)Math.Round(latDeg * 1e7),
                lon_int = (int)Math.Round(lonDeg * 1e7),
                alt = altRelM,
                vx = 0,
                vy = 0,
                vz = 0,
                afx = 0,
                afy = 0,
                afz = 0,
                yaw = 0,
                yaw_rate = 0
            };

            var pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_POSITION_TARGET_GLOBAL_INT, msg);
            SendPacket(pkt, $"SET_POSITION_TARGET_GLOBAL_INT -> {latDeg:F6},{lonDeg:F6}@{altRelM:F1}");
        }

        // Stream it until within 'stopMeters' of the target, or timeout
        public async Task<bool> FlyGuidedToAsyncStreaming(double latDeg, double lonDeg, float altRelM,
                                                          double stopMeters = 3, int hz = 3, int timeoutSec = 60)
        {
            // Ensure GUIDED & armed
            await SendDoSetModeAsync(4);
            await WaitUntilModeIsAsync("GUIDED", 3000);

            if (!GetActiveConnection()?.IsArmed ?? true)
            {
                var armOk = await ArmIfNeededAsyncForActiveCtx(true);
                var armed = await WaitUntilArmedAsync(true, 5000);
                if (!armOk || !armed) return false;
            }

            // If still on ground, do a small takeoff first (e.g., 10 m)
            var relAlt = GetLastRelativeAltitudeMeters();
            if (double.IsNaN(relAlt) || relAlt < 2.0)
            {
                var toOk = await GuidedTakeoffAsync(altRelM: Math.Min(altRelM, 15f));
                if (!toOk) return false;
            }

            int periodMs = (int)Math.Round(1000.0 / Math.Max(1, hz));
            var end = DateTime.UtcNow.AddSeconds(timeoutSec);

            while (DateTime.UtcNow < end)
            {
                SendSetPositionTargetGlobalInt(GetActiveConnection(), latDeg, lonDeg, altRelM);

                var pos = GetDroneCurrentPosition(); // your existing helper (lat/lon)
                var d = GetDistance(pos, new PointLatLng { Lat = latDeg, Lng = lonDeg });
                if (d <= stopMeters) return true;

                await Task.Delay(periodMs);
            }
            return false;
        }

        public void GuidedGoto_SET_POSITION_TARGET_GLOBAL_INT(double latDeg, double lonDeg, float altRelMeters)
        {
            var msg = new MAVLink.mavlink_set_position_target_global_int_t
            {
                time_boot_ms = 0, // autopilot will ignore
                target_system = _targetSystem,
                target_component = _targetComp,
                coordinate_frame = (byte)MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT_INT,
                type_mask = TYPE_MASK_POS_ONLY,
                lat_int = (int)Math.Round(latDeg * 1e7),
                lon_int = (int)Math.Round(lonDeg * 1e7),
                alt = altRelMeters,
                vx = 0,
                vy = 0,
                vz = 0,
                afx = 0,
                afy = 0,
                afz = 0,
                yaw = 0,
                yaw_rate = 0
            };

            var pkt = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_POSITION_TARGET_GLOBAL_INT, msg);
            SendPacket(pkt);
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
            double deltaLon = (distance / 6371000.0) * Math.Sin(headingRad) / Math.Cos(shipPosition.Lat * (Math.PI / 180));

            double futureLat = shipPosition.Lat + (deltaLat * (180 / Math.PI));
            double futureLon = shipPosition.Lng + (deltaLon * (180 / Math.PI));

            return new PointLatLng(futureLat, futureLon);
        }

        private PointLatLng GetDroneCurrentPosition()
        {
            var ctx = GetActiveConnection();
            if (ctx != null && ctx.Handler != null)
            {
                PointLatLng currentPosition = ctx.Handler.DroneCurrentPosition;
                return currentPosition;
            }
            // fallback to legacy
            PointLatLng legacy = mavlinkMessageHandler != null ? mavlinkMessageHandler.DroneCurrentPosition : new PointLatLng(0, 0);
            return legacy;
        }

        private Double GetDroneGroundSpeed()
        {
           var ctx = GetActiveConnection();
           if (ctx != null && ctx.Handler != null)
           {
               Double currentSpeed = ctx.Handler.DroneGroundSpeed;
               return currentSpeed;
           }
           Double legacy = mavlinkMessageHandler != null ? mavlinkMessageHandler.DroneGroundSpeed : 0.0;
           return legacy;
        }

        private Double GetDroneCurrentHeading()
            {
            var ctx = GetActiveConnection();
            if (ctx != null && ctx.Handler != null)
            {
                Double currentHeading = ctx.Handler.DroneHeading;
                return currentHeading;
            }
            Double legacy = mavlinkMessageHandler != null ? mavlinkMessageHandler.DroneHeading : 0.0;
            return legacy;
        }



        private double GetDistance(PointLatLng point1, PointLatLng point2)
        {
            double R = 6371000; // Earth's radius in meters
            double lat1 = point1.Lat * (Math.PI / 180.0);
            double lat2 = point2.Lat * (Math.PI / 180.0);
            double deltaLat = (point2.Lat - point1.Lat) * (Math.PI / 180.0);
            double deltaLon = (point2.Lng - point1.Lng) * (Math.PI / 180.0);

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

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
            double dLon = DegreesToRadians(point.Lng - reference.Lng);

            double x = EarthRadius * dLon * Math.Cos(DegreesToRadians(reference.Lat)); // East
            double y = EarthRadius * dLat; // North

            return (x, y);
        }

        private static PointLatLng ENUToLatLng(double x, double y, PointLatLng reference)
        {
            double dLat = y / EarthRadius;
            double dLon = x / (EarthRadius * Math.Cos(DegreesToRadians(reference.Lat)));

            double lat = reference.Lat + RadiansToDegrees(dLat);
            double lng = reference.Lng + RadiansToDegrees(dLon);

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
            double deltaLon = (offsetX / earthRadius) * (180 / Math.PI) / Math.Cos(origin.Lat * Math.PI / 180);

            // Return the new geographic coordinate
            return new PointLatLng(origin.Lat + deltaLat, origin.Lng + deltaLon);
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
            double lonRad = origin.Lng * Math.PI / 180.0;

            double angularDistance = distance / earthRadius;

            double newLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(angularDistance) +
                                         Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

            double newLonRad = lonRad + Math.Atan2(Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
                                                   Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad));

            double newLat = newLatRad * 180.0 / Math.PI;
            double newLon = newLonRad * 180.0 / Math.PI;

            return new PointLatLng(newLat, newLon);
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
                       Math.Cos(lat1 * Math.PI / 180.0) *
                       Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Pow(Math.Sin(dlon / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double earthRadius = 6371000.0; // meters
            return earthRadius * c;
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            startVolcanoHeatmap();
            //StartShipMeasurementPattern();
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


        static (double dLatDegPerMeter, double dLonDegPerMeter) DegPerMeter(double lat0Deg)
        {
            const double mPerDegLat = 111_320.0;
            double mPerDegLon = 111_320.0 * Math.Cos(lat0Deg * Math.PI / 180.0);
            return (1.0 / mPerDegLat, 1.0 / mPerDegLon);
        }

        // rotate ENU coordinates
        static (double x, double y) Rot(double x, double y, double headingDeg)
        {
            double th = headingDeg * Math.PI / 180.0;
            double c = Math.Cos(th), s = Math.Sin(th);
            return (x * c - y * s, x * s + y * c);
        }

        static PointLatLng ToLatLon(double lat0, double lon0, double x_m, double y_m)
        {
            var (dLat, dLon) = DegPerMeter(lat0);
            return new PointLatLng(lat0 + y_m * dLat, lon0 + x_m * dLon);
        }

        // Plan a lawnmower starting from a given corner of the quadrant
        public static List<PointLatLng> PlanFromCorner(QuadrantPlan p)
        {
            double halfW = p.Width_m / 2.0;
            double halfH = p.Height_m / 2.0;

            // determine quadrant bounds in local ENU relative to center
            double xMin = -halfW;
            double xMax = halfW;
            double yMin = -halfH;
            double yMax = halfH;

            // choose start coordinates based on corner
            double startX = (p.StartCorner == Corner.NE || p.StartCorner == Corner.SE) ? xMax : xMin;
            double startY = (p.StartCorner == Corner.NE || p.StartCorner == Corner.NW) ? yMax : yMin;

            bool horizontal = true; // east–west lanes for 1 km sweeps
            var path = new List<PointLatLng>();

            if (horizontal)
            {
                // stepping direction depends on start corner
                bool stepSouth = (p.StartCorner == Corner.NE || p.StartCorner == Corner.NW);
                bool westFirst = (p.StartCorner == Corner.NE || p.StartCorner == Corner.SE);

                // ✅ Add half-lane offset so we start in the middle of first lane
                double initialYOffset = stepSouth ? -(p.LaneSpacing_m / 2.0) : (p.LaneSpacing_m / 2.0);

                int lanes = (int)Math.Ceiling(p.Height_m / p.LaneSpacing_m);

                for (int i = 0; i < lanes; i++)
                {
                    double y = startY + initialYOffset + (stepSouth ? -i * p.LaneSpacing_m : i * p.LaneSpacing_m);
                    bool goWest = westFirst ^ (i % 2 == 1);

                    double xA = goWest ? xMax : xMin;
                    double xB = goWest ? xMin : xMax;

                    var (xrA, yrA) = Rot(xA, y, p.HeadingDeg);
                    var (xrB, yrB) = Rot(xB, y, p.HeadingDeg);

                    path.Add(ToLatLon(p.CenterLatDeg, p.CenterLonDeg, xrA, yrA));
                    path.Add(ToLatLon(p.CenterLatDeg, p.CenterLonDeg, xrB, yrB));
                }
            }

            return path;
        }






    }

}

