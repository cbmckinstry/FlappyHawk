using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class RunDataLogger
{
    private const string FullFileName = "game_runs.csv";
    private const string LeaderboardFileName = "leaderboard.csv";
    private const string RunIdKey = "run_id_counter";

    private static readonly string LogsFolder = Path.Combine(Application.dataPath, "Logs");
    private static readonly string FullFilePath = Path.Combine(LogsFolder, FullFileName);
    private static readonly string LeaderboardFilePath = Path.Combine(LogsFolder, LeaderboardFileName);

    static RunDataLogger()
    {
        if (!Directory.Exists(LogsFolder))
            Directory.CreateDirectory(LogsFolder);
    }

    public static int GetNextRunId()
    {
        int id = PlayerPrefs.GetInt(RunIdKey, 0) + 1;
        PlayerPrefs.SetInt(RunIdKey, id);
        PlayerPrefs.Save();
        return id;
    }

    public static void AppendRun(RunLogData data)
    {
        WriteFullCSV(data);
        WriteLeaderboardCSV(data);
    }


    private static void WriteFullCSV(RunLogData data)
    {
        try
        {
            bool newFile = !File.Exists(FullFilePath);

            using (var sw = new StreamWriter(FullFilePath, append: true))
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
                    data.roundSeconds.ToString("0.###", CultureInfo.InvariantCulture),
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
            Debug.Log($"[RunDataLogger] Saved run → {FullFilePath}");
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RunDataLogger] Error writing log: {ex}");
        }
    }

    private static void WriteLeaderboardCSV(RunLogData data)
    {
        try
        {
            bool newFile = !File.Exists(LeaderboardFilePath);

            using (var sw = new StreamWriter(LeaderboardFilePath, append: true))
            {
                if (newFile)
                    sw.WriteLine("name,score,mode,difficulty");

                int leaderboardScore = data.gameMode.Contains("GameDay")
                    ? data.playerScore - data.enemyScore
                    : data.score;

                sw.WriteLine(string.Join(",",
                    Escape(data.playerName),
                    leaderboardScore,
                    Escape(data.gameMode),
                    Escape(data.difficulty)
                ));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RunDataLogger] Error writing leaderboard log: {ex}");
        }
    }


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

    public static string GetLogFolder()
    {
        return LogsFolder;
    }
}


public class RunLogData
{
    public int runId = RunDataLogger.GetNextRunId();

    public string playerName;

    public string gameMode;
    public string difficulty;

    public int score;          // Iowa mode only
    public int playerScore;    // Gameday mode player points
    public int enemyScore;     // Gameday opponent points

    public float roundSeconds;
    public int obstaclesSpawned;
    public int jumps;
    public int helmetsCollected;

    public int offenseDrives;
    public int defenseRoundsWon;
    public int defenseRoundsFailed;
}
