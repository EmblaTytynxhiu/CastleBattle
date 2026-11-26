using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One team's build area. The object's position is the bottom-left corner of the grid (on the ground).
/// 1 grid square = 1 Unity unit. While building:
///   pick a piece in the RESOURCES panel -> a see-through preview follows the mouse and snaps to the squares
///   green = allowed, red = not allowed (outside the grid, overlapping, or floating in the air)
///   left click = place,  right click on a piece = take it back (only if nothing rests on top of it)
/// The King is placed automatically in the middle of the bottom row.
/// </summary>
public class BuildManager : MonoBehaviour
{
    public int team;                   // 0 = Red, 1 = Blue
    public int cols = 12, rows = 10;
    public PieceCatalog catalog;
    public GameObject kingPrefab;
    public SpriteRenderer gridRenderer;
    public bool faceLeft;

    public KingUnit King { get; private set; }
    public bool IsBuilding { get; private set; }
    public Vector2 Origin => transform.position;
    public Vector2 Center => Origin + new Vector2(cols * 0.5f, rows * 0.5f);
    public Vector2 KingTarget => King != null ? (Vector2)King.transform.position + Vector2.up * 0.9f : Origin + new Vector2(cols * 0.5f, 1f);

    class Placed { public GameObject go; public int cat, idx, x, y, w, h; }

    int[,] owner;                      // -1 empty, -2 king, otherwise index into placed
    readonly List<Placed> placed = new List<Placed>();
    ResourcePanel panel;
    Camera cam;
    GameObject ghost;
    SpriteRenderer ghostSr;
    GameObject ghostPrefab;

    public void Init(int c, int r, Sprite gridSprite)
    {
        cols = c; rows = r;
        owner = new int[c, r];
        for (int x = 0; x < c; x++) for (int y = 0; y < r; y++) owner[x, y] = -1;
        if (gridRenderer != null)
        {
            gridRenderer.sprite = gridSprite;
            gridRenderer.transform.localPosition = new Vector3(c * 0.5f, r * 0.5f, 0f);
            if (gridSprite != null)
            {
                Vector2 s = gridSprite.bounds.size;
                gridRenderer.transform.localScale = new Vector3(c / Mathf.Max(0.01f, s.x), r / Mathf.Max(0.01f, s.y), 1f);
            }
            gridRenderer.enabled = false;
        }
        PlaceKing();
    }

    void PlaceKing()
    {
        if (King != null || kingPrefab == null) return;
        int kx = cols / 2;
        var go = Instantiate(kingPrefab, Origin + new Vector2(kx + 0.5f, 0f), Quaternion.identity, transform);
        go.name = team == 0 ? "King_Red" : "King_Blue";
        King = go.GetComponent<KingUnit>();
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb) rb.bodyType = RigidbodyType2D.Kinematic;
        if (King) { King.team = team; King.SetFacing(faceLeft); }
        owner[kx, 0] = -2;
        if (rows > 1) owner[kx, 1] = -2;
    }

    public void BeginBuild(ResourcePanel p)
    {
        panel = p;
        cam = Camera.main;
        IsBuilding = true;
        if (gridRenderer) gridRenderer.enabled = true;
    }

    public void EndBuild()
    {
        IsBuilding = false;
        if (gridRenderer) gridRenderer.enabled = false;
        if (ghost) Destroy(ghost);
        ghostPrefab = null;
    }

    void Update()
    {
        if (!IsBuilding || panel == null || Time.timeScale == 0f) return;
        if (cam == null) cam = Camera.main;
        GameObject prefab = panel.SelectedSlot >= 0 && catalog != null ? catalog.Get(panel.CurrentCategory, panel.SelectedSlot) : null;
        Vector2 m = GameInput.MouseWorld(cam);
        bool overUI = GameInput.OverUI;

        if (prefab != null)
        {
            var blk = prefab.GetComponent<Block>();
            int w = blk ? blk.cols : 1, h = blk ? blk.rows : 1;
            int cx = Mathf.FloorToInt(m.x - Origin.x - w * 0.5f + 0.5f);
            int cy = Mathf.FloorToInt(m.y - Origin.y - h * 0.5f + 0.5f);
            bool ok = CanPlace(cx, cy, w, h);
            ShowGhost(prefab, cx, cy, w, h, ok, !overUI);
            int cat = panel.CurrentCategory, idx = panel.SelectedSlot;
            if (GameInput.LeftDown && !overUI && ok && panel.UseSelected())
                Place(prefab, cat, idx, cx, cy, w, h);
        }
        else if (ghost) ghost.SetActive(false);

        if (GameInput.RightDown && !overUI) TryRemoveAt(m);
    }

    void ShowGhost(GameObject prefab, int cx, int cy, int w, int h, bool ok, bool visible)
    {
        if (ghost == null)
        {
            ghost = new GameObject("PlacementPreview");
            ghostSr = ghost.AddComponent<SpriteRenderer>();
            ghostSr.sortingOrder = 20;
        }
        if (ghostPrefab != prefab)
        {
            ghostPrefab = prefab;
            var psr = prefab.GetComponent<SpriteRenderer>();
            ghostSr.sprite = psr ? psr.sprite : null;
            ghost.transform.localScale = prefab.transform.localScale;
        }
        ghost.SetActive(visible);
        ghost.transform.position = Origin + new Vector2(cx + w * 0.5f, cy + h * 0.5f);
        ghostSr.color = ok ? new Color(0.6f, 1f, 0.6f, 0.75f) : new Color(1f, 0.45f, 0.45f, 0.6f);
    }

    public bool CanPlace(int x, int y, int w, int h)
    {
        if (owner == null || x < 0 || y < 0 || x + w > cols || y + h > rows) return false;
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                if (owner[x + i, y + j] != -1) return false;
        if (y == 0) return true;                       // standing on the ground
        for (int i = 0; i < w; i++)
            if (owner[x + i, y - 1] != -1) return true;  // something underneath
        return false;                                  // would float in the air
    }

    public GameObject Place(GameObject prefab, int cat, int idx, int x, int y, int w, int h)
    {
        var go = Instantiate(prefab, Origin + new Vector2(x + w * 0.5f, y + h * 0.5f), Quaternion.identity, transform);
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb) rb.bodyType = RigidbodyType2D.Kinematic;
        var blk = go.GetComponent<Block>();
        if (blk) blk.team = team;
        int id = placed.Count;
        placed.Add(new Placed { go = go, cat = cat, idx = idx, x = x, y = y, w = w, h = h });
        for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) owner[x + i, y + j] = id;
        Fx.Dust(go.transform.position + Vector3.down * h * 0.5f, 0.6f + w * 0.2f);
        if (GameAssets.I != null) AudioManager.Play(GameAssets.I.place, 0.8f);
        return go;
    }

    /// <summary>Used by the computer player: place a catalog piece at a grid square if the rules allow it.</summary>
    public bool TryPlace(int cat, int idx, int x, int y)
    {
        var prefab = catalog != null ? catalog.Get(cat, idx) : null;
        if (prefab == null) return false;
        var blk = prefab.GetComponent<Block>();
        int w = blk ? blk.cols : 1, h = blk ? blk.rows : 1;
        if (!CanPlace(x, y, w, h)) return false;
        Place(prefab, cat, idx, x, y, w, h);
        return true;
    }

    void TryRemoveAt(Vector2 world)
    {
        int gx = Mathf.FloorToInt(world.x - Origin.x), gy = Mathf.FloorToInt(world.y - Origin.y);
        if (gx < 0 || gy < 0 || gx >= cols || gy >= rows) return;
        int id = owner[gx, gy];
        if (id < 0) return;
        var p = placed[id];
        if (p.go == null) return;
        if (p.y + p.h < rows)
            for (int i = 0; i < p.w; i++)
            {
                int above = owner[p.x + i, p.y + p.h];
                if (above != -1 && above != id) return;   // something rests on it
            }
        for (int i = 0; i < p.w; i++) for (int j = 0; j < p.h; j++) owner[p.x + i, p.y + j] = -1;
        Destroy(p.go);
        p.go = null;
        if (panel != null) panel.ReturnPiece(p.cat, p.idx);
    }

    /// <summary>READY was pressed: turn on real physics for every piece and the King.</summary>
    public void ActivatePhysics()
    {
        foreach (var p in placed)
        {
            if (p.go == null) continue;
            var rb = p.go.GetComponent<Rigidbody2D>();
            if (rb) { rb.bodyType = RigidbodyType2D.Dynamic; rb.WakeUp(); }
        }
        if (King != null)
        {
            var rb = King.GetComponent<Rigidbody2D>();
            if (rb) { rb.bodyType = RigidbodyType2D.Dynamic; rb.WakeUp(); }
        }
    }

    /// <summary>True when nothing in this castle is still moving.</summary>
    public bool IsCalm(float speed = 0.25f)
    {
        foreach (var p in placed)
        {
            if (p.go == null) continue;
            var rb = p.go.GetComponent<Rigidbody2D>();
            if (rb && rb.bodyType == RigidbodyType2D.Dynamic && (rb.Vel().magnitude > speed || Mathf.Abs(rb.angularVelocity) > 25f)) return false;
        }
        if (King != null)
        {
            var rb = King.GetComponent<Rigidbody2D>();
            if (rb && rb.bodyType == RigidbodyType2D.Dynamic && rb.Vel().magnitude > speed) return false;
        }
        return true;
    }

    public int PieceCount
    {
        get { int n = 0; foreach (var p in placed) if (p.go != null) n++; return n; }
    }
}
