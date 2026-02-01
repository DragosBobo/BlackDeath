using System.Text;
using TMPro;
using UnityEngine;

public class PeasantStatsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statsText;

    [Header("Refs")]
    [SerializeField] private GameManager gameManager;

    [Header("Refresh")]
    [Tooltip("How often (in seconds) to refresh counts. 0.25 is plenty.")]
    [SerializeField] private float refreshInterval = 0.25f;

    private float nextRefreshTime;
    private readonly StringBuilder sb = new StringBuilder(220);

    private void Reset()
    {
        statsText = FindFirstObjectByType<TMP_Text>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Awake()
    {
        if (!statsText) statsText = FindFirstObjectByType<TMP_Text>();
        if (!gameManager) gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (!statsText) return;

        // Use unscaled time so UI continues updating even when Time.timeScale = 0
        if (Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + refreshInterval;

        // Static counters (fast)
        int totalStatic = PeasantController.TotalPeasants;
        int sickStatic = PeasantController.SickPeasants;
        int contStatic = PeasantController.ContagiousPeasants;

        // Live scan (accurate "right now" view)
        var peasants = FindObjectsByType<PeasantController>(FindObjectsSortMode.None);
        int totalScan = peasants.Length;
        int sickNow = 0;
        int contNow = 0;

        for (int i = 0; i < totalScan; i++)
        {
            var p = peasants[i];
            if (!p) continue;

            if (p.IsSick) sickNow++;
            if (p.IsContagious) contNow++;
        }

        sb.Clear();

        // Prefer scan total if you suspect duplicates; otherwise static is fine.
        sb.Append("Peasants: ").Append(totalScan).Append('\n');

        if (gameManager)
        {
            sb.Append("Sick: ").Append(sickNow).Append(" / ").Append(gameManager.MaxAllowedSick).Append('\n');
            sb.Append("Contagious (now): ").Append(contNow).Append('\n');
            sb.Append("Time: ").Append(gameManager.TimeRemaining.ToString("0")).Append("s\n");
        }
        else
        {
            sb.Append("Sick: ").Append(sickNow).Append('\n');
            sb.Append("Contagious (now): ").Append(contNow).Append('\n');
        }

        // Debug line so you can verify your static counters match reality
        sb.Append("Static (dbg) T:")
          .Append(totalStatic).Append(" S:")
          .Append(sickStatic).Append(" C:")
          .Append(contStatic);

        statsText.text = sb.ToString();
    }
}
