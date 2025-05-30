
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using WIA;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using System.Linq;

public static class WIAScanner
{
    public static string StartScanAndReturnPath(ScanConfig config)
    {
        try
        {
            Logger.Log("WIA Scan started...");
            var deviceManager = new DeviceManager();

            DeviceInfo availableScanner = null;
            foreach (DeviceInfo info in deviceManager.DeviceInfos)
            {
                if (info.Type == WiaDeviceType.ScannerDeviceType)
                {
                    availableScanner = info;
                    break;
                }
            }

            if (availableScanner == null)
            {
                Logger.Log("No WIA scanner found.");
                return null;
            }

            var device = availableScanner.Connect();
            var item = device.Items[1];

            int dpi = config.Resolution;
            string formatID = FormatID.wiaFormatJPEG;

            SetItemProperty(item.Properties["6147"], dpi);
            SetItemProperty(item.Properties["6148"], dpi);

            Logger.Log($"Scanning at {dpi} DPI...");

            var imageFile = (ImageFile)item.Transfer(formatID);

            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scans");
            Directory.CreateDirectory(outputDir);

            var filenameBase = $"Scan_{DateTime.Now:yyyyMMdd_HHmmss}";
            var jpgPath = Path.Combine(outputDir, filenameBase + ".jpg");
            if (File.Exists(jpgPath)) File.Delete(jpgPath);
            imageFile.SaveFile(jpgPath);
            Logger.Log("WIA scan saved to " + jpgPath);

            if (config.OutputFormat.ToLower() == "pdf")
            {
                string pdfPath = GenerateCompressedPdf(jpgPath, config.OutputFileQuality);
                Logger.Log("PDF generated: " + pdfPath);
                return pdfPath;
            }
            else
            {
                return jpgPath;
            }
        }
        catch (Exception ex)
        {
            Logger.Log("WIA Scan Error: " + ex.Message);
            return null;
        }
    }

    private static void SetItemProperty(Property prop, int value)
    {
        if (prop != null) prop.set_Value(value);
    }

    private static void SaveJpeg(string path, System.Drawing.Image img, long quality)
    {
        var encoder = ImageCodecInfo.GetImageDecoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
        if (encoder == null) return;

        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        img.Save(path, encoder, encoderParams);
    }

    private static string GenerateCompressedPdf(string jpgPath, long quality)
    {
        string tempJpg = Path.ChangeExtension(jpgPath, ".compressed.jpg");
        using (var image = System.Drawing.Image.FromFile(jpgPath))
        {
            SaveJpeg(tempJpg, image, quality);
        }

        string pdfPath = Path.ChangeExtension(jpgPath, ".pdf");

        var writer = new PdfWriter(pdfPath, new WriterProperties().SetCompressionLevel(9));
        var pdf = new PdfDocument(writer);
        var doc = new Document(pdf);

        var imgData = ImageDataFactory.Create(tempJpg);
        var imageElement = new iText.Layout.Element.Image(imgData).ScaleToFit(400, 700);

        doc.Add(imageElement);
        doc.Close();

        File.Delete(jpgPath);
        File.Delete(tempJpg);
        return pdfPath;
    }
}
