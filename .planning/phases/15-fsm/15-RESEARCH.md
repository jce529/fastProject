# Phase 15: 보스 FSM & 빈틈 타겟팅 - Research

**Researched:** 2026-07-15
**Domain:** Unity 6 (URP 2D) C# gameplay FSM — single-target boss encounter reusing existing enemy/combat architecture
**Confidence:** HIGH (all findings sourced from the actual codebase already in this repo; no external library uncertainty)

## Summary

This phase does not introduce any new technology — it is a composition problem. The codebase already contains two working 4-state enemy FSMs (`MeleeEnemy`, `RangedEnemy`), a death-effect pipeline (`EnemyDeathEffect`), a spawn-gating pipeline (`ISpawnGatable`/`EnemySpawnEffect`), a targeting system that already skips non-`IsAlive` enemies (`CombatController.FindNearestEnemyInRange`), and a highlight system that writes directly to `SpriteRenderer.color` (`CombatController.UpdateHighlight`/`ClearHighlight`). `BossEnemy` is a new class that recombines these exact pieces with one twist: `IsAlive` is overloaded to mean "currently vulnerable" rather than "not yet dead," and a hit does not end the encounter — it resets a loop and increments a private counter.

The single highest-risk area is **not** the FSM itself — it's the two places where the boss's state interacts with pre-existing systems that were not designed with a re-toggling `IsAlive` in mind: (1) `CombatController`'s hit pipeline calls `target.OnDashHit()` unconditionally once a dash is committed, and continues to fire SFX/score/camera-shake regardless of what `OnDashHit()` does internally — so `OnDashHit()` must never silently no-op just because the vulnerable window closed mid-dash; and (2) `ClearHighlight()` is called by every enemy class with a hardcoded `Color.white`, which will stomp the boss's own vulnerability tint if the player highlights-then-cancels while the boss is still vulnerable. Both are solvable with a few extra lines in `BossEnemy`, not architectural changes to `CombatController` (which stays untouched, per the locked roadmap decision).

**Primary recommendation:** Build `BossEnemy` as a direct structural clone of `MeleeEnemy`'s coroutine-driven `TelegraphAndAttack()` pattern (same `Time.unscaledDeltaTime` loop, same `!IsAlive` mid-loop guards, same single-coroutine-field discipline), but split "vulnerable" (toggles `IsAlive`) from "defeated" (a new one-way private bool) so `OnDashHit()` can always register a hit regardless of the timing race between dash-arrival and vulnerable-window-end, and override `ClearHighlight()` to restore the boss's *current* state color instead of hardcoded white.

## User Constraints

### Locked Decisions

- **D-01:** Boss's telegraph attack is melee (dash/swing) — implement by referencing `MeleeEnemy`'s melee-hitbox attack pattern.
- **D-02:** "Vulnerable" state must be shown with BOTH a stop-in-place AND a color change — either alone is insufficient (user judgment).
- **D-03:** Vulnerable duration ~0.8–1.2s — longer than `MeleeEnemy`'s existing 0.45s telegraph, to give boss fights a readable rhythm. Exact number is Claude's Discretion.
- **D-04:** During telegraph, the boss moves at reduced speed (does NOT stop) while telegraphing — same approach as `MeleeEnemy` D-05 (999.4).
- **D-05:** Telegraph→Vulnerable loop is a **single repeating pattern** — no pattern-type cycling this phase. Claude's Discretion applies only to the repeat cadence/numbers.
- **D-06:** A hit during Vulnerable plays a boss-specific hit reaction — **programmatic only** (color flash + brief knockback/stagger + existing hit-spark reuse). No new animation clips (no boss art exists yet).
- **D-07:** After a hit, the pattern resets to the start after a **short pause** (not instantly) — long enough for the D-06 hit reaction to read. Exact pause length is Claude's Discretion.
- **D-08:** On the 7th hit (death), reuse `EnemyDeathEffect`'s sequence (Die animation → particles → SpriteMask rise-fade → Destroy) but extend it (longer/more exaggerated) for the boss. Exact extension mechanism (particle scale, duration, camera shake) is Claude's Discretion.
- **D-09:** Boss kill grants a new `ScoreManager` constant/method, significantly larger than `KillScore` (100) — guideline: 500–1000. Exact number is Claude's Discretion.
- **D-10:** No new boss art this phase — reuse existing enemy sprites, resized/recolored only. Real boss art deferred to post–Phase 16.
- **D-11:** Phase 15 FSM validation happens via `DebugRoomTeleporter` extended with a boss prefab field (same pattern as `_meleePrefab`/`_rangedPrefab`). No boss-room content or `WorldGenerator` integration needed this phase.

### Claude's Discretion

- Exact vulnerable-state duration (within 0.8–1.2s)
- Exact hit-reaction implementation details (flash intensity, knockback distance, hit-spark placement)
- Exact pause length before pattern reset after a hit
- Exact "boss-exclusive extension" mechanism for the death sequence (particle scale, duration, camera shake intensity)
- Exact kill score bonus (within 500–1000)
- Exact scale/color transform values to make existing enemy sprites read as "boss"
- Exact telegraph move-speed multiplier, and whether the melee attack is a simple dash or a hitbox sweep

### Deferred Ideas (OUT OF SCOPE)

- Multiple attack pattern types cycling (2-3+) — single pattern only this phase; candidate for future boss types after framework validation.
- Boss-specific hit-reaction animation clips — programmatic effects only this phase; swap to animation-based once real boss art exists.
- Dedicated boss sprite/art production — sprite variation (scale/color) only this phase; tracked under v2 Requirements BOSS-11/12.
- Boss room spawning/probability, solo-combat guarantee, camera work, floor-timer pause, `WorldGenerator` cleanup exceptions, `WorldGenerator` integration/EXIT flow after boss death — all explicitly Phase 16/17.
- Boss HP bar / multi-phase combat — explicitly excluded project-wide (REQUIREMENTS.md Out of Scope): no HP system exists anywhere in this game; do not introduce one for the boss.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| BOSS-03 | 보스는 예고→빈틈 루프를 반복하며, 빈틈 상태에서만 돌진 대상이 된다 | `MeleeEnemy.TelegraphAndAttack()` coroutine structure (Code Examples §1) directly reusable; `IsAlive`-as-vulnerability overload confirmed compatible with `CombatController.FindNearestEnemyInRange()`'s existing `!enemy.IsAlive` skip (line ~401) — zero `CombatController` changes needed (Architecture Patterns §Vulnerability Gating) |
| BOSS-04 | 7회 피격 시 처치, 매 피격마다 패턴 처음부터 재시작 | Hit-count threshold pattern (Code Examples §3) + Common Pitfalls §1 (off-by-one) + §2 (OnDashHit race) + coroutine restart discipline (Architecture Patterns §Coroutine Lifecycle) |
| BOSS-05 | 처치 진행률 비노출 | No UI work required — confirmed no existing HP-bar/progress UI component to accidentally wire up; `_hitCount` stays a private field with no Inspector/UI binding |
| BOSS-06 | 처치 시 ScoreManager 점수 보너스 | `ScoreManager.AddKillScore()` sibling-method pattern (Code Examples §4); **Pitfall 4** flags that `CombatController.ExecuteDash()` already calls `AddKillScore(false)` unconditionally on every `OnDashHit()` — including the boss's 6 non-lethal hits — which the planner must explicitly account for |

## Standard Stack

No new packages. This phase is 100% composition of already-installed, already-used engine features.

### Core (already installed, already used identically elsewhere in this codebase)

| Component | Version | Purpose | Why Standard (for this project) |
|---------|---------|---------|--------------|
| UnityEngine (Coroutines) | 6000.3.11f1 | Telegraph→Vulnerable loop, hit-reaction sequencing | Every existing enemy FSM (`MeleeEnemy`, `RangedEnemy`) and every effect pipeline (`EnemyDeathEffect`, `EnemySpawnEffect`) is coroutine-driven with `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` — this is the established, locked project convention (CLAUDE.md, STATE.md "Technical Constraints to Enforce Every Phase") |
| `SpriteRenderer.color` | n/a (built-in) | Vulnerability tint (D-02), hit flash (D-06), highlight interop | Already the sole mechanism `CombatController.UpdateHighlight()`/`ClearHighlight()` and `MeleeEnemy.ClearHighlight()`/`RangedEnemy.ClearHighlight()` use — introducing a second mechanism (MaterialPropertyBlock) for one boss instance would fork conventions for no measurable benefit (see Don't Hand-Roll) |
| `com.unity.inputsystem` | 1.19.0 | N/A directly — no new input this phase | Already locked project-wide |
| `com.unity.render-pipelines.universal` | 17.3.0 | 2D rendering, SpriteMask (death effect reuse) | Already locked project-wide |

### Supporting

| Component | Purpose | When to Use |
|---------|---------|-------------|
| `Physics2D.OverlapCircle` w/ pre-allocated `Collider2D[]` buffer | Player detection (reused from `MeleeEnemy`/`RangedEnemy` `IsPlayerInRange()`) | Every per-frame detection check — never `FindObjectsOfType`/LINQ (STATE.md constraint) |
| `ParticleSystem` (procedural, `AddComponent`-based) | Boss death particle burst (D-08 extension) | Reuse `EnemyDeathEffect.SpawnDeathParticles()` pattern, scaled up |
| `CameraFollow.Shake(duration, amplitude)` | Boss-death emphasis (D-08 "더 과장됨") | Already `Time.unscaledDeltaTime`-based (verified in `CameraFollow.cs:87-93`) — safe to call from `BossEnemy`'s death branch exactly like `CombatController.ExecuteDash()` already does for normal hits |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `SpriteRenderer.color` tinting | `MaterialPropertyBlock` + shader color property | MPB is the "more correct" mobile-perf-conscious answer for many-instance scenarios, but (1) SRP Batcher compatibility is itself contested for `SpriteRenderer` in URP 2D (see State of the Art), (2) this project has exactly one boss on screen at a time (BOSS-02, solo combat guaranteed by design), so the SRP-batching argument that motivates MPB doesn't apply, and (3) it would be the only enemy class in the codebase using a different color-mutation mechanism, breaking `ClearHighlight()`'s existing polymorphic contract. **Recommendation: use `SpriteRenderer.color`, consistent with every other enemy.** |
| Single-coroutine pattern loop (recommended) | State machine framework / Animator-driven FSM | Project has zero FSM libraries installed and CLAUDE.md's simplicity-first + surgical-changes directives explicitly forbid introducing new abstractions for a single-boss prototype phase |
| Private `_isDefeated` bool separate from `IsAlive` | Overload `IsAlive` alone for both "vulnerable" and "dead" | Conflating them makes `OnDashHit()` unable to distinguish "hit landed outside vulnerable window due to dash-travel race" from "hit landed on an already-fully-dead boss" — see Pitfall 2. A second bool costs one field and removes an entire class of race bug. |

**Installation:** None required — all dependencies already present in `Packages/manifest.json` (verified 2026-07-15: `com.unity.inputsystem` 1.19.0, `com.unity.render-pipelines.universal` 17.3.0, `com.unity.modules.physics2d` 1.0.0, `com.unity.modules.particlesystem` 1.0.0 all confirmed installed).

## Architecture Patterns

### Recommended Project Structure

No new folders. Follows existing convention exactly:

```
Assets/Scripts/Enemy/
├── IEnemy.cs                  # UNCHANGED — 3-member contract, BossEnemy implements as-is
├── ISpawnGatable.cs            # UNCHANGED — BossEnemy implements as-is (reuses EnemySpawnEffect verbatim)
├── MeleeEnemy.cs               # UNCHANGED — reference/copy source for BossEnemy's telegraph structure
├── RangedEnemy.cs              # UNCHANGED — reference only
├── EnemyDeathEffect.cs         # EXTEND (small, additive) — expose boss-intensity knobs (see Pitfall 5)
├── EnemySpawnEffect.cs         # UNCHANGED — already enemy-type-agnostic (Phase 14 D-11 premise)
└── BossEnemy.cs                 # NEW — this phase's primary deliverable

Assets/Scripts/World/
├── ScoreManager.cs             # EXTEND (additive) — new const + method beside AddKillScore()
└── DebugRoomTeleporter.cs      # EXTEND (additive) — new _bossPrefab field, same pattern as _meleePrefab
```

### Pattern 1: Telegraph→Vulnerable Coroutine Loop (reuse of `MeleeEnemy.TelegraphAndAttack()`)

**What:** A single owned `Coroutine` field drives the entire pattern; the FSM enum's `Telegraph`/`Vulnerable` cases are coroutine-owned (no per-frame `Update()` logic for those states), exactly like `MeleeEnemy.Telegraph`/`Attack`.

**When to use:** For the boss's main repeating pattern loop (D-05: single pattern, repeats indefinitely until 7th hit).

**Example (structural skeleton, adapted from `MeleeEnemy.cs:188-245`):**
```csharp
// Source: Assets/Scripts/Enemy/MeleeEnemy.cs:188-245 (existing, verified pattern in this repo)
private enum BossState { Telegraph, Vulnerable, HitReaction, Dead }
private BossState _state = BossState.Telegraph;
private Coroutine _patternCoroutine;
private int _hitCount;
private bool _isDefeated; // separate from IsAlive — see Pitfall 2

private IEnumerator PatternLoop()
{
    while (true)
    {
        // -- Telegraph (D-01/D-04: move at reduced speed while telegraphing) --
        _state = BossState.Telegraph;
        IsAlive = false; // not vulnerable — CombatController skip check excludes boss from targeting
        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (_isDefeated) yield break;
            // move at telegraphSpeedMultiplier, FlipSprite — mirrors MeleeEnemy:206-211
            yield return null;
        }

        // -- Vulnerable (D-02: stop + color change; D-03: 0.8-1.2s) --
        _state = BossState.Vulnerable;
        IsAlive = true; // targetable — CombatController now includes boss in FindNearestEnemyInRange()
        _rb.linearVelocity = Vector2.zero;
        _sr.color = vulnerableTintColor;
        float vulnElapsed = 0f;
        while (vulnElapsed < vulnerableDuration)
        {
            vulnElapsed += Time.unscaledDeltaTime;
            if (_isDefeated) yield break; // OnDashHit() already stopped this coroutine by the time this could matter
            yield return null;
        }

        // Vulnerable window closed without a hit — restore tint, loop back to Telegraph
        IsAlive = false;
        if (!IsHighlightedByPlayer) _sr.color = Color.white; // see Pitfall 3 for the highlight-interop nuance
    }
}
```

### Pattern 2: Vulnerability Gating via `IsAlive` (locked decision, D-locked in ROADMAP)

**What:** `IsAlive` is read by exactly one external consumer — `CombatController.FindNearestEnemyInRange()`'s `if (enemy == null || !enemy.IsAlive) continue;` (line 401). Setting `IsAlive = false` during Telegraph and `IsAlive = true` during Vulnerable requires **zero changes** to `CombatController`.

**When to use:** Confirmed compatible — this is the entire mechanism BOSS-03 depends on.

**Verified integration point (read, not modified):**
```csharp
// Source: Assets/Scripts/Player/CombatController.cs:397-401 (read-only reference — do not modify)
var enemy = _hitBuffer[i].GetComponent<IEnemy>();
// Skip dead enemies — physics broadphase may lag behind collider.enabled=false (Pitfall 6)
if (enemy == null || !enemy.IsAlive) continue;
```
This comment ("Skip dead enemies") is stale relative to the boss's new semantics ("skip non-vulnerable"), but the code itself needs no edit — only the comment could optionally be updated to acknowledge the dual meaning (`IsAlive` = "not dead AND not currently invulnerable" for `BossEnemy`). Flag this for the planner as a documentation nicety, not a functional requirement.

### Pattern 3: Highlight/Vulnerability Color Interop (new pattern this phase — no existing precedent)

**What:** `CombatController.UpdateHighlight()` writes `sr.color = Color.red` directly to whatever `IEnemy` is currently nearest while the player holds Attack, and `ClearHighlight()` (called per-enemy) resets to a hardcoded `Color.white`. `MeleeEnemy`/`RangedEnemy` can get away with a hardcoded white in `ClearHighlight()` because their sprite color has no other owner. `BossEnemy` is the first enemy type where the sprite color has TWO simultaneous owners: the FSM's own vulnerability tint (D-02) and the shared highlight system.

**When to use:** `BossEnemy.ClearHighlight()` MUST NOT hardcode white — see Pitfall 3.

### Pattern 4: Death Sequence Intensity Extension (D-08)

**What:** `EnemyDeathEffect` currently exposes exactly two `[SerializeField]` knobs (`_maskRiseDuration`, `_particleColor`) and hardcodes everything else (`Burst(0f, 12)` particle count, `startSpeed = 3f`, etc.) directly in `SpawnDeathParticles()`. Since it's added via `gameObject.AddComponent<EnemyDeathEffect>()` at runtime (not a prefab reference), there is no Inspector to tune per-instance — the only way to make the boss's death "bigger" is to add a small public configuration surface.

**Recommended minimal-diff approach:**
```csharp
// Source: Assets/Scripts/Enemy/EnemyDeathEffect.cs (existing file — additive change only)
// Add ONE public method, called only by BossEnemy right after AddComponent, before StartCoroutine:
public void ConfigureIntensity(float maskRiseDuration, Color particleColor, int particleBurstCount)
{
    _maskRiseDuration = maskRiseDuration;
    _particleColor = particleColor;
    _particleBurstCount = particleBurstCount; // NEW field, default 12, replaces hardcoded literal in SpawnDeathParticles()
}
```
`MeleeEnemy`/`RangedEnemy` continue calling `PlayDeathSequence()` with zero changes (defaults unchanged) — this is a pure additive, surgical diff consistent with CLAUDE.md's "정밀한 변경" principle. Camera shake is NOT part of `EnemyDeathEffect` at all today — `BossEnemy` must call `CameraFollow.Shake()` directly from its own death branch (same call site pattern `CombatController.ExecuteDash()` already uses).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Generic FSM / state machine | A reusable `StateMachine<T>` class or ScriptableObject-based FSM framework | `enum` + `switch` in `Update()`, coroutine-owned states — exact `MeleeEnemy`/`RangedEnemy` pattern | Project has zero FSM abstractions installed; CLAUDE.md simplicity-first explicitly forbids introducing unrequested flexibility for a single boss type in a prototype phase |
| Vulnerability/targetability flag | A new `IVulnerable` interface or extending `IEnemy` | Reuse `IsAlive` exactly as the locked ROADMAP Implementation Notes specify | `IEnemy` is an explicitly locked 3-member contract (`IEnemy.cs:1-17`) — modifying it breaks `MeleeEnemy`/`RangedEnemy` binary compatibility for zero benefit, since `CombatController` only ever reads `IsAlive` |
| Hit-count/health tracking | Any `HealthComponent`, `IDamageable`, or numeric HP system | A single private `int _hitCount` field with a `>= 7` threshold check | REQUIREMENTS.md Out of Scope explicitly excludes HP bars/multi-phase combat — "HP 시스템은 이 게임 어디에도 존재하지 않음." Building anything resembling a health system, even hidden, over-engineers past what BOSS-04/BOSS-05 ask for |
| Color/tint feedback delivery | `MaterialPropertyBlock`, Shader Graph exposed color property, or a new `TintController` utility | Direct `SpriteRenderer.color` writes, mirroring `ClearHighlight()`'s existing pattern in every other enemy class | Solo-boss encounter (BOSS-02) removes the batching argument for MPB; introducing a second color-mutation mechanism forks the `IEnemy.ClearHighlight()` contract's implicit behavior across enemy types |
| Boss visual distinction | New sprite import pipeline / `Unity_AssetGeneration` art generation | `transform.localScale` multiplier + `SpriteRenderer.color` tint on the existing Melee/Ranged sprite, applied at prefab-build time or in `Awake()` | D-10 explicitly locks this — new art is out of scope for Phase 15 |
| Death/spawn sequencing | A new `BossDeathEffect`/`BossSpawnEffect` class written from scratch | `EnemyDeathEffect.ConfigureIntensity()` (additive extension) + `ISpawnGatable`/`EnemySpawnEffect` used completely unmodified | `EnemySpawnEffect`'s doc comment (`EnemySpawnEffect.cs:5-9`) explicitly states it was built enemy-type-agnostic "Phase 16 BossEnemy 재사용 전제" — this premise is now due; duplicating logic here would waste an already-paid design investment |

**Key insight:** Every piece this phase needs already exists in the codebase in a directly reusable form. The engineering risk is entirely in the *seams* between systems (timing races, shared mutable state like `SpriteRenderer.color`), not in writing new mechanics from scratch. Treat this phase as an integration exercise, not a greenfield build.

## Common Pitfalls

### Pitfall 1: Off-by-one in the 7-hit threshold
**What goes wrong:** `_hitCount++` followed by `if (_hitCount > 7)` kills the boss on the 8th hit, not the 7th; conversely `if (_hitCount >= 7)` checked *before* incrementing kills on the 6th.
**Why it happens:** Classic fencepost error, made worse because "hit 7 = kill" reads ambiguously as either "the 7th increment crosses the threshold" or "count equals 7 exactly."
**How to avoid:** Increment first, then compare with `>=`: `_hitCount++; if (_hitCount >= 7) { Die(); } else { ResetPattern(); }`. Roadmap Success Criterion 3 is explicit and testable: "정확히 7회 피격 시 처치되며, 6회까지는 처치되지 않는다" — verify by counting hits 1-6 (survives) then hit 7 (dies) in playtest.
**Warning signs:** Boss survives an 8th hit, or dies on the 6th — both indicate the increment/compare ordering is wrong.

### Pitfall 2: `OnDashHit()` race between dash-commit and vulnerable-window-end
**What goes wrong:** `CombatController.DashOrWhiff()` re-validates `cachedTarget.IsAlive` at the moment the Attack button is released (`CombatController.cs:236`), but `ExecuteDash()`'s travel time (`dashDuration = 0.15f`, real-time after `ExitSlowMotion()`) elapses *before* `target.OnDashHit()` is actually called (`CombatController.cs:308`). If the boss's `Vulnerable` window ends and its `PatternLoop()` coroutine flips `IsAlive = false` during that ~0.15s window, `OnDashHit()` fires on a boss whose `IsAlive` is already `false`. If `BossEnemy.OnDashHit()` mirrors `MeleeEnemy`'s guard (`if (!IsAlive) return;`), the hit is silently swallowed — but `CombatController.ExecuteDash()` has ALREADY unconditionally played the slash SFX, hit-spark, camera shake, and called `ScoreManager.AddKillScore()` by the time this would happen (`CombatController.cs:308-312`, all of which execute regardless of what `OnDashHit()` internally does). The player would see/hear/score a "successful hit" that the boss's own state silently ignored.
**Why it happens:** `IsAlive` was repurposed to mean two different things across the boss's lifetime (transient "not vulnerable right now" vs. terminal "permanently dead"), but `OnDashHit()`'s natural instinct (copied from `MeleeEnemy`) is to guard on that same flag.
**How to avoid:** Introduce a separate one-way `_isDefeated` bool. `OnDashHit()` should guard ONLY on `_isDefeated` (`if (_isDefeated) return;`), never on `IsAlive`/vulnerability. Once `CombatController` has committed to calling `OnDashHit()` on a specific boss instance, that hit must always register (increment `_hitCount`, stop the pattern coroutine, play D-06 reaction, schedule D-07 reset) regardless of whether the vulnerable window has technically closed by the time the coroutine arrives. This is the single most important non-obvious finding of this research.
**Warning signs:** Playtest reports of "I definitely hit the boss (saw the slash, heard the sound, score went up) but the pattern didn't reset / hit counter didn't move" — especially near the tail end of the vulnerable window.

### Pitfall 3: `ClearHighlight()` hardcoded-white stomps the boss's own vulnerability tint
**What goes wrong:** `CombatController.ExitAttackPending()` calls `_lastHighlighted.ClearHighlight()` whenever the player releases/cancels an attack-pending state (`CombatController.cs:220-224`). Every existing `IEnemy.ClearHighlight()` implementation hardcodes `sr.color = Color.white` (`MeleeEnemy.cs:103-107`, `RangedEnemy.cs:108-112`). If the boss is mid-`Vulnerable` (tinted, e.g., yellow per D-02) and becomes the `_lastHighlighted` target, then the player cancels the attack-pending state (holds Attack, sees the boss highlighted red via `UpdateHighlight()`, then releases Roll instead of Attack, or the gauge empties without a dash), `ClearHighlight()` fires and resets the boss to pure white — losing the D-02 vulnerability tint even though the boss is still, in fact, vulnerable.
**Why it happens:** The highlight system (`red` on select, `white` on deselect) was designed assuming the underlying sprite has no other color state to preserve — true for `MeleeEnemy`/`RangedEnemy`, false for `BossEnemy`.
**How to avoid:** Override `ClearHighlight()` in `BossEnemy` to restore the boss's *current FSM-appropriate* color rather than a hardcoded literal:
```csharp
public void ClearHighlight()
{
    _sr.color = (_state == BossState.Vulnerable) ? vulnerableTintColor : Color.white;
}
```
**Warning signs:** Boss visually "loses" its vulnerability tint after a cancelled attack-pending sequence while still being targetable — a purely visual bug (D-02's dual-signal requirement silently degrades to single-signal, since the stop-in-place cue would still be present but the color cue would be wrong).

### Pitfall 4: Every non-lethal boss hit also grants the normal `KillScore` (100pts) — verify this is intended
**What goes wrong (or: what needs an explicit decision):** `CombatController.ExecuteDash()` calls `ScoreManager.AddKillScore(isRespawnKill)` unconditionally after every `OnDashHit()` (`CombatController.cs:312`), with no knowledge of enemy type. Since the roadmap explicitly locks "no `CombatController` changes" for targeting, this call site is very likely to stay untouched — meaning each of the boss's 7 hits (not just the killing 7th) will add +100 to score via the existing path, and the boss-specific bonus (D-09, 500-1000) stacks on top only on the 7th. Net score per full boss kill would be `7 × 100 + bossBonus` = 1200–1700, not just the bonus.
**Why it happens:** `CombatController` has no concept of "boss" — it treats every `OnDashHit()` identically, by design (that's the reusability that made BOSS-03 cheap to implement).
**How to avoid:** This is not necessarily a bug — REQUIREMENTS.md BOSS-06 only specifies a bonus "on kill," it does not forbid per-hit score. Flag this explicitly for the planner/user as a design decision point rather than silently "fixing" it (which would require touching the locked-untouched `CombatController`). If per-hit scoring during boss fights is undesired, the alternative requires either (a) `CombatController` special-casing boss targets before calling `AddKillScore` (contradicts the "no `CombatController` changes" premise), or (b) `ScoreManager.AddKillScore` accepting a "is this the boss and not yet dead" signal — both are bigger changes than this phase's locked scope implies. **Recommendation: accept the stacking as intended behavior** (each hit already feels rewarding per the core value pillar — "손을 떼면 적에게 돌진해 한 방에 처치하는 손맛"), and document the actual total score in the phase's plan for user awareness.
**Warning signs:** None at runtime — this is a design-intent question, not a functional defect. Surface it during planning, not during a bug hunt.

### Pitfall 5: `EnemyDeathEffect`'s runtime-`AddComponent` pattern has no Inspector — intensity knobs must be code-configured, not tuned in-editor
**What goes wrong:** Because `MeleeEnemy`/`RangedEnemy`/`BossEnemy` all call `gameObject.AddComponent<EnemyDeathEffect>()` at runtime rather than referencing a pre-configured prefab component, there is no way to tune `_maskRiseDuration`/`_particleColor` per-enemy-type through the Inspector — any per-type variation (D-08's "boss extension") must be passed programmatically via a new public method (see Architecture Patterns §4), not by creating a second prefab variant.
**Why it happens:** The `AddComponent` pattern was chosen (Phase 3/999.x) specifically so death VFX doesn't require per-prefab setup — a reasonable tradeoff that now costs a small amount of extra plumbing for boss-specific intensity.
**How to avoid:** Add the `ConfigureIntensity()`-style method (additive, default-preserving) rather than trying to differentiate via prefab-level `[SerializeField]` values that will never be read (since the component is always freshly `AddComponent`-ed, never a serialized prefab reference).
**Warning signs:** Boss death looks visually identical to a regular enemy death despite D-08's "more exaggerated" requirement — the giveaway that `EnemyDeathEffect`'s hardcoded defaults were never actually overridden.

### Pitfall 6: Coroutine leak from starting a new pattern coroutine without stopping the old one
**What goes wrong:** If `OnDashHit()`'s hit-reaction-then-reset logic calls `StartCoroutine(HitReactionAndRestart())` without first `StopCoroutine`-ing the currently-running `_patternCoroutine`, both coroutines run concurrently — the old `PatternLoop()` iteration continues moving/timing the boss while the new hit-reaction sequence also runs, causing visually contradictory state (e.g., boss re-enters `Telegraph` movement while still playing the hit-flash) and, if repeated across multiple hits without cleanup, an accumulating pile of dead-but-still-yielding coroutines.
**Why it happens:** Coroutines are not automatically cancelled by starting a new one — `StartCoroutine` always returns a new independent coroutine; only an explicit `StopCoroutine(oldRef)` (or `StopAllCoroutines()`) halts the old one. Verified against Unity's own coroutine documentation and community-reported pitfalls (see Sources).
**How to avoid:** Follow the exact discipline already used by `MeleeEnemy._attackCoroutine`/`RangedEnemy._telegraphCoroutine`: a single `Coroutine _patternCoroutine` field, always reassigned via `if (_patternCoroutine != null) StopCoroutine(_patternCoroutine); _patternCoroutine = StartCoroutine(NextPhase());` — never call `StartCoroutine` for boss-pattern logic without going through this single field.
**Warning signs:** Boss appears to "glitch" between states after being hit (e.g., briefly both flashing and still telegraphing), or Profiler shows a growing coroutine count over a long play session.

## Code Examples

### 1. Hit registration with race-safe defeat guard (addresses Pitfall 2 + Pitfall 1)
```csharp
// New in BossEnemy.cs — pattern combines MeleeEnemy.OnDashHit() structure (IEnemy.cs:12-13 contract)
// with a defeat flag distinct from the vulnerability-overloaded IsAlive.
public void OnDashHit()
{
    if (_isDefeated) return; // guards ONLY terminal death, never the transient vulnerability flag — Pitfall 2

    _hitCount++;
    if (_patternCoroutine != null) StopCoroutine(_patternCoroutine); // Pitfall 6: always stop before starting next

    if (_hitCount >= 7) // Pitfall 1: >= after increment, not > or a pre-increment check
    {
        _isDefeated = true;
        IsAlive = false; // ensure CombatController can no longer select this boss at all
        Die();
        return;
    }

    _patternCoroutine = StartCoroutine(HitReactionAndReset()); // D-06 + D-07
}
```

### 2. Death sequence, boss-extended (D-08, addresses Pitfall 5)
```csharp
// New in BossEnemy.cs — mirrors MeleeEnemy.OnDashHit() death branch (MeleeEnemy.cs:96-100)
private void Die()
{
    if (_rb != null) { _rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static; }
    foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
    _animator?.SetBool("isDead", true);

    var deathEffect = GetComponent<EnemyDeathEffect>();
    if (deathEffect == null) deathEffect = gameObject.AddComponent<EnemyDeathEffect>();
    deathEffect.ConfigureIntensity(bossMaskRiseDuration, bossParticleColor, bossParticleBurstCount); // NEW additive call — see Architecture Patterns §4
    StartCoroutine(deathEffect.PlayDeathSequence(_animator));

    _cameraFollow?.Shake(bossDeathShakeDuration, bossDeathShakeAmplitude); // CameraFollow.cs:32, already unscaledDeltaTime-based
    ScoreManager.AddBossKillScore(); // D-09, new sibling method — see example 4
}
```

### 3. `ScoreManager` boss bonus (D-09, sibling pattern to existing `AddKillScore`)
```csharp
// Additive change to Assets/Scripts/World/ScoreManager.cs — same style as existing AddKillScore()
public const int BossKillScore = 750; // D-09: within locked 500-1000 guideline, exact value Claude's Discretion

public static void AddBossKillScore()
{
    Score += BossKillScore;
}
```

### 4. `DebugRoomTeleporter` boss field (D-11, additive)
```csharp
// Additive change to Assets/Scripts/World/DebugRoomTeleporter.cs
// Mirrors _meleePrefab/_rangedPrefab (DebugRoomTeleporter.cs:18-19) exactly.
[SerializeField] private GameObject _bossPrefab;

// In TeleportToRoom() — EnemySpawner currently only distinguishes EnemyType.Melee/Ranged (EnemySpawner.cs:15).
// A boss test-spawn does NOT need to go through EnemySpawner's Melee/Ranged dichotomy at all for isolated
// FSM testing (D-11 explicitly scopes this to "즉시 스폰/테스트" — simplest correct approach is a direct
// Instantiate call alongside the existing spawner loop, NOT a new EnemySpawner.EnemyType.Boss enum value,
// since EnemySpawner's room-integration role is explicitly Phase 16/17 scope, not this phase's.
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|---------------|--------|
| N/A — no prior boss implementation in this codebase | This is the first boss; all "state of the art" here is internal-to-project convention, not industry drift | Phase 15 (this phase) is the origin point | N/A |

**Deprecated/outdated:** None. All referenced APIs (`Physics2D.OverlapCircle`, `SpriteRenderer.color`, `Coroutine`/`StartCoroutine`/`StopCoroutine`, `ParticleSystem`, `SpriteMask`) are current, non-deprecated Unity 6 APIs, and all are already in active use elsewhere in this exact codebase — there is no drift risk since the "standard" here is the project's own established pattern, not an external ecosystem that could have moved on since Claude's training cutoff.

**One external claim worth flagging (LOW→MEDIUM confidence, verified via WebSearch):** `SpriteRenderer` compatibility with the URP SRP Batcher has historically been inconsistent, and modifying shader properties via `MaterialPropertyBlock` is documented to explicitly break SRP Batcher eligibility for a given renderer. This supports (does not contradict) the recommendation to keep using `SpriteRenderer.color` directly rather than introducing MPB — MPB would not even deliver the mobile-perf benefit it's normally chosen for, in a URP 2D project, and would cost SRP-batcher compatibility. Confidence is MEDIUM (community discussion, not a single authoritative doc page) but the practical conclusion (don't introduce MPB here) is HIGH confidence given the solo-boss, no-batching-pressure context.

## Open Questions

1. **Should the boss grant `ScoreManager.AddKillScore(false)` (+100) on every non-lethal hit, in addition to the D-09 bonus on the 7th?**
   - What we know: `CombatController.ExecuteDash()` calls `AddKillScore()` unconditionally on every `OnDashHit()`, and the roadmap locks "no `CombatController` changes."
   - What's unclear: Whether this stacking (7×100 + 500-1000 = 1200-1700 total) is the intended design, or an unconsidered side effect.
   - Recommendation: Treat as intended (matches the core "each hit feels rewarding" value pillar) unless the user/planner explicitly wants it suppressed — suppressing it would require touching the locked-untouched `CombatController`, which is out of this phase's scope as currently defined. Surface this explicitly in the plan for a quick user confirmation rather than silently deciding.

2. **Does `EnemySpawner` need a `Boss` case in its `EnemyType` enum this phase, or is a direct `Instantiate()` in `DebugRoomTeleporter` sufficient for D-11's isolated test scope?**
   - What we know: D-11 explicitly scopes boss testing to `DebugRoomTeleporter` only, not `WorldGenerator`/room-content integration (that's Phase 16/17). `EnemySpawner.Spawn()` currently branches on `EnemyType.Melee`/`Ranged` only.
   - What's unclear: Whether the planner should add a third `EnemyType.Boss` case now (slightly ahead of when it's strictly needed, but avoids a rename/rework in Phase 16) or keep `BossEnemy` spawning fully outside `EnemySpawner` for this phase (minimal, but Phase 16 will need to revisit `EnemySpawner` anyway per BOSS-01/BOSS-02 requirements).
   - Recommendation: Keep boss spawn logic in `DebugRoomTeleporter` as a direct `Instantiate()` this phase (simplest, matches "정밀한 변경" — don't touch `EnemySpawner`'s public contract for a feature Phase 16 will properly design). This avoids speculative `EnemySpawner` changes for a room-integration feature not yet designed.

3. **Exact melee attack implementation for D-01: simple dash-hitbox vs. `MeleeEnemy`'s full windup-then-brief-hitbox pattern?**
   - What we know: D-01 says "reference `MeleeEnemy`'s melee hitbox attack pattern"; Claude's Discretion explicitly covers "단순 돌진 vs 히트박스 스윕."
   - What's unclear: Whether the boss's attack should deal damage to the player identically to `MeleeEnemy` (instant player death via `PlayerController.TriggerDeath()` on hitbox overlap — this game has no player HP either, per the core one-shot-kill-both-ways design) or whether boss attacks need any visual differentiation beyond scale/color (D-10).
   - Recommendation: Reuse `MeleeEnemy.OnTriggerEnter2D()`'s exact hitbox-overlap → `PlayerController.TriggerDeath()` pattern verbatim — this game's established rule is one-shot-kill in both directions, and there's no stated reason for the boss to be an exception. Planner should confirm this reading is correct.

## Environment Availability

Skipped — this phase has no external dependencies beyond already-installed Unity packages (all verified present in `Packages/manifest.json`, Step 2.6 audit confirms no new tool/service/runtime is required).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | `com.unity.test-framework` 1.6.0 is installed (`Packages/manifest.json:20`), but **zero test files exist under `Assets/`** — no EditMode/PlayMode test assembly has ever been created in this project across 15 phases |
| Config file | none — no `.asmdef` test assembly under `Assets/Scripts/` |
| Quick run command | N/A — no automated harness exists |
| Full suite command | N/A |

**Honest assessment:** This project's actual, established verification convention — consistently used across every prior phase (13, 14, 999.2, 999.3, 999.4, per STATE.md's "플레이테스트 체크리스트 전부 통과" entries) — is **manual playtest checklists mapped to numbered Success Criteria**, executed live in the editor via `DebugRoomTeleporter`'s isolated test rooms, not automated NUnit tests. Introducing an automated test assembly now, unrequested, would contradict CLAUDE.md's simplicity-first/surgical-changes directives (adding infrastructure beyond what's needed for this phase) and would deviate from 15 phases of established precedent. **Recommendation: continue the manual playtest checklist convention for this phase**, mapped explicitly to ROADMAP Success Criteria 1-5 below, rather than introducing automated Unity Test Framework tests as a new precedent.

### Phase Requirements → Manual Verification Map
| Req ID | Behavior | Verification Type | Manual Steps | Pass Criteria |
|--------|----------|-----------|-------------------|-------------|
| BOSS-03 | Telegraph→Vulnerable loop; only targetable when vulnerable | Manual playtest (isolated `DebugRoomTeleporter` boss room) | Hold Attack while boss is Telegraphing — confirm boss is NOT highlighted/selectable; hold Attack while boss is Vulnerable — confirm boss IS highlighted red and dash-targetable | Boss never selected as dash target outside the Vulnerable window, across ≥5 loop cycles |
| BOSS-04 | Exactly 7 hits to kill, pattern resets on each non-lethal hit | Manual playtest | Land dash hits 1-6, confirm boss survives and pattern visibly restarts from Telegraph after each (with D-07 pause); land hit 7, confirm death sequence plays | Boss dies on exactly the 7th hit, not before, not after (Pitfall 1 regression check) |
| BOSS-05 | No progress UI exposed | Manual playtest + code review | Visually scan HUD/screen during a full boss fight; grep codebase for any new UI Text/Canvas binding to `_hitCount` | Zero UI elements reference hit count anywhere |
| BOSS-06 | Score bonus on kill | Manual playtest | Note `ScoreManager.Score` before boss fight, confirm it increases by the expected total (bonus + any per-hit stacking per Open Question 1) after the 7th hit | Score increases by the documented expected amount at the moment of the 7th hit |

### Sampling Rate
- **Per task commit:** Manual spot-check of the specific state transition just implemented (e.g., after adding Telegraph→Vulnerable, verify the loop visually in-editor)
- **Per wave merge:** Full manual playtest checklist above, run once per boss encounter in the `DebugRoomTeleporter` isolated test room
- **Phase gate:** All 4 rows of the verification map pass before `/gsd:verify-work`

### Wave 0 Gaps
None — no test infrastructure setup is required or recommended (see Test Framework assessment above). The isolated test environment itself (`DebugRoomTeleporter` + boss prefab field) IS this phase's Wave 0-equivalent deliverable, per locked decision D-11.

## Sources

### Primary (HIGH confidence — direct repository inspection)
- `Assets/Scripts/Enemy/MeleeEnemy.cs` (full file read) — 4-state FSM structure, `TelegraphAndAttack()` coroutine, `OnDashHit()`/`ClearHighlight()` implementations
- `Assets/Scripts/Enemy/RangedEnemy.cs` (full file read) — second FSM reference, confirms pattern generality across enemy types
- `Assets/Scripts/Enemy/IEnemy.cs` (full file read) — locked 3-member contract
- `Assets/Scripts/Enemy/ISpawnGatable.cs` (full file read) — additive interface pattern precedent
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` (full file read) — death sequence, runtime `AddComponent` pattern, hardcoded intensity values
- `Assets/Scripts/Enemy/EnemySpawnEffect.cs` (full file read) — enemy-type-agnostic spawn VFX, explicit boss-reuse premise in doc comment
- `Assets/Scripts/World/EnemySpawner.cs` (full file read) — `Spawn()`/`Activate()` two-stage pattern, `EnemyType` enum
- `Assets/Scripts/Player/CombatController.cs` (full file read) — `FindNearestEnemyInRange()` line 364-425, `UpdateHighlight()`/`ExitAttackPending()` line 210-441, `ExecuteDash()`/`DashOrWhiff()` line 229-318 (source of Pitfall 2 and Pitfall 4)
- `Assets/Scripts/World/ScoreManager.cs` (full file read) — `AddKillScore()` sibling pattern
- `Assets/Scripts/World/DebugRoomTeleporter.cs` (full file read) — `_meleePrefab`/`_rangedPrefab` field pattern, `TeleportToRoom()` spawn loop
- `Assets/Scripts/Camera/CameraFollow.cs` (full file read) — `Shake()` signature, confirmed `Time.unscaledDeltaTime`-based
- `Packages/manifest.json` — confirmed installed package versions
- `.planning/REQUIREMENTS.md`, `.planning/phases/15-fsm/15-CONTEXT.md`, `.planning/STATE.md`, `.planning/ROADMAP.md` §Phase 15 — locked decisions, requirement text, prior milestone conventions

### Secondary (MEDIUM confidence — WebSearch, cross-checked against project's own established convention)
- Unity coroutine `StopCoroutine`/`StartCoroutine` restart discipline (null-check before stop, cache-and-reassign single reference) — confirmed to match this codebase's existing `_attackCoroutine`/`_telegraphCoroutine` pattern already in production use
- URP SRP Batcher vs. `SpriteRenderer`/`MaterialPropertyBlock` compatibility discussion — supports (does not overturn) the recommendation to keep using direct `SpriteRenderer.color`

### Tertiary (LOW confidence)
- None — no unverified claims are load-bearing in this document. Every architectural recommendation is traceable to an existing, working pattern already committed in this repository.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — zero new dependencies, everything sourced from installed packages + existing in-repo usage
- Architecture: HIGH — every pattern is a direct read of working, committed code in this exact repository, not inference from documentation
- Pitfalls: HIGH for Pitfalls 1, 3, 5, 6 (directly derivable from reading the exact interacting code paths); MEDIUM-HIGH for Pitfall 2 (the race window is real and traceable through `CombatController`'s exact call sequence, though its practical frequency depends on tuned timing values not yet chosen); this is flagged as the most important finding regardless of exact frequency, since the failure mode is silent and would be confusing to debug post-hoc

**Research date:** 2026-07-15
**Valid until:** Indefinite for the architectural findings (internal-to-project, not subject to external ecosystem drift) — re-verify only if `CombatController.cs`, `IEnemy.cs`, or `EnemyDeathEffect.cs` are modified by an intervening phase before Phase 15 execution begins.
