---
phase: 4
slug: hud-game-loop
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-16
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Editor Play Mode (NUnit test runner deliberately removed — quick task 260604-vst) |
| **Config file** | None active |
| **Quick run command** | N/A — editor Play Mode manual verification |
| **Full suite command** | N/A |
| **Estimated runtime** | ~2 min per cycle (manual) |

---

## Sampling Rate

- **After every task commit:** Verify in Play Mode per task checklist below
- **After every plan wave:** Run full 5-cycle die-restart loop
- **Before `/gsd:verify-work`:** All manual checklist items must pass
- **Max feedback latency:** 2 minutes

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 04-01-T0 | 01 | 0 | UI-01 | Manual editor | TMP import + FloorManager.cs created | ❌ W0 | ⬜ pending |
| 04-01-T1 | 01 | 1 | UI-01 | Manual Play Mode | Floor label shows "Floor 1" on HUD | ❌ W0 | ⬜ pending |
| 04-01-T2 | 01 | 1 | UI-01 | Manual Play Mode | Gauge fill bar drains/refills in sync with actual gauge | ❌ W0 | ⬜ pending |
| 04-01-T3 | 01 | 1 | UI-01 | Manual Play Mode | Attack type label shows "LINEAR" or "FAN" correctly | ❌ W0 | ⬜ pending |
| 04-02-T1 | 02 | 2 | UI-02 | Manual Play Mode | Death screen panel appears within 1s of player death, game pauses | ❌ W0 | ⬜ pending |
| 04-02-T2 | 02 | 2 | UI-02 | Manual Play Mode | Restart returns to floor 1 with HUD initialized (gauge full, floor 1) | ❌ W0 | ⬜ pending |
| 04-02-T3 | 02 | 2 | UI-02 | Manual Play Mode | 5 consecutive die-restart cycles complete without dev intervention | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Import TMP Essential Resources: `Window > TextMeshPro > Import TMP Essential Resources` in Unity Editor (human editor action — required before any TMP component renders)
- [ ] `Assets/Scripts/World/FloorManager.cs` — new file (stub acceptable)
- [ ] `Assets/Scripts/UI/HUDController.cs` — new file
- [ ] `Assets/Scripts/UI/DeathScreenController.cs` — new file
- [ ] Canvas + HUDPanel + DeathPanel GameObject hierarchy in SampleScene — editor action (human required for scene wiring and Inspector field assignment)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Floor "Floor 1" label visible on HUD | UI-01 | Unity UI render — no automated query | Enter Play Mode, check top of screen |
| Gauge fill bar syncs with CombatController.GaugeValue | UI-01 | No test framework active | Hold attack button, observe bar draining |
| Attack type label shows correct value | UI-01 | AttackTypeSelector.Selected is static | Start game, check label matches selection screen |
| Death screen appears within 1s of death | UI-02 | Requires game event timing | Walk into enemy — panel must appear before 1s elapses |
| Restart resets all HUD values | UI-02 | Scene reload state verification | Restart from death screen, verify gauge=full, floor=1 |
| 5× die-restart loop w/ zero dev intervention | UI-02 | Requires real gameplay session | Run 5 consecutive cycles without touching editor |

---

## Validation Sign-Off

- [ ] All tasks have manual Play Mode verification steps defined
- [ ] Wave 0 prerequisites listed (TMP import, new script stubs, scene hierarchy)
- [ ] No automated test commands expected (NUnit runner removed per 260604-vst)
- [ ] Feedback latency < 2 min per cycle
- [ ] `nyquist_compliant: true` set in frontmatter after execution

**Approval:** pending
