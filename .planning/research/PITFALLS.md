# Pitfalls Research: v4.0 보스 캐릭터 확장 & 게임 모드

**Domain:** Adding 4 mechanically-distinct boss modules (DeadEye/SAMURAI/MAX/NOVA) + boss-unlock progression + 2 game modes to an existing single-mechanic Unity 2D mobile platformer prototype ("Fast")
**Researched:** 2026-07-20
**Confidence:** HIGH (grounded in direct read of current source: `BossEnemy.cs`, `EnemyBase.cs`, `CombatController.cs`, `WorldGenerator.cs`, `InputManager.cs`, `ScoreManager.cs`, `FloorManager.cs`, `DeathScreenController.cs`, `Assets/InputSystem_Actions.inputactions`, `.planning/phases/16-boss-room-lifecycle/16-CONTEXT.md`, `.planning/phases/15-fsm/15-06-PLAN.md`, `STORY.md`)

> Note: this file supersedes the previous (v3.1, 2026-07-08) boss-room/VFX/audio pitfalls research. That milestone's pitfalls (regular enemies leaking into the boss room's shared spawn pipeline, etc.) targeted a single boss and a different set of concerns. This file focuses exclusively on the **new** pitfalls introduced by v4.0: 4 additional mechanically-distinct bosses, module-swap progression, persistence across the death/restart loop, and touch-input requirements for those new mechanics. Every pitfall below is anchored to a specific file/pattern that already exists in this repo — not generic Unity advice.

## Critical Pitfalls

### Pitfall 1: New boss timers silently skip the `unscaledDeltaTime`/`WaitForSecondsRealtime` convention

**What goes wrong:**
DeadEye's 6-shot reload timer, SAMURAI's parry window, MAX's wall-stun duration, and NOVA's independent drone attack-cooldown are all "counts down while combat continues" timers — exactly the class of state this codebase has a hard-won, repeatedly-documented rule for. If any of the 4 new boss scripts (or the player-side parry/reload/drone-control counterparts) use `Time.deltaTime`, `WaitForSeconds`, or a `float elapsed; elapsed += Time.deltaTime` loop instead of the real-time equivalents, that timer will **appear to work in normal play** but freeze or crawl to near-zero speed during the player's own Overclock Mode slowmo (`Time.timeScale = 0.2`, `CombatController.cs`) and freeze completely during `HitFreeze` (`Time.timeScale = 0f`, `CombatController.cs:312-322`). Because slowmo is triggered by the *player's own attack button*, this bug is invisible until a specific interaction is tested (e.g., "hold Attack while DeadEye is mid-reload" or "roll during SAMURAI's parry window") — it will not show up in a boss fought in isolation without ever pressing Attack.

**Why it happens:**
Every existing boss/player timer in the codebase (`BossEnemy.cs` telegraph/vulnerable/hit-reaction loops, `InvincibilityHandler.cs`, `RollController.cs`, `ChronoGaugeController.cs`, `FloorTimer`) was written against this rule from day one because it was baked into Phase 1 (`PROJECT.md` Key Decisions: "Time.timeScale 보정을 Phase 1에 선반영"). A developer writing 4 *new* boss scripts under time pressure, copy-pasting from generic Unity tutorials or from patterns that predate the boss FSM, can easily reintroduce `Time.deltaTime` because it "just works" in isolated testing (no Attack button pressed during that specific playtest pass).

**How to avoid:**
- Add a one-line Definition-of-Done checklist item to every new-boss task: "grep the new file for `Time.deltaTime` and `WaitForSeconds(` (not `WaitForSecondsRealtime`) — must be zero matches outside intentionally-scaled visual tweens."
- Where a boss timer must visually "feel" affected by slowmo (e.g., a projectile that should look natural inside slowmo), make that an explicit, commented exception — don't let it be silent.
- Reuse `BossEnemy.cs`'s existing pattern literally (its Telegraph/Attack/Vulnerable loops already use `Time.unscaledDeltaTime` and `WaitForSecondsRealtime`) as the copy-paste template for the new bosses' pattern loops, rather than starting from scratch.

**Warning signs:**
- Any `while (elapsed < duration) { elapsed += Time.deltaTime; ... }` in a new boss script.
- Any `yield return new WaitForSeconds(x)` (missing `Realtime`) in boss/parry/reload code.
- QA report of the form "the boss's [attack/reload/stun] seems to freeze if I hold the attack button."

**Phase to address:**
The phase(s) implementing each new boss's pattern FSM (DeadEye/SAMURAI/MAX/NOVA), verified by an explicit playtest step "trigger boss pattern while holding Attack / during HitFreeze" in each boss's success criteria — do not defer this check to a later polish phase.

---

### Pitfall 2: 4x copy-paste of the non-inherited `BossEnemy.cs` FSM instead of extracting a shared boss base

**What goes wrong:**
`BossEnemy.cs` (Phase 15) is a **standalone** `MonoBehaviour, IEnemy, ISpawnGatable` implementation that does **not** inherit `EnemyBase` — this was already flagged as a known gap in Phase 16's own context ("Boss Enemy(Phase 15, 아직 미구현)도 이 EnemyBase를 상속하도록 향후 설계하면 3번째 복붙을 방지할 수 있음 — 단 이번 Phase 범위는 아님", `16-CONTEXT.md` D-05). It currently duplicates, rather than shares, `EnemyBase`'s: `OnPlayerDeath` subscribe/unsubscribe, `SetSpawnGate`, death-sequence boilerplate (rb static, colliders disabled, animator `isDead`, `EnemyDeathEffect` trigger), and the "IsAlive overloaded as vulnerability, not death" pattern documented at length in its own header comment. Building DeadEye/SAMURAI/MAX/NOVA by copy-pasting `BossEnemy.cs` four more times creates **5 independent copies** of this non-trivial FSM plumbing. Any bugfix found in one (e.g., a death-sequence ordering bug, or an `_isDefeated` race condition) now needs to be manually propagated to 4 other files, and inevitably drifts — one boss gets the fix, three don't.

**Why it happens:**
The fastest path to "make a second boss" is to duplicate the working `BossEnemy.cs` and rename fields — it compiles immediately and behaves correctly on day one. The cost only appears later, when a shared bug is found or a shared behavior (e.g., "all bosses should exempt WorldGenerator cleanup during combat," Pitfall 4 below) needs retrofitting into 5 places instead of 1.

**How to avoid:**
- Before writing the first of the 4 new bosses, do a small, scoped extraction: pull `BossEnemy.cs`'s IsAlive/vulnerability-window pattern, `_isDefeated` guard, `OnPlayerDied` cleanup, and death-sequence trigger into a shared boss base (parallel to the existing minimal `EnemyBase`, or ideally by finally making `BossEnemy` inherit `EnemyBase` per Phase 16's own deferred note) — do this **once**, before the 4x fan-out, not as a retrofit after 4 divergent copies exist.
- Each of the 4 new bosses' *pattern* content (telegraph shape, hit/defeat condition, unique per-boss mechanic) stays boss-specific; only the FSM plumbing/death/spawn-gate boilerplate gets shared.
- Do not attempt the "full inheritance refactor" of `MeleeEnemy`/`RangedEnemy` alongside this — Phase 16 already explicitly scoped that as "minimal extraction only, not full inheritance" per user instruction; keep the same discipline for bosses.
- Note also: `BossEnemy.cs`'s current defeat condition is a flat `RequiredHits = 7` counter — this does not generalize to SAMURAI's parry-punish, MAX's wall-stun window, or DeadEye's reload-vulnerability window, all of which are fundamentally different "when is this boss killable" conditions, not just a different hit count. The shared base should abstract "defeated" as a boss-owned decision, not assume every boss shares F.I.O.R.A/BOSS-04's N-hits-then-die shape.

**Warning signs:**
- A second boss script appears with `_isDefeated`, `IsAlive`-as-vulnerability, and the exact same `OnPlayerDied`/death-sequence block copy-pasted with only cosmetic renames.
- A bugfix commit touches only one boss file when the same bug logically applies to all bosses.

**Phase to address:**
A dedicated "shared boss infrastructure" phase (or an explicit Task 0 inside the first new-boss phase) that must land **before** the second, third, and fourth boss are implemented — sequencing this after even one additional boss copy exists means the extraction has to reconcile divergence instead of starting clean.

---

### Pitfall 3: Module-swap (mid-combat, Boss Rush) leaks state because `CombatController` has no module abstraction at all

**What goes wrong:**
`CombatController.cs` is not "F.I.O.R.A's module implementation with room for 4 more" — it **is** the entire combat system, hardcoded. `AttackTypeSelector.Selected` (Linear/Fan) is the *only* existing "variant" axis, and it only toggles a search-shape branch inside the one dash-to-target mechanic; it says nothing about how to represent DeadEye's ammo/reload resource, SAMURAI's parry-input-detection, MAX's momentum-is-the-attack, or NOVA's second controllable unit. If the 5 modules are bolted on as more `if (currentModule == X)` branches inside the same `Update()`/`ExecuteDash()` methods (the same shape as the existing `AttackTypeSelector.Selected == AttackType.Linear` checks), mid-combat swapping in 보스 러시 mode will leak state across the switch: `_isSlowMo`/`Time.timeScale` left engaged from the outgoing module, `ChronoGaugeController.Value` (F.I.O.R.A-specific resource) still draining/regenerating while DeadEye's ammo-count is active, `_isBusy` stuck true if a dash coroutine was mid-flight when the swap happened, a leftover `_lastHighlighted` enemy never cleared, or (for NOVA) an orphaned drone GameObject left alive after swapping away from NOVA's module.
`ForceExitCombatState()` already exists and is the correct *pattern* for this (it's called by `WorldGenerator` before floor transitions specifically to prevent this class of leak) — but it only resets F.I.O.R.A's own state (`ExitSlowMotion()` + `ExitAttackPending()`). It knows nothing about DeadEye/SAMURAI/MAX/NOVA state and won't be extended automatically just by adding new modules elsewhere.

**Why it happens:**
The single-module, single-scene-lifetime assumption is baked into every layer: `AttackSelectController` sets the choice once at scene load and never again; `CombatController` is a single `MonoBehaviour` with no concept of "current module" as swappable data; `ChronoGaugeController`, `InvincibilityHandler`, `RangeDisplay` are all `[RequireComponent]`-coupled to `CombatController` as if there's exactly one combat system. Adding modules by branching inside this monolith is the path of least resistance for each *individual* boss, but it's precisely what causes cross-module leaks the moment swapping (not just picking-once-before-Play) is required.

**How to avoid:**
- Introduce an explicit module abstraction (interface or per-module component) *before* implementing DeadEye/SAMURAI/MAX/NOVA's player-side counterpart mechanics — each module owns enter/exit lifecycle hooks analogous to `ForceExitCombatState()`, called by a swap-orchestrator whenever the active module changes (both at 한계 시험's pre-run selection and at 보스 러시's mid-combat swap).
- Module exit must be idempotent and defensive: stop own coroutines, zero own timeScale/gauge influence, destroy/deactivate own spawned objects (NOVA's drone), and clear own highlight/target state — mirroring exactly what `ForceExitCombatState()` already does for F.I.O.R.A, generalized.
- Reuse `AttackTypeSelector`'s proven "static selection + live zone-trigger swap" pattern as *precedent that live swapping is safe for a narrow axis* — but do not assume it validates swapping entire control schemes; the state surface being swapped is far larger (gauge, dash, invincibility, animator bools, spawned sub-objects) than Linear/Fan ever was.
- 한계 시험 mode (single module, chosen pre-run, roguelike floors) can ship with the *simpler* half of this abstraction (enter-once, no swap) — but design the interface so 보스 러시's mid-combat swap doesn't require a second incompatible implementation later.

**Warning signs:**
- Slowmo still active (or `Time.timeScale` stuck non-1) after switching modules mid-fight.
- Gauge/resource UI showing the wrong module's meter after a swap.
- NOVA's drone still visible/active in the scene after swapping to a different module.
- `_isBusy`-style lockouts left permanently true, freezing all future input after a swap performed during a dash/whiff coroutine.

**Phase to address:**
A "module abstraction" phase must precede (or be Task 0 of) the first boss whose mechanic is fundamentally incompatible with the current F.I.O.R.A-shaped `CombatController` (likely DeadEye or SAMURAI, whichever is built first) — retrofitting the abstraction after 2+ modules are hardcoded as branches is significantly more expensive than designing it up front. The mid-combat-swap-specific leak testing belongs in the 보스 러시 game-mode phase, using every module pairing (not just one), since leaks are typically module-pair-specific (e.g., NOVA-drone-orphan only reproduces when swapping *away from* NOVA, not *into* it).

---

### Pitfall 4: The "boss room is already exempted from WorldGenerator cleanup" premise is false — it does not exist in code yet

**What goes wrong:**
Direct inspection of `WorldGenerator.cs` confirms it has **zero boss-awareness**. `CleanupSection()` (called by both `RemoveTail()` and `RemoveHead()`) unconditionally destroys any room/corridor that falls outside the `_lookaheadCount`/`_lookbehindCount` window around the player's chain position — there is no check for "is a boss fight currently active in this room." Phase 16's own context file lists "전투 판정 & 생명주기 게이팅 세부조건... BOSS-10 예외의 정확한 트리거 조건" explicitly under `<deferred>` as **"미논의, 다음 세션 필수"** (undiscussed, mandatory to discuss next session) — it was never implemented, only proposed as a requirement ID (BOSS-10) in `REQUIREMENTS.md`/`ROADMAP.md`. If v4.0 assumes this exemption already exists and simply adds 4 more boss rooms on top, every one of them inherits the *actual* current behavior: a boss room can be `Destroy()`-ed mid-fight the moment the player's chain index moves far enough away, which is entirely plausible for MAX (constant forward momentum can push the player past the boss room's trailing edge while the fight is still in progress) or for any boss fight that runs long relative to normal room traversal pacing.

**Why it happens:**
A requirement ID existing in `REQUIREMENTS.md`/`ROADMAP.md` (BOSS-10) is not the same as implemented code — this is an easy false-positive when working from planning docs instead of source, and it's exactly what happened here: BOSS-10 was proposed and named, then explicitly deferred without implementation when the Phase 16 session redirected to a pure refactoring track.

**How to avoid:**
- Treat "boss-room-exempt-from-cleanup" as **net-new work for v4.0**, not a solved precondition. This must be designed before or alongside the first new boss room, not assumed.
- The Phase 16 deferred gray areas are the right starting menu of design questions to resolve first: spawn architecture (chain-slot replacement vs. branch portal — this decision determines whether "exemption" even means "skip cleanup of a chain node" or something structurally different), and the precise trigger condition for "combat active" (boss `IsAlive`/`_isDefeated` state is already a reasonable signal to gate on, per `BossEnemy.cs`'s existing fields).
- Because 5 bosses now exist (not 1), the exemption logic must be boss-type-agnostic (query via `IEnemy`/whatever shared boss base emerges from Pitfall 2, not `BossEnemy`-the-single-class specifically) — do not hardcode this check against one concrete type only to redo it when the 2nd-5th bosses arrive.
- Do not confuse the currently-swapped-in `Room_BossFsmTest` scene-data hack (single-element room pool, `_lookaheadCount`/`_lookbehindCount` forced to 0, per `15-06-PLAN.md`) with a real fix — that hack exists purely to isolate one boss's FSM for testing and deliberately disables the chain-generation machinery entirely, so it cannot exercise (or validate a fix for) this cleanup-exception problem at all.

**Warning signs:**
- A boss fight ends with a `MissingReferenceException`/null-ref referencing a destroyed boss GameObject, or the boss visually vanishes mid-fight without a death sequence.
- Player defeats a boss but score/unlock doesn't register because the boss GameObject (and its `Die()` call) was already destroyed by `CleanupSection()` before the killing hit landed.
- QA report of "boss room disappeared while I was still fighting," specifically when a boss fight runs unusually long or the player is pushed/dashes far during the fight (MAX is the highest-risk case here given its stated "can't stop moving" design).

**Phase to address:**
Must be resolved as its own explicit design+implementation step (this is literally the still-open Phase 16 deferred item) before or in parallel with the first new boss room being wired into the real chain-based spawn flow — not bundled silently into an individual boss's implementation phase where it's likely to be overlooked again.

---

### Pitfall 5: Mouse-only aim direction + zero touch bindings for movement — the existing input layer cannot run on the target platform, and new boss mechanics will inherit that gap

**What goes wrong:**
Two separate, compounding issues, both confirmed directly in code:
1. `CombatController.GetMouseWorldDirection()` calls `UnityEngine.InputSystem.Mouse.current` unconditionally to compute attack-aim direction (used for both Linear/Fan target-shape filtering). On an Android device with no mouse, `Mouse.current` is `null`, and the fallback path (`_mainCamera.WorldToScreenPoint(origin)`) collapses aim direction to a degenerate vector pointing at the player's own screen position — meaning **aim-direction-dependent targeting is already non-functional on touch**, independent of anything new in v4.0.
2. `Assets/InputSystem_Actions.inputactions` has **no touch/on-screen-control binding for the `Move` action at all** (only `<Gamepad>/leftStick`, keyboard WASD/arrows, `<Joystick>/stick`, XR — confirmed by direct inspection of the action asset). There is also no on-screen joystick/button UI implementation anywhere in `Assets/` (no `OnScreenStick`, no virtual-joystick `Canvas`, etc. — none found). The `Attack` action does have `<Touchscreen>/primaryTouch/tap` bound, but that only covers "tap to trigger the button," not the aim-direction problem in #1.

Given this, any new mechanic that depends on directional aiming (DeadEye's reticle-at-boss, or reading a screen position for a parry-timing tap) inherits the same broken assumption, and the source design doc's PC-specific bindings (left-click hold / right-click fire / arrow-keys for a second unit — NOVA's drone) have **no existing analog to port from** on this input layer; they'd need to be designed from scratch for touch, not "ported."

**Why it happens:**
The prototype was clearly developed and playtested in the Unity Editor with mouse+keyboard, and Android-readiness for the *input layer specifically* was never validated end-to-end, even though `ProjectSettings` already targets Android (minSdk 25, ARM64) and the project constraints call out Android as the primary platform. This is a pre-existing gap that predates v4.0, but v4.0 is the first milestone whose new mechanics (aim reticles, timing-based parries, a second controllable unit) cannot function at all without solving it first — the existing single mechanic (auto-target-nearest-in-range) is largely aim-direction-independent in practice (`FindNearestEnemyInRange` already auto-selects rather than requiring precise aim), which is likely why this gap has gone unnoticed so far.

**How to avoid:**
- Do not treat "port PC bindings to touch" as a per-boss task — solve the underlying input abstraction once: replace `Mouse.current`-based aim direction with a touch/pointer-agnostic source (Unity Input System's `<Pointer>` covers mouse+touch uniformly, or explicit `Touchscreen.current` handling with a designed on-screen aim mechanism, e.g., drag-to-aim or auto-aim-toward-nearest as the primary touch paradigm instead of a reticle).
- For NOVA's dual-unit control (body + drone) specifically: do not attempt a literal "arrow keys for drone" port — this needs a touch-native redesign (e.g., auto-piloted drone with a tap-to-target toggle, or a second on-screen virtual stick) decided explicitly, not inherited from the PC design doc by default.
- Add a basic on-screen touch control layer (movement + attack-hold + roll, at minimum) as its own early infrastructure task if the project intends to actually validate on Android during this milestone — this is foundational and blocks realistic playtesting of every new mechanic, not just the boss-specific ones.
- If Android validation is explicitly out-of-scope for v4.0 and Editor mouse/keyboard testing is accepted as sufficient for this milestone, that must be an explicit, written scope decision (not a silent assumption) — because the project constraints state Android as the primary target.

**Warning signs:**
- Any new boss playtest note along the lines of "works fine in Editor" without ever running on an actual Android build/device.
- A new mechanic's design spec references "click," "right-click," or "arrow keys" without a corresponding touch-equivalent decision recorded anywhere.
- `Mouse.current` (or any other `Mouse`/`Keyboard`-specific API) appearing in new boss-adjacent player scripts.

**Phase to address:**
Should be resolved (or explicitly descoped with a written decision) before or very early in the milestone — ideally as shared infrastructure alongside Pitfall 3's module abstraction, since aim-direction and on-screen controls are needed by essentially every new module. Deferring it risks discovering the gap only when the first Android build is actually tested, potentially very late.

---

### Pitfall 6: Persisting boss-unlock state is entirely new — no persistence infrastructure exists, and the "reset everything on death" convention is currently absolute

**What goes wrong:**
Direct inspection confirms there is **no persistence mechanism anywhere** in the codebase — no `PlayerPrefs`, no serialized save file, no third-party save library. The only "state that survives a scene reload" today is plain C# static fields (`FloorManager.CurrentFloor`, `ScoreManager.Score`), and both are **explicitly, unconditionally reset to their defaults** in `DeathScreenController.RestartGame()` (`FloorManager.CurrentFloor = 1; ScoreManager.Reset();`) before reloading the `AttackSelect` scene. In other words, the existing convention is not "some things survive death, some don't" — it is "literally nothing survives death, by explicit design." Boss-unlock progression is the **first ever feature** that needs to survive the death/restart loop, and there is no existing pattern to extend — this has to be designed from zero, and if it's bolted on carelessly (e.g., stored as another static field that nobody remembers to *exclude* from `RestartGame()`'s reset sweep, or the reverse — accidentally reset by a future refactor of that method), unlocked modules will either wrongly vanish on every death (breaking the core unlock-progression premise) or wrongly persist across an actual app relaunch/reinstall when the design only intended within-session persistence.
There's also a scope ambiguity baked into the design itself: `STORY.md`'s loop description ("F.A.S.T.가 쓰러지면 시뮬레이션은 재시작된다... 매 회차마다 데이터가 쌓인다") implies unlocks should survive the death loop indefinitely (i.e., real save-game persistence across app sessions, not just within one Play session), but nothing in the current codebase distinguishes "survives scene reload within a session" from "survives app process restart" — these require different mechanisms (a static field vs. `PlayerPrefs`/a save file) and the milestone doesn't yet specify which is required.

**Why it happens:**
The prototype has intentionally avoided all persistent-progression systems so far (`PROJECT.md`'s "Out of Scope" explicitly lists "복잡한 성장 시스템... 성장은 검증 후 추가" — growth systems were deferred until now), so there is no existing precedent, utility class, or convention to follow. This is a green-field subsystem being added to a codebase whose only related pattern (`FloorManager`/`ScoreManager` static classes) is a deliberate *anti*-pattern for this exact need (unconditional reset on death).

**How to avoid:**
- Make an explicit, written decision early: does "boss-unlock persists" mean (a) survives the death→AttackSelect→SampleScene loop within one continuous play session, (b) survives closing and reopening the app, or both? This determines whether a new static class (session-scoped, like `FloorManager`) or `PlayerPrefs`/file-backed storage (app-restart-scoped) is correct — do not default to whichever is easiest to code without confirming which the design requires.
- Whichever mechanism is chosen, the "reset on death" call site (`DeathScreenController.RestartGame()`) must be explicitly audited and kept in sync: it currently resets `FloorManager` + `ScoreManager` unconditionally, and the new unlock-state class must be deliberately *excluded* from that reset (or given its own explicit non-reset path) — a future contributor touching `RestartGame()` for something else could easily "helpfully" add a full reset of all statics, silently breaking unlock persistence.
- Scope exactly what unlock state must NOT reset on death (unlocked module list) vs. what still must reset (floor progress, score, any per-run resource) — write this down as an explicit list, since this codebase has never before had to distinguish these two categories.
- If cross-app-session persistence is required, this is also the first time the project needs any serialization strategy at all — keep it minimal (a small `PlayerPrefs` int bitmask or JSON blob of unlocked boss IDs is more than sufficient for 4-5 booleans; do not over-engineer a generic save system for a prototype).

**Warning signs:**
- Unlocked modules disappearing after a death/restart in playtesting (persistence not wired, or wrongly caught by an existing reset call).
- Unlocked modules persisting across a full app close/reopen when only within-session persistence was intended (or vice versa) — surfaces as confusing playtest reports ("I unlocked SAMURAI but it's gone now" / "why do I still have everything after reinstalling").
- A future edit to `DeathScreenController.RestartGame()` (e.g., for an unrelated reset need) that iterates/resets "all game statics" generically and inadvertently wipes unlock state.

**Phase to address:**
Should be designed and implemented as its own small, early phase (before or alongside the first boss whose defeat is meant to grant an unlock) since every subsequent boss-defeat phase depends on it working correctly, and retrofitting persistence after several unlock call-sites already exist ad hoc is more error-prone than building the one shared API first (e.g., `BossUnlockManager.Unlock(BossId)` / `IsUnlocked(BossId)`) and having every boss's `Die()` call into it.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|-----------------|-----------------|
| Copy-paste `BossEnemy.cs` for the next boss instead of extracting shared base first | Fastest way to get boss #2 on screen | 4x maintenance surface, guaranteed drift on shared bugfixes (Pitfall 2) | Never — the extraction cost is small and known upfront |
| Add new module as `if (currentModule == X)` branches inside `CombatController` | No new files/abstraction needed for boss #1's player-side mechanic | Mid-combat swap leaks (Pitfall 3); branch count becomes unmanageable at 5 modules | Only acceptable for 한계 시험-only, never-swapped modules, and only if 보스 러시's swap requirement is confirmed descoped |
| Reuse the `_roomPrefabs`-swapped-to-1-element / `_lookaheadCount=0` scene hack (currently left in place per `15-06-PLAN.md` checkpoint) as "the" boss room testing method going forward | Already works, zero new tooling | Not a real spawn architecture — doesn't exercise lookahead/lookbehind/cleanup interactions at all, so it cannot catch Pitfall 4 | Acceptable only for isolated FSM-behavior testing of a single new boss's pattern loop; must be restored (`Fast/Phase15/Restore WorldGenerator Original Room Pool`) before any integration/lifecycle testing |
| Store boss-unlock state as a bare new static class field without designing app-restart vs. session-scope explicitly | Works immediately in Editor testing | Wrong persistence scope discovered only via confusing playtest reports (Pitfall 6) | Never for the initial implementation — acceptable only as a very short-lived spike explicitly labeled as such |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|-----------------|-------------------|
| New boss FSM ↔ `PlayerController.OnPlayerDeath` static event | Forgetting `OnDisable` unsubscribe (crashes/double-fires on next Play session since the event is `static` and survives domain-reload-disabled Editor settings) | Always mirror `BossEnemy.cs`'s existing `OnEnable`/`OnDisable` subscribe/unsubscribe pair exactly |
| New module ↔ `ChronoGaugeController`/`InvincibilityHandler` (`[RequireComponent]`-coupled to `CombatController`) | Assuming every module needs its own gauge/invincibility component, duplicating them per module | Decide per-module whether F.I.O.R.A's gauge concept applies at all (DeadEye's "6-shot then reload" is not a drain/regen gauge) — don't force-fit every module into the existing gauge shape |
| New boss room ↔ `WorldGenerator._chain` (`LinkedList`) | Assuming the boss room can be treated as "just another Complex_Room" for spawn/cleanup purposes | Resolve Pitfall 4's spawn-architecture decision (chain-slot vs. branch-portal) before wiring any new boss room into the real (non-test-hack) flow |
| Boss-unlock state ↔ `DeathScreenController.RestartGame()` | Letting the unlock-state reset call get silently added to (or omitted from) this method by an unrelated future change | Give unlock state its own explicitly-named, explicitly-audited reset boundary, not "whatever `RestartGame()` happens to touch" |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|-----------------|
| NOVA's drone as a second fully-simulated `Rigidbody2D` + its own `Physics2D.OverlapCircle` detection loop, times however many are active in 보스 러시 free-swap | Frame drops specifically in Boss Rush mode when NOVA-type patterns are active alongside other module overhead | Reuse existing pre-allocated buffer patterns (`CombatController._hitBuffer`, `EnemyBase._detectionBuffer`) for any new per-frame physics query — do not allocate new arrays per-frame | Noticeable on Android (ARM64, mobile GPU/CPU budget) well before it would show up in Editor testing on a dev PC |
| 4 new boss FSMs each independently polling `FindFirstObjectByType<PlayerController>()` in `Awake()` (as `BossEnemy.cs` already does) | Non-issue at 1 boss per room (current architecture guarantees solo boss fights), but worth confirming this assumption still holds for any new boss-room design that changes solo-fight guarantees | Keep the "one boss room = one active boss" invariant explicit in the new spawn architecture (Pitfall 4) rather than assuming it by convention only | Only relevant if a future spawn-architecture change (chain-slot vs branch-portal) accidentally allows multiple boss instances active simultaneously |

## Security Mistakes

Not applicable in the traditional sense — this is a local single-player prototype with no network layer, accounts, or server-authoritative state. The closest analog is client-side-only persistence integrity (Pitfall 6): since unlock state will live in `PlayerPrefs`/local storage with no server validation, there's no meaningful "cheating" risk worth engineering against at prototype stage — do not over-invest in tamper-proofing a save file that only the local player can ever read.

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-------------------|
| Porting DeadEye's "6 reticles then fire" pattern literally 1:1 from a mouse-precision design to touch without adjusting reticle size/telegraph timing | Reticles too small/fast to read or dodge on a phone screen, unlike on a monitor | Treat DeadEye's telegraph readability as a touch-specific tuning pass, not a direct value copy from the design doc |
| SAMURAI's parry window (precise-timing input) implemented with the same input-polling cadence as existing button-press detection, but touch input latency/jitter differs from mouse/keyboard | Parry feels "unfair" or inconsistent specifically on touch devices even if it feels fine in Editor testing with mouse | Playtest SAMURAI's parry timing window specifically on-device (Android), not just in Editor, before tuning the window width |
| Boss Rush's "swap module anytime mid-fight" without a clear on-screen affordance for touch (no cursor to hover a module icon) | Players may not discover or reliably execute mid-combat swapping on a touchscreen | Design the swap UI/gesture for touch first (e.g., dedicated on-screen module buttons), not adapted from a hypothetical PC hotkey scheme |

## "Looks Done But Isn't" Checklist

- [ ] **New boss pattern FSM:** Often missing a check against slowmo/HitFreeze interaction — verify by holding Attack (entering slowmo) while the boss is mid-pattern, and by landing a kill on a *different* enemy (triggering `HitFreeze`, `Time.timeScale=0`) while the new boss's timer is running.
- [ ] **Module swap (Boss Rush):** Often missing teardown of the *outgoing* module's spawned sub-objects/timeScale/gauge — verify by swapping away from each module mid-slowmo, mid-dash-coroutine, and (for NOVA) while its drone is alive.
- [ ] **New boss room:** Often missing verification that the room survives while the player's chain index would normally trigger `RemoveTail()`/`RemoveHead()` — verify by deliberately stalling combat (or triggering MAX's forward-momentum) long enough that a normal room would already have been cleaned up.
- [ ] **Boss-unlock persistence:** Often missing an explicit test of the *reset boundary* — verify unlocked modules survive a full death→restart cycle, and separately verify whether they are (or are not) expected to survive an app close/reopen, matching the explicit scope decision made for Pitfall 6.
- [ ] **Touch input for any new directional/aim mechanic:** Often only tested with Editor mouse — verify on an actual Android build/device, not just Play-mode-in-Editor with a mouse simulating touch.

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|----------------|------------------|
| Pitfall 1 (timeScale convention violated) | LOW | Mechanical find-replace of `Time.deltaTime`→`Time.unscaledDeltaTime` and `WaitForSeconds`→`WaitForSecondsRealtime` in the offending script, since the pattern is already well-established elsewhere in the codebase to copy from |
| Pitfall 2 (4x boss duplication drift) | MEDIUM | Retroactive extraction of a shared boss base after the fact — larger diff than doing it upfront, but mechanical (diff the 4-5 boss files to find the true common subset, same methodology already used for `EnemyBase`'s Phase 16 extraction) |
| Pitfall 3 (module-swap state leak) | MEDIUM-HIGH | Requires designing the missing abstraction after multiple modules already exist as branches — higher cost than doing it first, since existing branch-based code has to be refactored into the new module interface rather than written against it originally |
| Pitfall 4 (boss room destroyed mid-fight) | HIGH | Requires resolving the deferred Phase 16 spawn-architecture decision under pressure (post-bug-report) rather than as planned design work; may require reworking however many boss rooms were already wired into the un-exempted flow |
| Pitfall 5 (mouse-only input) | MEDIUM | Input abstraction swap (`Mouse.current`→`Pointer`/`Touchscreen`-aware) is localized to `CombatController.GetMouseWorldDirection()` plus whatever new aim code was added per-module — contained if caught before many modules copy the same mouse-only pattern |
| Pitfall 6 (persistence scoping wrong) | LOW-MEDIUM | Migrating from wrong-scope storage (e.g., session-only static) to correct-scope storage (`PlayerPrefs`) is a small, isolated change if the unlock API (`BossUnlockManager.Unlock/IsUnlocked`) was designed as a clean seam from the start — costly only if unlock checks were scattered ad hoc across many call sites instead |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|--------------------|----------------|
| 1. TimeScale convention violation in new boss/module timers | Each new boss's pattern-FSM implementation phase | Explicit playtest step: trigger boss pattern while holding Attack / during a HitFreeze from killing another enemy; grep new files for `Time.deltaTime`/`WaitForSeconds(` |
| 2. Non-inherited boss duplication drift | Dedicated "shared boss infrastructure" phase (or Task 0 of the first new-boss phase), before boss #2 | Diff the resulting boss files — shared plumbing (spawn-gate, death sequence, player-death cleanup) should be structurally identical/inherited, not copy-pasted with renames |
| 3. Mid-combat module-swap state leaks | "Module abstraction" phase preceding the first non-F.I.O.R.A-shaped boss mechanic; leak-specific testing in the 보스 러시 game-mode phase | Swap through every module pairing mid-slowmo, mid-dash-coroutine, and with NOVA's drone alive; confirm `Time.timeScale`, gauge/resource UI, and spawned sub-objects are all correctly torn down each time |
| 4. Boss-room WorldGenerator cleanup exception missing | Its own explicit design+implementation step, before/alongside the first production (non-test-hack) boss room | Deliberately stall a boss fight (or use MAX's forward momentum) past the point a normal room would be `Destroy()`-ed; confirm the boss room and its shared-boss-base instance survive until combat resolves |
| 5. Mouse-only aim / missing touch bindings | Shared input-infrastructure phase, early in the milestone, alongside Pitfall 3's abstraction work | Run on an actual Android build/device (not just Editor mouse) for at least one aim-dependent new mechanic and basic movement |
| 6. Unscoped death/restart persistence | Its own small early phase, before the first boss whose defeat grants an unlock | Full death→restart cycle confirms unlocked modules survive; explicit written decision on app-restart-scope is confirmed against actual behavior |

## Sources

- Direct source inspection (HIGH confidence, primary evidence for every pitfall above): `Assets/Scripts/Enemy/BossEnemy.cs`, `Assets/Scripts/Enemy/EnemyBase.cs`, `Assets/Scripts/Player/CombatController.cs`, `Assets/Scripts/Player/InputManager.cs`, `Assets/Scripts/Player/RollController.cs`, `Assets/Scripts/Player/InvincibilityHandler.cs`, `Assets/Scripts/Player/ChronoGaugeController.cs`, `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/Player/PlayerDeathHandler.cs`, `Assets/Scripts/World/WorldGenerator.cs`, `Assets/Scripts/World/FloorManager.cs`, `Assets/Scripts/World/ScoreManager.cs`, `Assets/Scripts/World/EnemySpawner.cs`, `Assets/Scripts/Room/RoomClearCondition.cs`, `Assets/Scripts/UI/DeathScreenController.cs`, `Assets/Scripts/UI/AttackSelectController.cs`, `Assets/Scripts/UI/AttackTypeSelector.cs`, `Assets/InputSystem_Actions.inputactions`
- Project planning docs (HIGH confidence, primary evidence): `.planning/PROJECT.md`, `.planning/phases/16-boss-room-lifecycle/16-CONTEXT.md`, `.planning/phases/16-boss-room-lifecycle/16-DISCUSSION-LOG.md`, `.planning/phases/15-fsm/15-06-PLAN.md`, `STORY.md`
- No external/Context7/WebSearch sources were needed or used — every pitfall here is specific to this codebase's actual current implementation, not generic Unity/mobile-game domain knowledge.

---
*Pitfalls research for: Fast v4.0 (보스 캐릭터 확장 & 게임 모드)*
*Researched: 2026-07-20*
