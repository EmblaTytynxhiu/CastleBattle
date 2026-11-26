using UnityEngine;

/// <summary>
/// Every sprite, prefab and sound the game code needs, in one place.
/// Created and filled automatically by "Tools > Castle Battle > 7. Create Game Prefabs + Assets".
/// </summary>
[CreateAssetMenu(menuName = "Castle Battle/Game Assets")]
public class GameAssets : ScriptableObject
{
    public static GameAssets I;

    [Header("Pieces")]
    public PieceCatalog redCatalog;
    public PieceCatalog blueCatalog;
    public GameObject kingRed, kingBlue;

    [Header("Grids (8x8, 12x12, 12x10, 16x16)")]
    public Sprite[] grids = new Sprite[4];

    [Header("Ammo (Cannonball, StoneBall, Fire, Explosive, Chain, Split)")]
    public Sprite[] ammo = new Sprite[6];
    public Sprite splitPiece;

    [Header("Effects")]
    public Sprite dust, smoke, spark, explosion, shockwave, splash, hitStar;
    public Sprite[] fire;
    public Sprite debrisWood, debrisStone, debrisMetal, debrisGlass, debrisIce;

    [Header("Sounds")]
    public AudioClip cannon, explosionSfx, hitWood, hitStone, hitMetal, breakWood, breakStone, breakGlass,
                     splashSfx, fireSfx, place, kingHit, victory, defeat, music;

    public Sprite Debris(PieceMaterial m)
    {
        switch (m)
        {
            case PieceMaterial.Stone: return debrisStone;
            case PieceMaterial.Metal: return debrisMetal;
            case PieceMaterial.Glass: return debrisGlass;
            case PieceMaterial.Ice: return debrisIce;
            default: return debrisWood;
        }
    }

    public AudioClip HitSound(PieceMaterial m)
    {
        switch (m)
        {
            case PieceMaterial.Stone: return hitStone;
            case PieceMaterial.Metal: return hitMetal;
            case PieceMaterial.Glass:
            case PieceMaterial.Ice: return hitMetal;
            default: return hitWood;
        }
    }

    public AudioClip BreakSound(PieceMaterial m)
    {
        switch (m)
        {
            case PieceMaterial.Stone:
            case PieceMaterial.Metal: return breakStone;
            case PieceMaterial.Glass:
            case PieceMaterial.Ice: return breakGlass;
            default: return breakWood;
        }
    }
}
