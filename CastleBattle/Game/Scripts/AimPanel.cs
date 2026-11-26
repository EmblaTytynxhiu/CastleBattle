using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The aiming panel shown under the castle whose turn it is:
///
///      ANGLE   [ - ] ========o======== [ + ]   35°
///      POWER   [ - ] ============o==== [ + ]   60%
///                     [   FIRE!   ]
///
/// Hold a - / + button to keep turning, or drag a slider. Everything stays in sync with the
/// mouse slingshot and the W/S, A/D keys, and the dotted arc always shows the result.
/// The panel builds its own UI the first time it is used, so you don't need to set anything up.
/// Optional sprites and fonts can be filled in (the scene builder does this) to match the menus.
/// </summary>
public class AimPanel : MonoBehaviour
{
    [Header("Look (optional - plain colours are used if empty)")]
    public Font titleFont;
    public Font bodyFont;
    public Sprite panelSprite;
    public Sprite buttonSprite;
    public Sprite fireSprite;
    public Sprite sliderTrack, sliderFill, sliderHandle;

    [Header("Layout")]
    public Vector2 size = new Vector2(540f, 220f);
    [Tooltip("Distance of the panel's centre from the bottom-left (Red) or bottom-right (Blue) corner.")]
    public Vector2 cornerOffset = new Vector2(295f, 125f);

    static readonly Color RedTeam = new Color32(200, 52, 44, 255);
    static readonly Color BlueTeam = new Color32(52, 104, 205, 255);
    static readonly Color Cream = new Color32(255, 244, 222, 255);

    RectTransform root;
    Image background, fireImage;
    Text title, angleValue, powerValue, fireLabel;
    Slider angleSlider, powerSlider;
    Button fireButton;
    CannonController cannon;
    bool built;

    // ------------------------------------------------------------------ public

    /// <summary>Show the panel for a cannon. Pass interactable = false for the computer's turn.</summary>
    public void Bind(CannonController c, bool interactable)
    {
        if (!built) Build();
        cannon = c;
        root.gameObject.SetActive(c != null);
        if (c == null) return;

        bool right = c.faceLeft;                 // the Blue castle is on the right
        root.anchorMin = root.anchorMax = new Vector2(right ? 1f : 0f, 0f);
        root.anchoredPosition = new Vector2(right ? -cornerOffset.x : cornerOffset.x, cornerOffset.y);

        Color tc = c.team == 0 ? RedTeam : BlueTeam;
        if (background && panelSprite == null) background.color = new Color(0.14f, 0.1f, 0.08f, 0.88f);
        title.text = c.team == 0 ? "RED CANNON" : "BLUE CANNON";
        title.color = Color.Lerp(tc, Color.white, 0.35f);
        if (fireImage && fireSprite == null) fireImage.color = tc;

        angleSlider.minValue = c.minAngle;
        angleSlider.maxValue = c.maxAngle;
        powerSlider.minValue = 0f;
        powerSlider.maxValue = 100f;

        foreach (var s in root.GetComponentsInChildren<Selectable>(true)) s.interactable = interactable;
        fireLabel.text = interactable ? "FIRE!" : "AIMING...";
        Refresh();
    }

    public void Hide()
    {
        cannon = null;
        if (root) root.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ update

    void Update()
    {
        if (cannon == null || root == null || !root.gameObject.activeSelf) return;
        Refresh();
        if (!cannon.IsMyTurn && cannon.HasFired) fireButton.interactable = false;
    }

    void Refresh()
    {
        angleSlider.SetValueWithoutNotify(cannon.angle);
        powerSlider.SetValueWithoutNotify(cannon.powerPercent);
        angleValue.text = Mathf.RoundToInt(cannon.angle) + "°";
        powerValue.text = Mathf.RoundToInt(cannon.powerPercent) + "%";
    }

    bool CanControl => cannon != null && cannon.IsHumanTurn && Time.timeScale > 0f;

    // ------------------------------------------------------------------ building the UI

    void Build()
    {
        built = true;
        if (titleFont == null) titleFont = BuiltinFont();
        if (bodyFont == null) bodyFont = titleFont;

        var go = new GameObject("AimPanel", typeof(RectTransform), typeof(Image));
        root = (RectTransform)go.transform;
        root.SetParent(transform, false);
        root.sizeDelta = size;
        background = go.GetComponent<Image>();
        SetSprite(background, panelSprite);
        if (panelSprite == null) background.color = new Color(0.14f, 0.1f, 0.08f, 0.88f);
        background.raycastTarget = true;          // clicks on the panel never start a mouse-drag shot

        title = MakeText("Title", root, titleFont, 26, new Vector2(0f, size.y * 0.5f - 24f), new Vector2(size.x - 20f, 34f), TextAnchor.MiddleCenter);

        float rowW = size.x - 40f;
        angleSlider = MakeRow("ANGLE", 46f, rowW, out angleValue,
            d => { if (CanControl) cannon.AdjustAngle(d); },
            () => cannon != null ? cannon.angleSpeed : 30f,
            v => { if (CanControl) cannon.SetAngle(v); });
        powerSlider = MakeRow("POWER", -6f, rowW, out powerValue,
            d => { if (CanControl) cannon.AdjustPower(d); },
            () => cannon != null ? cannon.powerSpeed : 40f,
            v => { if (CanControl) cannon.SetPower(v); });

        // FIRE button
        var fire = new GameObject("FireButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var frt = (RectTransform)fire.transform;
        frt.SetParent(root, false);
        frt.sizeDelta = new Vector2(220f, 56f);
        frt.anchoredPosition = new Vector2(0f, -size.y * 0.5f + 40f);
        fireImage = fire.GetComponent<Image>();
        SetSprite(fireImage, fireSprite != null ? fireSprite : buttonSprite);
        if (fireSprite == null) fireImage.color = RedTeam;
        fireButton = fire.GetComponent<Button>();
        fireButton.targetGraphic = fireImage;
        fireButton.onClick.AddListener(() => { if (CanControl) cannon.FireCurrent(); });
        fireLabel = MakeText("Label", frt, titleFont, 28, Vector2.zero, frt.sizeDelta, TextAnchor.MiddleCenter);
        fireLabel.text = "FIRE!";

        root.gameObject.SetActive(false);
    }

    Slider MakeRow(string label, float y, float width, out Text valueText, Action<float> adjust, Func<float> speed, Action<float> onSlide)
    {
        var row = new GameObject(label + "_Row", typeof(RectTransform));
        var rrt = (RectTransform)row.transform;
        rrt.SetParent(root, false);
        rrt.sizeDelta = new Vector2(width, 44f);
        rrt.anchoredPosition = new Vector2(0f, y);
        float left = -width * 0.5f;

        var l = MakeText("Label", rrt, titleFont, 22, new Vector2(left + 48f, 0f), new Vector2(96f, 40f), TextAnchor.MiddleLeft);
        l.text = label;

        MakeStepButton("Minus", rrt, new Vector2(left + 124f, 0f), "-", -1f, adjust, speed);

        var slider = MakeSlider(rrt, new Vector2(left + 270f, 0f), new Vector2(216f, 26f));
        slider.onValueChanged.AddListener(v => onSlide(v));

        MakeStepButton("Plus", rrt, new Vector2(left + 410f, 0f), "+", 1f, adjust, speed);

        valueText = MakeText("Value", rrt, titleFont, 24, new Vector2(width * 0.5f - 34f, 0f), new Vector2(68f, 40f), TextAnchor.MiddleRight);
        return slider;
    }

    void MakeStepButton(string name, RectTransform parent, Vector2 pos, string sign, float dir, Action<float> adjust, Func<float> speed)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(HoldRepeat));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(42f, 42f);
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        SetSprite(img, buttonSprite);
        if (buttonSprite == null) img.color = new Color32(122, 84, 52, 255);
        var b = go.GetComponent<Button>();
        b.targetGraphic = img;
        var hold = go.GetComponent<HoldRepeat>();
        hold.button = b;
        // a tap moves exactly 1 degree / 1 percent, holding keeps going smoothly
        hold.onTap = () => adjust(dir);
        hold.onHold = dt => adjust(dir * speed() * dt);
        var t = MakeText("Sign", rt, titleFont, 30, Vector2.zero, rt.sizeDelta, TextAnchor.MiddleCenter);
        t.text = sign;
    }

    Slider MakeSlider(RectTransform parent, Vector2 pos, Vector2 sz)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = sz;
        rt.anchoredPosition = pos;

        var bg = NewImage("Background", rt, sliderTrack, new Color(0.08f, 0.06f, 0.05f, 1f));
        Stretch(bg.rectTransform, 0f, 0.2f, 1f, 0.8f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        var fa = (RectTransform)fillArea.transform;
        fa.SetParent(rt, false);
        Stretch(fa, 0f, 0.25f, 1f, 0.75f);
        fa.offsetMin = new Vector2(5f, 0f); fa.offsetMax = new Vector2(-5f, 0f);
        var fill = NewImage("Fill", fa, sliderFill, new Color32(236, 186, 72, 255));
        fill.rectTransform.sizeDelta = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        var ha = (RectTransform)handleArea.transform;
        ha.SetParent(rt, false);
        Stretch(ha, 0f, 0f, 1f, 1f);
        ha.offsetMin = new Vector2(10f, 0f); ha.offsetMax = new Vector2(-10f, 0f);
        var handle = NewImage("Handle", ha, sliderHandle, Cream);
        handle.rectTransform.sizeDelta = new Vector2(26f, 0f);
        handle.preserveAspect = sliderHandle != null;

        var s = go.GetComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        return s;
    }

    // ------------------------------------------------------------------ small helpers

    static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Image NewImage(string name, Transform parent, Sprite sprite, Color fallback)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        SetSprite(img, sprite);
        if (sprite == null) img.color = fallback;
        return img;
    }

    static void SetSprite(Image img, Sprite s)
    {
        img.sprite = s;
        img.type = s != null && s.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
    }

    static Text MakeText(string name, Transform parent, Font font, int size, Vector2 pos, Vector2 box, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Cream;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        go.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        return t;
    }

    static Font BuiltinFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        return f;
    }
}

/// <summary>A button that does one step when tapped and keeps going while it is held down.</summary>
public class HoldRepeat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Button button;
    public Action onTap;
    public Action<float> onHold;
    public float holdDelay = 0.3f;

    bool down;
    float heldFor;

    public void OnPointerDown(PointerEventData e)
    {
        if (button != null && !button.interactable) return;
        down = true;
        heldFor = 0f;
        onTap?.Invoke();
    }

    public void OnPointerUp(PointerEventData e) { down = false; }
    public void OnPointerExit(PointerEventData e) { down = false; }
    void OnDisable() { down = false; }

    void Update()
    {
        if (!down) return;
        if (button != null && !button.interactable) { down = false; return; }
        heldFor += Time.unscaledDeltaTime;
        if (heldFor > holdDelay) onHold?.Invoke(Time.deltaTime);
    }
}
