using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class RunDataLogger
{
    private const string FileName = "game_runs.csv";
    private const string RunIdKey = "run_id_counter";

    private static readonly string PreferredDesktopDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FlappyHawk", "Logs");

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

    // Auto-incrementing run ID
    public static int GetNextRunId()
    {
        int id = PlayerPrefs.GetInt(RunIdKey, 0) + 1;
        PlayerPrefs.SetInt(RunIdKey, id);
        PlayerPrefs.Save();
        return id;
    }

    // ============================================
    // MAIN ENTRY
    // ============================================
    public static void AppendRun(RunLogData data)
    {
        try
        {
            bool newFile = !File.Exists(FilePath);

            using (var sw = new StreamWriter(FilePath, append: true))
            {
                if (newFile)
                {
                    sw.WriteLine(
                        "run_id,player_name,game_mode,difficulty,score,player_score,enemy_score," +
                        "round_seconds,obstacles_spawned,jumps,helmets_collected," +
                        "offense_drives,defense_rounds_won,defense_rounds_failed"
                    );
                }

                string line = string.Join(",",
                    data.runId,
                    Escape(data.playerName),
                    Escape(data.gameMode),
                    Escape(data.difficulty),
                    data.score,
                    data.playerScore,
                    data.enemyScore,
                    data.roundSeconds.ToString("0.###"),
                    data.obstaclesSpawned,
                    data.jumps,
                    data.helmetsCollected,
                    data.offenseDrives,
                    data.defenseRoundsWon,
                    data.defenseRoundsFailed
                );

                sw.WriteLine(line);
            }

#if UNITY_EDITOR
            Debug.Log($"[RunDataLogger] Saved run → {FilePath}");
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RunDataLogger] Error writing log: {ex}");
        }
    }

    // ============================================
    // HELPERS
    // ============================================
    private static bool TryEnsureWritable(string dir)
    {
        try
        {
            if (string.IsNullOrEmpty(dir))
                return false;

            Directory.CreateDirectory(dir);

#if UNITY_STANDALONE || UNITY_EDITOR
            string probe = Path.Combine(dir, ".probe.tmp");
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

// ============================================
// UPDATED RUN DATA HOLDER
// ============================================
public class RunLogData
{
    public int runId = RunDataLogger.GetNextRunId();

    public string playerName;   

    public string gameMode;      
    public string difficulty;    

    public int score;
    public int playerScore;
    public int enemyScore;

    public float roundSeconds;

    public int obstaclesSpawned;
    public int jumps;
    public int helmetsCollected;

    public int offenseDrives;
    public int defenseRoundsWon;
    public int defenseRoundsFailed;
}
