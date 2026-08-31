using UnityEngine;
using UnityEngine.SceneManagement;

public enum RoguelikeRunResult
{
    None,
    Victory,
    Defeat
}

/// <summary>
/// Owns the lifecycle of a single card-roguelike run. Map navigation and an
/// individual encounter stay with their existing controllers; this component
/// only transitions between a fresh run, a terminal result, and the menu.
/// </summary>
public class RoguelikeRunController : MonoBehaviour
{
    public GameManager gameManager;
    public RoguelikeResultPanel resultPanel;
    public string mainMenuSceneName = "start";

    public RoguelikeRunResult CurrentResult { get; private set; }
    public string LastRequestedSceneName { get; private set; }
    public bool IsTerminal => CurrentResult != RoguelikeRunResult.None;

    public bool StartNewRun()
    {
        GameManager manager = ResolveGameManager();
        if (manager == null)
            return false;

        ResetRunState();
        CurrentResult = RoguelikeRunResult.None;
        LastRequestedSceneName = null;
        EnsureResultPanel().Hide();
        return manager.ResetMapForNewRun();
    }

    public bool CompleteRun()
    {
        return TrySetResult(RoguelikeRunResult.Victory);
    }

    public bool FailRun()
    {
        return TrySetResult(RoguelikeRunResult.Defeat);
    }

    public void ReturnToMainMenu()
    {
        ResetRunState();
        LastRequestedSceneName = mainMenuSceneName;

        if (Application.isPlaying)
            SceneManager.LoadScene(mainMenuSceneName);
    }

    private bool TrySetResult(RoguelikeRunResult result)
    {
        if (IsTerminal)
            return false;

        GameManager manager = ResolveGameManager();
        if (manager != null)
        {
            manager.contentManager?.ClearCurrentContent();
            if (manager.mapGenerator != null && manager.mapGenerator.mapContainer != null)
                manager.mapGenerator.mapContainer.gameObject.SetActive(false);
        }

        CurrentResult = result;
        EnsureResultPanel().Show(result, () => { StartNewRun(); }, ReturnToMainMenu);
        return true;
    }

    private GameManager ResolveGameManager()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        return gameManager;
    }

    private RoguelikeResultPanel EnsureResultPanel()
    {
        if (resultPanel != null)
            return resultPanel;

        resultPanel = GetComponentInChildren<RoguelikeResultPanel>(true);
        if (resultPanel != null)
            return resultPanel;

        GameObject panelObject = new GameObject("RoguelikeRunResultPanel");
        panelObject.transform.SetParent(transform, false);
        resultPanel = panelObject.AddComponent<RoguelikeResultPanel>();
        return resultPanel;
    }

    private void ResetRunState()
    {
        PlayerStats.EnsureInstance();
        DeckManager.EnsureInstance();
        PlayerStats.Instance.ResetForNewRun();
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.ResetForNewRun();
        DeckManager.Instance.ResetForNewRun();
        CollectibleManager.ResetForNewRun();

        GameManager manager = ResolveGameManager();
        if (manager != null && manager.contentManager != null)
            manager.contentManager.ClearCurrentContent();
    }
}
