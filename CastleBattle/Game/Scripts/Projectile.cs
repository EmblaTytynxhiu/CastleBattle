using System.Collections.Generic;
using UnityEngine;

public enum AmmoType { Cannonball, StoneBall, Fire, Explosive, Chain, Split }

/// <summary>
/// A cannon shot. Behaviour by ammo type:
///  Cannonball – normal.   StoneBall – heavy, strong against wood.   Fire – sets wood on fire.
///  Explosive – blows up on impact (area damage + push).   Chain – spinning, shreds thin pieces.
///  Split – click while it flies to split into 3 (like the yellow bird).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public static readonly List<Projectile> Active = new List<Projectile>();

    public AmmoType type;
    public int team;
    public bool canSplit;          // true when a human fired a Split shot

    Rigidbody2D rb;
    float life, slowTime;
    bool hitSomething, done;

    // ------------------------------------------------------------ spawning
    public static Projectile Spawn(AmmoType type, Vector2 pos, Vector2 velocity, int team, bool humanShot)
    {
        var ga = GameAssets.I;
        var go = new GameObject("Shot_" + type);
        go.transform.position = pos;
        go.layer = 0;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 30;
        Sprite s = ga != null ? ga.ammo[(int)type] : null;
        sr.sprite = s;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        float worldSize = 0.5f, mass = 2f;
        switch (type)
        {
            case AmmoType.StoneBall: worldSize = 0.55f; mass = 3.5f; break;
            case AmmoType.Fire: worldSize = 1.0f; mass = 1.5f; break;
            case AmmoType.Explosive: worldSize = 0.75f; mass = 2f; break;
            case AmmoType.Chain: worldSize = 1.9f; mass = 2.5f; break;
            case AmmoType.Split: worldSize = 0.5f; mass = 1.8f; break;
        }
        if (s != null)
        {
            float sz = Mathf.Max(s.bounds.size.x, s.bounds.size.y);
            go.transform.localScale = Vector3.one * (worldSize / Mathf.Max(0.01f, sz));
        }
        float scale = go.transform.localScale.x;
        if (type == AmmoType.Chain)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.85f, 0.4f) / scale;
            rb.angularVelocity = 720f;
        }
        else
        {
            var cc = go.AddComponent<CircleCollider2D>();
            cc.radius = (type == AmmoType.Fire ? 0.2f : worldSize * 0.46f) / scale;
            if (type == AmmoType.Fire) cc.offset = new Vector2(0.24f, 0f) / scale;
        }
        rb.mass = mass;
        rb.SetVel(velocity);

        var p = go.AddComponent<Projectile>();
        p.type = type;
        p.team = team;
        p.canSplit = type == AmmoType.Split && humanShot;
        return p;
    }

    void Awake() { rb = GetComponent<Rigidbody2D>(); }
    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    void Update()
    {
        life += Time.deltaTime;
        Vector2 v = rb.Vel();
        if (type == AmmoType.Fire && v.sqrMagnitude > 1f)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
            if (Random.value < 0.4f && GameAssets.I != null)
                Fx.Show(GameAssets.I.fire != null && GameAssets.I.fire.Length > 0 ? GameAssets.I.fire[0] : null, transform.position, 0.4f, 0.3f, 0.4f);
        }
        if (canSplit && !hitSomething && life > 0.15f && GameInput.LeftDown && Time.timeScale > 0f) DoSplit();

        slowTime = v.magnitude < 0.3f ? slowTime + Time.deltaTime : 0f;
        Vector3 pos = transform.position;
        if (slowTime > 1f || life > 9f || pos.y < -12f || Mathf.Abs(pos.x) > 90f) Finish();
    }

    public float DamageMultiplier(Block b)
    {
        switch (type)
        {
            case AmmoType.StoneBall: return b.material == PieceMaterial.Wood ? 1.6f : 1.15f;
            case AmmoType.Fire: return 0.6f;
            case AmmoType.Explosive: return 0.5f;
            case AmmoType.Chain: return (b.cols == 1 || b.rows == 1) ? 1.9f : 1f;
            case AmmoType.Split: return 0.8f;
            default: return 1f;
        }
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (done) return;
        bool first = !hitSomething;
        hitSomething = true;
        Vector2 p = c.contactCount > 0 ? c.GetContact(0).point : (Vector2)transform.position;
        if (first) Fx.Dust(p, 0.9f);
        var block = c.collider.GetComponentInParent<Block>();
        if (type == AmmoType.Fire && first)
        {
            if (block != null) block.Ignite();
            foreach (var col in Physics2D.OverlapCircleAll(p, 1.2f))
            {
                var b = col.GetComponentInParent<Block>();
                if (b != null && Random.value < 0.5f) b.Ignite();
            }
        }
        if (type == AmmoType.Explosive) Explode(p);
        if (type == AmmoType.Chain && first) Fx.Sparks(p, 6);
    }

    public void Explode(Vector2 p)
    {
        if (done) return;
        const float radius = 2.8f;
        Fx.Explosion(p, radius);
        if (GameAssets.I != null) AudioManager.Play(GameAssets.I.explosionSfx, 1f, 0.05f);
        foreach (var col in Physics2D.OverlapCircleAll(p, radius))
        {
            var body = col.attachedRigidbody;
            if (body == null || body == rb) continue;
            Vector2 d = body.position - p;
            float k = 1f - Mathf.Clamp01(d.magnitude / radius);
            if (body.bodyType == RigidbodyType2D.Dynamic)
                body.AddForce(d.normalized * 30f * k + Vector2.up * 6f * k, ForceMode2D.Impulse);
            var b = col.GetComponentInParent<Block>();
            if (b != null && Block.DamageEnabled) b.TakeDamage(130f * k);
            var king = col.GetComponentInParent<KingUnit>();
            if (king != null && Block.DamageEnabled) king.TakeDamage(60f * k);
        }
        Finish();
    }

    void DoSplit()
    {
        canSplit = false;
        Vector2 v = rb.Vel();
        for (int i = -1; i <= 1; i++)
        {
            Vector2 nv = Quaternion.Euler(0f, 0f, i * 12f) * (v * 1.05f);
            var piece = Spawn(AmmoType.Cannonball, (Vector2)transform.position + (Vector2)(Quaternion.Euler(0, 0, 90) * v.normalized) * i * 0.3f, nv, team, false);
            piece.GetComponent<Rigidbody2D>().mass = 1f;
            if (GameAssets.I != null && GameAssets.I.splitPiece != null)
            {
                var sr = piece.GetComponent<SpriteRenderer>();
                sr.sprite = GameAssets.I.splitPiece;
                float sz = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                piece.transform.localScale = Vector3.one * (0.35f / Mathf.Max(0.01f, sz));
                piece.GetComponent<CircleCollider2D>().radius = 0.16f / piece.transform.localScale.x;
            }
        }
        Fx.Sparks(transform.position, 8);
        if (GameAssets.I != null) AudioManager.Play(GameAssets.I.cannon, 0.4f, 0.2f);
        Finish();
    }

    void Finish()
    {
        if (done) return;
        done = true;
        Destroy(gameObject);
    }
}
