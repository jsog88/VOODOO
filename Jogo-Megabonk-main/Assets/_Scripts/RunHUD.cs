using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RunHUD : MonoBehaviour
{
    [Header("Referencias del Jugador (Auto-detectadas si se dejan vacías)")]
    public PlayerHealth playerHealth;
    public ExperienceManager experienceManager;

    [Header("Textos del HUD")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bossGemsText;
    public TextMeshProUGUI bossAlertText;

    [Header("Panel de Victoria")]
    public GameObject victoryPanel;

    [Header("Panel de Derrota (Game Over)")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverStatsText;

    private Coroutine _alertRoutine;

    private void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (bossAlertText != null) bossAlertText.gameObject.SetActive(false);

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (experienceManager == null)
            experienceManager = FindFirstObjectByType<ExperienceManager>();

        if (playerHealth != null)
            playerHealth.OnPlayerDied += ShowGameOverScreen;

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.OnTimerUpdated += UpdateTimer;
            GameRunManager.Instance.OnBossGemCollected += UpdateGemsCounter;
            GameRunManager.Instance.OnBossSpawned += ShowBossAlert;
            GameRunManager.Instance.OnGameWon += ShowVictoryScreen;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnPlayerDied -= ShowGameOverScreen;

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.OnTimerUpdated -= UpdateTimer;
            GameRunManager.Instance.OnBossGemCollected -= UpdateGemsCounter;
            GameRunManager.Instance.OnBossSpawned -= ShowBossAlert;
            GameRunManager.Instance.OnGameWon -= ShowVictoryScreen;
        }
    }

    private void UpdateTimer(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        if (timerText != null)
            timerText.text = $"{mins:00}:{secs:00}";
    }

    private void UpdateGemsCounter(int current, int required)
    {
        if (bossGemsText != null)
            bossGemsText.text = $"Gemas de Jefe: {current} / {required}";
    }

    private void ShowBossAlert(string bossName)
    {
        if (bossAlertText == null) return;
        if (_alertRoutine != null) StopCoroutine(_alertRoutine);
        _alertRoutine = StartCoroutine(AlertCoroutine($"¡{bossName.ToUpper()} HA APARECIDO!"));
    }

    private IEnumerator AlertCoroutine(string message)
    {
        bossAlertText.text = message;
        bossAlertText.gameObject.SetActive(true);
        yield return new WaitForSeconds(4f);
        bossAlertText.gameObject.SetActive(false);
    }

    private void ShowVictoryScreen()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    private void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverStatsText != null)
        {
            float seconds = GameRunManager.Instance != null ? GameRunManager.Instance.ElapsedTime : 0f;
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int level = experienceManager != null ? experienceManager.currentLevel : 1;

            gameOverStatsText.text = $"Tiempo sobrevivido: {mins:00}:{secs:00}\nNivel alcanzado: {level}";
        }
    }

    public void OnRetryButtonClicked()
    {
        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.RestartRun();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}