using System.Collections;
using UnityEngine;

/// <summary>
/// The computer opponent. Builds a sensible castle around its King and aims at your King.
/// It starts a little inaccurate and gets better every turn.
/// </summary>
public class AIPlayer : MonoBehaviour
{
    [Range(0f, 0.4f)] public float startError = 0.14f;
    public float minError = 0.03f;
    public float improvePerTurn = 0.025f;
    public float thinkTime = 1.2f;

    float error = -1f;

    // ------------------------------------------------------------ building
    // Catalog order (each tab): 0 Block1x1, 1 Block2x1, 2 Block3x1, 3 Block1x2, 4 Block2x2,
    // 5 Tri1x1, 6 Tri2x2, 7 Roof2x1, 8 Arch2x1, 9 Circle, 10 Battlement1x1, 11 Battlement2x1
    const int WOOD = 0, STONE = 1, METAL = 2;

    BuildManager target;
    int used, limit;

    bool P(int cat, int idx, int x, int y)
    {
        if (used >= limit) return false;
        if (target.TryPlace(cat, idx, x, y)) { used++; return true; }
        return false;
    }

    public void BuildCastle(BuildManager bm, int pieces)
    {
        target = bm; used = 0; limit = pieces;
        int c = bm.cols, r = bm.rows, k = c / 2;

        // 1. a strong box around the King
        P(STONE, 3, k - 1, 0); P(STONE, 3, k + 1, 0);
        if (!P(METAL, 2, k - 1, 2)) P(STONE, 2, k - 1, 2);
        P(STONE, 11, k - 1, 3);
        // 2. outer towers
        int towerH = Mathf.Clamp(r - 3, 2, 8);
        for (int y = 0; y + 1 < towerH; y += 2) { P(STONE, 4, 0, y); P(STONE, 4, c - 2, y); }
        int ty = (towerH / 2) * 2;
        P(STONE, 11, 0, ty); P(STONE, 11, c - 2, ty);
        // 3. walls between towers and the King box
        for (int x = 2; x < c - 2; x++)
        {
            if (x >= k - 1 && x <= k + 1) continue;
            if (!P(WOOD, 3, x, 0)) P(WOOD, 0, x, 0);
        }
        for (int x = 2; x < c - 2; x += 2)
        {
            if (x + 1 >= k - 1 && x <= k + 1) continue;
            if (!P(STONE, 1, x, 2)) P(WOOD, 1, x, 2);
        }
        // 4. defenses on top (deflectors face the enemy side)
        P(3, 4, bm.faceLeft ? 2 : c - 3, 3);
        for (int x = 2; x < c - 2; x += 2) P(STONE, 11, x, 3);
        // 5. spend what is left on extra layers
        for (int y = 4; y < r && used < limit; y++)
            for (int x = 0; x < c && used < limit; x++)
                if (Random.value < 0.35f) P(Random.value < 0.5f ? STONE : WOOD, Random.value < 0.6f ? 0 : 1, x, y);
    }

    // ------------------------------------------------------------ shooting
    public IEnumerator TakeTurn(CannonController cannon, Vector2 target)
    {
        if (error < 0f) error = startError;
        yield return new WaitForSeconds(thinkTime);

        // pick ammo
        int[] choices = { 1, 2, 3, 4, 5 };
        int pick = 0;
        if (Random.value < 0.55f)
        {
            int c = choices[Random.Range(0, choices.Length)];
            if (cannon.HasAmmo((AmmoType)c)) pick = c;
        }
        cannon.SelectAmmo(pick);

        Vector2 v = Solve(cannon.MuzzlePos, target, cannon.faceLeft, cannon.maxPower);
        v *= 1f + Random.Range(-error, error);
        v = Quaternion.Euler(0f, 0f, Random.Range(-error, error) * 20f) * v;

        // swing the barrel into position
        float t = 0f;
        Vector2 from = cannon.faceLeft ? Vector2.left : Vector2.right;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            cannon.AimBarrel(Vector2.Lerp(from, v.normalized, t / 0.7f).normalized);
            yield return null;
        }
        cannon.Fire(v);
        error = Mathf.Max(minError, error - improvePerTurn);
    }

    static Vector2 Solve(Vector2 from, Vector2 to, bool faceLeft, float maxPower)
    {
        float g = -Physics2D.gravity.y;
        float dx = Mathf.Abs(to.x - from.x);
        float dy = to.y - from.y;
        float f = faceLeft ? -1f : 1f;
        for (float deg = 35f; deg <= 75f; deg += 5f)
        {
            float a = deg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a), tan = Mathf.Tan(a);
            float denom = 2f * cos * cos * (dx * tan - dy);
            if (denom <= 0f) continue;
            float speed = Mathf.Sqrt(g * dx * dx / denom);
            if (speed <= maxPower) return new Vector2(Mathf.Cos(a) * f, Mathf.Sin(a)) * speed;
        }
        float r = 45f * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r) * f, Mathf.Sin(r)) * maxPower;
    }
}
