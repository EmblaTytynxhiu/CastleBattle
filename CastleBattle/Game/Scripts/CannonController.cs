using UnityEngine;

/// <summary>
/// A team's cannon. It stands on the ground in front of its castle.
///
/// The cannon always has an ANGLE (0-80 degrees) and a POWER (0-100 %). On a human turn you can set them
/// three ways, and the dotted arc always shows where the shot will go:
///   1. MOUSE  - press anywhere (not on a button), drag BACK away from the enemy like a slingshot, let go to fire.
///   2. PANEL  - the ANGLE / POWER buttons and sliders under your castle, then press FIRE.
///   3. KEYS   - W/S or Up/Down = angle, A/D or Left/Right = power, Space = fire, 1-6 = ammo.
/// The computer player calls AimBarrel() + Fire() itself.
/// </summary>
public class CannonController : MonoBehaviour
{
    public int team;
    public bool faceLeft;
    public Transform barrel;
    public Transform muzzle;

    [Header("Power (launch speed)")]
    public float minPower = 7f;
    public float maxPower = 27f;
    public float maxDrag = 4.5f;

    [Header("Aim limits")]
    public float minAngle = 0f;
    public float maxAngle = 80f;
    [Tooltip("Degrees per second when holding an angle button / key.")]
    public float angleSpeed = 35f;
    [Tooltip("Power % per second when holding a power button / key.")]
    public float powerSpeed = 45f;

    [Header("Current aim (changes while you play)")]
    [Range(0f, 80f)] public float angle = 35f;
    [Range(0f, 100f)] public float powerPercent = 60f;

    [Header("Ammo (Cannonball, StoneBall, Fire, Explosive, Chain, Split)")]
    public AmmoType selectedAmmo = AmmoType.Cannonball;
    public int[] ammo = { 99, 3, 2, 2, 2, 2 };

    public bool HasFired { get; private set; }
    public bool IsMyTurn { get; private set; }
    public bool IsHumanTurn => IsMyTurn && human;
    public Vector2 MuzzlePos => muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position;

    /// <summary>Launch speed for the current power %.</summary>
    public float LaunchSpeed => Mathf.Lerp(minPower, maxPower, powerPercent / 100f);

    /// <summary>World-space direction of the current angle (mirrored for the cannon that faces left).</summary>
    public Vector2 AimDirection
    {
        get
        {
            float r = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r) * (faceLeft ? -1f : 1f), Mathf.Sin(r));
        }
    }

    public Vector2 AimVelocity => AimDirection * LaunchSpeed;

    bool human, dragging;
    Vector2 pressWorld;
    TrajectoryDots dots;
    Camera cam;
    float recoil;
    Vector3 barrelBase;

    void Awake()
    {
        dots = gameObject.AddComponent<TrajectoryDots>();
        if (barrel) barrelBase = barrel.localPosition;
    }

    void Start()
    {
        AimBarrel(AimDirection);
    }

    // ------------------------------------------------------------------ turn

    public void BeginTurn(bool isHuman)
    {
        IsMyTurn = true;
        human = isHuman;
        HasFired = false;
        dragging = false;
        if (!HasAmmo(selectedAmmo)) selectedAmmo = AmmoType.Cannonball;
        AimBarrel(AimDirection);
    }

    public bool HasAmmo(AmmoType t) => t == AmmoType.Cannonball || ammo[(int)t] > 0;

    public void SelectAmmo(int i)
    {
        if (i < 0 || i >= ammo.Length) return;
        if (HasAmmo((AmmoType)i)) selectedAmmo = (AmmoType)i;
    }

    // ------------------------------------------------------------------ aim controls (used by the panel, keys and mouse)

    public void SetAngle(float a)
    {
        angle = Mathf.Clamp(a, minAngle, maxAngle);
        AimBarrel(AimDirection);
    }

    public void SetPower(float percent)
    {
        powerPercent = Mathf.Clamp(percent, 0f, 100f);
    }

    public void AdjustAngle(float delta) { SetAngle(angle + delta); }
    public void AdjustPower(float delta) { SetPower(powerPercent + delta); }

    /// <summary>Fires with the current angle and power (the panel's FIRE button and the Space key).</summary>
    public void FireCurrent()
    {
        if (!IsHumanTurn || Time.timeScale == 0f) return;
        dragging = false;
        dots.Hide();
        Fire(AimVelocity);
    }

    // ------------------------------------------------------------------ update

    void Update()
    {
        if (barrel && recoil > 0f)
        {
            recoil = Mathf.Max(0f, recoil - Time.deltaTime * 2f);
            barrel.localPosition = barrelBase - barrel.localRotation * Vector3.right * recoil * 0.4f;
        }

        if (!IsMyTurn || !human)
        {
            if (dots) dots.Hide();
            return;
        }
        if (Time.timeScale == 0f) return;
        if (cam == null) cam = Camera.main;

        // ammo keys
        for (int i = 1; i <= 6; i++) if (GameInput.NumberDown(i)) SelectAmmo(i - 1);

        // keyboard aim
        float aKey = GameInput.AngleAxis, pKey = GameInput.PowerAxis;
        if (aKey != 0f) AdjustAngle(aKey * angleSpeed * Time.deltaTime);
        if (pKey != 0f) AdjustPower(pKey * powerSpeed * Time.deltaTime);

        // mouse slingshot aim
        if (GameInput.LeftDown && !GameInput.OverUI)
        {
            dragging = true;
            pressWorld = GameInput.MouseWorld(cam);
        }
        if (dragging)
        {
            Vector2 drag = pressWorld - GameInput.MouseWorld(cam);
            float d = Mathf.Min(drag.magnitude, maxDrag);
            if (d > 0.25f)
            {
                float f = faceLeft ? -1f : 1f;
                SetAngle(Mathf.Atan2(drag.y, drag.x * f) * Mathf.Rad2Deg);
                SetPower(d / maxDrag * 100f);
            }
            if (GameInput.LeftUp)
            {
                dragging = false;
                if (d > 0.25f) { Fire(AimVelocity); return; }
            }
        }

        // keyboard fire
        if (GameInput.FireDown && !dragging) { Fire(AimVelocity); return; }

        // always show where the current aim will land
        dots.Show(MuzzlePos, AimVelocity);
    }

    /// <summary>Points the barrel along a world direction (the cannon root is mirrored when facing left).</summary>
    public void AimBarrel(Vector2 dir)
    {
        if (!barrel) return;
        float a = Mathf.Atan2(dir.y, dir.x * (faceLeft ? -1f : 1f)) * Mathf.Rad2Deg;
        barrel.localRotation = Quaternion.Euler(0f, 0f, a);
    }

    public void Fire(Vector2 velocity)
    {
        if (!IsMyTurn) return;

        // keep the panel numbers in step with the shot (useful for the computer's turn too)
        float f = faceLeft ? -1f : 1f;
        angle = Mathf.Clamp(Mathf.Atan2(velocity.y, velocity.x * f) * Mathf.Rad2Deg, minAngle, maxAngle);
        powerPercent = Mathf.Clamp01(Mathf.InverseLerp(minPower, maxPower, velocity.magnitude)) * 100f;
        AimBarrel(velocity);

        if (!HasAmmo(selectedAmmo)) selectedAmmo = AmmoType.Cannonball;
        if (selectedAmmo != AmmoType.Cannonball) ammo[(int)selectedAmmo]--;
        Projectile.Spawn(selectedAmmo, MuzzlePos, velocity, team, human);
        Fx.Smoke(MuzzlePos, 1.4f);
        Fx.Sparks(MuzzlePos, 5);
        if (GameAssets.I != null) AudioManager.Play(GameAssets.I.cannon, 1f, 0.06f);
        recoil = 1f;
        dots.Hide();
        HasFired = true;
        IsMyTurn = false;
        if (!HasAmmo(selectedAmmo)) selectedAmmo = AmmoType.Cannonball;
    }

    // ------------------------------------------------------------------ placement

    /// <summary>
    /// Stands the cannon on the ground at world x: the bottom of its wheels touches the ground surface.
    /// Uses a raycast so it works even if you move or resize the Ground object.
    /// </summary>
    public void PlaceOnGround(float x, float fallbackGroundY = 0f)
    {
        float groundY = fallbackGroundY;
        var hits = Physics2D.RaycastAll(new Vector2(x, 50f), Vector2.down, 200f);
        foreach (var h in hits)
        {
            if (h.collider.isTrigger) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<Block>() != null) continue;      // ignore castle pieces
            if (h.collider.GetComponentInParent<KingUnit>() != null) continue;
            groundY = h.point.y;
            break;
        }

        // how far below the cannon's pivot its wheels reach (the Carriage sprite, or every sprite if there is none)
        float bottomOffset = 0f;
        var carriage = transform.Find("Carriage");
        var renderers = carriage != null ? carriage.GetComponentsInChildren<SpriteRenderer>()
                                         : GetComponentsInChildren<SpriteRenderer>();
        float bottom = float.MaxValue;
        foreach (var r in renderers)
        {
            if (r.sprite == null || r.name.StartsWith("AimDot")) continue;
            bottom = Mathf.Min(bottom, r.bounds.min.y);
        }
        if (bottom != float.MaxValue) bottomOffset = transform.position.y - bottom;

        transform.position = new Vector3(x, groundY + bottomOffset, transform.position.z);
    }
}

/// <summary>The dotted arc that shows where a shot will go.</summary>
public class TrajectoryDots : MonoBehaviour
{
    public int count = 26;
    public float step = 0.075f;
    SpriteRenderer[] dots;
    static Sprite dotSprite;

    void Awake()
    {
        if (dotSprite == null)
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    float a = Mathf.Clamp01(15f - d);
                    bool rim = d > 11.5f;
                    tex.SetPixel(x, y, rim ? new Color(0.15f, 0.1f, 0.08f, a) : new Color(1f, 0.97f, 0.85f, a));
                }
            tex.Apply();
            dotSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }
        dots = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("AimDot" + i);
            go.transform.SetParent(transform, false);
            dots[i] = go.AddComponent<SpriteRenderer>();
            dots[i].sprite = dotSprite;
            dots[i].sortingOrder = 70;
            dots[i].enabled = false;
        }
    }

    public void Show(Vector2 start, Vector2 velocity)
    {
        Vector2 g = Physics2D.gravity;
        for (int i = 0; i < dots.Length; i++)
        {
            float t = (i + 1) * step;
            Vector2 p = start + velocity * t + 0.5f * g * t * t;
            dots[i].transform.position = p;
            float s = Mathf.Lerp(0.32f, 0.12f, i / (float)dots.Length);
            dots[i].transform.localScale = Vector3.one * s / Mathf.Abs(transform.lossyScale.y);
            dots[i].enabled = p.y > -1f;          // don't draw dots under the ground
        }
    }

    public void Hide()
    {
        if (dots == null) return;
        foreach (var d in dots) d.enabled = false;
    }
}
