using System.Runtime.InteropServices;

namespace RoslynDiff.Cli;

/// <summary>
/// Detects available code editors on the system by checking for installed applications.
/// </summary>
public static class EditorDetector
{
    /// <summary>
    /// Editor information including app path and URL scheme.
    /// </summary>
    public record EditorInfo(string Name, string AppPath, string UrlScheme);

    /// <summary>
    /// Known editors and their detection paths on macOS.
    /// </summary>
    private static readonly EditorInfo[] MacOSEditors =
    [
        new("vscode", "/Applications/Visual Studio Code.app", "vscode://"),
        new("rider", "/Applications/Rider.app", "jetbrains://"),  // Uses JetBrains Toolbox
        new("pycharm", "/Applications/PyCharm.app", "pycharm://"),
        new("zed", "/Applications/Zed.app", "zed://"),
    ];

    /// <summary>
    /// Known editors and their detection paths on Windows.
    /// </summary>
    private static readonly EditorInfo[] WindowsEditors =
    [
        new("vscode", @"C:\Program Files\Microsoft VS Code\Code.exe", "vscode://"),
        new("vscode", @"C:\Users\%USERNAME%\AppData\Local\Programs\Microsoft VS Code\Code.exe", "vscode://"),
        new("rider", @"C:\Program Files\JetBrains\Rider\bin\rider64.exe", "jetbrains://"),
        new("pycharm", @"C:\Program Files\JetBrains\PyCharm\bin\pycharm64.exe", "pycharm://"),
    ];

    /// <summary>
    /// Known editors and their detection paths on Linux.
    /// </summary>
    private static readonly EditorInfo[] LinuxEditors =
    [
        new("vscode", "/usr/share/code/code", "vscode://"),
        new("vscode", "/snap/code/current/usr/share/code/code", "vscode://"),
        new("zed", "/usr/bin/zed", "zed://"),
    ];

    /// <summary>
    /// Detects which editors are available on the current system.
    /// </summary>
    /// <returns>List of available editor names (e.g., "vscode", "rider", "zed").</returns>
    public static IReadOnlyList<string> DetectAvailableEditors()
    {
        var editors = new HashSet<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            foreach (var editor in MacOSEditors)
            {
                if (Directory.Exists(editor.AppPath) || File.Exists(editor.AppPath))
                {
                    editors.Add(editor.Name);
                }
            }

            // Also check for JetBrains Toolbox (required for Rider URL scheme)
            if (editors.Contains("rider") && !Directory.Exists("/Applications/JetBrains Toolbox.app"))
            {
                // Rider is installed but Toolbox isn't - URL scheme won't work
                // Keep it anyway since user might have standalone Rider
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var editor in WindowsEditors)
            {
                var path = Environment.ExpandEnvironmentVariables(editor.AppPath);
                if (File.Exists(path))
                {
                    editors.Add(editor.Name);
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            foreach (var editor in LinuxEditors)
            {
                if (File.Exists(editor.AppPath))
                {
                    editors.Add(editor.Name);
                }
            }
        }

        return editors.ToList();
    }
}
