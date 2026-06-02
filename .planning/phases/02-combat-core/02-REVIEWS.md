---
phase: 2
reviewers: [gemini]
reviewed_at: 2026-06-02T00:00:00Z
plans_reviewed: [02-01-PLAN.md, 02-02-PLAN.md, 02-03-PLAN.md, 02-04-PLAN.md]
note: Claude (current runtime) skipped per independence rule — Gemini reviewed independently
---

# Cross-AI Plan Review — Phase 2: Combat Core

## Gemini Review

The overall assessment of the Phase 2 implementation plans for the "Fast" prototype is **highly positive**. The plans demonstrate a sophisticated understanding of Unity's time-scale nuances and are meticulously aligned with the requested stack constraints.

### 1. Summary

The architecture is robust, with a clear separation of concerns between state management (`CombatController`), resource management (`GaugeController`), and specialized movement (`RollController`). The decision to restore `timeScale` immediately before the dash execution is a standout technical choice that ensures physics-based movement is predictable and snappy. The plans successfully cover all six success criteria and provide a clear path to validating the core combat feel on mobile.

### 2. Strengths

- **Precision Time Coupling**: Mandatory pairing of `timeScale` and `fixedDeltaTime` is correctly applied across all systems, preventing physics jitter during slow-motion.
- **Mobile-First Performance**: Proactive use of `OverlapCircleNonAlloc` and pre-allocated arrays ensures the `FindNearestEnemy` logic won't trigger GC spikes on Android.
- **Responsive Mechanics**: Using `Time.unscaledDeltaTime` for i-frames and cooldowns ensures the player is never "cheated" by time manipulation; a 0.8s cooldown feels like 0.8s regardless of slow-mo.
- **Callback-Driven Interaction**: Moving away from physics triggers (`OnTriggerEnter`) for dash hits in favor of explicit `OnDashHit()` calls avoids "ghost hits" at high speeds.

### 3. Concerns

- **[HIGH] Dash Obstacle Collision**: `Rigidbody2D.MovePosition` respects colliders but can lead to the player getting "snagged" on the floor or walls if the dash path isn't clear. If a player dashes to an enemy behind a wall, they might stop midway, potentially failing the "kill in one hit" expectation.
- **[MEDIUM] Input State Edge Cases**: Relying on a combination of `!IsAttackDown` and `AttackReleased` in `Update` can be brittle. If the Input System's "Released" event occurs between frames or is consumed by a UI element, the player could get stuck in infinite slow-motion.
- **[MEDIUM] TimeScale Race Conditions**: The `AttackTypeSelector` pauses the world with `timeScale = 0`. If `HitFreeze` (also setting `timeScale = 0`) triggers or resolves during a UI transition, there is a risk of the world resuming prematurely or staying frozen.
- **[LOW] Reflection Fragility**: While reflection is great for testing private methods, it will break if field names are refactored. Since these are Play Mode tests, timing (e.g., `WaitForSecondsRealtime`) can be flaky depending on the target machine's frame rate.

### 4. Suggestions

- **Path Validation**: In `ExecuteDash`, perform a quick `Physics2D.Linecast` between the player and the target enemy. If an environment collider is hit, adjust the dash destination to a point slightly in front of the obstacle to ensure a "safe" landing.
- **State-Based Input**: Refactor `CombatController` to use an internal state enum (`Idle`, `Aiming`, `Dashing`, `Whiffing`). Trigger the dash simply when `state == Aiming && !IsAttackDown`, which is more reliable than checking for a specific "Release" trigger.
- **Global Pause Handling**: Implement a simple static `PauseManager` or a check in `CombatController` that prevents any `timeScale` restoration if the `AttackTypeSelector` (or a future Pause menu) is active.
- **Mobile UI Scaling**: For the landscape 1920x1080 target, ensure the `AttackTypeSelector` buttons utilize `Safe Area` components and are large enough for thumb-presses (minimum 100-150px height).

### 5. Risk Assessment

**Overall Risk: MEDIUM**

The technical logic is solid and follows all "Stack Constraints." The primary risk lies in the **subjective "feel"** of the dash. A 3-frame `MovePosition` dash at `timeScale = 1` is extremely fast (approx. 0.05s). While this prevents "slow" dashes, it may look like a teleport without a trail renderer or ghosting effect (though polish is out of scope). Ensuring the dash distance and speed "feel" right on a small mobile screen is the biggest hurdle for this phase.

**Approved for implementation with the recommendation to add simple line-of-sight checking for dashes.**

---

## Consensus Summary

*Single reviewer — Gemini. Claude skipped for independence.*

### Key Strengths (Confirmed)
- timeScale/fixedDeltaTime always paired correctly throughout all 4 plans
- `OverlapCircleNonAlloc` + pre-allocated buffer is the right call for mobile
- `OnDashHit()` called explicitly (not via physics trigger) ensures correct kill sequencing
- Unscaled timers for i-frames/cooldowns prevent slow-motion "cheating" of game mechanics

### Priority Concerns (Act On Before Execution)

| Severity | Concern | Plan | Recommended Action |
|----------|---------|------|-------------------|
| HIGH | Dash obstacle collision — MovePosition may snag on walls/floor between player and enemy | 02-02 | Add `Physics2D.Linecast` check; if blocked, either skip target or snap to obstacle-safe position |
| MEDIUM | Input edge case — `!IsAttackDown && !AttackReleased` logic brittle; could leave slow-mo stuck | 02-02 | Consider adding a `_isSlowMo && Time.unscaledTime > _slowMoStartTime + maxSlowMoDuration` safety timeout |
| MEDIUM | timeScale race condition — AttackTypeSelector sets `timeScale=0` at Start; any playback of HitFreeze before selection could interact unexpectedly | 02-01/02-02 | Already isolated by `_isBusy` and wave ordering, but worth noting for playtest |
| LOW | Reflection-based tests fragile — method/field renames silently break tests (null invoke) | 02-04 | Add null-check asserts on reflected method references: `Assert.IsNotNull(method, "CombatController.EnterSlowMotion not found")` |

### Divergent Views
*N/A — single reviewer.*

### To Incorporate This Feedback
```
/gsd:plan-phase 2 --reviews
```
