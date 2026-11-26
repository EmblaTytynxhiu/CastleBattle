# Castle Siege – Cartoon Medieval Asset Pack v2 (Unity 2D)

All sprites are transparent PNGs. **128 px = 1 block.**

## Unity import settings (select all PNGs)
- Texture Type: **Sprite (2D and UI)** · Sprite Mode: **Single**
- **Pixels Per Unit: 128** → 1 block = 1 Unity unit, so the build grid is 8 x 8 units
- Filter: Bilinear · Compression: None or High Quality · Mesh Type: Tight
- Background layers: use PPU 100 or scale them to fit your camera.
- `Rope_Segment` / `Chain_Segment`: set Wrap Mode = Repeat and Draw Mode = Tiled (they tile vertically).

## Folder layout
```
Red/ and Blue/          (same layout for both teams)
  Pieces/<Material>/    Wood, Stone, Metal, Glass, Ice – 18 shapes each
  Pieces/<Material>/Damage/  _Damage1 (cracked), _Damage2 (broken), _Damage3 (crumbling)
  King/                 King_<Team>_Normal / Scared / Hit / Falling   (1 x 2)
  Cannon/               Barrel (rotates), Carriage, Full
  Parts/                Hinge, Spring (+Compressed), Wheel 1x1 & 2x2, Counterweight 1x1 & 2x2, Lever 3x0.5 & 4x0.5
  Defenses/             Deflector 1x1/2x2/3x3 (+Damage), Shield, Turret_Base, Turret_Barrel
  Units/                Archer, Guard  (1 x 1)
  Props/                Flag (1 x 2)
Shared/
  Ammo/        Cannonball, StoneBall, FireShot (+ FireShot_Ball), ExplosiveBarrel, ChainShot, SplitShot (+ SplitShot_Piece), Arrow
  Parts/       Rope_Segment, Chain_Segment, Chain_Link, Hook_Anchor
  Defenses/    WaterBarrel
  Particles/   Dust 1-3, Smoke 1-3, Fire 1-4 (animation frames), Spark, Explosion, Shockwave, HitStar,
               Debris (Wood, Stone, Metal, Glass, Ice), Water_Droplet, Water_Splash
  Grid/        BuildGrid_8x8, GridCell, GridCell_Valid, GridCell_Invalid
  Background/  BG_0_Sky, BG_1_FarHills (with distant castle), BG_2_NearHills, BG_3_Ground (tiles horizontally),
               Cloud 1-3, BG_Full_Preview
```

## Piece shapes
Block 1x1, 2x2, 3x3, 2x1, 3x1, 1x2, 1x3 · Triangle 1x1, 2x2, 3x3 · Roof 2x1, 3x2 · Arch 2x1, 3x2 · Circle 1x1 · Battlement 1x1, 2x1, 3x1

## Suggested gameplay numbers (starting points to tune)
| Material | Mass per block | Health | Notes |
|---|---|---|---|
| Ice | 0.6 | 20 | cheap, shatters, melts with fire (x3 damage) |
| Glass | 0.5 | 15 | cheapest, good bait |
| Wood | 1 | 40 | burns (fire x3), weak to stone ball (x1.5) |
| Stone | 2.5 | 90 | fire x0.3 |
| Metal | 4 | 150 | fire x0.2, use for Deflectors (low friction, bouncy material) |

| Ammo | Idea |
|---|---|
| Cannonball | normal |
| StoneBall | heavy (mass x2), slow launch, x1.5 vs wood |
| FireShot | sets wood on fire (spawn Fire particles, damage over time); WaterBarrel nearby puts it out |
| ExplosiveBarrel | on hit: Physics2D.OverlapCircle + AddExplosionForce-style impulse, spawn Explosion + Shockwave |
| ChainShot | two Rigidbody2D balls with a DistanceJoint2D, spinning; high damage to thin 1-wide pieces |
| SplitShot | on tap: destroy, spawn 3 SplitShot_Piece with spread velocity (yellow-bird style) |

**Damage stages:** swap the sprite at 75% / 50% / 25% health (Damage1/2/3), and spawn the matching Debris particles when the piece breaks.

**King:** swap to `Scared` when a shot is in the air nearby, `Hit` on impact, and `Falling` when his downward velocity gets high.

## Building-part hints
- Rope/chain: a chain of small Rigidbody2D links with HingeJoint2D. Use the segment sprites per link, and `Hook_Anchor` at the top.
- Hinge: HingeJoint2D anchor point (trap doors, drawbridges).
- Spring: SpringJoint2D, or swap to `Spring_Compressed` and apply an impulse to launch.
- Wheel: WheelJoint2D for battering rams and mobile towers.
- Lever + Counterweight: lever pivot is the hole in the centre (HingeJoint2D). Put a heavy counterweight on one end.

## Cannon & turret
- `Cannon_Barrel` pivot: about x = 0.17, y = 0.5. `Turret_Barrel` uses the same pivot and sits on top of `Turret_Base`.
- The sprites face right. Flip X for the other side.
