---
phase: 10
slug: exit-portal-floor-transition
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-07-03
---

# Phase 10 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — no NUnit/PlayMode test assembly exists in this project (only Unity package-cache tests, zero project-owned test assemblies) |
| **Config file** | none |
| **Quick run command** | N/A — established pattern (Phase 8/9 precedent) is Unity MCP Play Mode automation (`Unity_RunCommand` + `Unity_GetConsoleLogs`) combined with human visual playtesting |
| **Full suite command** | N/A |
| **Estimated runtime** | ~2-3 minutes per manual playtest pass |

---

## Sampling Rate

- **After every task commit:** Manual Play Mode smoke test in Unity Editor — enter Play mode, confirm no console errors, confirm expected `[WorldGenerator]`-tagged Debug.Log lines appear
- **After every plan wave:** Full manual playtest walkthrough (spawn portal, walk to it, confirm floor transition) — mirrors Phase 9's 09-03 plan style
- **Before `/gsd:verify-work`:** All three phase success criteria manually confirmed, exactly as Phase 9 did
- **Max feedback latency:** ~1 Play Mode session (under 5 minutes)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 10-01-xx | 01 | 0 | EXIT-01 | manual + Unity MCP | `Unity_RunCommand` + `Unity_GetConsoleLogs` scan for `[WorldGenerator] Portal spawned in {room}` | ❌ W0 | ⬜ pending |
| 10-0x-xx | TBD | TBD | EXIT-02 | manual + Unity MCP | console-log `_activeExitCount` transitions, confirm never exceeds `_maxExitsActive` | ❌ W0 | ⬜ pending |
| 10-0x-xx | TBD | TBD | EXIT-03 | manual + Unity MCP | console-log `[WorldGenerator] EnterPortal → Floor {N}` + Hierarchy scan confirming old chain destroyed | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Exact Task IDs assigned by planner once plan files are created.*

---

## Wave 0 Requirements

- [ ] Add `Debug.Log($"[WorldGenerator] Portal spawned in {room.name}")` at the exit-portal-spawn success path (EXIT-01 verification)
- [ ] Add `Debug.Log($"[WorldGenerator] _activeExitCount = {count}")` on every increment/decrement of the active portal counter (EXIT-02 verification)
- [ ] Add `Debug.Log($"[WorldGenerator] EnterPortal → Floor {FloorManager.CurrentFloor}")` at the start of the floor transition sequence (EXIT-03 verification)

No automated test framework gap to fill — this Unity gameplay prototype intentionally uses Unity MCP + manual playtesting (Phase 8/9 precedent), not an automated suite.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Portal spawn probability (0%/100% boundary) | EXIT-01 | No automated test harness in project; requires visual/console confirmation across multiple room spawns | Set `_exitSpawnChance=1.0f`, Play, walk through several rooms, confirm every room gets a portal via console log + Hierarchy. Repeat with `0.0f`, confirm none do. |
| Max simultaneous active portals | EXIT-02 | Requires extended playtest across multiple room spawns to observe concurrent portal count | Set `_maxExitsActive=1`, Play, walk past several rolled rooms, confirm active portal count in console never exceeds 1 |
| Floor transition + chain reset | EXIT-03 | Requires human observation of Hierarchy state and camera/HUD behavior during the transition coroutine | Walk into an active EXIT portal, confirm HUD floor counter increments, confirm old room+corridor chain GameObjects are destroyed, confirm new chain starts from the activated standby room |
| ENT-marker vertical teleport (folded todo) | EXIT-03 | Visual-only — confirms player doesn't spawn in empty air after floor transition | For each of the 4 previously-ENT-less Complex_Room prefabs, trigger a floor transition landing in that room and confirm the player lands on solid ground at the RoomEntry marker position |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (console-log-driven manual verification, per established project convention)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (every task gets a Play Mode smoke test)
- [x] Wave 0 covers all MISSING references (3 Debug.Log additions specified above)
- [x] No watch-mode flags
- [x] Feedback latency < 5 minutes (single Play Mode session)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
