---
gsd_state_version: 1.0
milestone: v2.0
milestone_name: Phases
current_phase: 07
current_plan: 1
status: unknown
stopped_at: Completed quick/260624-lre-floorspawner-step-2-y-nextroom-roomentry
last_updated: "2026-06-24T06:55:00.000Z"
last_activity: "2026-06-24 - Completed quick task 260624-q65: MeleeEnemy MovePosition→linearVelocity.x 전환 + 점프 로직 추가"
progress:
  total_phases: 7
  completed_phases: 5
  total_plans: 18
  completed_plans: 16
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

Phase: 07 (attackselect-scene-scene-flow) — EXECUTING
Plan: 1 of 2
**Current Phase:** 07
**Current Plan:** 1
**Phase Status:** Checkpoint at T3 — T1/T2 code complete, T3 requires Unity Editor GUI

```
Progress: [X] Phase 1  [ ] Phase 2  [ ] Phase 3  [ ] Phase 4
           |___________|___________|___________|___________|
                 25%                                    100%
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
| Plans completed | TBD | 2 |

---
| Phase 01-foundation-movement P01 | 4 | 2 tasks | 7 files |
| Phase 01-foundation-movement P02 | ~3min | 2 tasks | 4 files |
| Phase 01-foundation-movement P03 | 8 | 2 tasks | 4 files |
| Phase 02-combat-core P02-01 | 25 | 2 tasks | 7 files |
| Phase 02-combat-core P02-02 | 3 | 2 tasks | 3 files |
| Phase 02-combat-core P02-03 | 15 | 2 tasks | 8 files |
| Phase 03-enemy-system P03-04 | ~8min | 2 tasks | 2 files |
| Phase 04-hud-game-loop P04-01 | ~5min | 2 tasks (T3 pending human) | 2 files |

## Accumulated Context

### Key Decisions Locked

| Decision | Rationale |
|----------|-----------|
| Infrastructure merged into Phase 1 | No v1 requirements map to pure infrastructure; merging keeps phase count honest for prototype scope |
| MOVE-03 (roll) in Phase 2 not Phase 1 | Roll is a combat tool (slow-mo usable, i-frames), not a movement primitive — belongs with the combat system it interacts with |
| FEEL-01 (hit-freeze) in Phase 2 | Must ship with the attack system — hit-freeze is the punctuation of the kill, cannot be validated separately |
| Floor system deferred to v2 | User decision: validate combat feel in a single test room first |
| Phase 5 polish not created | No v1 requirements are polish-only; tuning lives inside Phase 2-3 success criteria |
| jumpCutMultiplier = 0.4 (D-02) | Drops ascending velocity to 40% on button release — clear tap-vs-hold arc difference |
| Time.timeScale compensation in Phase 1 | 1f / Time.timeScale in ApplyMovement baked in now — Phase 2 slow-mo requires no PlayerController rewrite |
| PlayerInput notification = SendMessages (0) | PlayerController reads actions directly via playerInput.actions[], behavior mode is irrelevant |
| WaitForSecondsRealtime for i-frames (01-03) | Phase 2 sets timeScale ~0.2; WaitForSeconds would extend 1s to 5s. WaitForSecondsRealtime is timeScale-immune. |
| Vector3 _lastSafePosition value type (01-03) | Storing Transform reference would become stale null ref when floor objects recycled in v2. Vector3 copy is immune (Pitfall 14). |
| Layer constants hardcoded 7/8 (01-03) | Matches TagManager.asset from Plan 01. Avoids LayerMask.NameToLayer() string lookup overhead each call. |
| RangedEnemy moveSpeed=0f default (03-04) | Stationary at start per D-10 — Chase state immediately telegraphs (Risk 6 mitigation: no distance-check needed). Inspector-adjustable post-playtest. |
| TelegraphAndFire uses yield return null + unscaledDeltaTime (03-04) | Frame-by-frame alpha accumulation matches RangeDisplay pattern, fully timeScale-immune for slow-mo compatibility. |
| FloorManager as static class, not MonoBehaviour (04-01) | Data-only int needs no scene lifecycle; static field is sufficient and avoids scene coupling |
| (AttackType)(-1) dirty-check sentinel (04-01) | Forces first-frame label update; -1 is never a valid AttackType so it always differs on first Update() |
| SetText("{0}", int) over string interpolation (04-01) | TMP's int overload uses internal char buffer — zero allocation per frame vs. $"" allocating new string every frame |

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

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260601-lzr | InputManager 생성 — WASD 이동, Shift 구르기, 마우스 클릭 공격 (Unity New Input System) | 2026-06-01 | — | [260601-lzr](./quick/260601-lzr-inputmanager-wasd-shift-unity-new-input-/) |
| 260601-mrm | 이동 가속도 기반 변경, Sprint가 기본 달리기, Idle→Sprint 전환 중 Walk 애니메이션 | 2026-06-01 | ca4a70e | [260601-mrm](./quick/260601-mrm-sprint-idle-sprint-walk/) |
| 260604-sou | 적(DummyEnemy) 크기를 키우고 콜라이더를 크기에 맞게 조정 | 2026-06-04 | — | [260604-sou](./quick/260604-sou-dummyenemy/) |
| 260604-vst | Phase 2-4 테스트러너 제거 에디터 직접 플레이테스트로 전환 | 2026-06-04 | — | [260604-vst](./quick/260604-vst-phase-2-4/) |
| 260605-l0c | Phase 2 에디터 플레이테스트 가이드 재작성 (코드 기준 수치 반영) | 2026-06-05 | 1b20012 | [260605-l0c](./quick/260605-l0c-phase-2/) |
| 260605-r61 | enemy 프리팹 콜라이더(0.16→0.8x1.2) + Rigidbody2D Kinematic 교정 | 2026-06-05 | 2056400 | [260605-r61-enemy](./quick/260605-r61-enemy/) |
| 260605-rhs | RangeDisplay lineWidth + 마우스 단일 빔 + 닫힌 부채꼴 Fan | 2026-06-05 | 1a04a21 | [260605-rhs](./quick/260605-rhs-rangedisplay-3-1-linewidth-2-linear-3-fa/) |
| 260605-sj2 | CheckGround offset 0.51f→0.05f — CapsuleCollider2D pivot 기준 수정 | 2026-06-05 | 0e42dba | [260605-sj2](./quick/260605-sj2-groundcheck-offset-fix-capsulecollider2d/) |
| 260605-tss | CombatController 슬로우모션 하이라이트 버그 2종 수정 — Update() 매 프레임 갱신 + ExitSlowMotion 이중 호출 제거 | 2026-06-05 | ac19ef4 | [260605-tss](./quick/260605-tss-combatcontroller-1-update-findnearestene/) |
| 260607-19i | FastPlayerAnimator에 IsDashing Bool 파라미터 + Dash 스테이트 + AnyState→Dash / Dash→Idle 트랜지션 추가 | 2026-06-07 | e23f2cf | [260607-19i](./quick/260607-19i-fastplayeranimator-controller-isdashing-/) |
| 260607-kif | AttackType 디버그 오버레이 생성 — OnGUI 기반 Linear/Fan 실시간 표시 + Player 자동 부착 에디터 스크립트 | 2026-06-07 | aff97da | [260607-kif](./quick/260607-kif-debug-attack-type-tracker/) |
| 260607-ku4 | AttackTypeSelector.SetType null-instance 가드 제거 — Selected 무조건 갱신, RefreshHighlights null-conditional | 2026-06-07 | fb1e0b6 | [260607-ku4](./quick/260607-ku4-attacktypeselector-settype-instance-null/) |
| 260607-vot | 카메라가 플레이어를 따라가도록 구현 — CameraFollow 컴포넌트를 Main Camera에 부착, Player Transform 연결 | 2026-06-07 | a2557fc | [260607-vot](./quick/260607-vot-camera-follow-player/) |
| 260608-lb9 | CombatController 공개 프로퍼티 3개 추가 + RangeDisplay 중복 SerializeField 제거 — 단일 진실 소스 연결 | 2026-06-08 | 3af0077 | [260608-lb9](./quick/260608-lb9-combatcontroller-fanradius-fanhalfangled/) |
| 260608-09z | Fan 공격방식 마우스 방향 기준으로 변경 — RangeDisplay 표시 + CombatController 판정 모두 교체 | 2026-06-08 | 3713b57 | [260608-09z](./quick/260608-09z-fan/) |
| 260609-wvp | Enemy 오브젝트들이 죽으면 삭제하지 않고 Die 애니메이션 재생 후 죽은 상태로 씬에 남겨놓기 | 2026-06-09 | — | [260609-wvp-enemy-die](./quick/260609-wvp-enemy-die/) |
| 260609-x6d | 두 Enemy 오브젝트에 공격 애니메이션 추가 — MeleeEnemy에 SwordAttack.anim, RangedEnemy에 GunFire.anim 연결 | 2026-06-14 | 3b625ea, e86fa00 | [260609-x6d](./quick/260609-x6d-enemy-meleeenemy-swordattack-anim-ranged/) |
| 260616-qlg | GaugeController 클래스 이름을 ChronoGaugeController로 변경 | 2026-06-16 | 9213d55, 91e7f1d | [260616-qlg](./quick/260616-qlg-gaugecontroller-chronogaugecontroller/) |
| 260616-s3m | ChronoGauge 버그 2종 수정: HUDController 게이지 미반영 + 게이지 소진 시 범위 표시 유지 | 2026-06-16 | — | [260616-s3m](./quick/260616-s3m-chronogauge-hudcontroller/) |
| 260616-vo4 | HUDController.Start()에 _gauge null 체크 디버그 로그 추가 (원인 진단용 임시 코드) | 2026-06-16 | 1ad0e0f | [260616-vo4](./quick/260616-vo4-hudcontroller-start-gauge-null/) |
| 260617-02t | RangeDisplay chestOffset 추가(가슴 오프셋 기준 origin) + RangedEnemy 텔레그래프 origin → firePoint 기준 교체 | 2026-06-17 | 0e779c5, f9b8827 | [260617-02t](./quick/260617-02t-rangedisplay-rangedenemy-transform-posit/) |
| 260617-0el | RangedEnemy detectionRadius(감지)/aimLineLength(공격 트리거) 분리 — Chase 중 aimLineLength 진입 시에만 텔레그래프 시작 | 2026-06-17 | bd63875 | [260617-0el](./quick/260617-0el-rangedenemy-detectionradius-aimlinelengt/) |
| 260617-0t2 | MeleeEnemy patrol→SwordWalk/chase→SwordRunAltGrip 분리, RangedEnemy patrol→GunWalk 추가, _animator 캐시 통일 | 2026-06-17 | 926bb8f, b5d2438 | [260617-0t2](./quick/260617-0t2-meleeenemy-swordwalk-swordrunaltgrip-ran/) |
| 260617-jwe | Room 프리팹 폴더 구조 생성 (Assets/Prefabs/Rooms/ 아래 14개 Room 폴더) | 2026-06-17 | — | [260617-jwe](./quick/260617-jwe-room/) |
| 260618-u8j | RoomPrefabBuilder 에디터 스크립트 — Fast/Build Room Prefabs 메뉴로 14개 Room 프리팹 생성 (Platform/KillZone/마커 계층) | 2026-06-18 | 98309c2 | [260618-u8j](./quick/260618-u8j-14-room-editor/) |
| 260623-ntw | MainMenuController + MainMenuSceneBuilder — Start/Quit 버튼 로직 + EditorScript(Fast/Build MainMenu Scene 메뉴) | 2026-06-23 | ec13a46, ad54d11 | [260623-ntw](./quick/260623-ntw-mainmenu-fast-samplescene-application-qu/) |
| 260623-t6i | DeathScreen RestartLabel 텍스트를 "메인 메뉴"로 변경 | 2026-06-23 | — | [260623-t6i](./quick/260623-t6i-deathscreen/) |
| 260624-e5y | GameBootstrapper — 어느 씬에서 Play해도 항상 MainMenu로 리디렉션 (BeforeSceneLoad) | 2026-06-24 | 90f9e2c | [260624-e5y](./quick/260624-e5y-mainmenu/) |
| 260624-lre | FloorSpawner Step 2 텔레포트를 고정 Y 공식에서 nextRoom의 RoomEntry 위치로 변경 | 2026-06-24 | — | [260624-lre](./quick/260624-lre-floorspawner-step-2-y-nextroom-roomentry/) |
| 260624-mcv | FloorSpawner SpawnRoom() 적 Instantiate에 room.transform parent 추가 — Room 파괴 시 적 자동 정리 (FLOOR-04) | 2026-06-24 | ada935e | [260624-mcv](./quick/260624-mcv-floorspawner-cs-157-instantiate-room-tra/) |
| 260624-ml3 | RoomClearCondition 버그 2개 수정 — enemies 없을 때 즉시 활성화 + GetComponentsInChildren 동적 탐색 | 2026-06-24 | 8181b9f | [260624-ml3](./quick/260624-ml3-roomclearcondition-cs-2-1-enemies-target/) |
| 260624-oh2 | ROOM_NOTES.md 기반 프리팹 수정: Room_Gap 적 스폰포인트 삭제, Room_Combat 적 5개 추가 | 2026-06-24 | — | [260624-oh2](./quick/260624-oh2-room-notes-md-5-room-gap-room-combat-5/) |
| 260624-q65 | MeleeEnemy MovePosition→linearVelocity.x 전환 + 앞 장애물/바닥 끊김 시 점프 로직 추가 | 2026-06-24 | f5b4349 | [260624-q65](./quick/260624-q65-meleeenemy-moveposition-velocity-x/) |

---

## Session Continuity

**How to resume after /clear:**

1. Read `.planning/STATE.md` (this file) — current position and decisions
2. Read `.planning/ROADMAP.md` — phase goals and success criteria
3. Read `.planning/REQUIREMENTS.md` — requirement details and traceability
4. Check which phase plan exists in `.planning/` (e.g., `PLAN-phase-1.md`)
5. Continue from Current Phase listed above

**Last session:** 2026-06-24T00:00:00Z
**Stopped at:** Completed quick/260624-e5y-mainmenu
**Last activity:** 2026-06-24

---
*State initialized: 2026-05-27*
*Last updated: 2026-06-24 — quick task 260624-e5y GameBootstrapper*
