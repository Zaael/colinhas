using System.IO;

namespace Colinhas.Services;

/// <summary>
/// Simple file logger. Packaged WinUI apps don't show Console output when
/// launched via `dotnet run`, so we write to a file you can tail instead.
/// Log lives at: %LOCALAPPDATA%\Colinhas\colinhas.log
/// </summary>
public static class Logger
{
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Colinhas", "colinhas.log");

    private static readonly object Gate = new();

    static Logger()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!); }
        catch { }
    }

    public static void Log(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}";
        System.Diagnostics.Debug.WriteLine(line);
        try
        {
            lock (Gate)
                File.AppendAllText(FilePath, line + Environment.NewLine);
        }
        catch { }
    }
}
