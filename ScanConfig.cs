
public class ScanConfig
{
    public string ScannerType { get; set; }
    public int Resolution { get; set; }
    public string OutputFormat { get; set; }
    public string ColorMode { get; set; }
    public bool Duplex { get; set; }
    public long OutputFileQuality { get; set; } = 80;
}
