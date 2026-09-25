using System.Text;

namespace NoMoreThaiNums.Logging;

/// <summary>
/// Append-only error log used exclusively for application faults (load/save
/// failures, hook errors, unexpected exceptions).
///
/// PRIVACY: this class must NEVER receive typed characters, keystrokes, or any
/// user input content. Only exception messages and stack traces are written.
/// </summary>
public static class ErrorLog
{
    private const int MaxBytes = 256 * 1024;
    private static readonly object Gate = new();

    /// <summary>
    /// Overridable log directory, used by the test suite so unit tests never
    /// write to the real user profile. Null means the default location.
    /// </summary>
    internal static string? DirectoryOverride;

    private static string GetLogDirectory()
    {
        if (DirectoryOverride is not null)
        {
            return DirectoryOverride;
        }

        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "NoMoreThaiNums");
    }

    public static string GetLogFilePath() => Path.Combine(GetLogDirectory(), "error.log");

    public static void Write(string message, Exception? exception = null)
    {
        try
        {
            lock (Gate)
            {
                string dir = GetLogDirectory();
                Directory.CreateDirectory(dir);
                string path = GetLogFilePath();
                RotateIfTooLarge(path);

                var sb = new StringBuilder();
                sb.Append('[')
                  .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                  .AppendLine("]");
                sb.AppendLine(message);
                if (exception is not null)
                {
                    sb.AppendLine(exception.ToString());
                }

                sb.AppendLine();
                File.AppendAllText(path, sb.ToString());
            }
        }
        catch
        {
            // Logging must never crash the app. Deliberately silent.
        }
    }

    private static void RotateIfTooLarge(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Exists && info.Length > MaxBytes)
            {
                string backup = path + ".old";
                File.Delete(backup);
                File.Move(path, backup);
            }
        }
        catch (IOException)
        {
            // Rotation is best-effort; the append that follows may still work.
        }
    }
}
