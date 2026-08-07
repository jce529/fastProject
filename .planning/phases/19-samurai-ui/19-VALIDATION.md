---
phase: 19
slug: samurai-ui
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-08-07
---

# Phase 19 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None (manual playtest checklist — established project convention; no `*.Tests.cs`/NUnit/`.asmdef` files exist anywhere in `Assets/`, `com.unity.test-framework` is an unused transitive package) |
| **Config file** | none |
| **Quick run command** | Enter Play mode in `DebugScene.unity` (`Fast/Debug/Build DebugScene` if not yet rebuilt for SAMURAI) |
| **Full suite command** | Full D-01–D-18 checklist walkthrough in `DebugScene`, mirroring Phase 18/18.1's Task 3 checkpoint pattern |
| **Estimated runtime** | ~10-15 minutes per full pass (manual, in-Editor) |

---

## Sampling Rate

- **After every task commit:** Manual smoke check in `DebugScene` for the specific mechanic just implemented (mirrors Phase 18.1's per-task checkpoint granularity).
- **After every plan wave:** Re-run the full D-01–D-18 checklist against the latest `DebugScene` build.
- **Before `/gsd:verify-work`:** All 6 Roadmap Success Criteria + D-01–D-18 confirmed via real play (not code-inspection-only) — matches the precedent set by every prior boss phase (15, 18, 18.1) in this project's history.
- **Max feedback latency:** ~1-2 minutes (Unity Editor Play mode enter/exit cycle)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 19-01-xx | 01 | 0/1 | SAMURAI-01 | manual playtest | Defeat SAMURAI in DebugScene, confirm `PlayerPrefs.GetInt("boss_unlock_Samurai")==1` persists after Play restart | ❌ W0 — `SamuraiBoss.cs` doesn't exist yet |
| 19-01-xx | 01 | 0/1 | SAMURAI-02 | manual playtest | Tap Attack with 기본전투모듈/사무라이 전투형 모듈 selected — confirm `Time.timeScale` never leaves 1 during the swing | ❌ W0 |
| 19-01-xx | 01 | 0/1 | SAMURAI-03 | manual playtest | Tap Attack aimed at SAMURAI during its parry-window projectile flight — confirm projectile destroyed/redirected, no death | ❌ W0 |
| 19-01-xx | 01 | 0/1 | SAMURAI-04 | manual playtest | Let a parry window expire with no input — confirm death; roll through one instead — confirm survival (D-05) | ❌ W0 |
| 19-01-xx | 01 | 0/1 | SAMURAI-05 | manual playtest, iterative | Multiple playtest passes adjusting window width until parry feels "fair but not trivial" per input-lag allowance | ❌ W0 — mechanic must exist first |
| 19-02-xx | 02 | 1/2 | UNLOCK-02 | manual playtest | Load module-select (lobby) screen with 기본전투모듈 always unlocked + Overclock/사무라이 전투형 모듈 conditionally unlocked — confirm each unlocked slot is selectable and loads the correct module in `SampleScene` | ❌ W0 |
| 19-02-xx | 02 | 1/2 | UNLOCK-03 | manual playtest | Fresh `PlayerPrefs` (or explicitly cleared `boss_unlock_Samurai`/`boss_unlock_Fiora`) — confirm locked slots show lock icon + are non-interactable, while 기본전투모듈 slot stays unlocked | ❌ W0 |
| 19-0x-xx | TBD | TBD | D-15/D-16 | manual playtest | Confirm 기본전투모듈 has no parry response during SAMURAI's parry-only window (only roll survives), and remains selectable in the module screen after SAMURAI is defeated | ❌ W0 |
| 19-0x-xx | TBD | TBD | D-17 | manual playtest | Confirm Overclock slot stays locked when `boss_unlock_Fiora` is unset, regardless of Samurai/basic module state | ❌ W0 |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Exact Task IDs/wave numbers TBD — plans not yet created (this table is filled in more precisely once gsd-planner produces PLAN.md files with real task IDs).*

---

## Wave 0 Requirements

- [ ] `SamuraiBoss.cs` (Assets/Scripts/Enemy/Boss/) — does not exist yet, `BossEnemyBase` subclass
- [ ] Basic combat module + parry combat module (Assets/Scripts/Player/Combat/) — net-new, e.g. shared base class + parry subclass per D-15
- [ ] `IRealtimeCombatModule` (or equivalent host-hook per research recommendation) — net-new
- [ ] `CombatModuleRegistry.cs` / `CombatModuleSelector.cs` (or equivalent N-way selection state) — net-new
- [ ] SAMURAI test room/prefab (e.g. `Room_SamuraiFsmTest.prefab` via new editor builder tool, or `DebugSceneBuilder.cs` extension) — mirrors `RoomBossFsmTestBuilder.cs`/`BossEnemyPrefabBuilder.cs` precedent from Phase 15/18
- [ ] No test framework install needed — this project does not use automated tests; do not introduce NUnit/asmdefs for this phase (inconsistent with established convention)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|--------------------|
| Parry timing window "feel" (fair but not trivial given input lag) | SAMURAI-05 | Subjective gameplay-feel tuning, inherently requires human play; no automated framework exists in this project | Iterative playtest passes in DebugScene, adjust window width `[SerializeField]` until parry consistently succeeds on a good-faith timed tap without becoming free/trivial |
| Groggy gauge accumulation pacing (D-09) | SAMURAI-04 (implicitly) | Threshold/weighting is explicitly Claude's Discretion + playtest-tuned per CONTEXT.md, no formula specified upfront | Playtest full 7-cycle groggy→hit loop, confirm pacing feels intentional (neither instant nor grindy) |
| N-way module select screen readability/lock affordance | UNLOCK-03 | Visual/UX judgment (lock icon clarity, button disabled state legibility) | Load module-select screen with mixed lock states, visually confirm locked vs unlocked slots are unambiguous |
| Boss + module flow ordering assumptions hold in DebugScene | D-15/D-16/D-17/D-18 | No real WorldGenerator/scene-transition flow exists yet (D-18 out of scope) — must confirm module/boss logic in isolation still matches intended eventual flow | Manually switch equipped module via DebugScene-side toggle (not real lobby UI) and confirm 기본전투모듈 vs 사무라이 전투형 모듈 behavior differs only by parry presence |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies — N/A, this project uses manual playtest checklists exclusively (see Test Infrastructure note); each task instead needs explicit manual verification steps as acceptance criteria.
- [ ] Sampling continuity: no 3 consecutive tasks without a manual playtest checkpoint
- [ ] Wave 0 covers all MISSING references (SamuraiBoss.cs, combat modules, registry/selector, test room)
- [ ] No watch-mode flags — N/A (no test framework)
- [ ] Feedback latency < ~2 minutes (Editor Play mode cycle)
- [x] `nyquist_compliant: true` set in frontmatter (manual-playtest form, consistent with project convention)

**Approval:** pending
