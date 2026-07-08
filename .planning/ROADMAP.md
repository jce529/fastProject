<details>
<summary>✅ v1.0 — Combat Test Room (완료, 접힘)</summary>

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
- [ ] **Phase 5: 절차적 맵 생성 — 무한 스테이지** - 청크 기반 절차적 생성으로 플레이어가 탑을 무한히 올라갈 수 있는 스테이지 구성

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
**Plans**: 4 plans

Plans:
- [x] 03-01-PLAN.md — IEnemy interface + DummyEnemy implements IEnemy + CombatController DummyEnemy→IEnemy migration (ENMY-01, ENMY-02)
- [x] 03-02-PLAN.md — PlayerDeath event + PlayerDeathHandler + FallDetector rewrite (D-17) + EnemyProjectile layer + Physics2D matrix (ENMY-01, ENMY-02)
- [x] 03-03-PLAN.md — MeleeEnemy FSM: patrol, chase, windup telegraph, hitbox attack (ENMY-01) *[Quick: 260624-q65, 260624-t4e, 260617-0t2]*
- [x] 03-04-PLAN.md — RangedEnemy FSM: detection/aim separation, telegraph, projectile fire (ENMY-02) *[Quick: 260617-0el, 260617-02t, 260617-0t2]*

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
| 2. Combat Core | 3/4 | In Progress | - |
| 3. Enemy System | 4/4 | Complete | 2026-06-24 |
| 4. HUD & Game Loop | 0/? | Not started | - |
| 5. 절차적 맵 생성 | 2/2 | Complete | 2026-06-26 |

---

### Phase 5: 절차적 맵 생성 — 무한 스테이지
**Goal**: 출구 트리거 기반 Room 프리팹 스폰으로 플레이어가 탑을 무한히 올라갈 수 있는 스테이지 구성. FloorSpawner가 6단계 전환 시퀀스를 실행하고 이전 층을 즉시 파괴해 모바일 메모리를 관리한다.
**Depends on**: Phase 4
**Requirements**: FLOOR-01, FLOOR-02, FLOOR-03, FLOOR-04
**Success Criteria** (what must be TRUE):
  1. 게임 시작 시 Room_Combat(1층)이 자동 스폰되고 HUD에 "Floor: 1"이 표시된다
  2. 출구 트리거를 밟으면 6단계 전환 시퀀스(입력잠금→이동→카메라→적활성→재개)가 발동된다
  3. 전환 완료 후 이전 층 GameObject가 씬에서 사라지고 현재 층만 남는다
  4. 2층 이상부터 4개 Room 중 랜덤으로 선택되며 D-07 난이도 테이블에 따라 적이 스폰된다
  5. 사망 후 재시작 시 1층으로 정상 리셋된다
**Plans**: 2 plans

**Design Notes (captured 2026-06-16):**
- 4~6종 층 레이아웃 프리팹 풀 → 가중치 랜덤 선택
- 출구 트리거(Trigger Collider2D) 감지 → FloorTransitionSequence() 코루틴
- 현재 층 + 다음 층만 유지, 이전 층 파괴 (CLAUDE.md 모바일 메모리 제약)
- 층 번호 → 적 수/배치 난이도 스케일링 (D-07)
- HUD 층 카운터(UI-01)와 연동 — FloorManager.CurrentFloor 증가로 자동 갱신

Plans:
- [x] 05-01-PLAN.md — FloorSpawner.cs + RoomExit.cs + RoomClearCondition + CameraFollow/CameraBound 전환 시퀀스 (FLOOR-02, FLOOR-03, FLOOR-04) *[Quick: 260624-lre, 260624-mcv, 260624-ml3, 260624-u3w, 260626-il8, 260626-j9b, 260626-jpm]*
- [x] 05-02-PLAN.md — Room 프리팹 7종 (Prefabs/Rooms/) + RoomPrefabBuilder 에디터 스크립트 + FloorSpawner 씬 배치 (FLOOR-01, FLOOR-02, FLOOR-03) *[Quick: 260617-jwe, 260618-u8j, 260624-oh2]*

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
| FLOOR-01 | Phase 5 | 프리셋 기반 층 생성 |
| FLOOR-02 | Phase 5 | 층 전환 시퀀스 6단계 |
| FLOOR-03 | Phase 5 | 전환 중 적 비활성화 |
| FLOOR-04 | Phase 5 | 이전 층 파괴 (모바일 메모리) |

**v1 Coverage: 13/13 requirements mapped. No orphans.**
**v2 Floor Coverage: 4/4 FLOOR requirements mapped to Phase 5.**

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
- Floor transition: `WaitForSecondsRealtime` only — `Time.timeScale` may be 0 at transition start

---

## Backlog

*(비어 있음 — 모든 항목이 활성 Phase로 승격됨)*

---

---

</details>

<details>
<summary>✅ v2.0 — 게임 시작 플로우 (완료, 접힘)</summary>

# Roadmap: Fast (가칭) — v2.0

**Milestone:** v2.0 — 게임 시작 플로우
**Granularity:** Standard
**Coverage:** 7/7 v2.0 requirements mapped

---

## v2.0 Phases

- [x] **Phase 6: MainMenu Scene** - 앱 실행 시 MainMenu가 첫 화면이 되고, Start/Quit 버튼이 동작한다 (completed 2026-06-23)
- [ ] **Phase 7: AttackSelect Scene & Scene Flow** - AttackSelect 씬에서 공격 방식을 선택하면 SampleScene이 로드되고 선택값이 유지되며, 사망 후 AttackSelect로 복귀한다

---

## v2.0 Phase Details

### Phase 6: MainMenu Scene
**Goal**: 앱을 실행하면 MainMenu가 첫 화면으로 열리고, Start/Quit 버튼이 동작한다
**Depends on**: Phase 5
**Requirements**: MENU-01, MENU-02, MENU-03
**Success Criteria** (what must be TRUE):
  1. 앱(또는 에디터 Play)을 실행하면 MainMenu.unity가 첫 화면으로 열린다 — SampleScene이 먼저 뜨지 않는다
  2. MainMenu의 Start 버튼을 누르면 AttackSelect 씬으로 전환된다
  3. MainMenu의 Quit 버튼을 누르면 앱이 즉시 종료된다 (에디터에서는 PlayMode 종료)
**Plans**: TBD
**UI hint**: yes

---

### Phase 7: AttackSelect Scene & Scene Flow
**Goal**: AttackSelect.unity 씬에서 Linear 또는 Fan을 선택하면 SampleScene이 로드되고, 선택한 공격 방식이 게임플레이에 즉시 반영되며, SampleScene 내 기존 오버레이가 제거되고, 사망 후 AttackSelect로 복귀한다
**Depends on**: Phase 6
**Requirements**: ATKS-01, ATKS-02, ATKS-03, FLOW-01
**Success Criteria** (what must be TRUE):
  1. AttackSelect 씬에서 Linear 버튼을 누르면 SampleScene이 로드되고, 범위 표시가 직선형으로 동작한다
  2. AttackSelect 씬에서 Fan 버튼을 누르면 SampleScene이 로드되고, 범위 표시가 부채꼴형으로 동작한다
  3. SampleScene 로드 후 기존 AttackTypeSelector 캔버스 오버레이가 화면에 보이지 않는다
  4. 사망 화면의 "다시 선택" 버튼을 누르면 AttackSelect 씬으로 복귀한다 — SampleScene이 재로드되지 않는다
  5. 완전한 플로우(앱 실행 → MainMenu → AttackSelect → SampleScene → 사망 → AttackSelect)를 개발자 개입 없이 연속 3회 수행할 수 있다
**Plans**: TBD
**UI hint**: yes

---

## v2.0 Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 6. MainMenu Scene | 1/1 | Complete | 2026-06-23 |
| 7. AttackSelect Scene & Scene Flow | 0/? | Not started | - |

---

## v2.0 Coverage Map

| Requirement | Phase | Description |
|-------------|-------|-------------|
| MENU-01 | Phase 6 | MainMenu.unity at Build index 0 (first screen) |
| MENU-02 | Phase 6 | Start button navigates to AttackSelect |
| MENU-03 | Phase 6 | Quit button exits app |
| FLOW-01 | Phase 7 | Death screen "다시 선택" returns to AttackSelect (index 1) |
| ATKS-01 | Phase 7 | AttackSelect scene loads SampleScene on selection |
| ATKS-02 | Phase 7 | Selected attack type persists into SampleScene |
| ATKS-03 | Phase 7 | Existing AttackTypeSelector overlay removed from SampleScene |

**v2.0 Coverage: 7/7 requirements mapped. No orphans.**

---

## v2.0 Implementation Notes (for plan-phase reference)

- MainMenuController.cs and MainMenuSceneBuilder.cs (EditorScript) already exist (committed: ec13a46, ad54d11)
- MainMenuController.OnStartClicked() currently loads SampleScene — must change to load AttackSelect (index 1) in Phase 7
- Phase 6 scope for OnStartClicked: wire to AttackSelect or use placeholder index until AttackSelect scene exists
- Attack type data passing: use PlayerPrefs or a static GameManager singleton — decide in Phase 7 plan
- Build Settings order must be: MainMenu (0), AttackSelect (1), SampleScene (2)
- DeathScreenController.SceneManager.LoadScene() must target AttackSelect (index 1), not MainMenu (index 0) — change in Phase 7
- Death screen button label: "다시 선택" (changed from "메인 메뉴") — quick task 260623-t6i-deathscreen set it to "메인 메뉴", Phase 7 updates to "다시 선택"
- AttackTypeSelector in SampleScene is a Canvas overlay — remove GameObject from scene or disable in ATKS-03

---
*v2.0 Roadmap created: 2026-06-23*
*Last updated: 2026-06-23 — v2.0 phases 6-7 added*

---

---

</details>

<details>
<summary>✅ v3.0 — 무한 복도 층 시스템 (완료, 접힘) — 전체 아카이브: .planning/milestones/v3.0-ROADMAP.md</summary>

# Roadmap: Fast (가칭) — v3.0

**Milestone:** v3.0 — 무한 복도 층 시스템
**Granularity:** Standard
**Coverage:** 12/12 v3.0 requirements mapped

---

## v3.0 Phases

- [ ] **Phase 8: 룸-길 아키텍처** - Room과 Corridor 프리팹이 마커 기반으로 체인 연결될 수 있는 아키텍처가 갖춰진다
- [ ] **Phase 9: 무한 양방향 생성 & 정리** - 플레이어 이동 방향 앞 2개 Room+Corridor가 자동 생성되고, 뒤로 2개 초과 시 자동 Destroy된다
- [x] **Phase 10: EXIT 포탈 & 층 전환** - Room 스폰 시 확률적으로 EXIT 포탈이 생성되고, 진입 시 층 번호가 올라가며 WorldGenerator가 초기화된다 (completed 2026-07-06)
- [x] **Phase 11: 타이머 & 난이도** - 층 진입마다 HUD에 카운트다운이 표시되고 시간 초과 시 게임오버, 층이 높아질수록 몬스터 수가 증가한다 (completed 2026-07-07)

---

## v3.0 Phase Details

### Phase 8: 룸-길 아키텍처
**Goal**: Room과 Corridor 프리팹이 마커 기반으로 체인 연결될 수 있는 아키텍처가 갖춰진다
**Depends on**: Phase 7
**Requirements**: ARCH-01, ARCH-02, ARCH-03
**Success Criteria** (what must be TRUE):
  1. Room 프리팹의 END_Left/END_Right Transform 마커가 씬 뷰에서 확인되고, 에디터 Gizmo로 위치가 표시된다
  2. Corridor 3종(상승/직진/하강) 프리팹이 각각 전투 구간(적 스폰 포인트 + 장애물)을 포함하여 에셋 폴더에 존재한다
  3. Corridor의 ENT 마커를 Room의 END 마커에 정렬하면 플레이어가 Room→Corridor→Room 순서로 물리적으로 막힘 없이 통과한다
  4. RoomConnector 컴포넌트가 인스펙터에서 연결 방향(Left/Right)과 연결된 오브젝트를 직렬화한다
**Plans**: 3 plans

Plans:
- [x] 08-01-PLAN.md — RoomConnector.cs + RoomMarkerTool.cs (에디터 도구) — ARCH-01, ARCH-03
- [x] 08-02-PLAN.md — CorridorBuilder.cs (Corridor 3종 프리팹 생성) — ARCH-02
- [x] 08-03-PLAN.md — 에디터 도구 실행 + SampleScene 배치 플레이테스트 검증 — ARCH-01, ARCH-02, ARCH-03
- [x] (사용자 수동 진행, ARCH-04) — 14개 Room 프리팹 SpriteRenderer+BoxCollider2D → Tilemap 방식 전환

---

### Phase 9: 무한 양방향 생성 & 정리
**Goal**: 플레이어가 이동하는 방향으로 Room+Corridor가 자동 생성되고, 뒤에 남겨진 구간은 자동으로 Destroy된다
**Depends on**: Phase 8
**Requirements**: GEN-01, GEN-02, GEN-03
**Success Criteria** (what must be TRUE):
  1. 플레이어가 오른쪽으로 이동 시 진행 방향 앞 2개의 Room+Corridor 쌍이 이미 생성되어 허공이 없다
  2. 플레이어 기준 뒤 2개를 초과하는 Room+Corridor GameObject가 씬 Hierarchy에서 사라진다
  3. 5회 Play 반복 시 새 Room 스폰마다 상승/직진/하강 Corridor 중 랜덤 선택이 동작하여 단일 타입만 반복되지 않는다
**Plans**: 3 plans

Plans:
- [x] 09-01-PLAN.md — RoomMarkerTool.cs 업데이트: 전체 14개 룸 Door/ENT(Left)+Door/EXIT(Right) RoomConnector 부착
- [x] 09-02-PLAN.md — WorldGenerator.cs 신규 구현: 무한 체인 Start/Update/SpawnNextPair/SelectCorridor/RemoveTail
- [x] 09-03-PLAN.md — Unity Editor 실행(RoomMarkerTool) + SampleScene 배치 + 5회 플레이테스트 검증

---

### Phase 10: EXIT 포탈 & 층 전환
**Goal**: Room 스폰 시 확률적으로 EXIT 포탈이 생성되고, 진입 시 층 번호가 올라가며 WorldGenerator가 초기화된다
**Depends on**: Phase 9
**Requirements**: EXIT-01, EXIT-02, EXIT-03
**Success Criteria** (what must be TRUE):
  1. _exitSpawnChance를 1.0f로 설정하면 모든 Room 스폰 시 포탈이 생성되고, 0.0f이면 생성되지 않는다
  2. _maxExitsActive를 1로 설정했을 때 씬에 활성 EXIT 포탈이 동시에 2개 이상 존재하지 않는다
  3. 플레이어가 EXIT 포탈 Collider에 진입하면 FloorNumber가 +1 증가하고 WorldGenerator가 리셋되어 새 Room+Corridor 체인이 시작된다
**Plans**: 4 plans

Plans:
- [x] 10-01-PLAN.md — ExitSpawnPoint.cs + ExitPortal.cs + ExitPortalBuilder.cs (마커/트리거 컴포넌트 계약) (EXIT-01, EXIT-03)
- [x] 10-02-PLAN.md — WorldGenerator.cs: TrySpawnExitPortal + EnterPortal + FloorTransitionSequence 6단계 코루틴 (EXIT-01, EXIT-02, EXIT-03)
- [x] 10-03-PLAN.md — Unity Editor 수동 작업: ExitPortal 프리팹 빌드 실행 + Complex_Room 6종 ExitSpawnPoint/RoomEntry 마커 배치 (EXIT-01, EXIT-03)
- [x] 10-04-PLAN.md — SampleScene Inspector 연결 + EXIT-01/02/03 플레이테스트 검증 (EXIT-01, EXIT-02, EXIT-03)
**UI hint**: yes

---

### Phase 11: 타이머 & 난이도
**Goal**: 층마다 HUD에 카운트다운이 표시되고 시간 초과 시 게임오버가 발생하며, 층이 올라갈수록 몬스터 수가 증가하고, 남은 시간에 비례한 점수가 누적된다
**Depends on**: Phase 10
**Requirements**: TIMER-01, TIMER-02, DIFF-01, SCORE-01, SCORE-02 (SCORE-01/02는 2026-07-07 discuss-phase에서 Phase 11로 편입 확정)
**Success Criteria** (what must be TRUE):
  1. 층에 진입하면 HUD 타이머가 60초에서 즉시 카운트다운을 시작하고, 슬로우모션(Time.timeScale ≈ 0.2) 중에도 실시간으로 감소한다
  2. 타이머가 0에 도달하는 순간 PlayerController.OnPlayerDeath가 발동해 기존 PlayerDeathHandler/DeathScreenController 사망 화면이 표시된다
  3. 3층에서 스폰되는 총 몬스터 수가 1층보다 눈에 띄게 많다 (기존 FloorSpawner.GetEnemyCount() 계단식 테이블 재사용)
  4. EXIT 포탈 진입 시 남은 시간(초) × 10점이 ScoreManager.Score에 누적되고 HUD에 실시간 반영된다
**Plans**: 4 plans

Plans:
- [x] 11-01-PLAN.md — FloorTimer 정적 클래스 + ScoreManager.AddTimeBonus (TIMER-01, TIMER-02, SCORE-01)
- [x] 11-02-PLAN.md — WorldGenerator 통합: 난이도 적 스폰 + 타이머 리셋/틱 + 점수 가산 배선 (TIMER-01, TIMER-02, DIFF-01, SCORE-01)
- [x] 11-03-PLAN.md — HUDController: 타이머 표시 + 점멸 경고 (TIMER-01, SCORE-02)
- [x] 11-04-PLAN.md — 에디터 도구 + SampleScene Inspector 연결 + 플레이테스트 검증 (TIMER-01, TIMER-02, DIFF-01, SCORE-01, SCORE-02)
**UI hint**: yes

---

## v3.0 Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 8. 룸-길 아키텍처 | 3/3 | Complete | 2026-07-01 |
| 9. 무한 양방향 생성 & 정리 | 3/3 | Complete | 2026-07-01 |
| 10. EXIT 포탈 & 층 전환 | 4/4 | Complete    | 2026-07-06 |
| 11. 타이머 & 난이도 | 4/4 | Complete    | 2026-07-07 |

---

## v3.0 Coverage Map

| Requirement | Phase | Description |
|-------------|-------|-------------|
| ARCH-01 | Phase 8 | Room 프리팹 END_Left/END_Right 마커 |
| ARCH-02 | Phase 8 | Corridor 3종 프리팹 (상승/직진/하강, 전투 구간) |
| ARCH-03 | Phase 8 | RoomConnector: ENT→END 체인 연결 |
| GEN-01 | Phase 9 | 앞 2개 Room+Corridor 미리 생성 |
| GEN-02 | Phase 9 | 뒤 2개 초과 Room+Corridor 자동 Destroy |
| GEN-03 | Phase 9 | Corridor 3종 랜덤 선택 |
| EXIT-01 | Phase 10 | Room 스폰 시 확률적 EXIT 포탈 생성 (기본 15%) |
| EXIT-02 | Phase 10 | 포탈 확률/최대 개수 FloorSpawner 인스펙터 노출 |
| EXIT-03 | Phase 10 | 포탈 진입 시 층 전환 + WorldGenerator 초기화 |
| TIMER-01 | Phase 11 | HUD 카운트다운 (Time.unscaledDeltaTime) |
| TIMER-02 | Phase 11 | 시간 초과 시 PlayerDeathHandler 호출 |
| DIFF-01 | Phase 11 | 층 번호 비례 EnemySpawner 카운트 증가 |
| SCORE-01 | Phase 11 | 남은 시간(초) × 10점 EXIT 진입 시 누적 |
| SCORE-02 | Phase 11 | HUD 실시간 점수 표시 (기존 _scoreLabel 재사용) |

**v3.0 Coverage: 14/14 requirements mapped. No orphans.**

---

## v3.0 Implementation Notes (for plan-phase reference)

- FloorSpawner.cs 기존 수직 층 전환 로직 → WorldGenerator로 대체 (Phase 9)
- RoomExit.cs 기존 출구 트리거 → ExitPortal 컴포넌트로 교체 (Phase 10)
- ScoreManager.cs 기존 타이머 로직 → FloorTimer 정적 클래스로 이관 (Phase 11)
- PlayerDeathHandler.cs 기존 게임오버 핸들러 → TIMER-02에서 직접 재사용
- HUDController.cs 기존 HUD → Phase 11에서 카운트다운 UI 슬롯 추가
- EnemySpawner.GetEnemyCount() 기존 난이도 테이블 → DIFF-01에서 층 번호 파라미터 확장
- 타이머는 반드시 Time.unscaledDeltaTime 사용 — 슬로우모션(Time.timeScale ≈ 0.2) 면역 필수
- WaitForSecondsRealtime 코루틴: 층 전환 시퀀스 전 구간에 걸쳐 타임스케일 면역 보장

### Phase 12: 포탈 진입/퇴장 애니메이션 구현 (10-TRANSITION-DESIGN.md 설계 반영) 및 공격/히트 애니메이션 개선

**Goal:** 포탈 전환 시 10-TRANSITION-DESIGN.md 원안의 SpriteMask 입장/퇴장 연출이 실제로 재생되고, Whiff/Roll 애니메이터 트리거 버그가 수정되며, 처치 순간 히트 스파크+카메라 쉐이크+강화된 돌진 잔상이 재생되고, 적 처치 시 파티클+SpriteMask 페이드 연출 후 GameObject가 정리된다.
**Requirements**: TBD (REQUIREMENTS.md에 매핑된 v3.0 핵심 요구사항 없음 — discuss-phase로 범위가 확정된 비주얼 폴리싱 Phase. 12-CONTEXT.md의 D-01~D-11 결정사항을 실질적 요구사항으로 추적한다)
**Depends on:** Phase 11
**Success Criteria** (what must be TRUE):
  1. 포탈 진입 시 플레이어가 SpriteMask에 가려지며 점진적으로 사라지고, 새 층에서 포탈 이펙트가 성장한 뒤 플레이어가 걸어나오듯 점진적으로 나타난다 (D-01~D-04)
  2. 헛베기 시 AirSlash 모션이, 구르기 시 Roll 모션이 실제로 재생된다 — 이전에는 컨트롤러에 파라미터가 없어 무효였다 (D-05, D-06)
  3. 처치 순간 GuardImpact 히트 스파크와 카메라 쉐이크가 재생되고, 대시 중 강화된 그라데이션 TrailRenderer 잔상이 표시된다 (D-07, D-08, D-10)
  4. 적 처치 시 Die 애니메이션 재생 후 파티클+SpriteMask 상승 페이드가 재생되고 GameObject가 실제로 Destroy된다 (D-09)
  5. 적(MeleeEnemy/RangedEnemy)의 기존 공격 모션 자체는 변경되지 않는다 (D-11 범위 확인)
**Plans**: 9 plans (3 waves)

Plans:
- [x] 12-01-PLAN.md — RuntimeMaskSprite.cs + FloorTransitionEffect.cs (D-01, D-03, D-04)
- [x] 12-02-PLAN.md — PortalEffectBuilder.cs + WorldGenerator.cs FloorTransitionSequence 재작성 (D-01, D-02, D-03, D-04)
- [x] 12-03-PLAN.md — 에디터 도구 실행 + Inspector 연결 + 포탈 전환 플레이테스트 (D-01~D-04)
- [x] 12-04-PLAN.md — PlayerAnimatorPatcher.cs (Whiff/Roll 트리거+상태 패치 도구) (D-05, D-06)
- [x] 12-05-PLAN.md — 에디터 도구 실행 + Whiff/Roll 애니메이션 플레이테스트 (D-05, D-06)
- [x] 12-06-PLAN.md — AutoDestroySelf/HitSparkBuilder/PlayerTrailBuilder + CameraFollow.Shake + CombatController 배선 (D-07, D-08, D-10)
- [x] 12-07-PLAN.md — 에디터 도구 실행 + Inspector 연결 + 히트 임팩트 플레이테스트 (D-07, D-08, D-10)
- [x] 12-08-PLAN.md — EnemyDeathEffect.cs + MeleeEnemy/RangedEnemy 사망 연출 배선 (D-09, D-11)
- [x] 12-09-PLAN.md — 적 사망 연출 플레이테스트 (D-09, D-11)


---
*v3.0 Roadmap created: 2026-06-28*
*Last updated: 2026-07-08 — v3.0 milestone shipped, all 5 phases (8-12) complete*

</details>

---

## v3.1 — 보스 룸 & 연출 고도화 (in progress)

**Milestone:** v3.1 — 보스 룸 & 연출 고도화
**Granularity:** Standard
**Coverage:** 18/18 v3.1 requirements mapped

---

## Phases

- [ ] **Phase 13: 오디오 기반 구축 & 연출 사운드 폴리싱** - AudioManager 신설, 포탈전환/히트임팩트/적사망 사운드 추가 및 기존 타이밍 어색함 개선
- [ ] **Phase 14: 적 등장 스폰 연출** - 근접/원거리 적(추후 보스 포함)이 포탈을 타고 등장하는 스폰 VFX, 연출 중 감지/타겟팅 차단
- [ ] **Phase 15: 보스 FSM & 빈틈 타겟팅** - 예고→빈틈 공격 패턴 루프, 빈틈에서만 피격 가능, 7회 피격 처치(진행률 비노출), 처치 점수 보너스
- [ ] **Phase 16: 보스 룸 콘텐츠 & 생명주기 게이팅** - 확률적 보스 룸 스폰(솔로 전투 보장), 입장 카메라 연출+전용 스폰 사운드, 층 타이머 일시정지, 전투 중 정리(Destroy) 예외
- [ ] **Phase 17: WorldGenerator 통합 & 보스 처치 후 층 진행** - 정상 플레이 경로에서 보스 룸이 실제로 등장하고, 처치 후에도 EXIT 포탈로만 층 전환되는 전체 플로우 통합

---

## Phase Details

### Phase 13: 오디오 기반 구축 & 연출 사운드 폴리싱
**Goal**: 프로젝트에 오디오 재생 인프라가 신설되고, 포탈전환/히트임팩트/적사망 연출에 사운드가 추가되며, 기존 타이밍·피드백 어색함이 개선된다
**Depends on**: Phase 12
**Requirements**: SFX-01, SFX-02, SFX-03, SFX-04, SFX-06
**Success Criteria** (what must be TRUE):
  1. AudioManager 싱글턴이 3개 씬(MainMenu/AttackSelect/SampleScene) 전환 간에도 유지되며 PlaySfx() 호출로 사운드가 재생된다
  2. 포탈 진입/퇴장 시 사운드가 재생된다
  3. 대시 히트 임팩트 순간 사운드가 재생되고, 슬로우모션(Time.timeScale 저하) 중에도 피치/타이밍이 깨지지 않는다
  4. 적 사망 시 사운드가 재생된다
  5. 포탈전환/히트/사망 연출에서 v3.0 Phase 12까지 남아있던 타이밍·피드백 어색함(이펙트-사운드 싱크 어긋남, 지연 불일치 등)이 체감상 개선된다
**Plans**: TBD

---

### Phase 14: 적 등장 스폰 연출
**Goal**: 근접형/원거리형 적이 스폰될 때 플레이어처럼 포탈을 타고 등장하는 연출이 재생되고, 연출이 끝나기 전까지 감지/공격 대상이 되지 않는다
**Depends on**: Phase 13
**Requirements**: SPWN-01, SPWN-02
**Success Criteria** (what must be TRUE):
  1. 적이 EnemySpawner.Activate() 시점에 스폰 VFX(포탈 스타일)를 재생하며 등장한다 — 룸 미리 생성/대기 시점에는 재생되지 않는다
  2. 스폰 연출 재생 중에는 해당 적이 CombatController.FindNearestEnemyInRange()의 타겟 후보에서 제외된다
  3. 스폰 연출 재생 중에는 적이 플레이어를 감지/추격/공격하지 않는다
  4. 연출 완료 즉시 적이 정상 FSM(감지/공격/피격)으로 전환된다
  5. 이 스폰 연출 컴포넌트는 적 타입에 종속되지 않아, 이후 보스 룸 통합(Phase 16) 시 별도 구현 없이 재사용된다
**Plans**: TBD

---

### Phase 15: 보스 FSM & 빈틈 타겟팅
**Goal**: 보스가 예고→빈틈 공격 패턴을 반복하고, 빈틈 상태에서만 플레이어의 돌진 공격 대상이 되며, 7회 피격 시 처치되고(진행률은 비노출), 처치 시 점수 보너스가 지급된다
**Depends on**: Phase 14
**Requirements**: BOSS-03, BOSS-04, BOSS-05, BOSS-06
**Success Criteria** (what must be TRUE):
  1. 보스는 예고 → 빈틈 공격 패턴 루프를 반복하며, 빈틈 상태가 아닐 때는 플레이어의 돌진 공격 대상으로 선택되지 않는다
  2. 빈틈 상태에서 플레이어가 돌진 공격을 성공시키면 보스가 즉시 피격 판정을 받고, 공격 패턴이 처음부터 다시 시작된다
  3. 보스는 정확히 7회 피격 시 처치(사망 연출 재생 + Destroy)되며, 6회까지는 처치되지 않는다
  4. 보스 처치 진행률(피격 횟수)을 나타내는 UI/텍스트 요소가 화면 어디에도 존재하지 않는다
  5. 보스 처치 시 ScoreManager에 일반 적보다 큰 점수 보너스가 즉시 누적된다
**Plans**: TBD

---

### Phase 16: 보스 룸 콘텐츠 & 생명주기 게이팅
**Goal**: 보스 룸이 EXIT 포탈처럼 확률적으로 스폰되어 솔로 전투를 보장하고, 입장 시 카메라 연출과 전용 스폰 사운드가 재생되며, 층 타이머가 일시정지되고, 전투 중에는 WorldGenerator 정리 로직에서 예외 처리된다
**Depends on**: Phase 14, Phase 15
**Requirements**: BOSS-01, BOSS-02, BOSS-07, BOSS-09, BOSS-10, SFX-05
**Success Criteria** (what must be TRUE):
  1. 보스 룸은 EXIT 포탈과 마찬가지로 확률적으로 스폰되며, 동시에 활성 보스 룸이 1개를 초과하지 않는다
  2. 보스 룸에는 EnemySpawner 마커가 없어 일반 적(근접/원거리)이 스폰되지 않는다 — 보스 단독 전투가 보장된다
  3. 플레이어가 보스 룸에 입장하면 카메라 잠금/줄임 연출이 재생됨과 동시에 보스 전용 스폰 사운드가 재생된다
  4. 보스 룸 입장과 동시에 층 타이머가 일시정지되고, 보스 룸을 벗어나면(처치 또는 EXIT 이용) 재개된다
  5. 보스와의 전투가 진행 중인 동안에는 WorldGenerator의 앞뒤 2개 유지 정리(Destroy) 로직에서 보스 룸이 예외 처리되어 파괴되지 않는다
**Plans**: TBD

---

### Phase 17: WorldGenerator 통합 & 보스 처치 후 층 진행
**Goal**: 보스 룸이 실제 무한 생성 체인(WorldGenerator)을 통해 확률적으로 등장하고, 보스 처치 후에도 층 진입은 기존 EXIT 포탈을 통해서만 이루어져 보스 콘텐츠가 정상 플레이 경로로 완전히 통합된다
**Depends on**: Phase 16
**Requirements**: BOSS-08
**Success Criteria** (what must be TRUE):
  1. 별도 디버그 씬이 아닌 SampleScene의 정상 플레이 경로에서 WorldGenerator의 룸 체인 생성 로직을 통해 보스 룸이 실제로 스폰된다
  2. 보스를 처치해도 층 번호가 자동으로 오르지 않으며, 플레이어는 여전히 EXIT 포탈을 찾아 진입해야 층이 전환된다
  3. 보스 전투 종료(처치 또는 EXIT 진입) 후에는 보스 룸도 다시 WorldGenerator의 일반 앞뒤 2개 정리 대상에 포함된다
  4. 보스 룸 전체 플로우(확률적 스폰 → 입장 연출+사운드 → 솔로 전투 → 7회 피격 처치 → 점수 보너스 → EXIT 포탈로 층 전환)를 개발자 개입 없이 처음부터 끝까지 플레이할 수 있다
**Plans**: TBD

---

## Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 13. 오디오 기반 구축 & 연출 사운드 폴리싱 | 0/? | Not started | - |
| 14. 적 등장 스폰 연출 | 0/? | Not started | - |
| 15. 보스 FSM & 빈틈 타겟팅 | 0/? | Not started | - |
| 16. 보스 룸 콘텐츠 & 생명주기 게이팅 | 0/? | Not started | - |
| 17. WorldGenerator 통합 & 보스 처치 후 층 진행 | 0/? | Not started | - |

---

## Coverage Map

| Requirement | Phase | Description |
|-------------|-------|-------------|
| SFX-01 | Phase 13 | AudioManager 인프라 신설 |
| SFX-02 | Phase 13 | 포탈 전환 사운드 |
| SFX-03 | Phase 13 | 히트 임팩트 사운드 |
| SFX-04 | Phase 13 | 적 사망 사운드 |
| SFX-06 | Phase 13 | 포탈/히트/사망 타이밍·피드백 어색함 개선 |
| SPWN-01 | Phase 14 | 적 스폰 시 포탈 연출 재생 (보스 재사용 전제) |
| SPWN-02 | Phase 14 | 스폰 연출 중 감지/타겟팅 차단 |
| BOSS-03 | Phase 15 | 예고→빈틈 공격 패턴 루프 |
| BOSS-04 | Phase 15 | 7회 피격 처치, 매 피격 후 패턴 리셋 |
| BOSS-05 | Phase 15 | 처치 진행률 비노출 |
| BOSS-06 | Phase 15 | 처치 시 점수 보너스 |
| BOSS-01 | Phase 16 | 보스 룸 확률적 스폰 (동시 1개 제한) |
| BOSS-02 | Phase 16 | 일반 적 미스폰 (솔로 전투) |
| BOSS-07 | Phase 16 | 보스 룸 입장 시 층 타이머 일시정지/재개 |
| BOSS-09 | Phase 16 | 입장 카메라 연출 |
| BOSS-10 | Phase 16 | 전투 중 WorldGenerator 정리 예외 |
| SFX-05 | Phase 16 | 보스 스폰 전용 사운드 |
| BOSS-08 | Phase 17 | 보스 처치 후에도 EXIT 포탈로만 층 전환 |

**v3.1 Coverage: 18/18 requirements mapped. No orphans.**

---

## Implementation Notes (for plan-phase reference)

- AudioManager는 MonoBehaviour 싱글턴 + 풀링된 AudioSource 배열로 구현 (ScoreManager/FloorTimer의 순수 static 패턴과 의도적으로 다름 — WorldGenerator와 동일한 이유)
- 모든 신규 오디오 타이밍 코드는 Time.unscaledDeltaTime/WaitForSecondsRealtime 컨벤션 준수 (프로젝트 전역 슬로우모션 면역 규칙)
- EnemySpawnEffect는 EnemySpawner.Activate()에서만 트리거 — Awake()/OnEnable()에서 트리거 시 오프스크린/중복 재생 버그 발생
- BossEnemy : MonoBehaviour, IEnemy — IEnemy 3-member 계약 변경 없음, CombatController 변경 없이 대시킬 가능
- 빈틈 타겟팅 게이팅은 CombatController.FindNearestEnemyInRange()의 기존 `!IsAlive` 스킵 체크를 재사용 (신규 인터페이스 불필요)
- 보스 프리팹은 Complex_Room 복제로 만들되 EnemySpawner 마커를 반드시 제거하고, WorldGenerator._roomPrefabs 풀에는 절대 포함시키지 않음 (BOSS-02/Pitfall 1 방지)
- 보스 룸은 ExitPortal과 동일한 "확률적 오버레이" 패턴으로 스폰 — `_bossSpawnChance`/`_maxBossRoomsActive`는 WorldGenerator에 EXIT 포탈과 별도 필드로 추가
- FloorTimer × 보스 룸: 일시정지/재개 방식으로 확정 (BOSS-07) — 연장/면제 방식은 채택하지 않음
- 보스는 플레이어를 원샷킬한다는 전제 유지 (일반 적과 동일한 왕복 원샷원킬 규칙) — 텔레그래프 타이밍만 보스 전용으로 더 길게 설계
- 빌드 순서 권장(research): AudioManager → 사운드 폴리싱 → EnemySpawnEffect → BossEnemy FSM → 보스 룸 프리팹 → WorldGenerator 통합 (최고 위험도 변경은 마지막)

---
*v3.1 Roadmap created: 2026-07-08*
*Last updated: 2026-07-08 — v3.1 roadmap created, Phase 13-17 defined*
