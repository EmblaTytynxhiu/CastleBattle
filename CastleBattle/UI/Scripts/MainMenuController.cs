using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Castle Battle – main menu logic.
/// START loads the game scene, HOW TO PLAY / SETTINGS open their panels, EXIT quits.
/// "Tools > Castle Battle > Build Main Menu" creates the whole menu and wires these up for you.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene loaded by START (add it in File > Build Settings / Build Profiles)")]
    public string gameSceneName = "Game";

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject howToPlayPanel;
    public GameObject settingsPanel;

    [Header("Settings widgets")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle fullscreenToggle;

    [Header("Audio (optional)")]
    public AudioSource musicSource;

    /// <summary>Read this from your game code to scale sound-effect volume (0..1).</summary>
    public static float SfxVolume { get; set; } = 1f;

    const string MusicKey = "CB_MusicVolume";
    const string SfxKey = "CB_SfxVolume";

    void Start()
    {
        float music = PlayerPrefs.GetFloat(MusicKey, 0.8f);
        float sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
        if (musicSlider) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(sfx);
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        SetMusicVolume(music);
        SetSfxVolume(sfx);
        ShowMain();
    }

    void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape) && mainPanel && !mainPanel.activeSelf) ShowMain();
#endif
    }

    // ---------------- buttons ----------------
    public void StartGame()
    {
        if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogError($"[Castle Battle] Scene '{gameSceneName}' is not in the build list. " +
                           "Add your game scene in File > Build Settings, or change 'Game Scene Name' on MainMenuController.");
    }

    public void OpenHowToPlay() { SetPanels(false, true, false); }
    public void OpenSettings()  { SetPanels(false, false, true); }
    public void ShowMain()      { SetPanels(true, false, false); }

    public void ExitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------- settings ----------------
    public void SetMusicVolume(float v)
    {
        if (musicSource) musicSource.volume = v;
        AudioManager.SetMusicVolume(v);
        PlayerPrefs.SetFloat(MusicKey, v);
    }

    public void SetSfxVolume(float v)
    {
        SfxVolume = v;
        PlayerPrefs.SetFloat(SfxKey, v);
    }

    public void SetFullscreen(bool on) { Screen.fullScreen = on; }

    void SetPanels(bool main, bool how, bool settings)
    {
        if (mainPanel) mainPanel.SetActive(main);
        if (howToPlayPanel) howToPlayPanel.SetActive(how);
        if (settingsPanel) settingsPanel.SetActive(settings);
    }
}
