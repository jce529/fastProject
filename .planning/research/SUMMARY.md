# Project Research Summary

**Project:** Fast (가칭) — v4.0 보스 캐릭터 확장 & 게임 모드
**Domain:** Mobile 2D action platformer — pluggable combat-module boss expansion, meta-progression, game modes
**Researched:** 2026-07-20
**Confidence:** MEDIUM-HIGH

## Executive Summary

Fast v4.0 takes a single-mechanic mobile action prototype (hold=slowmo, release=dash-teleport-OHK, i.e. "Overclock"/F.I.O.R.A) and turns it into a 5-module combat system: 4 new mechanically-distinct bosses (DeadEye's ammo/reload economy, SAMURAI's parry-timing tutorial fight, MAX's unstoppable-momentum risk/reward, NOVA's dual body+drone control) each paired with a player-side control scheme, plus a persistent boss-unlock progression layer and two game modes (한계 시험: single locked module, roguelike floor-climb; 보스 러시: free module-swap, endless boss gauntlet). All four research passes agree this is fundamentally an architecture-extraction problem before it is a content problem: the existing codebase has never needed pluggability (one hardcoded CombatController, one non-inherited BossEnemy FSM, zero persistence, mouse-only aim direction) and every one of the 4 new mechanics stresses one of those hardcoded assumptions simultaneously.

The recommended approach, consistent across STACK/ARCHITECTURE/PITFALLS research, is: (1) extract a small IPlayerCombatModule strategy interface from CombatController, migrating the existing Overclock logic verbatim as the first module (pure refactor, must be regression-tested byte-for-byte before new modules are added); (2) extract a BossEnemyBase sibling to EnemyBase from the current BossEnemy.cs (its _isDefeated/vulnerability/death-sequence plumbing) before writing a second boss, not after, to avoid 4x copy-paste drift; (3) build BossUnlockManager as a new PlayerPrefs-backed static class, this project's first-ever disk persistence, kept structurally isolated from DeathScreenController.RestartGame()'s existing "reset everything" convention; (4) fix the pre-existing Mouse.current-only aim-direction gap (no touch equivalent exists today) as shared infrastructure, since 3 of 4 new mechanics need a working touch aim/target signal. No new Unity packages are required anywhere; com.unity.inputsystem@1.19.0 already ships EnhancedTouch, OnScreenStick/Button, and Pointer, covering every input need identified.

The dominant risk is not any single mechanic's feasibility (all 4 are technically buildable with what's installed) but sequencing and state-leak risk at the seams: mid-combat module swapping (보스 러시) will silently leak Time.timeScale, gauge state, _isBusy lockouts, or orphaned GameObjects (NOVA's drone) across a swap unless an explicit enter/exit lifecycle is designed before the second module exists; boss rooms have no exemption from WorldGenerator's cleanup sweep today (a requirement ID was proposed and deferred, never implemented) so a long or momentum-driven fight (MAX especially) can have its room destroyed mid-combat; and every new boss/module timer must use the existing unscaledDeltaTime/WaitForSecondsRealtime convention or it will silently break under the player's own slowmo/hit-freeze. All four research files independently converge on the same build order: shared infrastructure first (unlock manager, module interface, input fix), SAMURAI second (lowest mechanical novelty, designed as the tutorial boss), DeadEye third, MAX and NOVA last (highest mobile-control and architecture risk), Boss Rush mode dead last (pure integration on top of all 4 modules, mirrors this project's own prior-milestone history of getting blocked exactly this way in Phase 15/16).

## Key Findings

### Recommended Stack

No new packages needed. Everything required (multi-touch tracking, virtual on-screen controls, pointer-agnostic input) already ships inside the installed com.unity.inputsystem@1.19.0. The one required fix is architectural, not a dependency: Mouse.current-based aim direction has to become Pointer.current/EnhancedTouch-based, since it currently silently degrades to a dead vector on any device without a mouse (i.e., every Android build).

**Core technologies:**
- EnhancedTouch.Touch.activeTouches (installed, com.unity.inputsystem 1.19.0) — per-finger stable-ID multi-touch tracking, required for NOVA's dual streams and DeadEye's multi-tap marking; the raw positional Touchscreen.touches[n] array is unusable for this since slots reassign as fingers lift.
- OnScreenStick/OnScreenButton (UnityEngine.InputSystem.OnScreen, installed) — virtual joystick/buttons wired into the existing action-map architecture instead of a parallel bespoke touch system; avoids third-party joystick asset packages entirely.
- PlayerPrefs (built-in) — sufficient for the entire unlock-progression scope (a handful of module-unlock booleans); this project has zero existing persistence code, so this is genuinely new infrastructure, but does not need a save-system package.
- Pointer.current (replacing Mouse.current) — resolves to whichever device produced input last (mouse in Editor, touch on device) without branching code; the single most important "fix before building anything new" item from Stack research.

### Expected Features

**Must have (table stakes / MVP for v4.0):**
- SAMURAI boss + parry mechanic (all attacks parry-gated, no mixed variety yet) — tutorial-first, lowest dependency surface
- DeadEye boss + ammo/reload mechanic, reusing the existing hold-release/Fan-shape skeleton with a release-side ammo gate
- MAX boss + kinematic constant-momentum player mechanic (no physics drift/bounce sim), forgiving wall-collision buffer (not zero-buffer)
- NOVA boss + toggle/possession-swap dual control (not true simultaneous dual-stick), body and drone independently damageable
- PlayerPrefs-based module unlock persistence + N-way module-select UI (generalized from existing 2-way AttackSelectController)
- 한계 시험 (Limit Test) mode — near-direct reuse of existing floor-climb loop, module locked in at run start

**Should have (after P1 validated):**
- 보스 러시 (Boss Rush) mode with free module switching — only once all 4 modules are independently stable
- True simultaneous dual-stick NOVA control — only if toggle-swap validates the hypothesis and testers want more
- Full physics-based MAX momentum (drift/bounce) — only if kinematic version proves fun but feels too rigid

**Defer (v2+/explicitly out of scope):**
- Module upgrade tiers, passive skill trees, shops — explicitly banned by PROJECT.md's Out of Scope
- Boss-order randomization/weighted difficulty in Boss Rush — replayability polish, not core-hypothesis-relevant
- Cloud save / cross-device sync — unnecessary for a local Android prototype

### Architecture Approach

The existing codebase has exactly one instance of each pattern this milestone needs multiples of: one hardcoded combat scheme (CombatController), one standalone non-inherited boss FSM (BossEnemy), zero persistence, and a straight-line scene flow with a single binary choice. The research converges on extracting minimal, proven-necessary abstractions rather than building speculative generalized frameworks, consistent with this project's own established convention ("minimal extraction, not full inheritance," per EnemyBase's own header comment).

**Major components:**
1. IPlayerCombatModule (new interface) + OverclockModule/DeadEyeModule/SamuraiParryModule/MaxMomentumModule/NovaDualModule — CombatController becomes a host retaining slow-mo lifecycle/gauge/_isBusy lockout/hit-freeze, delegating only targeting+resolution to the active module.
2. BossEnemyBase (new, sibling to EnemyBase, not inheriting it) — extracted from current BossEnemy.cs: defeat-guard, death sequence, spawn-gate wiring, player-death cleanup, vulnerable-tint highlight helpers. Each of the 4 new bosses subclasses this with its own independent pattern-loop state machine (no shared generalized Telegraph-Attack-Vulnerable FSM, the 4 mechanics don't share a state shape).
3. BossUnlockManager (new, static, PlayerPrefs-backed) — first disk persistence in the project; deliberately kept out of DeathScreenController.RestartGame()'s existing unconditional reset sweep.
4. GameModeManager (new, static, data-only, mirrors FloorManager convention) + ModeSelectController — inserted as MainMenu -> ModeSelect -> ModuleSelect(extended AttackSelectController) -> SampleScene.
5. WorldGenerator mode-branch (modified last, highest risk) — 보스 러시's endless boss-only loop either branches this highest-fan-in file or forks a parallel simpler generator; deferred to the final integration step, matching this project's own prior-milestone pattern of saving WorldGenerator integration for last.

### Critical Pitfalls

1. New boss/module timers silently break under the player's own slowmo/hit-freeze — any new script using Time.deltaTime/WaitForSeconds instead of Time.unscaledDeltaTime/WaitForSecondsRealtime will freeze or crawl during Overclock's slowmo or HitFreeze (Time.timeScale=0), invisible until a specific interaction (holding Attack during another boss's pattern) is tested. Prevention: grep-check for zero Time.deltaTime/WaitForSeconds( matches in every new boss file; copy BossEnemy.cs's existing realtime pattern verbatim.
2. 4x copy-paste of the non-inherited BossEnemy.cs FSM instead of extracting a shared base first — creates 5 independent copies of non-trivial death/spawn-gate/vulnerability plumbing that will drift on any shared bugfix. Prevention: extract BossEnemyBase once, before boss #2 is written, not as a retrofit after 4 divergent copies exist.
3. Mid-combat module swap (Boss Rush) leaks state — CombatController has no module abstraction today; swapping modules mid-fight without dedicated enter/exit lifecycle hooks will leave Time.timeScale stuck, gauge/resource UI showing the wrong module, _isBusy permanently locked, or NOVA's drone orphaned in the scene. Prevention: design the module interface's enter/exit teardown (generalizing the existing ForceExitCombatState() pattern) before implementing the second module, and test every module-pair swap combination, not just one.
4. Boss rooms have no exemption from WorldGenerator's cleanup sweep — this was proposed (BOSS-10) and explicitly deferred in Phase 16, never implemented; a long or momentum-driven fight (MAX especially, given constant forward motion) can have its room Destroy()-ed mid-combat today. Prevention: treat this as net-new required work, resolved before/alongside the first new boss room touching the real chain-based spawn flow, not assumed solved.
5. Mouse-only aim direction + zero existing touch bindings for movement — CombatController.GetMouseWorldDirection() reads Mouse.current unconditionally, degenerate on Android; InputSystem_Actions.inputactions has no touch/on-screen binding for Move at all. Prevention: fix once as shared input infrastructure (Pointer/EnhancedTouch-based), not per-boss; do this early since 3 of 4 new mechanics need working touch aim.

## Implications for Roadmap

Based on combined research, suggested phase structure (7 phases, risk-ordered):

### Phase 1: Shared Infrastructure — Unlock Persistence + Combat Module Abstraction + Touch Input Fix
**Rationale:** Zero/low coupling to gameplay content; every subsequent boss/module phase depends on these three seams existing correctly. Retrofitting any of the three after 2+ modules/bosses exist is materially more expensive (Pitfalls 2, 3, 5's recovery costs are all MEDIUM-HIGH vs. LOW if done first).
**Delivers:** BossUnlockManager (PlayerPrefs-backed, isolated from RestartGame()'s reset sweep); IPlayerCombatModule interface with OverclockModule as the first migrated (verbatim, zero-behavior-change) concrete module; Pointer/EnhancedTouch-based aim-direction replacement for Mouse.current.
**Addresses:** Meta-progression unlock storage (FEATURES.md); module pluggability precondition for all 4 new mechanics.
**Avoids:** Pitfall 3 (module-swap leaks), Pitfall 5 (mouse-only input), Pitfall 6 (persistence scoping).

### Phase 2: Shared Boss Infrastructure — BossEnemyBase Extraction
**Rationale:** Must land before boss #2 is written (Pitfall 2); reuses the exact "minimal extraction, not full inheritance" methodology this project already applied to EnemyBase in Phase 16.
**Delivers:** BossEnemyBase (sibling to EnemyBase) carrying _isDefeated guard, death sequence, spawn-gate wiring, player-death cleanup, vulnerable-tint highlight helpers, abstracted so "defeated" is a boss-owned decision (not a flat hit-counter).
**Uses:** Direct extraction from current BossEnemy.cs.
**Implements:** Architecture Q2 recommendation (do not force one generalized Telegraph-Attack-Vulnerable FSM across 4 structurally different bosses).

### Phase 3: SAMURAI Boss + Parry Module
**Rationale:** PROJECT.md itself flags this as tutorial/highest-priority unlock; architecturally closest to existing precedent (melee+telegraph already proven); lowest dependency surface of the 4 (no slowmo, no ammo economy, no momentum physics, no dual control), genuine argument for building first per FEATURES.md's dependency graph.
**Delivers:** SamuraiBoss : BossEnemyBase, parry-timing player-side module (own component or module, not IEnemy-contract-extended), touch-tuned generous parry window (recommend 200-250ms initial, tune on real low-end Android device).
**Addresses:** SAMURAI mechanic (FEATURES.md P1); tutorial-boss role.
**Avoids:** Pitfall 1 (realtime timers) — first real test of the convention on a genuinely new timing-sensitive mechanic.

### Phase 4: DeadEye Boss + Ammo/Reload Module
**Rationale:** Next-lowest risk — reuses existing hold-release/Fan-shape/RangedEnemy aim-line precedent; only new piece is a shot-counter/reload state, no new physics or input paradigm. Sequencing after SAMURAI stress-tests the module interface on a second, still-conventional case before the true outliers (MAX, NOVA).
**Delivers:** DeadEyeBoss : BossEnemyBase (6 tracking reticles + fire delay), DeadEyeModule (ammo-gated release action, unscaled-time reload timer), ammo-counter HUD element.
**Uses:** IPlayerCombatModule interface, EnhancedTouch for the 6-tap marking gesture (fat-finger mitigations: generous hit-test radius, immediate visual tag confirmation, untag-on-retap).

### Phase 5: MAX Boss + Momentum Module
**Rationale:** Highest architecture-fit risk of the "normal" cases — "movement IS the attack" may not fit the hold-slowmo-release-resolve shape at all, and needs a novel collision-triggered (not timer-triggered) vulnerability signal. Sequencing after 2 conventional modules means interface gaps surface on stable ground first.
**Delivers:** MaxBoss : BossEnemyBase (careen-into-wall stun trigger), kinematic constant-speed-with-steering player mechanic (not full physics sim), forgiving wall-collision buffer, Time.timeScale-compensated velocity lock (reusing PlayerController's existing compensation pattern).
**Avoids:** Pitfall 4 (WorldGenerator cleanup) is highest-stakes here specifically, since MAX's constant forward momentum is the likeliest mechanic to push a player past a room's cleanup boundary mid-fight — this phase should not proceed until Phase 4.5 below is resolved.

### Phase 4.5 (parallel or just before Phase 5): WorldGenerator Boss-Room Cleanup Exemption
**Rationale:** This is explicitly still-open, deferred work from Phase 16 (BOSS-10), not solved infrastructure — must not be assumed. MAX's design (highest risk of triggering the missing exemption) makes this a hard prerequisite before MAX's boss room goes into the real spawn flow.
**Delivers:** Boss-type-agnostic (not BossEnemy-specific) cleanup exemption logic in WorldGenerator.CleanupSection(), gated on shared-base IsAlive/_isDefeated state.
**Avoids:** Pitfall 4 directly — recovery cost is flagged HIGH if discovered post-bug-report rather than designed upfront.

### Phase 6: NOVA Boss + Dual-Body/Drone Module
**Rationale:** Highest content and mobile-control risk of the 4 — two coordinated objects, an open design question (is the drone independently IEnemy-targetable), and the mobile thumb-budget problem makes literal PC-style dual-stick control a known foot-gun. Build last among bosses so interface adjustments discovered in Phases 3-5 are already stable.
**Delivers:** NovaBoss : BossEnemyBase (body-evade + drone-harass concurrent behavior), toggle/possession-swap player control (MVP; not true simultaneous dual-stick), CameraFollow leash-range cap for the drone.
**Research Flag:** The body-vs-drone IEnemy targetability decision (Architecture Q2) is a game-design call that should be resolved explicitly during this phase's planning, not defaulted silently.

### Phase 7: Game Modes — 한계 시험 + 보스 러시
**Rationale:** True integration step, analogous to WorldGenerator's role in the v3.0 milestone — depends on all 4 modules + unlock system existing and individually validated. This project's own history (Phase 15/16 blocking) shows attempting integration before individual pieces are proven causes exactly this kind of stall; do this decisively last.
**Delivers:** GameModeManager, ModeSelectController, mode-aware DeathScreenController.RestartGame(), WorldGenerator mode-branch for the endless boss-only loop of Boss Rush mode (or a parallel simpler generator).
**Research Flag:** The endless boss-only floor loop of Boss Rush mode is the single highest-risk piece in the entire milestone — flag for dedicated research/design discussion during phase planning, not assumed to be a simple mode toggle.

### Phase Ordering Rationale

- Dependency graph from FEATURES.md is explicit: SAMURAI has the lowest dependency surface (no slowmo/dual-control/momentum/ammo), meta-progression has almost no dependency on mechanic complexity (build early/parallel), and Boss Rush requires all 4 modules pre-existing and stable.
- Architecture research's "risk-ordered build order" and Pitfalls research's "phase to address" columns independently converge on the identical sequence: shared infrastructure -> shared boss base -> SAMURAI -> DeadEye -> MAX -> NOVA -> game modes.
- This mirrors the project's own documented history (v3.0 Phase 8 before Phase 9's WorldGenerator integration; v3.1 Phase 15's isolated test-room tooling before the still-blocked Phase 16 WorldGenerator integration) — the same "isolate and validate before touching the highest-fan-in file" discipline applies here.

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 3 (SAMURAI):** Exact real-device parry-window millisecond tuning is unverified (MEDIUM-LOW confidence in STACK.md) — needs on-device Android measurement, not ported from mouse/keyboard timing.
- **Phase 4.5 (WorldGenerator exemption):** Genuinely undiscussed design space (Phase 16's own deferred item) — spawn-architecture decision (chain-slot replacement vs. branch portal) needs explicit resolution before implementation.
- **Phase 6 (NOVA):** Control-scheme choice (toggle-swap vs. split-zone drag vs. dual-stick) is a playtesting question, not solved by this research pass — flag for an early playable-prototype pass before committing.
- **Phase 7 (Boss Rush):** Endless boss-only loop design (fork WorldGenerator vs. parallel generator) is the highest-risk net-new architecture piece in the milestone — needs its own focused research/design pass.

Phases with standard patterns (skip research-phase):
- **Phase 1 (shared infra):** Well-documented — PlayerPrefs, EnhancedTouch, Pointer.current are all standard, verified-stable Input System APIs; BossUnlockManager follows the project's own existing FloorManager/ScoreManager static-class convention.
- **Phase 2 (BossEnemyBase):** Direct, mechanical extraction from existing code using the same methodology already applied to EnemyBase — no new unknowns.
- **Phase 4 (DeadEye):** Reuses existing RangedEnemy/hold-release/Fan-shape precedent almost directly — lowest architectural novelty of the 4 mechanics.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | MEDIUM-HIGH | Input System APIs verified against official docs (HIGH); mobile touch-latency/parry-window figures are general HCI literature, not Unity-specific or project-measured (MEDIUM) |
| Features | MEDIUM | Genre-pattern analysis cross-checked against actual codebase (HIGH on architectural claims); no single canonical "mobile dual-control boss" source exists, so several mechanic-specific feel/risk calls are explicitly flagged LOW-MEDIUM pending playtest |
| Architecture | HIGH | 100% codebase-derived — every claim traces to a specific file read in this session; no web research needed since this is a pure internal-architecture question |
| Pitfalls | HIGH | Grounded in direct source inspection of the actual current implementation (not generic Unity/mobile advice); cross-referenced against this project's own planning docs and prior-milestone history |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **SAMURAI parry window exact timing:** No project-specific or Unity-specific benchmark exists for touch-input parry latency; must be measured on an actual minSdk-25-class low-end Android device during Phase 3, not tuned solely in Editor.
- **MAX's fit into the IPlayerCombatModule interface:** "Movement IS the attack" may not cleanly fit the hold-slowmo-release-resolve shape the interface assumes; Architecture research explicitly flags this as the single highest-uncertainty item and defers a final answer to Phase 5 planning.
- **NOVA's control scheme and orb-targetability:** Both are explicitly unresolved design questions (not architecture questions) across FEATURES.md, ARCHITECTURE.md, and STACK.md — all three recommend a default (toggle-swap control; single-authority targeting) but flag it for explicit confirmation, not silent adoption, during Phase 6 planning.
- **The endless boss-only floor-loop design for Boss Rush mode (fork vs. parallel generator):** Entirely unspecified at the research stage — this is the same class of decision that stalled the prior v3.1 milestone (Phase 15/16) and should get a dedicated research/design pass before Phase 7 implementation begins.
- **Whether unlock persistence must survive app-restart, not just within-session death/restart:** STORY.md's framing implies real save-game persistence, but this is not explicitly confirmed against PROJECT.md's stated scope — recommend confirming this as a written decision during Phase 1 planning rather than defaulting silently.

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection across all 4 research passes: Assets/Scripts/Player/CombatController.cs, PlayerController.cs, InputManager.cs, ChronoGaugeController.cs, RollController.cs, InvincibilityHandler.cs, RangeDisplay.cs; Assets/Scripts/Enemy/BossEnemy.cs, EnemyBase.cs, IEnemy.cs, ISpawnGatable.cs, MeleeEnemy.cs; Assets/Scripts/World/WorldGenerator.cs, FloorManager.cs, ScoreManager.cs, GameBootstrapper.cs; Assets/Scripts/UI/AttackSelectController.cs, AttackTypeSelector.cs, MainMenuController.cs, DeathScreenController.cs; Assets/InputSystem_Actions.inputactions; Packages/manifest.json
- .planning/PROJECT.md, .planning/phases/16-boss-room-lifecycle/16-CONTEXT.md, .planning/phases/15-fsm/15-06-PLAN.md, STORY.md — milestone scope, deferred Phase 16 gaps, prior-milestone history
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Touch.html — EnhancedTouch multi-touch API
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/OnScreen.html — OnScreenStick/OnScreenButton
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html — InputSystemUIInputModule multi-pointer handling

### Secondary (MEDIUM confidence)
- General mobile touch-latency figures (50-200ms commercial touchscreen latency, ~69-96ms tap-perception JND) — general HCI/mobile literature, not Unity-specific
- Genre-precedent analysis (Sekiro/Cuphead parry, Resident Evil/Enter the Gungeon ammo tension, Dark Souls/Zelda environmental-bait bosses, R-Type Force-pod dual-entity, Hades/Risk of Rain single-loadout roguelike, Mega Man/Cuphead boss-rush structure) — training-data genre-pattern knowledge, cross-checked against codebase constraints
- Touch Controls for Mobile Games (Cursa), Display Response Time / Parry Timing (KTC Play) — corroborate touch-latency and parry-window-widening recommendations

### Tertiary (LOW confidence)
- Endless/roguelike mode structure comparison (single-pass WebSearch synthesis, not cross-verified against a canonical source) — informs the fixed-endpoint-vs-endless framing between the two game modes only

---
*Research completed: 2026-07-20*
*Ready for roadmap: yes*
