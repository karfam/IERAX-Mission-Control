using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GMap.NET.Entity.OpenStreetMapGraphHopperGeocodeEntity;

namespace IERAX_MissionControl
{
    public partial class AnalyzerForm : Form
    {
        // UI Controls
        private TextBox txtGNSSTimestamp,txtTimestamp, txtTemperature, txtNO2,txtNO, txtCO2, txtBattery, txtPressure, txtSO2, txtRH;
        private TextBox txtFSC, txtSO2NO2Ratio;
        private Timer updateTimer;
        private Label lblStatus;
        private TableLayoutPanel _mainLayout;

        private string _measurementsUrl;

        private double _so2Bkg, _co2Bkg;

        // where your other fields live:
        private readonly string _logFilePath =
            Path.Combine(Application.StartupPath, "analyzerData\\sensor_log.csv");



        private string previousLastUpdate = "";
        private DateTime _lastLoggedTimestamp = DateTime.MinValue;

        private readonly Dictionary<string, double> _coeffs = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        // Change the readonly field `_measurementBoxes` to be initialized in the constructor instead of being assigned later.
        private readonly Dictionary<string, TextBox> _measurementBoxes;

        // HTTP client & API settings
        private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "https://purcon-ws.i-comesure.com/api/sensors/";
        private const string AuthToken = "dd8a62fbdf6db810d15dd4468c7ea86d64d8744c";
        private const string SensorName = "ECT2-X-WB4-01430"; // Target sensor
        private int sensorId = -1; // Will be set dynamically




        private void calculationDataInputButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new manualDataInputForm())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    // grab the background inputs
                    _so2Bkg = dlg.C_SO2_bkg;
                    _co2Bkg = dlg.C_CO2_bkg;               
                }
            }
        }

        private void AnalyzerForm_Load(object sender, EventArgs e)
        {

        }


        public static string InstantAnalyzerCO2 { get; set; }

        public AnalyzerForm()
        {
            LoadCoefficients("analyzerData\\coefficients.txt");
            InitializeComponent();
            InitLogFile();
            InitializeUI();
            StartUpdateTimer();

            // Initialize the readonly field `_measurementBoxes` here
            _measurementBoxes = new Dictionary<string, TextBox>(StringComparer.OrdinalIgnoreCase)
            {
                ["Temperature"] = txtTemperature,
                ["Pressure"] = txtPressure,
                ["RH"] = txtRH,
                ["Battery"] = txtBattery
            };

            // Wire up Load
            this.Load += async (s, e) =>
            {
                Console.WriteLine("[DEBUG] AnalyzerForm.Load — starting initial fetch");
                await FetchSensorIdAndInitialMeasurements();
                Console.WriteLine("[DEBUG] AnalyzerForm.Load — initial fetch done");
            };
        }


        private void InitLogFile()
        {
            if (!File.Exists(_logFilePath))
            {
                LogStartup();

    
                var header = 
                    "Date," +
                    "SO2_Instant,CO2_Instant,NO2_Instant,NO_Instant," +  // αυτά βγαίνουν από αναλυτή
                    "Temperature,Pressure,RH," +  // αυτά βγαίνουν από αναλυτή;
              "SO2_Cal,CO2_Cal,NO2_Cal,NO_Cal," + // αυτά βγαίνουν από τους τύπους;
              "FSC,SO2_NO2_Ratio," +  // αυτά βγαίνουν από τυπους το μονο σίγουρο.
              "SO2_Background,CO2_Background," +         // αυτά τα εισάγουμε     
              "CO2_pcb,CO2Max_pcb,CO2MaxTime_pcb," +  //αυτά τα δίνει το drone
              "CO2HD_pcb,CO2HDMax_pcb,CO2HDMaxTimem_pcb"; //αυτά τα δίνει το drone

                File.AppendAllText(_logFilePath, header + Environment.NewLine);
            }
        }

        private void LogToCsv(
    DateTime date,
    double so2Instant, double co2Instant, double no2Instant, double noInstant,
    double temp, double pressure, double rh,
    double SO2Calibrated, double CO2Calibrated,double NO2Calibrated,double NOCalibrated,
    double fsc, double ratio,double so2Bkg,double co2Bkg,double co2_DroneAnalyzer,double co2Max_DroneAnalyzer,string co2MaxTimestamp_DroneAnalyzer,
    double co2HD_DroneAnalyzer, double co2HDMax_DroneAnalyzer, string co2HDMaxTimestamp_DroneAnalyzer)
        {
            // format all numbers with InvariantCulture so you always get “.” as decimal separator
            string line = string.Join(",",
                date.ToString("o"),
                so2Instant.ToString("F3", CultureInfo.InvariantCulture),
                co2Instant.ToString("F3", CultureInfo.InvariantCulture),
                no2Instant.ToString("F3", CultureInfo.InvariantCulture),
                noInstant.ToString("F3", CultureInfo.InvariantCulture),
                temp.ToString("F2", CultureInfo.InvariantCulture),
                pressure.ToString("F2", CultureInfo.InvariantCulture),
                rh.ToString("F2", CultureInfo.InvariantCulture),
                SO2Calibrated.ToString("F3", CultureInfo.InvariantCulture),
                CO2Calibrated.ToString("F3", CultureInfo.InvariantCulture),
                NO2Calibrated.ToString("F3", CultureInfo.InvariantCulture),
                NOCalibrated.ToString("F3", CultureInfo.InvariantCulture),
                fsc.ToString("F5", CultureInfo.InvariantCulture),
                ratio.ToString("F3", CultureInfo.InvariantCulture),
                so2Bkg.ToString("F3", CultureInfo.InvariantCulture),
                co2Bkg.ToString("F3", CultureInfo.InvariantCulture),
                co2_DroneAnalyzer.ToString("F3", CultureInfo.InvariantCulture)?? "N/A",
                co2Max_DroneAnalyzer.ToString("F3", CultureInfo.InvariantCulture) ?? "N/A",
                co2MaxTimestamp_DroneAnalyzer ?? "N/A",
                co2HD_DroneAnalyzer.ToString("F3", CultureInfo.InvariantCulture) ?? "N/A",
                co2HDMax_DroneAnalyzer.ToString("F3", CultureInfo.InvariantCulture) ?? "N/A",
                co2HDMaxTimestamp_DroneAnalyzer ?? "N/A"
            );

            // append the line
            File.AppendAllText(_logFilePath, line + Environment.NewLine);
        }




        /// <summary>
        /// Reads key=value lines from a text file into the _coeffs dictionary.
        /// </summary>
        private void LoadCoefficients(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show($"Coefficient file not found: {path}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue; // skip blank or comments
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var valStr = parts[1].Trim();

                if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                {
                    _coeffs[key] = val;
                }
                else
                {
                    MessageBox.Show($"Invalid coefficient '{key}' in file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

        }

        private void LogStartup()
        {
            using (StreamWriter sw = new StreamWriter(_logFilePath, append: true))
            {
                // Log start message with timestamp
                sw.WriteLine($"[LOG START] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
                sw.WriteLine("[DEBUG] Loaded coefficients:");

                // Log each coefficient with dot as decimal separator
                foreach (var kv in _coeffs)
                {
                    string formatted = $"{kv.Key} = {kv.Value.ToString(CultureInfo.InvariantCulture)}";
                    sw.WriteLine($"  {formatted}");
                }

                // Optional: write CSV line of key=value pairs
                string line = string.Join(",", _coeffs.Select(kv => $"{kv.Key}={kv.Value.ToString(CultureInfo.InvariantCulture)}"));
                sw.WriteLine("CSV: " + line);
                sw.WriteLine(); // Blank line
            }
        }

        private void InitializeUI()
        {
            this.Text = "Analyzer Form";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScroll = true;
            this.Padding = new Padding(10);

            // create the main two-column layout
            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                ColumnStyles =
        {
            new ColumnStyle(SizeType.Absolute, 160),    // label col
            new ColumnStyle(SizeType.Percent, 100)      // textbox col
        },
                RowCount = 0,
            };
            this.Controls.Add(_mainLayout);

            // Add sections & fields
            AddSection("Timestamps");
            AddField("GNSS Timestamp:", out txtGNSSTimestamp);
            AddField("Analyzer Timestamp:", out txtTimestamp);

            AddSection("Calibrated Readings");
            AddField("Calibrated SO₂:", out txtSO2);
            AddField("Calibrated CO₂:", out txtCO2);
            AddField("Calibrated NO₂:", out txtNO2);
            AddField("Calibrated NO:", out txtNO);
            AddField("Temperature:", out txtTemperature);
            AddField("Pressure (hPa):", out txtPressure);
            AddField("RH (%):", out txtRH);
            AddField("Battery (%):", out txtBattery);

            AddSection("Computed Values");
            AddField("FSC Ratio", out txtFSC);
            AddField("SO₂/NO₂ Ratio:", out txtSO2NO2Ratio);

            // status label at the bottom (spanning both columns)
            lblStatus = new Label
            {
                AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Italic),
                Dock = DockStyle.Fill,
            };
            _mainLayout.RowCount++;
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _mainLayout.Controls.Add(lblStatus, 0, _mainLayout.RowCount - 1);
            _mainLayout.SetColumnSpan(lblStatus, 2);
        }

        private void AddSection(string title)
        {
            var header = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                Padding = new Padding(0, 0, 0, 0)
            };
            _mainLayout.RowCount++;
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _mainLayout.Controls.Add(header, 0, _mainLayout.RowCount - 1);
            _mainLayout.SetColumnSpan(header, 2);
        }

        private void AddField(string labelText, out TextBox textBox)
        {
            // increase row count
            int row = _mainLayout.RowCount++;
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // label
            var lbl = new Label
            {
                Text = labelText,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 4, 0, 4)
            };
            _mainLayout.Controls.Add(lbl, 0, row);

            // textbox
            textBox = new TextBox
            {
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 4, 3, 4)
            };
            _mainLayout.Controls.Add(textBox, 1, row);
        }

        private void StartUpdateTimer()
        {
            updateTimer = new Timer();
            updateTimer.Interval = 5000; // Poll every 5 seconds (adjust as needed)
            updateTimer.Tick += async (s, e) => await PollMeasurementsIfUpdated();
            updateTimer.Start();
        }


        #region Computation Methods

        public double ComputeSO2(double C_SO2_inst, double C_CO2_inst, double C_NO2_inst, double C_NO_inst,
                           double T, double P, double RH)
        {
            return _coeffs["aSO2"] * C_SO2_inst
               + _coeffs["aCO2"] * C_CO2_inst
               + _coeffs["aNO2"] * C_NO2_inst
               + _coeffs["aNO"] * C_NO_inst
               + _coeffs["aT"] * T
               + _coeffs["aP"] * P
               + _coeffs["aRH"] * RH
               + _coeffs["eSO2"];
        }



        public double ComputeCO2(double C_SO2_inst, double C_CO2_inst, double C_NO2_inst, double C_NO_inst, double T, double P, double RH)
        {
            return _coeffs["bSO2"] * C_SO2_inst
                  + _coeffs["bCO2"] * C_CO2_inst
                  + _coeffs["bNO2"] * C_NO2_inst
                  + _coeffs["bNO"] * C_NO_inst
                  + _coeffs["bT"] * T
                  + _coeffs["bP"] * P
                  + _coeffs["bRH"] * RH
                  + _coeffs["eCO2"];  
        }

        public double ComputeNO2(
                                double C_SO2_inst,
                                double C_CO2_inst,
                                double C_NO2_inst,
                                double C_NO_inst,
                                double T,
                                double P,
                                double RH)
        {
            return _coeffs["cSO2"] * C_SO2_inst
                  + _coeffs["cCO2"] * C_CO2_inst
                  + _coeffs["cNO2"] * C_NO2_inst
                  + _coeffs["cNO"] * C_NO_inst
                  + _coeffs["cT"] * T
                  + _coeffs["cP"] * P
                  + _coeffs["cRH"] * RH
                  + _coeffs["eNO2"];
        }

        public double ComputeNO(
                                double C_SO2_inst,
                                double C_CO2_inst,
                                double C_NO2_inst,
                                double C_NO_inst,
                                double T,
                                double P,
                                double RH)
        {
            return _coeffs["dSO2"] * C_SO2_inst
                  + _coeffs["dCO2"] * C_CO2_inst
                  + _coeffs["dNO2"] * C_NO2_inst
                  + _coeffs["dNO"] * C_NO_inst
                  + _coeffs["dT"] * T
                  + _coeffs["dP"] * P
                  + _coeffs["dRH"] * RH
                  + _coeffs["eNO"];
        }



        public double ComputeFSC(double C_SO2, double C_CO2, double C_SO2_bkg, double C_CO2_bkg)
        {
            // 1) Log all inputs
            Console.WriteLine($"[DEBUG] ComputeFSC called with: C_SO2={C_SO2}, C_CO2={C_CO2}, " +
                              $"C_SO2_bkg={C_SO2_bkg}, C_CO2_bkg={C_CO2_bkg}");

            // 2) Compute delta and log it
            double deltaCO2 = C_CO2 - C_CO2_bkg;
            Console.WriteLine($"[DEBUG] deltaCO2 = C_CO2 - C_CO2_bkg = {C_CO2} - {C_CO2_bkg} = {deltaCO2}");

            // 3) Guard against zero and log error
            if (Math.Abs(deltaCO2) < double.Epsilon)
            {
                Console.WriteLine("[ERROR] deltaCO2 is zero (or extremely close), cannot divide by zero.");
                throw new DivideByZeroException("Background-subtracted CO₂ is zero. Cannot compute FSC.");
            }

            // 4) Compute FSC and log result
            double fsc = (C_SO2 - C_SO2_bkg) / deltaCO2 * 0.232;
            Console.WriteLine($"[DEBUG] FSC = (C_SO2 - C_SO2_bkg) / deltaCO2 * 0.232 = " +
                              $"({C_SO2} - {C_SO2_bkg}) / {deltaCO2} * 0.232 = {fsc}");

            return fsc;
        }


        public double ComputeSO2NO2Ratio(double C_SO2, double C_NO2)
        {
            if (Math.Abs(C_NO2) < 1e-6)
                throw new DivideByZeroException("C_NO2 is too close to zero.");
            return C_SO2 / C_NO2;
        }

        #endregion


        // 1) Initial discovery + load of last 10 entries
        private async Task FetchSensorIdAndInitialMeasurements()
        {
            Console.WriteLine("[DEBUG] >> FetchSensorIdAndInitialMeasurements()");

            try
            {
                // Authenticate for the sensor‐list GET
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Token", AuthToken);

                Console.WriteLine($"[DEBUG]    GET {BaseUrl}");
                using (var listResp = await client.GetAsync(BaseUrl))
                {
                    Console.WriteLine($"[DEBUG]    sensor list → {listResp.StatusCode}");
                    if (!listResp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[ERROR] Failed to get sensor list: {(int)listResp.StatusCode} {listResp.ReasonPhrase}");
                        lblStatus.Text = $"Error {(int)listResp.StatusCode} fetching sensors";
                        return;
                    }

                    string listJson = await listResp.Content.ReadAsStringAsync();
                    using (var listDoc = JsonDocument.Parse(listJson))
                    {
                        var sensors = listDoc.RootElement.GetProperty("results");
                        foreach (var s in sensors.EnumerateArray())
                        {
                            if (s.GetProperty("serial_number").GetString() == SensorName)
                            {
                                sensorId = s.GetProperty("id").GetInt32();
                                _measurementsUrl = s.GetProperty("measurements").GetString();
                                break;
                            }
                        }
                    }
                }

                if (sensorId < 0)
                {
                    lblStatus.Text = "Sensor not found";
                    return;
                }

                // Fetch the last 10 measurement blocks
                string initialUrl = $"{_measurementsUrl}?format=json&limit=10";
                Console.WriteLine($"[DEBUG]    GET initial 10 measurements → {initialUrl}");
                using (var measResp = await client.GetAsync(initialUrl))
                {
                    Console.WriteLine($"[DEBUG]    initial measurements → {measResp.StatusCode}");
                    measResp.EnsureSuccessStatusCode();

                    string measJson = await measResp.Content.ReadAsStringAsync();
                    using (var measDoc = JsonDocument.Parse(measJson))
                    {
                        var results = measDoc.RootElement.GetProperty("results");
                        int count = results.GetArrayLength();
                        Console.WriteLine($"[DEBUG]    results.count = {count}");
                        if (count == 0)
                        {
                            txtTimestamp.Text = "No data";
                            ClearMeasurementFields("No data");
                            return;
                        }

                        // Build and sort the list of (Date, Measurements)
                        var sets = results
                            .EnumerateArray()
                            .Select(e => new {
                                Date = DateTime.Parse(
                                    e.GetProperty("date").GetString(),
                                    null,
                                    DateTimeStyles.AdjustToUniversal),
                                Measurements = e.GetProperty("measurements")
                            })
                            .OrderBy(x => x.Date)
                            .ToList();

                        // Display & log each one in chronological order
                        foreach (var s in sets)
                        {
                            txtTimestamp.Text = s.Date.ToString("O");
                            DisplayMeasurements(s.Measurements);
                            previousLastUpdate = s.Date.ToString("O");
                        }

                        lblStatus.Text = $"Initial load of {sets.Count} entries complete.";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.GetType().Name}: {ex.Message}");
                lblStatus.Text = "Initialization error";
            }
            finally
            {
                Console.WriteLine("[DEBUG] << FetchSensorIdAndInitialMeasurements()");
            }
        }



        // 2) Poll only new data
        private async Task PollMeasurementsIfUpdated()
        {
            if (string.IsNullOrEmpty(_measurementsUrl)) return;

            // build your GET URL with your last-seen timestamp
            string url = $"{_measurementsUrl}?format=json&limit=10&start="
                       + WebUtility.UrlEncode(previousLastUpdate);

            Console.WriteLine($"[DEBUG] GET {url}");
            using (var resp = await client.GetAsync(url))
            {
                Console.WriteLine($"[DEBUG] poll response ␦ {resp.StatusCode}");
                if ((int)resp.StatusCode == 429)
                {
                    // Replace the problematic line with the following:
                  // 429 is the HTTP status code for Too Many Requests
                    // back off logic …
                    return;
                }

                resp.EnsureSuccessStatusCode();
                string body = await resp.Content.ReadAsStringAsync();

                using (var doc = JsonDocument.Parse(body))
                {
                    var results = doc.RootElement.GetProperty("results");
                    int count = results.GetArrayLength();
                    Console.WriteLine($"[DEBUG] poll results count = {count}");
                    if (count == 0) return;

                    // 1) Parse into a list + sort ascending by date
                    var sets = results
                        .EnumerateArray()
                        .Select(r => new
                        {
                            Date = DateTime.Parse(r.GetProperty("date").GetString(), null, DateTimeStyles.AdjustToUniversal),
                            Measurements = r.GetProperty("measurements")
                        })
                        .OrderBy(x => x.Date)
                        .ToList();

                    // 2) Display them in order (oldest → newest)
                    foreach (var set in sets)
                    {
                        Console.WriteLine($"[DEBUG] applying set date = {set.Date:O}");
                        txtTimestamp.Text = set.Date.ToString("O");
                        DisplayMeasurements(set.Measurements);
                    }

                    // 3) Advance your cursor to the **newest** set’s date
                    var newest = sets.Last().Date;
                    previousLastUpdate = newest.ToString("O");
                    lblStatus.Text = $"Received {count} new update(s), last at {newest:O}.";
                }
            }
        }

        // ─────────── HELPERS ──────────────────────────────────────

     

        /// <summary>
        /// Updates a TextBox either via the lookup map or falls back to timestamp.
        /// </summary>
        /// <summary>
        /// Called every time any measurement textbox gets updated.
        /// </summary>
        private void UpdateTextBox(string type, string value)
        {
            if (_measurementBoxes.TryGetValue(type, out var box))
            {
                box.Text = value;
                if (type.Equals("CO2", StringComparison.OrdinalIgnoreCase))
                    InstantAnalyzerCO2 = value;
            }

            // Now that raw sensor boxes might have changed, refresh FSC/ratio:
       
        }



        /// <summary>
        /// Iterates through the JSON measurements array and updates UI.
        /// </summary>
        private void DisplayMeasurements(JsonElement measurementsArray)
        {
            // 1) Build a map of type→double for all parsable values
            var numeric = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in measurementsArray.EnumerateArray())
            {
                string key = m.GetProperty("type").GetString();
                string raw = m.GetProperty("value").ToString();
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    numeric[key] = v;
            }

            // 2) Extract instrument readings & environmentals (0 if missing)
            double so2Instant = numeric.TryGetValue("SO2 Inst", out var t1) ? t1 : 0;
            double co2Instant = numeric.TryGetValue("CO2 Inst", out var t2) ? t2 : 0;
            double no2Instant = numeric.TryGetValue("NO2 Inst", out var t3) ? t3 : 0;
            double noInstant = numeric.TryGetValue("NO Inst", out var t4) ? t4 : 0;
            double temp = numeric.TryGetValue("Temperature", out var t5) ? t5 : 0;
            double pressure = numeric.TryGetValue("Pressure", out var t6) ? t6 : 0;
            double rh = numeric.TryGetValue("RH", out var t7) ? t7 : 0;

            // 3) Compute modeled concentrations
            double SO2Calibrated = ComputeSO2(so2Instant, co2Instant, no2Instant, noInstant, temp, pressure, rh);
            double CO2Calibrated = ComputeCO2(so2Instant, co2Instant, no2Instant, noInstant, temp, pressure, rh);
            double NO2Calibrated = ComputeNO2(so2Instant, co2Instant, no2Instant, noInstant, temp, pressure, rh);
            double NOCalibrated = ComputeNO(so2Instant, co2Instant, no2Instant, noInstant, temp, pressure, rh);

            // 4) Update the modeled‐values UI
            txtSO2.Text = $"{SO2Calibrated:F3} ppb";
            txtCO2.Text = $"{CO2Calibrated:F3} ppm";
            txtNO2.Text = $"{NO2Calibrated:F3} ppb";
            txtNO.Text = $"{NOCalibrated:F3} ppb";

            double fsc = ComputeFSC(SO2Calibrated, CO2Calibrated, _so2Bkg, _co2Bkg);
            txtFSC.Text = $"{fsc:F5} %";

            // 5) Compute SO2/NO2 ratio if possible
            double no2Raw = numeric.TryGetValue("NO2", out var nu) ? nu : 0;
            txtSO2NO2Ratio.Text = Math.Abs(no2Raw) > 1e-6
                ? ComputeSO2NO2Ratio(SO2Calibrated, no2Raw).ToString("F3", CultureInfo.InvariantCulture)
                : "-";

            // 6) Update every raw‐measurement TextBox & debug print
            foreach (var m in measurementsArray.EnumerateArray())
            {
                string type = m.GetProperty("type").GetString();
                string value = m.GetProperty("value").ToString();
                UpdateTextBox(type, value);
                Console.WriteLine($"[DEBUG] {type} = {value}");
            }

            // 7) Log to CSV *only if* this timestamp is strictly newer
            if (DateTime.TryParse(txtTimestamp.Text,
                                  null,
                                  DateTimeStyles.RoundtripKind,
                                  out var ts) &&
                ts > _lastLoggedTimestamp)
            {
                _lastLoggedTimestamp = ts;

                LogToCsv(
                    ts,
                    so2Instant, co2Instant, no2Instant, noInstant,
                    temp, pressure, rh,
                    SO2Calibrated,
                    CO2Calibrated,
                    NO2Calibrated,
                    NOCalibrated,
                    fsc,
                    no2Raw,
                    _so2Bkg,
                    _co2Bkg,
                    GlobalSensorStats.CO2Instant,
                    GlobalSensorStats.MaxCO2,
                    GlobalSensorStats.MaxCO2Timestamp,
                    GlobalSensorStats.HDCO2Instant,
                    GlobalSensorStats.MaxHDCO2,
                    GlobalSensorStats.MaxHDCO2Timestamp
                );
                Console.WriteLine($"[DEBUG] Logged CSV row for {ts:O}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] Skipped logging for {txtTimestamp.Text}");
            }
        }



        /// <summary>
        /// Clears all measurement boxes to a common message.
        /// </summary>
        private void ClearMeasurementFields(string message)
        {
            foreach (var box in _measurementBoxes.Values)
                box.Text = message;
        }

        /// <summary>
        /// Handles generic fetch errors by marking UI and showing a message.
        /// </summary>
        private void HandleFetchError(Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
            txtTimestamp.Text = "Error";
            ClearMeasurementFields("Error");
            MessageBox.Show($"Failed to fetch sensor data: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    }
