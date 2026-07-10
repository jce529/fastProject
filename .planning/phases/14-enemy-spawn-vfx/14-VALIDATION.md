---
phase: 14
slug: enemy-spawn-vfx
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-07-10
---

# Phase 14 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None detected — no `.asmdef`, no NUnit test files, no `Tests/` directories anywhere in `Assets/` |
| **Config file** | none — manual playtest only (see Wave 0 Requirements) |
| **Quick run command** | N/A (manual playtest only) |
| **Full suite command** | N/A (manual playtest only) |
| **Estimated runtime** | N/A |

This matches the established project convention: Phase 12/13 (`13-04-PLAN.md`) validated SFX/VFX timing entirely via manual in-Editor playtest sign-off, not automated tests. This phase's success criteria (visual VFX timing, FSM gating during a coroutine window, physical walk-out motion) are inherently playtest-verifiable, not unit-testable without a Unity PlayMode test harness that doesn't currently exist in this repo.

---

## Sampling Rate

- **After every task commit:** Manual in-Editor Play Mode smoke check (enter a room ahead of time via fast player movement, observe portal VFX timing)
- **After every plan wave:** Full manual playtest pass through several rooms + corridors + at least one floor transition
- **Before `/gsd:verify-work`:** Full manual playtest sign-off against Success Criteria 1-5, mirroring the `13-04-PLAN.md` sign-off pattern
- **Max feedback latency:** N/A (manual verification, no automated test loop)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 14-*-* | TBD | TBD | SPWN-01 | manual | N/A — visually verify portal VFX plays at `Activate()` time, not pre-gen | ❌ W0 (no harness) | ⬜ pending |
| 14-*-* | TBD | TBD | SPWN-02 | manual | N/A — visually verify spawning enemy excluded from targeting/FSM until VFX completes | ❌ W0 (no harness) | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements. No automated test framework exists in this Unity project (confirmed: zero `.asmdef`, zero `Tests/` folders) — this is a pre-existing condition, not something to remediate within this phase. The project's established validation method for VFX/timing-sensitive features is manual playtest sign-off (see `13-04-PLAN.md` precedent). No Wave 0 test-infrastructure task is required; continue the existing manual-verification convention.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Portal VFX plays at room/corridor entry, not pre-gen | SPWN-01 | Visual timing/animation correctness; no PlayMode test harness exists | Move ahead so 2 rooms are pre-generated; confirm enemies stay invisible/inactive until player actually enters that room/corridor; confirm portal grow → enemy walk-out → shrink sequence plays (~1.2s) |
| Spawning enemy excluded from targeting and FSM | SPWN-02 | Requires live gameplay interaction (slow-mo + dash targeting) to observe | During a still-spawning enemy's VFX window, attempt slow-mo targeting; confirm it cannot be highlighted/dashed-to, and confirm it does not chase/attack the player until VFX completes; confirm normal FSM resumes immediately after |
| Corridor spawn parity (D-03) | SPWN-01, SPWN-02 | Depends on manual placement of `EnemySpawner` markers in Corridor prefabs and live traversal | Walk through all 3 Corridor types (Up/Flat/Down) with enemies placed; confirm identical spawn VFX/gating behavior as in Rooms |
| Multi-enemy staggered portal emergence (D-05) | SPWN-01 | Timing/visual pacing judgment call, not a pass/fail assertion | Enter a room/corridor with 2+ `EnemySpawner` markers; confirm portals appear in sequence with a visible stagger, not simultaneously |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies — N/A, manual-only phase per project convention
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify — N/A, all tasks are manual-verify by design
- [ ] Wave 0 covers all MISSING references — N/A, no Wave 0 test infra needed
- [ ] No watch-mode flags — N/A
- [ ] Feedback latency < N/A
- [ ] `nyquist_compliant: true` set in frontmatter — done

**Approval:** pending
