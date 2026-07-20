---
phase: 18
slug: shared-infra
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-07-20
---

# Phase 18 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> **D-04 (locked, 18-CONTEXT.md):** Manual playtest verification only for this phase — no automated test framework wiring. This overrides the default Nyquist automated-sampling expectation; `nyquist_compliant: true` reflects that the phase intentionally has no automatable behavior and the manual-only strategy is fully documented below (not a gap).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None installed/wired for this phase — `com.unity.test-framework` 1.6.0 is present in `Packages/manifest.json` but no test assembly, test folder, or test file exists anywhere in `Assets/` |
| **Config file** | none |
| **Quick run command** | N/A — manual playtest only, per D-04 |
| **Full suite command** | N/A — manual playtest only, per D-04 |
| **Estimated runtime** | N/A |

---

## Sampling Rate

- **Per task commit:** Manual Play-mode check of the specific behavior just touched (per D-04).
- **Per wave merge:** Full manual playtest pass covering all three requirements' checklists below.
- **Phase gate:** All three manual checklists pass before `/gsd:verify-work` — no automated full-suite gate exists for this phase, by explicit user decision.
- **Max feedback latency:** N/A (manual)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| — | — | — | INFRA-01 | manual | N/A | N/A | ⬜ pending |
| — | — | — | INFRA-03 | manual | N/A | N/A | ⬜ pending |
| — | — | — | UNLOCK-01 | manual | N/A | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

*Table filled in by planner once tasks are assigned to plans/waves.*

---

## Wave 0 Requirements

None — Wave 0 test-infrastructure setup is explicitly not applicable, since D-04 locks manual verification only and no test framework wiring is in scope for this phase. If a future phase (per the deferred idea "자동화 PlayMode 회귀 테스트") revisits automated testing, it would need to start from Wave 0 scratch at that time.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Overclock hold=slowmo+range, release=dash-kill behaves identically after `IPlayerCombatModule` migration | INFRA-01 | D-04 locked; no test framework wired; behavior is real-time input+physics feel, not unit-testable | Play SampleScene. Hold Attack — verify slow-motion + range indicator appear. Release near an enemy — verify dash-kill fires. Release with no target in range — verify whiff + lockout. Hold through gauge-empty — verify auto-exit-slowmo but dash still available. |
| `BossEnemyBase`-derived class usable without rewriting defeat-guard/death-sequence/spawn-gate/highlight | INFRA-03 | D-04 locked; requires live FSM interaction over multiple hits in Play mode | In `Room_BossFsmTest` (Phase 15 isolated test room), defeat the migrated boss via 7 hits. Confirm pattern resets correctly on hits 1-6. Confirm death sequence + score bonus + camera shake fire on hit 7. Confirm spawn-gate still blocks targeting during spawn VFX. |
| Boss defeat writes PlayerPrefs flag, survives app restart | UNLOCK-01 | D-04 locked; requires process restart, not testable in a single Play-mode session | Defeat the boss in `Room_BossFsmTest`. Confirm unlock flag is set (e.g. temporary `Debug.Log(BossUnlockManager.IsUnlocked(...))` or Editor PlayerPrefs inspection). Fully stop and restart Play mode (or the built player). Confirm flag still reads true. |

---

## Validation Sign-Off

- [x] All tasks have manual verify path (no automated framework exists or is in scope — D-04)
- [x] Sampling continuity: manual checklist covers every requirement, no automated gaps to track
- [x] Wave 0 covers all MISSING references (N/A — no Wave 0 needed)
- [x] No watch-mode flags
- [x] Feedback latency: N/A (manual)
- [x] `nyquist_compliant: true` set in frontmatter (manual-only strategy is the compliant strategy per D-04)

**Approval:** pending
