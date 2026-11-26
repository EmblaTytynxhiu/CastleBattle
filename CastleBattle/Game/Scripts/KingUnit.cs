using UnityEngine;

/// <summary>
/// The King in the castle. He loses when his health runs out, when he is knocked over (tilted too far)
/// or when he falls off the map. Uses KingAnimator for Idle / Cheer / Fall.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class KingUnit : MonoBehaviour
{
    public int team;                    // 0 = Red, 1 = Blue
    public float maxHealth = 100f;
    public float knockOverAngle = 65f;

    public float Health { get; private set; }
    public bool IsDefeated { get; private set; }

    Rigidbody2D rb;
    KingAnimator anim;
    float lastHurtSound;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<KingAnimator>();
        Health = maxHealth;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (!Block.DamageEnabled || IsDefeated) return;
        float impulse = 0f;
        for (int i = 0; i < c.contactCount; i++) impulse += c.GetContact(i).normalImpulse;
        float dmg = impulse * 5f * (c.collider.GetComponentInParent<Projectile>() != null ? 1.5f : 1f);
        if (dmg >= 5f) TakeDamage(dmg);
    }

    public void TakeDamage(float amount)
    {
        if (IsDefeated) return;
        Health -= amount;
        if (Time.time - lastHurtSound > 0.3f && GameAssets.I != null)
        {
            lastHurtSound = Time.time;
            AudioManager.Play(GameAssets.I.kingHit, 0.9f);
            Fx.Show(GameAssets.I.hitStar, (Vector2)transform.position + Vector2.up * 2f, 0.5f, 0.6f, 1.2f, Vector2.up);
        }
        if (Health <= 0f) IsDefeated = true;
    }

    void Update()
    {
        if (IsDefeated || !Block.DamageEnabled) return;
        float tilt = Mathf.Abs(Mathf.DeltaAngle(0f, transform.eulerAngles.z));
        if (tilt > knockOverAngle || transform.position.y < -4f) IsDefeated = true;
    }

    void Freeze()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.SetVel(Vector2.zero);
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
        if (transform.position.y < 0f) transform.position = new Vector3(transform.position.x, 0f, 0f);
    }

    public void PlayLoss()
    {
        Freeze();
        if (anim) anim.PlayFall();
    }

    public void PlayWin()
    {
        Freeze();
        if (anim) anim.PlayCheer();
    }

    public void SetFacing(bool left)
    {
        if (anim) anim.faceLeft = left;
    }
}
