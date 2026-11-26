using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum GameMode { VsComputer, TwoPlayers }
public enum GridSize { Grid8x8, Grid12x12, Grid12x10, Grid16x16 }

/// <summary>
/// Runs a whole match:
///   1. BUILD  – Red builds (then Blue, or the computer builds Blue instantly). READY ends a build.
///   2. BATTLE – physics turns on, the castles settle, then the players take turns firing.
///   3. END    – when a King is knocked over or loses his health, he plays his fall animation,
///               the winner cheers, and the Victory / Defeat screen appears.
/// "Tools > Castle Battle > 8. Build Game Scene" creates and wires all of this.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("Match")]
    public GameMode mode = GameMode.VsComputer;
    public GridSize gridSize = GridSize.Grid12x10;
    [Tooltip("Gap between the two castles, in grid squares.")]
    public float castleGap = 16f;
    [Tooltip("How far in front of its castle each cannon stands, in grid squares.")]
    public float cannonDistance = 1.6f;

    [Header("References (filled in by the builder)")]
    public GameAssets assets;
    public BuildManager red, blue;
    public CannonController redCannon, blueCannon;
    public AIPlayer ai;
    public CameraController cam;
    public ResourcePanel resourcePanel;
    public GameObject buildUI, battleUI;
    public Text bannerText;
    public RectTransform bannerRoot;
    public AmmoBar ammoBar;
    [Tooltip("Angle / Power / FIRE panel under the castle. Created automatically if empty.")]
    public AimPanel aimPanel;
    public ResultScreen resultScreen;
    public PauseMenu pauseMenu;

    public int CurrentTeam { get; private set; }
    bool readyPressed;

    public static Vector2Int Dimensions(GridSize g)
    {
        switch (g)
        {
            case GridSize.Grid8x8: return new Vector2Int(8, 8);
            case GridSize.Grid12x12: return new Vector2Int(12, 12);
            case GridSize.Grid16x16: return new Vector2Int(16, 16);
            default: return new Vector2Int(12, 10);
        }
    }

    public static int PiecesAllowed(GridSize g)
    {
        switch (g)
        {
            case GridSize.Grid8x8: return 18;
            case GridSize.Grid12x12: return 34;
            case GridSize.Grid16x16: return 56;
            default: return 30;
        }
    }

    void Awake()
    {
        I = this;
        if (assets != null) GameAssets.I = assets;
        Block.DamageEnabled = false;
        MainMenuController.SfxVolume = PlayerPrefs.GetFloat("CB_SfxVolume", 1f);
    }

    void Start()
    {
        var d = Dimensions(gridSize);
        int gi = (int)gridSize;
        Sprite grid = assets != null && assets.grids != null && gi < assets.grids.Length ? assets.grids[gi] : null;
        red.transform.position = new Vector3(-castleGap * 0.5f - d.x, 0f, 0f);
        blue.transform.position = new Vector3(castleGap * 0.5f, 0f, 0f);
        red.Init(d.x, d.y, grid);
        blue.Init(d.x, d.y, grid);
        // cannons stand ON THE GROUND just in front of their castles
        redCannon.PlaceOnGround(-castleGap * 0.5f + cannonDistance);
        blueCannon.PlaceOnGround(castleGap * 0.5f - cannonDistance);
        SetUpAimPanel();
        if (assets != null) AudioManager.PlayMusic(assets.music);
        if (battleUI) battleUI.SetActive(false);
        StartCoroutine(Match());
    }

    void SetUpAimPanel()
    {
        if (aimPanel == null) aimPanel = FindCompat.First<AimPanel>();
        if (aimPanel == null)
        {
            // put it inside the battle HUD so it hides together with the ammo bar
            Transform parent = battleUI != null ? battleUI.transform : null;
            if (parent == null)
            {
                var canvas = FindCompat.First<Canvas>();
                if (canvas != null) parent = canvas.transform;
            }
            if (parent == null) return;
            aimPanel = parent.gameObject.AddComponent<AimPanel>();
        }
        if (aimPanel.titleFont == null && bannerText != null) aimPanel.titleFont = bannerText.font;
    }

    /// <summary>Hooked to the READY! button of the RESOURCES panel.</summary>
    public void PlayerReady() { readyPressed = true; }

    void Banner(string text)
    {
        if (bannerText) bannerText.text = text;
    }

    IEnumerator Match()
    {
        // ---------------- build phase ----------------
        yield return Build(red, "RED KING  ·  BUILD YOUR CASTLE");
        if (mode == GameMode.TwoPlayers)
            yield return Build(blue, "BLUE KING  ·  BUILD YOUR CASTLE");
        else
            ai.BuildCastle(blue, PiecesAllowed(gridSize));

        // ---------------- battle ----------------
        if (buildUI) buildUI.SetActive(false);
        if (battleUI) battleUI.SetActive(true);
        if (bannerRoot) bannerRoot.anchoredPosition = new Vector2(0f, -70f);      // centred for the battle
        cam.ShowBattle(red, blue);
        red.ActivatePhysics();
        blue.ActivatePhysics();
        Banner("TO BATTLE!");
        yield return new WaitForSeconds(2f);           // let the castles settle before anything can break
        Block.DamageEnabled = true;

        CurrentTeam = 0;
        while (true)
        {
            var cannon = CurrentTeam == 0 ? redCannon : blueCannon;
            var enemy = CurrentTeam == 0 ? blue : red;
            bool human = mode == GameMode.TwoPlayers || CurrentTeam == 0;
            string who = CurrentTeam == 0 ? "RED KING" : "BLUE KING";
            Banner(human ? who + "  ·  AIM WITH THE PANEL OR DRAG THE MOUSE" : who + " IS AIMING...");
            if (ammoBar) ammoBar.Bind(cannon);
            cannon.BeginTurn(human);
            if (aimPanel) aimPanel.Bind(cannon, human);
            if (!human) StartCoroutine(ai.TakeTurn(cannon, enemy.KingTarget));
            while (!cannon.HasFired) yield return null;

            yield return WaitForResolution();

            int winner;
            if (CheckWinner(out winner)) { yield return EndGame(winner); yield break; }
            CurrentTeam = 1 - CurrentTeam;
        }
    }

    IEnumerator Build(BuildManager bm, string text)
    {
        readyPressed = false;
        if (buildUI) buildUI.SetActive(true);
        if (bannerRoot) bannerRoot.anchoredPosition = new Vector2(-470f, -70f);   // over the build area (left)
        resourcePanel.ResetAll(PiecesAllowed(gridSize));
        bm.BeginBuild(resourcePanel);
        cam.ShowBuild(bm);
        Banner(text);
        while (!readyPressed) yield return null;
        bm.EndBuild();
    }

    IEnumerator WaitForResolution()
    {
        float t = 0f;
        while (Projectile.Active.Count > 0 && t < 10f) { t += Time.deltaTime; yield return null; }
        float calm = 0f;
        t = 0f;
        while (t < 5f)
        {
            t += Time.deltaTime;
            calm = red.IsCalm() && blue.IsCalm() ? calm + Time.deltaTime : 0f;
            if (calm > 0.6f) break;
            yield return null;
        }
        yield return new WaitForSeconds(0.3f);
    }

    bool CheckWinner(out int winner)
    {
        bool redDown = red.King == null || red.King.IsDefeated;
        bool blueDown = blue.King == null || blue.King.IsDefeated;
        winner = -1;
        if (!redDown && !blueDown) return false;
        winner = redDown && blueDown ? CurrentTeam : (redDown ? 1 : 0);
        return true;
    }

    IEnumerator EndGame(int winner)
    {
        if (pauseMenu) pauseMenu.canPause = false;
        if (aimPanel) aimPanel.Hide();
        var winKing = winner == 0 ? red.King : blue.King;
        var loseKing = winner == 0 ? blue.King : red.King;
        if (loseKing) loseKing.PlayLoss();
        if (winKing) winKing.PlayWin();
        Banner(winner == 0 ? "THE RED KING WINS!" : "THE BLUE KING WINS!");
        yield return new WaitForSeconds(2.5f);
        bool playerWon = mode == GameMode.TwoPlayers || winner == 0;
        if (assets != null) AudioManager.Play(playerWon ? assets.victory : assets.defeat, 1f, 0f);
        if (battleUI) battleUI.SetActive(false);
        if (resultScreen) resultScreen.Show(playerWon, winner == 0 ? ResultScreen.Team.Red : ResultScreen.Team.Blue);
    }
}
