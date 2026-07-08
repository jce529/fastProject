# Feature Research

**Domain:** 2D action platformer / roguelite climber — boss encounter design + spawn VFX + audio polish
**Researched:** 2026-07-08
**Confidence:** MEDIUM (genre conventions well-documented via Dead Cells/Rogue Legacy/Downwell-style references; specifics verified only via WebSearch, not Context7/official docs — no official "boss design spec" exists for this genre)

## Feature Landscape

### Table Stakes (Users Expect These)

Features players assume exist once a "boss room" is announced. Missing these makes the boss feel like a reskinned regular enemy, not a boss.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Telegraphed attacks (clear visual/audio cue before each attack lands) | Genre standard (Dead Cells: attacks show a red line/wind-up before executing) — players expect to react, not guess | LOW-MEDIUM | Fast already does this for regular enemies (melee wind-up animation, ranged aim line). Boss needs the same discipline, just with a more distinct/exaggerated tell since stakes are higher. |
| Dedicated arena / solo fight (no regular enemies mixed in) | Already specified as a milestone requirement; also genre-standard — mixing trash mobs into a boss fight muddies readability | LOW-MEDIUM | Requires a boss-specific room prefab distinct from the 6 Complex_Room variants; must guarantee no other Spawner_* points activate in that room. |
| Clear defeat feedback (visual + audio + score payoff) | Boss kill needs to feel more significant than a regular one-shot kill, or the "boss" label is meaningless | LOW | Reuse existing enemy death particle/fade pattern, extend with a bigger flourish (longer particle burst, camera shake, distinct SFX) + ScoreManager bonus. |
| Readable "danger state" vs "opening state" | Players must be able to tell when the boss is attacking (avoid) vs vulnerable (attack) — core to any boss loop | MEDIUM | This is the load-bearing design decision for this milestone — see Dependency Notes below for how it interacts with the game's one-shot-kill rule. |
| Boss doesn't ambush the player | Player needs at least a beat of warning before the fight starts (entering room != instant attack) | LOW | Satisfied by the spawn-in VFX itself if it has any wind-up (portal appears, then boss emerges) — see spawn VFX section. |

### Differentiators (Competitive Advantage)

Features that make the FIRST boss room memorable without adding disproportionate scope.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Invulnerable-except-during-opening pattern (boss attacks in a way that covers most of the arena, then exposes a brief one-shot-killable window) | Preserves the game's core one-shot-kill value (both directions) instead of bolting on an HP bar — makes the boss "hard to reach" rather than "hard to kill," which is a natural extension of the existing dash-kill mechanic | MEDIUM | Recommended approach — see Dependency Notes. Reuses the existing "closest enemy in range" dash-target logic; the opening is just a timing gate on when the boss becomes targetable. |
| Boss intro beat (brief camera pan/zoom or pause on room entry before the fight starts) | Signals "this is different" without dialogue/cutscene overhead; matches existing polish level (camera shake, portal SpriteMask reveal already exist) | LOW-MEDIUM | Can piggyback on the existing FloorTransitionEffect/camera-lock pattern used for portal transitions — same toolkit, new trigger. |
| Distinct boss spawn stinger (2-3 second audio+visual flourish, not full music) | Cheap way to make the boss feel like an "event" without a music system | LOW | A single one-shot SFX + the spawn VFX (see below) covers this; no adaptive music needed. |
| Unique arena silhouette/layout (not just a reskinned Complex_Room) | Reinforces "this room is special" spatially, reduces reliance on UI/text to signal boss presence | MEDIUM-HIGH | Milestone already requires a dedicated arena; complexity is in tilemap/prefab authoring, not code. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that look like standard "boss fight" requirements but conflict with this project's scope or core value.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|------------------|-------------|
| Multi-hit boss with HP bar | Nearly every action-game boss reference (Dead Cells, Rogue Legacy) uses HP bars and damage-over-time fights | Directly conflicts with the validated core rule "원샷원킬 (플레이어·적 모두)" — introducing a damage/health system for one enemy type means building a parallel combat model (HP tracking, damage numbers, hit-reduction UI) that doesn't exist anywhere else in the codebase, for a single fight. High cost, validates a different mechanic than the one this prototype exists to test. | Invulnerable-except-during-opening pattern (see Differentiators) — boss is still one-shot-killable, just gated by timing instead of HP. |
| Multi-phase fights (attack set changes at HP thresholds) | Standard in Dead Cells/Rogue Legacy-style bosses to keep long fights fresh | Requires the HP system above as a prerequisite (phases are usually triggered by HP%), plus extra animation/attack authoring for a single fight in a prototype milestone explicitly scoped to "1 boss type" | Single continuous attack-pattern loop (e.g. cycle through 2-3 telegraphed attacks) is enough to validate the boss-room concept; save phase transitions for a future milestone once the base loop is proven fun. |
| Adaptive/dynamic music system (intensity layers, boss theme swapping in/out) | "Games have boss music" is a strong genre expectation | No music system exists in the codebase at all today (confirmed: zero audio currently implemented) — building an adaptive music layer before even having basic SFX is solving a problem two steps ahead of where the project is | Short audio stinger on boss spawn + existing ambient (if any); defer full music to a dedicated audio milestone. |
| Full boss roster / boss variety this milestone | "Why build a boss framework for just one boss?" | Milestone explicitly scopes to 1 boss type; over-building the framework (e.g. data-driven attack-pattern authoring tools) before a second boss exists to validate the abstraction risks guessing wrong about what needs to be generic | Build the one boss with a lightweight FSM (same pattern as existing MeleeEnemy/RangedEnemy), but keep boss-specific logic in its own script/prefab rather than forcing a generic "BossBase" abstraction prematurely — extract shared code only once a second boss is actually being built. |
| Boss dialogue / voice lines / name-card cutscene | Common in narrative-driven boss intros | Adds VO/localization/UI scope with no connection to validating the core combat feel (the 6 prototype validation goals in PROJECT.md are all about combat feel, not narrative) | Visual-only intro beat (camera + spawn VFX + stinger SFX) carries the "this is a boss" signal without dialogue systems. |
| Frame-perfect hit-stop/combo timing systems | Common "juice" advice for action games | This game has no combo system (one dash-kill per engagement, explicitly out of scope: "콤보 시스템... 핵심 검증과 무관") — a combo-oriented hit-stop economy is solving for a mechanic that doesn't exist here | Simple, fixed-duration hit-stop (a few frames) synced to a single hit-impact SFX on both regular hits and boss hits — already partially implied by existing HitSparkBuilder/camera shake, just needs an audio hook. |

## Feature Dependencies

```
Boss Room Feature
    |--requires--> Boss arena room prefab (new, dedicated layout, no regular enemy spawn points)
    |--requires--> Boss enemy behavior (FSM extending existing MeleeEnemy/RangedEnemy pattern)
    |--requires--> Probabilistic room-type spawn logic (extends ExitPortal/WorldGenerator pattern: chance-based, max 1 concurrent)
    |--requires--> Enemy spawn-in VFX (shared with regular enemy spawn-in, see below)
    `--enhances--> ScoreManager (adds a one-off bonus-scoring hook on boss defeat)

Invulnerable-except-during-opening pattern (boss combat design)
    `--requires--> Existing dash-target/closest-enemy-in-range logic (CombatController) -- extends it with a
                   targetable/untargetable flag driven by boss attack-state, not a new damage system

Enemy Spawn-in VFX (regular + boss)
    `--requires--> Generalizing the existing player-only portal entry effect (PortalEffectBuilder / SpriteMask
                   reveal) into a reusable "spawn at point" component -- currently coupled to player-only usage

Audio Polish (portal / hit / death / boss spawn SFX)
    |--requires--> New AudioManager / SFX-pooling infrastructure (currently zero audio in codebase -- foundational,
    |              built from scratch, not an extension of an existing system)
    `--enhances--> Portal transition, Hit impact, Enemy death, Boss spawn/intro (hooks into existing VFX trigger points)

Boss intro camera beat
    `--enhances--> Boss Room Feature (uses existing FloorTransitionEffect/camera-lock toolkit, new trigger point)
```

### Dependency Notes

- **Boss Room requires probabilistic room-type spawn logic that extends the ExitPortal pattern:** ExitPortal already solves "spawn something with a probability, cap at 1 concurrent, inside a room" for floor transitions. The boss room should reuse this exact pattern (probability roll during room generation, max-1-concurrent guard) rather than invent a new spawn-gating mechanism. The dependency is on WorldGenerator's room-selection logic being extended to recognize a "boss room" room-type, separate from the existing Complex_Room random pool.
- **Invulnerable-except-during-opening pattern requires the existing dash-target logic, not a new damage system:** This is the key design decision this milestone must make explicit. The project's validated core rule is one-shot-kill for both player and enemies, system-wide, across all of v1.0-v3.0. A genre-standard HP-bar boss would break that rule for exactly one enemy type. The lower-cost, value-consistent alternative is to keep the boss one-shot-killable but gate *when* it can be targeted/dashed-to (e.g., only during a telegraphed "opening" after an attack sequence, similar to how "적 없으면 헛베기" already gates on range/proximity). This should be surfaced to the user as an explicit decision before phase planning, not assumed silently.
- **Enemy Spawn VFX enhances Boss Room:** the milestone asks for enemy spawn-in VFX as a general feature (applies to regular enemies too), and boss entrance is the highest-value place to use it dramatically (paired with the intro camera beat). Building it as a generic "spawn at point" component (rather than boss-only) means regular enemy spawning gets the same polish for near-zero extra cost.
- **Audio Polish has no existing system to extend:** unlike VFX (which already has PortalEffectBuilder/HitSparkBuilder to build on), there is currently no AudioManager, no AudioSource pooling, and no SFX assets anywhere in the project. This work is foundational and should be scoped as its own early step before hooking sounds into portal/hit/death/boss-spawn trigger points -- not bundled in as an afterthought during boss VFX work.
- **Boss intro camera beat enhances but does not block Boss Room:** the boss room MVP works without a camera flourish (arena + telegraph + defeat feedback are enough to validate the concept); the intro beat is additive polish using tooling that already exists (FloorTransitionEffect-style camera lock).

## MVP Definition

### Launch With (v1 -- this milestone, v3.1)

Minimum viable product -- what's needed to validate "does a boss room work in this game."

- [ ] Boss room: probabilistic spawn reusing the ExitPortal pattern (chance roll, max 1 concurrent, dedicated arena room) -- core ask of the milestone
- [ ] One boss type with a small telegraphed attack-pattern loop (2-3 attacks cycling) and an invulnerable-except-during-opening targeting gate (preserves one-shot-kill core value) -- essential to make the fight readable and consistent with existing combat feel
- [ ] Solo fight guarantee (no regular enemy spawns active in the boss room) -- required so the fight is legible as "the boss encounter," not a mob fight
- [ ] Score bonus on boss defeat, hooked into existing ScoreManager -- required by milestone, low cost
- [ ] Enemy spawn-in VFX for both regular enemies and the boss, generalized from the existing player portal-entry effect -- explicit milestone ask, and needed so the boss's arrival doesn't feel like it silently "was already there"
- [ ] Basic AudioManager/SFX-pooling infrastructure -- prerequisite for any sound at all; currently zero audio exists
- [ ] SFX for: portal transition, hit impact, enemy death, boss spawn (each with basic pitch/volume randomization to avoid repetition fatigue) -- explicit milestone ask, addresses "currently silent" gap

### Add After Validation (v1.x -- future milestone, if boss room proves fun)

Features to add once the core boss-room loop is confirmed engaging in playtesting.

- [ ] Second and third boss types (validate the extensibility of the boss framework built this milestone) -- triggered by: v3.1 boss room testing well and roadmap wanting more content variety
- [ ] Arena environmental hazards beyond the boss's own attacks (e.g. edge hazards, moving terrain) -- triggered by: base fight feeling "flat" without extra spatial pressure
- [ ] Boss intro camera beat (pan/zoom/pause before fight starts) -- nice-to-have polish, defer if time-constrained since arena+telegraph+defeat feedback already validate the core concept

### Future Consideration (v2+)

Features to defer until the core combat/boss concept has more validation data.

- [ ] Multi-phase boss fights (attack-set changes at thresholds) -- defer until/unless the invulnerable-except-during-opening pattern is validated as fun; phases assume an HP-based system this project doesn't have
- [ ] Full music system (ambient + boss theme + adaptive layers) -- defer to a dedicated audio milestone; this milestone is scoped to SFX polish only
- [ ] Boss dialogue/name-card intro -- defer indefinitely; no narrative validation goal in PROJECT.md
- [ ] Data-driven boss-attack authoring tools (ScriptableObject-based pattern editor) -- defer until a second/third boss actually exists to prove the abstraction is worth building

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|----------------------|----------|
| Boss room probabilistic spawn (reuse ExitPortal pattern) | HIGH | MEDIUM | P1 |
| Boss telegraph + invulnerable-except-opening attack pattern | HIGH | MEDIUM | P1 |
| Solo-fight guarantee (no mob mixing) | HIGH | LOW | P1 |
| Score bonus on boss defeat | MEDIUM | LOW | P1 |
| Enemy spawn-in VFX (regular + boss, generalized portal effect) | MEDIUM-HIGH | LOW-MEDIUM | P1 |
| Basic AudioManager/SFX pooling infra | HIGH | LOW-MEDIUM | P1 |
| Core SFX set (portal/hit/death/boss-spawn, pitch-varied) | HIGH | LOW | P1 |
| Boss intro camera beat | MEDIUM | MEDIUM | P2 |
| Arena environmental hazards | LOW-MEDIUM | HIGH | P3 |
| Second/third boss types | MEDIUM | MEDIUM-HIGH | P3 (future milestone) |
| Multi-phase boss fights | LOW (conflicts with core value) | HIGH | Anti-feature / P3+ |
| Adaptive music system | LOW (no system exists yet) | HIGH | Anti-feature / P3+ |
| Boss HP bar / multi-hit combat | LOW (conflicts with core value) | HIGH | Anti-feature |

**Priority key:**
- P1: Must have for this milestone (v3.1)
- P2: Should have, add if time allows within v3.1
- P3: Nice to have, explicitly deferred to a future milestone

## Competitor Feature Analysis

| Feature | Dead Cells | Rogue Legacy | Fast (v3.1 plan) |
|---------|------------|---------------|-------------------|
| Attack telegraphing | Red line/wind-up cues before every boss attack | Boss-specific tells (e.g. charge-up glow) before each attack | Reuse existing melee/ranged telegraph conventions, exaggerated for the boss |
| Damage model | Multi-hit HP bar, damage numbers | Multi-hit HP bar, RNG-modified stats | One-shot-kill preserved via invulnerable-except-during-opening gating (no HP bar) -- deliberate divergence to stay consistent with core value |
| Arena design | Boss-specific hazards (e.g. water, tentacles reshaping the fight) | Fixed arena per castle, boss interacts with room geometry | Dedicated arena room prefab, hazards deferred to future milestone (arena shape only, no dynamic terrain this pass) |
| Fight structure | Multi-phase, escalating intensity | Single continuous fight with escalating patterns per difficulty | Single continuous attack-pattern loop (no HP-based phases) -- matches "1 boss type" milestone scope |
| Music/audio on boss entry | Full boss theme track | Full boss theme track | Short SFX stinger only -- no music system exists yet, explicitly deferred |

## Sources

- [Boss Battles-How to Design One? (Medium)](https://medium.com/@foster_sawyer2/boss-battles-how-to-design-one-733c788e5494) -- MEDIUM confidence, general design essay
- [Boss Design: How to Make an Unforgettable Boss Battle (GameDesignSkills)](https://gamedesignskills.com/game-design/game-boss-design/) -- MEDIUM confidence, cross-referenced with multiple other sources on telegraphing/arena design
- [Boss Battle Design and Structure (Game Developer / Gamasutra)](https://www.gamedeveloper.com/design/boss-battle-design-and-structure) -- MEDIUM confidence, industry publication
- [Dead Cells Wiki -- Conjunctivius](https://deadcells.fandom.com/wiki/Conjunctivius) -- MEDIUM confidence, specific boss mechanic reference (tentacle/eye pattern, telegraphed beams)
- [Dead Cells -- Wikipedia](https://en.wikipedia.org/wiki/Dead_Cells) -- MEDIUM confidence, general game structure reference
- [Mastering VFX in Unity: Spawning, Collision, and Explosions (Medium)](https://medium.com/@Brian_David/mastering-vfx-in-unity-spawning-collision-and-explosions-efc33791f2e0) -- LOW-MEDIUM confidence, general Unity VFX approach, not project-specific
- [Using State Patterns for Dynamic AI in Unity (Medium)](https://medium.com/@Brian_David/using-state-patterns-for-dynamic-ai-1579e089931d) -- MEDIUM confidence, confirms FSM approach already used in this codebase (MeleeEnemy/RangedEnemy) generalizes to boss design
- [State Machines and Boss Fights (Unibear Studio)](https://www.unibearstudio.com/tutorial/state-machines-and-boss-fights) -- MEDIUM confidence, IBossState pattern reference
- [The 2026 Indie Dev's Roadmap to Game Audio (Tortuga Soundtracks)](https://tortugasoundtracks.com/blogs/the-ultimate-guide-to-game-audio-how-sound-shapes-player-experience/posts/7684069/the-2026-indie-dev-s-roadmap-to-game-audio-strategic-sound-design-for-high-conversion-titles) -- LOW-MEDIUM confidence, marketing-adjacent blog but consistent with general audio-design consensus (bake audio in early, avoid "audio debt")
- [Feel (More Mountains) -- game feel/juice asset docs](https://feel.moremountains.com/) -- MEDIUM confidence, documents standard hit-stop/audio-sync/screenshake patterns used as a reference model (not necessarily to be adopted as a dependency)
- Existing codebase inspection (PROJECT.md) for current state: PortalEffectBuilder, HitSparkBuilder, FloorTransitionEffect, WorldGenerator, ExitPortal, ScoreManager, MeleeEnemy/RangedEnemy FSM -- HIGH confidence (primary source, own repo)

---
*Feature research for: 2D action platformer boss room content + spawn VFX + audio polish (Fast v3.1)*
*Researched: 2026-07-08*
