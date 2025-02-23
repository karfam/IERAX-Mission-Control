using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IERAX_MissionControl
{
    public partial class AnalyzerForm : Form
    {
        // UI Controls
        private TextBox txtTimestamp, txtTemperature, txtNO2, txtCO2, txtBattery, txtPressure, txtSO2, txtRH;
        private TextBox txtFSC, txtSO2NO2Ratio;
        private Timer updateTimer;
        private Label lblStatus;

        private string previousLastUpdate = "";

        // HTTP client & API settings
        private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "https://purcon-ws.i-comesure.com/api/sensors/";
        private const string AuthToken = "dd8a62fbdf6db810d15dd4468c7ea86d64d8744c";
        private const string SensorName = "ECT2-X-WB4-01430"; // Target sensor
        private int sensorId = -1; // Will be set dynamically

        // Coefficients for equations (SO2, CO2, NO2, NO)
        private float aSO2 = 2.0f, aNO2 = 1.0f, aCO2 = 3.0f, aT = 2.0f, aRH = 1.0f, aP = 1.0f, aNO = 2.0f, eSO2 = 4.0f;
        private float bSO2 = 2.0f, bNO2 = 1.0f, bCO2 = 3.0f, bT = 2.0f, bRH = 1.0f, bP = 1.0f, bNO = 2.0f, eCO2 = 4.0f;
        private float cSO2 = 2.0f, cNO2 = 1.0f, cCO2 = 3.0f, cT = 2.0f, cRH = 1.0f, cP = 1.0f, cNO = 2.0f, eNO2 = 4.0f;
        private float dSO2 = 2.0f, dNO2 = 1.0f, dCO2 = 3.0f, dT = 2.0f, dRH = 1.0f, dP = 1.0f, dNO = 2.0f, eNO = 4.0f;

        public static string InstantAnalyzerCO2 { get; set; }

        public AnalyzerForm()
        {
            InitializeComponent();
            InitializeUI();
            FetchSensorId(); // Get sensor ID, then fetch measurements
            StartUpdateTimer();
        }

        private void InitializeUI()
        {
            this.Text = "Analyzer Form";
            this.Width = 600;
            this.Height = 600;

            // Create status label
            lblStatus = new Label
            {
                Left = 20,
                Top = 10,
                Width = 500,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
            };
            this.Controls.Add(lblStatus);

            // Add fields (label + read-only textbox)
            int top = 40;
            AddField("Timestamp:", out txtTimestamp, top += 40);
            AddField("Temperature:", out txtTemperature, top += 40);
            AddField("NO₂:", out txtNO2, top += 40);
            AddField("CO₂:", out txtCO2, top += 40);
            AddField("SO₂:", out txtSO2, top += 40);
            AddField("Pressure (hPa):", out txtPressure, top += 40);
            AddField("RH (%):", out txtRH, top += 40);
            AddField("Battery (%):", out txtBattery, top += 40);
            AddField("FSC:", out txtFSC, top += 40);
            AddField("SO₂/NO₂ Ratio:", out txtSO2NO2Ratio, top += 40);
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
            updateTimer.Interval = 5000; // Poll every 5 seconds (adjust as needed)
            updateTimer.Tick += async (s, e) => await PollMeasurementsIfUpdated();
            updateTimer.Start();
        }

        private async Task FetchSensorId()
        {
            try
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Token", AuthToken);
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
               
                    // Then start fetching measurements (or start your polling timer)
                    await FetchMeasurements();
                }
                else
                {
                    MessageBox.Show("Sensor not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch sensor ID: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        #region Computation Methods

        public double ComputeSO2(double C_SO2_inst, double C_CO2_inst, double C_NO2_inst, double C_NO_inst)
        {
            return aSO2 * C_SO2_inst +
                   aCO2 * C_CO2_inst +
                   aNO2 * C_NO2_inst +
                   aNO * C_NO_inst +
                   aT + aP + aRH +
                   eSO2;
        }

        public double ComputeCO2(double C_SO2_inst, double C_CO2_inst, double C_NO2_inst, double C_NO_inst)
        {
            return bSO2 * C_SO2_inst +
                   bCO2 * C_CO2_inst +
                   bNO2 * C_NO2_inst +
                   bNO * C_NO_inst +
                   bT + bP + bRH +
                   eCO2;
        }

        public double ComputeNO2(double C_SO2_inst, double C_CO2_inst, double C_NO2_inst, double C_NO_inst)
        {
            return cSO2 * C_SO2_inst +
                   cCO2 * C_CO2_inst +
                   cNO2 * C_NO2_inst +
                   cNO * C_NO_inst +
                   cT + cP + cRH +
                   eNO2;
        }

        public double ComputeNO(double C_SO2_inst, double C_CO2_inst, double C_NO2_inst, double C_NO_inst)
        {
            return dSO2 * C_SO2_inst +
                   dCO2 * C_CO2_inst +
                   dNO2 * C_NO2_inst +
                   dNO * C_NO_inst +
                   dT + dP + dRH +
                   eNO;
        }

        public double ComputeFSC(double C_SO2, double C_SO2_bkg, double C_CO2, double C_CO2_bkg)
        {
            double denominator = C_CO2 - C_CO2_bkg;
            if (Math.Abs(denominator) < 1e-6)
                throw new DivideByZeroException("C_CO2 - C_CO2_bkg is too close to zero.");
            return ((C_SO2 - C_SO2_bkg) / denominator) * 0.232;
        }

        public double ComputeSO2NO2Ratio(double C_SO2, double C_NO2)
        {
            if (Math.Abs(C_NO2) < 1e-6)
                throw new DivideByZeroException("C_NO2 is too close to zero.");
            return C_SO2 / C_NO2;
        }

        #endregion

        private async Task FetchMeasurements()
        {
            if (sensorId == -1)
            {
                Console.WriteLine("Sensor ID is not set. Exiting FetchMeasurements.");
                return;
            }

            try
            {
                // First: Fetch sensor info to get the last_update value
                string sensorInfoUrl = $"{BaseUrl}{sensorId}/?format=json";
                Console.WriteLine($"[DEBUG] Fetching sensor info from URL: {sensorInfoUrl}");

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Token", AuthToken);
                HttpResponseMessage sensorResponse = await client.GetAsync(sensorInfoUrl);
                sensorResponse.EnsureSuccessStatusCode();

                string sensorInfoBody = await sensorResponse.Content.ReadAsStringAsync();
                var sensorJson = JsonDocument.Parse(sensorInfoBody);
                string lastUpdate = sensorJson.RootElement.GetProperty("last_update").GetString();
                txtTimestamp.Text = lastUpdate; // Use the last_update value

                // Next: Fetch the latest measurement(s)
                string measurementsUrl = $"{BaseUrl}{sensorId}/measurements/?format=json&limit=1";
                Console.WriteLine($"[DEBUG] Fetching measurements from URL: {measurementsUrl}");

                HttpResponseMessage measResponse = await client.GetAsync(measurementsUrl);
                measResponse.EnsureSuccessStatusCode();

                string measBody = await measResponse.Content.ReadAsStringAsync();
                var measJson = JsonDocument.Parse(measBody);
                var root = measJson.RootElement;

                if (root.GetProperty("results").GetArrayLength() > 0)
                {
                    var latestMeasurement = root.GetProperty("results")[0];
                    // We now use last_update from the sensor info, so we ignore the measurement date
                    var measurements = latestMeasurement.GetProperty("measurements");

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
                            case "RH":
                                txtRH.Text = value;
                                break;
                        }
                    }
                    lblStatus.Text = "Measurements updated.";
                    Console.WriteLine("[DEBUG] UI updated with new values.");
                }
                else
                {
                    Console.WriteLine("[DEBUG] No measurement results found in the response.");
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
                MessageBox.Show($"Failed to fetch sensor data: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task PollMeasurementsIfUpdated()
        {
            if (sensorId == -1)
            {
                Console.WriteLine("Sensor ID is not set. Exiting PollMeasurementsIfUpdated.");
                return;
            }

            try
            {
                // Fetch sensor info first
                string sensorInfoUrl = $"{BaseUrl}{sensorId}/?format=json";
                Console.WriteLine($"[DEBUG] Polling sensor info from URL: {sensorInfoUrl}");

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Token", AuthToken);
                HttpResponseMessage sensorResponse = await client.GetAsync(sensorInfoUrl);
                sensorResponse.EnsureSuccessStatusCode();

                string sensorInfoBody = await sensorResponse.Content.ReadAsStringAsync();
                using (JsonDocument sensorJson = JsonDocument.Parse(sensorInfoBody))
                {
                    string currentLastUpdate = sensorJson.RootElement.GetProperty("last_update").GetString();

                    // If there is no update, simply exit.
                    if (currentLastUpdate == previousLastUpdate)
                    {
                        Console.WriteLine("[DEBUG] No update detected. Skipping measurement fetch.");
                        return;
                    }
                    else
                    {
                        // Update our stored last_update value
                        previousLastUpdate = currentLastUpdate;
                        // Update the UI with the new timestamp.
                        txtTimestamp.Text = currentLastUpdate;
                    }
                }

                // Now fetch measurements since there is an update.
                string measurementsUrl = $"{BaseUrl}{sensorId}/measurements/?format=json&limit=1";
                Console.WriteLine($"[DEBUG] Fetching updated measurements from URL: {measurementsUrl}");

                HttpResponseMessage measResponse = await client.GetAsync(measurementsUrl);
                measResponse.EnsureSuccessStatusCode();

                string measBody = await measResponse.Content.ReadAsStringAsync();
                var measJson = JsonDocument.Parse(measBody);
                var root = measJson.RootElement;

                if (root.GetProperty("results").GetArrayLength() > 0)
                {
                    var latestMeasurement = root.GetProperty("results")[0];
                    var measurements = latestMeasurement.GetProperty("measurements");

                    foreach (var measurement in measurements.EnumerateArray())
                    {
                        string type = measurement.GetProperty("type").GetString();
                        string value = measurement.GetProperty("value").ToString();
                        Console.WriteLine($"[DEBUG] Measurement Type: {type}, Value: {value}");

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
                            case "RH":
                                txtRH.Text = value;
                                break;
                        }
                    }
                    lblStatus.Text = "Measurements updated.";
                    Console.WriteLine("[DEBUG] UI updated with new values.");
                }
                else
                {
                    Console.WriteLine("[DEBUG] No measurement results found in the response.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception occurred while polling: {ex.Message}");
                txtTimestamp.Text = "Error";
                // Optionally update other fields to "Error"
            }
        }

    }

    }
