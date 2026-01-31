using System.Text;
using TMPro;
using UnityEngine;

public class PeasantStatsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statsText;

    [Header("Refresh")]
    [Tooltip("How often (in seconds) to refresh counts. 0.25 is plenty.")]
    [SerializeField] private float refreshInterval = 0.25f;

    private float nextRefreshTime;
    private readonly StringBuilder sb = new StringBuilder(128);

    private void Reset()
    {
        statsText = FindFirstObjectByType<TMP_Text>();
    }

    private void Update()
    {
        if (!statsText) return;
        if (Time.time < nextRefreshTime) return;

        nextRefreshTime = Time.time + refreshInterval;

        var peasants = FindObjectsByType<PeasantController>(FindObjectsSortMode.None);

        int total = peasants.Length;
        int sick = 0;
        int contagious = 0;

        for (int i = 0; i < total; i++)
        {
            var p = peasants[i];
            if (!p) continue;

            if (p.IsSick) sick++;
            if (p.IsContagious) contagious++;
        }

        sb.Clear();
        sb.Append("Peasants: ").Append(total).Append('\n');
        sb.Append("Sick: ").Append(sick).Append('\n');
        sb.Append("Contagious: ").Append(contagious);

        statsText.text = sb.ToString();
    }
}
