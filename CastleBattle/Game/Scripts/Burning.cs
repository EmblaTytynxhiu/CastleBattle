using UnityEngine;

/// <summary>Fire on a wooden piece: hurts it over time and can spread to nearby wood. Water barrels put it out.</summary>
public class Burning : MonoBehaviour
{
    public float duration = 6f;
    public float damagePerSecond = 10f;
    public float spreadRadius = 1.3f;
    public float spreadChance = 0.25f;

    Block block;
    SpriteRenderer flame;
    float t, spreadTimer;

    public static void Ignite(Block b)
    {
        if (b == null || b.material != PieceMaterial.Wood) return;
        var existing = b.GetComponent<Burning>();
        if (existing != null) { existing.t = 0f; return; }
        b.gameObject.AddComponent<Burning>();
        if (GameAssets.I != null) AudioManager.Play(GameAssets.I.fireSfx, 0.7f);
    }

    void Start()
    {
        block = GetComponent<Block>();
        var go = new GameObject("Flame");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        flame = go.AddComponent<SpriteRenderer>();
        var host = GetComponent<SpriteRenderer>();
        flame.sortingOrder = (host ? host.sortingOrder : 5) + 2;
        float size = Mathf.Max(block ? Mathf.Max(block.cols, block.rows) : 1, 1) * 0.6f + 0.4f;
        go.transform.localScale = Vector3.one * size;
        go.transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        t += Time.deltaTime;
        var fr = GameAssets.I != null ? GameAssets.I.fire : null;
        if (flame != null && fr != null && fr.Length > 0)
        {
            flame.sprite = fr[Mathf.FloorToInt(Time.time * 10f) % fr.Length];
            flame.transform.rotation = Quaternion.identity;   // flames always point up
        }
        if (block != null && Block.DamageEnabled) block.TakeDamage(damagePerSecond * Time.deltaTime);

        spreadTimer += Time.deltaTime;
        if (spreadTimer >= 1f)
        {
            spreadTimer = 0f;
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, spreadRadius))
            {
                var b = col.GetComponentInParent<Block>();
                if (b != null && b != block && b.GetComponent<Burning>() == null && Random.value < spreadChance) b.Ignite();
            }
        }
        if (t >= duration) Extinguish();
    }

    public void Extinguish()
    {
        if (flame != null) Destroy(flame.gameObject);
        Fx.Smoke(transform.position, 1f);
        Destroy(this);
    }
}
