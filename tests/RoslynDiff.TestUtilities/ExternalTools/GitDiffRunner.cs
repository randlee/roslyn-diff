using System.Diagnostics;
using System.Text;

namespace RoslynDiff.TestUtilities.ExternalTools;

/// <summary>
/// Provides functionality to run git diff command.
/// </summary>
public static class GitDiffRunner
{
    /// <summary>
    /// Checks if git is available on the system.
    /// </summary>
    /// <returns>True if git is available, false otherwise.</returns>
    public static bool IsAvailable()
    {
        try
        {
            var result = Run("--version", 1000);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Runs git diff --no-index on two files.
    /// </summary>
    /// <param name="oldFile">Path to the old file.</param>
    /// <param name="newFile">Path to the new file.</param>
    /// <param name="contextLines">Number of context lines (default: 3).</param>
    /// <returns>A tuple containing exit code, stdout, and stderr.</returns>
    public static (int ExitCode, string StdOut, string StdErr) RunDiff(
        string oldFile,
        string newFile,
        int contextLines = 3)
    {
        if (oldFile == null) throw new ArgumentNullException(nameof(oldFile));
        if (newFile == null) throw new ArgumentNullException(nameof(newFile));

        var args = $"diff --no-index -U{contextLines} \"{oldFile}\" \"{newFile}\"";
        return Run(args);
    }

    /// <summary>
    /// Runs the git command with custom arguments.
    /// </summary>
    /// <param name="arguments">The arguments to pass to git.</param>
    /// <param name="timeout">Timeout in milliseconds (default: 30000).</param>
    /// <returns>A tuple containing exit code, stdout, and stderr.</returns>
    public static (int ExitCode, string StdOut, string StdErr) Run(
        string arguments,
        int timeout = 30000)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(timeout))
        {
            process.Kill();
            throw new TimeoutException($"git command timed out after {timeout}ms");
        }

        // Note: git diff returns 0 if files are identical, 1 if different
        return (process.ExitCode, output.ToString(), error.ToString());
    }

    /// <summary>
    /// Parses git diff output to extract changed line numbers.
    /// </summary>
    /// <param name="diffOutput">The output from git diff.</param>
    /// <returns>A collection of line numbers that were changed.</returns>
    public static IEnumerable<int> ExtractChangedLineNumbers(string diffOutput)
    {
        if (string.IsNullOrEmpty(diffOutput))
        {
            yield break;
        }

        var lines = diffOutput.Split('\n');
        var lineNumbers = new HashSet<int>();

        foreach (var line in lines)
        {
            if (line.StartsWith("@@"))
            {
                // Parse hunk header: @@ -l,s +l,s @@
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var newRange = parts[2]; // +l,s
                    if (newRange.StartsWith("+"))
                    {
                        var rangeParts = newRange[1..].Split(',');
                        if (int.TryParse(rangeParts[0], out var start))
                        {
                            var count = rangeParts.Length > 1 && int.TryParse(rangeParts[1], out var c) ? c : 1;
                            for (int i = 0; i < count; i++)
                            {
                                lineNumbers.Add(start + i);
                            }
                        }
                    }
                }
            }
        }

        foreach (var lineNumber in lineNumbers.OrderBy(n => n))
        {
            yield return lineNumber;
        }
    }
}
