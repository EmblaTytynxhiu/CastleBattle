using UnityEngine;

/// <summary>Simple sprite effects: dust, smoke, debris, explosions, splashes. No particle systems needed.</summary>
public static class Fx
{
    public static GameObject Show(Sprite s, Vector2 pos, float worldSize, float life, float grow = 1.5f,
                                    Vector2 vel = default, float spin = 0f, int order = 40, bool gravity = false)
    {
        if (s == null) return null;
        var go = new GameObject("Fx_" + s.name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        float size = Mathf.Max(s.bounds.size.x, s.bounds.size.y);
        go.transform.localScale = Vector3.one * (worldSize / Mathf.Max(0.01f, size));
        var fx = go.AddComponent<FxSprite>();
        fx.life = life; fx.grow = grow; fx.spin = spin;
        if (gravity)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.SetVel(vel);
            rb.angularVelocity = spin;
            fx.spin = 0f;
        }
        else fx.velocity = vel;
        return go;
    }

    public static void Dust(Vector2 pos, float size)
    {
        if (GameAssets.I == null) return;
        Show(GameAssets.I.dust, pos, size, 0.6f, 1.8f, new Vector2(Random.Range(-0.3f, 0.3f), 0.4f));
    }

    public static void Smoke(Vector2 pos, float size)
    {
        if (GameAssets.I == null) return;
        Show(GameAssets.I.smoke, pos, size, 1.2f, 2f, new Vector2(Random.Range(-0.2f, 0.2f), 0.8f));
    }

    public static void Debris(Vector2 pos, PieceMaterial m, int count)
    {
        if (GameAssets.I == null) return;
        var s = GameAssets.I.Debris(m);
        for (int i = 0; i < count; i++)
        {
            Vector2 v = new Vector2(Random.Range(-4f, 4f), Random.Range(2f, 7f));
            Show(s, pos + Random.insideUnitCircle * 0.4f, Random.Range(0.25f, 0.5f), 1.6f, 1f, v, Random.Range(-500f, 500f), 45, true);
        }
    }

    public static void Explosion(Vector2 pos, float radius)
    {
        if (GameAssets.I == null) return;
        Show(GameAssets.I.explosion, pos, radius * 1.4f, 0.45f, 1.6f, default, 0f, 60);
        Show(GameAssets.I.shockwave, pos, radius * 0.8f, 0.4f, 3f, default, 0f, 59);
        for (int i = 0; i < 4; i++) Smoke(pos + Random.insideUnitCircle * radius * 0.4f, radius * 0.7f);
        for (int i = 0; i < 6; i++)
            Show(GameAssets.I.spark, pos, 0.35f, 0.5f, 0.6f, Random.insideUnitCircle.normalized * Random.Range(5f, 9f), 0f, 61);
    }

    public static void Splash(Vector2 pos)
    {
        if (GameAssets.I == null) return;
        Show(GameAssets.I.splash, pos + Vector2.up * 0.4f, 1.6f, 0.6f, 1.4f, default, 0f, 50);
    }

    public static void Sparks(Vector2 pos, int n)
    {
        if (GameAssets.I == null) return;
        for (int i = 0; i < n; i++)
            Show(GameAssets.I.spark, pos, 0.25f, 0.35f, 0.5f, Random.insideUnitCircle.normalized * Random.Range(3f, 6f), 0f, 61);
    }
}

/// <summary>Fades, grows, moves and then removes an effect sprite.</summary>
public class FxSprite : MonoBehaviour
{
    public float life = 1f, grow = 1.5f, spin;
    public Vector2 velocity;
    SpriteRenderer sr;
    Vector3 baseScale;
    float t;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / life);
        transform.localScale = baseScale * Mathf.Lerp(1f, grow, k);
        transform.position += (Vector3)(velocity * Time.deltaTime);
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        if (sr != null) { var c = sr.color; c.a = 1f - k * k; sr.color = c; }
        if (t >= life) Destroy(gameObject);
    }
}
