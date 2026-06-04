# Roadmap: Fast (가칭)

**Project:** Mobile 2D Platformer — Slow-Motion Dash-Attack Prototype
**Milestone:** v1 — Combat Test Room
**Granularity:** Standard
**Coverage:** 13/13 v1 requirements mapped

---

## Phases

- [x] **Phase 1: Foundation & Movement** - Project infrastructure + player locomotion on a static test floor (completed 2026-05-28)
- [ ] **Phase 2: Combat Core** - Slow-motion aiming, dash-kill, roll, and all gauge mechanics
- [ ] **Phase 3: Enemy System** - Melee and ranged enemies with FSM behavior and one-shot-kill logic
- [ ] **Phase 4: HUD & Game Loop** - All on-screen feedback, death screen, and restart flow

---

## Phase Details

### Phase 1: Foundation & Movement
**Goal**: A player character moves responsively on a static test floor and recovers from falls without dying
**Depends on**: Nothing
**Requirements**: MOVE-01, MOVE-02
**Success Criteria** (what must be TRUE):
  1. Player moves left/right with immediate directional response — holding the opposite direction reverses momentum within one frame, not after a slide
  2. Player can tap-jump for a short hop or hold-jump for a higher arc, with full air-direction control throughout the jump
  3. Player falling off any platform edge reappears on the last-stood platform position within half a second, with a brief visual invincibility indicator active on arrival
  4. All of the above remain stable: no physics tunneling, no stuck states, no console errors after 2 minutes of freeform testing
**Plans**: 3 plans

Plans:
- [x] 01-01-PLAN.md — Scene layout, layer matrix, and CameraFollow script
- [x] 01-02-PLAN.md — PlayerController: instant reversal, jump cut, full air control (MOVE-01)
- [x] 01-03-PLAN.md — FallDetector + InvincibilityHandler: teleport recovery + sprite flicker (MOVE-02)

---

### Phase 2: Combat Core
**Goal**: The complete hold-to-aim, release-to-dash combat loop is playable against stationary dummies, including gauge, roll, and hit-freeze
**Depends on**: Phase 1
**Requirements**: MOVE-03, ATCK-01, ATCK-02, ATCK-03, ATCK-04, ATCK-05, FEEL-01
**Success Criteria** (what must be TRUE):
  1. At game start, two buttons appear — player selects Linear or Fan attack type, and that shape is used for all range displays in the session
  2. Holding the attack button visibly slows the world (enemies, particles, everything except player responsiveness), and the selected attack shape renders clearly over the slow scene
  3. Releasing the attack button with a dummy in range causes an instant dash to that dummy, a perceptible freeze (50-100ms), then a short post-kill pause before control returns — the freeze must feel like a punctuation mark, not a stutter
  4. Releasing the attack button with no dummy in range plays a whiff animation and imposes a longer lockout than a successful kill — the penalty is clearly longer than the success delay
  5. The time-stop gauge drains while holding the attack button, auto-recovers when released, and refills visibly on each kill; depleting the gauge releases slow-motion but the player can still release the attack button to dash
  6. Roll button activates during both normal time and slow-motion, grants a brief invincibility window, and cannot be triggered again until the cooldown expires — cooldown timer runs in real time regardless of timeScale
**Plans**: 4 plans
**UI hint**: yes

Plans:
- [x] 02-01-PLAN.md — AttackTypeSelector Canvas overlay + DummyEnemy + scene layout (ATCK-01)
- [x] 02-02-PLAN.md — CombatController + GaugeController: slow-mo, dash, whiff, hit-freeze, gauge (ATCK-02/03/04/05/FEEL-01)
- [x] 02-03-PLAN.md — RangeDisplay LineRenderer + RollController with i-frames (MOVE-03, ATCK-02)
- [ ] 02-04-PLAN.md — Test infrastructure: PlayMode.asmdef + CombatTests + RollTests (all requirements)

---

### Phase 3: Enemy System
**Goal**: Two distinct enemy types patrol, telegraph, and attack — and die in one hit from the player's dash
**Depends on**: Phase 2
**Requirements**: ENMY-01, ENMY-02
**Success Criteria** (what must be TRUE):
  1. A melee enemy detects the player, closes the distance, plays a visible wind-up animation, then executes a melee attack — the telegraph is long enough that a playtester with no prior instruction can roll through it
  2. A ranged enemy detects the player, displays a visible aim indicator line, then fires a projectile along that line — a playtester can read the aim direction before the projectile launches
  3. One successful player dash-attack kills either enemy type instantly (one-shot); one melee hit or one projectile hit kills the player instantly (one-shot the other way)
  4. Both enemy types can be targeted by the attack range indicator and eliminated cleanly, with FEEL-01 hit-freeze firing on each kill
**Plans**: TBD

---

### Phase 4: HUD & Game Loop
**Goal**: All session-critical information is always visible and a player can die, see the death screen, and restart without developer intervention
**Depends on**: Phase 3
**Requirements**: UI-01, UI-02
**Success Criteria** (what must be TRUE):
  1. HUD is always visible during play: floor counter displays the current floor number, time-stop gauge reflects the actual gauge value in real time, and the selected attack type (Linear / Fan) is labeled and correct for the session
  2. When the player is killed by an enemy, the game pauses on a death screen within one second — the screen shows a restart button and nothing else required to understand what to do
  3. Tapping restart from the death screen returns the player to floor 1 in under three seconds with the HUD correctly initialized (gauge full, floor counter reset)
  4. The complete loop — enter combat, kill enemies, die, restart — can be run five consecutive times by a playtester with zero developer assistance
**Plans**: TBD
**UI hint**: yes

---

## Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation & Movement | 3/3 | Complete   | 2026-05-28 |
| 2. Combat Core | 0/4 | Planned | - |
| 3. Enemy System | 0/? | Not started | - |
| 4. HUD & Game Loop | 0/? | Not started | - |

---

## Coverage Map

| Requirement | Phase | Description |
|-------------|-------|-------------|
| MOVE-01 | Phase 1 | Fast movement + jump |
| MOVE-02 | Phase 1 | Fall recovery + invincibility |
| MOVE-03 | Phase 2 | Roll mechanic |
| ATCK-01 | Phase 2 | Attack type selection screen |
| ATCK-02 | Phase 2 | Hold = slow-mo + range display |
| ATCK-03 | Phase 2 | Release = dash-kill |
| ATCK-04 | Phase 2 | Whiff penalty |
| ATCK-05 | Phase 2 | Gauge auto-recovery + kill recovery |
| FEEL-01 | Phase 2 | Hit-freeze on kill |
| ENMY-01 | Phase 3 | Melee enemy |
| ENMY-02 | Phase 3 | Ranged enemy |
| UI-01 | Phase 4 | HUD |
| UI-02 | Phase 4 | Death screen + restart |

**v1 Coverage: 13/13 requirements mapped. No orphans.**

---

## Stack Constraints (for plan-phase reference)

- `Time.timeScale` slow-motion: always set `Time.fixedDeltaTime = 0.02f * Time.timeScale` together
- Player velocity compensation in FixedUpdate during slow-mo: `rb.linearVelocity *= (1f / Time.timeScale)`
- All i-frame timers and cooldowns: `Time.unscaledDeltaTime` only
- Enemy range queries: `Physics2D.OverlapCircleNonAlloc()` with pre-allocated array — no LINQ in Update
- Collision: `Rigidbody2D` Continuous detection + Interpolate mode
- Dash implementation: `Rigidbody2D.MovePosition()` over 2-3 frames, not a velocity spike
- Invincibility: layer swap between `PlayerHurtbox` and `PlayerInvincible` layers, not `Physics2D.IgnoreLayerCollision`
- Animator transitions for action states: Transition Duration = 0
- HUD text updates: `TextMeshProUGUI.SetText("{0}", value)` — no string allocation

---
*Roadmap created: 2026-05-27*
*Last updated: 2026-06-02 after Phase 2 planning*
