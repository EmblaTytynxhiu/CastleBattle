using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Castle Battle – the RESOURCES panel on the castle building screen.
/// Tabs (Wood / Stone / Metal / Special), 12 piece slots with counts, the big counter badge (pieces left) and READY.
///
/// Hook it to your building code:
///   onPieceSelected(category, index)  -> start dragging / previewing that piece on the grid
///   UseSelected()                     -> call when the player places the piece (counts go down)
///   ReturnPiece(category, index)      -> call when the player removes a placed piece (counts go up)
///   onReady                           -> start the battle
/// </summary>
public class ResourcePanel : MonoBehaviour
{
    [System.Serializable]
    public class Category
    {
        public string name;
        [TextArea] public string description;
        public Sprite[] icons;
        public string[] pieceNames;
        public int[] counts;
    }

    [System.Serializable] public class PieceEvent : UnityEvent<int, int> { }

    [Header("Data")]
    public Category[] categories;
    [Tooltip("Total pieces the player may still place (the big round badge).")]
    public int piecesLeft = 11;

    [Header("UI (filled in by the builder)")]
    public Image[] tabs;
    public Sprite[] tabActive, tabInactive;
    public Image[] slots;
    public Image[] slotIcons;
    public GameObject[] slotBadges;
    public Text[] slotCounts;
    public Sprite slotNormal, slotSelected, slotLocked;
    public Text counterText, infoTitle, infoText;

    [Header("Events")]
    public PieceEvent onPieceSelected;
    public UnityEvent onReady;

    public int CurrentCategory { get; private set; }
    public int SelectedSlot { get; private set; } = -1;
    public Sprite SelectedIcon => SelectedSlot >= 0 ? categories[CurrentCategory].icons[SelectedSlot] : null;

    int[][] startCounts;

    void Awake()
    {
        if (categories == null) return;
        startCounts = new int[categories.Length][];
        for (int i = 0; i < categories.Length; i++)
            startCounts[i] = categories[i].counts != null ? (int[])categories[i].counts.Clone() : new int[0];
    }

    void Start()
    {
        SelectTab(0);
    }

    /// <summary>Refill every piece count (e.g. for the second player) and set how many pieces may be placed.</summary>
    public void ResetAll(int pieces)
    {
        if (startCounts != null)
            for (int i = 0; i < categories.Length && i < startCounts.Length; i++)
                categories[i].counts = (int[])startCounts[i].Clone();
        piecesLeft = pieces;
        SelectTab(0);
    }

    public void SelectTab(int c)
    {
        if (categories == null || c < 0 || c >= categories.Length) return;
        CurrentCategory = c;
        SelectedSlot = -1;
        for (int i = 0; i < tabs.Length; i++)
            if (tabs[i]) tabs[i].sprite = i == c ? tabActive[i] : tabInactive[i];
        Refresh();
    }

    public void SelectSlot(int i)
    {
        var cat = categories[CurrentCategory];
        if (i < 0 || i >= cat.icons.Length || Count(cat, i) <= 0 || piecesLeft <= 0) return;
        SelectedSlot = i;
        Refresh();
        onPieceSelected?.Invoke(CurrentCategory, i);
    }

    /// <summary>Call when the selected piece is placed on the grid. Returns false if nothing is left.</summary>
    public bool UseSelected()
    {
        if (SelectedSlot < 0 || piecesLeft <= 0) return false;
        var cat = categories[CurrentCategory];
        if (Count(cat, SelectedSlot) <= 0) return false;
        cat.counts[SelectedSlot]--;
        piecesLeft--;
        if (cat.counts[SelectedSlot] <= 0) SelectedSlot = -1;
        Refresh();
        return true;
    }

    public void ReturnPiece(int category, int index)
    {
        var cat = categories[category];
        if (index < 0 || index >= cat.counts.Length) return;
        cat.counts[index]++;
        piecesLeft++;
        Refresh();
    }

    public void Ready() { onReady?.Invoke(); }

    static int Count(Category c, int i) => c.counts != null && i < c.counts.Length ? c.counts[i] : 0;

    void Refresh()
    {
        var cat = categories[CurrentCategory];
        for (int i = 0; i < slots.Length; i++)
        {
            bool has = cat.icons != null && i < cat.icons.Length && cat.icons[i] != null;
            int n = has ? Count(cat, i) : 0;
            if (slotIcons[i])
            {
                slotIcons[i].enabled = has;
                if (has) { slotIcons[i].sprite = cat.icons[i]; slotIcons[i].SetNativeSize(); }
                slotIcons[i].color = n > 0 ? Color.white : new Color(1, 1, 1, 0.35f);
            }
            if (slots[i]) slots[i].sprite = !has || n <= 0 ? slotLocked : (i == SelectedSlot ? slotSelected : slotNormal);
            if (slotBadges[i]) slotBadges[i].SetActive(has);
            if (slotCounts[i]) slotCounts[i].text = n.ToString();
        }
        if (counterText) counterText.text = piecesLeft.ToString();
        if (infoTitle) infoTitle.text = SelectedSlot >= 0 ? cat.pieceNames[SelectedSlot] : cat.name.ToUpper();
        if (infoText) infoText.text = cat.description;
    }
}
