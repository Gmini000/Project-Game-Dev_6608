
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameOverPanel;
    [SerializeField] private RunEndPanel runEndPanel;
    private bool showingResult;

    void Start()
    {
        // Ensure normal game speed
        Time.timeScale = 1f;

        // Hide Game Over when game starts
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public void ShowGameOver(PlayerHealth player) => ShowResult(player, false);

    public void ShowVictory(PlayerHealth player) => ShowResult(player, true);

    private void ShowResult(PlayerHealth player, bool victorious)
    {
        if (showingResult)
            return;

        showingResult = true;
        Time.timeScale = 0f;
        if (runEndPanel != null)
            Instantiate(runEndPanel).Show(player, victorious, RestartGame);
        else if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        // Restore time before loading
        PlayerHealth.ResetStageTransfer();
        Time.timeScale = 1f;

        SceneManager.LoadScene(MainMenu.FirstStage);
    }
}
