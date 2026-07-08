# Domain Pitfalls: Boss Room + VFX/Audio Polish (Fast v3.1)

**Project:** Fast (가칭)
**Domain:** Adding boss room content, spawn-in VFX, and a first-ever audio system to an existing Unity 6 LTS / URP 2D mobile platformer with infinite procedural room generation, slow-motion combat, and one-shot-kill balance
**Researched:** 2026-07-08
**Confidence:** HIGH for codebase-specific pitfalls (verified by directly reading `WorldGenerator.cs`, `ExitPortal.cs`, `EnemySpawner.cs`, `CombatController.cs`, `MeleeEnemy.cs`, `RangedEnemy.cs`, `EnemyDeathEffect.cs`, `FloorTimer.cs`, `RoomClearCondition.cs`, `FloorTransitionEffect.cs`, `CameraFollow.cs`, `IEnemy.cs`, `GameBootstrapper.cs`); MEDIUM for general Unity audio/boss-design claims (verified via WebSearch against Unity community discussions/support docs; no Context7 library applies since this is engine-level behavior, not a package API)

> Note: this file supersedes the previous (pre-v1.0, 2026-05-27) generic Unity/mobile pitfalls research. Those pitfalls (timeScale/physics compensation, i-frame timing, dash tunneling, camera jitter, GC in Update, IL2CPP stripping, on-screen button dead zones, floor-recycle memory leaks) are now **already solved and enforced** per `CLAUDE.md`'s hard-won constraints (`Time.unscaledDeltaTime` everywhere, `Continuous`+`Interpolate` Rigidbody2D, layer-swap invincibility, `WaitForSecondsRealtime` for all timers, pre-allocated `OverlapCircle` buffers). This file focuses exclusively on **new** pitfalls introduced by the v3.1 milestone: boss room, spawn VFX, and first-time audio.

## Critical Pitfalls

### Pitfall 1: Boss room silently gets regular enemies via the shared spawn pipeline

**What goes wrong:**
`WorldGenerator.SpawnNextPair()`/`SpawnPrevPair()`/`Start()` call `TrySpawnExitPortal(room)` and `TrySpawnEnemies(room, floor)` unconditionally on **every** room instantiated from `_roomPrefabs[]`, with zero knowledge of room "type." `TrySpawnEnemies` just walks `room.GetComponentsInChildren<EnemySpawner>(true)` and fills melee/ranged counts per `GetEnemyCount(floor)`. If the boss room prefab is added to `_roomPrefabs` (or spawned through the same code path) and happens to contain any leftover `EnemySpawner` markers — very likely if the boss room is built by duplicating an existing `Complex_Room` prefab, the fastest way to reuse its Tilemap/CameraBound/RoomConnector/ExitSpawnPoint setup — regular melee/ranged enemies will spawn and fight alongside the boss, silently breaking "solo 전투" with no error or warning.

**Why it happens:**
The generation pipeline has no concept of room categories today — `_roomPrefabs` is a flat pool, and `TrySpawnEnemies` gating is purely count-based, not type-based. The boss room will almost certainly be authored by duplicating a Complex_Room prefab, which carries over its `EnemySpawner` children unless manually stripped.

**How to avoid:**
- Give the boss room prefab **zero** `EnemySpawner` components (delete them after duplicating the base prefab), and additionally add an explicit `isBossRoom` flag (e.g. a `BossRoomMarker` MonoBehaviour on the room root) that `WorldGenerator` checks before calling `TrySpawnEnemies` — belt-and-suspenders, so a future accidental leftover `EnemySpawner` is still skipped by code, not just by prefab hygiene.
- The boss room should NOT be selected through the same `_roomPrefabs[Random.Range(...)]` pool as regular rooms — spawn it through its own probability roll (mirroring `TrySpawnExitPortal`'s `_exitSpawnChance`/`_maxExitsActive` pattern) so it can never appear as a random lookahead/lookbehind filler room by accident.

**Warning signs:**
A melee/ranged enemy patrolling in the background during boss-room playtesting; `GetComponentsInChildren<EnemySpawner>(true)` returning non-empty on the boss room prefab in the Inspector.

**Phase to address:**
Boss room core/gating phase — must be solved before any boss AI work; this is structural wiring, not a balance tweak.

---

### Pitfall 2: WorldGenerator's 2-ahead/behind recycle rule can Destroy() the boss room mid-fight

**What goes wrong:**
`WorldGenerator.Update()` trims the chain purely by **player X position relative to room ENT/EXIT connectors** (`UpdatePlayerIndex()` → `RemoveTail()`/`RemoveHead()`), with no awareness of "is the player currently in combat in this room." A boss fight is expected to keep the player inside one room for much longer than a normal room, and unlike normal enemies (which die in one dash-hit and never block the player), a boss fight might involve knockback or the player retreating to dodge a pattern — which can push `_playerCurrentIndex` outside the `_lookbehindCount`/`_lookaheadCount` window. If that happens, `RemoveTail()`/`RemoveHead()` fires and `Destroy(room)` deletes the boss GameObject **while `CombatController`'s dash coroutine still holds `IEnemy cachedTarget` referencing it**, or while the boss's own attack-pattern coroutine is still running on it.

**Why it happens:**
`RemoveTail`/`RemoveHead` are unconditional `Destroy()` calls, added in v3.0 purely for mobile memory management of *disposable* combat rooms where losing an enemy mid-fight is harmless. A boss encounter breaks that assumption: it has persistent state (phase/timers) that must not evaporate underneath an in-progress coroutine.

**How to avoid:**
- Add an explicit "boss encounter active" guard checked at the top of the trim loops in `Update()`: if the room about to be destroyed contains an unresolved boss, skip the destroy this frame (and don't advance `_playerCurrentIndex` either, to keep the invariant consistent).
- Simplest option: freeze the entire chain-gating pipeline while a boss encounter is active — a single `_bossEncounterActive` boolean checked at the top of `Update()` that skips `SpawnNextPair`/`SpawnPrevPair`/`RemoveTail`/`RemoveHead` for the duration of the fight. This is cheaper and safer than per-room checks, and mirrors how `ForceExitCombatState`/`LockInput` already pause other systems during floor transitions.
- Any boss coroutine (attack patterns, telegraphs) should guard against destruction the same way `MeleeEnemy.TelegraphAndAttack()`/`RangedEnemy.TelegraphAndFire()` already guard with `if (!IsAlive) yield break;`.

**Warning signs:**
`MissingReferenceException` thrown from a boss coroutine or from `CombatController.ExecuteDash()`'s `target.OnDashHit()` call during a boss fight; boss visually vanishing mid-fight.

**Phase to address:**
Boss room lifecycle/gating phase — must be solved before any boss AI/pattern work, since it silently corrupts any pattern coroutine built on top of it.

---

### Pitfall 3: Naive `IEnemy` implementation makes the boss die to one dash, defeating the purpose

**What goes wrong:**
Every existing enemy (`MeleeEnemy`, `RangedEnemy`) implements `IEnemy.OnDashHit()` as an unconditional, instant one-shot kill — exactly how `CombatController.ExecuteDash()` treats **any** `IEnemy` in range. If the boss simply implements `IEnemy` the same way — the path of least resistance, and the only precedent in the codebase — the "boss" is functionally a reskinned `MeleeEnemy` that dies in one dash like everything else, trivializing the entire encounter.

**Why it happens:**
`IEnemy` (`Assets/Scripts/Enemy/IEnemy.cs`) is deliberately minimal — `IsAlive`, `OnDashHit()`, `ClearHighlight()` — with no HP/phase concept anywhere in the codebase (by design: one-shot-kill for both sides is an explicit, unchanged v1.0-v3.0 decision per `PROJECT.md`). Simply satisfying the interface contract compiles and "works" without any deliberate design pass, which is exactly why it's easy to skip.

**How to avoid:**
- Preserve the core value (one-hit-kill dash) but gate *when* the dash is lethal, not *whether* it is — e.g. the boss has a distinct "vulnerable" window (only lethal during a telegraphed opening after a pattern), similar to Titan Souls' single-hit-kill-both-ways design where challenge comes from exposing a weak point rather than surviving repeated hits. Concretely: keep the boss's `IsAlive` semantics identical to other enemies, but gate whether it's a valid target at all during `CombatController.FindNearestEnemyInRange()` — cheapest lever, since `CombatController` already skips any enemy where `!enemy.IsAlive` (reuse that exact mechanism: only flip a boss's effective targetability, not literal death, in vulnerable windows).
- Decide explicitly (document as a Key Decision) whether the boss also one-shot-kills the player identically to regular enemies, or needs longer/more readable telegraph timing given the added complexity — don't let this default silently.
- Since the requirement explicitly asks for "확장 가능한 프레임워크" for future boss types, this is the one place where a light abstraction is justified: a small reusable base (e.g. a `BossController` exposing `SetVulnerable(bool)` checked by a shared `OnDashHit()`/targeting gate) — but keep it to exactly this pattern. Do not build a generic boss-phase-config ScriptableObject system for a single boss; that is over-engineering for this milestone.

**Warning signs:**
Boss dies in under 2 seconds to the very first dash, indistinguishable from a `MeleeEnemy`; no visible strategic difference between fighting the boss and fighting a regular enemy.

**Phase to address:**
Boss AI/pattern design phase — this design decision should be locked before implementation starts, since it changes the shape of the FSM (telegraphed phases vs. instant idle→chase→attack).

---

### Pitfall 4: FloorTimer's 60-second countdown keeps ticking through the boss fight

**What goes wrong:**
`FloorTimer.Tick()` runs unconditionally every frame inside `WorldGenerator.Update()` regardless of room or combat state, and calls `PlayerController.TriggerDeath()` the instant `RemainingSeconds` hits 0 (`Time.unscaledTime`-based, so slow-motion/hit-freeze do not pause it — by design). A solo boss fight (telegraphs, dodge windows, multiple attempts) will plausibly take longer than whatever budget remains from the floor's 60s countdown, since the boss room is entered mid-floor like any other room — not via an EXIT portal, which is the only thing that currently calls `FloorTimer.Reset()`. The player can be killed by the timer *during* the boss encounter, an unrelated system silently ending what's meant to be a self-contained set-piece fight.

**Why it happens:**
`FloorTimer` was designed around "fast escape" tension for the standard room+corridor loop (v3.0's core validation goal), with no concept of an exception room. Nothing in `WorldGenerator`/`FloorTimer` currently pauses or extends the timer for any room type.

**How to avoid:**
- Decide explicitly whether the boss room pauses `FloorTimer` for the fight duration, grants a time bonus/extension on entry, or is deliberately exempt from the countdown. `FloorTimer` is a static class with only `Reset()`/`RemainingSeconds`/`Tick()` — adding `Pause()`/`Resume()` (a `_paused` bool checked at the top of `Tick()`) is a small, additive change, not a rewrite.
- Whatever is chosen, document it as a Key Decision — this cross-system interaction is easy to miss because `FloorTimer` and boss room live in different mental "phases" of the milestone but share runtime state every frame.

**Warning signs:**
Player dies to "시간 초과" mid-boss-fight in playtesting with no visible indication the death was timer-related versus boss-related.

**Phase to address:**
Boss room lifecycle/gating phase — same phase as Pitfall 2, since both concern what "boss room active" should suspend.

---

### Pitfall 5: Boss activates immediately on room instantiation, before the player has "entered" for a solo fight

**What goes wrong:**
`WorldGenerator.SpawnNextPair()`/`SpawnPrevPair()` instantiate rooms **2 lookahead pairs in advance** of the player (`_lookaheadCount = 2`), and `TrySpawnEnemies()` calls `spawner.Spawn(...)` **and immediately** `spawner.Activate()` — regular enemies are already active (patrolling, `Update()`-driven, detection colliders live) the instant the room exists, typically while the player is still 1-2 rooms away and the room is off-screen. If the boss is spawned/activated through the same immediate pattern, a "solo fight" won't actually gate on the player entering the room — the boss's FSM/telegraph timers could start running off-screen, or the fight could already be "in progress" by the time the player's camera catches up, undermining the dramatic "enter room → fight begins" framing implied by "솔로 전투."

**Why it happens:**
The existing spawn pipeline was built entirely around "ambient" enemies that don't need a scripted start (patrol from spawn, chase on detection) — there's no precedent in the codebase for "wait until player crosses a trigger, then start." `RoomClearCondition.cs` exists but only *reacts* to enemies already being dead to *reveal* something afterward — it doesn't gate combat *start*.

**How to avoid:**
- Do not use `EnemySpawner.Spawn()+Activate()` for the boss at all. Instead: instantiate the boss **disabled** (`SetActive(false)`, matching how standby rooms are already handled), and use a separate boss-room-only entry trigger (a `Collider2D` at the room's ENT `RoomConnector`, conceptually the mirror of `ExitPortal`'s trigger) to activate the boss and (optionally) seal the room only once the player physically walks in — the same "player-triggered activation" pattern already proven by `ExitPortal.OnTriggerEnter2D()`.
- Because the boss room still needs to sit in the lookahead window for chain integrity, "instantiated early but inert until entry-triggered" is the correct model — don't try to delay instantiation itself, which would require special-casing the chain-building loop.

**Warning signs:**
Boss visibly moving/attacking before the player's camera has scrolled into the room; boss telegraph or attack triggering with the player nowhere near the arena.

**Phase to address:**
Boss room lifecycle/gating phase.

---

### Pitfall 6: Enemy prefab's spawn-in VFX fires in `Awake()`/`OnEnable()` — off-screen, at standby time, or twice

**What goes wrong:**
Standby-room enemies are already instantiated well before they're relevant: `TrySpawnExitPortal()` instantiates the next floor's standby room (and calls `TrySpawnEnemies` on it, up to `_floorHeight = 40` units above the current room) — but `Instantiate(standbyPrefab, ...)` runs the room's (and its children's) `Awake()` synchronously **before** the very next line, `standbyRoom.SetActive(false)`, executes. `EnemySpawner.Spawn()` does the identical thing: `Instantiate(prefab, ...)` (runs the enemy's `Awake()`) followed immediately by `_spawned.SetActive(false)`. If a new "spawn-in VFX" is naively implemented as "play on `Awake()`" or "play on `OnEnable()`," it will fire the instant the enemy is instantiated — potentially far off-screen, a floor early, long before the player will ever see it — and then fire **a second time** when `EnemySpawner.Activate()` later calls `SetActive(true)` (re-triggering `OnEnable()`).

**Why it happens:**
`Awake()`/`OnEnable()` on a prefab feel like the obvious place to kick off a spawn animation, but this codebase's spawn pipeline deliberately separates "exists in memory" (`Spawn()`, inactive) from "gameplay-active" (`Activate()`), specifically for lookahead pre-generation — any VFX trigger must be wired to the same seam `Activate()` already provides, not to Unity lifecycle callbacks.

**How to avoid:**
- Add the spawn-in VFX trigger as an explicit step inside `EnemySpawner.Activate()` (or a method the enemy exposes, e.g. `PlaySpawnIn()`, called by `Activate()` alongside/instead of raw `SetActive(true)`) — never inside the enemy's own `Awake()`/`OnEnable()`/`Start()`.
- The enemy's gameplay-active state (FSM `Update()` ticking, detection `OverlapCircle` calls, hitbox colliders) should not start until the spawn-in VFX completes — see Pitfall 7 for the concrete mechanism.

**Warning signs:**
Portal/spawn VFX playing in a room the player just left (standby room instantiated ahead of time), or playing twice on the same enemy; VFX firing with no player anywhere near the camera.

**Phase to address:**
Spawn VFX phase — but the fix (hooking into `Activate()`) has a hard dependency on the existing v3.0 `EnemySpawner` code, so it must be scoped against `EnemySpawner.cs` directly, not designed in isolation.

---

### Pitfall 7: Enemy becomes lethal/detectable before its spawn-in VFX finishes (or vice versa)

**What goes wrong:**
`MeleeEnemy`/`RangedEnemy`'s `Update()` FSM (`UpdateIdle`/`UpdateChase`) and collision-based detection (`Physics2D.OverlapCircle` in `IsPlayerInRange`) run every frame the instant the GameObject is active — there is currently no "spawning" state distinct from "idle" in the FSM. If `EnemySpawner.Activate()` is changed to call `SetActive(true)` and simultaneously start a spawn-in animation, the enemy's `Update()` starts running in the very same frame the VFX begins — meaning the enemy can detect the player, chase, or even reach `TelegraphAndAttack()` while still visually emerging from a portal. Conversely, if the FSM is naively frozen for the VFX duration without deciding the intended interaction, the player might land a dash-kill on something meant to still be "arriving" and untargetable.

**Why it happens:**
Extending an FSM that has exactly one "always-on-when-active" assumption (`Idle`→`Chase`→`Telegraph`→`Attack`, no `SpawningIn` state) requires deliberately inserting a new state and deciding where detection/hitboxes/dash-targetability turn on relative to it — easy to skip because the enemy "looks fine" in isolation without the new state; the timing sync between animation and danger is then accidental rather than designed.

**How to avoid:**
- Add an explicit `SpawningIn` FSM state (entered on `Activate()`, before `Idle`) during which `Update()`'s switch does nothing (mirrors how `Telegraph`/`Attack` already "do nothing, coroutine owns this state"), detection `OverlapCircle` calls are skipped, and the enemy is excluded from `CombatController.FindNearestEnemyInRange()` targeting — cheapest lever: keep `IsAlive` false until the VFX completes, then flip true, reusing the *existing* dead-enemy skip check (`if (enemy == null || !enemy.IsAlive) continue;`) in `CombatController.cs` for free.
- Decide the intended feel explicitly (does the spawn VFX telegraph "incoming danger" the player should react to, or is it purely cosmetic before gameplay starts?) — this determines whether hitboxes activate at VFX start or VFX end, and should be a documented decision, not an accident of implementation order.

**Warning signs:**
Player takes a "surprise" hit from an enemy still mid-spawn-animation; player can dash-kill a "spawning" enemy that shouldn't yet be a valid target.

**Phase to address:**
Spawn VFX phase, same pass as Pitfall 6 — both are about wiring the new VFX seam into the existing `EnemySpawner`/enemy FSM contract and should be solved together.

---

### Pitfall 8: Slow-motion/hit-freeze breaks new audio timing if built on `Time.deltaTime`-based coroutines

**What goes wrong:**
This project has **no audio system at all today** (zero `AudioSource`/`AudioListener`/`AudioClip` references anywhere in `Assets/Scripts`) — every audio feature (SFX-timed-to-hit, portal transition sound, death sound) is new code, and the single most common first-audio-system mistake in a project with a slow-motion/hit-freeze mechanic is writing any fade/sequencing logic (volume fade-in/out, delayed SFX cues) using `Time.deltaTime`/`WaitForSeconds` instead of `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` — which this codebase enforces everywhere else (i-frames, telegraphs, floor timer, transition effects) but is easy to forget for a brand-new subsystem with no existing pattern to copy from.
Additionally, `AudioSource.pitch` is **not** automatically affected by `Time.timeScale` in Unity — if the intent is for SFX to audibly "slow down" during slow-mo (a natural expectation given the existing dash-trail/hit-spark/camera-shake polish already ties visual feel to the slow-mo state), that must be done manually (e.g. `audioSource.pitch = basePitch * Time.timeScale`, updated via `Time.unscaledDeltaTime`-driven logic) — nothing does this by default, so naive `AudioSource.Play()` calls play at full speed/pitch regardless of slow-mo. Separately, `Time.timeScale = 0f` during `CombatController.HitFreeze()` does **not** pause already-playing `AudioSource`s — a sustained SFX started on dash keeps playing through the 75ms world-freeze unless explicitly designed for.

**Why it happens:**
There is no existing audio code to imitate the project's `Time.unscaledDeltaTime` convention from — the default tutorial pattern of `Time.deltaTime` is correct in most Unity projects but wrong in this one specifically.

**How to avoid:**
- Any new fade/sequencing coroutine for audio must use `Time.unscaledDeltaTime`/`WaitForSecondsRealtime`, exactly like `InvincibilityHandler`/`MeleeEnemy`/`RangedEnemy`/`FloorTransitionEffect` already do — treat this as a hard project convention to carry into the new subsystem, not something to re-derive.
- Explicitly decide (as a Key Decision) whether SFX should pitch-shift with `Time.timeScale` during slow-mo/hit-freeze; if so, implement it as a small centralized helper (e.g. an `AudioManager.PlaySfx(clip)` that reads `Time.timeScale` once per call) rather than duplicating pitch math at every call site.

**Warning signs:**
An SFX fade-out or delayed cue audibly "pausing"/stretching during slow-motion/hit-freeze in a way nothing else in the game does; SFX playing at a jarringly normal pitch/speed while the whole screen is in bullet-time.

**Phase to address:**
Audio system phase (foundational — must be settled before wiring individual SFX to portal/hit/death/boss events, since every call site inherits whichever convention the core audio helper establishes).

---

### Pitfall 9: Rapid dash-kills overlapping/clipping SFX with no voice management

**What goes wrong:**
The core loop (dash → kill → next dash) can chain kills in well under a second (`postKillLockout = 0.2f`, `hitFreezeDuration = 0.075f`), and multiple enemies can die in quick succession (regular rooms already spawn 2-4 melee + 0-3 ranged per `GetEnemyCount`). A naive one-`AudioSource.PlayOneShot()`-per-kill approach, or `Instantiate`-ing a fresh `GameObject`+`AudioSource` per event (mirroring how `SpawnHitSpark()`/`EnemyDeathEffect.SpawnDeathParticles()` already instantiate a throwaway VFX object per event), will either clip/stack indistinguishably under the default 32-voice limit, or generate GC churn on mobile from constant `Instantiate`/`Destroy` — the same GC-pressure lesson this codebase already learned and fixed for detection buffers (`_hitBuffer`, `_detectionBuffer` pre-allocated) but hasn't yet applied to audio because none exists.

**Why it happens:**
`PlayOneShot`-per-event is the simplest possible first implementation and works fine in isolated testing (one enemy, one kill) — the failure mode only appears under this game's actual rapid-chain-kill conditions, which won't show up unless explicitly tested with a room full of enemies.

**How to avoid:**
- Use a small pre-warmed pool of reusable `AudioSource`s (e.g. 4-8) for SFX playback instead of instantiating a new `AudioSource`/GameObject per event — same pre-allocation principle already applied to `Collider2D[]` buffers elsewhere in this codebase.
- For kill SFX specifically, consider a short per-clip cooldown/max-simultaneous-instances guard so several near-simultaneous kills don't all fire the same clip at full volume and phase-clip into distortion.

**Warning signs:**
Audio sounding distorted/harsh in multi-enemy rooms; GC spikes correlated with kill events once audio is added; frame hitches during rooms with several clustered enemies.

**Phase to address:**
Audio system phase — the pooling decision should be made when the core audio-playing helper is built, not retrofitted after every call site already assumes naive `PlayOneShot`/`Instantiate`.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|--------------------|-----------------|------------------|
| Boss implements plain `IEnemy.OnDashHit()` with no vulnerability gating (ships as a reskinned `MeleeEnemy`) | Fastest path to a "working" boss room for a demo | Defeats the entire point of the milestone ("보스전답게" framing); needs a rework the moment anyone playtests it | Never — this is the one thing this milestone is explicitly about |
| Boss room prefab duplicated from Complex_Room without stripping `EnemySpawner` markers | Reuses proven Tilemap/RoomConnector/CameraBound/ExitSpawnPoint setup instantly | Regular enemies silently co-spawn with the boss, breaking solo-fight framing (Pitfall 1) | Never — must strip/gate before first playtest, not "later" |
| New audio code written with `Time.deltaTime`/`WaitForSeconds` instead of unscaled equivalents | Slightly less code to think about, matches generic Unity tutorials | Audio desyncs from visuals during every slow-mo/hit-freeze moment — i.e. constantly, since slow-mo is the core loop | Never in this project |
| Spawn-in VFX triggered from `Awake()`/`OnEnable()` instead of `EnemySpawner.Activate()` | Looks correct in a quick single-enemy test scene | Fires off-screen at standby-room instantiation time and/or double-fires on `Activate()` (Pitfall 6) | Never — always wire to `Activate()` |
| `AudioSource`/effect-GameObject `Instantiate()` per SFX event instead of pooling | Works fine in isolated testing, less upfront plumbing | GC churn + voice clipping once multiple enemies die in quick succession (Pitfall 9) | Acceptable only for a first internal prototype pass explicitly flagged for a pooling follow-up before any real playtest/profiling pass |
| Skipping `FloorTimer` interaction entirely ("boss room just happens to always have enough time") | No code change needed right now | Silent, confusing mid-fight deaths the moment a playtester takes longer than expected (Pitfall 4) | Never — must be an explicit decision, even if the decision is "boss room grants +N seconds" |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|-----------------|-------------------|
| Boss room ↔ `WorldGenerator` chain (`_roomPrefabs`, `SpawnNextPair`/`SpawnPrevPair`) | Adding the boss room straight into `_roomPrefabs[]` so it can appear as any random lookahead/lookbehind filler room | Spawn boss room through its own dedicated probability roll/marker, analogous to `TrySpawnExitPortal`'s `_exitSpawnChance`, never as a plain pool entry |
| Boss room ↔ EXIT portal requirement ("층 진입은 기존 EXIT 포탈 그대로 필요") | Building a bespoke boss-exit mechanic instead of reusing `ExitPortal`/`ExitSpawnPoint` | Boss room should still contain a normal `ExitPortal`+`ExitSpawnPoint`, gated so it doesn't spawn/activate until the boss is defeated — reuse `RoomClearCondition`'s "watch a list of IEnemy, activate targetObject on all-dead" pattern directly, it already exists for exactly this "reveal exit after clearing enemies" shape |
| Boss activation ↔ `EnemySpawner`/`Activate()` seam | Reusing `EnemySpawner.Spawn()+Activate()` verbatim for the boss (immediate activation on lookahead spawn, Pitfall 5) | Boss needs its own entry-triggered activation (mirrors `ExitPortal.OnTriggerEnter2D`), not the ambient always-active pattern regular enemies use |
| New audio subsystem ↔ existing `Time.unscaledDeltaTime` convention | Writing fade/sequencing timers with `Time.deltaTime`/`WaitForSeconds` because no existing audio code to copy from | Follow the exact convention already used by `InvincibilityHandler`/`FloorTransitionEffect`/enemy telegraphs: `Time.unscaledDeltaTime` + `WaitForSecondsRealtime` everywhere |
| Spawn VFX ↔ `EnemyDeathEffect`'s `RuntimeMaskSprite`/SpriteMask pattern | Building a completely separate spawn-in visual technique from scratch | Reuse the already-proven `RuntimeMaskSprite.CreateMaskSprite()` + `SpriteMask` rise/shrink pattern (used identically by `FloorTransitionEffect` for entry/exit and `EnemyDeathEffect` for death) — a mirrored "mask shrinks away to reveal" is a natural, consistent spawn-in visual and avoids inventing a new masking technique |
| Camera bounds ↔ boss room's `CameraBound` | Forgetting the boss room needs its own `CameraBound` child marker (like every existing Complex_Room) — without it, `RecomputeCameraBounds()` silently falls back to a merged bounds from adjacent rooms/corridors, letting the camera drift outside the boss arena during the fight | Add a `CameraBound` sized to the boss arena exactly like existing rooms; verify `RecomputeCameraBounds()` is re-triggered appropriately if the boss encounter isolates the chain |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|-----------------|
| Per-kill `AudioSource`/effect GameObject `Instantiate()` (mirroring existing `SpawnHitSpark`/`SpawnDeathParticles` pattern, now applied to audio) | GC spikes/frame hitches on Android during multi-enemy rooms | Pre-allocate a small `AudioSource` pool (4-8 sources) instead of instantiate-per-event | Breaks once 3+ enemies die within ~1 second (very achievable given `postKillLockout=0.2s`) |
| Boss pattern coroutines using `GetComponentsInChildren`/`FindObjectsOfType`/LINQ per frame (easy to reach for when scripting multi-phase attack patterns) | Frame time spikes during boss fight specifically — the single most scrutinized scene in the milestone | Reuse the project's established pattern: pre-allocated `Collider2D[]` buffers + `Physics2D.OverlapCircle(ContactFilter2D, results[])`, cached layer masks in `Awake()` | Immediately on first profiling pass of the boss fight |
| Spawn-in VFX using a new `ParticleSystem`/mask `GameObject` per enemy spawn without cleanup (copy of `EnemyDeathEffect.SpawnDeathParticles`'s per-instance pattern, now also applied at spawn time in addition to death time) | Rooms with several spawners (2-4 melee + 0-3 ranged per `GetEnemyCount`) doubling transient GameObject churn (spawn effect + existing death effect) | Confirm `stopAction = ParticleSystemStopAction.Destroy` (or equivalent) is set on any new spawn-VFX `ParticleSystem`, same as death VFX already does | Noticeable once rooms with 4+ enemies (max `GetEnemyCount`, floor 11+) all spawn-in within the same lookahead-generation frame |

## Security Mistakes

Not applicable in the traditional sense — this is a single-player offline prototype with no network/auth/backend. No domain-specific security concerns beyond standard Unity asset hygiene (no untrusted user-generated audio/asset loading is planned).

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|--------------|-------------------|
| Boss dies to the very first dash with no visible resistance (Pitfall 3) | Anticlimactic — "boss" feels like a reskinned regular enemy, breaking the sense of occasion the milestone is meant to create | Gate lethality behind a telegraphed vulnerability window; make the boss's non-vulnerable state visually distinct (shield/color VFX) so players understand why a dash didn't kill it, rather than it feeling broken |
| Player killed by `FloorTimer` mid-boss-fight, indistinguishable from a normal "시간 초과" death (Pitfall 4) | Feels like an unrelated system betrayal — player was doing well in the fight and lost for reasons outside the encounter | Explicitly pause/extend the timer for boss rooms, or clearly signal (HUD) that the boss room is exempt, so players never wonder "did I lose to the boss or the clock?" |
| Spawn-in VFX plays but the enemy is already lethal underneath it, or vice versa (Pitfall 7) | Player takes a "cheap" hit from something still visually arriving, or wastes a dash on something not yet a valid target — feels unfair/buggy rather than intentional | Explicitly design and test the vulnerability/hitbox timing relative to VFX start/end as one decision, not an accidental byproduct of implementation order |
| New portal/hit/death SFX added without a relative-volume/mix pass (first-ever audio, easy to just wire clips without comparing loudness) | Some SFX (e.g. death) can drown out others (e.g. hit spark) or feel jarringly loud/quiet relative to each other since there's no existing baseline | Do a dedicated relative-volume pass across all new SFX together (portal, hit, death, boss-specific) rather than tuning each in isolation as it's added |

## "Looks Done But Isn't" Checklist

- [ ] **Boss room prefab:** Often still contains leftover `EnemySpawner` markers copied from the base Complex_Room prefab it was duplicated from — verify `GetComponentsInChildren<EnemySpawner>(true)` returns empty (or an explicit gate skips them even if present).
- [ ] **Boss `IEnemy` implementation:** Often technically "implements the interface" (compiles, `CombatController` can target/kill it) without any vulnerability-window gating — verify the boss survives at least one dash outside its telegraphed opening before declaring the encounter "done."
- [ ] **Spawn-in VFX:** Often triggers correctly in a hand-placed single-enemy test scene but not verified against the actual lookahead-pregeneration path (`SpawnNextPair`'s `TrySpawnEnemies` calling `Spawn()`+`Activate()` back-to-back on a room the player hasn't reached, plus the separate standby-room pre-instantiation in `TrySpawnExitPortal`) — verify by watching Scene view (not just Game view) for VFX firing on off-screen/standby rooms.
- [ ] **Audio timing:** Often "works" in normal-speed testing but not verified during an actual slow-mo hold + hit-freeze + dash sequence — verify by deliberately triggering SFX while slow-mo/hit-freeze is active and confirming no fade/coroutine desyncs (`Time.deltaTime` leaks).
- [ ] **FloorTimer × boss room:** Often not tested end-to-end with a long boss fight (dev testers might beat the boss quickly, hiding the interaction) — verify by deliberately taking 60+ real seconds inside the boss room and confirming the intended timer behavior, not an accidental death.
- [ ] **WorldGenerator recycle vs. boss room:** Often not tested with player movement patterns that push `_playerCurrentIndex` outside the lookbehind/lookahead window mid-fight (e.g. deliberately backing away from the boss room entrance and re-entering) — verify the boss room is never `Destroy()`-ed while a boss encounter is unresolved.
- [ ] **Multiple simultaneous SFX:** Often only tested against a single enemy — verify against a full room (max `GetEnemyCount`, floor 11+: 2 melee + up to 3 ranged) killed in rapid succession without audible clipping/distortion.

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|-----------------|------------------|
| Boss trivialized by naive one-shot `IEnemy` (Pitfall 3) | MEDIUM | Introduce a vulnerability flag and gate targetability/`OnDashHit()` on it; requires revisiting the attack-pattern coroutine to insert vulnerability windows, but doesn't require touching `CombatController` itself |
| Regular enemies leaking into boss room (Pitfall 1) | LOW | Strip `EnemySpawner` children from the boss prefab and/or add the type-check gate in `TrySpawnEnemies`; no runtime state migration needed, this is prefab/authoring hygiene |
| WorldGenerator destroying boss room mid-fight (Pitfall 2) | MEDIUM | Add the `_bossEncounterActive` guard to `Update()`'s trim loops; requires careful testing of chain invariants (`_playerCurrentIndex` math) to ensure skipping a trim doesn't desync lookahead/lookbehind counts once the guard lifts |
| FloorTimer killing player mid-boss-fight (Pitfall 4) | LOW | Add `Pause()`/`Resume()` to `FloorTimer` (small additive static-class change); wire calls at boss-room-entered/boss-defeated hooks |
| Spawn VFX firing off-screen/twice (Pitfall 6) | LOW | Move the trigger call from `Awake()`/`OnEnable()` into `EnemySpawner.Activate()`; delete the erroneous early call site |
| Audio desyncing during slow-mo (Pitfall 8) | LOW-MEDIUM | Replace `Time.deltaTime`/`WaitForSeconds` with unscaled equivalents in affected audio coroutines; low cost if caught early, medium if many call sites already copied the wrong pattern |
| SFX clipping/GC churn under rapid kills (Pitfall 9) | MEDIUM | Retrofit an `AudioSource` pool behind existing "play SFX" call sites; requires touching every call site that currently does `Instantiate`-per-event, but the pool itself is a small, self-contained addition |

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|----------------|------------|
| Boss room core/gating (room type, activation, lifecycle) | Regular enemies leak in (P1); WorldGenerator destroys boss room mid-fight (P2); FloorTimer kills player mid-fight (P4); boss activates before player enters (P5) | Strip `EnemySpawner` from boss prefab + explicit type gate; freeze chain trimming during encounter; pause/extend `FloorTimer`; entry-triggered activation mirroring `ExitPortal` |
| Boss AI/pattern design | Naive one-shot `IEnemy` trivializes the fight (P3) | Vulnerability-window gating on targetability, not a rewrite of `IEnemy`/`CombatController` |
| Spawn VFX (enemy spawn-in) | VFX fires in Awake/OnEnable — off-screen/double-fire (P6); enemy lethal/detectable before VFX completes or vice versa (P7) | Wire VFX trigger into `EnemySpawner.Activate()` only; add a `SpawningIn` FSM state gating detection/targetability until VFX completes |
| Audio system (first-ever) | `Time.deltaTime`-based audio desyncs during slow-mo/hit-freeze (P8); rapid-kill SFX clipping/GC churn (P9) | Enforce `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` convention from day one; build a pooled `AudioManager` before wiring individual SFX call sites |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|--------------------|-----------------|
| 1. Regular enemies leak into boss room | Boss Room Core/Gating phase | Boss room prefab has zero `EnemySpawner` children; a deliberately-reintroduced test `EnemySpawner` is still skipped by code, not just absent |
| 2. WorldGenerator destroys boss room mid-fight | Boss Room Core/Gating phase | Move the player back-and-forth across the lookbehind/lookahead boundary while the boss encounter is unresolved; boss room and boss state must survive |
| 3. Boss trivialized by naive one-shot kill | Boss AI/Pattern Design phase | A dash outside the telegraphed vulnerability window does not kill the boss; a dash inside the window does |
| 4. FloorTimer kills player mid-fight | Boss Room Core/Gating phase | Spend 60+ real seconds inside the boss room without triggering the countdown death (unless "boss room grants extension" was the explicit decision, in which case verify that instead) |
| 5. Boss activates before player enters room | Boss Room Core/Gating phase | Watch Scene view while the boss room is lookahead-only (player hasn't reached it) — boss must be inert until an entry trigger fires |
| 6. Spawn VFX fires in Awake/OnEnable (off-screen/double-fire) | Spawn VFX phase | Watch Scene view during standby-room pre-instantiation (`TrySpawnExitPortal`) and lookahead generation (`SpawnNextPair`) — VFX must not fire until `Activate()` is called on an on-screen-relevant enemy |
| 7. Enemy lethal/detectable before spawn VFX completes | Spawn VFX phase | During the VFX window, the enemy must not appear in `CombatController.FindNearestEnemyInRange()` results and must not damage the player; confirm the moment gameplay-active starts relative to VFX end is an explicit, tested decision |
| 8. Audio breaks during slow-mo/hit-freeze | Audio System phase | Trigger a fading/sequenced SFX, then immediately enter slow-mo and hit-freeze mid-playback — fade timing must track real time, not scaled time |
| 9. Rapid-kill SFX clipping/GC churn | Audio System phase | Profile (or at minimum audibly test) a full room's worth of enemies (floor 11+ max count) killed in quick succession; no distortion, no GC spike correlated with kill events |

## Sources

- Direct source reading (HIGH confidence, this codebase): `Assets/Scripts/World/WorldGenerator.cs`, `Assets/Scripts/World/ExitPortal.cs`, `Assets/Scripts/World/EnemySpawner.cs`, `Assets/Scripts/World/FloorTimer.cs`, `Assets/Scripts/World/FloorManager.cs`, `Assets/Scripts/World/RoomConnector.cs`, `Assets/Scripts/World/FloorTransitionEffect.cs`, `Assets/Scripts/Room/RoomClearCondition.cs`, `Assets/Scripts/Player/CombatController.cs`, `Assets/Scripts/Player/InvincibilityHandler.cs`, `Assets/Scripts/Enemy/MeleeEnemy.cs`, `Assets/Scripts/Enemy/RangedEnemy.cs`, `Assets/Scripts/Enemy/EnemyDeathEffect.cs`, `Assets/Scripts/Enemy/IEnemy.cs`, `Assets/Scripts/Camera/CameraFollow.cs`, `Assets/Scripts/World/GameBootstrapper.cs`, `.planning/PROJECT.md`
- [PlayOneShot Performance — Unity Discussions](https://discussions.unity.com/t/playoneshot-performance/595405) — MEDIUM confidence, community discussion on voice-count/pooling
- [10 Unity Audio Optimisation Tips — Game Dev Beginner](https://gamedevbeginner.com/unity-audio-optimisation-tips/) — MEDIUM confidence, mobile pooling/GC guidance
- [How to fix the audio when using Time.TimeScale? — Unity Discussions](https://discussions.unity.com/t/how-to-fix-the-audio-when-using-time-timescale/843414) — MEDIUM confidence, confirms `AudioSource.pitch` is not auto-linked to `Time.timeScale`
- [Adjust audio playback rate with Time.timeScale — Unity Discussions](https://discussions.unity.com/t/adjust-audio-playback-rate-with-time-timescale/40379) — MEDIUM confidence, corroborates manual pitch-scaling pattern
- [I am getting a lot of sound latency when developing my game for Android — Unity Support](https://support.unity.com/hc/en-us/articles/206116316) — MEDIUM confidence, Android audio latency baseline (200-300ms) and DSP buffer trade-offs
- [70 Tips for Better Boss Battles — M.T. Black Games](https://www.mtblackgames.com/blog/65-tips-for-better-boss-battles) — MEDIUM confidence, general boss-design telegraphing guidance
- [What are your thoughts on insta-kill attacks? — NeoGAF](https://www.neogaf.com/threads/what-are-your-thoughts-on-insta-kill-attacks.1419454/) — LOW-MEDIUM confidence, community discussion; cited for the Titan Souls one-hit-kill-both-ways precedent as design grounding for Pitfall 3

---
*Pitfalls research for: Fast (가칭) v3.1 — 보스 룸 콘텐츠 + 연출 고도화(사운드/타이밍/신규 스폰 VFX)*
*Researched: 2026-07-08*
