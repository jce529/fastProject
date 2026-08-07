---
phase: 25
slug: camera-system-overhaul
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-08-07
---

# Phase 25 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — no `.asmdef`/PlayMode or EditMode test assembly exists under `Assets/`. `com.unity.test-framework` 1.6.0 is installed but unused by this project. |
| **Config file** | none |
| **Quick run command** | N/A — no automated harness |
| **Full suite command** | N/A — no automated harness |
| **Estimated runtime** | N/A |

This matches the project's established verification pattern for every prior camera/feel-sensitive phase (Phase 18, 18.1, 999.4): structured **manual playtest checklists**, executed and reported by the user. Camera "feel" (lead intensity, zoom snappiness, catch-up aggressiveness) is inherently a subjective/tunable-by-playtest property, not a unit-testable one — introducing an automated test harness for this phase would be scope creep beyond CLAUDE.md's "단순성 우선" principle for a prototype-stage feel-tuning phase.

---

## Sampling Rate

- **After every task commit:** N/A — no automated command exists; executor performs a source-level self-check (compiles, no console errors) instead
- **After every plan wave:** Manual playtest checklist covering the wave's decisions (see Per-Task Verification Map)
- **Before `/gsd:verify-work`:** Full manual checklist (all D-01~D-10 rows + regressions) must pass
- **Max feedback latency:** N/A (manual)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 25-01-* | TBD | TBD | D-01/D-02/D-03 | manual | — (visual/feel judgment: aim-lead always active, ~15-25% screen size, own SmoothDamp, default-0 = instant) | N/A | ⬜ pending |
| 25-01-* | TBD | TBD | D-04 | manual | — (observe during repeated dash-chain playtest: lead suppressed during dash, resumes after) | N/A | ⬜ pending |
| 25-01-* | TBD | TBD | D-05/D-06/D-07/D-08 | manual | — (chain-dash playtest: zoom triggers on distance+speed, caps ~9, asymmetric in/out, zoom-in starts post-HitFreeze) | N/A | ⬜ pending |
| 25-01-* | TBD | TBD | D-09/D-10 | manual | — (deliberately outrun camera near room edges: catch-up only outside quartile 1/4, exponential curve) | N/A | ⬜ pending |
| 25-01-* | TBD | TBD | Regression: DebugSceneCameraBinder | manual | — (Play DebugScene, confirm camera frames boss room correctly) | N/A | ⬜ pending |
| 25-01-* | TBD | TBD | Regression: CameraBound clamp | manual | — (playtest in Room_Combat/Room_Dodge at both zoom extremes, confirm camera doesn't escape room bounds) | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Task IDs will be finalized once the planner assigns actual plan/wave numbers.*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements — no test framework, no stubs. Do not introduce an automated test harness for this phase (see Test Infrastructure rationale above).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|--------------------|
| Aim-lead offset direction/intensity | D-01, D-02, D-03 | Feel/subjective — no numeric pass/fail threshold defined by CONTEXT.md | Move mouse in all directions during normal play; confirm camera visibly leads toward cursor by roughly 15-25% of screen size, with no perceptible input lag at default (0) SmoothDamp setting |
| Lead offset suppression during dash | D-04 | Requires observing a live dash sequence's camera behavior | Chain 3+ dash-kills in a row; confirm lead offset drops to 0 the instant a dash starts and smoothly resumes after each dash completes |
| Dynamic zoom trigger + asymmetric damping | D-05, D-06, D-07, D-08 | Zoom "feel" (speed of zoom-out vs zoom-in) is a tuning judgment, not a fixed value | Perform dashes at varying distances/speeds; confirm zoom-out is fast and zoom-in is slow, orthoSize never visibly exceeds ~9, and zoom-in begins immediately after HitFreeze ends |
| Tension catch-up onset + curve | D-09, D-10 | Screen-quartile boundary crossing and curve shape must be observed live | Deliberately move/dash toward a room edge until player enters the outer 25% on either side; confirm camera stays static until that boundary, then catches up with increasing (not constant) speed as distance grows |
| DebugSceneCameraBinder regression | (Phase 18.1 precedent risk) | Requires loading DebugScene and observing camera framing | Play DebugScene; confirm `SnapToRoom` still frames the boss test room correctly with no camera drift/misalignment |
| CameraBound clamp at both zoom extremes | (Pitfall 3 from research) | Requires visually checking for out-of-bounds exposure | At orthoSize 7 (rest) and orthoSize ~9 (max zoom), pan across Room_Combat and Room_Dodge; confirm no area outside CameraBound's `_size` becomes visible |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies — N/A here; all tasks route to manual verify per project convention (no test framework exists)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify — N/A; this phase uses 100% manual verification by design, consistent with prior camera/feel phases
- [x] Wave 0 covers all MISSING references — no Wave 0 needed, no framework gaps
- [x] No watch-mode flags
- [x] Feedback latency < N/A (manual)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
