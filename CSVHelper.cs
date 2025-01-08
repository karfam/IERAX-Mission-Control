using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IERAX_MissionControl
{
    public static class CSVHelper
    {
        private static readonly string FilePath = GenerateFilePath();

        private static string GenerateFilePath()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"CO2_Data_{timestamp}.csv";
        }

        public static void WriteToCSV(MAVLinkData data)
        {
            try
            {
                string filePath = FilePath;

                // Check if the file exists; if not, create it with a header row
                if (!File.Exists(filePath))
                {
                    File.AppendAllText(filePath, "SysId,CompId,ParamName,ParamValue,Timestamp\n");
                }

                // Append the new data
                string line = $"{data.SysId},{data.CompId},{data.ParamName},{data.ParamValue},{data.Timestamp:O}";
                File.AppendAllText(filePath, line + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to CSV: {ex.Message}");
            }
        }

        public static void WriteBatchToCSV(List<MAVLinkData> dataBatch)
        {
            try
            {
                // Check if the file exists; if not, create it with a header row
                if (!File.Exists(FilePath))
                {
                    File.AppendAllText(FilePath, "SysId,CompId,ParamName,ParamValue,Timestamp\n");
                }

                // Append all data in one operation
                var lines = dataBatch.Select(data =>
                    $"{data.SysId},{data.CompId},{data.ParamName},{data.ParamValue},{data.Timestamp:O}");
                File.AppendAllText(FilePath, string.Join("\n", lines) + "\n");

                Console.WriteLine($"Batch written to file: {FilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing batch to CSV: {ex.Message}");
            }
        }


    }
}
