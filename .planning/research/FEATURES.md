# Feature Landscape

**Domain:** Mobile 2D platformer action game — slow-motion dash-attack prototype
**Project:** Fast (가칭)
**Researched:** 2026-05-27
**Confidence:** HIGH (core game feel patterns), MEDIUM (competitive differentiation), HIGH (anti-features for prototype)

---

## Table Stakes

Features mobile 2D platformer action game players expect. Missing any of these and players disengage within the first session.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Responsive left/right movement | Every platformer. Sub-100ms input feel is the baseline bar. | Low | Coyote time (~80ms after platform edge) reduces frustration significantly |
| Reliable jump with variable height | Single jump is fine. Must have hold-for-higher-jump or players feel cheated. | Low | Hold = higher; tap = short hop. Standard since NES era |
| Clear player state (alive/dead) | Players must know immediately when they die and why | Low | One-hit-kill makes this simple; visual flash + death screen enough |
| Attack input feedback | Button press must produce INSTANT visible/audible response | Low | No input lag or players think the button is broken |
| Enemy telegraph before attack | Mobile players cannot read fine details; enemies must clearly signal attacks | Medium | Melee: wind-up animation, Ranged: aim indicator line. Already in spec |
| Fall / out-of-bounds handling | "Where do I go when I fall?" — must be answered clearly | Low | Spec has fall-to-last-platform; this is above average, counts as table stakes |
| Death + restart flow | Players must be able to retry within 3 seconds of seeing death screen | Low | Single tap restart. Delay = frustration = uninstall |
| On-screen controls that do not obstruct | Buttons placed outside main action area, thumb-reachable | Medium | Landscape 1920x1080: place controls bottom-left (move/jump) and bottom-right (attack/roll) |
| Visual clarity (player vs enemy vs background) | Silhouette style helps here. Player must be instantly distinguishable | Low | Spec's silhouette style is a good fit; HIGH confidence this works |
| Camera following player | Camera must track without lag or players feel disoriented | Low | Simple follow with slight lead in movement direction is standard |
| Progress indicator | "How high am I?" — the tower's floor number answers this; players need it | Low | HUD floor counter. Already in spec |

---

## Differentiators

Features that make this specific game stand out. Not expected, but high value when done well. Ordered from highest to lowest expected impact.

### 1. Slow-Motion Attack Targeting (Core Mechanic — PRIMARY DIFFERENTIATOR)

**What it is:** Hold attack → time slows → attack range indicator appears → release → dash-kill to nearest enemy.

**Why it differentiates:** Time manipulation as a player-controlled tool tied to offensive commitment is rare in mobile games. PC/console examples (Superhot, Katana Zero, Sifu's focus parry) exist but almost no mobile games do this well. The hold/release input maps naturally to touch.

**What makes it feel good (based on game-feel research):**
- Slow-motion must be genuinely slow: 0.05–0.15x time scale. Too close to 1.0 and it feels like nothing.
- The transition into slow-mo needs a distinct audio cue (bass drop, heartbeat, muffled sound) + a brief (0.05–0.1s) camera push-in or chromatic aberration pulse.
- The dash-kill must be FAST — near-instant traversal to enemy. Players entering slow-mo to attack should feel violently decisive on release.
- Hit-freeze on kill: 3–6 frame freeze of all motion (Unity: `Time.timeScale = 0` for 50–100ms) is mandatory for satisfying hit feel.
- Screen shake on kill: short, sharp (0.1–0.15s), translational (not rotational). Rotational shake causes nausea on mobile.

**Complexity:** High (time-scale management with multiple interacting systems)

**Dependencies:** Requires time-scale system, attack range visualization, target selection logic, dash-to-target movement

---

### 2. Roll During Slow-Motion (Decision Depth)

**What it differentiates:** Allowing roll (with invincibility) while time is slowed creates a genuine 3-way decision per attack cycle: commit to attack / dodge the incoming threat / miss (whiff penalty). Most mobile action games have no meaningful choice in this window.

**Why it feels good:** The I-frames during roll give players a "cheat death" feeling. The cost is losing the attack opportunity + spending roll cooldown. This is the risk/reward loop that creates emergent skill expression.

**Complexity:** Medium (roll input must be polled during slow-mo; cooldown tracking)

**Dependencies:** Requires slow-motion system + roll system to be independent but co-aware

---

### 3. One-Hit-Kill Both Ways (Tension Architecture)

**What it differentiates:** Symmetrical lethality is uncommon. Most mobile action games give players several HP bars. One-hit-kill means every moment of play is high-stakes, which amplifies the value of slow-motion decision-making.

**Why it works:** Without HP bars, the slow-motion "planning window" becomes genuinely necessary, not just stylish. Players who skip it feel the consequence immediately. This tightens the feedback loop that the prototype is trying to validate.

**Risk:** One-hit-kill on mobile can feel unfair if enemy telegraphs are insufficient. Must pair with clear enemy telegraph timing. If playtesters say "I couldn't react," the problem is telegraph clarity, not the one-hit system.

**Complexity:** Low (simpler than HP bars) — but requires well-tuned telegraph durations

**Dependencies:** Enemy AI state machines (telegraph → attack), player death + fall-recovery system

---

### 4. Whiff Penalty (No Target in Range)

**What it differentiates:** If attack finds no target, the player enters a longer recovery animation (whiff). This rewards range selection and positioning. Very few mobile games punish missed attacks — they just do nothing.

**Why it differentiates:** Creates incentive to use slow-motion "correctly" (position before releasing). Differentiates skilled play from button mashing.

**Complexity:** Low (variant animation state + extended cooldown)

**Dependencies:** Target selection logic must return null-target case

---

### 5. Time-Stop Gauge (Metered Resource)

**What it differentiates:** Having a depleting gauge that recovers on kills creates an "offensive momentum" loop: kill to maintain slow-mo capability → slow-mo to kill safely → repeat. This is the roguelite kill-chain concept applied to a time mechanic.

**Why it works:** Players who play aggressively are rewarded with more slow-mo. Passive players are punished by gauge depletion. This biases the player toward the game's intended playstyle without hard-requiring it.

**Complexity:** Medium (gauge UI, drain logic, kill-recovery hook, behavior when depleted)

**Dependencies:** Slow-motion system, kill event system, HUD

---

### 6. Attack Type Pre-Selection (Linear vs Fan)

**What it differentiates:** Asking players to choose attack shape before a run creates a lightweight build-selection layer with zero complexity. This is the "pick your playstyle" that mobile players respond well to.

**Linear:** Higher range, narrower — rewards precise positioning
**Fan:** Wider arc — more forgiving, better vs groups, less range

**Complexity:** Low (two attack shape configs, shared systems)

**Dependencies:** Attack range visualization must support both shapes

---

### 7. Fall Recovery to Last Platform (Not Death)

**What it differentiates:** Replacing fall-death with fall-recovery-to-last-platform removes the most common frustration source in mobile platformers (accidental falls from imprecise touch). Players can take risks with positioning. This is player-friendly without removing consequence (brief stun + brief invincibility, costs time).

**Complexity:** Low (track last-safe-platform position, teleport + brief stun)

**Dependencies:** Platform contact tracking

---

### 8. Camera-Gated Enemy Activation

**What it differentiates:** Enemies only activate after the camera finishes rising to the new floor. This prevents the "surprise kill from off-screen" problem common in vertical climbers. Players always have a reaction window.

**Complexity:** Low (activation flag triggered by camera transition completion)

**Dependencies:** Floor transition system, camera animation

---

## Anti-Features

Features to deliberately NOT build in this prototype. Inclusion would waste build time, pollute the validation signal, or harm the core mechanic's feel.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| HP / health bar | Removes tension architecture; the one-hit system IS the design | Keep one-hit-kill; if it feels unfair, fix telegraphs |
| Combo counter / combo system | Adds score complexity that distracts from mechanic validation | Measure validation by "did the attack feel good," not score |
| Permanent upgrades / progression | Introduces confounds — is the mechanic fun, or just the upgrades? | Validate core mechanic first; add later if prototype succeeds |
| Weapon variety / multiple attack types mid-run | Too many variables obscure what's working | Pre-select attack type per run, keep it constant |
| Boss encounters | Requires unique design that is out of scope for mechanic validation | Standard floor enemies are sufficient |
| Double jump / wall jump / air dash | Additional movement tools change the positioning game entirely | Basic jump only; validate with minimum viable movement |
| Separate movement dash button | Blurs the line between attack-dash and movement-dash; pollutes control schema | Attack-dash only; roll is the evasion tool |
| Shop / currency system | Zero validation value; pure distraction | Omit entirely |
| Leaderboard / social features | Prototype does not need retention hooks; needs signal, not social | Omit entirely |
| Tutorial overlay / onboarding UI | This is a prototype for internal validation, not new-user onboarding | Direct playtest with verbal instruction is sufficient |
| Parry / counter mechanic | Secondary defensive option competing with roll | Roll is the single defensive tool; keep it simple |
| Enemy variety beyond 2 types | Diminishing validation return for additional enemy design work | Melee + ranged is sufficient to validate all 6 questions |
| Persistent save state between sessions | Prototype is a per-session loop; no save needed | Restart from floor 1 on every launch |
| Sound design beyond functional cues | Polish obscures whether the mechanic works bare | Placeholder SFX only (attack, hit, death, floor transition) |

---

## Feature Dependencies

```
Slow-Motion System
  ├── Attack Range Visualization (linear / fan)
  │     └── Target Selection Logic
  │           ├── Dash-to-Target Movement (+ on-kill hit-freeze)
  │           └── Whiff Animation (no target found)
  ├── Time-Stop Gauge (drain / kill-recovery / depleted behavior)
  └── Roll Input (polled during slow-mo; shares cooldown state)

Enemy System
  ├── Melee Enemy (approach → telegraph → attack → one-hit-kill both ways)
  └── Ranged Enemy (aim indicator → projectile → one-hit-kill both ways)

Floor System
  ├── Preset Floor Generation (3–5 presets)
  ├── Floor Transition (teleport → camera rise → activation flag)
  │     └── Camera-Gated Enemy Activation
  └── Last-Platform Tracker → Fall Recovery

HUD
  ├── Floor Counter
  ├── Time-Stop Gauge Display
  └── Attack Type Indicator

Death / Restart
  ├── Player Death (flash → death screen)
  └── Restart (floor 1 reset)
```

---

## MVP Recommendation

For the prototype validation goal (6 questions), prioritize in this order:

**Must ship first (validation-critical):**
1. Slow-motion attack system with hold/release input — this is the entire point
2. Dash-to-target with hit-freeze — "feel" lives here
3. Attack range visualization (both shapes) — required for Q2
4. Whiff penalty — required for whiff-vs-hit contrast
5. Roll with I-frames (standard + during slow-mo) — required for Q1 tension
6. One-hit-kill both ways — required for Q3
7. Melee enemy + ranged enemy with telegraphs — required for Q3, Q5
8. Fall-recovery to last platform — required for Q6
9. Floor transition + camera-gate — required for Q4

**Must ship but lower risk:**
10. Time-stop gauge (auto-recover + kill-recovery)
11. 3–5 floor presets
12. HUD (floor counter, gauge, attack type)
13. Death screen + restart

**Defer entirely (out of prototype scope):**
- Attack type pre-selection can initially hardcode one type and add the selector at the end once both shapes work
- Screen shake and audio cues: placeholder is sufficient for Q1 validation; polish later

---

## Sources and Confidence

| Claim | Confidence | Basis |
|-------|------------|-------|
| Time-scale 0.05–0.15x for "genuine" slow-mo feel | HIGH | Documented in GDC talks (Brace Yourself Games, Vlambeer) and Unity game-feel literature |
| Hit-freeze 3–6 frames / 50–100ms | HIGH | Vlambeer's "Game Feel" talk (GDC 2013); widely replicated in successful action games |
| Screen shake: translational not rotational on mobile | HIGH | Motion sickness research; standard mobile game design practice |
| Mobile touch: bottom-left move, bottom-right action | HIGH | iOS/Android HIG standards + every successful mobile action game (Dead Cells Mobile, Pascal's Wager, etc.) |
| One-hit-kill tension architecture | MEDIUM | Extrapolated from PC titles (Katana Zero, Hotline Miami); mobile adaptation less documented |
| Kill-chain resource loop (kill → reward → kill) | HIGH | Roguelite design canon; applies across platforms |
| Whiff penalty creating skill differentiation | MEDIUM | Game design principle; less empirically validated for mobile specifically |
| Camera-gated enemy activation preventing off-screen deaths | HIGH | Common practice in vertical mobile games; documented in multiple postmortems |
| Anti-features list | HIGH | Directly follows from prototype validation goal; not building distractors is standard prototype discipline |
