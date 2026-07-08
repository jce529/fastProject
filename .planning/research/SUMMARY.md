# Project Research Summary

**Project:** Fast (가칭) — v3.1 milestone
**Domain:** Boss room content (extensible framework) + VFX/audio polish for an existing Unity 6 URP 2D mobile action platformer with infinite procedural room generation
**Researched:** 2026-07-08
**Confidence:** HIGH

## Executive Summary

This milestone adds no new technology to the project — it is 100% new C# scripts and Editor-script-authored prefabs layered onto the existing, already-shipped v3.0 architecture (`WorldGenerator`, `IEnemy`/`MeleeEnemy`/`RangedEnemy`, `CombatController`, `ExitPortal`, `ScoreManager`). Three things are being added: (1) a solo boss encounter room, reusing the existing "probabilistic room-slot overlay" pattern already proven by `ExitPortal`; (2) generalized spawn-in VFX for enemies (and the boss), reusing the existing `RuntimeMaskSprite`/`SpriteMask` cosmetic-coroutine pattern already proven by `EnemyDeathEffect`/`FloorTransitionEffect`; and (3) the project's first-ever audio system (zero `AudioSource`/`AudioClip` usage exists today), which needs a small `MonoBehaviour`-singleton `AudioManager` with a pooled `AudioSource` array.

The single most important design decision — flagged consistently across all four research files — is how the boss stays "one-shot-killable" (the game's core, non-negotiable value) without becoming either trivial (dies to the first dash, indistinguishable from a regular enemy) or a genre-standard HP-bar fight (which would break the one-shot-kill rule system-wide for one enemy type). The recommended resolution is an **invulnerable-except-during-a-telegraphed-opening** pattern: the boss's `IsAlive`/one-shot-kill mechanics stay identical to every other enemy, but its *targetability* (via `CombatController.FindNearestEnemyInRange()`) is gated by an attack-pattern-driven vulnerability flag. This requires zero changes to `IEnemy` or `CombatController` — the boss's own coroutine flips whether it is currently a valid target, reusing the exact "skip if `!IsAlive`" check that already exists.

The key risks are almost entirely about **integration correctness with the existing procedural generation and timing systems**, not about the new content itself: (a) the boss room must be excluded from the normal room pool and its own `EnemySpawner` markers must not leak in, or regular enemies silently co-spawn with the boss; (b) `WorldGenerator`'s 2-room lookahead/lookbehind trim can `Destroy()` the boss room mid-fight if the player's position drifts outside the window during a longer encounter; (c) the 60-second `FloorTimer` keeps ticking through the boss fight with no exemption today and can kill the player mid-encounter; (d) any new spawn-in VFX must hook into `EnemySpawner.Activate()`, never `Awake()`/`OnEnable()`, or it fires off-screen/twice; and (e) all new audio timing code must follow the project's `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` convention (already enforced everywhere else) or SFX will visibly desync during the game's core slow-motion/hit-freeze loop. All five are addressable with small, additive changes; none require restructuring existing systems.

## Key Findings

### Recommended Stack

No new UPM packages are needed — every capability required (`AudioSource`/`AudioMixer`, `ParticleSystem`, `Animator`, `SpriteMask`) is already installed and used elsewhere in the codebase. The work is entirely new C# + Editor-script authoring following established project conventions (menu-item prefab builders, coroutine-driven FSMs, self-attaching cosmetic components).

**Core technologies:**
- `AudioSource` + `AudioMixer` (built into `com.unity.modules.audio`, already installed) — sufficient for ~5-8 one-shot SFX cues; audio middleware (FMOD/Wwise) is explicitly rejected as overkill for a prototype
- `BossEnemyBase`/`BossEnemy` as a plain `IEnemy`-implementing class (inheritance, not ScriptableObject-driven data authoring) — matches the existing `MeleeEnemy`/`RangedEnemy` style, right-sized for "1 boss now, extensible later"
- Coroutine + enum-phase FSM (same idiom as `MeleeEnemy.TelegraphAndAttack()`) — boss attack-pattern sequencing, already timeScale-safe via `WaitForSecondsRealtime`
- `RuntimeMaskSprite` + hand-rolled coroutine scale/mask animation (same pattern as `FloorTransitionEffect`) — reused for a new generalized `EnemySpawnEffect`
- `UnityEngine.Pool.ObjectPool<T>` — explicitly NOT needed at current scale; defer unless profiling shows GC pressure

### Expected Features

Genre convention (Dead Cells/Rogue Legacy-style boss design) strongly implies HP bars and multi-phase fights, but both are explicitly flagged as **anti-features** for this project because they conflict with the validated one-shot-kill core value. The MVP is scoped tightly around validating "does a boss room work in this game," not building a full boss roster.

**Must have (table stakes):**
- Telegraphed attacks (clear visual/audio cue before each attack lands), exaggerated versus regular enemies
- Dedicated solo arena (no regular enemies mixed in)
- Clear defeat feedback (visual + audio + score payoff), bigger than a regular kill
- Readable danger-state vs. opening-state signaling (load-bearing for the invulnerable-except-opening pattern)
- Basic AudioManager/SFX infrastructure + core SFX set (portal, hit, death, boss spawn) — currently zero audio exists

**Should have (differentiators):**
- Invulnerable-except-during-opening pattern (preserves one-shot-kill without an HP bar)
- Boss spawn stinger (audio+visual flourish) using the generalized spawn-in VFX
- Unique arena silhouette (dedicated room prefab, not a reskinned Complex_Room)

**Defer (v2+):**
- Multi-phase boss fights (assumes an HP system this project deliberately doesn't have)
- Full adaptive music system (no music system exists at all yet; scope this milestone to SFX only)
- Boss dialogue/name-card intro (no narrative validation goal)
- Data-driven ScriptableObject attack-pattern authoring tools (defer until a 2nd/3rd boss actually exists)
- Second/third boss types, arena environmental hazards, boss intro camera beat (P2/P3, nice-to-have polish)

### Architecture Approach

The boss room and its supporting systems integrate as parallel siblings to existing patterns rather than new architecture: the boss room is a new dedicated prefab selected via a **separate probabilistic roll** mirroring `ExitPortal`'s `_exitSpawnChance`/`_maxExitsActive` gating (never appended to the flat `_roomPrefabs` pool, which would make it appear ~1-in-7 rooms with no floor gate). `BossEnemy` is a new `IEnemy` sibling to `MeleeEnemy`/`RangedEnemy` — `IEnemy`'s 3-member contract is left untouched, and `CombatController` needs zero changes to dash-kill a boss. Audio is added as a `MonoBehaviour` singleton (`AudioManager.Instance` + static `PlaySfx()` wrapper) rather than a pure static class, since it must own real `AudioSource` components — this deliberately deviates from the project's existing pure-static-class manager pattern (`ScoreManager`/`FloorTimer`) in the same way `WorldGenerator` already does for the same reason.

**Major components:**
1. `AudioManager` (new, `MonoBehaviour` singleton, pooled `AudioSource[]`) — single call-in point for all new sound, `DontDestroyOnLoad` across the 3-scene flow
2. `BossEnemy : MonoBehaviour, IEnemy` (new FSM sibling) — owns its own attack-pattern states, vulnerability gating, and calls `ScoreManager.AddBossKillBonus()` itself from `OnDashHit()`
3. `EnemySpawnEffect` (new, mirrors `EnemyDeathEffect`) — reusable spawn-in VFX for regular enemies and boss, wired into `EnemySpawner.Activate()` (never `Awake()`/`OnEnable()`)
4. `WorldGenerator` extension (`SelectRoomPrefab`, `TrySpawnBoss`, `_bossRoomPrefab`/`_bossSpawnChance`/`_maxBossRoomsActive`/`_bossMinFloor`) — highest blast-radius change, done last per the suggested build order

**Suggested build order (dependency-driven):** `AudioManager` → sound/timing polish on existing components → `EnemySpawnEffect` (validated on existing enemies first) → `BossEnemy` FSM (standalone, tested in isolation) → boss room prefab authoring (parallel with above) → `WorldGenerator` integration (last, highest risk).

### Critical Pitfalls

1. **Boss room silently gets regular enemies via the shared spawn pipeline** — the boss prefab will likely be duplicated from an existing Complex_Room and may carry over leftover `EnemySpawner` markers; strip them and add a belt-and-suspenders type-check gate in `WorldGenerator`, and never add the boss prefab to the flat `_roomPrefabs` pool.
2. **WorldGenerator's lookahead/lookbehind recycle can `Destroy()` the boss room mid-fight** if the player's position drifts outside the trim window during a longer-than-normal encounter — freeze chain trimming (`_bossEncounterActive` guard) for the duration of an unresolved boss fight.
3. **Naive `IEnemy` implementation trivializes the boss** — implementing `OnDashHit()` the same unconditional way as `MeleeEnemy` makes the "boss" a reskinned regular enemy; gate targetability (not literal one-shot-kill semantics) behind a telegraphed vulnerability window instead.
4. **`FloorTimer`'s 60-second countdown keeps ticking through the boss fight** with no exemption today, and can kill the player mid-encounter for reasons unrelated to the fight; this must be an explicit decision (pause/extend/exempt), not silently skipped.
5. **Spawn-in VFX fires in `Awake()`/`OnEnable()`** — off-screen at lookahead/standby-room instantiation time, or twice (once on instantiate, once on `Activate()`) — must be wired into `EnemySpawner.Activate()` exclusively, paired with a new `SpawningIn` FSM state that gates detection/targetability until the VFX completes.
6. **Audio timing breaks under slow-motion/hit-freeze** if built on `Time.deltaTime`/`WaitForSeconds` instead of the project's existing `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` convention — easy to violate on a brand-new subsystem with no existing audio code to copy from.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Audio Foundation + Sound Polish Pass
**Rationale:** Zero dependencies, foundational for every other new sound hook (spawn VFX, boss cues); lowest risk since it's additive to already-stable, small components (`FloorTransitionEffect`, `CombatController`, `EnemyDeathEffect`).
**Delivers:** `AudioManager` singleton + pool, SFX for portal transition/hit impact/enemy death, all using `Time.unscaledDeltaTime` conventions from day one.
**Addresses:** "Basic AudioManager/SFX infrastructure" and "core SFX set" from FEATURES.md (both P1).
**Avoids:** Pitfall 6 (`Time.deltaTime`-based audio desync) and Pitfall 9 (rapid-kill SFX clipping/GC churn) — build the pool and unscaled-time convention correctly from the start rather than retrofitting.

### Phase 2: Enemy Spawn-in VFX (generalized)
**Rationale:** Validates the spawn-VFX-visibility tradeoff and the `Activate()`-seam wiring against the two *existing* enemy types before a third (boss) is introduced; depends only on Phase 1 for its spawn sound.
**Delivers:** `EnemySpawnEffect` component (mirrors `EnemyDeathEffect`, reuses `RuntimeMaskSprite`), wired into `EnemySpawner.Activate()`, plus a `SpawningIn` FSM state gating detection/targetability.
**Uses:** `RuntimeMaskSprite`/`SpriteMask` pattern from STACK.md; `AudioManager.PlaySfx()` from Phase 1.
**Implements:** "Cosmetic Layer" component from ARCHITECTURE.md.
**Addresses:** "Enemy spawn-in VFX (regular + boss, generalized portal effect)" from FEATURES.md (P1).
**Avoids:** Pitfall 5/6/7 (VFX firing off-screen/twice, enemy lethal before VFX completes).

### Phase 3: Boss Enemy FSM + Vulnerability Design
**Rationale:** Standalone and testable in an isolated debug scene (drop into an empty scene with a player, confirm dash-kill/score/death/spawn VFX all fire) before touching `WorldGenerator` at all — the highest-value design decision (invulnerable-except-opening) should be locked and validated in isolation first.
**Delivers:** `BossEnemy : MonoBehaviour, IEnemy` with a telegraph → windup → hitbox → recover attack-pattern loop (2-3 attacks cycling) and a vulnerability-window targeting gate; `ScoreManager.AddBossKillBonus()`.
**Addresses:** "One boss type with a small telegraphed attack-pattern loop and an invulnerable-except-during-opening targeting gate" from FEATURES.md MVP (P1).
**Avoids:** Pitfall 3 (naive one-shot kill trivializes the boss) — this is the phase's central risk and must be explicitly decided, not defaulted.

### Phase 4: Boss Room Content + Lifecycle Gating
**Rationale:** Can run in parallel with Phase 3 once the boss's collider/silhouette dimensions are known; must solve room-generation integration (solo-fight guarantee, chain-trim safety, timer interaction, entry-triggered activation) before wiring into `WorldGenerator`, since these are structural, not balance, concerns.
**Delivers:** `Room_Boss` prefab (dedicated arena, `RoomConnector`/`CameraBound`/`ExitSpawnPoint`, single `BossSpawner`, zero `EnemySpawner`), an entry-triggered boss activation (mirrors `ExitPortal.OnTriggerEnter2D`), a `_bossEncounterActive` chain-trim guard, and an explicit `FloorTimer` pause/extend/exempt decision.
**Addresses:** "Boss room: probabilistic spawn... Solo fight guarantee" from FEATURES.md MVP (P1).
**Avoids:** Pitfall 1 (regular enemies leak in), Pitfall 2 (WorldGenerator destroys boss room mid-fight), Pitfall 4 (FloorTimer kills player mid-fight), Pitfall 5 (boss activates before player enters).

### Phase 5: WorldGenerator Integration (Boss Room Spawn Gating)
**Rationale:** Highest blast-radius change to the most complex existing script; only makes sense once `BossEnemy` (Phase 3) and the boss room prefab (Phase 4) both already work standalone — done last per ARCHITECTURE.md's explicit build-order recommendation.
**Delivers:** `SelectRoomPrefab(floor)` refactor centralizing the 4 duplicated room-pick call sites, `_bossRoomPrefab`/`_bossSpawnChance`/`_maxBossRoomsActive`/`_bossMinFloor` fields, `TrySpawnBoss` wired at the same 4 sites as `TrySpawnEnemies`, `_activeBossCount` bookkeeping matching the existing `_activeExitCount` pattern.
**Delivers:** Full end-to-end boss encounter reachable through normal floor traversal.

### Phase Ordering Rationale

- Audio comes first because every later phase (spawn VFX, boss cues) has an inbound dependency on it, and it is the lowest-risk, most isolated piece of new work.
- Spawn VFX is validated on the two *existing* enemy types before the boss exists, so the `Activate()`-seam wiring and `SpawningIn` FSM state are proven independently of boss complexity.
- Boss FSM/vulnerability design and boss room content can run in parallel (different files/prefabs, no shared dependency) but both must complete before `WorldGenerator` integration, which is deliberately last because it is the highest blast-radius change to the most complex existing system (`WorldGenerator`).
- This ordering directly avoids the pitfalls that stem from touching `WorldGenerator` before the pieces it wires together are independently proven (Pitfalls 1, 2, 4, 5 are all `WorldGenerator`-adjacent lifecycle issues that are cheaper to design correctly upfront than retrofit).

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 3 (Boss FSM + Vulnerability Design):** The invulnerable-except-opening mechanic is a novel design decision for this codebase with no existing precedent to copy — worth a focused design pass even though the underlying code pattern (targetability gate reusing the `!IsAlive` skip check) is simple.
- **Phase 5 (WorldGenerator Integration):** Touches the most complex, highest-risk existing script; the `_activeBossCount`/chain-trim-guard interaction should be re-verified against the current `WorldGenerator.cs` at implementation time since it's easy to miss one of the 3+ places `_activeExitCount`-equivalent bookkeeping must be mirrored.

Phases with standard patterns (skip research-phase):
- **Phase 1 (Audio Foundation):** Well-documented Unity built-in API (`AudioSource`/`AudioMixer`/pooling), directly analogous to the existing `WorldGenerator.Instance` singleton pattern already in the codebase.
- **Phase 2 (Spawn VFX):** Directly mirrors the already-implemented `EnemyDeathEffect`/`FloorTransitionEffect` pattern; no new technique needed.
- **Phase 4 (Boss Room Content):** Directly mirrors the already-implemented `ExitPortal` probabilistic-overlay pattern and existing Complex_Room prefab authoring convention.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Built entirely on direct inspection of the existing codebase; zero new dependencies to evaluate, so uncertainty is minimal. |
| Features | MEDIUM | Genre conventions (Dead Cells/Rogue Legacy) are well-documented via WebSearch, but no official "boss design spec" exists for this specific genre/scope combination — the anti-feature analysis (HP bar rejection) is a project-specific inference, not externally validated. |
| Architecture | HIGH | All claims verified directly against current source in this repo; corrected a factual error in the original milestone framing (Editor-only builders vs. runtime components) during research. |
| Pitfalls | HIGH for codebase-specific pitfalls (verified by direct source reading); MEDIUM for general Unity audio/timeScale claims (verified via WebSearch/community discussion, no official Unity doc directly addresses `AudioSource.pitch` + `Time.timeScale` interaction). |

**Overall confidence:** HIGH

### Gaps to Address

- **FloorTimer × boss room interaction is an unresolved design decision, not just an implementation detail** — whether the boss room pauses, extends, or is exempt from the 60-second countdown must be decided explicitly (documented as a Key Decision) before Phase 4 implementation, not left to be discovered during playtesting.
- **Whether SFX should pitch-shift with `Time.timeScale` during slow-mo/hit-freeze** is an open aesthetic decision (nothing does this by default in Unity) — should be decided during Phase 1 so the `AudioManager.PlaySfx()` API shape is right the first time rather than needing every call site touched later.
- **Whether the boss also one-shot-kills the player identically to regular enemies** (vs. needing longer/more readable telegraph timing given added attack complexity) is flagged in PITFALLS.md as a decision that must not default silently — surface this explicitly during Phase 3 planning.
- Feature research confidence is MEDIUM (no official boss-design spec for this genre) — the anti-feature list (HP bar, multi-phase, adaptive music) should be treated as strong recommendations grounded in this project's specific core-value constraint, not universal genre truths, and can be revisited if playtesting data contradicts them.

## Sources

### Primary (HIGH confidence)
- Direct inspection of `Assets/Scripts/World/{WorldGenerator,ExitPortal,EnemySpawner,FloorTimer,FloorManager,RoomConnector,FloorTransitionEffect,ScoreManager,RuntimeMaskSprite,GameBootstrapper}.cs`
- Direct inspection of `Assets/Scripts/Enemy/{IEnemy,MeleeEnemy,RangedEnemy,EnemyDeathEffect}.cs`, `Assets/Scripts/Player/{CombatController,PlayerController,InvincibilityHandler}.cs`, `Assets/Scripts/Camera/CameraFollow.cs`, `Assets/Scripts/Room/RoomClearCondition.cs`
- Direct inspection of `Assets/Editor/{PortalEffectBuilder,HitSparkBuilder}.cs` (confirmed Editor-only, not runtime — corrected an error in the original milestone framing)
- Direct inspection of `Packages/manifest.json`/`packages-lock.json` (confirmed no new UPM packages needed)
- `.planning/PROJECT.md` (milestone requirements, decision log, Out of Scope list)

### Secondary (MEDIUM confidence)
- Unity official docs (`docs.unity3d.com/Manual/class-AudioSource.html`) and WebSearch on Unity 6 2D mobile audio best practice (AudioMixer as baseline before middleware)
- Unity Discussions threads on `PlayOneShot` performance/pooling and `AudioSource.pitch` vs. `Time.timeScale` interaction
- Boss design references: Dead Cells Wiki (Conjunctivius), GameDesignSkills, Game Developer/Gamasutra boss battle design articles
- Unity ScriptableObject boss attack-pattern FSM authoring pattern search (confirms SO-Strategy pattern is a recognized but premature alternative for this milestone's scope)

### Tertiary (LOW confidence)
- NeoGAF community discussion cited only as design grounding for the Titan Souls one-hit-kill-both-ways precedent (Pitfall 3) — illustrative, not authoritative
- Tortuga Soundtracks blog on 2026 indie audio strategy — marketing-adjacent, used only for general "bake audio in early" consensus framing

---
*Research completed: 2026-07-08*
*Ready for roadmap: yes*
