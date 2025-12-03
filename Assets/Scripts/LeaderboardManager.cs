using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown filterDropdown;
    public Transform contentParent;
    public GameObject rowPrefab;

    private string leaderboardPath;

    private class Entry
    {
        public string name;
        public int score;
        public string mode;
        public string difficulty;
    }

    private void Start()
    {
        leaderboardPath = Path.Combine(Application.dataPath, "Logs", "leaderboard.csv");

        if (filterDropdown != null)
            filterDropdown.onValueChanged.AddListener(delegate { Refresh(); });

        Refresh();
    }

    public void Refresh()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        List<Entry> rows = LoadData();
        rows = ApplyFilter(rows);

        rows.Sort((a, b) => b.score.CompareTo(a.score));

        int rank = 1;
        foreach (var r in rows)
        {
            GameObject row = Instantiate(rowPrefab, contentParent);
            row.transform.Find("RankText").GetComponent<TextMeshProUGUI>().text = rank.ToString();
            row.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = r.name;
            row.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>().text = r.score.ToString();

            rank++;
        }
    }

    private List<Entry> LoadData()
    {
        List<Entry> entries = new List<Entry>();

        if (!File.Exists(leaderboardPath))
            return entries;

        var lines = File.ReadAllLines(leaderboardPath);

        // Skip header
        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = SplitCSV(lines[i]);
            if (parts.Length < 4) continue;

            entries.Add(new Entry
            {
                name = parts[0],
                score = int.TryParse(parts[1], out int s) ? s : 0,
                mode = parts[2],
                difficulty = parts[3]
            });
        }

        return entries;
    }

    private List<Entry> ApplyFilter(List<Entry> data)
    {
        string selected = filterDropdown.options[filterDropdown.value].text;

        return data.FindAll(e =>
            (selected == "Iowa Mode - Easy" && e.mode == "Iowa" && e.difficulty == "Easy") ||
            (selected == "Iowa Mode - Normal" && e.mode == "Iowa" && e.difficulty == "Normal") ||
            (selected == "Iowa Mode - Hard" && e.mode == "Iowa" && e.difficulty == "Hard") ||
            (selected == "GameDay Mode - College" && e.mode == "GameDay" && e.difficulty == "College") ||
            (selected == "GameDay Mode - Pro" && e.mode == "GameDay" && e.difficulty == "Pro")
        );
    }

    private string[] SplitCSV(string line)
    {
        return line.Split(',');
    }
}
