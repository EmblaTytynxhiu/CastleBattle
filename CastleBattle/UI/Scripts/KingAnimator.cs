using UnityEngine;

/// <summary>
/// Plays the Castle Battle King's frame animations on a SpriteRenderer.
///   king.Play(KingAnimator.Anim.Walk);   king.PlayFall();   king.faceLeft = true;
/// "Tools > Castle Battle > 4. Add Animated Kings To Scene" sets this up for you.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class KingAnimator : MonoBehaviour
{
    public enum Anim { Idle, Walk, Cheer, Fall }

    [Header("Frames (filled in by the builder)")]
    public Sprite[] idle;
    public Sprite[] walk;
    public Sprite[] cheer;
    public Sprite[] fall;

    [Header("Speed (frames per second)")]
    public float idleFps = 8f, walkFps = 10f, cheerFps = 10f, fallFps = 10f;

    [Header("Options")]
    public Anim startAnimation = Anim.Idle;
    public bool faceLeft;

    public Anim Current { get; private set; }
    public bool IsFinished => Current == Anim.Fall && timer * fallFps >= fall.Length;

    SpriteRenderer sr;
    Sprite[] frames;
    float fps, timer;
    bool loop;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Play(startAnimation);
    }

    public void Play(Anim a)
    {
        Current = a;
        timer = 0f;
        switch (a)
        {
            case Anim.Walk: frames = walk; fps = walkFps; loop = true; break;
            case Anim.Cheer: frames = cheer; fps = cheerFps; loop = true; break;
            case Anim.Fall: frames = fall; fps = fallFps; loop = false; break;
            default: frames = idle; fps = idleFps; loop = true; break;
        }
        Apply();
    }

    public void PlayIdle() { Play(Anim.Idle); }
    public void PlayWalk() { Play(Anim.Walk); }
    public void PlayCheer() { Play(Anim.Cheer); }
    public void PlayFall() { Play(Anim.Fall); }

    void Update()
    {
        timer += Time.deltaTime;
        Apply();
    }

    void Apply()
    {
        if (sr == null) return;
        sr.flipX = faceLeft;
        if (frames == null || frames.Length == 0) return;
        int f = Mathf.FloorToInt(timer * fps);
        f = loop ? f % frames.Length : Mathf.Min(f, frames.Length - 1);
        sr.sprite = frames[f];
    }
}
