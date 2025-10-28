// RunDataLogger.cs
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class RunDataLogger
{
    private const string FileName = "game_runs.csv";
    private const string PlayerIdKey = "player_id";

    // Customize your preferred desktop directory
    private static readonly string PreferredDesktopDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FlappyBird", "Logs");

    // Choose best path (Desktop-friendly on Windows/mac/Linux; fallback for mobile/WebGL/sandbox)
    private static string BestWritableDir
    {
        get
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            // Try Documents/FlappyBird/Logs first
            if (TryEnsureWritable(PreferredDesktopDir))
                return PreferredDesktopDir;
#endif
            // Fallback: always works and is cross-platform
            var fallback = Application.persistentDataPath;
            TryEnsureWritable(fallback); // ensure it exists
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

    public static void AppendRun(
        string playerId,
        Difficulty difficulty,
        int score,
        double roundSeconds,
        DateTime startUtc,
        int pipesSpawned = 0,
        int jumps = 0
    )
    {
        try
        {
            var newFile = !File.Exists(FilePath);
            using (var sw = new StreamWriter(FilePath, append: true))
            {
                if (newFile)
                    sw.WriteLine("player_id,difficulty,score,round_seconds,start_utc,pipes_spawned,jumps");

                var line = string.Join(",",
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
            // quick probe
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
        if (s == null) return "";
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
