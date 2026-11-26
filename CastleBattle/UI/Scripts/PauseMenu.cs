using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Castle Battle – pause menu. Esc (or the round pause button) pauses and resumes.
/// RESUME, RESTART, OPTIONS, MAIN MENU, EXIT GAME. The game is frozen with Time.timeScale = 0 while paused.
/// "Tools > Castle Battle > 6. Add Pause Menu To Scene" builds and wires everything.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Parts (filled in by the builder)")]
    public GameObject pauseRoot;      // the whole pause screen
    public GameObject pausePanel;     // title + buttons
    public GameObject optionsPanel;
    public GameObject pauseButton;    // small round button shown during play
    public Slider musicSlider, sfxSlider;
    public Toggle fullscreenToggle;

    [Header("Audio (optional)")]
    public AudioSource musicSource;

    [Tooltip("Turn off while the Victory/Defeat screen is showing.")]
    public bool canPause = true;

    public bool IsPaused { get; private set; }
    public static event System.Action<bool> OnPauseChanged;

    void Start()
    {
        float music = PlayerPrefs.GetFloat("CB_MusicVolume", 0.8f);
        float sfx = PlayerPrefs.GetFloat("CB_SfxVolume", 1f);
        if (musicSlider) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(sfx);
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        SetMusicVolume(music);
        SetSfxVolume(sfx);
        SetPaused(false);
    }

    void Update()
    {
        if (!EscPressed()) return;
        if (IsPaused && optionsPanel && optionsPanel.activeSelf) CloseOptions();
        else TogglePause();
    }

    static bool EscPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape)) return true;
#endif
        return false;
    }

    // ---------------- buttons ----------------
    public void TogglePause() { SetPaused(!IsPaused); }
    public void Pause() { SetPaused(true); }
    public void Resume() { SetPaused(false); }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OpenOptions()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName)) SceneManager.LoadScene(mainMenuSceneName);
        else Debug.LogError($"[Castle Battle] Main menu scene '{mainMenuSceneName}' is not in Build Settings.");
    }

    public void ExitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------- options ----------------
    public void SetMusicVolume(float v)
    {
        if (musicSource) musicSource.volume = v;
        AudioManager.SetMusicVolume(v);
        PlayerPrefs.SetFloat("CB_MusicVolume", v);
    }

    public void SetSfxVolume(float v)
    {
        MainMenuController.SfxVolume = v;
        PlayerPrefs.SetFloat("CB_SfxVolume", v);
    }

    public void SetFullscreen(bool on) { Screen.fullScreen = on; }

    void SetPaused(bool p)
    {
        if (p && !canPause) return;
        IsPaused = p;
        Time.timeScale = p ? 0f : 1f;
        if (pauseRoot) pauseRoot.SetActive(p);
        if (pausePanel) pausePanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (pauseButton) pauseButton.SetActive(!p);
        OnPauseChanged?.Invoke(p);
    }

    void OnDestroy()
    {
        if (IsPaused) Time.timeScale = 1f;
    }
}
