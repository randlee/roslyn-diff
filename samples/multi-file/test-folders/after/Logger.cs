namespace MathLib.Utilities;

/// <summary>
/// Simple logging utility
/// </summary>
public class Logger
{
    public void LogInfo(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }

    public void LogError(string message)
    {
        Console.Error.WriteLine($"[ERROR] {message}");
    }
}
