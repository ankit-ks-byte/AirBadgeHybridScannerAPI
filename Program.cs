
using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

class Program
{
    static void Main()
    {
        Logger.Log("Hybrid Scanner API Starting...");
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5001/scan/");
        listener.Start();
        Logger.Log("Listening on http://localhost:5001/scan/");

        while (true)
        {
            var context = listener.GetContext();
            var response = context.Response;

            // Add CORS headers
            response.AddHeader("Access-Control-Allow-Origin", "*");

            if (context.Request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
                response.StatusCode = 200;
                response.Close();
                continue;
            }

            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                var config = JsonConvert.DeserializeObject<ScanConfig>(File.ReadAllText(configPath));

                string scannedFile = RunScan(config);
                if (File.Exists(scannedFile))
                {
                    var bytes = File.ReadAllBytes(scannedFile);
                    var base64 = Convert.ToBase64String(bytes);
                    var mimeType = Path.GetExtension(scannedFile).ToLower() == ".pdf" ? "application/pdf" : "image/jpeg";
                    var dataUri = $"data:{mimeType};base64,{base64}";

                    var responseObject = new
                    {
                        filename = Path.GetFileName(scannedFile),
                        mimeType = mimeType,
                        base64 = dataUri
                    };

                    response.ContentType = "application/json";
                    var json = JsonConvert.SerializeObject(responseObject);
                    using (var writer = new StreamWriter(response.OutputStream))
                    {
                        writer.Write(json);
                    }
                }
                else
                {
                    response.StatusCode = 500;
                    var error = new { error = "Scan failed" };
                    using (var writer = new StreamWriter(response.OutputStream))
                    {
                        writer.Write(JsonConvert.SerializeObject(error));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Unhandled server error: " + ex.Message);
                response.StatusCode = 500;
                var error = new { error = "Internal server error" };
                using (var writer = new StreamWriter(response.OutputStream))
                {
                    writer.Write(JsonConvert.SerializeObject(error));
                }
            }
            finally
            {
                response.Close();
            }
        }
    }

    static string RunScan(ScanConfig config)
    {
        string outputFile = null;
        switch (config.ScannerType.ToLower())
        {
            case "wia":
                outputFile = WIAScanner.StartScanAndReturnPath(config);
                break;
            case "twain":
                outputFile = TwainScanner.StartScanAndReturnPath();
                break;
            case "fujitsu":
                outputFile = FujitsuScanner.StartScanAndReturnPath(config);
                break;
        }
        return outputFile;
    }
}
