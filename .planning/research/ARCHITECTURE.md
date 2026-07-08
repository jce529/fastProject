# Architecture Research

**Domain:** Boss room content + VFX/audio polish integration into an existing Unity 6 (URP 2D) mobile platformer's room-chain generator
**Researched:** 2026-07-08
**Confidence:** HIGH (all claims verified directly against current source in this repo — no external ecosystem research was needed for this question)

> Supersedes the previous version of this file (dated 2026-05-27), which described a hypothetical v1.0 architecture before the current code existed. This version documents the actual, current v3.0-shipped codebase and how the v3.1 boss-room + VFX/audio milestone should integrate with it.

## Correction to Milestone Framing (important)

The milestone brief names `PortalEffectBuilder` and `HitSparkBuilder` as "existing VFX builders" that new audio should call into. Direct inspection of `Assets/Editor/PortalEffectBuilder.cs` and `Assets/Editor/HitSparkBuilder.cs` shows **both are Editor-only `[MenuItem]` prefab-construction tools** (they run once in the Editor to bake a `PortalEffect.prefab` / `HitSparkEffect.prefab` asset from existing sprites/animations). They do not run at play time and have no hook for triggering audio.

The actual **runtime** VFX components — the real integration points for audio — are:

| Runtime component | File | Plays at |
|---|---|---|
| `FloorTransitionEffect` | `Assets/Scripts/World/FloorTransitionEffect.cs` | Portal entry/exit (`PlayEntry`/`PlayExit`, called by `WorldGenerator.FloorTransitionSequence`) |
| `CombatController.SpawnHitSpark` | `Assets/Scripts/Player/CombatController.cs:354` | Dash-kill impact (called from `ExecuteDash`, alongside `_cameraFollow.Shake(...)` and `ScoreManager.AddKillScore()`) |
| `EnemyDeathEffect` | `Assets/Scripts/Enemy/EnemyDeathEffect.cs` | Enemy death fade-out (`AddComponent`+`StartCoroutine` from `MeleeEnemy.OnDashHit`/`RangedEnemy.OnDashHit`) |

Any audio work should hook these three, not the Editor builders. (The Editor builders may still be *touched* if a prefab needs a baked-in `AudioSource`, but that is not the recommended approach — see Anti-Patterns.)

Also confirmed via grep/glob: **zero** existing `AudioSource`/`AudioClip`/`AudioListener`/`AudioMixer` usage anywhere in `Assets/Scripts`, and zero `.wav`/`.mp3`/`.ogg` assets in the project. Audio is a fully greenfield addition, not an extension of anything existing.

## System Overview

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         WorldGenerator (MonoBehaviour, singleton)         │
│  Owns: _chain (LinkedList<room,corridor>), lookahead/lookbehind trimming  │
│  Per-room-instantiation hooks (existing):                                │
│    TrySpawnExitPortal(room)   — probabilistic child spawn + standby room  │
│    TrySpawnEnemies(room,floor)— EnemySpawner marker scan → Spawn+Activate │
│  NEW hooks this milestone:                                                │
│    SelectRoomPrefab(floor)    — centralizes room-pick, adds boss roll     │
│    TrySpawnBoss(room,floor)   — BossSpawner marker scan → Spawn+Activate  │
├──────────────────────────────────────────────────────────────────────────┤
│                    Room Prefab Contract (marker components)               │
│  RoomConnector(Left/Right)  CameraBound  ExitSpawnPoint  EnemySpawner*    │
│  NEW: BossSpawner (0 or 1 per boss-room prefab; boss rooms carry ZERO     │
│       EnemySpawner markers — that alone guarantees the "solo fight")     │
├──────────────────────────────────────────────────────────────────────────┤
│              Enemy Layer — IEnemy contract (unchanged, 3 members)         │
│  MeleeEnemy : MonoBehaviour, IEnemy      (existing FSM)                   │
│  RangedEnemy : MonoBehaviour, IEnemy     (existing FSM)                   │
│  NEW: BossEnemy : MonoBehaviour, IEnemy  (new FSM sibling, same contract) │
│  CombatController.ExecuteDash() targets ANY IEnemy generically —          │
│  boss needs NO changes to CombatController or IEnemy to be dash-killable │
├──────────────────────────────────────────────────────────────────────────┤
│                 Cosmetic Layer (self-attaching, coroutine-based)          │
│  EnemyDeathEffect   (existing) — SpriteMask rise + particles on death     │
│  NEW: EnemySpawnEffect — SpriteMask reveal on spawn (mirrors DeathEffect) │
│  FloorTransitionEffect (existing) — portal enter/exit mask animation      │
│  RuntimeMaskSprite (existing, shared, cached) — used by ALL of the above  │
├──────────────────────────────────────────────────────────────────────────┤
│               NEW: AudioManager (MonoBehaviour singleton, static API)     │
│  AudioManager.PlaySfx(AudioClip clip, float volume = 1f)                  │
│  Pooled AudioSource components; instance created once, DontDestroyOnLoad │
│  Called from: FloorTransitionEffect, CombatController, EnemyDeathEffect, │
│                EnemySpawnEffect, BossEnemy                                │
├──────────────────────────────────────────────────────────────────────────┤
│         Score/Floor static utilities (existing, unchanged contract)       │
│  ScoreManager.AddKillScore()      — generic, called by CombatController  │
│  NEW: ScoreManager.AddBossKillBonus() — called by BossEnemy itself        │
│  FloorTimer / FloorManager — untouched by this milestone                 │
└──────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | New / Modified |
|-----------|-----------------|-----------------|
| `WorldGenerator` | Centralize room-slot selection; add boss-room probabilistic roll parallel to the existing `TrySpawnExitPortal` pattern; call `TrySpawnBoss` at the same 4 sites `TrySpawnEnemies` is already called | **Modified** (highest blast radius — do last) |
| Boss room prefab (new `Room_Boss` under `Assets/Prefabs/Rooms/`) | Unique arena Tilemap layout + `RoomConnector` L/R + `CameraBound` + `ExitSpawnPoint` + single `BossSpawner` marker, **zero** `EnemySpawner` markers | **New** (content, built like the 6 `Complex_Room` variants) |
| `BossSpawner` | New marker component, structurally identical to `EnemySpawner` (Spawn/Activate two-phase) but for exactly one boss prefab reference | **New** (tiny — copy `EnemySpawner`'s shape) |
| `BossEnemy` | New FSM sibling of `MeleeEnemy`/`RangedEnemy` implementing `IEnemy` unchanged (`IsAlive`, `OnDashHit`, `ClearHighlight`); owns its own unique attack-pattern states; on `OnDashHit()` additionally calls `ScoreManager.AddBossKillBonus()` and plays its own death/audio cues before delegating to the same `EnemyDeathEffect` reuse pattern | **New** |
| `IEnemy` | **Not touched.** Its 3-member contract already covers boss dash-kill fully; do not add a 4th member or a parallel `IBoss` interface | **Unchanged** |
| `EnemySpawnEffect` | New component mirroring `EnemyDeathEffect`: `AddComponent` + `StartCoroutine` from an enemy's `Awake`/`Start`, reusing `RuntimeMaskSprite`, plays a short rise-in mask reveal | **New** |
| `AudioManager` | New MonoBehaviour singleton (`Instance`, static `PlaySfx` wrapper) providing the single call-in point for all new sound; pooled `AudioSource`s for overlapping one-shots | **New** |
| `FloorTransitionEffect` | Add `AudioClip` fields + `AudioManager.PlaySfx(...)` calls inside `PlayEntry`/`PlayExit`; fix any timing/feedback awkwardness called out in the milestone | **Modified** (small) |
| `CombatController` | Add `AudioClip` field + one `AudioManager.PlaySfx(...)` call beside the existing `SpawnHitSpark(destination)` line | **Modified** (small) |
| `EnemyDeathEffect` | Add `AudioClip` field + one `AudioManager.PlaySfx(...)` call in `PlayDeathSequence` | **Modified** (small) |
| `ScoreManager` | Add `AddBossKillBonus()` alongside existing `AddKillScore()`/`AddTimeBonus()` — same static, data-only pattern | **Modified** (small, additive only) |

## Key Architectural Decisions (answers to the research question)

### 1. Boss room: new dedicated Room prefab, selected via a SEPARATE probability roll — not added into `_roomPrefabs`

`WorldGenerator._roomPrefabs` is a flat, uniformly-random pool (`_roomPrefabs[Random.Range(0, _roomPrefabs.Length)]`) used at every room slot in `Start()`, `SpawnNextPair()`, `SpawnPrevPair()`, and again for the pre-spawned standby room inside `TrySpawnExitPortal()`. Simply appending a boss prefab to that array would make it appear with the same frequency as any of the 6 `Complex_Room` variants (~1-in-7 of *every* room slot, including the pre-spawned standby room for the *next* floor) — far too frequent for a "solo dedicated arena" encounter, with no floor-number gate and no protection against two boss rooms being active back-to-back (current chain + pre-spawned standby).

**Recommendation:** mirror the `ExitPortal` pattern exactly, since `WorldGenerator` already solves "rare, gated, at-most-N-concurrent special content attached to a room slot":
- New Inspector fields: `_bossRoomPrefab` (single `GameObject` for now; the milestone explicitly asks for "1 species, expandable framework" — an array is trivial to add later but a single field is honest about current scope), `_bossSpawnChance` (`[Range(0,1)]`, default low, e.g. 0.05–0.08), `_maxBossRoomsActive` (default 1), and a `_bossMinFloor` gate (e.g. floor ≥ 3) so the boss never appears on floor 1.
- Introduce `private GameObject SelectRoomPrefab(int floor)` that centralizes what is currently 4 duplicated `_roomPrefabs[Random.Range(...)]` call sites: it rolls for boss eligibility first (respecting `_maxBossRoomsActive` + `_bossMinFloor`, tracked via a new `_activeBossCount` field exactly like `_activeExitCount`), and falls back to the normal pool otherwise.
- The boss room prefab still needs `RoomConnector` L/R + `CameraBound` + `ExitSpawnPoint` — this is what lets it drop into `AlignByEntry`/`AlignByExit`/`RecomputeCameraBounds`/the `ExitSpawnPoint` teleport-in logic **for free**, with zero changes to those methods.
- **Solo fight is guaranteed by content, not by new gating code**: because the boss room prefab simply contains no `EnemySpawner` markers, `TrySpawnEnemies`'s `GetComponentsInChildren<EnemySpawner>` loop naturally finds nothing and spawns zero regular enemies there. No `if (isBossRoom) skip` branch is needed.
- Floor progression after the boss is defeated is **already solved**: `TrySpawnExitPortal(room)` is called unconditionally on every room (including a would-be boss room) at generation time, independent of enemy state — the portal is already sitting in the room waiting, exactly as the milestone requires ("층 진입은 기존 EXIT 포탈 그대로 필요"). No change needed here either.

### 2. Boss enemy: new `IEnemy` implementation, NOT a separate interface

`IEnemy` is deliberately minimal (`IsAlive`, `OnDashHit()`, `ClearHighlight()` — see the doc comment "D-01: ... are the sole interface members") and `CombatController.ExecuteDash()`/`FindNearestEnemyInRange()` interact with targets purely through this interface plus a `MonoBehaviour` cast for `transform`. A `BossEnemy : MonoBehaviour, IEnemy` sibling to `MeleeEnemy`/`RangedEnemy` gets the entire dash-target-search, highlight, and one-shot-kill pipeline **for free** as long as its `Collider2D` sits on the `Enemy` layer, exactly like the other two. There is no requirement in the milestone for multi-hit/phase HP, so introducing a parallel `IBoss` interface (or widening `IEnemy`) would violate the codebase's existing "정밀한 변경" (surgical change) principle and CLAUDE.md's minimal-interface intent for no concrete benefit. If a future milestone wants a phased/multi-hit boss, that is the point to revisit `IEnemy`, not now.

**Score bonus without touching `IEnemy` or `CombatController`:** `CombatController.ExecuteDash()` already calls `target.OnDashHit()` generically for any `IEnemy`, then unconditionally calls `ScoreManager.AddKillScore()` itself. Rather than adding a 4th `IEnemy` member (e.g. `int KillScoreValue`) to let `CombatController` differentiate, `BossEnemy.OnDashHit()` should simply call a new `ScoreManager.AddBossKillBonus()` **itself**, in addition to doing its own death/cleanup — `CombatController`'s generic flow (flat `AddKillScore()` + hit spark + camera shake + hit-freeze) still runs unmodified for every enemy including the boss; the boss's `OnDashHit()` override just does extra work on top, the same way `EnemyDeathEffect` self-attaches inside `OnDashHit` today.

### 3. Audio: MonoBehaviour singleton with a static convenience API — not a pure static class

Every existing "manager" in this codebase (`FloorManager`, `ScoreManager`, `FloorTimer`) is a pure static class explicitly described as "data-only, no scene lifecycle." That pattern cannot host `AudioSource` components, which must live on a `GameObject`/`MonoBehaviour`. The correct adaptation — and the smallest deviation from established convention — is a hybrid already used elsewhere in this codebase for exactly this reason: `WorldGenerator` is a `MonoBehaviour` that exposes `public static WorldGenerator Instance { get; private set; }` set in `Awake()`, letting other code call `WorldGenerator.Instance.EnterPortal(...)`.

**Recommendation:** `AudioManager : MonoBehaviour` with the same `Instance` pattern, plus a thin static wrapper for ergonomics:
```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [SerializeField] private AudioSource[] _pool; // 4-8 pooled one-shot sources

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (Instance == null || clip == null) return;
        Instance.PlayInternal(clip, volume);
    }

    private void PlayInternal(AudioClip clip, float volume)
    {
        foreach (var src in _pool)
            if (!src.isPlaying) { src.PlayOneShot(clip, volume); return; }
        _pool[0].PlayOneShot(clip, volume); // pool exhausted — steal the first
    }
}
```
Bootstrap it once, robust to the `MainMenu → AttackSelect → SampleScene` flow: either drop one `AudioManager` GameObject in `MainMenu` with `DontDestroyOnLoad` (consistent with `GameBootstrapper` always routing through `MainMenu` first), or use a `[RuntimeInitializeOnLoadMethod]` static bootstrap identical in spirit to `GameBootstrapper.EnsureMainMenu()`. Either is fine; the `DontDestroyOnLoad` GameObject approach is simpler to wire from the Inspector (drag in pooled `AudioSource`s) and is recommended.

**Call sites** (all additive, one `AudioClip` field + one `AudioManager.PlaySfx(...)` line each):
- `FloorTransitionEffect.PlayEntry`/`PlayExit` — portal whoosh/enter/exit stingers.
- `CombatController.ExecuteDash`, right beside the existing `SpawnHitSpark(destination)` call — hit-impact sound.
- `EnemyDeathEffect.PlayDeathSequence` — death sound (and a distinct one for `BossEnemy`'s own death, since `BossEnemy.OnDashHit()` can pass its own clip or bypass the shared component entirely if the boss needs a unique fanfare).
- New `EnemySpawnEffect` — spawn-in whoosh.
- `BossEnemy` — telegraph/attack/death cues unique to the boss, called directly (not through the shared `EnemyDeathEffect`/`EnemySpawnEffect` clip fields) since the milestone calls for the boss having entirely bespoke presentation.

### 4. Enemy spawn-in VFX: real timing constraint the roadmap must account for

`WorldGenerator.TrySpawnEnemies(room, floor)` calls `spawner.Spawn(...)` **immediately followed by** `spawner.Activate()` (`SetActive(true)`) in the same loop iteration, and this happens at **room-generation time** — i.e. up to `_lookaheadCount` (= 2) rooms ahead of the player, while that room is almost certainly off-screen. `EnemySpawner`'s own doc comment for `Activate()` still says "적 활성화 (FLOOR-03)" implying activation-on-entry, but the current `WorldGenerator` call site activates unconditionally at generation time — `FloorTransitionSequence` even has an explicit comment marking enemy activation as an intentional no-op at floor-entry ("Step 4 ... 의도적 no-op"). **Concretely: if a spawn-in VFX is simply added to `MeleeEnemy`/`RangedEnemy`'s `Awake()`/`OnEnable()`, it will, in the overwhelming majority of cases, finish playing 2 rooms before the player ever arrives and sees the enemy already idle-patrolling.** This is not a bug to fix silently — it is a real design tradeoff the roadmap should decide on explicitly:
- **Option A (recommended, lowest risk):** Add the spawn VFX anyway, purely cosmetic, cost ~0 (a short coroutine + a self-destroying `SpriteMask` GameObject, same shape as `EnemyDeathEffect`). Accept that for regular enemies it is usually invisible (harmless — no player-facing regression, no perf cost worth guarding against) but *is* visible in the cases that do matter: the very first room the player stands in at floor start (`Start()`'s `startRoom` — this one activates while the player is standing right there), and whenever the player back-tracks into a freshly `SpawnPrevPair()`-generated room. Do **not** restructure `WorldGenerator`'s proven activation timing to chase full visibility — that risks regressing the difficulty-scaling/memory-trim logic this milestone isn't scoped to touch.
- **Option B (higher payoff, higher risk):** For the **boss specifically**, don't reuse the generic `TrySpawnEnemies`-style immediate activation. Since the boss room is a single dedicated arena the player always walks fully into (unlike lookahead-generated rooms), gate `BossSpawner.Activate()` behind a small new trigger placed at the room's `ENT` `RoomConnector` (same `OnTriggerEnter2D` shape `ExitPortal` already uses) so the boss visibly "arrives" via the spawn VFX the moment the player steps into the arena — this is where the requested "portal-style spawn entrance" pays off, because it is guaranteed on-screen. This is a small, isolated addition (new trigger component on the boss room prefab) and does not touch `WorldGenerator`'s chain-generation code.

Recommendation: build Option A generically for all enemies (cheap, reusable, unblocks the spawn-VFX component early), and layer Option B on top specifically for the boss room once `BossEnemy` + boss room prefab exist, since that's the one place the effect's payoff (and the milestone's stated intent — "플레이어처럼 포탈을 타고 나오는 스폰 연출") is actually guaranteed to be seen.

## Data Flow

### Boss encounter flow

```
WorldGenerator.SelectRoomPrefab(floor)
    → boss roll passes (chance, floor gate, _activeBossCount < max)
    → Instantiate(_bossRoomPrefab) instead of normal pool pick
    → AlignByEntry/AlignByExit (existing, unchanged — RoomConnector contract satisfied)
    → TrySpawnExitPortal(room)      [existing, unmodified — portal placed regardless of boss state]
    → TrySpawnBoss(room, floor)     [NEW — mirrors TrySpawnEnemies structurally]
        → BossSpawner.Spawn(bossPrefab) → Instantiate inactive
        → BossSpawner.Activate()  (Option A: immediate)  OR
          arena-entry trigger fires BossSpawner.Activate() (Option B: on player entry)
    → BossEnemy.Awake() → AddComponent<EnemySpawnEffect>() → StartCoroutine(spawn VFX + AudioManager.PlaySfx)

Player enters arena → BossEnemy FSM runs its own unique attack-pattern states
    (structurally same shape as MeleeEnemy: Idle → Chase/Telegraph → Attack, but with
    boss-specific state count/behavior — no changes needed to CombatController)

Player dash-kills boss → CombatController.ExecuteDash(bossTarget)
    → target.OnDashHit()                      [BossEnemy override]
        → base one-shot-kill teardown (colliders off, rb static, isDead)
        → ScoreManager.AddBossKillBonus()      [NEW — boss-specific, called by BossEnemy itself]
        → AudioManager.PlaySfx(bossDeathClip)  [NEW]
        → AddComponent<EnemyDeathEffect>() + StartCoroutine(PlayDeathSequence) [reused as-is]
    → SpawnHitSpark(destination)               [existing, unmodified, runs for every enemy]
    → _cameraFollow.Shake(...)                 [existing, unmodified]
    → ScoreManager.AddKillScore()              [existing, unmodified — generic flat kill score, still applies]
    → HitFreeze coroutine                      [existing, unmodified]

Player reaches the ExitPortal already sitting in the (now-cleared) boss room
    → WorldGenerator.EnterPortal(portal) → FloorTransitionSequence (entirely existing, unmodified)
```

### Audio call flow (new)

```
Gameplay event (portal enter/exit, hit-kill, enemy death, enemy spawn, boss-specific cues)
    ↓
Owning component (FloorTransitionEffect / CombatController / EnemyDeathEffect / EnemySpawnEffect / BossEnemy)
    ↓ AudioManager.PlaySfx(clip, volume)
AudioManager.Instance.PlayInternal → first idle pooled AudioSource.PlayOneShot(clip)
```

## Architectural Patterns

### Pattern 1: Probabilistic room-slot overlay (existing — `ExitPortal`, extend for boss room)

**What:** A rare, capped-concurrency special element attached to a room slot at generation time, tracked via a simple `_activeXCount` counter compared against `_maxXActive`, and cleaned up in the exact same places normal chain trimming already happens (`RemoveTail`/`RemoveHead`/`FloorTransitionSequence`'s full-chain teardown).
**When to use:** Any new "sometimes this room slot gets special content" feature (boss room, in this milestone).
**Trade-offs:** Requires remembering to add the new counter's decrement everywhere the old `_activeExitCount` decrement already appears (3 places) — easy to miss one and leak the max-concurrency guarantee. Grep for `_activeExitCount` before finishing the boss-room change to confirm parity.

### Pattern 2: Self-attaching cosmetic coroutine component (existing — `EnemyDeathEffect`, extend for `EnemySpawnEffect`)

**What:** `owner.AddComponent<Effect>()` immediately followed by `StartCoroutine(effect.Play(...))`, fully self-contained (spawns its own temporary `GameObject`s, cleans up and `Destroy(gameObject)`s itself at the end), using `Time.unscaledDeltaTime` throughout so it survives slow-motion/hit-freeze.
**When to use:** Any new purely-visual one-shot effect tied to an enemy's lifecycle event (spawn, death).
**Trade-offs:** None significant for a prototype; the `RuntimeMaskSprite.CreateMaskSprite()` cache means adding more callers of this pattern costs no extra texture allocation.

**Example** (shape to copy for `EnemySpawnEffect`):
```csharp
// In MeleeEnemy/RangedEnemy/BossEnemy Awake(), after existing setup:
var spawnEffect = gameObject.AddComponent<EnemySpawnEffect>();
StartCoroutine(spawnEffect.PlaySpawnSequence(_sr)); // mirrors EnemyDeathEffect.PlayDeathSequence shape
```

### Pattern 3: Static-class data managers with a MonoBehaviour singleton escape hatch for Unity-object-backed needs

**What:** Pure static classes (`ScoreManager`, `FloorTimer`, `FloorManager`) for anything that's just numbers/state; a `MonoBehaviour` with a public static `Instance` (`WorldGenerator`, and now `AudioManager`) for anything that must own real Unity components (`AudioSource`, coroutines, `Transform`).
**When to use:** `AudioManager` firmly belongs in the second category — never force it into a pure static class.
**Trade-offs:** Singleton lifetime must be managed explicitly (`DontDestroyOnLoad` + a `Destroy(gameObject)` guard against duplicates across scene loads) — copy this guard, don't skip it, since the game's 3-scene flow (`MainMenu → AttackSelect → SampleScene`) will otherwise create a second `AudioManager` if one is dropped in more than one scene.

## Anti-Patterns

### Anti-Pattern 1: Adding the boss room to `_roomPrefabs`

**What people do:** Append the boss prefab to the same array used for the 6 `Complex_Room` variants for a "quick" integration.
**Why it's wrong:** Every room slot (including the pre-spawned next-floor standby room) rolls this pool uniformly — the boss would appear roughly 1-in-7 rooms, with no floor gate and no protection against two active boss encounters at once (current room + pre-spawned standby room could both be bosses).
**Instead:** Separate probabilistic roll + dedicated field(s), mirroring `TrySpawnExitPortal`'s `_exitSpawnChance`/`_maxExitsActive` pattern (see Decision 1 above).

### Anti-Pattern 2: Widening `IEnemy` (or adding `IBoss`) for scoring/data needs

**What people do:** Add `int ScoreValue { get; }` or similar to `IEnemy` so `CombatController` can differentiate kill rewards generically.
**Why it's wrong:** `IEnemy` is explicitly documented as minimal by design ("D-01: ... sole interface members"); widening it for one enemy type's bookkeeping need touches a contract three other components (`CombatController`, `MeleeEnemy`, `RangedEnemy`) all depend on, for a need that's entirely local to the boss.
**Instead:** Let `BossEnemy.OnDashHit()` call `ScoreManager.AddBossKillBonus()` itself, in addition to whatever generic behavior `CombatController` already runs for every `IEnemy` kill.

### Anti-Pattern 3: Baking `AudioSource` + `PlayOnAwake` directly into VFX prefabs instead of routing through `AudioManager`

**What people do:** Extend `PortalEffectBuilder`/`HitSparkBuilder` to attach an `AudioSource` component with a clip and `playOnAwake = true` to the generated prefab.
**Why it's wrong:** Loses a single centralized volume-control point (no future master/SFX volume slider without touching every prefab individually), and is inconsistent with the one component (`CombatController.SpawnHitSpark`) that instantiates a VFX prefab from code but has no equivalent hook for "also play a sound tied to game state" (e.g., only play a sound if not already at max concurrent hits, or duck under other SFX).
**Instead:** Keep VFX prefabs visual-only (as today); add `AudioClip` fields + `AudioManager.PlaySfx(...)` calls at the *triggering* runtime component (`CombatController`, `FloorTransitionEffect`, `EnemyDeathEffect`, `EnemySpawnEffect`, `BossEnemy`), not the instantiated prefab.

### Anti-Pattern 4: Assuming spawn-in VFX on `Awake()`/`OnEnable()` is automatically visible to the player

**What people do:** Add the spawn VFX to `MeleeEnemy`/`RangedEnemy`/`BossEnemy`'s `Awake()` and consider the feature done.
**Why it's wrong:** `WorldGenerator.TrySpawnEnemies` activates enemies at room-generation time, up to 2 rooms ahead of the player (off-screen); for the boss specifically this defeats the point of a visible "arrival" moment.
**Instead:** See Decision 4 — generic Option A (accept invisibility for regular fodder, it's harmless) + Option B for the boss specifically (gate `BossSpawner.Activate()` behind a room-entry trigger so the arrival is guaranteed on-screen).

## Integration Points

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| `WorldGenerator` ↔ boss room prefab | `GetComponentsInChildren<BossSpawner>` scan, same as `EnemySpawner` scan today | New `TrySpawnBoss` mirrors `TrySpawnEnemies` exactly; call it at the same 4 sites (`Start`'s `startRoom`, `SpawnNextPair`'s `room`, `SpawnPrevPair`'s `room`, `TrySpawnExitPortal`'s `standbyRoom`) |
| `WorldGenerator` ↔ `ExitPortal` | Unmodified — portal spawn logic (`TrySpawnExitPortal`) is content-agnostic and already runs for any room type, boss included | Zero code change required for "floor progression after boss" requirement |
| `CombatController` ↔ any `IEnemy` (incl. `BossEnemy`) | Existing `Physics2D.OverlapCircle` + `GetComponent<IEnemy>()` on the `Enemy` layer, `target.OnDashHit()` | Boss just needs its `Collider2D` on the `Enemy` layer; no `CombatController` changes |
| `BossEnemy` ↔ `ScoreManager` | New `ScoreManager.AddBossKillBonus()`, called directly by `BossEnemy.OnDashHit()` | Additive-only change to `ScoreManager`, same static pattern as existing methods |
| `BossEnemy`/`MeleeEnemy`/`RangedEnemy` ↔ `EnemyDeathEffect` | Unchanged — `AddComponent`+`StartCoroutine` self-attach pattern reused as-is | Boss may pass its own death `AudioClip` in place of (or alongside) `EnemyDeathEffect`'s if bespoke death sound is desired |
| `MeleeEnemy`/`RangedEnemy`/`BossEnemy` ↔ new `EnemySpawnEffect` | Same self-attach pattern as `EnemyDeathEffect`, called from `Awake()` | New component, mirrors `EnemyDeathEffect`'s shape, shares `RuntimeMaskSprite` |
| `FloorTransitionEffect` / `CombatController` / `EnemyDeathEffect` / `EnemySpawnEffect` / `BossEnemy` ↔ `AudioManager` | `AudioManager.PlaySfx(clip, volume)` static call | Single new integration surface all sound work funnels through |
| `AudioManager` bootstrap ↔ scene flow | `DontDestroyOnLoad` GameObject placed in `MainMenu` (first scene in `GameBootstrapper.EnsureMainMenu()` flow), or a `[RuntimeInitializeOnLoadMethod]` static bootstrap | Must guard against duplicate instances since `MainMenu` may reload after death (`DeathScreenController` → `AttackSelect` → `SampleScene` loop) |

## Scope Scaling (single boss species → future expansion)

The milestone explicitly wants "1 species, expandable framework." This maps to:

| Scope | Architecture Adjustments |
|-------|--------------------------|
| This milestone (1 boss species, 1 arena) | Single `_bossRoomPrefab` field, single `BossEnemy` class, single `Room_Boss` prefab. No pooling/weighting needed. |
| Near-future (2-3 boss species) | Promote `_bossRoomPrefab` to `GameObject[] _bossRoomPrefabs` (same pattern as `_roomPrefabs`) and pick both room+boss pairing together (they're coupled — a given arena's tilemap is built around its specific boss's attack patterns), still gated by the same `_bossSpawnChance`/`_maxBossRoomsActive`/`_bossMinFloor` fields. `BossEnemy` likely becomes an abstract base or keeps per-species subclasses/FSM variants, same relationship `MeleeEnemy`/`RangedEnemy` already have as `IEnemy` siblings. |
| Long-future (phased/multi-hit bosses) | This is the point to revisit the `IEnemy` minimal-interface decision — e.g., an `IPhaseBoss` addition or a `HitPoints` field — do not pre-build this now; it is explicitly out of scope and no current requirement needs it. |

## Suggested Build Order (dependency-driven)

1. **`AudioManager`** — zero dependencies, foundation for every other sound hook; validate in isolation (bind to a debug keypress, confirm a clip plays through the pool).
2. **Sound + timing polish pass on existing components** (`FloorTransitionEffect`, `CombatController` hit, `EnemyDeathEffect`) — depends only on step 1, delivers immediate milestone value, zero boss dependency, lowest risk (small additive diffs to already-stable components).
3. **`EnemySpawnEffect` component**, wired into `MeleeEnemy.Awake()`/`RangedEnemy.Awake()` — validates the spawn-VFX-visibility tradeoff (Decision 4, Option A) against the two *existing* enemy types before a third (boss) is added; depends on step 1 for its spawn sound.
4. **`BossEnemy` FSM + `ScoreManager.AddBossKillBonus()`** — standalone, testable in an isolated debug scene (drop one in an empty scene with a player, confirm dash-kill/score/death/spawn VFX all fire) before touching `WorldGenerator` at all; depends on steps 1 and 3 (reuses `AudioManager` and `EnemySpawnEffect`).
5. **Boss room prefab authoring** (`Room_Boss`: arena Tilemap, `RoomConnector` L/R, `CameraBound`, `ExitSpawnPoint`, single `BossSpawner`, zero `EnemySpawner`) — content task, can run in parallel with step 4 once the boss's collider/silhouette dimensions are known.
6. **`WorldGenerator` integration** (`SelectRoomPrefab` refactor, `_bossRoomPrefab`/`_bossSpawnChance`/`_maxBossRoomsActive`/`_bossMinFloor` fields, `TrySpawnBoss` wired at the 4 existing `TrySpawnEnemies` call sites, `_activeBossCount` bookkeeping in `RemoveTail`/`RemoveHead`/`FloorTransitionSequence`) — done **last**, since it is the highest blast-radius change to the most complex existing script, and only makes sense once `BossEnemy` (step 4) and the boss room prefab (step 5) both already work standalone.
7. **(Optional, Option B from Decision 4) Boss-room-entry activation trigger** — small, isolated addition on top of step 6, once the team confirms Option A's default "activate at generation time" doesn't deliver a satisfying on-screen boss arrival.

This ordering satisfies the two hard dependency constraints: audio before any VFX polish that plays a sound, and a working, standalone boss enemy before it gets wired into the room generator.

## Sources

- Direct source inspection (all HIGH confidence, no external research needed):
  - `Assets/Scripts/World/WorldGenerator.cs`
  - `Assets/Scripts/World/RoomConnector.cs`, `ExitPortal.cs`, `EnemySpawner.cs`, `ExitSpawnPoint.cs`, `ScoreManager.cs`, `FloorTimer.cs`, `FloorManager.cs`, `RuntimeMaskSprite.cs`, `FloorTransitionEffect.cs`, `GameBootstrapper.cs`
  - `Assets/Scripts/Enemy/IEnemy.cs`, `MeleeEnemy.cs`, `RangedEnemy.cs`, `EnemyDeathEffect.cs`
  - `Assets/Scripts/Player/CombatController.cs`, `PlayerController.cs`
  - `Assets/Scripts/Camera/CameraFollow.cs`
  - `Assets/Scripts/Room/RoomClearCondition.cs` (confirmed unused by any current `Complex_Room`/`Corridor` prefab via grep — legacy from the pre-v3.0 vertical room system, not a dependency for this milestone)
  - `Assets/Editor/PortalEffectBuilder.cs`, `Assets/Editor/HitSparkBuilder.cs` (confirmed Editor-only, not runtime)
  - `.planning/PROJECT.md` (milestone requirements, decision log, "Out of Scope" list)
  - Prefab inventory via `Glob` on `Assets/Prefabs/**/*.prefab` (confirmed 6 `Complex_Room` variants, `Enemies/MeleeEnemy.prefab`, `Enemies/RangedEnemy.prefab`, `World/ExitPortal/ExitPortal.prefab`, no existing boss/audio assets)
  - Confirmed zero existing `AudioSource`/`AudioClip`/`AudioListener`/`AudioMixer` usage anywhere in `Assets/Scripts` and zero `.wav`/`.mp3`/`.ogg` assets in the project — this is a fully greenfield addition, not an extension of an existing audio system

---
*Architecture research for: Boss room content + VFX/audio polish integration (Fast v3.1 milestone)*
*Researched: 2026-07-08*
