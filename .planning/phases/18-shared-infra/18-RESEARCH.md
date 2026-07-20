# Phase 18: 공유 인프라 — 전투 모듈 추상화 & 보스 베이스 - Research

**Researched:** 2026-07-20
**Domain:** Internal Unity/C# architecture refactor (no external library/ecosystem question) — extracting a Strategy-pattern combat module interface, a shared boss base class, and first-ever PlayerPrefs persistence from an existing, working, playtested Unity 6 2D codebase
**Confidence:** HIGH (every finding below is grounded in direct reads of the actual current source files in this repo, cross-checked against `.planning/research/ARCHITECTURE.md` and `.planning/research/PITFALLS.md` — both already 100% codebase-derived for this exact milestone. No Context7/WebSearch was needed; this is a pure internal-architecture question, not a library/framework question.)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**플랫폼 범위 재확정**
- **D-01:** 프로젝트 타겟 플랫폼을 Android/모바일에서 PC(Standalone, 마우스+키보드)로 영구 재설정 — 기획/로드맵 문서 전체(CLAUDE.md, PROJECT.md, ROADMAP.md, REQUIREMENTS.md, 기획서.md, research/*.md)에 반영 완료. Unity 엔진 Player Settings(Android AndroidMinSdkVersion 25/ARM64, iOS 15.0 등)는 의도적으로 변경하지 않음 — 문서 우선순위만 재조정.
- **D-02:** INFRA-02(터치 조준 입력)는 이번 Phase 범위에서 완전히 제거된다. `CombatController.GetMouseWorldDirection()`의 기존 `Mouse.current` 기반 방식은 그대로 유지 — 마이그레이션 대상 아님.

**F.I.O.R.A 보스 정체성**
- **D-03:** 기존 `BossEnemy.cs`(Phase 15에서 만든 유일한 보스 구현체)는 `BossEnemyBase` 추출과 동시에 `FioraBoss : BossEnemyBase`로 이름 붙여 명시적 정체성을 부여한다 — STORY.md/PROJECT.md가 F.I.O.R.A를 "이미 구현된 Overclock Mode의 원본"으로 이미 명시하고 있으므로, `PlayerPrefs` 언락키/향후 UI 표시 이름을 처음부터 일관되게 가져간다(예: boss id `"Fiora"`).

**INFRA-01 회귀 검증**
- **D-04:** `IPlayerCombatModule` 마이그레이션의 "회귀 없음" 검증은 **수동 플레이테스트만**으로 진행한다 — 자동화 PlayMode 테스트(오래 미완료 상태인 02-04-PLAN의 CombatTests/RollTests)는 이번 Phase 범위에 포함하지 않는다. 마이그레이션이 기존 로직의 verbatim move(로직 변경 없는 단순 이동)이므로 리스크가 낮다고 판단.

### Claude's Discretion
- `BossEnemyBase`/`FioraBoss` 파일 배치(기존 `Assets/Scripts/Enemy/` 플랫 폴더 유지 vs `Assets/Scripts/Enemy/Boss/` 하위 폴더 신설)
- `IPlayerCombatModule`/`OverclockModule`/`CombatContext`의 정확한 메서드 시그니처 — ARCHITECTURE.md Question 1의 제안을 기본 출발점으로 사용
- `BossUnlockManager`의 정확한 API 형태(딕셔너리 캐시, enum vs string id 등) — ARCHITECTURE.md Question 3의 제안을 기본 출발점으로 사용
- 수동 플레이테스트 체크리스트의 구체적 항목 구성

### Deferred Ideas (OUT OF SCOPE)
- **자동화 PlayMode 회귀 테스트(02-04-PLAN 완성)** — 이번 Phase는 수동 플레이테스트만 채택(D-04). 향후 필요성이 커지면 별도 Phase/quick task로 재검토.
- **Android/모바일 재지원** — D-01로 보류. 재검토 시점/조건은 아직 정해지지 않음, 필요 시 사용자가 명시적으로 재논의.
- **보스 러시 모드의 모듈 스왑 안전장치** — v4.0 전체 범위 밖(RUSH-01), Phase 18의 모듈 추상화는 스왑을 고려할 필요 없음(single Start()-time selection만 지원하면 충분).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| INFRA-01 | CombatController의 기존 Overclock(F.I.O.R.A) 로직이 IPlayerCombatModule 인터페이스로 무손상 마이그레이션된다 (기존 동작 100% 동일, 회귀 없음) | Architecture Patterns → Pattern 1 (IPlayerCombatModule extraction), Code Examples (verbatim CombatController.Update()/ExecuteDash() bodies to move), Common Pitfalls 1/3 (timeScale convention, monolith-branch anti-pattern), manual playtest checklist below |
| INFRA-03 | BossEnemy.cs에서 BossEnemyBase(EnemyBase와 별개의 형제 클래스)가 추출되어, 이후 신규 보스 4종이 이를 상속한다 | Architecture Patterns → Pattern 2 (BossEnemyBase extraction scope), Runtime State Inventory (GUID-preserving file rename for BossEnemy.prefab), Common Pitfalls 2 (copy-paste drift), Don't Hand-Roll (defeat-guard/death-sequence reuse) |
| UNLOCK-01 | 보스 격파 시 해당 보스의 전투 모듈이 영구 해금된다 (PlayerPrefs 기반, 앱 재시작 후에도 유지) | Architecture Patterns → Pattern 3 (BossUnlockManager), Common Pitfalls 6 (persistence scope, reset-sweep isolation), Code Examples (PlayerPrefs read/write/flush pattern) |
</phase_requirements>

## Summary

This phase is a pure internal refactor + one net-new persistence subsystem on an existing, working Unity 6 2D codebase — no new libraries, no ecosystem research needed. Three independent deliverables:

1. **`IPlayerCombatModule`**: extract only the *targeting* and *resolution* logic from `CombatController` (currently `FindNearestEnemyInRange`/`IsInAttackShape`/`ExecuteDash`/`ExecuteWhiff`) into a new interface, implemented first by `OverclockModule` as a **verbatim, zero-logic-change move**. Everything else — slow-mo lifecycle (`EnterSlowMotion`/`ExitSlowMotion`), `_isBusy` lockout, gauge integration, hit-freeze, camera shake, animator triggers, input polling — stays exactly where it is in `CombatController`, which becomes the module host.
2. **`BossEnemyBase`**: extract only the boss-universal plumbing already proven by the single existing `BossEnemy.cs` — the `_isDefeated` guard pattern (never gate on `IsAlive`, which is overloaded to mean "vulnerable"), the `Die()` sequence, `OnPlayerDied` subscribe/unsubscribe, `ISpawnGatable.SetSpawnGate()`, and the vulnerable-tint-aware `ClearHighlight()` override. The actual Telegraph→Attack→Vulnerable pattern loop stays in the concrete subclass (`FioraBoss`), because future bosses (parry/ammo/momentum/dual-control) do not share this state shape — this mirrors the project's own established "minimal extraction, not full inheritance" convention used for `EnemyBase`.
3. **`BossUnlockManager`**: a new data-only static class (same convention as `FloorManager`/`ScoreManager`), but PlayerPrefs-backed — this is the **first use of disk persistence anywhere in this project**. Must be structurally isolated from `DeathScreenController.RestartGame()`'s existing unconditional reset sweep (which resets `FloorManager.CurrentFloor` and `ScoreManager.Score`).

**Primary recommendation:** Follow `.planning/research/ARCHITECTURE.md` Questions 1/2/3 as the starting design (already codebase-derived and endorsed as the discretion baseline in CONTEXT.md) — do not re-litigate the architecture questions already answered there. This RESEARCH.md's job is to (a) confirm those recommendations still match the current source exactly, (b) surface one Unity-serialization gotcha not covered by ARCHITECTURE.md (GUID preservation when splitting `BossEnemy.cs`), and (c) flag an uncommitted-work interaction risk with the still-open Phase 15 checkpoint.

## Standard Stack

No new packages, no new libraries. This phase uses only Unity built-in APIs already present in the project:

| API | Purpose | Why Standard Here |
|-----|---------|--------------------|
| `PlayerPrefs.SetInt` / `GetInt` / `Save()` | Boss unlock flag persistence | Built into Unity, zero new packages, confirmed by `ARCHITECTURE.md` Question 3 as the right fit for "a handful of booleans" — ScriptableObjects don't persist runtime writes to disk in a built player without an extra file-write layer, which is unjustified complexity for this scope |
| C# `interface` (no MonoBehaviour) | `IPlayerCombatModule` | Matches existing `IEnemy`/`ISpawnGatable` closed-contract convention already used in this codebase |
| `WaitForSecondsRealtime` / `Time.unscaledDeltaTime` | Any new timer in `OverclockModule` (moved verbatim) or `BossEnemyBase` | Mandatory project-wide convention (CLAUDE.md, all prior phases) — slowmo/hit-freeze immune |

**No `npm view` / package-version verification applies** — this is a Unity project with no npm/pip dependency to check; `Packages/manifest.json` is unaffected by this phase.

## Architecture Patterns

### Recommended File Layout (Claude's Discretion area — recommended default)

```
Assets/Scripts/
├── Player/
│   ├── CombatController.cs              # MODIFIED — gutted to host: slow-mo lifecycle,
│   │                                     #   gauge, _isBusy lockout, hit-freeze, camera shake,
│   │                                     #   animator triggers, input polling. Delegates to _activeModule.
│   └── Combat/                          # NEW folder
│       ├── IPlayerCombatModule.cs       # NEW — strategy interface
│       ├── CombatContext.cs             # NEW — struct/class carrying shared refs (rb, sprite, invincibility, trail, animator, cameraFollow, tunables)
│       └── OverclockModule.cs           # NEW — verbatim move of ExecuteDash/ExecuteWhiff/FindNearestEnemyInRange/IsInAttackShape
├── Enemy/
│   ├── IEnemy.cs                        # NOT MODIFIED — stays closed at 3 members
│   ├── ISpawnGatable.cs                 # NOT MODIFIED — reused as-is
│   ├── EnemyBase.cs                     # NOT MODIFIED — sibling, not parent, of BossEnemyBase
│   └── Boss/                            # NEW folder (recommended — mirrors ARCHITECTURE.md's summary table)
│       ├── BossEnemyBase.cs             # NEW — extracted plumbing (see Pattern 2 below)
│       └── FioraBoss.cs                 # RENAMED from BossEnemy.cs (see Runtime State Inventory —
│                                         #   MUST preserve the .meta/GUID, do not delete+recreate)
└── Progression/                         # NEW folder
    └── BossUnlockManager.cs             # NEW — static, PlayerPrefs-backed
```

If flat-folder is preferred instead (keep `BossEnemyBase.cs`/`FioraBoss.cs` directly in `Assets/Scripts/Enemy/`), that's equally valid — this is explicitly Claude's discretion per CONTEXT.md. The `Boss/` subfolder is recommended only because ARCHITECTURE.md's "New vs. Modified Component Summary" table already anticipates it and it keeps the flat `Enemy/` folder from growing to 9+ files once DeadEye/SAMURAI/MAX/NOVA arrive in Phase 19-23.

### Pattern 1: `IPlayerCombatModule` — Strategy extraction, `CombatController` stays host

**What:** Extract only targeting (`FindNearestEnemyInRange`) and resolution (`ExecuteDash`/`ExecuteWhiff`) behind a small interface. Everything else stays in `CombatController` verbatim.

**When to use:** This exact phase, for `OverclockModule` only. Do NOT attempt to design the interface shape around DeadEye/SAMURAI/MAX/NOVA's mechanics yet — those are Phase 19-23's job. The interface only needs to fit the module we're actually migrating (Overclock) plus enough generality that ARCHITECTURE.md's Question 1 analysis (already done) holds.

**Verified against current source** (`Assets/Scripts/Player/CombatController.cs`, read in full this session):
- `CombatController.Update()` (lines 108-167) — input polling (`InputManager.Instance`), `_isBusy` gate, gauge drain (`_gauge.SetDraining`), slow-mo enter/exit (`EnterSlowMotion`/`ExitSlowMotion`), highlight update (`UpdateHighlight(FindNearestEnemyInRange())`), release-triggers-`DashOrWhiff` — **this loop stays in `CombatController`, unchanged**, except the `FindNearestEnemyInRange()` call becomes `_activeModule.FindTarget(...)`.
- `DashOrWhiff(IEnemy cachedTarget)` (lines 229-249) — already the exact seam ARCHITECTURE.md identifies: `target != null ? ExecuteDash(target) : ExecuteWhiff()` becomes `target != null ? _activeModule.Resolve(target, ctx) : _activeModule.Whiff(ctx)`.
- `ExecuteDash(IEnemy target)` (lines 251-301) — the body to move verbatim into `OverclockModule.Resolve()`. Note it calls back into host-owned things that must be passed via `CombatContext` or left as host callbacks: `ExitSlowMotion()` (host — call before module resolve, not inside it, since exiting slow-mo is explicitly host-owned per ARCHITECTURE.md), `_invincibilityHandler.StartInvincibility(...)`, `_trailRenderer`, `_animator`, `_spriteRenderer`, `_rb`, `_cameraFollow.Shake(...)`, `AudioManager.PlaySfx(Sfx.Slash)`, `SpawnHitSpark(...)`, `HitFreeze(...)` coroutine, `_gauge.AddKillBonus()`, `_attackCooldown = postKillLockout`. Most of these are genuinely host-owned (hit-freeze, gauge, cooldown) — only the dash-movement lerp + `target.OnDashHit()` + trail/sprite-flip visuals are truly module-specific.
- `ExecuteWhiff()` (lines 303-310) — trivially small, moves verbatim.
- `FindNearestEnemyInRange()`/`IsInAttackShape()`/`GetMouseWorldDirection()` (lines 351-434) — targeting logic, moves into module's `FindTarget()`. Note `GetMouseWorldDirection()` reads `AttackTypeSelector.Selected` for Linear/Fan shape — this stays exactly as-is (D-02 locked: not a migration target, `Mouse.current` usage untouched).
- `UpdateHighlight()` (lines 408-418) stays host-side (it's presentation, called every `Update()` frame regardless of module) — but it calls `_lastHighlighted.ClearHighlight()`/sets `sr.color = Color.red` via the `IEnemy` interface, which is module-agnostic already.

**Risk called out explicitly (from ARCHITECTURE.md, still valid):** Do not assume `IPlayerCombatModule`'s exact shape will fit MAX (Phase 22, "movement IS the attack") without revisiting during that phase's planning — this is out of scope for Phase 18 to solve, but the interface designed now should not *actively preclude* it (e.g., don't hardcode an assumption that `Resolve()` always teleports).

**Example (interface shape, ARCHITECTURE.md Question 1 recommendation — use as starting point):**
```csharp
// Assets/Scripts/Player/Combat/IPlayerCombatModule.cs
public interface IPlayerCombatModule
{
    IEnemy FindTarget(Vector2 origin, Vector2 aimDir, float searchRadius);
    IEnumerator Resolve(IEnemy target, CombatContext ctx); // replaces ExecuteDash
    IEnumerator Whiff(CombatContext ctx);                  // replaces ExecuteWhiff
}
```

### Pattern 2: `BossEnemyBase` — extract only what the current single boss proves universal

**What:** A sibling class to `EnemyBase` (NOT inheriting from it — same reasoning `BossEnemy.cs`'s own header comment already documents: `IsAlive` means "currently vulnerable," not "alive," which is structurally incompatible with `EnemyBase`'s plain alive/dead semantics).

**Verified against current source** (`Assets/Scripts/Enemy/BossEnemy.cs`, read in full — 302 lines):

Members that ARE boss-universal (safe to pull into `BossEnemyBase`):
- `_isDefeated` field + the invariant "`OnDashHit()` guards **only** on `_isDefeated`, never on `IsAlive`" (line 197: `if (_isDefeated) return;`) — this is the single most load-bearing rule in the file; a doc-comment explicitly traces it to a documented race-condition pitfall (15-RESEARCH.md Pitfall 2: gating on `IsAlive` would let the ~0.15s `ExecuteDash` travel time close the vulnerable window before the hit registers).
- `IsAlive { get; private set; } = true` property itself (same default as `EnemyBase`).
- `Die()` sequence (lines 256-274): stop rigidbody (`bodyType = RigidbodyType2D.Static`), disable all `Collider2D`, `animator.SetBool("isDead", true)`, `EnemyDeathEffect` `AddComponent`+`ConfigureIntensity(...)`+`PlayDeathSequence(...)`, `cameraFollow.Shake(...)`, `ScoreManager.AddBossKillScore()`. Recommend adding the `BossUnlockManager.Unlock(bossId)` call here too (or immediately adjacent) since it's the same trigger event — this is the natural single call site.
- `OnEnable`/`OnDisable` subscribe/unsubscribe to `PlayerController.OnPlayerDeath += /-= OnPlayerDied` (lines 96-104) — exact same pattern as `EnemyBase`, must preserve the paired subscribe/unsubscribe (Pitfalls doc: forgetting `OnDisable` unsubscribe crashes/double-fires next Play session since the event is `static`).
- `SetSpawnGate(bool isSpawning)` (lines 108-113) — `ISpawnGatable` implementation, including the "if not spawning and no pattern coroutine running and not defeated, start pattern loop" bootstrap logic — this interacts with the (per-boss) `PatternLoop()` coroutine, so the base class needs either a `protected abstract IEnumerator PatternLoop()` hook or a `protected abstract void StartPattern()` the subclass wires.
- The vulnerable-tint-aware `ClearHighlight()` override (lines 289-293): `_sr.color = (_state == BossState.Vulnerable) ? vulnerableTintColor : _baseColor;` — ARCHITECTURE.md flags this as "exactly the kind of easy-to-forget detail that should be centralized once." **Caveat:** `_state`/`vulnerableTintColor`/`_baseColor` are currently boss-specific fields tied to the Telegraph/Attack/Vulnerable enum — the base class needs a lighter-weight abstraction here, e.g. `protected abstract Color GetHighlightBaseColor()` or a `protected bool _isCurrentlyVulnerable` flag the subclass sets, since future bosses (parry/ammo/momentum) won't share the `BossState` enum shape. Do not just copy the field names — re-derive the minimal contract.
- `OnPlayerDied()` cleanup shape (stop pattern coroutine, hide exclamation icon, disable hitbox, `animator.SetBool("isMoving", false)`) — the "stop coroutine + disable child visuals" *shape* is universal; the specific child refs (`_exclamationIcon`, `_meleeHitbox`) are `FioraBoss`-specific (SAMURAI/DeadEye/MAX/NOVA won't have an identical exclamation-icon+melee-hitbox pair) — likely needs a `protected abstract void OnPlayerDiedCleanup()` hook rather than a hardcoded base implementation.

Members that must stay in `FioraBoss` (boss-specific, do NOT generalize):
- The entire `PatternLoop()` Telegraph→Attack→Vulnerable coroutine and its tunables (`moveSpeed`, `telegraphDuration`, `vulnerableDuration`, `hitboxActiveDuration`, etc.) — future bosses have fundamentally different "when am I killable" shapes (reload-counter, parry-punish-window, wall-collision-stun, dual-body), confirmed by ARCHITECTURE.md's comparison table.
- `RequiredHits = 7` hit-counter defeat condition — this is F.I.O.R.A/BOSS-04-specific, not universal. The base class's defeat trigger should be "subclass calls a protected `Defeat()` helper when its own condition is met," not "base class owns a hit counter."
- `HitReactionAndReset()` (flash+knockback+pause+restart-pattern) — boss-specific stagger feel; only the general shape (something happens on non-lethal hit, then pattern resumes) might be worth a template method, but the concrete flash color/knockback force/pause duration are `FioraBoss` tunables.
- `OnTriggerEnter2D` melee-kill-player logic — this uses `FioraBoss`'s own `_meleeHitbox` concept, which SAMURAI/DeadEye/MAX/NOVA will each implement differently (or not at all, in DeadEye's ranged case).

### Pattern 3: `BossUnlockManager` — static, PlayerPrefs-backed, isolated from restart-reset

**What:** A new data-only static class, following the exact `FloorManager`/`ScoreManager` convention already established (verified: `Assets/Scripts/World/FloorManager.cs` is 5 lines, `ScoreManager.cs` explicitly documents itself as "data-only, no scene lifecycle").

**Verified reset-sweep boundary** (`Assets/Scripts/UI/DeathScreenController.cs`, read in full — 38 lines): `RestartGame()` (lines 29-36) does exactly three things: reset `Time.timeScale`/`Time.fixedDeltaTime`, `FloorManager.CurrentFloor = 1`, `ScoreManager.Reset()`, then `SceneManager.LoadScene("AttackSelect")`. **`BossUnlockManager` must never be called from this method** — this is the single highest-risk "obvious-looking but wrong" edit in this phase (both ARCHITECTURE.md and PITFALLS.md flag it as Anti-Pattern 3 / Pitfall 6 independently).

**Recommended shape** (ARCHITECTURE.md Question 3 — use as starting point, confirmed still valid against current `ScoreManager`/`FloorManager` conventions):
```csharp
// Assets/Scripts/Progression/BossUnlockManager.cs
public static class BossUnlockManager
{
    private const string PrefsKeyPrefix = "boss_unlock_";
    private static readonly Dictionary<string, bool> _cache = new();

    public static bool IsUnlocked(string bossId)
    {
        if (_cache.TryGetValue(bossId, out var cached)) return cached;
        bool value = PlayerPrefs.GetInt(PrefsKeyPrefix + bossId, 0) == 1;
        _cache[bossId] = value;
        return value;
    }

    public static void Unlock(string bossId)
    {
        PlayerPrefs.SetInt(PrefsKeyPrefix + bossId, 1);
        PlayerPrefs.Save(); // flush immediately — survives app kill right after a boss kill
        _cache[bossId] = true;
    }
}
```
**String id vs enum (discretion call):** ARCHITECTURE.md's draft used a `BossModuleId` enum. Given D-03 already locks the boss id concept to `"Fiora"` as a string identity precedent (for future unlock-UI display names), a `string bossId` (or a small enum with an explicit `.ToString()`/display-name mapping) both work — this is explicitly Claude's discretion per CONTEXT.md. Recommend whichever is simpler to extend across Phase 19-23 without editing a shared enum file 4 more times; a `string` constant per boss class (e.g., `FioraBoss.BossId = "Fiora"`) avoids a central enum file becoming a cross-phase merge-conflict point.

**Call site:** `BossEnemyBase.Die()` (or `FioraBoss.Die()` if the base doesn't own the full sequence) calls `BossUnlockManager.Unlock(bossId)` alongside the existing `ScoreManager.AddBossKillScore()` — same trigger, same call site, no new event/observer plumbing.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cross-app-session save data | Custom JSON file writer, custom save-slot system | `PlayerPrefs.SetInt`/`GetInt`/`Save()` | Built into Unity, zero new packages, sufficient for a handful of booleans (ARCHITECTURE.md Question 3) — do not over-engineer a generic save system for a prototype-stage unlock flag set |
| Boss "am I killable right now" generalized state machine | A parametrized `BossFSM` base trying to cover Telegraph/Attack/Vulnerable/reload/parry/stun/dual-body in one shape | `BossEnemyBase` extracts only plumbing (defeat-guard, death sequence, spawn-gate, highlight); pattern loop stays per-subclass | Anti-Pattern 1 in ARCHITECTURE.md — four genuinely different state shapes, forcing one shared FSM is "premature abstraction fighting four different verbs," directly against this project's own "minimal extraction, not full inheritance" convention (see `EnemyBase`'s own header comment) |
| A 4th `IEnemy` member for boss-specific signals (parry, reload-state, etc.) | Extending the closed 3-member `IEnemy` contract | Direct side-channel public methods on the concrete boss class (e.g., future `SamuraiBoss.TryParry()`), exactly like `BossEnemy.OnTriggerEnter2D` already handles player-kill entirely outside `IEnemy` | `IEnemy` is explicitly documented as closed (`IEnemy.cs` line 5: "D-01: IsAlive, OnDashHit(), ClearHighlight() are the sole interface members") — every implementor (`DummyEnemy`, `MeleeEnemy`, `RangedEnemy`, all future bosses) would need a no-op implementation of any new member |

**Key insight:** This codebase has a hard-won, twice-documented convention (Phase 16's `EnemyBase` extraction, and now this phase's `BossEnemyBase` extraction) of extracting only the *proven-duplicated* subset, never speculatively generalizing ahead of need. Both this phase's own CONTEXT.md and the prior phase's precedent point the same direction — do not deviate toward a "cleaner" full-inheritance redesign.

## Runtime State Inventory

> This phase involves a rename/extraction of `BossEnemy.cs` into `BossEnemyBase.cs` + `FioraBoss.cs` (D-03) — Unity's asset-serialization model has GUID-based state that a plain file split can silently break. Documented explicitly per category below.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data (PlayerPrefs) | **None yet** — confirmed by direct grep: `PlayerPrefs` does not appear anywhere in `Assets/Scripts` today. `BossUnlockManager` is genuinely first-use; there is no existing key/value data to migrate. | None — pure greenfield addition, no migration needed. |
| Unity serialized asset references (prefab component GUIDs) | `Assets/Prefabs/Enemies/BossEnemy.prefab` (currently **untracked** in git) has one `MonoBehaviour` component serialized as `m_Script: {fileID: 11500000, guid: cb839023c498e514cab6bb76ab11cde9, type: 3}` — this GUID belongs to `Assets/Scripts/Enemy/BossEnemy.cs.meta`. Confirmed via direct grep: no other prefab or the scene (`SampleScene.unity`) currently reference this GUID (`Room_BossFsmTest.prefab` does NOT yet have the boss wired in — that wiring is the still-pending Phase 15 Task 3 human-action checkpoint, not yet executed). | **Rename, do not delete-and-recreate.** When creating `FioraBoss.cs`, rename the existing `BossEnemy.cs` file (and move its `.meta` alongside it, e.g. `git mv BossEnemy.cs FioraBoss.cs` + `git mv BossEnemy.cs.meta FioraBoss.cs.meta`) so the GUID `cb839023c498e514cab6bb76ab11cde9` is preserved — Unity resolves `MonoBehaviour` component references by this GUID, not by class name, so as long as the file (and its `.meta`) is renamed rather than deleted, `BossEnemy.prefab`'s existing serialized component (including its already-assigned `_exclamationIcon`/`_meleeHitbox` child references and any `[SerializeField]` tunable values) will continue to resolve correctly to the new `FioraBoss` class with zero data loss — Unity re-serializes MonoBehaviours by field name across the whole inheritance chain, so fields moved into the new `BossEnemyBase.cs` (a genuinely new file, new GUID, but referenced only as a C# base class, not as its own prefab component) keep their existing serialized values too. **Do not** delete `BossEnemy.cs` and create two brand-new files — that assigns fresh GUIDs to both, and `BossEnemy.prefab`'s component reference becomes "missing script" the next time Unity opens/serializes it. |
| Code references to the old type name | `Assets/Editor/BossEnemyPrefabBuilder.cs` line 42: `var boss = clone.AddComponent<BossEnemy>();` — a **hardcoded C# type reference**, currently untracked/uncommitted in git status. This will fail to compile once `BossEnemy` the class no longer exists under that name. | Update this line to `clone.AddComponent<FioraBoss>();` as part of the same task that performs the rename — this file is Editor-only tooling (not shipped in builds) but must still compile for the Editor to function at all (a compile error here blocks the whole Editor, not just this tool). `Assets/Editor/RoomBossFsmTestBuilder.cs` and `Assets/Editor/BossFsmTestPoolSwapTool.cs` were also checked — they reference `"Assets/Prefabs/Enemies/BossEnemy.prefab"` only by **path string**, not by C# type, so they need no code change (the prefab *file* itself is not being renamed, only the script class inside it). |
| OS-registered state / secrets / build artifacts | None applicable — this is a local Unity Editor project with no OS-level task registration, no env vars, no installed-package artifacts tied to these class names. | None. |

**Uncommitted-work interaction risk (flag for planning, not a blocker):** `git status` at the start of this research shows the Phase 15/v3.1 boss-FSM test-room work (`BossEnemy.prefab`, `Room_BossFsmTest.prefab`, `BossEnemyPrefabBuilder.cs`, `RoomBossFsmTestBuilder.cs`, `BossFsmTestPoolSwapTool.cs`) as **untracked, uncommitted** — this is the still-open Phase 15 Task 3 `checkpoint:human-action` from `15-05-PLAN.md`, per `STATE.md`'s "Parked Milestone" section. Phase 18's `BossEnemyBase`/`FioraBoss` extraction will touch/rename files that this uncommitted work already depends on (the prefab and the Editor tool above). Recommend the phase plan either (a) commit or explicitly acknowledge/carry forward this uncommitted state as part of Phase 18's own diff, or (b) verify with the user whether the Phase 15 test-room artifacts should be treated as already "part of the codebase" for Phase 18's extraction purposes (they are currently the *only* existing boss prefab, so Phase 18 has no choice but to build on them) — this is not a research gap, just a sequencing fact the planner needs to state explicitly rather than silently assume.

## Common Pitfalls

*(Filtered from `.planning/research/PITFALLS.md` to the 3 pitfalls actually in-scope for Phase 18 — Pitfalls 4 and 5 concern WorldGenerator boss-room-cleanup-exemption and touch input respectively, both explicitly out of scope for this phase per CONTEXT.md D-02 and the phase boundary.)*

### Pitfall 1: New timers silently skip the `unscaledDeltaTime`/`WaitForSecondsRealtime` convention
**What goes wrong:** Any new timer introduced in `OverclockModule` (during the verbatim move) or `BossEnemyBase` that uses `Time.deltaTime`/`WaitForSeconds` instead of the real-time equivalents will appear to work in normal play but freeze/crawl during the player's own Overclock slow-mo (`Time.timeScale = 0.2`) or freeze completely during `HitFreeze` (`Time.timeScale = 0f`).
**Why it happens:** Copy-paste from generic patterns, or introducing "new glue code" during the extraction (e.g., a lifecycle hook in `BossEnemyBase`) that wasn't in the original file and wasn't written against the convention from day one.
**How to avoid:** Since this phase's `OverclockModule` migration is explicitly verbatim (D-04: "verbatim move, not a rewrite"), the risk is low for that piece — the existing `CombatController.cs` already uses `WaitForSecondsRealtime`/`Time.unscaledDeltaTime` throughout (confirmed: `ExecuteWhiff` line 309, `HitFreeze` line 318, `Update()` line 118). The higher-risk surface is any **new** code written for `BossEnemyBase`'s extracted lifecycle glue (if any coroutine/timer logic is newly introduced during extraction rather than moved verbatim from `BossEnemy.cs`, which already correctly uses `Time.unscaledDeltaTime` throughout its `PatternLoop`/`HitReactionAndReset`).
**Warning signs:** Any `Time.deltaTime` or `WaitForSeconds(` (missing `Realtime`) appearing in a diff for this phase's new/modified files.
**Verification:** grep the new/modified files (`OverclockModule.cs`, `BossEnemyBase.cs`, `FioraBoss.cs`, `BossUnlockManager.cs`) for `Time.deltaTime` and `WaitForSeconds(` — expect zero matches.

### Pitfall 2: Treating this extraction as "just a copy" instead of the single de-risking opportunity before 4 more bosses arrive
**What goes wrong:** `BossEnemy.cs` is currently the *only* boss in the codebase — this is explicitly the last moment before DeadEye/SAMURAI/MAX/NOVA (Phase 19-23) arrive that a clean, non-divergent extraction is possible. If `BossEnemyBase` is extracted sloppily now (e.g., over-including boss-specific fields, or under-including something that turns out to be universal once the 2nd boss is built), the cost of fixing it *after* 4 more bosses exist is far higher (PITFALLS.md: "MEDIUM — retroactive extraction... larger diff than doing it upfront").
**Why it happens:** Time pressure to "just get boss #1 refactored" without stepping back to ask which of `BossEnemy.cs`'s members are truly boss-universal vs. F.I.O.R.A-pattern-specific — see Pattern 2 above for the explicit split this research already worked out.
**How to avoid:** Use the explicit universal/subclass-specific split enumerated in Architecture Patterns → Pattern 2 as the Definition-of-Done checklist for the extraction task, rather than re-deriving it from scratch during implementation.
**Verification:** After extraction, `FioraBoss.cs` should contain only: the `BossState` enum, `PatternLoop()`, `HitReactionAndReset()`, the 7-hit counter, tunables specific to F.I.O.R.A's telegraph/attack/vulnerable timings, and `OnTriggerEnter2D`. Everything else should have moved to `BossEnemyBase.cs`.

### Pitfall 6: Persistence reset-boundary must be structurally isolated, not just documented
**What goes wrong:** `DeathScreenController.RestartGame()` currently resets `FloorManager.CurrentFloor` and `ScoreManager.Score` unconditionally, by design. A future edit to this method (for an unrelated reason) could "helpfully" add a call to reset unlock state too, since all three would live in visually-adjacent lines of the same method.
**Why it happens:** This codebase's only existing precedent for "state that survives scene reload" (`FloorManager`/`ScoreManager`) is a deliberate *anti*-pattern for this exact need — both are explicitly reset on every restart. `BossUnlockManager` needs the opposite behavior with no existing convention to extend.
**How to avoid:** Keep `BossUnlockManager` in its own file with **no reset method exposed at all** (not even a private one) — this makes the wrong edit structurally harder, not just documented against, per ARCHITECTURE.md's Anti-Pattern 3.
**Verification:** After implementation, confirm `DeathScreenController.RestartGame()`'s diff contains zero references to `BossUnlockManager`, and confirm (manual playtest) that a boss-unlock flag set via `BossUnlockManager.Unlock(...)` survives a full death→restart cycle within the same Play session AND survives closing/reopening the Unity Editor Play mode (simulating app relaunch) — `PlayerPrefs` persists to the registry (Windows)/plist (Mac) across Editor Play sessions and real builds alike, so this is testable directly in-Editor without needing a built player.

## Code Examples

### Existing convention: data-only static class (to mirror for `BossUnlockManager`)
```csharp
// Source: Assets/Scripts/World/FloorManager.cs (existing, unmodified)
public static class FloorManager
{
    public static int CurrentFloor = 1;
}
```
```csharp
// Source: Assets/Scripts/World/ScoreManager.cs (existing, unmodified) — richer example of the same convention
public static class ScoreManager
{
    public static int Score { get; private set; }
    public static void Reset() { Score = 0; _roomStartTime = Time.unscaledTime; }
    // ... AddKillScore/AddBossKillScore/etc., all pure data mutation, no MonoBehaviour lifecycle
}
```

### Existing convention: reset-sweep boundary to NOT touch (verified exact current content)
```csharp
// Source: Assets/Scripts/UI/DeathScreenController.cs:29-36 (existing, unmodified)
private void RestartGame()
{
    Time.timeScale         = 1f;
    Time.fixedDeltaTime    = 0.02f;
    FloorManager.CurrentFloor = 1;
    ScoreManager.Reset();
    SceneManager.LoadScene("AttackSelect");
    // BossUnlockManager must NEVER be referenced in this method.
}
```

### Existing convention: defeat-guard invariant (must be preserved verbatim in `BossEnemyBase`)
```csharp
// Source: Assets/Scripts/Enemy/BossEnemy.cs:195-208 (existing — the exact pattern to pull into BossEnemyBase)
public void OnDashHit()
{
    if (_isDefeated) return; // guard ONLY on _isDefeated — never on IsAlive (IsAlive means "vulnerable", not "alive")
    if (_patternCoroutine != null) { StopCoroutine(_patternCoroutine); _patternCoroutine = null; }
    _hitCount++;
    if (_hitCount >= RequiredHits) // >= comparison, post-increment — exact hit death
    {
        _isDefeated = true;
        IsAlive = false;
        Die();
        return;
    }
    _patternCoroutine = StartCoroutine(HitReactionAndReset());
}
```
Note `_hitCount`/`RequiredHits` stay in `FioraBoss` (boss-specific defeat condition) — only the `_isDefeated` guard + `Die()` call convention generalizes.

## State of the Art

Not applicable in the ecosystem-version sense (no library/framework version drift to track) — this is a pure internal-architecture question against a fixed Unity 6000.3.11f1 project. The only "before/after" here is the codebase's own evolution:

| Old (current) | New (this phase) | Why Changed |
|--------------|------------------|--------------|
| `CombatController` hardcodes Overclock logic inline (`ExecuteDash`/`ExecuteWhiff`/`FindNearestEnemyInRange` as private methods) | `CombatController` hosts a swappable `_activeModule : IPlayerCombatModule`, with `OverclockModule` as the first (only, for this phase) concrete implementation | Phase 19-23 need to add DeadEye/SAMURAI/MAX/NOVA modules without further modifying `CombatController`'s core loop |
| `BossEnemy.cs` is the only boss, standalone `MonoBehaviour : IEnemy, ISpawnGatable` | `BossEnemyBase` (shared plumbing) + `FioraBoss : BossEnemyBase` (F.I.O.R.A-specific pattern loop) | Phase 19-23 need a shared base for DeadEye/SAMURAI/MAX/NOVA without 4x copy-paste drift (PITFALLS.md Pitfall 2) |
| No persistence anywhere in the project (`PlayerPrefs` used zero times) | `BossUnlockManager` — first-ever disk-persisted state | UNLOCK-01 requires unlock flags to survive app restart, which no existing static-class pattern in this project does (`FloorManager`/`ScoreManager` are explicitly session-scoped and reset-on-death) |

## Open Questions

1. **`BossEnemyBase`'s exact hook shape for `PatternLoop()`/`OnPlayerDiedCleanup()`/highlight-base-color**
   - What we know: the *content* split (what stays universal vs. subclass-specific) is well-established from direct source analysis (Pattern 2 above).
   - What's unclear: the exact C# mechanism (abstract method vs. virtual with default vs. protected field + template method) for wiring `SetSpawnGate()`'s "start pattern loop if not already running" logic into a subclass-owned coroutine, since `BossEnemy.cs` currently has this logic inline referencing its own concrete `PatternLoop()`.
   - Recommendation: this is a normal implementation-time design choice, not a research gap — the planner/implementer should pick whichever C# pattern (most likely `protected abstract IEnumerator PatternLoop();` called by a shared `protected void StartPatternIfNeeded()` helper) keeps `FioraBoss` closest to its current structure, since D-04's "verbatim move" spirit (low regression risk) applies here too even though INFRA-03 isn't explicitly a "zero regression" requirement the way INFRA-01 is.

2. **Whether `BossUnlockManager.Unlock()` is called from `BossEnemyBase.Die()` or left as a `FioraBoss`-only call for this phase**
   - What we know: Phase 18 has no new boss to actually unlock via real gameplay (FioraBoss's own module — Overclock — is not "unlocked" by defeating anything; F.I.O.R.A herself is the boss whose defeat *would* be the first thing to grant an unlock, but which unlock exactly is TBD since SAMURAI's parry module, not Overclock, is what SAMURAI-01 says gets unlocked first).
   - What's unclear: does defeating `FioraBoss` in this phase's manual playtest need to actually call `BossUnlockManager.Unlock("Fiora")`, or is that wiring premature since there's no unlock-consuming UI yet (that's UNLOCK-02/03 in Phase 19)?
   - Recommendation: wire the call anyway (it's cheap, and the manual playtest checklist for INFRA-03/UNLOCK-01 should verify the full loop: defeat FioraBoss in the test room → confirm `PlayerPrefs` key is set → restart app → confirm still set) — leaving it unwired would mean UNLOCK-01's success criterion ("보스를 격파하면 PlayerPrefs에 해금 플래그가 기록되고...") can't actually be verified end-to-end within Phase 18.

3. **Does the manual playtest checklist (D-04) need a written, versioned document, or is it verified ad hoc?**
   - What we know: D-04 locks manual playtest as the verification method, and CONTEXT.md's discretion list includes "수동 플레이테스트 체크리스트의 구체적 항목 구성" (the specific checklist composition) as Claude's discretion.
   - What's unclear: format/location — inline in the plan's verification steps, or a separate checklist artifact.
   - Recommendation: inline in the PLAN.md's per-task "how to verify" steps is sufficient given this project's existing convention (every prior phase's playtest checklists live inside CONTEXT.md/PLAN.md, not as separate files) — no new artifact type needed.

## Validation Architecture

**Note on scope conflict:** `.planning/config.json` has `workflow.nyquist_validation: true` (present, not `false`), which per the researcher protocol means this section is normally required. However, **CONTEXT.md D-04 is an explicit, locked user decision that overrides the default**: automated PlayMode test infrastructure (the long-incomplete `02-04-PLAN.md` CombatTests/RollTests) is explicitly deferred out of this phase's scope, and INFRA-01's regression verification is manual-playtest-only by user decision. Per the researcher's own authority model, locked CONTEXT.md decisions take precedence over default workflow settings — this section documents the **actual** (manual) validation approach the planner must use, not a hypothetical automated one.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None installed/wired for this phase — `com.unity.test-framework` 1.6.0 is present in `Packages/manifest.json` but **no test assembly, test folder, or test file exists anywhere in `Assets/`** (confirmed: `find Assets -iname "*test*"` returns only the unrelated `Room_BossFsmTest`/`BossFsmTestPoolSwapTool` Editor tooling from Phase 15, not NUnit tests) |
| Config file | none — see Wave 0 note below |
| Quick run command | N/A — manual playtest only, per D-04 |
| Full suite command | N/A — manual playtest only, per D-04 |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFRA-01 | Overclock hold=slowmo+range, release=dash-kill behaves identically after migration | manual-only (D-04 locked) | N/A — direct Play-mode comparison against pre-migration behavior (hold Attack, verify slow-mo+range display; release near an enemy, verify dash-kill; release with no target, verify whiff+lockout; hold through gauge-empty, verify auto-exit-slowmo-but-still-dashable) | N/A — no automated test, by design |
| INFRA-03 | `BossEnemyBase`-derived class usable without rewriting defeat-guard/death-sequence/spawn-gate/highlight | manual-only | N/A — defeat `FioraBoss` in the existing `Room_BossFsmTest` isolated test room (Phase 15 precedent tooling) via 7 hits, confirm: pattern resets correctly on hits 1-6, death sequence/score bonus/camera shake fire on hit 7, spawn-gate still blocks targeting during spawn VFX | N/A — no automated test, by design |
| UNLOCK-01 | Boss defeat writes PlayerPrefs flag, survives app restart | manual-only | N/A — defeat `FioraBoss`, confirm unlock flag set (e.g., via a temporary `Debug.Log(BossUnlockManager.IsUnlocked("Fiora"))` or Editor `PlayerPrefs` inspection), fully stop and restart Play mode (or the built player), confirm flag still reads true | N/A — no automated test, by design |

### Sampling Rate
- **Per task commit:** Manual Play-mode check of the specific behavior just touched (per D-04).
- **Per wave merge:** Full manual playtest pass covering all three requirements' checklists above.
- **Phase gate:** All three manual checklists pass before `/gsd:verify-work` — no automated full-suite gate exists for this phase, by explicit user decision.

### Wave 0 Gaps
None — Wave 0 test-infrastructure setup is explicitly not applicable, since D-04 locks manual verification only and no test framework wiring is in scope for this phase. If a future phase (per the deferred idea "자동화 PlayMode 회귀 테스트") revisits automated testing, it would need to start from Wave 0 scratch at that time (`Packages/manifest.json` already has `com.unity.test-framework`, but no `Tests/` assembly definition exists yet anywhere in the project).

## Sources

### Primary (HIGH confidence — direct source reads this session)
- `Assets/Scripts/Player/CombatController.cs` (full read, 435 lines) — Overclock lifecycle, targeting, dash/whiff resolution, exact line numbers cited above
- `Assets/Scripts/Enemy/BossEnemy.cs` (full read, 302 lines) — current single-boss FSM, `_isDefeated` guard, `Die()` sequence, `ClearHighlight()` override
- `Assets/Scripts/Enemy/EnemyBase.cs` (full read, 94 lines) — "minimal extraction" precedent to mirror
- `Assets/Scripts/Enemy/IEnemy.cs`, `ISpawnGatable.cs` (full reads) — confirmed closed 3-member contract
- `Assets/Scripts/UI/DeathScreenController.cs` (full read, 38 lines) — confirmed exact reset-sweep boundary
- `Assets/Scripts/World/ScoreManager.cs`, `FloorManager.cs` (full reads) — data-only static class convention
- `Assets/Scripts/Player/InputManager.cs`, `ChronoGaugeController.cs` (full reads) — input polling surface and gauge convention, confirms what stays host-side
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` (full read) — confirms `Die()` sequence's downstream dependency, unaffected by this phase
- `Assets/Editor/BossEnemyPrefabBuilder.cs` (full read) — surfaced the hardcoded `AddComponent<BossEnemy>()` type reference that must be updated alongside the rename
- `Assets/Editor/RoomBossFsmTestBuilder.cs`, `BossFsmTestPoolSwapTool.cs` (grepped for `BossEnemy` references) — confirmed these reference only the prefab *path*, not the C# type, so need no change
- `Assets/Prefabs/Enemies/BossEnemy.prefab`, `BossEnemy.cs.meta` (grepped) — confirmed the exact GUID (`cb839023c498e514cab6bb76ab11cde9`) that must be preserved through the file rename
- `Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab`, `Assets/Scenes/SampleScene.unity` (grepped for the same GUID) — confirmed neither currently references the boss prefab's script GUID (wiring still pending per Phase 15 Task 3 checkpoint)
- `.planning/phases/18-shared-infra/18-CONTEXT.md`, `18-DISCUSSION-LOG.md` — user decisions and discretion areas
- `.planning/REQUIREMENTS.md`, `.planning/STATE.md` — requirement text, project history, confirmed Phase 15/16 precedent and parked-milestone status
- `.planning/config.json` — confirmed `nyquist_validation: true` and the D-04 override rationale documented above

### Secondary (this session's synthesis of prior same-day research, already HIGH confidence/codebase-derived)
- `.planning/research/ARCHITECTURE.md` (full read) — Questions 1/2/3 architecture recommendations, already verified against current source in this session and found to still match exactly
- `.planning/research/PITFALLS.md` (full read) — Pitfalls 1, 2, 6 filtered as in-scope for this phase; Pitfalls 3, 4, 5 confirmed out of scope (module-swap safety deferred to RUSH-01, WorldGenerator boss-room exemption is a future phase's job, touch input descoped by D-02)

### Tertiary (LOW confidence)
None — no WebSearch/Context7/external sources were used or needed; this is a pure internal-codebase architecture question.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages, Unity built-in APIs only, directly verified
- Architecture: HIGH — extraction boundaries directly derived from reading the actual current source files, cross-checked against already-codebase-derived ARCHITECTURE.md
- Pitfalls: HIGH — every pitfall traces to a specific line/pattern in the current source, not generic Unity advice
- Runtime State Inventory (GUID preservation): HIGH — verified by direct grep of the actual `.meta` GUID and its only current reference site

**Research date:** 2026-07-20
**Valid until:** Stable — this is an internal-codebase question with no external version drift risk; valid until the source files themselves change (i.e., effectively until this phase is implemented, since no other phase touches these files concurrently)
