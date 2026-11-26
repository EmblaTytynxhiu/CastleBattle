using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Castle Battle – Victory / Defeat screen.
/// From your game code call:   resultScreen.ShowVictory(ResultScreen.Team.Red);
///                             resultScreen.ShowDefeat(ResultScreen.Team.Blue);   // the team that WON
/// "Tools > Castle Battle > 3. Build Result Screen" creates and wires everything.
/// </summary>
public class ResultScreen : MonoBehaviour
{
    public enum Team { Red, Blue }

    [Header("Scenes")]
    [Tooltip("Leave empty to reload the scene you are in.")]
    public string playAgainSceneName = "";
    public string mainMenuSceneName = "MainMenu";

    [Header("Parts (filled in by the builder)")]
    public GameObject screenRoot;
    public Image dim, rays, title, winnerRibbon, winnerKing;
    public GameObject confetti;

    [Header("Sprites")]
    public Sprite victoryTitle, defeatTitle, raysVictory, raysDefeat;
    public Sprite ribbonRedWins, ribbonBlueWins, kingRed, kingBlue;

    [Header("Winner King cheer animation (optional)")]
    public Sprite[] cheerRed;
    public Sprite[] cheerBlue;
    public float kingFps = 10f;
    Sprite[] kingFrames;

    [Header("Look")]
    public Color victoryDim = new Color(0.16f, 0.08f, 0f, 0.2f);
    public Color defeatDim = new Color(0.04f, 0.06f, 0.13f, 0.6f);
    public float raySpeed = 10f;
    public bool hideOnStart = true;

    bool victory;
    float timer;
    float titleBaseScale = 1f;

    void Awake()
    {
        if (title) titleBaseScale = title.rectTransform.localScale.x;
        if (hideOnStart && screenRoot) screenRoot.SetActive(false);
    }

    public void ShowVictory(Team winner) { Show(true, winner); }
    public void ShowDefeat(Team winner)  { Show(false, winner); }

    public void Show(bool playerWon, Team winner)
    {
        victory = playerWon;
        timer = 0f;
        if (title) { title.sprite = playerWon ? victoryTitle : defeatTitle; title.SetNativeSize(); }
        if (rays) rays.sprite = playerWon ? raysVictory : raysDefeat;
        if (dim) dim.color = playerWon ? victoryDim : defeatDim;
        if (winnerRibbon) winnerRibbon.sprite = winner == Team.Red ? ribbonRedWins : ribbonBlueWins;
        if (winnerKing) winnerKing.sprite = winner == Team.Red ? kingRed : kingBlue;
        kingFrames = winner == Team.Red ? cheerRed : cheerBlue;
        if (confetti) confetti.SetActive(playerWon);
        if (screenRoot) screenRoot.SetActive(true);
    }

    void Update()
    {
        if (screenRoot && !screenRoot.activeSelf) return;
        timer += Time.unscaledDeltaTime;
        if (rays) rays.rectTransform.Rotate(0f, 0f, -raySpeed * Time.unscaledDeltaTime);
        if (winnerKing && kingFrames != null && kingFrames.Length > 0)
            winnerKing.sprite = kingFrames[Mathf.FloorToInt(timer * kingFps) % kingFrames.Length];
        if (title)
        {
            // pop in, then a gentle pulse (victory) or a slow sag (defeat)
            float pop = Mathf.Min(1f, timer * 3f);
            float s = Mathf.LerpUnclamped(0.3f, 1f, EaseOutBack(pop));
            s *= (victory ? 1f + 0.03f * Mathf.Sin(timer * 3f) : 1f) * titleBaseScale;
            title.rectTransform.localScale = new Vector3(s, s, 1f);
        }
        if (confetti && confetti.activeSelf)
        {
            var rt = (RectTransform)confetti.transform;
            rt.anchoredPosition = new Vector2(Mathf.Sin(timer * 0.7f) * 12f, -Mathf.Repeat(timer * 40f, 1080f));
        }
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ---------------- buttons ----------------
    public void PlayAgain()
    {
        Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(playAgainSceneName) ? SceneManager.GetActiveScene().name : playAgainSceneName;
        SceneManager.LoadScene(scene);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogError($"[Castle Battle] Main menu scene '{mainMenuSceneName}' is not in Build Settings.");
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Right-click the component header in the Inspector (Play Mode) to test each screen.
    [ContextMenu("Test: Victory – Red wins")]  void T1() { Show(true, Team.Red); }
    [ContextMenu("Test: Victory – Blue wins")] void T2() { Show(true, Team.Blue); }
    [ContextMenu("Test: Defeat – Blue wins")]  void T3() { Show(false, Team.Blue); }
    [ContextMenu("Test: Defeat – Red wins")]   void T4() { Show(false, Team.Red); }
}
