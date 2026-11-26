#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One-click menu builder.
///   Tools > Castle Battle > 1. Set Up Sprite Import Settings
///   Tools > Castle Battle > 2. Build Main Menu In Open Scene
/// Keep the CastleBattleUI folder structure (Sprites/Buttons, Sprites/Panels, ...) anywhere under Assets.
/// </summary>
public static partial class CastleBattleMenuBuilder
{
    static string spritesDir;
    static Font titleFont, bodyFont;

    // ------------------------------------------------------------------ import settings
    [MenuItem("Tools/Castle Battle/1. Set Up Sprite Import Settings")]
    public static void SetupImports()
    {
        if (!Locate()) return;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { spritesDir });
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool isKing = path.Replace('\\', '/').Contains("/King/");
                if (ti.textureType == TextureImporterType.Sprite && !ti.mipmapEnabled && ti.isReadable &&
                    Mathf.Approximately(ti.spritePixelsPerUnit, isKing ? 320 : 100)) continue;   // already set up
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.isReadable = true;                       // needed for precise (alpha) button hit areas
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                string n = Path.GetFileNameWithoutExtension(path);
                if (n == "Slider_Track" || n == "Slider_Fill") ti.spriteBorder = new Vector4(22, 0, 22, 0);
                if (path.Replace('\\', '/').Contains("/King/"))
                {
                    ti.spritePixelsPerUnit = 320;                  // King is ~2 grid blocks tall
                    var set = new TextureImporterSettings();
                    ti.ReadTextureSettings(set);
                    if (n.Contains("_Fall_"))
                    {
                        set.spriteAlignment = (int)SpriteAlignment.Custom;
                        set.spritePivot = new Vector2(0.77f, 0.057f);   // his feet
                    }
                    else
                    {
                        set.spriteAlignment = (int)SpriteAlignment.Custom;
                        set.spritePivot = new Vector2(0.475f, 0.057f);  // between his boots
                    }
                    ti.SetTextureSettings(set);
                }
                ti.SaveAndReimport();
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
        Debug.Log($"[Castle Battle] Import settings applied to {guids.Length} sprites.");
    }

    // ------------------------------------------------------------------ build
    [MenuItem("Tools/Castle Battle/2. Build Main Menu In Open Scene")]
    public static void Build()
    {
        if (!Locate()) return;
        SetupImports();
        titleFont = LoadFont("Cardo-Bold");
        bodyFont = LoadFont("CrimsonText-SemiBold");

        var canvasGO = new GameObject("CastleBattle_MainMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Castle Battle Menu");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        EnsureEventSystem();
        Transform root = canvasGO.transform;

        // ---------- background ----------
        var bgImg = Img("Background", root, "BG_MainMenu");
        Stretch(bgImg);
        var fitter = bgImg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;   // always fills the screen, no stretching
        fitter.aspectRatio = 1920f / 1080f;

        var ctrl = canvasGO.AddComponent<MainMenuController>();

        // ---------- main panel (title + 4 buttons) ----------
        var main = Group("MainPanel", root);
        var title = Img("Title", main, "Title_CastleBattle");
        title.rectTransform.sizeDelta = new Vector2(620, 273);
        title.rectTransform.anchoredPosition = new Vector2(0, 380);
        Place(Img("Panel", main, "Panel_Main"), 0, -150);
        Place(Img("Ribbon", main, "Ribbon_MainMenu"), 0, 168);
        var bStart = MakeButton("StartButton", main, "Button_Start", 0, 35);
        var bHow = MakeButton("HowToPlayButton", main, "Button_HowToPlay", 0, -85);
        var bSet = MakeButton("SettingsButton", main, "Button_Settings", 0, -195);
        var bExit = MakeButton("ExitButton", main, "Button_Exit", 0, -305);
        Place(Img("Divider", main, "Divider"), 0, -405);
        UnityEventTools.AddPersistentListener(bStart.onClick, ctrl.StartGame);
        UnityEventTools.AddPersistentListener(bHow.onClick, ctrl.OpenHowToPlay);
        UnityEventTools.AddPersistentListener(bSet.onClick, ctrl.OpenSettings);
        UnityEventTools.AddPersistentListener(bExit.onClick, ctrl.ExitGame);

        // ---------- how to play ----------
        var how = Group("HowToPlayPanel", root);
        Dim(how);
        Place(Img("Panel", how, "Panel_Wide"), 0, -60);
        Place(Img("Ribbon", how, "Ribbon_HowToPlay"), 0, 280);
        var body = Label("Text", how, bodyFont, 34, new Vector2(800, 400), new Vector2(0, -30), TextAnchor.MiddleCenter);
        body.text =
            "<b>BUILD</b> your castle inside the 8 x 8 grid using wood, stone and metal.\n\n" +
            "<b>PROTECT</b> your King – if he falls, you lose!\n\n" +
            "<b>AIM</b> your cannon by dragging, then let go to <b>FIRE</b>.\n\n" +
            "<b>WIN</b> by knocking down the enemy King first.";
        body.lineSpacing = 1.05f;
        var howBack = MakeButton("BackButton", how, "Button_Back", 0, -330);
        var howClose = MakeButton("CloseButton", how, "Button_Close", 440, 250);
        UnityEventTools.AddPersistentListener(howBack.onClick, ctrl.ShowMain);
        UnityEventTools.AddPersistentListener(howClose.onClick, ctrl.ShowMain);

        // ---------- settings ----------
        var set = Group("SettingsPanel", root);
        Dim(set);
        Place(Img("Panel", set, "Panel_Main"), 0, -60);
        Place(Img("Ribbon", set, "Ribbon_Settings"), 0, 280);
        Label("MusicLabel", set, titleFont, 36, new Vector2(420, 50), new Vector2(0, 175), TextAnchor.MiddleCenter).text = "MUSIC";
        var music = MakeSlider("MusicSlider", set, new Vector2(0, 120), 0.8f);
        Label("SfxLabel", set, titleFont, 36, new Vector2(420, 50), new Vector2(0, 40), TextAnchor.MiddleCenter).text = "SOUND EFFECTS";
        var sfx = MakeSlider("SfxSlider", set, new Vector2(0, -15), 1f);
        Label("FullscreenLabel", set, titleFont, 36, new Vector2(300, 60), new Vector2(-50, -125), TextAnchor.MiddleRight).text = "FULLSCREEN";
        var fs = MakeToggle("FullscreenToggle", set, new Vector2(150, -125));
        var setBack = MakeButton("BackButton", set, "Button_Back", 0, -300);
        var setClose = MakeButton("CloseButton", set, "Button_Close", 290, 250);
        UnityEventTools.AddPersistentListener<float>(music.onValueChanged, ctrl.SetMusicVolume);
        UnityEventTools.AddPersistentListener<float>(sfx.onValueChanged, ctrl.SetSfxVolume);
        UnityEventTools.AddPersistentListener<bool>(fs.onValueChanged, ctrl.SetFullscreen);
        UnityEventTools.AddPersistentListener(setBack.onClick, ctrl.ShowMain);
        UnityEventTools.AddPersistentListener(setClose.onClick, ctrl.ShowMain);

        ctrl.mainPanel = main.gameObject;
        ctrl.howToPlayPanel = how.gameObject;
        ctrl.settingsPanel = set.gameObject;
        ctrl.musicSlider = music;
        ctrl.sfxSlider = sfx;
        ctrl.fullscreenToggle = fs;
        how.gameObject.SetActive(false);
        set.gameObject.SetActive(false);

        Selection.activeGameObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Debug.Log("[Castle Battle] Main menu built. Press Play to try it. Set 'Game Scene Name' on MainMenuController to your game scene.");
    }

    // ------------------------------------------------------------------ result screen
    [MenuItem("Tools/Castle Battle/3. Build Result Screen (Victory - Defeat) In Open Scene")]
    public static void BuildResults() { CreateResults(); }

    static ResultScreen CreateResults()
    {
        if (!Locate()) return null;
        SetupImports();

        var canvasGO = new GameObject("CastleBattle_ResultScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Castle Battle Result Screen");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;                       // draws above the game UI
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        EnsureEventSystem();

        var rs = canvasGO.AddComponent<ResultScreen>();
        var screen = Group("Screen", canvasGO.transform);

        var bg = Img("Background", screen, "BG_Battlefield");
        Stretch(bg);
        var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 1920f / 1080f;

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        dim.transform.SetParent(screen, false);
        Stretch(dim);
        dim.raycastTarget = true;                        // blocks clicks to the game underneath

        var rays = Place(Img("Rays", screen, "Rays_Victory"), 0, 20);
        var pedestal = Place(Img("Pedestal", screen, "Pedestal"), 0, -250);
        var king = Img("WinnerKing", screen, "King_Red_Winner");
        king.rectTransform.sizeDelta = new Vector2(380, 500);
        king.rectTransform.anchoredPosition = new Vector2(0, 10);
        king.preserveAspect = true;

        var confetti = Group("Confetti", screen);
        var c1 = Img("ConfettiA", confetti, "Confetti_Overlay"); c1.rectTransform.anchoredPosition = Vector2.zero;
        var c2 = Img("ConfettiB", confetti, "Confetti_Overlay"); c2.rectTransform.anchoredPosition = new Vector2(0, 1080);

        var title = Place(Img("Title", screen, "Title_Victory"), 0, 390);
        title.rectTransform.localScale = Vector3.one * 0.73f;
        var ribbon = Place(Img("WinnerRibbon", screen, "Ribbon_Winner_Red"), 0, 235);

        Place(Img("ButtonPanel", screen, "Panel_Buttons"), 0, -425);
        var bPlay = MakeButton("PlayAgainButton", screen, "Button_PlayAgain", -470, -425);
        var bMenu = MakeButton("MainMenuButton", screen, "Button_MainMenu", 0, -425);
        var bExit = MakeButton("ExitGameButton", screen, "Button_ExitGame", 470, -425);
        var nav = bPlay.navigation; nav.mode = Navigation.Mode.Horizontal;
        bPlay.navigation = nav; bMenu.navigation = nav; bExit.navigation = nav;
        UnityEventTools.AddPersistentListener(bPlay.onClick, rs.PlayAgain);
        UnityEventTools.AddPersistentListener(bMenu.onClick, rs.MainMenu);
        UnityEventTools.AddPersistentListener(bExit.onClick, rs.ExitGame);

        rs.screenRoot = screen.gameObject;
        rs.dim = dim; rs.rays = rays; rs.title = title; rs.winnerRibbon = ribbon; rs.winnerKing = king;
        rs.confetti = confetti.gameObject;
        rs.victoryTitle = S("Title_Victory"); rs.defeatTitle = S("Title_Defeat");
        rs.raysVictory = S("Rays_Victory"); rs.raysDefeat = S("Rays_Defeat");
        rs.ribbonRedWins = S("Ribbon_Winner_Red"); rs.ribbonBlueWins = S("Ribbon_Winner_Blue");
        rs.kingRed = S("King_Red_Winner"); rs.kingBlue = S("King_Blue_Winner");
        rs.cheerRed = Frames("Red", "Cheer"); rs.cheerBlue = Frames("Blue", "Cheer");
        dim.color = rs.victoryDim;

        Selection.activeGameObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Debug.Log("[Castle Battle] Result screen built. It hides itself on Play; call ShowVictory/ShowDefeat from your game, " +
                  "or right-click the ResultScreen component in Play Mode and pick a Test option.");
        return rs;
    }

    // ------------------------------------------------------------------ animated kings
    [MenuItem("Tools/Castle Battle/4. Add Animated Kings To Scene")]
    public static void AddKings()
    {
        if (!Locate()) return;
        SetupImports();
        var red = MakeKing("Red", new Vector3(-6f, 0f, 0f), false);
        var blue = MakeKing("Blue", new Vector3(6f, 0f, 0f), true);
        Selection.objects = new Object[] { red, blue };
        EditorSceneManager.MarkSceneDirty(red.scene);
        Debug.Log("[Castle Battle] Added King_Red and King_Blue. Their feet are at the object's position. " +
                  "Call GetComponent<KingAnimator>().Play(...) to switch animations.");
    }

    static GameObject MakeKing(string team, Vector3 pos, bool faceLeft)
    {
        var go = new GameObject("King_" + team, typeof(SpriteRenderer), typeof(KingAnimator));
        Undo.RegisterCreatedObjectUndo(go, "Add King");
        go.transform.position = pos;
        var k = go.GetComponent<KingAnimator>();
        k.idle = Frames(team, "Idle"); k.walk = Frames(team, "Walk");
        k.cheer = Frames(team, "Cheer"); k.fall = Frames(team, "Fall");
        k.faceLeft = faceLeft;
        var sr = go.GetComponent<SpriteRenderer>();
        if (k.idle.Length > 0) sr.sprite = k.idle[0];
        sr.flipX = faceLeft;
        sr.sortingOrder = 10;
        return go;
    }

    static Sprite[] Frames(string team, string anim)
    {
        string dir = spritesDir + "/King/" + team + "/" + anim;
        if (!AssetDatabase.IsValidFolder(dir)) { Debug.LogWarning("[Castle Battle] Missing folder " + dir); return new Sprite[0]; }
        var list = new System.Collections.Generic.List<string>();
        foreach (var g in AssetDatabase.FindAssets("t:Sprite", new[] { dir })) list.Add(AssetDatabase.GUIDToAssetPath(g));
        list.Sort(System.StringComparer.Ordinal);
        var res = new Sprite[list.Count];
        for (int i = 0; i < list.Count; i++) res[i] = AssetDatabase.LoadAssetAtPath<Sprite>(list[i]);
        return res;
    }

    // ------------------------------------------------------------------ castle building screen
    [MenuItem("Tools/Castle Battle/5. Build Castle Building Screen/8 x 8")]   static void Build8x8()   { BuildBuildScreen("8x8", 8, 8, 88); }
    [MenuItem("Tools/Castle Battle/5. Build Castle Building Screen/12 x 12")] static void Build12x12() { BuildBuildScreen("12x12", 12, 12, 58); }
    [MenuItem("Tools/Castle Battle/5. Build Castle Building Screen/12 x 10")] static void Build12x10() { BuildBuildScreen("12x10", 12, 10, 58); }
    [MenuItem("Tools/Castle Battle/5. Build Castle Building Screen/16 x 16")] static void Build16x16() { BuildBuildScreen("16x16", 16, 16, 44); }

    static Vector2 Scr(float x, float y) { return new Vector2(x - 960f, 540f - y); }   // screen px (y down) -> canvas

    static void BuildBuildScreen(string grid, int cols, int rows, int cell)
    {
        if (!Locate()) return;
        SetupImports();
        titleFont = LoadFont("Cardo-Bold");
        bodyFont = LoadFont("CrimsonText-SemiBold");

        var canvasGO = new GameObject("CastleBattle_BuildScreen_" + grid, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Castle Building Screen");
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        EnsureEventSystem();
        Transform root = canvasGO.transform;

        var bg = Img("Background", root, "BG_Build");
        Stretch(bg);
        var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 1920f / 1080f;

        // ----- left: build area -----
        int gw = cols * cell, gh = rows * cell;
        var gridImg = Img("BuildGrid", root, "BuildGrid_" + grid);
        gridImg.rectTransform.sizeDelta = new Vector2(gw, gh);
        gridImg.rectTransform.anchoredPosition = Scr(450, 590);
        gridImg.raycastTarget = true;                                   // receives clicks / drops for building
        Img("GridFrame", root, "Frame_Grid_" + grid).rectTransform.anchoredPosition = Scr(450, 590);
        Img("GridRibbon", root, "Ribbon_BuildArea_" + grid).rectTransform.anchoredPosition = Scr(450, 590 - (gh + 48) / 2f - 2);

        BuildResourcesPanel(root);

        Selection.activeGameObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Debug.Log("[Castle Battle] Building screen (" + grid + ") built. Hook ResourcePanel.onPieceSelected / onReady to your building code.");
    }

    static Sprite[] IconFrames(string cat)
    {
        string dir = spritesDir + "/Build/Icons/" + cat;
        if (!AssetDatabase.IsValidFolder(dir)) return new Sprite[0];
        var list = new System.Collections.Generic.List<string>();
        foreach (var g in AssetDatabase.FindAssets("t:Sprite", new[] { dir })) list.Add(AssetDatabase.GUIDToAssetPath(g));
        list.Sort(System.StringComparer.Ordinal);
        var res = new Sprite[list.Count];
        for (int i = 0; i < list.Count; i++) res[i] = AssetDatabase.LoadAssetAtPath<Sprite>(list[i]);
        return res;
    }

    static string PrettyName(string file)
    {
        // "02_Wood_Block_3x1" -> "WOOD BLOCK 3 x 1"
        int us = file.IndexOf('_');
        string s = us >= 0 ? file.Substring(us + 1) : file;
        s = s.Replace('_', ' ');
        var m = System.Text.RegularExpressions.Regex.Match(s, @"(\d+)x(\d+)");
        if (m.Success) s = s.Replace(m.Value, m.Groups[1].Value + " x " + m.Groups[2].Value);
        return s.ToUpper();
    }

    // ------------------------------------------------------------------ pause menu
    [MenuItem("Tools/Castle Battle/6. Add Pause Menu To Scene")]
    public static void BuildPause() { CreatePause(); }

    static PauseMenu CreatePause()
    {
        if (!Locate()) return null;
        SetupImports();
        titleFont = LoadFont("Cardo-Bold");
        bodyFont = LoadFont("CrimsonText-SemiBold");

        var canvasGO = new GameObject("CastleBattle_PauseMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Add Pause Menu");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;                         // above the game, below the result screen (100)
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        EnsureEventSystem();
        var pm = canvasGO.AddComponent<PauseMenu>();

        // small pause button in the top-right corner (visible while playing)
        var pb = MakeButton("PauseButton", canvasGO.transform, "Button_PauseIcon", 0, 0);
        var pbr = pb.GetComponent<RectTransform>();
        pbr.anchorMin = pbr.anchorMax = new Vector2(1, 1);
        pbr.anchoredPosition = new Vector2(-80, -80);
        UnityEventTools.AddPersistentListener(pb.onClick, pm.Pause);

        var root = Group("PauseScreen", canvasGO.transform);
        var bg = Img("Background", root, "BG_Build");
        Stretch(bg);
        var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 1920f / 1080f;
        bg.raycastTarget = true;
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        dim.transform.SetParent(root, false);
        Stretch(dim);
        dim.color = new Color(0.02f, 0.04f, 0.11f, 0.58f);
        dim.raycastTarget = true;
        // Tip: to show the frozen game instead of the picture, delete "Background" and raise Dim's alpha.

        var main = Group("PausePanel", root);
        var title = Img("Title", main, "Title_Paused");
        title.rectTransform.sizeDelta = new Vector2(980, 280);
        title.rectTransform.anchoredPosition = Scr(960, 135);
        Img("Panel", main, "Panel_Pause").rectTransform.anchoredPosition = Scr(960, 645);
        string[] names = { "Resume", "Restart", "Options", "MainMenu", "ExitGame" };
        float[] ys = { 365, 480, 595, 710, 825 };
        var buttons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            buttons[i] = MakeButton(names[i] + "Button", main, "Button_Pause_" + names[i], 0, 0);
            buttons[i].GetComponent<RectTransform>().anchoredPosition = Scr(960, ys[i]);
        }
        UnityEventTools.AddPersistentListener(buttons[0].onClick, pm.Resume);
        UnityEventTools.AddPersistentListener(buttons[1].onClick, pm.Restart);
        UnityEventTools.AddPersistentListener(buttons[2].onClick, pm.OpenOptions);
        UnityEventTools.AddPersistentListener(buttons[3].onClick, pm.MainMenu);
        UnityEventTools.AddPersistentListener(buttons[4].onClick, pm.ExitGame);
        Img("EscHint", main, "Hint_EscToResume").rectTransform.anchoredPosition = Scr(960, 935);

        var opt = Group("OptionsPanel", root);
        Place(Img("Panel", opt, "Panel_Options"), 0, -60);
        Place(Img("Ribbon", opt, "Ribbon_Options"), 0, 280);
        Label("MusicLabel", opt, titleFont, 36, new Vector2(420, 50), new Vector2(0, 175), TextAnchor.MiddleCenter).text = "MUSIC";
        var music = MakeSlider("MusicSlider", opt, new Vector2(0, 120), 0.8f);
        Label("SfxLabel", opt, titleFont, 36, new Vector2(420, 50), new Vector2(0, 40), TextAnchor.MiddleCenter).text = "SOUND EFFECTS";
        var sfx = MakeSlider("SfxSlider", opt, new Vector2(0, -15), 1f);
        Label("FullscreenLabel", opt, titleFont, 36, new Vector2(300, 60), new Vector2(-50, -125), TextAnchor.MiddleRight).text = "FULLSCREEN";
        var fs = MakeToggle("FullscreenToggle", opt, new Vector2(150, -125));
        var back = MakeButton("BackButton", opt, "Button_Back", 0, -300);
        UnityEventTools.AddPersistentListener<float>(music.onValueChanged, pm.SetMusicVolume);
        UnityEventTools.AddPersistentListener<float>(sfx.onValueChanged, pm.SetSfxVolume);
        UnityEventTools.AddPersistentListener<bool>(fs.onValueChanged, pm.SetFullscreen);
        UnityEventTools.AddPersistentListener(back.onClick, pm.CloseOptions);

        pm.pauseRoot = root.gameObject; pm.pausePanel = main.gameObject; pm.optionsPanel = opt.gameObject;
        pm.pauseButton = pb.gameObject;
        pm.musicSlider = music; pm.sfxSlider = sfx; pm.fullscreenToggle = fs;
        opt.gameObject.SetActive(false);
        root.gameObject.SetActive(false);

        Selection.activeGameObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Debug.Log("[Castle Battle] Pause menu added. Press Esc (or the round pause button) in Play Mode.");
        return pm;
    }

    static ResourcePanel BuildResourcesPanel(Transform root)
    {
        // ----- right: resources -----
        var panelRoot = Group("ResourcesPanel", root);
        var rp = panelRoot.gameObject.AddComponent<ResourcePanel>();
        Img("Panel", panelRoot, "Panel_Resources").rectTransform.anchoredPosition = Scr(1440, 548);
        Img("Ribbon", panelRoot, "Ribbon_Resources").rectTransform.anchoredPosition = Scr(1440, 100);
        Img("CounterBadge", panelRoot, "Badge_Counter").rectTransform.anchoredPosition = Scr(1790, 112);
        var counter = Label("CounterText", panelRoot, titleFont, 46, new Vector2(110, 70), Scr(1788, 108), TextAnchor.MiddleCenter);
        counter.color = new Color32(255, 246, 222, 255);
        counter.GetComponent<Shadow>().effectColor = new Color(0.05f, 0.1f, 0.3f, 1f);

        string[] cats = { "Wood", "Stone", "Metal", "Special" };
        string[] desc = { "Light and cheap · burns easily", "Heavy and strong · fireproof", "The toughest · bounces shots away", "Glass, ice, defenses and helpers" };
        float[] tabX = { -247, -82, 82, 247 };
        rp.tabs = new Image[4]; rp.tabActive = new Sprite[4]; rp.tabInactive = new Sprite[4];
        rp.categories = new ResourcePanel.Category[4];
        int[] defaultCounts = { 6, 4, 3, 4, 2, 4, 2, 2, 2, 3, 4, 2 };
        for (int c = 0; c < 4; c++)
        {
            rp.tabActive[c] = S("Tab_" + cats[c] + "_Active");
            rp.tabInactive[c] = S("Tab_" + cats[c] + "_Inactive");
            var tab = Img("Tab_" + cats[c], panelRoot, "Tab_" + cats[c] + (c == 0 ? "_Active" : "_Inactive"));
            tab.rectTransform.anchoredPosition = Scr(1440 + tabX[c], 215);
            tab.raycastTarget = true;
            var tb = tab.gameObject.AddComponent<Button>();
            tb.targetGraphic = tab;
            UnityEventTools.AddIntPersistentListener(tb.onClick, rp.SelectTab, c);
            rp.tabs[c] = tab;

            var icons = IconFrames(cats[c]);
            var names = new string[icons.Length];
            var counts = new int[icons.Length];
            for (int i = 0; i < icons.Length; i++)
            {
                names[i] = PrettyName(icons[i] ? icons[i].name : "");
                counts[i] = defaultCounts[i % defaultCounts.Length];
            }
            rp.categories[c] = new ResourcePanel.Category { name = cats[c], description = desc[c], icons = icons, pieceNames = names, counts = counts };
        }

        float[] slotX = { -232, -77, 77, 232 };
        float[] slotY = { 345, 505, 665 };
        rp.slots = new Image[12]; rp.slotIcons = new Image[12]; rp.slotBadges = new GameObject[12]; rp.slotCounts = new Text[12];
        rp.slotNormal = S("Slot_Normal"); rp.slotSelected = S("Slot_Selected"); rp.slotLocked = S("Slot_Locked");
        for (int i = 0; i < 12; i++)
        {
            var slot = Img("Slot_" + i, panelRoot, "Slot_Normal");
            slot.rectTransform.anchoredPosition = Scr(1440 + slotX[i % 4], slotY[i / 4]);
            slot.raycastTarget = true;
            var b = slot.gameObject.AddComponent<Button>();
            b.targetGraphic = slot;
            var cb = b.colors; cb.highlightedColor = new Color(1f, 0.95f, 0.75f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = cb;
            slot.gameObject.AddComponent<MenuButtonFX>().hoverScale = 1.06f;
            UnityEventTools.AddIntPersistentListener(b.onClick, rp.SelectSlot, i);
            var ic = Img("Icon", slot.transform, "Slot_Normal");
            ic.sprite = null; ic.preserveAspect = true;
            ic.rectTransform.anchoredPosition = new Vector2(0, 4);
            var badge = Img("CountBadge", slot.transform, "Badge_Count");
            badge.rectTransform.anchoredPosition = new Vector2(50, 50);
            var ct = Label("Count", badge.transform, titleFont, 22, new Vector2(52, 40), new Vector2(0, 1), TextAnchor.MiddleCenter);
            ct.color = new Color32(255, 246, 222, 255);
            ct.GetComponent<Shadow>().effectColor = new Color(0.2f, 0.03f, 0.03f, 1f);
            rp.slots[i] = slot; rp.slotIcons[i] = ic; rp.slotBadges[i] = badge.gameObject; rp.slotCounts[i] = ct;
        }

        rp.infoTitle = Label("InfoTitle", panelRoot, titleFont, 38, new Vector2(640, 50), Scr(1440, 775), TextAnchor.MiddleCenter);
        rp.infoText = Label("InfoText", panelRoot, bodyFont, 28, new Vector2(640, 40), Scr(1440, 818), TextAnchor.MiddleCenter);
        rp.infoText.color = new Color32(110, 80, 50, 255);
        rp.counterText = counter;

        var ready = MakeButton("ReadyButton", panelRoot, "Button_Ready", 0, 0);
        ready.GetComponent<RectTransform>().anchoredPosition = Scr(1440, 905);
        UnityEventTools.AddPersistentListener(ready.onClick, rp.Ready);

        return rp;
    }

    // ------------------------------------------------------------------ helpers
    static bool Locate()
    {
        var g = AssetDatabase.FindAssets("Button_Start_Normal t:Texture2D");
        if (g.Length == 0)
        {
            EditorUtility.DisplayDialog("Castle Battle", "Could not find the CastleBattleUI sprites. Copy the whole CastleBattleUI folder into your Assets folder first.", "OK");
            return false;
        }
        string buttons = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(g[0])).Replace('\\', '/');
        spritesDir = Path.GetDirectoryName(buttons).Replace('\\', '/');
        return true;
    }

    static Sprite S(string name)
    {
        foreach (var g in AssetDatabase.FindAssets(name + " t:Sprite", new[] { spritesDir }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }
        Debug.LogWarning("[Castle Battle] Sprite not found: " + name);
        return null;
    }

    static AudioClip FindClip(string name)
    {
        foreach (var g in AssetDatabase.FindAssets(name + " t:AudioClip"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<AudioClip>(p);
        }
        return null;
    }

    static Font LoadFont(string name)
    {
        foreach (var g in AssetDatabase.FindAssets(name + " t:Font"))
            return AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(g));
        return null;
    }

    static RectTransform Group(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Image Img(string name, Transform parent, string sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = S(sprite);
        img.raycastTarget = false;
        if (img.sprite) img.SetNativeSize();
        return img;
    }

    static void Stretch(Image img)
    {
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Image Place(Image img, float x, float y)
    {
        img.rectTransform.anchoredPosition = new Vector2(x, y);
        return img;
    }

    static void Dim(Transform parent)
    {
        var go = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.03f, 0.05f, 0.12f, 0.55f);
        img.raycastTarget = true;   // blocks clicks to the menu behind
        Stretch(img);
    }

    static Button MakeButton(string name, Transform parent, string baseName, float x, float y)
    {
        var img = Place(Img(name, parent, baseName + "_Normal"), x, y);
        img.raycastTarget = true;
        img.alphaHitTestMinimumThreshold = 0.3f;
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.SpriteSwap;
        var st = new SpriteState
        {
            highlightedSprite = S(baseName + "_Highlighted"),
            pressedSprite = S(baseName + "_Pressed"),
            selectedSprite = S(baseName + "_Normal"),
            disabledSprite = S(baseName + "_Disabled")
        };
        b.spriteState = st;
        var nav = b.navigation; nav.mode = Navigation.Mode.Vertical; b.navigation = nav;
        var fx = img.gameObject.AddComponent<MenuButtonFX>();
        fx.hoverSound = FindClip("ui_hover");
        fx.clickSound = FindClip("ui_click");
        return b;
    }

    static Text Label(string name, Transform parent, Font font, int size, Vector2 rectSize, Vector2 pos, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        if (font) t.font = font;
        t.fontSize = size;
        t.color = new Color32(58, 36, 20, 255);
        t.alignment = align;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var sh = go.GetComponent<Shadow>();
        sh.effectColor = new Color(1f, 0.96f, 0.85f, 0.6f);
        sh.effectDistance = new Vector2(0, -2);
        t.rectTransform.sizeDelta = rectSize;
        t.rectTransform.anchoredPosition = pos;
        return t;
    }

    static Slider MakeSlider(string name, Transform parent, Vector2 pos, float value)
    {
        var track = Img(name, parent, "Slider_Track");
        track.type = Image.Type.Sliced;
        track.raycastTarget = true;
        track.rectTransform.sizeDelta = new Vector2(420, 40);
        track.rectTransform.anchoredPosition = pos;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        var fa = (RectTransform)fillArea.transform;
        fa.SetParent(track.transform, false);
        fa.anchorMin = Vector2.zero; fa.anchorMax = Vector2.one;
        fa.offsetMin = new Vector2(2, 0); fa.offsetMax = new Vector2(-2, 0);
        var fill = Img("Fill", fa, "Slider_Fill");
        fill.type = Image.Type.Sliced;
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        var ha = (RectTransform)handleArea.transform;
        ha.SetParent(track.transform, false);
        ha.anchorMin = Vector2.zero; ha.anchorMax = Vector2.one;
        ha.offsetMin = new Vector2(18, 0); ha.offsetMax = new Vector2(-18, 0);
        var handle = Img("Handle", ha, "Slider_Handle");
        handle.raycastTarget = true;
        handle.rectTransform.sizeDelta = new Vector2(64, 30);   // height = track height + 30

        var s = track.gameObject.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0; s.maxValue = 1;
        s.value = value;
        return s;
    }

    static Toggle MakeToggle(string name, Transform parent, Vector2 pos)
    {
        var box = Img(name, parent, "Toggle_Box");
        box.raycastTarget = true;
        box.rectTransform.anchoredPosition = pos;
        var check = Img("Checkmark", box.transform, "Toggle_Check");
        var t = box.gameObject.AddComponent<Toggle>();
        t.targetGraphic = box;
        t.graphic = check;
        t.isOn = true;
        return t;
    }

    static void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
#else
        if (Object.FindObjectOfType<EventSystem>() != null) return;
#endif
        var es = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (t != null) es.AddComponent(t);
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
#endif
