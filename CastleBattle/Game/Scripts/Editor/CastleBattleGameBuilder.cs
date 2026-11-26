#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click game setup (shares helpers with CastleBattleMenuBuilder):
///   Tools > Castle Battle > 7. Create Game Prefabs + Assets
///   Tools > Castle Battle > 8. Build Game Scene > (grid size)
/// </summary>
public static partial class CastleBattleMenuBuilder
{
    static string artRoot, gameRoot, genDir;

    static readonly string[] Shapes =
        { "Block_1x1", "Block_2x1", "Block_3x1", "Block_1x2", "Block_2x2", "Triangle_1x1", "Triangle_2x2", "Roof_2x1", "Arch_2x1", "Circle_1x1", "Battlement_1x1", "Battlement_2x1" };

    // ------------------------------------------------------------------ locating folders
    static bool LocateArt()
    {
        string piece = FindPath("Wood_Block_1x1_Blue", "t:Texture2D");
        if (piece == null)
        {
            EditorUtility.DisplayDialog("Castle Battle", "Could not find the art pack (CastleSiegeAssets). Copy the whole CastleBattle folder into Assets.", "OK");
            return false;
        }
        string d = Path.GetDirectoryName(piece);               // .../Blue/Pieces/Wood
        for (int i = 0; i < 3; i++) d = Path.GetDirectoryName(d);
        artRoot = d.Replace('\\', '/');

        string gm = FindPath("GameManager", "t:MonoScript");
        if (gm == null)
        {
            EditorUtility.DisplayDialog("Castle Battle", "Could not find GameManager.cs. Copy the whole CastleBattle folder into Assets.", "OK");
            return false;
        }
        gameRoot = Path.GetDirectoryName(Path.GetDirectoryName(gm)).Replace('\\', '/');   // .../Game
        genDir = gameRoot + "/Generated";
        EnsureFolder(gameRoot, "Generated");
        EnsureFolder(genDir, "Prefabs");
        EnsureFolder(genDir + "/Prefabs", "Red");
        EnsureFolder(genDir + "/Prefabs", "Blue");
        EnsureFolder(genDir, "PhysicsMaterials");
        return true;
    }

    static string FindPath(string name, string filter, string inFolder = null)
    {
        var guids = inFolder == null ? AssetDatabase.FindAssets(name + " " + filter) : AssetDatabase.FindAssets(name + " " + filter, new[] { inFolder });
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p) == name) return p;
        }
        return null;
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    /// <summary>Sprite from the art pack (CastleSiegeAssets).</summary>
    static Sprite SA(string name)
    {
        string p = FindPath(name, "t:Sprite", artRoot);
        if (p == null) { Debug.LogWarning("[Castle Battle] Art sprite not found: " + name); return null; }
        return AssetDatabase.LoadAssetAtPath<Sprite>(p);
    }

    // ------------------------------------------------------------------ import settings for the art pack
    static void SetupArtImports()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { artRoot });
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string n = Path.GetFileNameWithoutExtension(path);
                bool bg = path.Replace('\\', '/').Contains("/Background/");
                if (ti.textureType == TextureImporterType.Sprite && !ti.mipmapEnabled &&
                    Mathf.Approximately(ti.spritePixelsPerUnit, bg ? 100 : 128)) continue;   // already set up
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = bg ? 100 : 128;          // 128 px = 1 grid square
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                var set = new TextureImporterSettings();
                ti.ReadTextureSettings(set);
                set.spriteMeshType = bg ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                set.spriteGenerateFallbackPhysicsShape = true;
                if (n.StartsWith("Cannon_Barrel"))
                {
                    set.spriteAlignment = (int)SpriteAlignment.Custom;
                    set.spritePivot = new Vector2(0.17f, 0.5f);   // rotates around the back of the barrel
                }
                else set.spriteAlignment = (int)SpriteAlignment.Center;
                ti.SetTextureSettings(set);
                ti.SaveAndReimport();
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------ physics materials
    static PhysicsMaterial2D PhysMat(string name, float friction, float bounce)
    {
        string p = genDir + "/PhysicsMaterials/" + name + ".physicsMaterial2D";
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(p);
        if (m == null)
        {
            m = new PhysicsMaterial2D(name);
            AssetDatabase.CreateAsset(m, p);
        }
        m.friction = friction;
        m.bounciness = bounce;
        EditorUtility.SetDirty(m);
        return m;
    }

    static PhysicsMaterial2D MatFor(PieceMaterial m)
    {
        switch (m)
        {
            case PieceMaterial.Stone: return PhysMat("Stone", 0.75f, 0.02f);
            case PieceMaterial.Metal: return PhysMat("Metal", 0.4f, 0.15f);
            case PieceMaterial.Glass: return PhysMat("Glass", 0.3f, 0.05f);
            case PieceMaterial.Ice: return PhysMat("Ice", 0.05f, 0.05f);
            case PieceMaterial.Light: return PhysMat("Light", 0.6f, 0.05f);
            default: return PhysMat("Wood", 0.65f, 0.05f);
        }
    }

    // ------------------------------------------------------------------ step 7: prefabs + assets
    [MenuItem("Tools/Castle Battle/7. Create Game Prefabs + Assets")]
    public static void CreateGameAssets()
    {
        if (!Locate() || !LocateArt()) return;
        SetupImports();
        SetupArtImports();
        var red = MakeCatalog("Red");
        var blue = MakeCatalog("Blue");

        string gaPath = genDir + "/GameAssets.asset";
        var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(gaPath);
        if (ga == null) { ga = ScriptableObject.CreateInstance<GameAssets>(); AssetDatabase.CreateAsset(ga, gaPath); }
        ga.redCatalog = red;
        ga.blueCatalog = blue;
        ga.kingRed = MakeKing("Red");
        ga.kingBlue = MakeKing("Blue");
        ga.grids = new[] { SA("BuildGrid_8x8"), SA("BuildGrid_12x12"), SA("BuildGrid_12x10"), SA("BuildGrid_16x16") };
        ga.ammo = new[] { SA("Cannonball"), SA("StoneBall"), SA("FireShot"), SA("ExplosiveBarrel"), SA("ChainShot"), SA("SplitShot") };
        ga.splitPiece = SA("SplitShot_Piece");
        ga.dust = SA("Dust_1"); ga.smoke = SA("Smoke_1"); ga.spark = SA("Spark"); ga.explosion = SA("Explosion");
        ga.shockwave = SA("Shockwave"); ga.splash = SA("Water_Splash"); ga.hitStar = SA("HitStar");
        ga.fire = new[] { SA("Fire_1"), SA("Fire_2"), SA("Fire_3"), SA("Fire_4") };
        ga.debrisWood = SA("Debris_Wood_1"); ga.debrisStone = SA("Debris_Stone_1"); ga.debrisMetal = SA("Debris_Metal");
        ga.debrisGlass = SA("Debris_Glass"); ga.debrisIce = SA("Debris_Ice");
        ga.cannon = FindClip("sfx_cannon"); ga.explosionSfx = FindClip("sfx_explosion");
        ga.hitWood = FindClip("sfx_hit_wood"); ga.hitStone = FindClip("sfx_hit_stone"); ga.hitMetal = FindClip("sfx_hit_metal");
        ga.breakWood = FindClip("sfx_break_wood"); ga.breakStone = FindClip("sfx_break_stone"); ga.breakGlass = FindClip("sfx_break_glass");
        ga.splashSfx = FindClip("sfx_splash"); ga.fireSfx = FindClip("sfx_fire"); ga.place = FindClip("sfx_place");
        ga.kingHit = FindClip("sfx_king_hit"); ga.victory = FindClip("jingle_victory"); ga.defeat = FindClip("jingle_defeat");
        ga.music = FindClip("music_battle_loop");
        EditorUtility.SetDirty(ga);
        AssetDatabase.SaveAssets();
        Debug.Log("[Castle Battle] Prefabs, catalogs and GameAssets created in " + genDir);
    }

    static PieceCatalog MakeCatalog(string team)
    {
        string path = genDir + "/Catalog_" + team + ".asset";
        var cat = AssetDatabase.LoadAssetAtPath<PieceCatalog>(path);
        if (cat == null) { cat = ScriptableObject.CreateInstance<PieceCatalog>(); AssetDatabase.CreateAsset(cat, path); }
        string[] mats = { "Wood", "Stone", "Metal" };
        PieceMaterial[] pm = { PieceMaterial.Wood, PieceMaterial.Stone, PieceMaterial.Metal };
        var lists = new GameObject[3][];
        for (int m = 0; m < 3; m++)
        {
            lists[m] = new GameObject[12];
            for (int i = 0; i < 12; i++)
            {
                string shape = Shapes[i];
                string size = shape.Substring(shape.LastIndexOf('_') + 1);       // "2x1"
                int w = int.Parse(size.Split('x')[0]), h = int.Parse(size.Split('x')[1]);
                int kind = shape.StartsWith("Block") ? 0 : shape.StartsWith("Circle") ? 1 : 2;
                string name = mats[m] + "_" + shape + "_" + team;
                lists[m][i] = MakePiece(team, name, name, pm[m], w, h, kind, false, name);
            }
        }
        cat.wood = lists[0]; cat.stone = lists[1]; cat.metal = lists[2];
        cat.special = new[]
        {
            MakePiece(team, "Glass_Block_1x1_" + team, "Glass_Block_1x1_" + team, PieceMaterial.Glass, 1, 1, 0, false, "Glass_Block_1x1_" + team),
            MakePiece(team, "Glass_Block_2x1_" + team, "Glass_Block_2x1_" + team, PieceMaterial.Glass, 2, 1, 0, false, "Glass_Block_2x1_" + team),
            MakePiece(team, "Ice_Block_1x1_" + team, "Ice_Block_1x1_" + team, PieceMaterial.Ice, 1, 1, 0, false, "Ice_Block_1x1_" + team),
            MakePiece(team, "Ice_Block_2x1_" + team, "Ice_Block_2x1_" + team, PieceMaterial.Ice, 2, 1, 0, false, "Ice_Block_2x1_" + team),
            MakePiece(team, "Deflector_1x1_" + team, "Deflector_1x1_" + team, PieceMaterial.Metal, 1, 1, 2, false, "Deflector_1x1_" + team),
            MakePiece(team, "Deflector_2x2_" + team, "Deflector_2x2_" + team, PieceMaterial.Metal, 2, 2, 2, false, "Deflector_2x2_" + team),
            MakePiece(team, "Shield_" + team, "Shield_" + team, PieceMaterial.Metal, 1, 1, 2, false, null),
            MakePiece(team, "Turret_Base_" + team, "Turret_" + team, PieceMaterial.Stone, 1, 1, 2, false, null),
            MakePiece(team, "Wheel_1x1_" + team, "Wheel_" + team, PieceMaterial.Wood, 1, 1, 1, false, null),
            MakePiece(team, "WaterBarrel", "WaterBarrel_" + team, PieceMaterial.Wood, 1, 1, 2, true, null),
            MakePiece(team, "Counterweight_1x1_" + team, "Counterweight_" + team, PieceMaterial.Metal, 1, 1, 2, false, null),
            MakePiece(team, "Archer_" + team, "Archer_" + team, PieceMaterial.Light, 1, 1, 2, false, null),
        };
        EditorUtility.SetDirty(cat);
        return cat;
    }

    /// <param name="colliderKind">0 = box, 1 = circle, 2 = traced from the picture</param>
    static GameObject MakePiece(string team, string sprite, string prefabName, PieceMaterial mat, int w, int h, int colliderKind, bool water, string damageBase)
    {
        var sp = SA(sprite);
        if (sp == null) return null;
        var go = new GameObject(prefabName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = 5;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = Mathf.Max(0.3f, w * h * MaterialStats.MassPerCell(mat));
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sharedMaterial = MatFor(mat);
        if (colliderKind == 0)
        {
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(w - 0.04f, h - 0.04f);
        }
        else if (colliderKind == 1)
        {
            var cc = go.AddComponent<CircleCollider2D>();
            cc.radius = 0.47f * Mathf.Min(w, h);
        }
        else go.AddComponent<PolygonCollider2D>();      // follows the shape of the sprite
        var b = go.AddComponent<Block>();
        b.material = mat;
        b.cols = w; b.rows = h;
        b.maxHealth = w * h * MaterialStats.HealthPerCell(mat);
        b.isWaterBarrel = water;
        b.team = team == "Red" ? 0 : 1;
        b.damageSprites = new Sprite[3];
        if (damageBase != null)
            for (int i = 0; i < 3; i++)
            {
                string p = FindPath(damageBase + "_Damage" + (i + 1), "t:Sprite", artRoot);
                b.damageSprites[i] = p != null ? AssetDatabase.LoadAssetAtPath<Sprite>(p) : null;
            }
        string path = genDir + "/Prefabs/" + team + "/" + prefabName + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject MakeKing(string team)
    {
        var go = new GameObject("King_" + team);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 6;
        var anim = go.AddComponent<KingAnimator>();
        anim.idle = Frames(team, "Idle"); anim.walk = Frames(team, "Walk");
        anim.cheer = Frames(team, "Cheer"); anim.fall = Frames(team, "Fall");
        if (anim.idle.Length > 0) sr.sprite = anim.idle[0];
        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 3f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sharedMaterial = MatFor(PieceMaterial.Wood);
        var cap = go.AddComponent<CapsuleCollider2D>();
        cap.direction = CapsuleDirection2D.Vertical;
        cap.size = new Vector2(0.85f, 1.85f);
        cap.offset = new Vector2(0f, 0.93f);
        var k = go.AddComponent<KingUnit>();
        k.team = team == "Red" ? 0 : 1;
        string path = genDir + "/Prefabs/King_" + team + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------------ step 8: the game scene
    [MenuItem("Tools/Castle Battle/8. Build Game Scene/12 x 10 (recommended)")] static void Game12x10() { BuildGameScene(GridSize.Grid12x10); }
    [MenuItem("Tools/Castle Battle/8. Build Game Scene/8 x 8")] static void Game8x8() { BuildGameScene(GridSize.Grid8x8); }
    [MenuItem("Tools/Castle Battle/8. Build Game Scene/12 x 12")] static void Game12x12() { BuildGameScene(GridSize.Grid12x12); }
    [MenuItem("Tools/Castle Battle/8. Build Game Scene/16 x 16")] static void Game16x16() { BuildGameScene(GridSize.Grid16x16); }

    static void BuildGameScene(GridSize gs)
    {
        if (!Locate() || !LocateArt()) return;
        CreateGameAssets();
        var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(genDir + "/GameAssets.asset");
        titleFont = LoadFont("Cardo-Bold");
        bodyFont = LoadFont("CrimsonText-SemiBold");

        // ---- camera ----
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cgo = new GameObject("Main Camera");
            cgo.tag = "MainCamera";
            cam = cgo.AddComponent<Camera>();
            cgo.AddComponent<AudioListener>();
            Undo.RegisterCreatedObjectUndo(cgo, "Create Camera");
        }
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.transform.position = new Vector3(0f, 7f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.36f, 0.62f, 0.95f);
        var camCtrl = cam.GetComponent<CameraController>();
        if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<CameraController>();

        // ---- background (its grass line sits at y = 0) ----
        var bg = new GameObject("Background", typeof(SpriteRenderer));
        Undo.RegisterCreatedObjectUndo(bg, "Build Game Scene");
        var bsr = bg.GetComponent<SpriteRenderer>();
        bsr.sprite = S("BG_Battlefield");
        bsr.sortingOrder = -100;
        if (bsr.sprite != null)
        {
            const float H = 34f;
            bg.transform.localScale = Vector3.one * (H / bsr.sprite.bounds.size.y);
            bg.transform.position = new Vector3(0f, -5f + H * 0.5f, 5f);
        }

        // ---- ground ----
        var ground = new GameObject("Ground");
        Undo.RegisterCreatedObjectUndo(ground, "Build Game Scene");
        ground.transform.position = new Vector3(0f, -2.5f, 0f);
        var gb = ground.AddComponent<BoxCollider2D>();
        gb.size = new Vector2(400f, 5f);
        gb.sharedMaterial = PhysMat("Ground", 0.85f, 0f);

        // ---- game manager, castles, cannons ----
        var gmGO = new GameObject("GameManager");
        Undo.RegisterCreatedObjectUndo(gmGO, "Build Game Scene");
        var gm = gmGO.AddComponent<GameManager>();
        gm.assets = ga;
        gm.gridSize = gs;
        gm.ai = gmGO.AddComponent<AIPlayer>();
        gm.cam = camCtrl;
        gm.red = MakeCastle("Red_Castle", 0, ga.redCatalog, ga.kingRed, false);
        gm.blue = MakeCastle("Blue_Castle", 1, ga.blueCatalog, ga.kingBlue, true);
        gm.redCannon = MakeCannon("Red", false);
        gm.blueCannon = MakeCannon("Blue", true);

        // ---- HUD ----
        var canvasGO = new GameObject("CastleBattle_GameHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Game Scene");
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        EnsureEventSystem();
        Transform hud = canvasGO.transform;

        var rp = BuildResourcesPanel(hud);
        gm.resourcePanel = rp;
        gm.buildUI = rp.gameObject;
        UnityEventTools.AddPersistentListener(rp.onReady, gm.PlayerReady);

        // banner (top)
        var banner = new GameObject("Banner", typeof(RectTransform));
        var brt = (RectTransform)banner.transform;
        brt.SetParent(hud, false);
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(-470f, -70f);
        var ribbon = Img("Ribbon", brt, "Ribbon_Blank");
        ribbon.rectTransform.sizeDelta = new Vector2(880f, 150f);
        var bannerText = Label("Text", brt, titleFont, 30, new Vector2(760f, 60f), new Vector2(0f, 8f), TextAnchor.MiddleCenter);
        bannerText.color = new Color32(255, 238, 214, 255);
        bannerText.GetComponent<Shadow>().effectColor = new Color(0.3f, 0.06f, 0.05f, 1f);
        bannerText.text = "CASTLE BATTLE";
        gm.bannerText = bannerText;
        gm.bannerRoot = brt;

        // ammo bar (bottom, battle only)
        var battle = Group("BattleUI", hud);
        var bar = battle.gameObject.AddComponent<AmmoBar>();
        string[] ammoNames = { "Cannonball", "StoneBall", "FireShot", "ExplosiveBarrel", "ChainShot", "SplitShot" };
        bar.slots = new Image[6]; bar.icons = new Image[6]; bar.counts = new Text[6];
        bar.slotNormal = S("Slot_Normal"); bar.slotSelected = S("Slot_Selected"); bar.slotLocked = S("Slot_Locked");
        for (int i = 0; i < 6; i++)
        {
            var slot = Img("Ammo_" + i, battle, "Slot_Normal");
            var srt = slot.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
            srt.sizeDelta = new Vector2(118f, 118f);
            srt.anchoredPosition = new Vector2((i - 2.5f) * 130f, 80f);
            slot.raycastTarget = true;
            var b = slot.gameObject.AddComponent<Button>();
            b.targetGraphic = slot;
            UnityEventTools.AddIntPersistentListener(b.onClick, bar.Select, i);
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(slot.transform, false);
            icon.sprite = SA(ammoNames[i]);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.sizeDelta = new Vector2(78f, 78f);
            icon.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            var count = Label("Count", slot.transform, titleFont, 26, new Vector2(60f, 36f), new Vector2(34f, -38f), TextAnchor.MiddleCenter);
            count.color = new Color32(255, 246, 222, 255);
            var key = Label("Key", slot.transform, titleFont, 22, new Vector2(36f, 30f), new Vector2(-40f, 40f), TextAnchor.MiddleCenter);
            key.color = new Color32(255, 220, 120, 255);
            key.text = (i + 1).ToString();
            bar.slots[i] = slot; bar.icons[i] = icon; bar.counts[i] = count;
        }
        gm.battleUI = battle.gameObject;
        gm.ammoBar = bar;

        // aiming panel (ANGLE / POWER buttons + sliders + FIRE) under the active castle
        var aim = battle.gameObject.AddComponent<AimPanel>();
        aim.titleFont = titleFont;
        aim.bodyFont = bodyFont;
        aim.sliderTrack = S("Slider_Track");
        aim.sliderFill = S("Slider_Fill");
        aim.sliderHandle = S("Slider_Handle");
        gm.aimPanel = aim;

        // ---- pause + victory/defeat ----
        gm.pauseMenu = CreatePause();
        gm.resultScreen = CreateResults();

        Selection.activeGameObject = gmGO;
        EditorSceneManager.MarkSceneDirty(gmGO.scene);
        Debug.Log("[Castle Battle] Game scene built (" + gs + "). Save the scene as 'Game', add it to Build Settings, and press Play!");
    }

    static BuildManager MakeCastle(string name, int team, PieceCatalog cat, GameObject king, bool faceLeft)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build Game Scene");
        var bm = go.AddComponent<BuildManager>();
        bm.team = team;
        bm.catalog = cat;
        bm.kingPrefab = king;
        bm.faceLeft = faceLeft;
        var g = new GameObject("BuildGrid");
        g.transform.SetParent(go.transform, false);
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        bm.gridRenderer = sr;
        return bm;
    }

    static CannonController MakeCannon(string team, bool faceLeft)
    {
        var root = new GameObject("Cannon_" + team);
        Undo.RegisterCreatedObjectUndo(root, "Build Game Scene");
        if (faceLeft) root.transform.localScale = new Vector3(-1f, 1f, 1f);
        var car = new GameObject("Carriage");
        car.transform.SetParent(root.transform, false);
        var csr = car.AddComponent<SpriteRenderer>();
        csr.sprite = SA("Cannon_Carriage_" + team);
        csr.sortingOrder = 9;
        var barrel = new GameObject("Barrel");
        barrel.transform.SetParent(root.transform, false);
        barrel.transform.localPosition = new Vector3(-0.45f, 0.25f, 0f);
        var bsr = barrel.AddComponent<SpriteRenderer>();
        bsr.sprite = SA("Cannon_Barrel_" + team);
        bsr.sortingOrder = 8;
        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(barrel.transform, false);
        muzzle.transform.localPosition = new Vector3(1.62f, 0f, 0f);
        var cc = root.AddComponent<CannonController>();
        cc.team = team == "Red" ? 0 : 1;
        cc.faceLeft = faceLeft;
        cc.barrel = barrel.transform;
        cc.muzzle = muzzle.transform;
        return cc;
    }
}
#endif
