using GMap.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IERAX_MissionControl
{
    public class MavlinkMessageHandler
    {
        private readonly Action<PointLatLng> updateDroneMarkerAction;
        private readonly Action<bool> updateArmStatusBoxAction;
        private readonly Action<double> updateAltimeterBoxAction;
        private readonly Action<string> UpdateDroneModeTextBox;

        public MavlinkMessageHandler(Action<PointLatLng> updateDroneMarkerAction, Action<bool> updateArmStatusBoxAction, Action<double> updateAltimeterBoxAction, Action<string> updateDroneModeTextBox)
        {
            this.updateDroneMarkerAction = updateDroneMarkerAction;
            this.updateArmStatusBoxAction = updateArmStatusBoxAction;
            this.updateAltimeterBoxAction = updateAltimeterBoxAction;
            this.UpdateDroneModeTextBox = updateDroneModeTextBox;
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

                default:
                    //Console.WriteLine($"Unhandled MAVLink message: {message.msgtypename}");
                    break;
            }
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
            UpdateDroneModeTextBox(flightMode);

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
