---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
current_phase: 1 — Foundation & Movement
current_plan: None (not started)
status: unknown
last_updated: "2026-05-27T13:43:15.862Z"
progress:
  total_phases: 4
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
---

# Project State: Fast (가칭)

*Single source of truth for project memory across sessions.*

---

## Project Reference

**Core Value:** 공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.

**Prototype Goal:** Single combat test room. Validate the hold-to-aim / release-to-dash feel before building floors or progression.

**Current Milestone:** v1 — Combat Test Room

---

## Current Position

**Current Phase:** 1 — Foundation & Movement
**Current Plan:** None (not started)
**Phase Status:** Not started

```
Progress: [ ] Phase 1  [ ] Phase 2  [ ] Phase 3  [ ] Phase 4
           |___________|___________|___________|___________|
                  0%                                    100%
```

**Phase Goals:**

- Phase 1: Player moves responsively, falls recover to last platform
- Phase 2: Hold-aim / release-dash loop + gauge + roll + hit-freeze fully playable
- Phase 3: Melee and ranged enemies with telegraph, FSM, one-shot-kill both ways
- Phase 4: HUD always visible, death screen, restart in under 3 seconds

---

## Performance Metrics

| Metric | Target | Current |
|--------|--------|---------|
| v1 Requirements mapped | 13/13 | 13/13 |
| Phases completed | 4 | 0 |
| Plans completed | TBD | 0 |

---

## Accumulated Context

### Key Decisions Locked

| Decision | Rationale |
|----------|-----------|
| Infrastructure merged into Phase 1 | No v1 requirements map to pure infrastructure; merging keeps phase count honest for prototype scope |
| MOVE-03 (roll) in Phase 2 not Phase 1 | Roll is a combat tool (slow-mo usable, i-frames), not a movement primitive — belongs with the combat system it interacts with |
| FEEL-01 (hit-freeze) in Phase 2 | Must ship with the attack system — hit-freeze is the punctuation of the kill, cannot be validated separately |
| Floor system deferred to v2 | User decision: validate combat feel in a single test room first |
| Phase 5 polish not created | No v1 requirements are polish-only; tuning lives inside Phase 2-3 success criteria |

### Technical Constraints to Enforce Every Phase

- `Time.unscaledDeltaTime` for ALL i-frame timers, cooldowns, coroutines
- `Physics2D.OverlapCircleNonAlloc()` — never `FindObjectsOfType` or LINQ in Update
- Animator Transition Duration = 0 for all action-state transitions
- `Rigidbody2D`: Continuous collision detection + Interpolate mode
- Dash: `MovePosition()` over 2-3 frames, never a velocity spike
- Invincibility: layer swap (PlayerHurtbox / PlayerInvincible), never IgnoreLayerCollision

### Open Empirical Questions (resolve during playtest)

- Optimal slow-motion timeScale value (research suggests 0.15-0.25x)
- Gauge drain rate vs. auto-regen balance
- Linear vs. fan attack shape — which feels better on mobile
- One-hit-kill fairness perception on mobile
- Cinemachine 3.x API status — if uncertain, use manual LateUpdate camera

### Todos

- [ ] Create SampleScene test layout (one flat floor with fall zones) before Phase 1 plan
- [ ] Confirm Cinemachine package version before Phase 1 plan
- [ ] Set up layer matrix (PlayerHurtbox, PlayerInvincible, Enemy, EnemyProjectile, Platform) before coding begins

### Blockers

None.

---

## Session Continuity

**How to resume after /clear:**

1. Read `.planning/STATE.md` (this file) — current position and decisions
2. Read `.planning/ROADMAP.md` — phase goals and success criteria
3. Read `.planning/REQUIREMENTS.md` — requirement details and traceability
4. Check which phase plan exists in `.planning/` (e.g., `PLAN-phase-1.md`)
5. Continue from Current Phase listed above

**Last session:** 2026-05-27T13:43:15.857Z

---
*State initialized: 2026-05-27*
*Last updated: 2026-05-27 after roadmap creation*
