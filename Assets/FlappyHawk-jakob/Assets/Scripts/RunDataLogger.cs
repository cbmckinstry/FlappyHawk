using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class RunDataLogger
{
    private const string FileName = "game_runs.csv";
    private const string PlayerIdKey = "player_id";

    // Save folder (Documents/FlappyHawk/Logs on desktop)
    private static readonly string PreferredDesktopDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FlappyHawk", "Logs");

    // Automatically choose best writable location
    private static string BestWritableDir
    {
        get
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (TryEnsureWritable(PreferredDesktopDir))
                return PreferredDesktopDir;
#endif
            var fallback = Application.persistentDataPath;
            TryEnsureWritable(fallback);
            return fallback;
        }
    }

    private static string FilePath => Path.Combine(BestWritableDir, FileName);

    public static string PlayerId
    {
        get
        {
            if (!PlayerPrefs.HasKey(PlayerIdKey))
            {
                PlayerPrefs.SetString(PlayerIdKey, Guid.NewGuid().ToString("N"));
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetString(PlayerIdKey);
        }
    }

    /// <summary>
    /// Append a gameplay run entry to the log CSV file.
    /// </summary>
    public static void AppendRun(
        string playerId,
        Difficulty difficulty,   // now uses global enum, not GameManager.Difficulty
        int score,
        float roundSeconds,
        DateTime startUtc,
        int pipesSpawned,
        int jumps
    )
    {
        try
        {
            var newFile = !File.Exists(FilePath);

            using (var sw = new StreamWriter(FilePath, append: true))
            {
                if (newFile)
                    sw.WriteLine("player_id,difficulty,score,round_seconds,start_utc,pipes_spawned,jumps");

                string line = string.Join(",",
                    Escape(playerId),
                    Escape(difficulty.ToString()),
                    score.ToString(CultureInfo.InvariantCulture),
                    roundSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                    Escape(startUtc.ToString("o", CultureInfo.InvariantCulture)),
                    pipesSpawned.ToString(CultureInfo.InvariantCulture),
                    jumps.ToString(CultureInfo.InvariantCulture)
                );

                sw.WriteLine(line);
            }

#if UNITY_EDITOR
            Debug.Log($"[RunDataLogger] Wrote run to: {FilePath}");
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RunDataLogger] Failed to write run: {ex}");
        }
    }

    public static string GetLogFilePath() => FilePath;
    public static string GetLogFolder() => BestWritableDir;

    private static bool TryEnsureWritable(string dir)
    {
        try
        {
            if (string.IsNullOrEmpty(dir)) return false;
            Directory.CreateDirectory(dir);
#if UNITY_STANDALONE || UNITY_EDITOR
            var probe = Path.Combine(dir, ".write_test.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
#endif
            return true;
        }
        catch { return false; }
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
