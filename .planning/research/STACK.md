# Stack Research

**Domain:** Boss room content (extensible framework) + VFX/Audio polish — Unity 6 URP 2D mobile prototype (Fast, v3.1 milestone)
**Researched:** 2026-07-08
**Confidence:** HIGH (built on direct inspection of existing codebase + Unity 6 official manual + cross-checked WebSearch on 2026 mobile audio/FSM practice)

## Scope note

This milestone adds **no new capability categories** to the project — it reuses/extends built-in Unity systems that are already installed (see `Packages/manifest.json`, unchanged from v3.0). There are **zero new UPM packages to add**. The work is architectural (new C# scripts + Editor prefab-builder scripts following the project's established convention), not a dependency-adoption decision. This document therefore focuses on *which built-in Unity API/pattern to use* and *how it integrates with existing components* (`PortalEffectBuilder`, `HitSparkBuilder`, `EnemyDeathEffect`, `WorldGenerator`, `ExitPortal`, `MeleeEnemy`/`RangedEnemy`, `ScoreManager`, `RoomClearCondition`).

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| `UnityEngine.AudioSource` + `AudioClip` | Built into `com.unity.modules.audio` 1.0.0 (already in manifest, no version bump) | Play one-shot SFX: portal enter/exit, hit spark, enemy death, enemy/boss spawn-in | Zero new dependency, zero learning curve, sufficient for a prototype with a handful of one-shot cues and no adaptive music. Official Unity 6 guidance treats AudioSource + AudioMixer as the correct baseline before reaching for middleware. |
| `UnityEngine.Audio.AudioMixer` | Built into `com.unity.modules.audio` (Editor asset, not a package) | Group SFX under a single `SFX` mixer group (and `Master`) so a future "SFX volume" slider is a one-line hookup, and so hit/death/portal sounds share consistent loudness/compression | This is the "structural core" every Unity audio guide recommends adding before wiring any `AudioSource` — retrofitting it later means re-touching every call site. Costs nothing to add now. |
| C# abstract class `BossEnemyBase : MonoBehaviour, IEnemy` | N/A (project code, not a package) | Extensible boss framework: shared death/highlight/dash-kill wiring (same contract `CombatController` already dashes into), subclasses only implement attack-pattern methods | `IEnemy` (`IsAlive`, `OnDashHit()`, `ClearHighlight()`) is the *only* contract `CombatController`/`RoomClearCondition` care about — a boss that implements it drops into the existing one-shot-kill dash pipeline with no changes to `CombatController`. Inheritance (not a new data-driven system) matches the project's existing style (`MeleeEnemy`/`RangedEnemy` are both plain enum-FSM `MonoBehaviour`s) and is right-sized for "1 boss now, more later" — a full ScriptableObject attack-pattern engine (see Alternatives) is unjustified for a single boss. |
| Coroutine + enum-based phase FSM (same idiom as `MeleeEnemy`/`RangedEnemy`) | N/A | Boss attack pattern sequencing (telegraph → windup → hitbox → recover, per phase) | Directly reuses the already-proven `TelegraphAndAttack()`-style coroutine idiom, including the `WaitForSecondsRealtime` timeScale-immunity rule the codebase already enforces everywhere (slow-mo/HitFreeze safety). No new pattern to learn or review. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `SpriteRenderer` + hand-rolled coroutine scale/mask animation (`RuntimeMaskSprite`, same pattern as `FloorTransitionEffect`/`PortalEffectBuilder`) | N/A (existing project code) | Enemy/boss spawn-in VFX (mirrors the player's portal-entry visual) | Add a new `EnemySpawnEffect` component that reuses `RuntimeMaskSprite.CreateMaskSprite()` + the existing `Portal_100x100px1.png` sprite (already reused twice via `PortalEffectBuilder`/`ExitPortalBuilder`) to grow a small portal ring behind a freshly-activated enemy, then mask-reveal the sprite — same math as `FloorTransitionEffect.PlayExit()`, just triggered from `EnemySpawner.Activate()` / `WorldGenerator.TrySpawnEnemies()` instead of `WorldGenerator.EnterPortal()`. |
| `ParticleSystem` (Shuriken, built-in) | `com.unity.modules.particlesystem` 1.0.0 (already installed) | Any additional spawn/hit/death particle bursts | Already the established choice (`EnemyDeathEffect.SpawnDeathParticles()`, `HitSparkEffect`). Keep burst counts low (≤20-30) per the v3.0 stack research's Android compute/GC guidance — still valid, unchanged this milestone. |
| `Animator` + `AnimatorController` created via Editor script (same convention as `HitSparkBuilder.cs` using `AnimatorController.CreateAnimatorControllerAtPath`) | `com.unity.modules.animation` 1.0.0 (already installed) | Boss sprite states (idle/telegraph/attack/hurt/death), and any new spawn-VFX animator if frame-based (not just tween) | Follow the exact `Assets/Editor/*Builder.cs` convention already used for `HitSparkController.controller` — build boss AnimatorController programmatically from existing/new clips rather than hand-wiring in the Editor, consistent with the rest of the codebase's reproducible-build-via-menu-item pattern. |
| `AudioSource.PlayOneShot(clip)` on a **persistent** GameObject for portal SFX | N/A | Portal enter/exit sound | The portal-transition sequence destroys the old room chain mid-sequence (`WorldGenerator.FloorTransitionSequence`). Attach the portal `AudioSource` to the **Player** GameObject (persists across the transition, already hosts `FloorTransitionEffect`) or to `WorldGenerator` itself (persistent singleton) — not to the `ExitPortal`/room GameObjects, which are destroyed mid-sequence and would cut the sound off. |
| `AudioSource.PlayOneShot(clip)` on the enemy/boss GameObject itself for hit/death SFocused | N/A | Hit spark + enemy/boss death sound | `EnemyDeathEffect.PlayDeathSequence()` already waits ~0.6s+ before `Destroy(gameObject)` — enough headroom for a short (<500ms) death SFX to finish before the owning GameObject dies. No pooling/persistent-source workaround needed here, unlike the portal case. |
| `UnityEngine.Pool.ObjectPool<T>` | Built-in since 2021.2, stable in Unity 6 (no package) | Only if the boss's attack pattern fires many short-lived projectiles/VFX per second | Not needed for hit sparks/spawn VFX/enemy death at current scale (a handful of instances per room, `AutoDestroySelf`/`stopAction=Destroy` already handles cleanup). Only reach for this if the single boss's pattern turns out to be a bullet-hell-style barrage — add then, not preemptively (YAGNI, matches CLAUDE.md's anti-overengineering rule). |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| Editor menu-item prefab builders (`Assets/Editor/*Builder.cs`) | Reproducible, code-defined prefab construction (no manual prefab editing to lose) | This project's established convention (`PortalEffectBuilder`, `HitSparkBuilder`, `RoomPrefabBuilder`, `CorridorBuilder`, `ExitPortalBuilder`). Add `BossSpawnEffectBuilder.cs`, `BossRoomBuilder.cs`, and (if needed) an `AudioMixerBuilder`-style manual one-time setup for the SFX group. Keep following this pattern rather than hand-authoring new prefabs in the Editor — it's how every other v3.0 effect prefab was made and reviewed. |
| Unity Audio Mixer window (`Window > Audio > Audio Mixer`) | Route all new `AudioSource`s to a shared `SFX` group | One-time manual setup (not scriptable via a builder in the same way — Editor asset, do by hand once). |
| `RoomClearCondition.cs` (existing, currently orphaned/unused in the scene graph but present in the codebase) | Watches an `IEnemy[]` array and activates a target GameObject when all are dead | Directly reusable for "solo boss fight, no regular enemies, defeat triggers a reward/gate" — since `BossEnemyBase` implements `IEnemy`, dropping the boss into this component's `enemies` array (or letting its dynamic `GetComponentsInChildren<MonoBehaviour>()` scan find it) requires **zero changes** to `RoomClearCondition` itself. |
| `ScoreManager` (existing static class) | Score bonus on boss defeat | Add `AddBossKillScore()` following the exact pattern of `AddKillScore()`/`AddTimeBonus()` — no new scoring system, just one more constant + method on the existing static class. |

## Installation

No `npm`/UPM installation step is required — every API used above is already present in `Packages/manifest.json` (`com.unity.modules.audio`, `com.unity.modules.animation`, `com.unity.modules.particlesystem`, `com.unity.modules.physics2d`) at the versions already pinned for v3.0. This milestone is pure C#/Editor-script authoring on top of the existing package set.

If a project-wide SFX volume slider or ducking is desired beyond a flat `SFX` mixer group, no package addition is needed either — `AudioMixerGroup`/`AudioMixerSnapshot` are part of the same built-in `com.unity.modules.audio` module.

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| Plain `AudioSource` + one `AudioMixer` group | FMOD for Unity / Wwise | Only if the game later needs adaptive/interactive music, real-time DSP layering, or a large SFX library with non-programmer authoring (sound designer owns mixing outside Unity). For a prototype with ~5-8 one-shot cues, middleware is pure overhead — extra native plugin size on an ARM64 APK, an extra build step, and a learning curve for zero prototype-stage benefit. |
| Inheritance-based `BossEnemyBase` (code-driven, matches `MeleeEnemy`/`RangedEnemy` style) | ScriptableObject-driven attack-pattern "Strategy pattern" (e.g. `AttackPatternSO` assets swapped per boss) | Reconsider if/when a **second or third boss type** is added and attack patterns need to be authored/tuned by a non-programmer without touching code, or patterns need to be mixed-and-matched across bosses at runtime. For "1 boss this milestone, framework extensible via subclassing," plain inheritance is simpler and reviewable in a single diff; introducing a full SO-based pattern engine now would be speculative generality the CLAUDE.md anti-overengineering rule explicitly warns against. |
| Coroutine-driven spawn/portal VFX (existing hand-rolled `Lerp`/`SpriteMask` idiom) | `com.unity.timeline` (already installed, 1.8.11, currently unused in the whole codebase) | Timeline is worth adopting only if a boss needs a scripted multi-beat **cutscene** (camera moves + multiple animator/audio tracks choreographed together) — e.g. a boss "intro" sequence. For a spawn VFX that's structurally identical to `FloorTransitionEffect.PlayExit()`, adding Timeline authoring overhead (director asset, tracks, bindings) for a single effect is not worth the inconsistency with every other effect in the game being coroutine-driven. |
| Built-in `ParticleSystem` (Shuriken) | `com.unity.vfx` (VFX Graph) | Never for this project on current scope — see What NOT to Use below. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| FMOD / Wwise / any audio middleware | Adds a native plugin dependency, increases APK size, and requires an authoring workflow (FMOD Studio / Wwise Authoring) entirely outside Unity for a prototype that needs maybe 5-8 short SFX. CLAUDE.md scope rule: "프로토타입 외 기능 추가 금지." | `AudioSource` + `AudioMixer` (built-in, already installed) |
| VFX Graph (`com.unity.visualeffectgraph`) | Requires Compute Shader support, which is not guaranteed across the Android minSdk 25 / ARM64 device range this project targets (hardware-dependent on lower-end GPUs) — same conclusion as the v3.0 STACK research, unchanged for this milestone. Also not installed in `manifest.json`; adding it is a new, heavier dependency for effects the built-in Shuriken system already produces adequately (hit spark, death particles, spawn burst). | `ParticleSystem` (Shuriken, already installed and used throughout) |
| ScriptableObject-based data-driven boss/attack-pattern framework (up front) | Speculative generality for a milestone shipping exactly one boss. Adds an authoring layer (SO assets, editor tooling to keep them in sync with code) with no current payoff, contradicting CLAUDE.md's "요청되지 않은 유연성을 추가하지 마세요" rule. | `BossEnemyBase` abstract class + subclass per boss (add the SO layer later only if/when a second boss's authoring pain justifies it) |
| `com.unity.timeline` for boss attack-pattern sequencing | Introduces a new authoring tool/asset type into a codebase where every other timed sequence (portal transition, hit freeze, death fade) is a hand-rolled coroutine — inconsistent with existing review-friendly pattern, and unnecessary for pattern logic that's just "telegraph → windup → hitbox → recover" (already the `MeleeEnemy`/`RangedEnemy` shape). | Coroutine + enum-phase FSM, same idiom as existing enemies |
| New custom object-pooling system, or premature `ObjectPool<T>` adoption for spawn/hit/death VFX at current scale | The project already relies on cheap `Instantiate`/`Destroy` with `AutoDestroySelf` / `ParticleSystemStopAction.Destroy` for a handful of concurrent effects per room — adding pooling now is unmeasured optimization for a non-demonstrated problem. | Keep `Instantiate`/`Destroy`; revisit with `UnityEngine.Pool.ObjectPool<T>` only if profiling on-device shows GC spikes from the new boss/spawn VFX specifically |
| Attaching the portal-transition `AudioSource` to the `ExitPortal`/room GameObject | `WorldGenerator.FloorTransitionSequence()` destroys the entire old room chain (including the portal) partway through the sequence (`D-07`) — a sound started on that GameObject would be cut off mid-playback when it's destroyed. | Attach to the Player GameObject (already hosts `FloorTransitionEffect`, persists across the transition) or to the `WorldGenerator` singleton |

## Stack Patterns by Variant

**If the boss needs multiple distinct attack patterns (phases):**
- Use a `protected` enum extending the existing `Idle/Chase/Telegraph/Attack` shape with boss-specific phase names (e.g. `Phase1`, `Phase2`, `Enrage`), still coroutine-driven.
- Because it's a one-line mental extension of the FSM shape already reviewed and proven in `MeleeEnemy.TelegraphAndAttack()` — no new architecture to learn.

**If future milestones add a 2nd/3rd boss and pattern reuse across bosses becomes painful:**
- Revisit the ScriptableObject "Strategy pattern" alternative (small `AttackPatternSO` assets executed by a shared `BossEnemyBase.RunPattern(AttackPatternSO)` coroutine driver) — a targeted refactor, not a v3.1 concern.
- Because premature introduction now would be unused flexibility per CLAUDE.md scope discipline; the trigger condition (a second boss existing) hasn't happened yet.

**If SFX asset sourcing becomes a blocker (no audio assets currently exist in the project):**
- Use short, free/CC0 SFX (e.g. from Unity Asset Store free packs, Kenney.nl, or freesound.org CC0 filtered results) trimmed to <500ms per one-shot.
- Set `AudioClip` import **Load Type = Decompress On Load** and **Compression Format = Vorbis (low quality, e.g. 40-70%)** for these short one-shots — standard Unity mobile guidance: short clips decompressed on load avoid per-frame CPU decompression cost, while Vorbis keeps APK size down versus PCM.
- Because this is a mobile Android target (minSdk 25/ARM64) — audio memory and APK size both matter even for a prototype.

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| `com.unity.modules.audio` 1.0.0 (unchanged) | Unity 6000.3.11f1 | No version change needed — audio module already declared in `manifest.json` from project creation; `AudioMixer`/`AudioSource`/`AudioClip` APIs used here are all stable since early Unity versions, unaffected by Unity 6 API renames (unlike `Rigidbody2D.velocity → linearVelocity` noted in the v3.0 STACK research). |
| `com.unity.modules.particlesystem` 1.0.0 (unchanged) | Unity 6000.3.11f1 | Already used by `EnemyDeathEffect`/`HitSparkEffect`; no change. |
| `com.unity.timeline` 1.8.11 (already installed, unused) | Unity 6000.3.11f1 | Present in manifest from project template default but intentionally not adopted this milestone (see What NOT to Use) — no action needed either way, it's a harmless unused dependency. |
| N/A — no new packages added this milestone | — | This milestone's stack is 100% code + Editor scripts on top of the existing v3.0 package set. |

## Sources

- Direct inspection of `D:\새 폴더\Fast\Packages\manifest.json` (confirms `com.unity.modules.audio`/`particlesystem`/`animation`/`timeline` already installed, no FMOD/Wwise/VFX Graph present) — HIGH confidence
- Direct inspection of `Assets/Editor/PortalEffectBuilder.cs`, `Assets/Editor/HitSparkBuilder.cs`, `Assets/Scripts/Enemy/EnemyDeathEffect.cs`, `Assets/Scripts/World/{WorldGenerator,ExitPortal,ScoreManager,EnemySpawner}.cs`, `Assets/Scripts/Enemy/{MeleeEnemy,RangedEnemy,IEnemy}.cs`, `Assets/Scripts/Room/RoomClearCondition.cs`, `Assets/Scripts/World/FloorTransitionEffect.cs` — HIGH confidence (these define every integration point recommended above)
- Grep confirmed zero existing `AudioSource`/`AudioClip`/`AudioMixer` usage anywhere in `Assets/Scripts` or `Assets/Editor` — HIGH confidence this milestone starts audio from zero
- WebSearch: "Unity 6 2D mobile game audio best practice AudioSource AudioMixer vs FMOD prototype 2026" — MEDIUM confidence (multiple independent sources agree AudioMixer is the correct baseline before middleware; https://docs.unity3d.com/Manual/class-AudioSource.html official docs cross-checked)
- WebSearch: "Unity ScriptableObject boss attack pattern FSM authoring pattern 2026" — MEDIUM confidence (confirms SO-based Strategy pattern is a recognized alternative but explicitly notes it's for reusable/designer-facing cases — reinforces the "not needed for 1 boss" recommendation here)
- `.planning/research/STACK.md` (v3.0, prior milestone) — carried-forward HIGH confidence facts reused verbatim where still valid (VFX Graph compute-shader risk on Android, `UnityEngine.Pool.ObjectPool<T>` availability, `ObjectPool` vs custom pooling guidance)

---
*Stack research for: Fast v3.1 — 보스 룸 콘텐츠 + 연출 고도화(사운드/타이밍/신규 스폰 VFX)*
*Researched: 2026-07-08*
