---
phase: 15
slug: fsm
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-07-15
---

# Phase 15 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | `com.unity.test-framework` 1.6.0 is installed (`Packages/manifest.json:20`), but zero test files exist under `Assets/` — no EditMode/PlayMode test assembly has ever been created in this project across 15 phases |
| **Config file** | none — no `.asmdef` test assembly under `Assets/Scripts/` |
| **Quick run command** | N/A — no automated harness exists |
| **Full suite command** | N/A |
| **Estimated runtime** | N/A |

This project's established, consistent-across-15-phases verification convention is **manual playtest checklists mapped to numbered Success Criteria**, executed live in the Unity editor via `DebugRoomTeleporter`'s isolated test rooms — not automated NUnit tests. Introducing an automated test assembly now, unrequested, would contradict CLAUDE.md's simplicity-first/surgical-changes directives. This phase continues that convention.

---

## Sampling Rate

- **After every task commit:** Manual spot-check of the specific state transition just implemented (e.g., after adding Telegraph→Vulnerable, verify the loop visually in-editor)
- **After every plan wave:** Full manual playtest checklist below, run once per boss encounter in the `DebugRoomTeleporter` isolated test room
- **Before `/gsd:verify-work`:** All 4 rows of the Per-Task Verification Map must pass
- **Max feedback latency:** N/A — manual, immediate (in-editor Play mode)

---

## Per-Task Verification Map

| Req ID | Behavior | Test Type | Manual Steps | Pass Criteria |
|--------|----------|-----------|---------------|----------------|
| BOSS-03 | Telegraph→Vulnerable loop; only targetable when vulnerable | manual (DebugRoomTeleporter boss room) | Hold Attack while boss is Telegraphing — confirm boss is NOT highlighted/selectable; hold Attack while boss is Vulnerable — confirm boss IS highlighted red and dash-targetable | Boss never selected as dash target outside the Vulnerable window, across ≥5 loop cycles |
| BOSS-04 | Exactly 7 hits to kill, pattern resets on each non-lethal hit | manual | Land dash hits 1-6, confirm boss survives and pattern visibly restarts from Telegraph after each (with D-07 pause); land hit 7, confirm death sequence plays | Boss dies on exactly the 7th hit, not before, not after |
| BOSS-05 | No progress UI exposed | manual + code review | Visually scan HUD/screen during a full boss fight; grep codebase for any new UI Text/Canvas binding to `_hitCount` | Zero UI elements reference hit count anywhere |
| BOSS-06 | Score bonus on kill only (no per-hit stacking, D-12) | manual | Note `ScoreManager.Score` before each of hits 1-6, confirm it is UNCHANGED after each (D-12 self-cancel via `SubtractScore`); note score before hit 7, confirm it increases by the documented boss-kill total after the 7th hit | Score is flat across hits 1-6; increases by exactly the documented amount at the moment of the 7th hit |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. The isolated test environment itself (`DebugRoomTeleporter` + boss prefab field, D-11) IS this phase's Wave 0-equivalent deliverable — it must exist before any other manual verification can run, so the plan should sequence the `DebugRoomTeleporter` boss-prefab field early (Wave 1) ahead of tasks that depend on visually testing `BossEnemy`.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Telegraph→Vulnerable visual loop timing/readability | BOSS-03 | No automated test harness exists in this project (15 phases, all manual); FSM correctness is best judged by eye against the "readable rhythm" intent behind D-03 | Play `DebugRoomTeleporter` boss room, observe ≥5 full loop cycles, confirm stop+color-change signal (D-02) is unambiguous |
| 7-hit kill threshold + per-hit pattern reset | BOSS-04 | Same — no test framework; also inherently requires live dash-hit input from the Input System, which isn't automatable without a harness this project doesn't have | Land dash hits 1 through 7 sequentially in Play mode, observe reset behavior after each of hits 1-6 and death sequence on hit 7 |
| Score bonus isolation (D-12 self-cancel) | BOSS-06 | `ScoreManager.Score` has no automated assertion path; verifying the +100/-100 cancel-out requires watching the live score HUD across a full 7-hit sequence | Watch on-screen score value across all 7 hits; it must not move on hits 1-6, then jump by the documented total on hit 7 |
| Progress non-exposure | BOSS-05 | Absence-of-UI is a visual/code-review check, not something a test assertion meaningfully covers for this prototype-scale project | Scan screen during full fight + grep for `_hitCount` outside `BossEnemy.cs` |

---

## Validation Sign-Off

- [x] All tasks have manual verify or Wave 0 dependencies (no automated framework exists in this project; consistent with 15 prior phases)
- [x] Sampling continuity: manual spot-check after every task commit, full checklist after every wave
- [x] Wave 0 covers all MISSING references (DebugRoomTeleporter boss field is itself the Wave 0 deliverable)
- [x] No watch-mode flags (N/A — no automated framework)
- [x] Feedback latency < immediate (in-editor Play mode manual check)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
