using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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
    [SerializeField] private string winSceneName = "WinScene";
    [SerializeField] private string loseSceneName = "LoseScene";
    [Tooltip("Delay in seconds before loading Win/Lose scene (uses unscaled time).")]
    [SerializeField] private float loadEndSceneDelay = 1.5f;

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

        // ESC = give up -> defeat (check first so it works even before peasants spawn)
        // Use new Input System: old Input is disabled when activeInputHandler is "Input System Package"
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndGame(GameState.Lost);
            return;
        }

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

        string sceneToLoad = endState == GameState.Won ? winSceneName : loseSceneName;
        if (!string.IsNullOrEmpty(sceneToLoad))
            StartCoroutine(LoadEndSceneAfterDelay(sceneToLoad, loadEndSceneDelay));
    }

    private IEnumerator LoadEndSceneAfterDelay(string sceneName, float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
