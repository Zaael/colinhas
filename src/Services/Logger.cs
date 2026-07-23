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

    /// <summary>
    /// Marca no log que o processo continua vivo aos 5s, 15s e 60s.
    ///
    /// Serve para separar dois casos que parecem iguais de fora — o app "some"
    /// sem morrer: subiu com o Windows e perdeu o ícone da bandeja, ou o Sair
    /// tirou o ícone mas deixou o processo. Se o heartbeat continua depois de o
    /// ícone sumir, o processo está vivo e o problema é do ícone, não um crash.
    ///
    /// Usa timer de thread de propósito: nesses cenários a fila da UI pode estar
    /// parada, e um DispatcherQueueTimer simplesmente não tocaria.
    /// </summary>
    public static void StartHeartbeat()
    {
        foreach (var seconds in new[] { 5, 15, 60 })
        {
            var mark = seconds;
            var timer = new System.Threading.Timer(
                _ => Log($"heartbeat: processo vivo há {mark}s"),
                null, TimeSpan.FromSeconds(mark), System.Threading.Timeout.InfiniteTimeSpan);

            // Sem guardar a referência, o Timer pode ser coletado antes de tocar.
            lock (Gate) Heartbeats.Add(timer);
        }
    }

    private static readonly List<System.Threading.Timer> Heartbeats = [];

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
