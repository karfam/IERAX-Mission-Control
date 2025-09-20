using System;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static MAVLink; // from NuGet

namespace IERAX_MissionControl
{
    public class MavClient : IDisposable
    {
        private readonly MavlinkParse _parser = new MavlinkParse();
        private SerialPort _serialPort;
        private NetworkStream _tcpStream;
        private CancellationTokenSource _cts;
        private bool _isConnected;
        private bool _isTcp;

        public byte SysId { get; private set; }
        public byte CompId { get; private set; }

        public event Action<MAVLinkMessage> PacketReceived;
        public event Action<string> LogMessage;

        public bool IsConnected => _isConnected;

        // ------------------ TCP Connect ------------------
        public async Task ConnectTcpAsync(string ip, int port)
        {
            Disconnect();
            try
            {
                var client = new TcpClient();
                await client.ConnectAsync(ip, port);
                _tcpStream = client.GetStream();
                _isTcp = true;
                _isConnected = true;

                Log($"✅ Connected to TCP {ip}:{port}");
                StartReadLoop(_tcpStream);
            }
            catch (Exception ex)
            {
                Log($"❌ TCP connect failed: {ex.Message}");
                throw;
            }
        }

        // ------------------ Serial Connect ------------------
        public void ConnectSerial(string portName, int baudRate)
        {
            Disconnect();
            try
            {
                _serialPort = new SerialPort(portName, baudRate)
                {
                    ReadTimeout = 2000
                };
                _serialPort.Open();
                _isTcp = false;
                _isConnected = true;

                Log($"✅ Connected to Serial {portName} @ {baudRate}");
                StartReadLoop(_serialPort.BaseStream);
            }
            catch (Exception ex)
            {
                Log($"❌ Serial connect failed: {ex.Message}");
                throw;
            }
        }

        // ------------------ Send Data ------------------
        public void SendPacket(byte[] data)
        {
            if (!_isConnected) return;
            try
            {
                if (_isTcp && _tcpStream != null)
                    _tcpStream.Write(data, 0, data.Length);
                else if (_serialPort != null && _serialPort.IsOpen)
                    _serialPort.BaseStream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Log($"❌ Send failed: {ex.Message}");
            }
        }

        // ------------------ Disconnect ------------------
        public void Disconnect()
        {
            _cts?.Cancel();
            _isConnected = false;

            try { _tcpStream?.Dispose(); } catch { }
            try { _serialPort?.Close(); } catch { }
        }

        // ------------------ Read Loop ------------------
        private void StartReadLoop(Stream stream)
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ReadLoop(stream, _cts.Token), _cts.Token);
        }

        private void ReadLoop(Stream stream, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // This will block until a complete MAVLink message is read
                    var packet = _parser.ReadPacket(stream);

                    if (packet != null)
                    {
                        SysId = packet.sysid;
                        CompId = packet.compid;
                        PacketReceived?.Invoke(packet);
                    }
                }
            }
            catch (IOException)
            {
                // Normal on disconnect
            }
            catch (Exception ex)
            {
                Log($"❌ Read loop error: {ex.Message}");
            }
        }


        private void Log(string msg) => LogMessage?.Invoke(msg);

        public void Dispose() => Disconnect();
    }
}
