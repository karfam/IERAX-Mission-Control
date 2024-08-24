using GMap.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IERAX_MissionControl
{
    public class MavlinkMessageHandler
    {
        private readonly Action<PointLatLng> updateDroneMarkerAction;
        private readonly Action<bool> updateArmStatusBoxAction;
        private readonly Action<double> updateAltimeterBoxAction;
        private readonly Action<string> updateDroneModeTextBox;
        private readonly Action<string, string> updateTextLabelGUI;

        public PointLatLng DroneCurrentPosition { get; set; }
        public float DroneGroundSpeed { get; set; }
        public double DroneHeading { get; set; }


        public MavlinkMessageHandler(Action<PointLatLng> updateDroneMarkerAction, Action<bool> updateArmStatusBoxAction,
                                 Action<double> updateAltimeterBoxAction, Action<string> updateDroneModeTextBox,
                                 Action<string, string> updateTextLabelGUI)
        {
            this.updateDroneMarkerAction = updateDroneMarkerAction;
            this.updateArmStatusBoxAction = updateArmStatusBoxAction;
            this.updateAltimeterBoxAction = updateAltimeterBoxAction;
            this.updateDroneModeTextBox = updateDroneModeTextBox;
            this.updateTextLabelGUI = updateTextLabelGUI;

        }



        public void HandleMavlinkMessage(MAVLink.MAVLinkMessage message)
        {
            switch (message.msgid)
            {
                case (byte)MAVLink.MAVLINK_MSG_ID.HEARTBEAT:
                    HandleHeartbeatMessage(message);
                    break;

                case (byte)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT:
                    HandleGlobalPositionMessage(message);
                    break;

                case (byte)MAVLink.MAVLINK_MSG_ID.COMMAND_ACK:
                    HandleCommandAckMessage(message);
                    break;
                case (byte)MAVLink.MAVLINK_MSG_ID.NAV_CONTROLLER_OUTPUT:
                    HandleNavControllerOutput(message);   
                    break;
                case (byte)MAVLink.MAVLINK_MSG_ID.VFR_HUD:
                    HandleVFR_HUDOutput(message);
                    break;
                case (byte)MAVLink.MAVLINK_MSG_ID.SYS_STATUS:
                   HandleSysStatusOutput(message);
                    break;
                case (byte)MAVLink.MAVLINK_MSG_ID.WIND:
                    HandleWindMessageOutput(message);
                    break;


                default:
                    //Console.WriteLine($"Unhandled MAVLink message: {message.msgtypename}");
                    break;
            }
        }


        private void HandleNavControllerOutput(MAVLink.MAVLinkMessage message)
        {
            var navControllerOutput = (MAVLink.mavlink_nav_controller_output_t)message.data;
            // Distance to the waypoint (in meters)
            double distanceToWP = navControllerOutput.wp_dist;

            // Assuming you have a known drone speed (convert to meters per second)
            double droneSpeedMps = DroneGroundSpeed;

            // Time to the waypoint (in seconds)
            double timeToWP = distanceToWP / droneSpeedMps;
            string distanceToWPString = distanceToWP.ToString("F2") + " meters";
            updateTextLabelGUI("DistanceToWPLabel", distanceToWPString);
            updateTextLabelGUI("TimeToWPLabel", timeToWP.ToString("F2"));
    

            // Optionally, print to the console for debugging
            //Console.WriteLine($"Distance to WP: {distanceToWP:F2} meters, Time to WP: {timeToWP:F2} seconds");
        }

        private void HandleVFR_HUDOutput(MAVLink.MAVLinkMessage message)
        {
            var vfrHud = (MAVLink.mavlink_vfr_hud_t)message.data;
            
            string Airspeed = vfrHud.airspeed.ToString("F2")+ "m/s";   // Airspeed in m/s
            string Groundspeed = vfrHud.groundspeed.ToString("F2") + "m/s"; // Groundspeed in m/s
            string Altitude = vfrHud.alt.ToString("F2");      // Altitude in meters
            string ClimbRate = vfrHud.climb.ToString("F2");     // Climb rate in m/s
            string Heading = vfrHud.heading.ToString();        // Heading in degrees
            string Throttle = vfrHud.throttle.ToString();      // Throttle percentage
                                                               // Update the global variable
            DroneGroundSpeed = vfrHud.groundspeed;
            DroneHeading = vfrHud.heading;

    

            updateTextLabelGUI("GSpeedLabel", string.IsNullOrEmpty(Groundspeed) ? "0.0" : Groundspeed);
            updateTextLabelGUI("ASpeedLabel", string.IsNullOrEmpty(Airspeed) ? "0.0" : Airspeed);
            updateTextLabelGUI("HeadingLabel", string.IsNullOrEmpty(Heading) ? "0" : Heading);
            /*updateTextLabelGUI("AltitudeLabel", string.IsNullOrEmpty(Altitude) ? "0.0" : Altitude);
            updateTextLabelGUI("ClimbLabel", string.IsNullOrEmpty(ClimbRate) ? "0.0" : ClimbRate);
            updateTextLabelGUI("HeadingLabel", string.IsNullOrEmpty(Heading) ? "0" : Heading);
            updateTextLabelGUI("ThrottleLabel", string.IsNullOrEmpty(Throttle) ? "0" : Throttle);*/

        }

        private void HandleSysStatusOutput(MAVLink.MAVLinkMessage message)
        {
            var sysStatus = (MAVLink.mavlink_sys_status_t)message.data;

            // Extract and format relevant fields
            string BatteryVoltage = (sysStatus.voltage_battery / 1000.0).ToString("F2") + "V";  // Battery voltage in volts
            string BatteryCurrent = (sysStatus.current_battery / 100.0).ToString("F2") + "A";   // Battery current in amperes
            string BatteryRemaining = sysStatus.battery_remaining.ToString() + "%";             // Battery remaining in percentage
            string DropRateComm = (sysStatus.drop_rate_comm / 100.0).ToString("F2") + "%";      // Communication drop rate
            string ErrorsComm = sysStatus.errors_comm.ToString();                               // Communication errors
            string Load = (sysStatus.load / 10.0).ToString("F1") + "%";                         // CPU load in percentage

        

            // Update GUI labels
            //updateTextLabelGUI("BatteryVoltageLabel", string.IsNullOrEmpty(BatteryVoltage) ? "0.0V" : BatteryVoltage);
            //updateTextLabelGUI("BatteryCurrentLabel", string.IsNullOrEmpty(BatteryCurrent) ? "0.0A" : BatteryCurrent);
            updateTextLabelGUI("BatteryRemainingLabel", string.IsNullOrEmpty(BatteryRemaining) ? "0%" : BatteryRemaining);
           // updateTextLabelGUI("DropRateCommLabel", string.IsNullOrEmpty(DropRateComm) ? "0.0%" : DropRateComm);
           // updateTextLabelGUI("ErrorsCommLabel", string.IsNullOrEmpty(ErrorsComm) ? "0" : ErrorsComm);
           // updateTextLabelGUI("LoadLabel", string.IsNullOrEmpty(Load) ? "0.0%" : Load);

        }

        private void HandleWindMessageOutput(MAVLink.MAVLinkMessage message)
        {
            
                var wind = (MAVLink.mavlink_wind_t)message.data;

                // Extract and format relevant fields
                string WindSpeed = wind.speed.ToString("F2") + "m/s";       // Wind speed in m/s
                string WindDirection = wind.direction.ToString("F1") + "°"; // Wind direction in degrees
                string WindSpeedZ = wind.speed_z.ToString("F2") + "m/s";    // Vertical wind speed in m/s

                // Update global variables if necessary
                //DroneWindSpeed = wind.speed;
                //DroneWindDirection = wind.direction;

                // Update GUI labels
                updateTextLabelGUI("WindSpeedLabel", string.IsNullOrEmpty(WindSpeed) ? "0.0m/s" : WindSpeed);
                updateTextLabelGUI("WindDirectionLabel", string.IsNullOrEmpty(WindDirection) ? "0.0°" : WindDirection);
                //updateTextLabelGUI("WindSpeedZLabel", string.IsNullOrEmpty(WindSpeedZ) ? "0.0m/s" : WindSpeedZ);
          

        }





        private void HandleCommandAckMessage(MAVLink.MAVLinkMessage message)
        {
            var ack = (MAVLink.mavlink_command_ack_t)message.data;
            if (ack.result == (byte)MAVLink.MAV_RESULT.ACCEPTED)
            {
                Console.WriteLine("Command accepted");
            }
            else
            {
                Console.WriteLine($"Command rejected with result: {ack.result}");
            }
        }


        private void HandleHeartbeatMessage(MAVLink.MAVLinkMessage message)
        {
            var heartbeat = (MAVLink.mavlink_heartbeat_t)message.data;
            string flightMode = DecodeFlightMode(heartbeat);

            bool isArmed = (heartbeat.base_mode & (byte)MAVLink.MAV_MODE_FLAG.SAFETY_ARMED) != 0;
            //Console.WriteLine(isArmed ? "Drone is armed." : "Drone is disarmed.");
            // Update the arm status box using the provided delegate
            updateArmStatusBoxAction(isArmed);
            updateDroneModeTextBox(flightMode);

           //Console.WriteLine($"HEARTBEAT: type={heartbeat.type}, autopilot={heartbeat.autopilot}, base_mode={heartbeat.base_mode}, system_status={heartbeat.system_status}, mavlink_version={heartbeat.mavlink_version}");
            // Console.WriteLine(isArmed ? "Drone is armed." : "Drone is disarmed.");
        }

        private void HandleGlobalPositionMessage(MAVLink.MAVLinkMessage message)
        {
            var positionMessage = (MAVLink.mavlink_global_position_int_t)message.data;

            double latitude = positionMessage.lat / 1e7;
            double longitude = positionMessage.lon / 1e7;
            double altitude = positionMessage.relative_alt / 1000.0; // in meters

            PointLatLng position = new PointLatLng(latitude, longitude);
            // Update the global variable
            DroneCurrentPosition = position;

            // Update the drone marker using the provided delegate
            updateDroneMarkerAction(position);

            // Update the AltimeterBox with the altitude
            updateAltimeterBoxAction(altitude);

            //Console.WriteLine($"GLOBAL_POSITION_INT: Lat={latitude}, Lng={longitude}, Alt={altitude}");
        }

    

        private string DecodeFlightMode(MAVLink.mavlink_heartbeat_t heartbeat)
        {
            // Decode the flight mode based on the autopilot type
            // This is an example, adapt it to your specific drone's flight mode encoding
            switch (heartbeat.custom_mode)
            {
                case 0:
                    return "Stabilize";  // Manual mode, good for beginners
                case 1:
                    return "Acro";       // Acrobatic mode, manual flying with no stabilization
                case 2:
                    return "AltHold";    // Altitude Hold mode
                case 3:
                    return "Auto";       // Autonomous flight mode
                case 4:
                    return "Guided";     // Guided mode, controlled by a ground station
                case 5:
                    return "Loiter";     // Position hold mode
                case 6:
                    return "RTL";        // Return to Launch mode
                case 7:
                    return "Circle";     // Circle around a point
                case 8:
                    return "Land";       // Land at the current position
                case 9:
                    return "Drift";      // Drift mode, for smooth turns, good for filming
                case 10:
                    return "Sport";      // Sport mode, fast and aggressive flying
                case 11:
                    return "Flip";       // Perform a flip in the air
                case 13:
                    return "Brake";      // Stop in mid-air
                case 14:
                    return "Throw";      // Launch the drone by throwing it into the air
                case 15:
                    return "Avoid ADSB"; // Avoid other aircraft detected via ADS-B
                case 16:
                    return "Guided No GPS"; // Guided mode without GPS
                case 17:
                    return "Smart RTL";  // Smart Return to Launch
                case 18:
                    return "FlowHold";   // Optical flow hold
                case 19:
                    return "Follow";     // Follow mode
                case 20:
                    return "ZigZag";     // Zigzag mode for mapping
                case 21:
                    return "System ID";  // System identification mode
                                         // Add other modes as needed based on your specific drone
                default:
                    return "Unknown";    // Unknown mode, should investigate
            }
        }



    }

}
