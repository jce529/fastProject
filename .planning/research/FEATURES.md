# Feature Research

**Domain:** Mobile 2D action platformer — 4 new boss mechanics, meta-progression unlock, 2 game modes (v4.0 milestone)
**Researched:** 2026-07-20
**Confidence:** MEDIUM (genre-pattern analysis from established action-game design conventions, cross-checked against current codebase; no single canonical "mobile dual-control boss" source exists to verify against, so mechanic-specific risk calls are flagged LOW where noted)

## Context Established From Codebase

Before the mechanic-by-mechanic breakdown, four load-bearing facts from the existing code shape every recommendation below:

1. **`CombatController.cs` is a single monolithic control scheme** — hold Attack = slow-mo, release = dash-teleport-kill nearest target in a Linear/Fan shape selected via the static `AttackTypeSelector`. There is no existing concept of "swappable attack modules." Aim direction is currently derived from **`Mouse.current`** (`GetMouseWorldDirection()`), a desktop-only input — this is a pre-existing gap, not new to v4.0, but every new mechanic that needs an aim/steer direction (DeadEye, NOVA, arguably MAX) will expose it harder than Linear/Fan ever did on a touch device.
2. **`BossEnemy.cs` does not inherit `EnemyBase`** — it is a standalone `MonoBehaviour` implementing `IEnemy`/`ISpawnGatable` directly, with its own Telegraph→Attack→Vulnerable FSM and its own `IsAlive` semantic override (`IsAlive` means "targetable," not "alive" — a real `_isDefeated` flag tracks death). This is the reusable pattern for new bosses' base contract, but the Telegraph→Attack→Vulnerable *loop* itself does not fit SAMURAI (no vulnerable window, pure parry timing), MAX (no telegraph, continuous careening), or NOVA (two independently-acting bodies) — each new boss needs a **parallel FSM**, not a subclass extension.
3. **No persistence layer exists anywhere in the codebase.** `ScoreManager`, `AttackTypeSelector`, `FloorManager` are all static in-memory classes reset on scene reload. Meta-progression (unlock persisting across runs/deaths) is **100% new infrastructure**.
4. **SAMURAI explicitly drops slow-mo** and MAX explicitly replaces normal movement with momentum-as-attack — both mean the "module" a player equips is not a variant of `CombatController`, it is a **full alternate control scheme that must suppress/replace `CombatController`'s hold-release loop while active**. This reframes "4 mechanics + 2 modes" as building 4 near-independent control schemes plus a runtime module-swap orchestrator — the single largest architectural undertaking implied by the design doc.

---

## Feature Landscape

### DeadEye — Revolver / Reload-Economy Combat

Genre precedent: resource-scarcity gunplay (Resident Evil-style ammo tension, Enter the Gungeon's reload-as-tempo-break), aim-then-fire fan weapons already partially precedented by the existing Fan attack type.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Visible ammo counter (6-round) | Any resource-gated weapon is unreadable without a persistent count — players must always know "can I fire?" | LOW | New HUD element, similar scope to existing gauge UI in `HUDController` |
| Reload state feedback (auto 1/round/sec, 3s full from empty) | Silent reload = player confusion about why attack didn't fire | LOW-MEDIUM | Needs its own timer component (`AmmoController`), unscaled-time based like `ChronoGaugeController`/roll cooldown to stay immune to slow-mo timeScale |
| Empty-fire feedback (dry-fire cue, no silent fail) | Universal resource-system table stakes — silent failure reads as a bug | LOW | Reuse `AudioManager` SFX pattern |
| Boss: 6 tracking reticles with a fire delay | Mirrors the player's own ammo count thematically — matches genre convention of "boss uses the mechanic it's about to teach you" | MEDIUM | Reuses BossEnemy's Telegraph-then-Attack shape conceptually but Attack phase spawns/tracks N independent markers instead of one hitbox |

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Reload-timing risk decisions (push with rounds left vs. retreat-reload) | Creates a resource-tension decision loop genuinely distinct from Overclock's auto-regen gauge | MEDIUM | Only a differentiator if reload cost is tuned to bite — see pitfall below |
| Weave-between-reticles boss pattern | Direct "read the pattern, don't get hit" skill test distinct from BossEnemy's existing single-hitbox dodge | MEDIUM | Six independently-tracking reticles is more dodge-pattern complexity than the current single-boss telegraph |

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|------------------|-------------|
| Ammo mechanic without keeping slow-mo aim | Feels "more like a real gun" | Removing slow-mo here (unlike SAMURAI, which explicitly drops it) is not specified in the design doc and would fragment the Core Value across 3 of 4 modules instead of 1 — undermines the "hold=slowmo" hypothesis this whole game exists to test | Keep DeadEye's hold=slow-mo/aim, release=fire; gate only the **release** action by ammo count, matching the design doc's framing as a variant on the existing philosophy, not a replacement of it |
| Manual/skill-based reload (mash button, timed reload bar) | Adds "depth" | Not specified in design doc (1/sec auto is explicit); a timed-reload minigame is scope creep for a prototype validating whether resource-gating itself is fun | Ship the flat auto-reload rates as specified; only consider a skill-reload layer post-validation |

**Dependencies on existing systems:** `CombatController` (needs a parallel/alternate branch or a fully separate `DeadEyeCombatController` that reuses its slow-mo/highlight/dash-kill skeleton but gates fire by ammo), `ChronoGaugeController` (co-exists, not replaced), `IEnemy`/`BossEnemy` pattern-loop shape (reused for the reticle-fire phase), `HUDController` (new ammo UI element), `AudioManager` (dry-fire/reload SFX).

---

### SAMURAI — Parry / No-Slowmo Tutorial Boss

Genre precedent: Sekiro-style posture/parry timing, Cuphead's timed parry bounce, Punch-Out!!-style pattern-read-and-react — and, per PROJECT.md's own note, this is explicitly the game's **first unlock/tutorial** and the mechanism by which parrying (previously Out of Scope for the whole project) enters scope at all.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| High-contrast, unambiguous parry telegraph | "Wrong input = instant death" demands the fairest possible read — any subtlety here reads as unfair rather than skillful | LOW-MEDIUM | Visual + audio double-signal, same "redundant signaling" pattern BossEnemy already uses (color tint + stop) |
| Generous parry window tuned for touchscreens | Precision-timing mechanics are latency-sensitive; touch input has materially worse precision/latency than mouse/controller | MEDIUM | See pitfall below — this is the single most important tuning flag for this mechanic |
| Distinct input from existing Attack/Roll gestures | Player must not confuse "parry" with "dash-teleport-kill" or "dodge-roll" muscle memory already trained by the base game | LOW-MEDIUM | Input-mapping decision needed: reuse Roll button contextually, or introduce a dedicated Parry input — flag as open design question |
| Clear success (counter opening) vs. failure (death) feedback | Since failure is lethal, ambiguity between "did I parry or not" is the single fastest way to make this feel cheap | LOW | Reuse existing hit-flash/death patterns already in the codebase |

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Counter-hit window on successful parry | Rewards precision generously — teaches "parry is worth it," critical for a tutorial-positioned boss | LOW-MEDIUM | Simplest version: successful parry = enemy enters BossEnemy-style Vulnerable state briefly |
| SAMURAI as the philosophical anchor for "reflex over resource" | Differentiates cleanly against DeadEye (resource), MAX (momentum), NOVA (multitasking) — four genuinely distinct combat philosophies is the differentiator across the whole roster, not any one boss alone | N/A | Design framing only, no extra implementation cost |

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|------------------|-------------|
| Frame-perfect / sub-150ms parry windows (genre-authentic "hardcore" timing) | Feels more "Sekiro-authentic," more skill-expressive | Touch input adds real latency and imprecision on top of the coded window (targeting tasks measurably degrade at latencies as low as ~41ms per UX research); a PC-tight parry window ported verbatim to touch will read as broken/unfair, not as a skill mechanic, and will contaminate "is parry fun" validation with "is touch laggy" noise | Widen the window generously (400-600ms class, tune in playtest) and treat window width as an explicit exposed tunable, the same way `CombatController`/`RollController` already expose timing values as `[SerializeField]` fields |
| Mixed parry-only + normal-dodge-required attacks in the same fight | More "authentic" boss variety | Doubles the FSM surface (two attack categories with two different correct-response rules) for the *tutorial* boss, which should have the lowest cognitive load of the four — risks muddying the very first lesson the game teaches | MVP: make every SAMURAI attack parry-gated (uniform rule, one thing to learn); defer attack variety to post-validation if the core parry loop proves fun |

**Dependencies on existing systems:** New parallel FSM (does **not** reuse `BossEnemy`'s Telegraph→Attack→Vulnerable loop structure — parry timing is a different shape entirely); must actively **suppress** `CombatController`'s hold=slow-mo behavior while this module/arena is active (no existing "disable combat controller" toggle exists beyond `ForceExitCombatState()`, which is a one-shot reset, not a suppression flag — new state needed); `PlayerController.TriggerDeath()` reused as-is for the instant-death path; explicitly **no** dependency on `ChronoGaugeController` — this mechanic actually *reduces* dependency surface versus the other three, which is a genuine argument for building it first.

---

### MAX — Movement-Is-Attack, Unstoppable Momentum

Genre precedent: the boss-side pattern (lure an unstoppable foe into a wall for a stun window) is a well-worn trope (Dark Souls' Taurus Demon-style environmental-bait bosses, classic Zelda "ram the boss into a pillar" fights) and is low-risk to implement. The **player-side** mechanic (an unstoppable, wall-lethal momentum kit as a reward ability) has few clean 1:1 genre precedents — closest analogues are momentum/skate-physics runners and "constant-motion, no braking" arcade movement, not a commonly-shipped melee-combat verb. Flag this honestly as the least-precedented of the four, which raises (not lowers) its validation importance.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Readable "unstoppable" feedback (speed trail, escalating camera shake/FOV cues) | Player must instantly understand "I cannot stop" or every wall death will feel like a bug, not a consequence | LOW-MEDIUM | Reuses `TrailRenderer`/`CameraFollow.Shake` patterns already wired into `CombatController` |
| Unambiguous collision rules (wall = death, enemy = kill, hazard tags consistent) | Inconsistent collision consequences is the fastest way to make a risk/reward mechanic feel arbitrary rather than exciting | LOW-MEDIUM | Needs clean tagging/layer discipline, not new tech |
| Boss: careen-into-wall stun window | Direct genre-standard "environmental bait" pattern, well understood, safe to implement | MEDIUM | Reuses BossEnemy's velocity-based Telegraph movement conceptually; the stun window is a variant of the existing Vulnerable-state concept |

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| High risk/reward tension (weapon = grenade) | Sharpest possible contrast against DeadEye/SAMURAI/NOVA's philosophies — good differentiation axis for the module roster as a whole | N/A | Design framing, contingent on tuning quality below |
| Player agency in steering an "unstoppable" body | Distinct control fantasy from anything else in the game (including the existing core Overclock dash, which is a single teleport, not sustained travel) | MEDIUM-HIGH | See pitfalls — this is the actual implementation risk |

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|------------------|-------------|
| Full physics-simulated momentum (acceleration curves, drift, wall bounce/ricochet angles) | "More authentic" momentum feel | Classic scope-creep bait for a prototype whose job is to validate *whether unstoppable momentum is fun*, not to ship a physics sandbox; also compounds the mobile-control-precision risk below | Kinematic constant-speed-with-steering model (`Rigidbody2D.MovePosition` along a steered direction at fixed speed) — same technique `BossEnemy`'s Telegraph phase already uses — is sufficient to test the hypothesis |
| Zero-buffer instant death on any wall contact | "Pure" risk/reward, no assists | Touchscreen steering of a constantly-moving, instant-death-on-touch object is one of the hardest control schemes to make feel fair on mobile (touch has materially more input imprecision/latency than a mouse or d-pad); a zero-buffer version risks reading as "the controls are bad" rather than "the risk is real," polluting the validation signal | Add a small forgiving grace/buffer window before lethal collision registers, exposed as a tunable (same pattern as `RollController.iFrameDuration`), tune in playtest |

**Dependencies on existing systems:** Needs a new movement-override component (conceptually a `MomentumController` sibling to `PlayerController`/`RollController`) that supersedes normal WASD movement and disables `CombatController`'s dash logic while MAX's module is active; interaction with `RollController`'s i-frames while MAX is active is an **open design question** (does Roll still exist under MAX? flag for planning, don't assume); reuses `CameraFollow.Shake`; new `MaxBoss` FSM reusing BossEnemy's velocity-driven-movement idiom but replacing Telegraph/Attack/Vulnerable with careen/wall-stun states. **Mobile-prototype flag:** this is, alongside NOVA, the highest technical-risk mechanic for a touchscreen build — recommend it not be built first, and recommend the kinematic/buffered version described above be treated as the actual MVP spec rather than the "pure" unstoppable/zero-buffer version implied by the design doc's wording.

---

### NOVA — Dual Independent Control (Body + Drone)

Genre precedent: R-Type's detachable "Force" pod (closest classic analogue — a companion device that can separate from and rejoin the main unit) is the cleanest precedent for a body+companion split; true simultaneous dual-analog control of two independently-moving bodies (à la *Brothers: A Tale of Two Sons*, which is a puzzle-platformer, not action-combat) is rare specifically in **combat** contexts because it collides with mobile's fundamental thumb-budget problem.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Clear "which entity is active" indicator | With two controllable/relevant bodies on screen, ambiguity about who's currently being steered is an immediate usability failure | LOW | Highlight/tint, same idiom as existing enemy-target highlight in `CombatController.UpdateHighlight()` |
| Independent hitboxes for body vs. drone | Design doc explicitly requires the player be able to choose "hit orb first or go straight for body" — both must be separately damageable | MEDIUM | Both likely need their own `IEnemy` implementation so `CombatController.FindNearestEnemyInRange()` can discriminate between them as distinct targets |
| Boss: body evades + drone harasses concurrently | Core differentiator of the "separated-control/harassment" philosophy — both halves must feel like part of one coordinated foe | MEDIUM-HIGH | Two coordinated-but-independent behaviors running at once; more moving FSM parts than any of the other 3 bosses |

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Genuinely novel "manage two things at once" combat philosophy | Sharpest differentiation of the whole module roster — no other module asks the player to split attention across two simultaneously-relevant entities | N/A | Contingent entirely on the control-scheme resolution below |
| Player-chosen engagement order (orb vs. body first) | Meaningful strategic choice at prototype scale without needing deep systems | LOW-MEDIUM | Emerges for free once both are independently damageable — no extra system needed beyond the hitbox split above |

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|------------------|-------------|
| True simultaneous free-movement dual-stick control (body on one virtual stick, drone on another) + a separate attack button | "Faithful" to "dual independent control" as literally worded in the design doc | This is 3 concurrent touch zones on one mobile screen at once — widely regarded as a control-scheme foot-gun even in dedicated twin-stick-shooter mobile ports, which is why mobile MOBAs resolve "pet/summon" control via auto-follow + tap-command rather than true dual analog; attempting this as literally specified is the single highest control-scheme risk in the entire milestone | A **toggle/possession-swap** scheme — one input target is "active" (freely moved) at a time, the other holds position or runs simple auto-behavior, with a quick tap/button to swap control — validates the "manage two things" hypothesis without requiring 3-way simultaneous touch input. Treat this as the actual MVP; true simultaneous dual-stick is a stretch goal only if the toggle version proves the hypothesis and players want more |
| Full pathfinding-quality evasion AI for the boss body | "Smarter" boss feels more challenging | Scope creep for a prototype — sophisticated evasion AI is a substantial systems investment unrelated to validating the core "dual control" hypothesis | Simple reactive flee-from-player heuristic, same complexity class as BossEnemy's existing directional Telegraph movement |
| Fully autonomous independent AI companion behavior for the drone's "harass" pattern | Feels more alive/unpredictable | Inventing a new AI system from scratch when a much cheaper reuse exists | Reuse `RangedEnemy`'s aim-line telegraph pattern for the drone's lunge/attack cadence instead of building new AI |

**Dependencies on existing systems:** `InputManager` needs a wholly new concept (possession/control-target swap) that doesn't exist today — mobile button budget is already tight (Move + Attack + Roll), so this input likely needs to reuse an existing button contextually rather than add a fourth persistent control; new drone `GameObject` with simplified (no-gravity) movement; both body and orb need `IEnemy` for `CombatController` targeting to discriminate; `CameraFollow` needs a leash-range cap on the drone to keep both entities on-screen on a mobile viewport — an explicit mobile-specific scope-limiter worth designing in from the start rather than discovering during playtesting.

---

## Meta-Progression (Boss Defeat → Module Unlock)

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Persistent unlock storage across runs/app-restarts | Currently **nothing** persists in this codebase (`ScoreManager`/`AttackTypeSelector`/`FloorManager` are all in-memory statics) — this is baseline new infrastructure, not an enhancement | LOW | `PlayerPrefs` is sufficient and appropriate for a local prototype; do not build a JSON save file or backend |
| Unlock notification + module-select screen | Player must clearly see "SAMURAI unlocked" and be able to pick it before a run | LOW-MEDIUM | Natural extension of the existing `AttackSelectController` scene, generalized from 2-way (Linear/Fan) to N-way module choice |
| Unlock survives player death | Boss-defeat progress must be independent from a run's score/floor state, which already resets on death | LOW | As long as the unlock write happens at boss-death time (in each new boss's `Die()`, alongside the existing `ScoreManager.AddBossKillScore()` call pattern already established in `BossEnemy.cs`), this falls out naturally |

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Narrative framing (HELIX "unlocking" F.A.S.T.'s modules) | Reinforces Core Value/story instead of reading as a bolted-on achievement system, at near-zero extra implementation cost | LOW | Copy/flavor-text only |

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|------------------|-------------|
| Full loadout/inventory system (multiple modules equipped at once, upgrade tiers, currency shop) | "Feels more like a real progression system" | Explicitly banned by PROJECT.md Out of Scope ("복잡한 성장 시스템 — 레벨업, 영구 강화, 상점") | A flat boolean-per-module unlock array; no tiers, no shop |
| Boss-order gating / prerequisite tree (must beat X before Y appears) | "More structured progression" | Design doc only specifies SAMURAI as first/tutorial; a full gating tree is unspecified scope creep requiring level-select UI | SAMURAI always available by default; the other 3 bosses unlock independently whenever encountered and defeated, in whatever order the floor RNG produces them |
| Cloud save / cross-device sync | "Modern" expectation | Unnecessary infrastructure for a local Android prototype | `PlayerPrefs` on-device only |

**Dependencies on existing systems:** New `PlayerPrefs`-backed unlock manager (net-new); each new `XBoss.Die()` must call it, following the exact pattern `BossEnemy.Die()` already uses for `ScoreManager.AddBossKillScore()`; `AttackSelectController`/its scene extended from a 2-choice to an N-choice, gated by which modules are unlocked.

---

## Game Mode 1: 한계 시험 (Limit Test)

Genre precedent: single-loadout-per-run roguelike structure (Hades' single-weapon-per-run choice, Risk of Rain's single-survivor-per-run) — this maps almost directly onto the *existing* infinite floor-climb loop (`WorldGenerator`/`FloorTimer`/`ScoreManager`/death→restart), just with a module chosen instead of an attack-type at the start.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Module-select before run start | Direct reuse of the existing `AttackSelectController` pre-run screen | LOW | Natural fit — this mode requires the *least* new plumbing of anything in the milestone |
| Score = HELIX evaluation metric | Reuses existing `ScoreManager` kill/clear/time-bonus scoring near verbatim | LOW | Framing/copy pass more than new logic |
| Locked module for the whole run; death returns to module-select | Matches the existing death→`AttackSelect` flow already shipped | LOW | No new state machine needed |

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|------------------|-------------|
| Mid-run relic/power-up drafts (Hades-style boons between floors) | "More roguelike" | Not specified in the design doc; explicit scope creep beyond PROJECT.md's Out of Scope ban on growth systems | None needed — "roguelike" here means infinite-climb + permadeath-into-restart, which v1.0-v3.0 already validated; the only new variable is which module is locked in |

**Complexity:** LOW overall — the main integration risk is ensuring whichever module's control scheme runs cleanly through the existing room/corridor/EXIT-portal pipeline that was designed only around the base Overclock loop (e.g., MAX's uncontrollable momentum could clip through geometry or overshoot an EXIT portal it can't stop for in time — flag as an integration test specific to MAX, not a Limit Test mode problem in general).

**Dependencies on existing systems:** `WorldGenerator`, `FloorTimer`, `ScoreManager`, `ExitPortal` (all reused as-is); `AttackSelectController` (extended); the meta-progression unlock manager (gates which modules are selectable).

---

## Game Mode 2: 보스 러시 (Boss Rush)

Genre precedent: classic strip-everything-but-bosses structure (Mega Man boss-rush stages, Cuphead's boss-only campaign, Furi's whole-game structure) with an endless/looping variant (Nuclear Throne/Enter the Gungeon-style endless boss cycling) matching "no run limit."

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Back-to-back boss gauntlet with no floor/corridor traversal | Definitional to the "boss rush" genre label | MEDIUM | A new, much simpler "boss arena only" scene flow that bypasses `WorldGenerator` entirely |
| Free module switching mid-fight | The single differentiator that separates this mode from Limit Test | HIGH | Requires a runtime module-swap orchestrator that can cleanly enable/disable each module's control-scheme MonoBehaviours mid-encounter — the single largest net-new architecture piece in the whole milestone |
| Looping/escalating difficulty, no fixed endpoint | Matches "no run limit, endless" | LOW-MEDIUM | Reuse the existing floor-based difficulty-scaling *philosophy* (`FloorManager.CurrentFloor`-driven scaling), applied per boss-lap instead of per-floor |

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|------------------|-------------|
| True randomized boss order + weighted difficulty curve | "More replayable" | Overkill for a prototype validating "is boss rush fun" — randomization adds a tuning surface unrelated to the core hypothesis | Fixed repeating sequence (e.g., SAMURAI→DeadEye→MAX→NOVA→repeat with a minor per-lap difficulty bump); defer true randomization to post-validation |
| Zero-cost, any-instant module switching | "Maximum flexibility" | Could let players cheese each mechanic's core tension (e.g., swap out of MAX right before a lethal wall hit) — trivializes the very tension each module exists to test | A minimal switch cooldown or a "can't switch during a vulnerable/committed window" rule — but avoid over-designing a complex ruleset upfront; treat exact restriction as a playtest-tuning question, not a system to build in one pass |

**Complexity:** MEDIUM-HIGH — this mode inherits *all* the risk of the 4 individual mechanics, then adds runtime-swap-safety on top (stale coroutines/velocity/input-listeners from one module must be fully torn down before the next module's MonoBehaviours take over — conceptually similar to the cleanup `CombatController.ForceExitCombatState()` already performs for floor transitions, but that method is a one-shot reset, not built for hot-swapping between fundamentally different control schemes). **Sequencing flag:** Boss Rush should be built **last**, only after all 4 modules are independently stable in solo-boss-room testing (Limit Test / standalone testing) — attempting free-switching before each module is proven on its own is a dependency-ordering risk for the roadmap.

**Dependencies on existing systems:** All 4 boss FSMs + all 4 modules (must exist first); a new lightweight boss-arena scene/prefab bypassing `WorldGenerator`; a module-swap UI affordance (mobile screen space is a real constraint — likely a small persistent HUD cycle-button); a new orchestrating layer that supersedes `CombatController`'s single-fixed-scheme assumption.

---

## Feature Dependencies

```
SAMURAI module (no slow-mo, no dual-control, no momentum-physics, no ammo economy)
    └── lowest dependency surface of the 4 — recommend building FIRST (also the design doc's own tutorial-first intent)

DeadEye module
    └──requires──> existing CombatController hold/release/highlight/dash skeleton (extends, doesn't replace)
    └──requires (new)──> AmmoController/reload timer

MAX module
    └──requires (new)──> MomentumController (movement override, replaces PlayerController+CombatController for its duration)
    └──open question──> interaction with RollController i-frames while active

NOVA module
    └──requires (new)──> possession/control-swap input concept in InputManager
    └──requires (new)──> drone GameObject + IEnemy split (body + orb both targetable)
    └──requires──> CameraFollow leash-range cap (mobile viewport constraint)

Meta-progression (PlayerPrefs unlock manager)
    └──requires──> at least 1 boss's Die() to call into it (can be stubbed early, low coupling to the mechanics themselves)

한계 시험 (Limit Test)
    └──requires──> 1+ stable modules + meta-progression unlock manager + existing WorldGenerator/FloorTimer/ScoreManager (already built)

보스 러시 (Boss Rush)
    └──requires──> ALL 4 modules independently stable
    └──requires (new)──> runtime module-swap orchestrator (highest-risk net-new architecture in the milestone)
```

### Dependency Notes

- **DeadEye requires the existing `CombatController` skeleton:** its hold=slow-mo/aim, release=fire loop is a variant of the current Fan-attack shape, not a rebuild — lowest architectural risk of the three non-SAMURAI mechanics.
- **MAX and NOVA both require entirely new control-scheme components** that don't extend anything in the current player-scripting layer — they are the two highest-complexity, highest-mobile-risk mechanics and should be sequenced later, with simplified fallback specs (kinematic MAX, toggle-swap NOVA) treated as the real MVP rather than the "pure" spec wording.
- **Boss Rush requires all 4 modules to pre-exist and be individually stable** — it is a pure integration/stress-test mode on top of the mechanics, not a parallel workstream; building it early would mean debugging module-swap issues and individual-mechanic issues simultaneously.
- **Meta-progression has almost no dependency on mechanic complexity** — it can and should be built early/in parallel, gated only by "at least one boss exists to unlock something."

---

## MVP Definition

### Launch With (v4.0 minimum to validate hypotheses)

- [ ] SAMURAI boss + parry mechanic, generous touch-tuned window, **all** attacks parry-gated (no mixed variety yet) — cheapest full slice, matches tutorial-first design intent
- [ ] DeadEye boss + revolver/reload mechanic, reusing existing hold-release/Fan-shape skeleton with an ammo gate on release
- [ ] MAX boss + **kinematic** constant-momentum player mechanic (no physics drift/bounce sim) with a forgiving wall-collision buffer, not zero-buffer
- [ ] NOVA boss + **toggle/possession-swap** dual control (not true simultaneous dual-stick), body and orb both independently damageable
- [ ] `PlayerPrefs`-based module unlock persistence + module-select UI extension (generalize `AttackSelectController` from 2-way to N-way)
- [ ] 한계 시험 (Limit Test) mode — near-direct reuse of the existing floor-climb loop with module lock-in at start

### Add After Validation (v4.x)

- [ ] 보스 러시 (Boss Rush) mode with free module switching — only after all 4 modules independently validated as fun and stable
- [ ] True simultaneous dual-stick NOVA control — only if the toggle-swap version validates the "manage two things" hypothesis and testers explicitly want more
- [ ] Full physics-based MAX momentum (drift/bounce) — only if the kinematic version proves the risk/reward hypothesis fun but feels too rigid
- [ ] Mixed parry + normal-dodge attack variety for SAMURAI — only after the uniform all-parry version proves the core loop fun

### Future Consideration (v5+, explicitly deferred)

- [ ] Module upgrade tiers / passive skill trees — banned by PROJECT.md Out of Scope precedent (no growth systems)
- [ ] Boss-order randomization / weighted difficulty curves in Boss Rush — pure replayability polish, unrelated to the core "is this fun" hypothesis
- [ ] Cross-device/cloud save for unlock progression — unnecessary for a local prototype

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| SAMURAI parry mechanic + boss | HIGH | MEDIUM | P1 |
| DeadEye ammo mechanic + boss | HIGH | MEDIUM | P1 |
| MAX kinematic momentum mechanic + boss | HIGH | MEDIUM-HIGH | P1 |
| NOVA toggle-swap dual control + boss | HIGH | HIGH | P1 |
| Meta-progression unlock (`PlayerPrefs`) | HIGH | LOW | P1 |
| 한계 시험 (Limit Test) mode | HIGH | LOW | P1 |
| 보스 러시 (Boss Rush) mode w/ free switching | MEDIUM-HIGH | HIGH | P2 |
| True simultaneous dual-stick NOVA | MEDIUM | HIGH | P3 |
| Full physics MAX momentum (drift/bounce) | LOW-MEDIUM | MEDIUM-HIGH | P3 |
| Module upgrade tiers / shop | LOW (out of scope) | HIGH | P3 (explicitly deferred) |

**Priority key:**
- P1: Must have to validate the milestone's core hypotheses
- P2: Should have, but sequenced after P1 mechanics are independently stable
- P3: Nice to have, future consideration only if playtesting signals demand it

## Genre-Precedent Reference

| Mechanic | Closest Genre Precedent | Our Approach |
|----------|--------------------------|---------------|
| DeadEye reload economy | Resident Evil-style ammo tension, Enter the Gungeon reload tempo-break | Gate only the existing hold/release loop's release-action by ammo; auto-reload rates as specified |
| SAMURAI parry | Sekiro/Cuphead-style timed parry, tuned for touch instead of mouse/controller | Wide, tunable parry window; uniform all-attacks-parryable for MVP |
| MAX momentum + wall-stun boss | Player side: momentum/skate-physics runners (weak precedent, genuinely novel). Boss side: Dark Souls/Zelda-style "lure into wall" bait bosses (strong, well-worn precedent) | Kinematic constant-speed steering with a forgiving collision buffer, not full physics sim |
| NOVA dual control | R-Type "Force" pod (companion device) is the closest analogue; true simultaneous dual-stick combat control is rare specifically because of the mobile thumb-budget problem | Toggle/possession-swap control scheme as MVP, not literal simultaneous dual-stick |
| Boss Rush mode | Mega Man/Cuphead boss-rush structure; Nuclear Throne/Enter the Gungeon endless-boss-cycling for the "no run limit" variant | Fixed repeating boss sequence with a minor per-lap difficulty bump; defer randomization |
| Limit Test mode | Hades/Risk of Rain single-loadout-per-run roguelike structure | Near-direct reuse of existing floor-climb loop, module replaces attack-type as the pre-run choice |

## Sources

- Existing codebase: `Assets/Scripts/Player/CombatController.cs`, `Assets/Scripts/Enemy/BossEnemy.cs`, `Assets/Scripts/Enemy/EnemyBase.cs`, `Assets/Scripts/World/ScoreManager.cs`, `Assets/Scripts/Player/RollController.cs`, `Assets/Scripts/Player/InputManager.cs`, `Assets/Scripts/UI/AttackSelectController.cs`, `Assets/Scripts/UI/AttackTypeSelector.cs`, `Assets/Scripts/World/FloorManager.cs` (read directly, HIGH confidence on all architectural/dependency claims)
- `.planning/PROJECT.md` (milestone goal, Out of Scope constraints, target feature spec — HIGH confidence, primary source of truth)
- Genre-pattern analysis (training-data knowledge, MEDIUM confidence): Sekiro/Cuphead parry conventions, Resident Evil/Enter the Gungeon ammo-tension design, Dark Souls/Zelda environmental-bait boss patterns, R-Type Force-pod dual-entity precedent, Hades/Risk of Rain single-loadout roguelike structure, Mega Man/Cuphead/Furi boss-rush structure
- [Touch Controls for Mobile Games: Input Patterns and Feedback (Cursa)](https://cursa.app/en/page/touch-controls-for-mobile-games-input-patterns-and-feedback) — MEDIUM confidence, corroborates touch-input latency/precision concerns raised for SAMURAI/MAX
- [How Display Response Time Affects Parry and Block Timing in Action RPGs (KTC Play)](https://us.ktcplay.com/blogs/technology-hub/display-response-time-affects-parry-timing-action-rpgs) — MEDIUM confidence, supports the "widen the parry window for touch" recommendation
- [Boss Rush explained (Pudgy Cat)](https://pudgycat.io/what-is-boss-rush-explained/) and [Boss Rush (TV Tropes)](https://tvtropes.org/pmwiki/pmwiki.php/Main/BossRush) — MEDIUM confidence, genre-structure grounding for the Boss Rush mode section
- Endless/roguelike mode structure comparison (WebSearch synthesis, LOW-MEDIUM confidence, single-pass search not cross-verified against a canonical source) — informs the Limit Test vs. Boss Rush "fixed endpoint vs. endless" framing

---
*Feature research for: mobile 2D action platformer boss-mechanic expansion (Fast v4.0)*
*Researched: 2026-07-20*
