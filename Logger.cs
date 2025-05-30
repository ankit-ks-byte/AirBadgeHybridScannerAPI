using System;
using System.IO;

public static class Logger
{
    private static readonly string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");

    public static void Log(string message)
    {
        var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
        File.AppendAllText(logPath, logMessage + Environment.NewLine);
        Console.WriteLine(logMessage);
    }
}
