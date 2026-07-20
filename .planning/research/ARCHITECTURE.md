# Architecture Research: Boss Expansion, Combat Module Pluggability, Unlock Progression, Game Modes

**Domain:** Integration architecture for adding 4 boss FSMs + pluggable player combat modules + persistent unlock progression + 2 game modes onto an existing Unity 2D mobile platformer
**Researched:** 2026-07-20
**Confidence:** HIGH (all findings grounded in direct reads of the actual codebase, not training-data assumptions)

> Supersedes the previous version of this file (dated 2026-07-08), which covered the v3.1 boss-room/VFX/audio milestone. This version covers the v4.0 milestone: 4 new distinct boss FSMs, pluggable player combat modules, persistent unlock progression, and 2 game modes.

## Method Note

This research is 100% codebase-derived. Every claim below traces to a specific file read during this session: `CombatController.cs`, `BossEnemy.cs`, `EnemyBase.cs`, `IEnemy.cs`, `ISpawnGatable.cs`, `MeleeEnemy.cs`, `ChronoGaugeController.cs`, `RollController.cs`, `AttackSelectController.cs`, `AttackTypeSelector.cs`, `MainMenuController.cs`, `PlayerDeathHandler.cs`, `DeathScreenController.cs`, `GameBootstrapper.cs`, `AudioManager.cs`, `ScoreManager.cs`, `FloorManager.cs`, `InputManager.cs`, `WorldGenerator.cs` (header), and `.planning/PROJECT.md`. No web research was needed — this is a pure internal-architecture question, not an ecosystem/library question.

## Current System Overview

```
┌───────────────────────────────────────────────────────────────────────┐
│  Scene Flow: MainMenu ──▶ AttackSelect ──▶ SampleScene                │
│  (MainMenuController)   (AttackSelectController,   (gameplay)         │
│                           AttackTypeSelector.SetType)                  │
├───────────────────────────────────────────────────────────────────────┤
│  Cross-scene survivors (bootstrapped via RuntimeInitializeOnLoadMethod)│
│    - GameBootstrapper: forces MainMenu on first load                  │
│    - AudioManager: MonoBehaviour singleton, DontDestroyOnLoad          │
│  Cross-scene DATA-ONLY statics (no MonoBehaviour lifecycle, reset      │
│  explicitly on restart, NOT persisted to disk):                       │
│    - FloorManager.CurrentFloor        (int)                           │
│    - ScoreManager.Score               (int, + bonuses)                │
│    - AttackTypeSelector.Selected      (enum, MonoBehaviour-backed      │
│                                         static — set via zone triggers)│
├───────────────────────────────────────────────────────────────────────┤
│  Player (single prefab, components composed side-by-side):            │
│    PlayerController  — movement/jump/fall-recovery, InputLocked gate   │
│    CombatController  — HARDCODED single module: Overclock (hold=slow  │
│                         mo+range, release=dash-teleport-OHK). Reads    │
│                         AttackTypeSelector.Selected only to pick       │
│                         Linear vs Fan SHAPE — not a different module.  │
│    ChronoGaugeController — slowmo resource, independent of module     │
│    RollController    — i-frame dodge, independent of module           │
│    InvincibilityHandler — shared i-frame primitive (dash + roll reuse) │
├───────────────────────────────────────────────────────────────────────┤
│  Enemies (two parallel lineages, NOT unified):                        │
│    EnemyBase (abstract) → MeleeEnemy, RangedEnemy                     │
│       shares: OnDashHit() death sequence, ClearHighlight(),           │
│       IsPlayerInRange(), OnEnable/OnDisable death-listener,            │
│       SetSpawnGate(). IsAlive == literal alive/dead.                  │
│    BossEnemy (standalone, MonoBehaviour : IEnemy, ISpawnGatable)       │
│       does NOT inherit EnemyBase — deliberate: IsAlive is OVERLOADED   │
│       to mean "currently in Vulnerable window", not "alive". Death is │
│       tracked by a separate _isDefeated flag. Telegraph→Attack→        │
│       Vulnerable single-pattern loop, 7-hit kill, own Die() sequence.  │
│       (Phase 15/16 work on this is currently BLOCKED/parked per        │
│       PROJECT.md — WorldGenerator spawn-pool integration incomplete)   │
├───────────────────────────────────────────────────────────────────────┤
│  World: WorldGenerator (Instance singleton) — infinite bidirectional   │
│  Room+Corridor chain gen/cleanup, exit-portal spawn, enemy-spawn       │
│  scheduling, floor-transition sequencing. Highest fan-in file in the   │
│  codebase (touches PlayerController, CombatController, EnemySpawner,   │
│  ExitPortal, FloorTransitionEffect, ScoreManager, FloorTimer).         │
└───────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities (existing, verified)

| Component | Responsibility | Persistence model |
|-----------|----------------|--------------------|
| `CombatController` | Slow-mo lifecycle, dash execution, whiff, hit-freeze, `_isBusy` lockout | None — per-scene instance |
| `ChronoGaugeController` | Slowmo resource drain/regen | None |
| `IEnemy` (interface) | 3-member contract: `IsAlive`, `OnDashHit()`, `ClearHighlight()` — explicitly documented as "sole interface members," locked | N/A |
| `EnemyBase` | Shared plumbing for Melee/Ranged: death sequence, spawn-gate, player-death listener | None |
| `BossEnemy` | Independent IEnemy implementation, Telegraph→Attack→Vulnerable loop | None |
| `FloorManager` | `CurrentFloor` int | In-memory static, explicitly reset to 1 on restart (`DeathScreenController.RestartGame()`) |
| `ScoreManager` | Score + bonus accumulation, "data-only, no scene lifecycle" (its own doc comment) | In-memory static, explicitly reset on restart |
| `AttackTypeSelector` | Linear/Fan shape choice | In-memory static, MonoBehaviour-backed, never persisted or reset |
| `AudioManager` | SFX playback pool | MonoBehaviour singleton, `DontDestroyOnLoad`, bootstrapped via `RuntimeInitializeOnLoadMethod` |
| `GameBootstrapper` | Forces MainMenu as entry scene | Static bootstrap, no state |

**Critical finding: nothing in this codebase persists to disk today.** `PlayerPrefs` is not used anywhere in `Assets/Scripts`. All "persistence" across scene loads is in-memory statics that are either explicitly reset (Score, Floor) or simply never reset (AttackTypeSelector). This means the unlock-progression requirement ("survives across deaths/runs") is a genuinely new capability class for this project, not an extension of an existing pattern.

## Question 1: Restructuring CombatController for Pluggable Combat Modules

### What must NOT change

`CombatController`'s Update loop currently fuses three concerns that are actually independent:
1. **Slow-mo lifecycle** (`EnterSlowMotion`/`ExitSlowMotion`, `_isBusy` lockout, gauge draining, safety timeout) — this is Overclock's *presentation frame*, but conceptually it is also probably shared by DeadEye/SAMURAI/MAX/NOVA (all are "hold=commit, release=resolve" per PROJECT.md's target descriptions: aim/reload, parry-timing, momentum-charge, dual-control). Confidence: MEDIUM — this must be confirmed against the actual boss-mechanic specs when phases are planned, since MAX ("순수 속도/관성 이동=공격") may not use hold-to-slowmo at all.
2. **Targeting + shape** (`FindNearestEnemyInRange`, `IsInAttackShape`) — Linear/Fan are variations of this, already parameterized by `AttackTypeSelector.Selected`.
3. **Resolution behavior** (`ExecuteDash` teleport-lerp-and-kill, `ExecuteWhiff`) — this is the part that is fundamentally different per new module: DeadEye resolves as a multi-shot burst not a single dash; SAMURAI's core loop isn't "player dashes to boss," it's "player reacts inside a boss-driven window"; MAX is stated as "이동=공격" (movement itself IS the attack, likely no teleport at all); NOVA needs to resolve against two objects.

Given concern #3, **the resolution step cannot be parameterized by a shape enum — it needs a real polymorphic seam.** Concern #1 (slow-mo host loop) is much more stable/shared and should stay in `CombatController`.

### Recommendation: Strategy pattern, CombatController becomes the host

Do not rewrite `CombatController` from scratch. Extract only the parts proven above to vary (targeting + resolution) behind a small interface; keep everything else (gauge integration, `_isBusy` lockout, slow-mo enter/exit, input polling, hit-freeze, camera shake, animator triggers) exactly as-is in `CombatController`, since those are proven, playtested, and slowmo-immune-timer-correct (`WaitForSecondsRealtime` usage throughout — do not touch).

```csharp
// New file: Assets/Scripts/Player/Combat/IPlayerCombatModule.cs
public interface IPlayerCombatModule
{
    // Called every Update while attack-pending, mirrors current UpdateHighlight() call site
    IEnemy FindTarget(Vector2 origin, Vector2 aimDir, float searchRadius);

    // Replaces ExecuteDash — owns its own resolution animation/physics.
    // CombatController still owns ExitSlowMotion() timing, invincibility, hit-freeze,
    // camera shake, and gauge bonus — module returns control via a simple bool/enum result.
    IEnumerator Resolve(IEnemy target, CombatContext ctx);

    IEnumerator Whiff(CombatContext ctx); // replaces ExecuteWhiff — some modules may not "whiff" (MAX has no target-miss branch if movement=attack)
}
```

`CombatContext` is a small struct/class carrying the refs `ExecuteDash` currently closes over locally (`_rb`, `_spriteRenderer`, `_invincibilityHandler`, `_trailRenderer`, `_animator`, `_cameraFollow`, tunables) — passed in so modules don't need `GetComponent` calls of their own.

Migrate the **existing** `ExecuteDash`/`ExecuteWhiff`/`IsInAttackShape`/`FindNearestEnemyInRange` bodies verbatim into a new `OverclockModule : IPlayerCombatModule` class with zero logic changes — this is a pure move, not a rewrite, so the existing playtested feel is preserved exactly. `CombatController.DashOrWhiff()` becomes:

```csharp
var target = (cachedTarget != null && cachedTarget.IsAlive) ? cachedTarget : _activeModule.FindTarget(...);
yield return target != null ? _activeModule.Resolve(target, ctx) : _activeModule.Whiff(ctx);
```

`_activeModule` is swapped based on unlock selection (see Question 3/4) — either at `Start()` for 한계 시험 (locked for the run) or on a mid-combat swap event for 보스 러시 (only allowed when `!_isBusy`, i.e., between attacks — reuse the existing lockout as the natural swap-safety gate, no new guard needed).

**Why not per-module MonoBehaviour components (like RollController sits beside CombatController)?** Because the modules need tight interaction with the slow-mo/gauge/lockout state machine that already lives in `CombatController` (see `_isBusy`, `_isAttackPending`, `_gauge.IsEmpty` checks interleaved through `Update()`). Making each module a separate always-enabled sibling component would require either (a) 5x duplicating the whole Update loop, or (b) a fragile enable/disable dance across components. A single host + swappable strategy object keeps exactly one `Update()` driving the state machine, which matches this codebase's existing preference for one authoritative state owner (see `EnemyBase`/`BossEnemy` also each having exactly one `Update`-or-coroutine driver).

**Risk called out explicitly:** MAX's "movement IS the attack" description suggests it may not fit the hold-slowmo→release-resolve shape at all — it may need its own always-active velocity/momentum system running in parallel to (or instead of) the slow-mo branch. Flag this for phase planning: confirm whether MAX module implements `IPlayerCombatModule` normally (with a degenerate `Resolve` that's basically instant) or needs a structurally different host hook. Do not assume the interface fits until MAX's phase is planned — this is the single highest-uncertainty item in this section.

## Question 2: Generalizing BossEnemy's FSM vs. Per-Boss IEnemy Implementations

### Follow the existing precedent, don't fight it

`BossEnemy.cs` is *already* a deliberate exception to the `EnemyBase` hierarchy, with an explicit, well-documented reason: its `IsAlive` means "currently vulnerable" rather than "alive," which is structurally incompatible with `EnemyBase`'s plain alive/dead semantics, and the doc comments explicitly reference this as a **locked design decision** (see `BossEnemy.cs` header comment citing `15-RESEARCH.md Pitfall 2`). This precedent generalizes cleanly to the new bosses because **the 4 new bosses do not share a single FSM shape with each other, or with the current BossEnemy**:

| Boss | Core loop (per PROJECT.md) | Structural difference from Telegraph→Attack→Vulnerable |
|------|------|------|
| DeadEye | 6-shot aim/reload resource management | "Vulnerable" = reload window, driven by a shot counter, not a fixed timer |
| SAMURAI | Parry-window melee | Needs a NEW interaction contract (player must react inside a boss-driven timing window) — this isn't just a faster/slower Telegraph, it's a different verb |
| MAX | Momentum/wall-bounce-stun | "Vulnerable" is triggered by a *physics collision event* (wall impact), not a timer at all |
| NOVA | Dual-body + orb evasion/harassment | Two coordinated objects; unclear which one (or both) should be `IEnemy`-targetable |

Given this, **do not build one generalized `BossFSM` base class that tries to parametrize Telegraph/Attack/Vulnerable for all four** — that would be premature abstraction fighting four genuinely different state shapes, and directly against this project's "minimal common extraction, not full inheritance" convention (see `EnemyBase`'s own header: *"풀 상속 리팩토링이 아니라 사용자 지시... 최소 범위 추출"*).

### Recommendation: extract only what BossEnemy already proves is boss-universal

Create `BossEnemyBase` (a sibling to `EnemyBase`, NOT inheriting from it — same reasoning as `BossEnemy` itself) that factors out the parts of the *current* `BossEnemy.cs` that are pattern-agnostic:

- `_isDefeated` flag + hit-count-to-death guard pattern (the `OnDashHit()` "guard only on `_isDefeated`, never on `IsAlive`" rule — this is the single most important invariant to preserve, since it's the fix for a documented race-condition pitfall)
- `Die()` sequence: stop rigidbody, disable colliders, `EnemyDeathEffect` trigger, camera shake, `ScoreManager.AddBossKillScore()`
- `OnPlayerDied` / `PlayerController.OnPlayerDeath` subscribe-unsubscribe wiring
- `ISpawnGatable.SetSpawnGate()` wiring + the "IsAlive gates targetability, not death" pattern as a protected helper each subclass's pattern loop calls into (e.g., `protected void EnterVulnerable()` / `protected void ExitVulnerable()` that toggle `IsAlive` + color tint consistently, since `ClearHighlight()`'s vulnerable-tint-aware override is exactly the kind of easy-to-forget detail that should be centralized once rather than reimplemented 4 times)

Leave the actual pattern loop (the coroutine equivalent of `PatternLoop()`) **abstract**, implemented independently per boss. Each of `DeadEyeBoss`, `SamuraiBoss`, `MaxBoss`, `NovaBoss` gets its own concrete class inheriting `BossEnemyBase`, with its own state enum and coroutine — exactly mirroring how `MeleeEnemy`/`RangedEnemy` each keep their own state enum and coroutine on top of the shared `EnemyBase`.

### SAMURAI's parry: do not extend the `IEnemy` contract

`IEnemy` is explicitly documented as closed: *"D-01: IsAlive, OnDashHit(), ClearHighlight() are the sole interface members."* Parry needs a genuinely new signal (a window during which a specific player input, not a dash, produces an effect). Recommend **not** adding a 4th member to `IEnemy` — instead give `SamuraiBoss` its own public method (e.g. `bool TryParry()`) called directly by a new player-side parry check (either inside the SAMURAI-specific combat module from Question 1, or a small dedicated `ParryController` sibling to `RollController`) via a direct component reference or an overlap check against the boss's own hitbox — this mirrors how `BossEnemy` already handles its own `OnTriggerEnter2D` for melee-kill entirely outside the `IEnemy` contract. Keeping `IEnemy` closed avoids forcing MeleeEnemy/RangedEnemy/every other boss to grow a no-op `TryParry()` they don't need.

### NOVA's dual body: flag as an open design decision, do not default-implement

Two defensible options exist and the choice has real gameplay consequences (whether the orb is a valid dash target):
1. **NOVA-as-single-authority:** `NovaBoss` is the only `IEnemy`; the orb is a non-`IEnemy` hazard/attacker object (its own `MonoBehaviour` with a trigger-based projectile-like behavior) that cannot itself be dashed to. Simpler, reuses existing single-target-per-dash `CombatController` contract unchanged.
2. **Dual-targetable:** both body and orb implement `IEnemy` independently, each trackable/targetable, boss "defeated" only when both are eliminated (or the orb is temporary/respawning). More faithful to "이원화 조작" but doubles state-tracking complexity and interacts with `FindNearestEnemyInRange`'s single-nearest-target assumption in ways that need explicit design.

This is a game-design call, not an architecture call — surface it explicitly during NOVA's phase-planning discussion rather than silently picking one. Recommendation for planning purposes: default to option 1 (simpler) unless the design brief for NOVA specifically requires the orb to be killable independently.

## Question 3: Where Persistent Unlock-State Should Live

### PlayerPrefs-backed static class, following the FloorManager/ScoreManager convention exactly

This codebase has an established, explicit convention (stated verbatim in `ScoreManager.cs`'s own doc comment) of **data-only static classes with no MonoBehaviour lifecycle** for cross-scene state that isn't tied to a `MonoBehaviour`'s per-frame needs (`FloorManager`, `ScoreManager`). `AudioManager` is the only MonoBehaviour-singleton-with-`DontDestroyOnLoad` in the project, and it needs that pattern specifically because it owns `AudioSource` pooling and a `LateUpdate` pitch-follow — needs that don't apply to unlock data.

Recommend a new static class, e.g. `Assets/Scripts/Progression/BossUnlockManager.cs`:

```csharp
public static class BossUnlockManager
{
    private const string PrefsKeyPrefix = "boss_unlock_";
    private static readonly Dictionary<BossModuleId, bool> _cache = new();

    public static bool IsUnlocked(BossModuleId id) { ... } // lazy-load from PlayerPrefs on first query
    public static void Unlock(BossModuleId id)
    {
        PlayerPrefs.SetInt(PrefsKeyPrefix + id, 1);
        PlayerPrefs.Save(); // flush immediately — survives app kill right after a boss kill
        _cache[id] = true;
    }
}
```

**Why PlayerPrefs over a ScriptableObject asset:** ScriptableObjects are excellent for *authored, designer-edited* data (e.g., per-boss tunables) but by default do **not** persist runtime changes back to disk in a built player — you'd need a separate JSON/file-write layer underneath them anyway, which is strictly more moving parts than `PlayerPrefs` for a problem this small (a handful of booleans). PlayerPrefs is built into Unity, works identically on the Android target (confirmed: no special permissions, no external storage), and requires zero new packages. Recommend reserving ScriptableObjects for boss *tuning* data (per-boss speed/damage/timing values, which this project already expresses as `[SerializeField]` tunables on each boss script) — not for unlock state.

**Why not fold it into `ScoreManager`/`FloorManager` directly:** those are explicitly reset on every restart (`DeathScreenController.RestartGame()`). Unlock state must survive exactly the opposite way — it must NOT be touched by that reset call. Keeping it in its own class makes "what gets reset on death" and "what survives forever" a structural (file-level) distinction instead of a per-field one buried inside a shared reset method — much harder to accidentally regress.

**Integration point:** `DeathScreenController.RestartGame()` (currently resets `FloorManager.CurrentFloor` and calls `ScoreManager.Reset()`) must NOT be extended to touch `BossUnlockManager` — this is the one line in the codebase most likely to be incorrectly "completed" by adding a reset call there out of habit. Call this out explicitly in the phase plan for whoever implements it.

**Boss-kill → unlock wiring:** each `BossEnemyBase.Die()` (or the shared base's death sequence) should call `BossUnlockManager.Unlock(thisBossModuleId)` alongside the existing `ScoreManager.AddBossKillScore()` call — same call site, same trigger event, no new event/observer plumbing needed.

## Question 4: Where Mode-Selection Should Live in the Scene Flow

### Current flow is a straight line with one decision point; it now needs two

`MainMenuController.OnStartClicked()` → loads `"AttackSelect"` unconditionally. `AttackSelectController` has exactly two button handlers (`OnLinearClicked`/`OnFanClicked`), each doing `AttackTypeSelector.SetType(...)` then `SceneManager.LoadScene("SampleScene")`. This is a single binary choice with no branching logic elsewhere — it's the simplest possible scene controller in the project, which is good news: there's no hidden coupling to unwind.

v4.0 needs to insert **two** new decisions upstream of `SampleScene`:
1. **Which game mode** — 한계 시험 (single module locked for the run) vs 보스 러시 (free swap, endless)
2. **Which combat module** — gated by `BossUnlockManager`, and whose *selection semantics* differ by mode (한계 시험: pick one, locked; 보스 러시: pick a starting one, or possibly skip straight in since swapping is free anyway)

Recommend: **MainMenu → ModeSelect → ModuleSelect → SampleScene**, where `ModuleSelect` is `AttackSelectController` extended (not replaced) to read `BossUnlockManager.IsUnlocked(id)` per button and gray-out/disable locked options, plus a new `GameModeManager` static (same data-only convention as `FloorManager`) storing the chosen mode, set by a new `ModeSelectController` before the existing module-select scene loads.

```csharp
public static class GameModeManager
{
    public enum Mode { LimitTrial, BossRush } // 한계 시험 / 보스 러시
    public static Mode Selected { get; private set; } = Mode.LimitTrial;
    public static void SetMode(Mode mode) => Selected = mode;
}
```

Reasons for this ordering (mode before module) rather than the reverse: 보스 러시's module choice is a "starting" pick with free swap thereafter, while 한계 시험's is a permanent commitment — the module-select screen's own UI behavior (e.g., whether it shows a "locked for this run" warning) depends on knowing the mode first. Doing it the other way (module before mode) would require the module screen to conditionally re-render based on a *later* choice, which is backwards.

**Whether ModeSelect is a new scene or a second panel inside the existing AttackSelect scene** is a UI/UX decision, not an architecture-critical one — both are equally cheap given the current scene-flow pattern is just `SceneManager.LoadScene(string)` calls with no passed parameters (state travels via the static classes, not scene params). Recommend leaving this choice to whoever builds the UI phase; either works with the static-class data flow described above.

**`DeathScreenController.RestartGame()` must become mode-aware:** currently it hardcodes `SceneManager.LoadScene("AttackSelect")`. In 한계 시험 mode this is probably still correct (return to module pick since the run is over). In 보스 러시 mode ("엔드리스"), death might restart differently (e.g., back to `ModeSelect`, or straight back into `SampleScene` with the same freely-swappable module set, no need to re-lock a module). This branch needs a design decision during phase planning — flagged here as a concrete integration point that will break silently if `RestartGame()` is left as an unconditional hardcoded scene name.

## New vs. Modified Component Summary

| File | New / Modified | Notes |
|------|------|------|
| `Assets/Scripts/Player/Combat/IPlayerCombatModule.cs` | **New** | Strategy interface extracted from `CombatController` |
| `Assets/Scripts/Player/Combat/OverclockModule.cs` | **New** | Verbatim move of existing `ExecuteDash`/`ExecuteWhiff`/targeting logic — zero behavior change |
| `Assets/Scripts/Player/Combat/DeadEyeModule.cs` | **New** | Reload-gated ranged module |
| `Assets/Scripts/Player/Combat/SamuraiParryModule.cs` (or a `ParryController` sibling) | **New** | Parry-timing player-side logic |
| `Assets/Scripts/Player/Combat/MaxMomentumModule.cs` | **New** | Highest interface-fit risk — may need host-hook changes |
| `Assets/Scripts/Player/Combat/NovaDualModule.cs` | **New** | Depends on NOVA design decision (Q2) |
| `Assets/Scripts/Player/CombatController.cs` | **Modified** | Gutted to host: slow-mo lifecycle, gauge integration, `_isBusy` lockout, hit-freeze, camera shake, animator triggers, input polling. Delegates targeting/resolution to `_activeModule` |
| `Assets/Scripts/Enemy/Boss/BossEnemyBase.cs` | **New** | Sibling to `EnemyBase` (not inheriting) — extracted from current `BossEnemy.cs`: `_isDefeated` guard, `Die()` sequence, player-death wiring, spawn-gate wiring, vulnerable-tint-aware highlight helpers |
| `Assets/Scripts/Enemy/BossEnemy.cs` | **Modified or retired** | Either becomes the reference implementation refactored onto `BossEnemyBase`, or stays as a generic test-boss fallback — clarify during phase planning whether this file represents F.I.O.R.A's boss-side counterpart or is purely scaffolding from the parked Phase 15/16 work |
| `Assets/Scripts/Enemy/Boss/DeadEyeBoss.cs`, `SamuraiBoss.cs`, `MaxBoss.cs`, `NovaBoss.cs` | **New** | Each inherits `BossEnemyBase`, own state enum + coroutine |
| `Assets/Scripts/Enemy/IEnemy.cs` | **Not modified** | Contract stays closed at 3 members — parry and other boss-specific signals go through side-channel APIs, not this interface |
| `Assets/Scripts/Enemy/ISpawnGatable.cs` | **Not modified** | Reused as-is by all new bosses |
| `Assets/Scripts/Progression/BossUnlockManager.cs` | **New** | Static, PlayerPrefs-backed, first use of disk persistence in this project |
| `Assets/Scripts/World/GameModeManager.cs` | **New** | Static, data-only, mirrors `FloorManager` convention |
| `Assets/Scripts/UI/ModeSelectController.cs` | **New** | Or a second panel inside the existing AttackSelect scene |
| `Assets/Scripts/UI/AttackSelectController.cs` → module-select | **Modified** | Extended to read `BossUnlockManager.IsUnlocked()` per option and reflect `GameModeManager.Selected` semantics |
| `Assets/Scripts/UI/MainMenuController.cs` | **Modified** | `OnStartClicked()` target scene changes to the new first selection step |
| `Assets/Scripts/UI/DeathScreenController.cs` | **Modified** | `RestartGame()` becomes mode-aware; must NOT touch `BossUnlockManager` |
| `Assets/Scripts/World/WorldGenerator.cs` | **Modified (last, highest risk)** | 보스 러시's endless boss-only loop needs either a mode-flag branch here or a parallel simpler generator — this is the single highest fan-in file in the codebase and the exact analog of "WorldGenerator saved for last" from the v3.0 milestone |
| `Assets/Scripts/World/ScoreManager.cs` | **Possibly modified** | Minor — per-module or per-mode scoring variants if needed |

## Data Flow: Unlock Persistence

```
[Boss Die() sequence, per boss subclass or shared BossEnemyBase]
    ↓
BossUnlockManager.Unlock(bossModuleId)
    ↓
PlayerPrefs.SetInt(key, 1) + PlayerPrefs.Save()   ← immediate flush, survives app kill
    ↓
[Later session] ModuleSelectController.Start()
    ↓
BossUnlockManager.IsUnlocked(id)  → gates which module buttons are enabled
```

## Data Flow: Mode Selection

```
MainMenu.OnStartClicked()
    ↓
ModeSelect scene/panel → GameModeManager.SetMode(LimitTrial | BossRush)
    ↓
ModuleSelect (extended AttackSelectController)
    - reads BossUnlockManager.IsUnlocked() per option
    - reads GameModeManager.Selected to decide "locked for run" vs "starting pick" framing
    ↓
CombatController.Start() in SampleScene reads the chosen module + GameModeManager.Selected
    - LimitTrial: _activeModule fixed for the run
    - BossRush: swap allowed when !_isBusy (reuses existing lockout as the safety gate)
    ↓
WorldGenerator (mode-branch, built last): LimitTrial reuses existing infinite Room+Corridor
loop; BossRush needs its own boss-only endless loop — design TBD, treat as its own phase
```

## Recommended Build Order (risk-ordered, per project history precedent)

This project's history shows a consistent pattern: build isolated, low-coupling pieces first, and save the highest-fan-in integration point for last (v3.0: Room/Tilemap rework in Phase 8 before `WorldGenerator`'s infinite-gen in Phase 9; v3.1/Phase 15: standalone boss-FSM test-room tooling — `RoomBossFsmTestBuilder`, `BossEnemyPrefabBuilder` — built and iterated on before attempting real `WorldGenerator` spawn-pool integration in Phase 16, which is exactly where that milestone got blocked). Apply the same discipline here:

1. **`BossUnlockManager` (persistent unlock-state).** Zero coupling to gameplay systems — pure data, first use of `PlayerPrefs` in the project. Build and unit-test in isolation first because every other piece (module-select UI, mode-select UI, even dev testing of the 4 new bosses) benefits from being able to fake-unlock things without a full boss-kill loop.

2. **`CombatController` → pluggable-module refactor, Overclock migrated as the first concrete module.** Pure refactor, no new content. Must be verified to produce byte-for-byte identical feel to the current shipped Overclock before any new module is added — this is a regression-proof step, not a feature step, and should be validated against the existing playtested SampleScene before moving on.

3. **SAMURAI boss + parry module.** PROJECT.md itself flags this as "튜토리얼 보스, 최우선 해금" (highest-priority unlock) — build first among the four both because the design intends it as the first thing players see, and because it's architecturally the closest to existing precedent (melee + telegraph, already proven by `MeleeEnemy`/current `BossEnemy`; parry is the only new primitive). Build in an isolated test room first (reuse the Phase 15 pattern: a dedicated test-room editor tool), not directly in the live spawn pool.

4. **DeadEye boss + reload-gated ranged module.** Next-lowest risk — reuses existing `RangedEnemy`/`ProjectileController` aim-line precedent; the only new piece is a shot-counter/reload state, no new physics or input paradigm.

5. **MAX boss + momentum/wall-bounce module.** Higher risk: "movement IS the attack" likely breaks the assumed hold-slowmo→release-resolve shape (flagged in Q1) and needs its own stun-via-collision detection (novel trigger source for "vulnerable," unlike a timer). Sequencing this after two normal cases (3, 4) means the `IPlayerCombatModule`/`BossEnemyBase` seams get stress-tested by conventional cases first, surfacing interface gaps before the outlier has to work around them.

6. **NOVA boss + dual-body/orb module.** Highest content risk of the four — two coordinated objects, and the orb-targetability question (Q2) needs a design decision before implementation starts. Build last among bosses so any interface adjustments discovered in steps 3-5 are already stable.

7. **Game modes (한계 시험 / 보스 러시) + `GameModeManager` + `ModeSelect` scene wiring + `WorldGenerator` mode-branch.** This is the true integration step, analogous to `WorldGenerator`'s Phase 9 role in v3.0 — depends on all 4 modules and the unlock system existing and individually validated. 보스 러시's endless boss-only floor loop is the single highest-risk piece in this entire milestone (touches `WorldGenerator`, already the most complex/fragile file in the codebase, and is exactly the kind of system-level design question — reuse vs. fork the chain-generator — that caused the Phase 15/16 blocking in the prior milestone). Do this decisively last, once every module/boss has been proven individually.

## Anti-Patterns to Avoid

### Anti-Pattern 1: Forcing all 4 bosses through one generalized Telegraph→Attack→Vulnerable state machine

**Why bad:** The four target mechanics (reload-counter, parry-timing, collision-triggered stun, dual-body) don't share a state shape — only superficially similar names ("windup," "vulnerable"). Forcing a shared FSM class would require exactly the kind of speculative parametrization this project's own conventions explicitly reject (see `EnemyBase`'s "minimal extraction, not full inheritance" comment). **Instead:** extract only the proven-universal boss plumbing (`BossEnemyBase`: death sequence, defeat-guard, spawn-gate, highlight-tint) and let each boss own its pattern loop independently.

### Anti-Pattern 2: Extending `IEnemy` to add boss-specific signals (parry, reload state, etc.)

**Why bad:** `IEnemy` is explicitly documented as closed at 3 members and is consumed generically by `CombatController.FindNearestEnemyInRange()`/`UpdateHighlight()` — every implementor (including `DummyEnemy`, `MeleeEnemy`, `RangedEnemy`) would need a no-op implementation of any new member, and `CombatController`'s generic targeting code has no reason to know about parry windows. **Instead:** boss-specific interactions (parry, reload) go through direct references/side-channel public methods on the concrete boss class, exactly as `BossEnemy.OnTriggerEnter2D` already handles player-kill outside the `IEnemy` contract.

### Anti-Pattern 3: Reflexively adding `BossUnlockManager` to `DeathScreenController.RestartGame()`'s existing reset call

**Why bad:** `RestartGame()` currently resets `FloorManager.CurrentFloor` and `ScoreManager.Score` by design — unlock state must do the *opposite* (never reset on death). Since these three would live in visually-adjacent lines of the same method, this is the single most likely "obvious-looking but wrong" edit in this milestone. **Instead:** keep unlock state in its own file with no reset method exposed at all — make the wrong edit structurally harder, not just documented against.

### Anti-Pattern 4: Building the 보스 러시 endless-mode floor loop before all 4 bosses + module system are individually proven

**Why bad:** This is precisely the ordering that caused the current Phase 15/16 blocking (attempting `WorldGenerator` integration before the boss-FSM pattern was fully validated in isolation, per the project's own git history: `BossFsmTestPoolSwapTool`/test-room tooling built specifically because direct `WorldGenerator` integration was deferred). **Instead:** validate every boss + module in an isolated test room first (mirroring the existing `RoomBossFsmTestBuilder` pattern), and treat the `WorldGenerator` mode-branch as the final, most-supervised step.

## Sources

- `Assets/Scripts/Player/CombatController.cs` (full read) — Overclock lifecycle, targeting, dash/whiff resolution
- `Assets/Scripts/Enemy/BossEnemy.cs` (full read) — current single-boss FSM, `IsAlive` overload rationale, defeat-guard pattern
- `Assets/Scripts/Enemy/EnemyBase.cs`, `IEnemy.cs`, `ISpawnGatable.cs`, `MeleeEnemy.cs` (full reads) — shared enemy plumbing, closed-contract precedent
- `Assets/Scripts/Player/ChronoGaugeController.cs`, `RollController.cs` (full reads) — sibling-component pattern, timeScale-immune timer convention
- `Assets/Scripts/UI/AttackSelectController.cs`, `AttackTypeSelector.cs`, `MainMenuController.cs`, `DeathScreenController.cs` (full reads) — current scene-flow and reset-on-restart behavior
- `Assets/Scripts/World/FloorManager.cs`, `ScoreManager.cs`, `GameBootstrapper.cs` (full reads) — data-only static class convention, explicit reset-on-restart behavior
- `Assets/Scripts/Audio/AudioManager.cs` (full read) — MonoBehaviour-singleton + `DontDestroyOnLoad` + `RuntimeInitializeOnLoadMethod` bootstrap pattern (used to justify why unlock-state should NOT follow this pattern)
- `Assets/Scripts/Player/InputManager.cs` (full read) — input action surface available for new module input needs
- `Assets/Scripts/World/WorldGenerator.cs` (header read) — confirmed as highest fan-in file, informs build-order-last recommendation
- `.planning/PROJECT.md` — milestone goal, target boss mechanics, key decisions log, confirmation that Phase 15/16 (boss-room-lifecycle) is currently blocked/parked
- `.planning/phases/15-fsm/` directory listing — confirms existence of isolated test-room tooling pattern (`RoomBossFsmTestBuilder`, `BossFsmTestPoolSwapTool`) as the established precedent for de-risking boss integration before touching `WorldGenerator`

---
*Architecture research for: Fast (가칭) v4.0 milestone — boss expansion, combat module pluggability, unlock progression, game modes*
*Researched: 2026-07-20*
