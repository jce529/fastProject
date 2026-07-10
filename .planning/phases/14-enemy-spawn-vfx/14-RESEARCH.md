# Phase 14: 적 등장 스폰 연출 - Research

**Researched:** 2026-07-10
**Domain:** Unity 6 2D — runtime VFX sequencing (SpriteMask/portal reuse), enemy FSM gating, WorldGenerator infinite-chain hook design
**Confidence:** HIGH (all findings verified directly against current repo source — no external library research needed; this is a pure in-repo architecture phase)

## Summary

This phase has no third-party dependency risk — everything needed already exists in the codebase (`PortalEffect.prefab`, `RuntimeMaskSprite`, `FloorTransitionEffect.PlayExit()`, `AudioManager.PlaySfx`). The real work is architectural: (1) splitting `EnemySpawner.Spawn()`/`Activate()` so VFX-triggering activation is deferred from pre-generation time to actual player arrival, (2) building a new player-entered-this-room/corridor hook inside `WorldGenerator` that doesn't yet exist in a usable granularity, and (3) gating `IEnemy.IsAlive` (private setter) from an external spawn-effect component without touching the 3-member `IEnemy` contract.

I verified every canonical_refs claim in CONTEXT.md against the live files — line numbers are accurate within ±5 lines. I found two things CONTEXT.md does not call out that materially change the plan:

1. **`IsAlive` has a `private set`** in both `MeleeEnemy` and `RangedEnemy`. An external `EnemySpawnEffect` component cannot set `enemy.IsAlive = false` from outside — a small method must be added to each enemy class (not to `IEnemy`) to allow this.
2. **Corridor prefabs currently have zero `EnemySpawner` markers.** D-03 ("Corridor 3종에도 동일한 스폰 연출 로직 적용... 기존 마커 배치를 그대로 사용") assumes markers already exist in Corridor prefabs — they do not. Only 12 Room prefabs have them (`grep -rl EnemySpawner Assets/Prefabs/` returns zero matches under `Assets/Prefabs/Corridors/`). This is a scope decision the planner must resolve explicitly (see Open Questions).

**Primary recommendation:** Add a lightweight, additive `ISpawnGatable` interface (not part of `IEnemy`) implemented by `MeleeEnemy`/`RangedEnemy` (and later `BossEnemy`) that internally flips the existing `IsAlive` backing field. A single new `EnemySpawnEffect` MonoBehaviour (mirroring the `EnemyDeathEffect` AddComponent+StartCoroutine convention) drives the portal-grow + enemy-walk-out + mask-shrink sequence and toggles the gate at start/end. `EnemySpawner` gets a `HasActivated` guard so `Activate()` becomes idempotent (solves D-02's "no replay on re-entry" for free). `WorldGenerator` needs a new room/corridor-entry detection mechanism finer-grained than the existing `_playerCurrentIndex` (which only fires at Room-boundary crossings, not Corridor-boundary crossings).

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Spawn portal appears only when the player actually enters that room/corridor. Current `WorldGenerator.TrySpawnEnemies()` calls `Spawn()+Activate()` together immediately at pre-generation time (2 rooms ahead) — this must be split. `Spawn()` (inactive instance creation) can stay at pre-gen time; `Activate()` (+ spawn VFX trigger) must be deferred until the player reaches that room/corridor section. Room entry detection can use the `_playerCurrentIndex` change point `WorldGenerator` already tracks.
- **D-02:** A room/section that already played its spawn VFX does not replay it on re-entry — one-shot spawn, not infinite respawn.
- **D-03:** Apply the same spawn-VFX logic to all 3 Corridor types (up/flat/down — all combat sections), not just Rooms — unified via `EnemySpawner` marker, no Room/Corridor distinction in code.
- **D-04:** Multiple enemies in one room/corridor → one portal per `EnemySpawner` marker position (one marker = one portal, reuse existing marker placement).
- **D-05:** Multiple portals in a section appear staggered, not simultaneously ("queue them, release one at a time"). Exact stagger interval is Claude's Discretion.
- **D-06:** Reuse `FloorTransitionEffect.PlayExit()`'s structural pattern (PortalEffect prefab grow → SpriteMask shrink fade-in → portal shrink+Destroy, ~1.2s total). Reuse existing `PortalEffect.prefab` (visual-only, no Collider) and `RuntimeMaskSprite.CreateMaskSprite()` as-is.
- **D-07:** Unlike the player's exit (static position + mask reveal only), the enemy must have actual Rigidbody2D movement walking out from the portal center — mask-shrink and physical movement happen together. (User's stated complaint about the player-side version being static; enemy spawn should not repeat that flaw.)
- **D-08:** Portal size must auto-scale to the enemy's sprite size — size-independent so Phase 16 boss reuse needs no extra work.
- **D-09:** Normal enemies (melee/ranged) also get spawn sound — reuse existing `AudioManager.PlaySfx(Sfx.PortalEnter)`/`Sfx.PortalExit` (no new clip import). SFX-05 (boss-only spawn sound) is separate, Phase 16 scope.
- **D-10:** No player input restriction during enemy spawn VFX — movement/attack/roll all free (unlike floor-transition's input lock; this is a combat-entry flourish, not a scene transition).
- **D-11:** Enemy playing spawn VFX must be excluded from `CombatController.FindNearestEnemyInRange()` target candidates, and its own detection/chase/attack FSM must not run (SPWN-02). Implementation approach (reuse `IEnemy.IsAlive` gate vs. new `IsSpawning` flag) is Claude's Discretion — but the `IEnemy` 3-member contract (`IsAlive`, `OnDashHit()`, `ClearHighlight()`) itself must not change, since Phase 15/16 BossEnemy integration assumes "no IEnemy contract changes."

### Claude's Discretion

- Exact stagger interval between multiple portals (e.g., 0.2–0.4s range).
- Implementation mechanism for detection/targeting block (IsAlive reuse vs. new flag) — must preserve IEnemy contract.
- Exact implementation location/mechanism for room/corridor entry detection inside `WorldGenerator`.
- Multi-enemy release order (marker component traversal order vs. random).

### Deferred Ideas (OUT OF SCOPE)

- **적 무한 리스폰 메커니즘**: Idea to respawn enemies every time a room/corridor is re-entered (fits "AI endlessly fighting in simulation" story theme). Deferred — balance/trigger-rule design needed, separate future milestone. This phase is one-shot spawn only (D-02).
- **플레이어 포탈 연출 재작업 (FloorTransitionEffect 개선)**: Improving the already-shipped Phase 12 player-side portal effect (entry suction effect, exit using dash animation instead of static mask fade) is out of scope. The player-side complaint directly informed D-07's enemy-side fix, but the player-side code itself is not touched in this phase.

</br>

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SPWN-01 | 일반 적(근접/원거리)과 보스가 스폰될 때 플레이어처럼 포탈을 타고 등장하는 연출이 재생된다 | `PortalEffect.prefab` + `RuntimeMaskSprite` + `FloorTransitionEffect.PlayExit()` pattern verified reusable as-is; new `EnemySpawnEffect` component design provided below with exact code skeleton |
| SPWN-02 | 스폰 연출이 끝나기 전까지 적은 감지/공격 대상이 되지 않는다 | `CombatController.FindNearestEnemyInRange()` line 400 `!enemy.IsAlive` check verified; `MeleeEnemy`/`RangedEnemy` `Update()` line ~120-122 `if (!IsAlive) return;` guard verified; `ISpawnGatable` interface design closes the private-setter gap |

## Project Constraints (from CLAUDE.md)

- Unity 6000.3.11f1, C# 9.0, Mono backend, .NET Standard 2.1 — no new language features beyond what's already used in the codebase.
- **GSD workflow enforcement**: no direct file edits outside `/gsd:execute-phase`.
- **All timing must use `Time.unscaledDeltaTime`/`WaitForSecondsRealtime`** — slow-motion (`Time.timeScale`) and HitFreeze immune. This is already the pattern in every reused reference file (`FloorTransitionEffect`, `EnemyDeathEffect`).
- `Physics2D.OverlapCircle(ContactFilter2D, results[])` — never `FindObjectsOfType`/LINQ in `Update()`. No new per-frame physics queries are needed for this phase (detection reuses existing `CombatController`/enemy `_playerFilter` buffers).
- Scope discipline: prototype-only, current phase only — do not build Phase 15/16 BossEnemy code now; only design the spawn-effect component to be enemy-type-agnostic so Phase 16 can reuse it without modification (per D-08/Success Criterion 5).
- STATE.md "Technical Constraints to Enforce Every Phase" explicitly states: **"Spawn VFX must hook `EnemySpawner.Activate()` only — never `Awake()`/`OnEnable()`"** — do not trigger VFX from the enemy's own lifecycle methods; the trigger must originate from `EnemySpawner.Activate()`/`WorldGenerator`.

## Standard Stack

No new packages needed. This phase is 100% in-repo, using existing Unity 6 built-in systems (SpriteRenderer, SpriteMask, Rigidbody2D, Coroutines, ParticleSystem already used by `EnemyDeathEffect`).

### Core (existing, reused)
| Asset/Class | Location | Purpose | Why reused |
|---------|---------|---------|--------------|
| `PortalEffect.prefab` | `Assets/Prefabs/World/PortalEffect/PortalEffect.prefab` | Visual-only portal sprite (no Collider) | D-06 locked decision; already used by `FloorTransitionEffect.PlayExit()` |
| `RuntimeMaskSprite.CreateMaskSprite()` | `Assets/Scripts/World/RuntimeMaskSprite.cs` | Cached 4x4 white `Sprite` for `SpriteMask` | Shared static helper already used by both `FloorTransitionEffect` and `EnemyDeathEffect` |
| `AudioManager.PlaySfx(Sfx.PortalEnter/PortalExit)` | `Assets/Scripts/Audio/AudioManager.cs:82` | SFX playback | D-09; static entry point, null-safe |

### Supporting (new, small)
| Component | Purpose | When to Use |
|---------|---------|-------------|
| `EnemySpawnEffect : MonoBehaviour` (new file, e.g. `Assets/Scripts/Enemy/EnemySpawnEffect.cs`) | Plays portal-grow + walk-out + mask-shrink sequence on an enemy GameObject | AddComponent'd by `EnemySpawner.Activate()` or its caller, mirroring `EnemyDeathEffect` convention |
| `ISpawnGatable` (new interface, e.g. `Assets/Scripts/Enemy/ISpawnGatable.cs`) | `void SetSpawnGate(bool isSpawning)` — additive, not part of `IEnemy` | Implemented by `MeleeEnemy`/`RangedEnemy` (and future `BossEnemy`) to let `EnemySpawnEffect` toggle `IsAlive` without touching `IEnemy`'s 3-member contract |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `ISpawnGatable` additive interface | New `IsSpawning` bool field on each enemy class, checked via type-casting from `EnemySpawnEffect` | Casting to concrete types (`MeleeEnemy`/`RangedEnemy`) inside a "type-agnostic" component defeats Success Criterion 5 (must be reusable for Boss without modification). The interface approach avoids this. |
| Reusing `IsAlive` as the gate | New parallel `IsAlive2`/`IsTargetable` property added to `IEnemy` | Would change the `IEnemy` contract — explicitly forbidden by D-11 and the Phase 15/16 "no IEnemy contract changes" premise. |
| Per-spawner `HasActivated` flag (idempotent `Activate()`) | WorldGenerator-side `HashSet<GameObject>` tracking already-triggered rooms | Flag-on-marker is simpler, colocated with the data it guards, and requires no new WorldGenerator-level bookkeeping structure. |

**Installation:** None — no new packages. All new code is plain C# MonoBehaviours/interfaces added to `Assets/Scripts/Enemy/` and `Assets/Scripts/World/`.

## Architecture Patterns

### Recommended file layout (additions only)
```
Assets/Scripts/
├── Enemy/
│   ├── ISpawnGatable.cs        # NEW — additive interface, not part of IEnemy
│   ├── EnemySpawnEffect.cs     # NEW — mirrors EnemyDeathEffect.cs convention
│   ├── MeleeEnemy.cs           # MODIFIED — implement ISpawnGatable (1 method)
│   └── RangedEnemy.cs          # MODIFIED — implement ISpawnGatable (1 method)
└── World/
    ├── EnemySpawner.cs         # MODIFIED — split Spawn()/Activate(), add HasActivated guard
    └── WorldGenerator.cs       # MODIFIED — new room/corridor entry hook, staggered activation coroutine
```

### Pattern 1: `ISpawnGatable` — additive interface to bridge the private `IsAlive` setter

**What:** `IEnemy.IsAlive` has a `private set` in both `MeleeEnemy` and `RangedEnemy` (verified — see Code Examples). `CombatController.FindNearestEnemyInRange()` and each enemy's own `Update()` guard already gate on `!IsAlive`, so reusing this single property is the cleanest way to satisfy SPWN-02 (excluded from targeting AND FSM stopped) with zero changes to `CombatController` or `IEnemy`.

**When to use:** Any time an external system (here, `EnemySpawnEffect`) needs to toggle an enemy's alive/targetable state without being aware of the concrete enemy type.

**Example:**
```csharp
// Assets/Scripts/Enemy/ISpawnGatable.cs (NEW FILE)
/// <summary>
/// Additive interface — NOT part of IEnemy's 3-member contract (IsAlive, OnDashHit, ClearHighlight).
/// Lets EnemySpawnEffect toggle the existing IsAlive gate during spawn VFX without any enemy-type
/// casting, keeping EnemySpawnEffect reusable for Phase 16 BossEnemy without modification (SPWN Success Criterion 5).
/// </summary>
public interface ISpawnGatable
{
    /// <summary>True while spawn VFX is playing — implementor should force IsAlive=false during this window.</summary>
    void SetSpawnGate(bool isSpawning);
}
```

```csharp
// MeleeEnemy.cs / RangedEnemy.cs — add ONE method each, no other changes to IEnemy members
public class MeleeEnemy : MonoBehaviour, IEnemy, ISpawnGatable
{
    // ... existing IsAlive property unchanged: public bool IsAlive { get; private set; } = true;

    public void SetSpawnGate(bool isSpawning) => IsAlive = !isSpawning;
}
```

Because `IsAlive`'s setter is `private`, this method must live inside the class itself — it cannot be added via extension method or partial class from outside.

### Pattern 2: `EnemySpawner` two-phase Spawn/Activate split with idempotent guard (D-01, D-02)

**What:** Decouple instance creation (safe at pre-gen time, object stays inactive/invisible) from VFX-triggered activation (must wait for actual player arrival). Add a `HasActivated` flag so calling `Activate()` again on re-entry is a safe no-op — this satisfies D-02 without needing any WorldGenerator-side "already visited this room" bookkeeping.

**Example (current code — see Code Examples section for exact diff needed):**
```csharp
// EnemySpawner.cs — current (BEFORE)
public void Spawn(GameObject meleePrefab, GameObject rangedPrefab) { ... _spawned.SetActive(false); }
public void Activate() { if (_spawned != null) _spawned.SetActive(true); }
```
Target shape:
```csharp
public bool HasActivated { get; private set; }

public void Activate()
{
    if (HasActivated || _spawned == null) return;
    HasActivated = true;
    _spawned.SetActive(true);
    // caller (WorldGenerator) is responsible for AddComponent<EnemySpawnEffect> + gating IsAlive
    // BEFORE this frame's Update() runs — see Pitfall 1 below on ordering.
}
```

### Pattern 3: Room/Corridor entry detection — the actual hard part

**What:** `WorldGenerator` currently tracks `_playerCurrentIndex`/`_playerCurrentNode` at **Room granularity only** (see `UpdatePlayerIndex()`, `WorldGenerator.cs:458-490`). The forward-advance check compares `player.position.x` against the **next Room's Left (ENT) connector** — i.e., the index only advances once the player has already walked entirely through the intervening Corridor and reached the next Room's entrance.

**Why this matters for D-03:** If Corridor `EnemySpawner` markers exist inside the corridor's own bounds, and activation is only triggered on this Room-granularity index change, the corridor's spawn portal would appear only once the player has already reached the *next room* — i.e., after having walked straight through the corridor without ever seeing the portal appear, defeating the stated goal ("스폰 포탈은 플레이어가 해당 룸/Corridor에 실제로 진입할 때 나타난다").

**Recommendation:** Add a second, finer-grained threshold check using the **Corridor's own Left connector** (`FindConnector(corridor, RoomConnector.Direction.Left)`), which spatially coincides with the *previous* Room's Right (EXIT) connector (because `AlignByEntry()` glues the corridor's entry to the previous room's exit point — verified in `SpawnNextPair()`, `WorldGenerator.cs:139-146`). Track this as a second boolean/threshold check per chain node, independent of `_playerCurrentIndex`, e.g.:

```csharp
// Conceptual addition inside WorldGenerator.Update() or a new method called from it.
// _playerCurrentNode already gives us the current room's tuple (room, corridorToItsLeft) and
// _playerCurrentNode.Next gives the next room. The corridor "between" current and next room is
// _playerCurrentNode.Next.Value.corridor (per D-09: corridor = room's LEFT/entry side).
private void CheckCorridorEntry()
{
    if (_playerCurrentNode.Next == null) return;
    var nextCorridor = _playerCurrentNode.Next.Value.corridor;
    if (nextCorridor == null || _activatedCorridors.Contains(nextCorridor)) return;

    var corridorEntry = FindConnector(nextCorridor, RoomConnector.Direction.Left);
    if (corridorEntry != null && _playerTransform.position.x > corridorEntry.transform.position.x)
    {
        _activatedCorridors.Add(nextCorridor);
        StartCoroutine(ActivateSectionEnemies(nextCorridor));
    }
}
```

This is a genuinely new piece of tracking state, not a reuse of `_playerCurrentIndex`. Flagged as an **Open Question** below for planner sign-off since it's architecturally non-trivial and touches `Update()` hot path (still O(1) per frame — one connector lookup already cached-findable, not a `GetComponentsInChildren` call per frame).

### Pattern 4: Staggered multi-portal release (D-05) reusing existing type-filtered spawn order

**What:** `TrySpawnEnemies()` already filters `room.GetComponentsInChildren<EnemySpawner>(true)` by type against `GetEnemyCount(floor)` counts (D-04b logic, `WorldGenerator.cs:390-411`). Per CONTEXT.md `<specifics>`, this same filtered set (in existing traversal order) is reused as the stagger sequence — no new manual queue structure needed. Concretely: at `Spawn()`-time (pre-gen), collect the *same* filtered `List<EnemySpawner>` that would have been activated, but store it (e.g., attach a `RoomEnemyManifest` component to the room root holding `List<EnemySpawner> ToActivate`) instead of calling `Activate()` immediately. When the room/corridor-entry hook fires, `StartCoroutine` iterates that stored list with `WaitForSecondsRealtime(staggerInterval)` between each `Activate()` call.

### Anti-Patterns to Avoid
- **Triggering VFX from `Awake()`/`OnEnable()`**: Explicitly forbidden by STATE.md's locked constraint ("Spawn VFX must hook `EnemySpawner.Activate()` only"). `Awake()`/`OnEnable()` fire whenever `SetActive(true)` is called, which happens both at legitimate activation time AND (if the code isn't careful) could be re-triggered by pooling/edge cases — hook the explicit `Activate()` call site, not lifecycle methods.
- **Casting to concrete enemy types inside the spawn-effect component**: Breaks Success Criterion 5 (component must be enemy-type-agnostic for Phase 16 Boss reuse). Use `GetComponent<ISpawnGatable>()` instead of `GetComponent<MeleeEnemy>()`/`GetComponent<RangedEnemy>()`.
- **Setting `IsAlive = true` (default field initializer) and only gating via a separate flag that `CombatController` doesn't know about**: Since `CombatController.FindNearestEnemyInRange()` only checks `!enemy.IsAlive` (line 400) — verified, no other property exists on `IEnemy` to check — any new flag not funneled through `IsAlive` will NOT exclude the enemy from targeting. The gate must go through `IsAlive` (via `ISpawnGatable`), not a parallel untracked field.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| SpriteMask fade/reveal | New masking shader or coroutine tween library | `RuntimeMaskSprite.CreateMaskSprite()` + manual `Mathf.Lerp` width animation (copy `FloorTransitionEffect.PlayExit()` pattern) | Already solved, cached (no per-call Texture2D allocation — mobile GC concern per CLAUDE.md), and battle-tested through Phase 12/13 playtest sign-off |
| Portal grow/shrink scale tween | DOTween or other tweening package | `ScaleTransform()` coroutine pattern (private helper in `FloorTransitionEffect`, ~15 lines, `Time.unscaledDeltaTime`-based) | No new package dependency; project has zero tweening libraries currently and this phase doesn't need one |
| Death/spawn effect component wiring | A generic "VFX Manager" singleton or event bus | `AddComponent<T>()` + `StartCoroutine(...)` per-instance (exact `EnemyDeathEffect` convention) | Established, minimal, scoped to the GameObject's own lifetime — matches existing project pattern precisely |

**Key insight:** Every visual building block this phase needs was already built in Phase 10/12/13. The only genuinely new code is (1) the `ISpawnGatable` bridge, (2) the `EnemySpawner`/`WorldGenerator` two-phase split, and (3) actual physical movement (Rigidbody2D `MovePosition` walk-out) — which `FloorTransitionEffect` does NOT have (it only fades a mask; the player's Transform never physically moves during the mask fade). D-07 is the one genuinely new piece of motion logic in this phase.

## Common Pitfalls

### Pitfall 1: IsAlive gate must be forced BEFORE the enemy's first `Update()` after activation
**What goes wrong:** `_spawned.SetActive(true)` runs `Awake()` synchronously (if this is the enemy's first activation) which sets `IsAlive = true` via field initializer executed at `Instantiate()` time (not at `SetActive` time — field initializers actually run at `Instantiate()`, so `IsAlive` is already `true` by the time `Activate()` calls `SetActive(true)`, since `Instantiate()` happened earlier during `Spawn()`). If `SetSpawnGate(true)` (forcing `IsAlive=false`) is not called in the SAME method call chain as `Activate()`, before the engine's next `Update()` tick, there is a real risk the enemy's FSM starts (Idle/patrol) and `CombatController` finds it targetable for one or more frames before the gate closes.
**Why it happens:** Unity field initializers on MonoBehaviours run at `Instantiate()` time (during `Spawn()`, potentially many frames before `Activate()`), not at `SetActive(true)` time. `IsAlive` is `true` well before the object becomes visible.
**How to avoid:** In the caller (`WorldGenerator`'s staggered activation coroutine), always call `spawner.Activate()` and *synchronously, same call*, `spawnedGameObject.GetComponent<ISpawnGatable>()?.SetSpawnGate(true)` — do this as one atomic step, then `AddComponent<EnemySpawnEffect>()` + `StartCoroutine(PlaySpawnSequence(...))` which calls `SetSpawnGate(false)` only at the very end of the VFX sequence.
**Warning signs:** Enemy briefly flashes/attacks/moves before the portal VFX visually resolves; `CombatController` highlights an enemy that's still "in the portal."

### Pitfall 2: Corridor-granularity entry detection does not exist yet
**What goes wrong:** Assuming `_playerCurrentIndex`/`UpdatePlayerIndex()` already fires at the right moment for Corridor spawn triggers (D-03) — it does not; it's Room-boundary-only (see Pattern 3 above). Naively reusing the existing index-change hook for Corridors will trigger the corridor's portal only after the player has already walked past it.
**Why it happens:** `_chain` stores `(room, corridor)` tuples where `corridor` is the room's *left* neighbor, but the only tracked crossing threshold is the *next room's* Left/ENT connector — the corridor's own entry point is never separately checked.
**How to avoid:** Add the second threshold check described in Pattern 3, using the corridor's own Left connector position (which coincides with the previous room's Right/EXIT connector due to `AlignByEntry()`).
**Warning signs:** Playtesting shows corridor enemies "already standing there" with no portal animation, appearing only once the player is already in the next room.

### Pitfall 3: Portal scale target is hardcoded to `Vector3.one`, not the prefab's authored scale
**What goes wrong:** `FloorTransitionEffect.PlayExit()` does `portalEffect.transform.localScale = Vector3.zero;` then animates to `Vector3.one` — but the `PortalEffect.prefab` asset's own root transform has an authored (possibly non-uniform, non-1x1x1) local scale (currently `{x:1, y:2, z:1}` in the working tree — though this happens to be an uncommitted local edit unrelated to this phase, it demonstrates the prefab's baked scale is NOT what actually renders during the transition, since code always resets to `Vector3.zero → Vector3.one`).
**Why it happens:** The grow-animation target is hardcoded as `Vector3.one` in `ScaleTransform()`, ignoring whatever scale the prefab was authored with.
**How to avoid:** For D-08 (auto-scale portal to enemy sprite size), compute a target scale from the enemy's `SpriteRenderer.bounds` (e.g., `Vector3.one * Mathf.Max(enemySr.bounds.size.x, enemySr.bounds.size.y) * someMultiplier`) and pass that as the animation's end value — do NOT rely on the prefab's baked `localScale` remaining intact, since the reused `ScaleTransform()` pattern always overrides it from zero.
**Warning signs:** Portal appears the same size regardless of enemy sprite size; boss reuse in Phase 16 looks visually wrong (portal too small for a presumably larger boss sprite) unless this is parameterized now.

### Pitfall 4: `EnemySpawner.Activate()` re-entrancy across pre-gen and real activation
**What goes wrong:** `TrySpawnEnemies()` is currently called from 4 call sites: `Start()` (initial room), `SpawnNextPair()`, `SpawnPrevPair()`, and `TrySpawnExitPortal()` (for the next-floor standby room). All 4 currently call `spawner.Spawn(...)` AND `spawner.Activate()` together. After the D-01 split, ALL FOUR call sites must be changed to call `Spawn()` only (never `Activate()` directly) — missing even one call site means that room's enemies activate immediately at pre-gen time with no VFX, silently violating Success Criterion 1.
**Why it happens:** Enemy spawning is wired into 4 separate room-creation code paths (start room, forward chain growth, backward chain growth, and next-floor standby room), and it's easy to fix only the "main" path.
**How to avoid:** Search all 4 call sites listed above during implementation; each is currently `spawner.Spawn(_meleeEnemyPrefab, _rangedEnemyPrefab); spawner.Activate();` inside `TrySpawnEnemies()` — since this is a single shared private method (line 390-411), fixing the ONE method body covers all 4 call sites automatically (they all funnel through `TrySpawnEnemies()`). Confirm this during implementation — the shared-method design actually makes this pitfall low-risk IF the refactor touches only `TrySpawnEnemies()`'s body and not each call site individually.
**Warning signs:** Some rooms show enemies with no spawn VFX (e.g., the standby room for next floor, or the start room) while others do.

### Pitfall 5: Standby room enemies (`TrySpawnExitPortal`'s next-floor room) are nested under a `SetActive(false)` parent
**What goes wrong:** `TrySpawnExitPortal()` (`WorldGenerator.cs:356-364`) calls `TrySpawnEnemies(standbyRoom, floor+1)` BEFORE `standbyRoom.SetActive(false)`. Under the post-refactor design, this still just calls `Spawn()` (inactive child creation) — safe. But when this standby room later becomes the active room after `FloorTransitionSequence` (`WorldGenerator.cs:536-537`, `newRoom.SetActive(true)`), the new room-entry hook must fire for it too (it becomes `_chain`'s sole entry with `_playerCurrentIndex = 0`) — verify the entry-detection hook (Pattern 3) is initialized/reset correctly for this floor-transition path, not just the steady-state chain-growth path.
**Why it happens:** Floor transition (`EnterPortal`/`FloorTransitionSequence`) is a structurally different code path from steady-state room chain growth — it clears `_chain` entirely and starts fresh with a single room, bypassing `SpawnNextPair()`/`SpawnPrevPair()`.
**How to avoid:** Explicitly handle activation of the new floor's starting room's `EnemySpawner`s inside `FloorTransitionSequence()` (there is already a commented "Step 4 — 적 활성화... 의도적 no-op" placeholder at `WorldGenerator.cs:558-559` marking exactly this gap — Phase 11 intentionally left it as a no-op; Phase 14 is likely where this gets filled in).
**Warning signs:** Enemies in the very first room of a newly-entered floor either don't spawn at all, or spawn without VFX (silently skipping the new activation path).

## Runtime State Inventory

Not applicable — this is a greenfield feature addition (new component + interface), not a rename/refactor/migration phase. No stored data, live service config, OS-registered state, secrets, or build artifacts are affected.

## Code Examples

### Current `EnemySpawner.cs` (full, verified as of research date)
```csharp
// Source: Assets/Scripts/World/EnemySpawner.cs (current, unmodified)
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged }

    [SerializeField] private EnemyType _type = EnemyType.Melee;
    public EnemyType Type => _type;

    private GameObject _spawned;

    public void Spawn(GameObject meleePrefab, GameObject rangedPrefab)
    {
        GameObject prefab = _type == EnemyType.Melee ? meleePrefab : rangedPrefab;
        if (prefab == null) return;

        _spawned = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        _spawned.SetActive(false);
    }

    public void Activate()
    {
        if (_spawned != null)
            _spawned.SetActive(true);
    }
}
```

### Current `WorldGenerator.TrySpawnEnemies()` — the single method that must change (line 390-411)
```csharp
// Source: Assets/Scripts/World/WorldGenerator.cs:390-411 (current)
private void TrySpawnEnemies(GameObject room, int floor)
{
    (int meleeCount, int rangedCount) = GetEnemyCount(floor);
    int meleeSpawned = 0;
    int rangedSpawned = 0;

    foreach (EnemySpawner spawner in room.GetComponentsInChildren<EnemySpawner>(true))
    {
        if (spawner.Type == EnemySpawner.EnemyType.Melee && meleeSpawned < meleeCount)
        {
            spawner.Spawn(_meleeEnemyPrefab, _rangedEnemyPrefab);
            spawner.Activate();   // <-- D-01: this line must move OUT of pre-gen, into the new entry-triggered coroutine
            meleeSpawned++;
        }
        else if (spawner.Type == EnemySpawner.EnemyType.Ranged && rangedSpawned < rangedCount)
        {
            spawner.Spawn(_meleeEnemyPrefab, _rangedEnemyPrefab);
            spawner.Activate();   // <-- same
            rangedSpawned++;
        }
    }
}
```
All 4 call sites (`Start()` line 104, `SpawnNextPair()` line 157, `SpawnPrevPair()` line 192, `TrySpawnExitPortal()` line 363) funnel through this single method, so the fix is localized to this one method body — see Pitfall 4.

### `FindNearestEnemyInRange()` exact skip check (verified, line 400)
```csharp
// Source: Assets/Scripts/Player/CombatController.cs:396-400 (current, unmodified)
for (int i = 0; i < count; i++)
{
    var enemy = _hitBuffer[i].GetComponent<IEnemy>();
    // Skip dead enemies — physics broadphase may lag behind collider.enabled=false (Pitfall 6)
    if (enemy == null || !enemy.IsAlive) continue;
```
No changes needed to this file — `ISpawnGatable` routes through the existing `IsAlive` check.

### `MeleeEnemy`/`RangedEnemy` FSM guard (verified identical in both files)
```csharp
// Source: Assets/Scripts/Enemy/MeleeEnemy.cs:120-122 and RangedEnemy.cs:124-126 (identical pattern)
private void Update()
{
    if (!IsAlive) return;   // <-- this single guard already stops FSM when IsAlive=false
    ...
}
```

### `EnemyDeathEffect` AddComponent+StartCoroutine convention to mirror
```csharp
// Source: Assets/Scripts/Enemy/MeleeEnemy.cs:95-97 (OnDashHit(), the pattern to replicate for spawn)
var deathEffect = GetComponent<EnemyDeathEffect>();
if (deathEffect == null) deathEffect = gameObject.AddComponent<EnemyDeathEffect>();
StartCoroutine(deathEffect.PlayDeathSequence(_animator));
```
`EnemySpawnEffect` should follow the identical shape: `AddComponent<EnemySpawnEffect>()` then `StartCoroutine(effect.PlaySpawnSequence(...))`, called from wherever the new staggered-activation coroutine lives in `WorldGenerator`.

### `AudioManager.PlaySfx` exact signature (verified)
```csharp
// Source: Assets/Scripts/Audio/AudioManager.cs:82
public static void PlaySfx(Sfx id, float volume = 1f) => Instance?.PlayInternal(id, volume);
// Sfx enum (Assets/Scripts/Audio/AudioManager.cs:3-9): PortalEnter, PortalExit, Slash, EnemyDeathGlitch
```

## State of the Art

Not applicable in the traditional sense (no external library churn) — this section instead documents **within-project** state changes relevant to planning:

| Old Approach (Phase 9-13) | New Approach Needed (Phase 14) | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `EnemySpawner.Spawn()+Activate()` always called together, immediately at room instantiation | Split: `Spawn()` at pre-gen, `Activate()` deferred to actual player arrival | This phase | All 4 `TrySpawnEnemies()` call sites affected via the single shared method |
| `WorldGenerator` tracks player position only at Room granularity (`_playerCurrentIndex`) | Needs an additional Corridor-granularity threshold check (Pattern 3) | This phase | New tracking state (e.g., `HashSet<GameObject> _activatedSections`), not just index math |
| `IEnemy.IsAlive` toggled only internally by `OnDashHit()` | Also toggled externally by `EnemySpawnEffect` via new `ISpawnGatable` bridge | This phase | Two small, additive changes to `MeleeEnemy.cs`/`RangedEnemy.cs`; zero changes to `IEnemy.cs` |
| `FloorTransitionSequence()` Step 4 is an intentional no-op (`WorldGenerator.cs:558-559`, "Pitfall 5" comment referencing "EXIT-01/02/03 범위 밖") | This no-op likely needs to be filled in for the new floor's starting room enemies to spawn-with-VFX correctly | This phase (probable) | See Pitfall 5 above |

**Deprecated/outdated:** None — no external APIs are being replaced.

## Open Questions

1. **Do Corridor prefabs need `EnemySpawner` markers added as part of this phase's scope, or is that a separate manual Unity-Editor task?**
   - What we know: D-03 says "기존 마커 배치를 그대로 사용" (reuse existing marker placement), implying markers already exist. Verified via `grep -rl EnemySpawner Assets/Prefabs/Corridors/` — **zero matches**. All 12 rooms with markers are Room prefabs (`Room_AllInOne`, `Room_EdgeRun`, `Room_GaugeOutpost`, `Room_LastStand`, `Room_RiskCrossing`, `Room_Vertical_Gauntlet`, `Room_Chain`, `Room_Crossroad`, `Room_Hunt`, `Room_LadderDanger`, `Room_Recovery`, `Room_Sniper`). None of the 3 Corridor prefabs (`Corridor_Down`, `Corridor_Flat`, `Corridor_Up`) have any.
   - What's unclear: Whether the planner should (a) add a task to manually place `EnemySpawner` markers in the 3 corridor prefabs (Unity Editor work, not pure C#), (b) treat D-03 as "the code path must support corridors generically, to be exercised once markers are added in a later content pass" (code-only, no visible behavior change yet since no markers exist to trigger it), or (c) confirm with the user this was a documentation assumption error.
   - Recommendation: Surface this to the user/planner explicitly before writing tasks — do not silently assume corridors already have markers. If (b) is chosen, the plan should still write the generic (Room-or-Corridor) activation code, but the "5 Success Criteria" success statement about corridors having visible spawn VFX cannot be verified via playtest until markers are actually placed somewhere.

2. **Exact mechanics of the new Corridor-granularity entry-detection hook (Pattern 3).**
   - What we know: The existing `_playerCurrentIndex`/`UpdatePlayerIndex()` mechanism is Room-granularity only; a second threshold check against the corridor's own Left connector is needed.
   - What's unclear: Whether to implement this as a full parallel index/state machine (more robust, more code) or a simpler `HashSet<GameObject>`-based "already triggered" check combined with a single additional per-frame connector-position comparison (simpler, matches existing code style). CONTEXT.md explicitly marks "룸/Corridor 진입 감지 훅의 정확한 구현 위치" as Claude's Discretion, so this is expected to be resolved during planning, not pre-decided.
   - Recommendation: Favor the simpler `HashSet`-based approach shown in Pattern 3 — it's O(1) per frame, requires no restructuring of the existing `_chain`/`_playerCurrentIndex` mechanism, and is easy to reset on floor transition (`_chain.Clear()` already happens in `FloorTransitionSequence()`, so a fresh `HashSet` per floor is trivial to wire in alongside it).

3. **Does `FloorTransitionSequence()`'s Step 4 no-op (line 558-559) get filled in during this phase, or does the new floor's starting room simply go through the same entry-detection hook as steady-state rooms (making the explicit no-op naturally resolve itself)?**
   - What we know: The comment explicitly says "적 활성화... 의도적 no-op (Pitfall 5 — 적 스폰 배선은 EXIT-01/02/03 범위 밖. 이 단계는 구조적 자리만 유지한다.)" — meaning it was deliberately deferred to a future phase, likely this one.
   - What's unclear: Whether the new room/corridor entry hook (which resets `_playerCurrentIndex=0`/`_playerCurrentNode=newNode` right after this point, per `WorldGenerator.cs:539-540`) will naturally fire for the new floor's starting room without any special-casing, or whether `FloorTransitionSequence` needs an explicit call to the new activation coroutine.
   - Recommendation: Design the entry-detection hook to run generically off `_playerCurrentNode` state (not off a specific `SpawnNextPair()`/`SpawnPrevPair()` call site), so it naturally covers the floor-transition path too — then the Step 4 comment can likely be deleted/updated to point at the new generic hook rather than requiring bespoke floor-transition-specific code.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected — no `.asmdef`, no NUnit test files, no `Tests/` directories anywhere in `Assets/` |
| Config file | none — see Wave 0 |
| Quick run command | N/A (manual playtest only) |
| Full suite command | N/A (manual playtest only) |

This matches the established project convention: Phase 12/13 (`13-04-PLAN.md`) validated SFX/VFX timing entirely via manual in-Editor playtest sign-off, not automated tests. This phase's success criteria (visual VFX timing, FSM gating during a coroutine window, physical walk-out motion) are inherently playtest-verifiable, not unit-testable in a meaningful way without a Unity PlayMode test harness that doesn't currently exist in this repo.

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SPWN-01 | Enemy plays portal VFX at `Activate()` time, not pre-gen time | manual-only | N/A — visually verify in Play Mode: 2-room-ahead enemies invisible/inactive until player walks into their room, then portal grow→enemy walk-out→shrink plays | ❌ Wave 0 (no harness exists; manual playtest per project convention) |
| SPWN-02 | Enemy excluded from `FindNearestEnemyInRange()` and its own FSM during spawn VFX | manual-only | N/A — visually verify: attempt slow-mo+dash-target during a still-spawning enemy's VFX window; confirm it cannot be highlighted/targeted, and it does not chase/attack until VFX completes | ❌ Wave 0 (no harness exists; manual playtest per project convention) |

### Sampling Rate
- **Per task commit:** Manual in-Editor Play Mode smoke check (enter a room ahead of time via fast player movement, observe portal VFX timing)
- **Per wave merge:** Full manual playtest pass through several rooms + corridors + at least one floor transition (to exercise Open Question 3's floor-transition-entry path)
- **Phase gate:** Full manual playtest sign-off before `/gsd:verify-work`, mirroring the `13-04-PLAN.md` sign-off pattern (explicit checklist of Success Criteria 1-5, played through and confirmed OK)

### Wave 0 Gaps
- No automated test framework exists in this Unity project (confirmed: zero `.asmdef`, zero `Tests/` folders). This is a pre-existing condition, not something to remediate within this phase — the project's established validation method for VFX/timing-sensitive features is manual playtest sign-off (see `13-04-PLAN.md` precedent). No Wave 0 test-infrastructure task is recommended; continue the existing manual-verification convention.

## Sources

### Primary (HIGH confidence — direct source read, current working-tree state as of 2026-07-10)
- `Assets/Scripts/World/EnemySpawner.cs` — full file read, verified `Spawn()`/`Activate()` current shape
- `Assets/Scripts/World/WorldGenerator.cs` — full file read (569 lines), verified `TrySpawnEnemies()` (390-411), `GetEnemyCount()` (378-383), `SpawnNextPair()`/`SpawnPrevPair()`, `_playerCurrentIndex`/`UpdatePlayerIndex()` (458-490), `FloorTransitionSequence()` (502-568) including the Step 4 no-op comment (558-559)
- `Assets/Scripts/World/FloorTransitionEffect.cs` — full file read, verified `PlayEntry()`/`PlayExit()` exact structure and `ScaleTransform()` helper
- `Assets/Scripts/World/RuntimeMaskSprite.cs` — full file read, verified `CreateMaskSprite()` signature and caching
- `Assets/Scripts/Enemy/IEnemy.cs` — full file read, verified exact 3-member contract
- `Assets/Scripts/Enemy/MeleeEnemy.cs` — full file read, verified `IsAlive { get; private set; }`, `Update()` guard (line 120-122), `OnDashHit()` (81-98)
- `Assets/Scripts/Enemy/RangedEnemy.cs` — full file read, verified identical `IsAlive`/`Update()` guard pattern (line 124-126)
- `Assets/Scripts/Player/CombatController.cs` — read lines 1-130 and 363-424, verified `FindNearestEnemyInRange()` skip check at line 400, `_enemyFilter` layer mask setup (Awake, line 94-96)
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` — full file read, verified AddComponent+StartCoroutine convention and SpriteMask reveal pattern
- `Assets/Editor/PortalEffectBuilder.cs` — full file read, verified how `PortalEffect.prefab` was authored (no Collider, single SpriteRenderer)
- `Assets/Prefabs/World/PortalEffect/PortalEffect.prefab` — read raw YAML, verified single-GameObject structure (Transform + SpriteRenderer only)
- `Assets/Scripts/Audio/AudioManager.cs` — full file read, verified `Sfx` enum values and `PlaySfx()` signature
- `Assets/Scripts/World/ExitPortal.cs` — read, verified `StandbyRoom` property and trigger-based portal pattern (parallel reference, not directly reused)
- Repo `grep` search confirming zero `EnemySpawner` component references in any Corridor prefab (`Assets/Prefabs/Corridors/Corridor_{Up,Flat,Down}/*.prefab`)
- `.planning/config.json` — verified `nyquist_validation: true`
- Repo-wide `find` search confirming no `.asmdef`/test files exist anywhere under `Assets/`

### Secondary (MEDIUM confidence)
- None needed — no external/web research was required for this phase; all findings are internal-repo verification.

### Tertiary (LOW confidence)
- None.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; all reused assets verified directly by reading source
- Architecture: HIGH for reused patterns (PortalEffect/RuntimeMaskSprite/AudioManager); MEDIUM for the new Corridor-entry-detection design (Pattern 3) since it is a novel design not yet validated against actual playtest — flagged as Open Question 2
- Pitfalls: HIGH — all 5 pitfalls derived from direct code reading (field-initializer timing, 4-call-site funnel, hardcoded Vector3.one scale target, standby-room nesting), not speculation

**Research date:** 2026-07-10
**Valid until:** Effectively indefinite for the internal-repo findings (they only go stale if the referenced files are modified before planning/execution) — recommend re-verifying line numbers if more than ~1 week elapses before `/gsd:plan-phase 14` runs, since `RollController.cs`/prefabs in this repo currently have uncommitted local edits in flight (see git status) indicating active development churn.
