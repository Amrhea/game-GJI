using System.IO;
using UnityEngine;

/// <summary>
/// Playtest helper: mirrors every Unity console message (log/warning/error and
/// exceptions with stack traces) into Logs/runtime-playtest.log at the project
/// root, so runtime output can be read from outside the editor without the
/// console. The file is overwritten at the start of every Play session and
/// flushed per line, so it can be read while the game is still running.
/// No scene setup needed (auto-hooked). Safe to delete before shipping.
/// </summary>
public static class RuntimeLogFile
{
    private const string FileName = "runtime-playtest.log";
    private static readonly object Lock = new object();
    private static StreamWriter _writer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        lock (Lock)
        {
            // Re-hook defensively (no-ops when not subscribed) so repeated
            // loads never double-subscribe or duplicate lines.
            Application.logMessageReceived -= OnLogMessage;
            Application.quitting -= Close;

            string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            Directory.CreateDirectory(directory);

            _writer?.Dispose();
            _writer = new StreamWriter(Path.Combine(directory, FileName), append: false);
            _writer.WriteLine($"=== Play session started {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} — scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}' ===");
            _writer.Flush();

            Application.logMessageReceived += OnLogMessage;
            Application.quitting += Close;
        }
    }

    private static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        lock (Lock)
        {
            if (_writer == null)
            {
                return;
            }

            _writer.WriteLine($"[{Time.realtimeSinceStartup:0.00}] [{type}] {condition}");
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                _writer.Write(stackTrace);
            }

            _writer.Flush();
        }
    }

    private static void Close()
    {
        lock (Lock)
        {
            Application.logMessageReceived -= OnLogMessage;
            Application.quitting -= Close;
            _writer?.Dispose();
            _writer = null;
        }
    }
}
