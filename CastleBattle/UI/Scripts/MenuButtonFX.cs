using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Makes menu buttons feel juicy: grow on hover, squash on press, optional sounds.
/// Works together with the Button's Sprite Swap (Normal/Highlighted/Pressed/Disabled sprites).
/// </summary>
public class MenuButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.05f;
    public float pressScale = 0.96f;
    public float speed = 14f;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    Vector3 baseScale;
    float target = 1f;
    bool hovering;
    Selectable selectable;

    void Awake()
    {
        baseScale = transform.localScale;
        selectable = GetComponent<Selectable>();
    }

    void OnDisable()
    {
        hovering = false;
        target = 1f;
        transform.localScale = baseScale;
    }

    void Update()
    {
        float s = Mathf.Lerp(transform.localScale.x / baseScale.x, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        transform.localScale = baseScale * s;
    }

    bool Active => selectable == null || selectable.IsInteractable();

    public void OnPointerEnter(PointerEventData e)
    {
        if (!Active) return;
        hovering = true; target = hoverScale; Play(hoverSound);
    }

    public void OnPointerExit(PointerEventData e) { hovering = false; target = 1f; }

    public void OnPointerDown(PointerEventData e)
    {
        if (!Active) return;
        target = pressScale; Play(clickSound);
    }

    public void OnPointerUp(PointerEventData e) { target = hovering ? hoverScale : 1f; }

    void Play(AudioClip clip)
    {
        if (clip == null) return;
        Vector3 pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, pos, MainMenuController.SfxVolume);
    }
}
