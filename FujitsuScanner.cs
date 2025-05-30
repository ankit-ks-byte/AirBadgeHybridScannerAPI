
using System;

public static class FujitsuScanner
{
    public static string StartScanAndReturnPath(ScanConfig config)
    {
        Logger.Log("FujitsuScanner using TWAIN via PaperStream with OutputFileQuality...");
        return TwainScanner.StartScanAndReturnPath(config);
    }
}
