using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Won, Lost }

    [Header("Rule: Keep Sick Below X")]
    [Range(0, 50)]
    [SerializeField] private int maxAllowedSick = 10;

    [Header("Win Condition")]
    [SerializeField] private bool enableTimerWin = true;
    [SerializeField] private float winAfterSeconds = 180f;

    [Header("End Game")]
    [SerializeField] private bool freezeTimeOnEnd = true;

    public GameState State { get; private set; } = GameState.Playing;
    public float Elapsed { get; private set; }
    public float TimeRemaining => enableTimerWin ? Mathf.Max(0f, winAfterSeconds - Elapsed) : 0f;
    public int MaxAllowedSick => maxAllowedSick;

    private bool ready; // becomes true once peasants exist

    private void Awake()
    {
        // Must be first
        PeasantController.ResetCounts();
        State = GameState.Playing;
        Elapsed = 0f;
        ready = false;

        if (freezeTimeOnEnd)
            Time.timeScale = 1f; // in case you stopped time last run in editor
    }

    private void Update()
    {
        if (State != GameState.Playing) return;

        // Wait until the world is actually populated
        int total = PeasantController.TotalPeasants;
        if (!ready)
        {
            if (total > 0) ready = true;
            else return;
        }

        int sick = PeasantController.SickPeasants;

        // LOSE: sick exceeds allowed
        if (sick > maxAllowedSick)
        {
            EndGame(GameState.Lost);
            return;
        }

        // WIN: survive timer while staying under threshold
        if (enableTimerWin)
        {
            Elapsed += Time.deltaTime;
            if (Elapsed >= winAfterSeconds)
            {
                EndGame(GameState.Won);
                return;
            }
        }
    }

    private void EndGame(GameState endState)
    {
        State = endState;

        if (freezeTimeOnEnd)
            Time.timeScale = 0f;

        Debug.Log($"[GameManager] {State} — Total={PeasantController.TotalPeasants}, Sick={PeasantController.SickPeasants}, Allowed={maxAllowedSick}");
    }
}
