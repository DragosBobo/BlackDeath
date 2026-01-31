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
    private readonly StringBuilder sb = new StringBuilder(160);

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
        if (Time.time < nextRefreshTime) return;

        nextRefreshTime = Time.time + refreshInterval;

        // Use static counters (fast + consistent)
        int total = PeasantController.TotalPeasants;
        int sick = PeasantController.SickPeasants;
        int contagious = PeasantController.ContagiousPeasants;

        sb.Clear();
        sb.Append("Peasants: ").Append(total).Append('\n');

        if (gameManager)
        {
            sb.Append("Sick: ").Append(sick).Append(" / ").Append(gameManager.MaxAllowedSick).Append('\n');
            sb.Append("Contagious: ").Append(contagious).Append('\n');

            //sb.Append("State: ").Append(gameManager.State).Append('\n');

            // Timer info (if enabled)
            // If you didn't implement TimeRemaining, comment this out and just show Elapsed.
            sb.Append("Time: ").Append(gameManager.TimeRemaining.ToString("0")).Append("s");
        }
        else
        {
            // Fallback if GameManager is missing
            sb.Append("Sick: ").Append(sick).Append('\n');
            sb.Append("Contagious: ").Append(contagious).Append('\n');
            sb.Append("State: (no GameManager)");
        }

        statsText.text = sb.ToString();
    }
}
