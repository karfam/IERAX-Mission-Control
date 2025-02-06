using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IERAX_MissionControl
{
    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public partial class AnalyzerForm : Form
    {
        private TextBox txtTimestamp, txtTemperature, txtNO2, txtCO2, txtBattery, txtPressure, txtSO2, txtRH;
        private Timer updateTimer;
        private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "https://purcon-ws.i-comesure.com/api/sensors/";
        private const string AuthToken = "dd8a62fbdf6db810d15dd4468c7ea86d64d8744c";
        private const string SensorName = "ECT2-X-WB4-01430"; // Target sensor
        private int sensorId = -1; // Will be set dynamically
        public static String InstantAnalyzerCO2 { get; set; }

        public AnalyzerForm()
        {
            InitializeComponent();
            InitializeUI();
            FetchSensorId(); // First, find the sensor ID
            StartUpdateTimer();
        }

        private void InitializeUI()
        {
            this.Text = "Analyzer Form";
            this.Width = 600;
            this.Height = 400;

            // Add Labels and TextBoxes
            AddField("Timestamp:", out txtTimestamp, 20);
            AddField("Temperature (°C):", out txtTemperature, 60);
            AddField("NO₂ (µg/m³):", out txtNO2, 100);
            AddField("CO₂ (ppm):", out txtCO2, 140);
            AddField("Battery (%):", out txtBattery, 180);
            AddField("Pressure (hPa):", out txtPressure, 220);
            AddField("SO₂ (µg/m³):", out txtSO2, 260);
            AddField("Relative Humidity (%):", out txtRH, 300);
        }

        private void AddField(string labelText, out TextBox textBox, int top)
        {
            Label label = new Label { Text = labelText, Left = 20, Top = top, Width = 150 };
            textBox = new TextBox { Left = 180, Top = top, Width = 200, ReadOnly = true };
            this.Controls.Add(label);
            this.Controls.Add(textBox);
        }

        private void StartUpdateTimer()
        {
            updateTimer = new Timer();
            updateTimer.Interval = 5000; // Update every 5 seconds
            updateTimer.Tick += async (s, e) => await FetchMeasurements();
            updateTimer.Start();
        }

        private async Task FetchSensorId()
        {
            try
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", AuthToken);
                HttpResponseMessage response = await client.GetAsync(BaseUrl);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement.GetProperty("results");

                foreach (var sensor in root.EnumerateArray())
                {
                    if (sensor.GetProperty("serial_number").GetString() == SensorName)
                    {
                        sensorId = sensor.GetProperty("id").GetInt32();
                        break;
                    }
                }

                if (sensorId != -1)
                {
                    await FetchMeasurements(); // Fetch data after getting ID
                }
                else
                {
                    MessageBox.Show("Sensor not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch sensor ID: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task FetchMeasurements()
        {
            if (sensorId == -1)
            {
                Console.WriteLine("Sensor ID is not set. Exiting FetchMeasurements.");
                return;
            }

            try
            {
                string measurementsUrl = $"{BaseUrl}{sensorId}/measurements/?format=json&limit=1";
                Console.WriteLine($"[DEBUG] Fetching data from URL: {measurementsUrl}");

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", AuthToken);
                HttpResponseMessage response = await client.GetAsync(measurementsUrl);

                string responseBody = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement;

                if (root.GetProperty("results").GetArrayLength() > 0)
                {
                    var latestMeasurement = root.GetProperty("results")[0];
                    string timestamp = latestMeasurement.GetProperty("date").GetString();
                    var measurements = latestMeasurement.GetProperty("measurements");

                    txtTimestamp.Text = timestamp;

                    foreach (var measurement in measurements.EnumerateArray())
                    {
                        string type = measurement.GetProperty("type").GetString();
                        string value = measurement.GetProperty("value").ToString();

                        Console.WriteLine($"[DEBUG] Measurement Type: {type}, Value: {value}");

                        // Update TextBoxes based on measurement type
                        switch (type)
                        {
                            case "Temperature":
                                txtTemperature.Text = value;
                                break;
                            case "NO2":
                                txtNO2.Text = value;
                                break;
                            case "CO2":
                                txtCO2.Text = value;
                                InstantAnalyzerCO2 = value;
                                break;
                            case "Battery":
                                txtBattery.Text = value;
                                break;
                            case "Pressure":
                                txtPressure.Text = value;
                                break;
                            case "SO2":
                                txtSO2.Text = value;
                                break;
                            case "RH": // Relative Humidity
                                txtRH.Text = value;
                                break;
                        }
                    }
                    Console.WriteLine("[DEBUG] UI Updated with new values.");
                }
                else
                {
                    Console.WriteLine("[DEBUG] No results found in the response.");
                    txtTimestamp.Text = "No data";
                    txtTemperature.Text = "No data";
                    txtNO2.Text = "No data";
                    txtCO2.Text = "No data";
                    txtBattery.Text = "No data";
                    txtPressure.Text = "No data";
                    txtSO2.Text = "No data";
                    txtRH.Text = "No data";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception occurred: {ex.Message}");
                txtTimestamp.Text = "Error";
                txtTemperature.Text = "Error";
                txtNO2.Text = "Error";
                txtCO2.Text = "Error";
                txtBattery.Text = "Error";
                txtPressure.Text = "Error";
                txtSO2.Text = "Error";
                txtRH.Text = "Error";
                MessageBox.Show($"Failed to fetch sensor data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }



}
