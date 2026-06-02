---
phase: 2
slug: combat-core
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-02
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.6.0 (NUnit, bundled) |
| **Config file** | `Assets/Tests/PlayMode/PlayMode.asmdef` — Wave 0 creates this |
| **Quick run command** | Unity Editor: Window > General > Test Runner > Play Mode > Run Selected |
| **Full suite command** | Unity Editor: Window > General > Test Runner > Play Mode > Run All |
| **Estimated runtime** | ~30 seconds (coroutine-heavy Play Mode tests) |

---

## Sampling Rate

- **After every task commit:** Run Play Mode tests for that wave's requirement
- **After every plan wave:** Run full Play Mode suite
- **Before `/gsd:verify-work`:** Full suite must be green + ATCK-04 manual step signed off
- **Max feedback latency:** ~30 seconds per full run

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 02-01-01 | 01 | 1 | ATCK-01 | Edit Mode | Test Runner > CombatTests > SelectLinear_SetsSelectedToLinear | ❌ W0 | ⬜ pending |
| 02-01-02 | 01 | 1 | ATCK-02 | Play Mode | Test Runner > CombatTests > AttackHeld_EntersSlowMotion | ❌ W0 | ⬜ pending |
| 02-01-03 | 01 | 1 | ATCK-03 | Play Mode | Test Runner > CombatTests > DashRelease_MovesPlayerToEnemy | ❌ W0 | ⬜ pending |
| 02-01-04 | 01 | 1 | ATCK-04 | Manual | Manual validation step (see below) | N/A | ⬜ pending |
| 02-01-05 | 01 | 1 | ATCK-05 | Play Mode | Test Runner > CombatTests > Gauge_DrainsAndRegens | ❌ W0 | ⬜ pending |
| 02-01-06 | 01 | 1 | FEEL-01 | Play Mode | Test Runner > CombatTests > DashKill_TriggersHitFreeze | ❌ W0 | ⬜ pending |
| 02-02-01 | 02 | 1 | MOVE-03 | Play Mode | Test Runner > RollTests > Roll_GrantsInvincibilityAndCooldown | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Tests/PlayMode/` directory — create directory
- [ ] `Assets/Tests/PlayMode/PlayMode.asmdef` — Play Mode assembly definition referencing `Unity.TestFramework.Tests`
- [ ] `Assets/Tests/PlayMode/CombatTests.cs` — stubs for ATCK-01, ATCK-02, ATCK-03, ATCK-05, FEEL-01
- [ ] `Assets/Tests/PlayMode/RollTests.cs` — stubs for MOVE-03

*Note: Unity Test Framework 1.6.0 is already in the project — no install step needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Whiff lockout is visibly longer than kill lockout | ATCK-04 | Lockout duration comparison requires wall-clock timing; Play Mode test timer resolution insufficient for subjective feel validation | 1) Enter Play mode. 2) Position player away from all dummies. 3) Hold Attack (slow-mo starts) then release immediately (whiff). 4) Count real seconds until player can move again. 5) Repeat with a dummy in range. 6) Verify whiff lockout (target: 0.5s) is clearly longer than kill lockout (target: 0.2s). 7) Confirm whiff animation plays (distinct from idle). |
| Roll works during slow-motion | MOVE-03 (partial) | Slow-motion interaction requires actual timeScale manipulation; input simulation in tests does not easily replicate held attack + roll simultaneously | 1) Enter Play mode. 2) Hold Attack (slow-mo). 3) Press Roll (Shift). 4) Verify roll animation plays and player moves at normal visual speed. 5) Verify cooldown ticks correctly (test: cooldown should expire after ~0.8 real seconds, not ~4 scaled seconds). |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
