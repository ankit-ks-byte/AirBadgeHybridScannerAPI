
using NTwain;
using NTwain.Data;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;

public static class TwainScanner
{
    static TwainSession _twain;
    static DataSource _ds;
    static string savedPath = null;

    static readonly string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
    static void Log(string message)
    {
        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}{Environment.NewLine}");
    }

    public static string StartScanAndReturnPath(ScanConfig config = null)
    {
        savedPath = null;
        _twain = new TwainSession(TWIdentity.CreateFromAssembly(DataGroups.Image, typeof(TwainScanner).Assembly));

        _twain.TransferError += (s, e) =>
        {
            Log("Transfer error: " + e.Exception?.Message);
        };

        _twain.DataTransferred += (s, e) =>
        {
            if (e.NativeData != IntPtr.Zero)
            {
                try
                {
                    using (var image = System.Drawing.Image.FromHbitmap(e.NativeData))
                    {
                        string filename = $"Scan_{DateTime.Now.Ticks}.jpg";
                        savedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
                        SaveJpeg(savedPath, image, config?.OutputFileQuality ?? 40L);
                        Log("Image saved to " + savedPath);
                    }
                }
                catch (Exception ex)
                {
                    Log("Image conversion error: " + ex.Message);
                }
            }
        };

        _twain.StateChanged += (s, e) =>
        {
            Log("TWAIN State changed to: " + _twain.State);
            if (_twain.State == 4)
            {
                _ds = _twain.FirstOrDefault(d =>
                    d.Name.ToLower().Contains("fujitsu") ||
                    d.Name.ToLower().Contains("fi-") ||
                    d.Name.ToLower().Contains("canon") ||
                    d.Name.ToLower().Contains("lide")
                );

                if (_ds == null)
                {
                    Log("Scanner not found.");
                    return;
                }

                Log("Scanner selected: " + _ds.Name);
                _ds.Open();
                _ds.Enable(SourceEnableMode.ShowUI, false, IntPtr.Zero);
            }
        };

        Log("TWAIN session opened.");
        _twain.Open();

        System.Threading.Thread.Sleep(10000);

        _ds?.Close();
        _twain?.Close();

        if (config != null && config.OutputFormat.ToLower() == "pdf" && savedPath != null)
        {
            savedPath = GenerateCompressedPdf(savedPath);
        }

        return savedPath;
    }

    private static void SaveJpeg(string path, System.Drawing.Image img, long quality)
    {
        var encoder = ImageCodecInfo.GetImageDecoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
        if (encoder == null) return;

        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        img.Save(path, encoder, encoderParams);
    }

    private static string GenerateCompressedPdf(string jpgPath)
    {
        string pdfPath = Path.ChangeExtension(jpgPath, ".pdf");

        var writer = new PdfWriter(pdfPath, new WriterProperties().SetCompressionLevel(9));
        var pdf = new PdfDocument(writer);
        var doc = new Document(pdf);

        var imgData = ImageDataFactory.Create(jpgPath);
        var image = new iText.Layout.Element.Image(imgData).ScaleToFit(400, 700);

        doc.Add(image);
        doc.Close();

        File.Delete(jpgPath);
        return pdfPath;
    }
}
