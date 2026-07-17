---
gsd_state_version: 1.0
milestone: v3.1
milestone_name: — 보스 룸 & 연출 고도화
status: executing
stopped_at: "Checkpoint reached in 15-06-PLAN.md Task 3 (checkpoint:human-action) -- Task 1/2 complete"
last_updated: "2026-07-17T00:00:00.000Z"
last_activity: 2026-07-17
progress:
  total_phases: 8
  completed_phases: 5
  total_plans: 27
  completed_plans: 25
---

# Project State: Fast (가칭)

*Single source of truth for project memory across sessions.*

---

## Project Reference

**Core Value:** 공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.

**Prototype Goal:** 보스 룸 콘텐츠(1종, 확장 가능한 프레임워크)를 추가하고, 기존 포탈/히트/사망 연출에 사운드·타이밍 개선을 더하고, 적 등장(스폰) 연출을 신설한다.

**Last Shipped Milestone:** v3.0 — 무한 복도 층 시스템 (2026-07-08)

**Current Milestone:** v3.1 — 보스 룸 & 연출 고도화 (roadmap created, Phase 13-17)

---

## Current Position

Phase: 15 (fsm) — EXECUTING
Plan: 6 of 6 (15-06, supersedes 15-05 Task 2/3, D-13 WorldGenerator pool-swap entry path). 15-04 superseded/skipped (see 15-04-SUMMARY.md).
Status: Checkpoint reached — Task 3 (checkpoint:human-action, Unity Editor menu execution) blocking. Task 1/2 complete (commits 01ed135, 5c240be on main).
Last activity: 2026-07-17

```
Progress: [████░░░░░░] v3.1 milestone: 2/5 phases complete (Phase 13-17) — Phase 999.2/999.3/999.4(backlog) 전부 완료
```

---

## Accumulated Context

### Key Decisions Locked for v3.1 (2026-07-08 roadmap kickoff)

| Decision | Rationale |
|----------|-----------|
| 보스는 7회 피격으로 처치 (HP바 없음, 진행률 비노출) | 원샷원킬 코어 밸류 유지하면서 일반 적과 차별화 — 매 피격 후 공격 패턴 리셋으로 긴장감 유지 |
| 빈틈(예고→빈틈 루프) 상태에서만 타겟팅 허용 | HP바 없이 "언제 때릴 수 있는가"를 텔레그래프로 표현 — CombatController 변경 없이 `!IsAlive` 스킵 체크 재사용 |
| 보스 룸 입장 시 층 타이머 일시정지, 퇴장 시 재개 (연장/면제 아님) | 보스전 도중 무관한 타이머 사망 방지, 동시에 무제한 시간 부여도 아님 |
| 보스 룸은 WorldGenerator 앞뒤 2개 정리(Destroy)에서 전투 중 예외 | 긴 전투 중 플레이어 위치 드리프트로 보스 룸이 파괴되는 사고 방지 |
| 보스 처치는 층 자동 진행을 트리거하지 않음 (기존 EXIT 포탈 필수) | 보스전과 층 전환을 분리 — EXIT 포탈의 존재 의미 유지 |
| 보스 룸 입장 시 카메라 잠금/줄임 연출 | 보스전 진입을 일반 룸과 구분되는 이벤트로 체감시킴 |
| 스폰 VFX(포탈 스타일)는 일반 적+보스 공통, 연출 중 감지/타겟팅 차단 | EnemySpawner.Activate() 시점에서만 트리거하는 적 타입 비종속 컴포넌트로 설계 — 보스 통합 시 재사용 |
| AudioManager는 처음부터 신규 구축 (기존 오디오 코드 0건) | MonoBehaviour 싱글턴 + 풀링 방식, Time.unscaledDeltaTime 컨벤션 준수 필수 |
| 빌드 순서: Audio → Spawn VFX → Boss FSM → Boss Room → WorldGenerator 통합 | research 권장 순서 — 최고 위험도(WorldGenerator) 변경을 마지막으로 미룸 |
| DSP Buffer Size 512(Good latency) 채택 (Plan 13-01) | 1024는 히트 사운드 체감 지연, 256은 에디터 스터터/저사양 Android 크래클 보고 — 512가 안전한 중간값 |
| AudioImportSettings AssetPostprocessor를 오디오 팩 복사 이전에 커밋 (Plan 13-01) | 최초 임포트부터 Force To Mono + ADPCM + Decompress On Load 자동 적용 보장 — 이후 추가 시 Reimport 필요 |
| SFX-06 폴리싱 미적용 — 전 항목 OK (Plan 13-04) | 플레이테스트에서 SC1-4 + SFX-06 A-D 전부 통과 보고 — 13-03 훅 배치만으로 타이밍/피드백 어색함 없음, 4개 대상 파일(EnemyDeathEffect.cs/HitSparkBuilder.cs/AudioManagerPrefabBuilder.cs/FloorTransitionEffect.cs) diff 0 확정 |
| EnemySpawner.Activate(GameObject = null) 기본 매개변수로 기존 DebugRoomTeleporter 호출부 무변경 유지 (Plan 14-02) | 2단계 Spawn/Activate 분리를 기존 호출자 수정 없이 완료 — 정밀 변경 원칙 준수 |
| CorridorEnemySpawnerTool 메뉴 실행은 14-04로 연기 (Plan 14-02) | 도구는 코드만 작성, 실제 프리팹 저장(Unity 에디터 조작)은 checkpoint:human-action이 필요한 14-04에서 수행 |
| WorldGenerator TrySpawnEnemies를 수집 전용으로 분리하고 TryActivateSection/ActivateStaggered로 실제 진입 시점에 D-05 스태거 Activate (Plan 14-03) | Room뿐 아니라 Corridor(CheckCorridorEntry Pattern 3 임계값 체크)와 새 층 시작 Room(FloorTransitionSequence Step 4)까지 동일한 진입 감지 게이트로 통일 |
| FloorTransitionSequence 옛 체인 파괴 루프에서 _activatedSections/_pendingSpawns를 블랭킷 Clear() 대신 개별 엔트리만 제거 (Plan 14-03) | standbyRoom(=newRoom)은 _chain에 속한 적이 없어 사전 등록된 _pendingSpawns 엔트리가 Step 4까지 보존되어야 함 — 블랭킷 Clear 시 새 층 시작 Room의 적이 영원히 비활성화됨 |
| CorridorEnemySpawnerTool 실행으로 Corridor 3종에 EnemySpawner(Melee) 마커 부착 완료 (Plan 14-04) | Room+Corridor 전체에서 스폰 VFX 콘텐츠 동등성 확보 (D-03) — git diff로 각 프리팹당 컴포넌트 1개만 추가됨을 확인, 재실행 시 멱등성 확인 |
| Phase 14 전체 플레이테스트 체크리스트 10개 항목 전부 통과 판정 (Plan 14-04) | SPWN-01/SPWN-02 및 ROADMAP SC1-5, D-01~D-09 전부 실제 플레이로 검증 완료 — 별도 폴리싱/수정 불요, Phase 15/16 보스 스폰 재사용 전제 성립 |
| RangedEnemy 키팅 kitingRetreatRange=7.5f/kitingSpeed=3f 확정, 재조정 불필요 (Plan 999.4-02) | D-07(단순 거리-유지)/D-08(Chase 진입 즉시 발동)/D-09(조준·발사 병행)/D-10(aimLineLength 50% 임계값) 플레이테스트 체크리스트 1~7번 전부 통과, D-06(원거리 텔레그래프) 회귀 없음 확인 |
| MeleeEnemy jumpForce=13f/maxJumpableGapWidth=3.2f 확정, 재조정 불필요 (Plan 999.4-01) | D-01(예방) — Room_Gap(3유닛) 점프 클리어, Room_Fall(5.5유닛) 턴어라운드, 플레이테스트 체크리스트 1~7 전부 통과 |
| FallZoneTrigger Enemy 태그 사후처리(즉시 Destroy, VFX 없음) 확정 (Plan 999.4-01) | D-02/D-03 — MeleeEnemy/RangedEnemy 공통 Enemy 태그로 두 타입 모두 커버, Enemy×Default Layer Collision Matrix 활성화 확인, Player 낙사 경로(FallDetector.OnFall) 회귀 없음 |
| MeleeEnemy telegraphDuration=0.45f/telegraphSpeedMultiplier=0.4f 확정, 재조정 불필요 (Plan 999.4-03) | D-04(0.8s→0.45s 단축)/D-05(이동하며 예고+FlipSprite)/D-06(RangedEnemy 미변경 회귀 없음) 플레이테스트 체크리스트 1~7 전부 통과 — Phase 999.4(enemy-ai-enhancement-pack) 3/3 plans 전체 완료 |
| MeleeEnemy.prefab ExclamationIcon SpriteRenderer 스프라이트 미할당(m_Sprite fileID 0) 버그를 quick task 260714-fnr로 수정 (커밋 93a3d99) | 999.4-03 코드 로직은 정상이었으나 프리팹 애셋에 "!" 스프라이트가 애초에 배정된 적이 없어 Telegraph 아이콘이 안 보이던 사전 존재 버그 — ExclamationIconBuilder.cs 절차적 생성 도구로 해결, 재사용 가능 |
| RoomClearCondition.DiscoverEnemies() 분리 + RoomRespawnGate.ConsumeRespawn()에 별도 쿨다운 타이머 미도입 확정 (Plan 999.2-01) | Start()/ResetForRespawn() 양쪽에서 동일 적 탐색 로직 재사용(중복 제거), D-03(쿨다운 없음)에 따라 "나갔다가 다시 들어옴" 자체가 게이트 역할을 하도록 설계 — GameObject 수명 종속 상태로 Case B(체인 이탈 재생성)를 구조적으로 배제(D-01a) |
| EnemySpawner.ResetForRespawn()은 HasActivated와 _spawned를 함께 비움 (Plan 999.2-02) | 재무장만 하고 _spawned를 안 비우면 Activate()가 댕글링 참조로 영구 no-op됨(999.2-RESEARCH.md Pitfall 1) — 반드시 함께 초기화 |
| RespawnedEnemyMarker 빈 additive 컴포넌트로 리스폰 개체 태깅, IEnemy 미확장 (Plan 999.2-02) | ISpawnGatable(Phase 14)과 동일한 프로젝트 컨벤션 — 잠긴 3-member IEnemy 계약을 건드리지 않고 리스폰 판정 가능 |
| ScoreManager.RespawnKillScore=30 (KillScore의 약 30%) 확정 (Plan 999.2-02) | 리스폰 파밍을 점수 관점에서 비효율적으로 만들되 완전히 막지는 않음 — D-09(무제한 리스폰)와 일관 |
| WorldGenerator.MarkRoomLeft()/TryRespawnRoom()을 새 트리거 콜라이더 없이 UpdatePlayerIndex()의 기존 체인-인덱스 전이 신호에 배선 (Plan 999.2-03) | 999.2-RESEARCH.md Pitfall 2 회피 — Room 진입/이탈이 이미 매 프레임 계산되므로 별도 감지 시스템 불필요, Corridor는 RoomRespawnGate 미부착으로 자동 no-op(D-04) |
| Phase 999.2(enemy-infinite-respawn-mechanism) 4/4 plans 전체 완료 판정 (Plan 999.2-04) | RoomRespawnTool 메뉴 실행으로 Complex_Room 6종에 RoomClearCondition+RoomRespawnGate 실제 부착 확인, D-01~D-10(D-01a 포함) 플레이테스트 체크리스트 11개 항목 전부 통과 — 리스폰 파이프라인은 Case A(체인 내 재진입)로 한정되고 Case B(체인 이탈 재생성)/Corridor/보스 룸에는 부작용 없음이 확인됨, 추가 폴리싱/수정 불요 |
| FloorTransitionEffect.PlayEntry()/PlayExit()를 SpriteMask 페이드에서 Rigidbody2D 실제 이동으로 전면 재작성 (Plan 999.3-02) | PlayEntry는 999.3-01 산출물 PortalVortex.mat 소용돌이 오버레이 + 포탈 중심으로 실제 이동, PlayExit은 CombatController가 이미 쓰는 IsDashing 애니메이터 bool을 재사용한 수직 도약으로 대체 — PlayExit() 호출 시그니처는 그대로 유지해 WorldGenerator EXIT 호출부 무변경, ENTRY 호출부만 _portalVortexMaterial 전달로 갱신 |
| SampleScene WorldGenerator._portalVortexMaterial 배선 + EntryVortex 회오리 반복 플레이테스트 튜닝 후 Phase 999.3 전체 완료 판정 (Plan 999.3-03) | 초기 튜닝 회귀를 999.3-01 확정 베이스라인으로 되돌린 뒤 재튜닝 — 월드 사이즈 축소, distortion scale 재조정, phase 드라이버를 절대 시간에서 로컬 경과 시간으로 전환, winding count/강도 강화까지 다단계 사용자 피드백 반영, 최종 사용자 확인("회오리는 이정도면 괜찮은것 같아")으로 D-01~D-08 전체 체크리스트 완주 없이 페이즈 종료 |
| TestWorldGenerator.cs/FloorSpawner.cs/RoomExit.cs(+.meta) 삭제 및 MeleeEnemy.cs/RangedEnemy.cs 죽은 LayerPlayerHurtbox/LayerPlayerInvincible 상수 제거 완료 (Plan 16-01) | D-01~D-04 — GUID 교차검증으로 100% 죽은 코드 확인 후 삭제, Physics2D 충돌 매트릭스로 대체된 중복 상수 정리. Unity 에디터 측 잔여 정리(비활성 FloorSpawner GameObject, 구형 Room_*.prefab 14종+Room_Debug.prefab 삭제)는 사용자가 직접 처리하기로 확정 — 이 플랜 범위 밖. Wave 2(16-03) EnemyBase 추출의 사전 정지 작업 완료 |
| EnemyBase 추상 클래스 신설 후 MeleeEnemy/RangedEnemy가 상속하도록 리팩토링 완료, D-08 점수 시점 재설계 전체 완성 (Plan 16-03) | D-05 — OnDashHit() 공통부(가드+IsAlive=false+rb정지+콜라이더비활성화+animator+DeathEffect)/ClearHighlight()/IsPlayerInRange()/OnEnable-OnDisable 구독/SetSpawnGate()를 최소 범위로만 추출, 타입별 고유 로직(텔레그래프/히트박스/점프-gap, 조준선/발사체/카이팅)은 그대로 서브클래스에 유지. D-08 — 각 적이 EnemyBase.OnDashHit() 안에서 IsAlive=false 커밋 직후 스스로 RespawnedEnemyMarker 판정 후 ScoreManager.AddKillScore() 호출(16-02의 CombatController 측 제거와 합쳐 D-08 완성). Phase 15(BossEnemy, 미실행) EnemyBase 상속 전제 마련 |
| ScoreManager.BossKillScore=750 + AddBossKillScore()/SubtractScore(int) 정적 메서드, EnemyDeathEffect.ConfigureIntensity() additive 메서드 확정 (Plan 15-01) | BOSS-06/D-09(보스 처치 보너스)/D-12(비치명타 자기 상쇄)/D-08(보스 전용 사망 연출 강도)를 위해 BossEnemy.cs(15-02)가 즉시 참조할 계약을 순수 추가 변경으로 선반영 — MeleeEnemy.cs/RangedEnemy.cs 호출부 git diff 0 확인 |
| BossEnemy.cs 신설 완료 — Telegraph→Attack→Vulnerable 단일 패턴 루프, IsAlive를 "빈틈 여부"로 오버로드 + 별도 _isDefeated로 처치 판정 분리, 7회 피격 처치 + 보스 전용 사망 연출/점수 보너스 (Plan 15-02) | BOSS-03/04/05/06 — EnemyBase를 상속하지 않는 독립 IEnemy+ISpawnGatable 구현으로 다중 히트/패턴 리셋 요구를 EnemyBase.OnDashHit()의 단발성 사망 경로와 분리. D-12(비치명타 점수 자기 상쇄) 코드는 최종 제거 — Phase 16(16-02/16-03)이 ScoreManager.AddKillScore() 호출을 CombatController.ExecuteDash()에서 EnemyBase.OnDashHit()으로 이미 옮겨서 CombatController가 보스 히트에 +100을 더 이상 주지 않으므로 상쇄할 점수 자체가 없음 (15-CONTEXT.md D-12 SUPERSEDED와 일치) |
| BossEnemyPrefabBuilder.cs 신설 완료 — MeleeEnemy.prefab 구조 복제해 BossEnemy.prefab 생성(1.6배 스케일+진한 붉은색 틴트) + Room_Debug.prefab 배선 도구 2종, DebugRoomTeleporter._bossPrefab 필드로 EnemySpawner 우회 직접 Instantiate 스폰 배선 완료 (Plan 15-03) | BOSS-03/04/05/06/D-10/D-11 — 두 메뉴("Build BossEnemy Prefab", "Wire Boss Into Room Debug") 모두 멱등적, 실제 실행(프리팹 자산 생성/Room_Debug 배선)은 Unity 에디터 조작이 필요한 15-04로 연기. 보스는 EnemySpawner를 거치지 않고 직접 Instantiate되어 BossEnemy 기본 IsAlive=true + Start() 가드로 패턴 루프 즉시 시작(RESEARCH.md Open Question 2 권장안, Phase 16에서 EnemySpawner 경유로 교체 예정) |
| 15-04-PLAN.md SUPERSEDED, 15-05-PLAN.md가 D-11 재논의(RE-RESOLVED) 반영해 대체 — Task 1(RoomBossFsmTestBuilder.cs)/Task 2(BossEnemyPrefabBuilder.cs 재배선) 완료, Task 3(checkpoint:human-action) 대기 중 (Plan 15-05) | D-11 RE-RESOLVED — 목적지 룸을 Room_Debug.prefab이 아니라 신규 독립 프리팹 Room_BossFsmTest.prefab으로, 진입 지점을 Room_Debug.prefab 자식이 아니라 영속 씬(SampleScene.unity)의 독립 GameObject "BossFsmTest_Teleporter"로 배치해 Phase 16의 Room_Debug 삭제와 완전히 무관하게 함. Room_Debug.prefab git diff 0 확인. 실행 환경 참고: 이 세션은 격리된 git worktree(branch worktree-agent-a6441b325656830c7)에서 작업 — 세션 시작 시 main보다 92커밋 뒤처져 있어(Phase 999.4까지만 반영) 편집 전 `git merge main --ff-only`로 무손실 동기화 후 Task 1/2 커밋(a220a39, 42d404c) 진행. Unity 에디터를 열어 Task 3 메뉴를 실행하기 전, 실제 프로젝트 체크아웃에서 `git merge worktree-agent-a6441b325656830c7 --ff-only`로 이 두 커밋을 반드시 먼저 병합해야 함(충돌 없는 순수 fast-forward) |

### Key Decisions Locked (v1.0/v2.0/v3.0)

Full decision log lives in `.planning/PROJECT.md` Key Decisions table. Recent highlights:

- WaitForSecondsRealtime/Time.unscaledDeltaTime for all timers/i-frames — slow-motion immune (전 마일스톤 공통 제약, v3.1도 동일 적용)
- WorldGenerator (신규 MonoBehaviour)가 FloorSpawner 대체 — 수평 양방향 체인 생성
- ExitSpawnPoint 기반 랜덤 텔레포트로 허공 스폰 버그 근본 해결

### Technical Constraints to Enforce Every Phase

- `Time.unscaledDeltaTime` for ALL i-frame timers, cooldowns, coroutines, audio timing
- `Physics2D.OverlapCircle(ContactFilter2D, results[])` — never `FindObjectsOfType` or LINQ in Update
- Animator Transition Duration = 0 for all action-state transitions
- `Rigidbody2D`: Continuous collision detection + Interpolate mode
- Invincibility: layer swap (PlayerHurtbox / PlayerInvincible), never IgnoreLayerCollision
- Floor transition / boss room lifecycle: `WaitForSecondsRealtime` only
- Spawn VFX must hook `EnemySpawner.Activate()` only — never `Awake()`/`OnEnable()`

### Pending Todos

| Date | Title | Area | File |
|------|-------|------|------|
| _None pending_ | | | |

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260714-fnr | MeleeEnemy prefab ExclamationIcon sprite 미할당 수정 | 2026-07-14 | 93a3d99 | [260714-fnr-meleeenemy-prefab-exclamationicon-sprite](./quick/260714-fnr-meleeenemy-prefab-exclamationicon-sprite/) |
| 260715-kci | WorldGenerator.cs RemoveTail/RemoveHead/FloorTransitionSequence 중복 정리 로직을 CleanupSection() 헬퍼로 추출 (Phase 16 사전 정지 작업) | 2026-07-15 | 646a5f4 | [260715-kci-worldgenerator-cs-removetail-removehead-](./quick/260715-kci-worldgenerator-cs-removetail-removehead-/) |

---

## Session Continuity

**How to resume after /clear:**

1. Read `.planning/STATE.md` (this file) — current position and decisions
2. Read `.planning/ROADMAP.md` — v3.1 section active (Phase 13-17); v1.0/v2.0/v3.0 archived in collapsed `<details>`
3. Read `.planning/REQUIREMENTS.md` — v3.1 requirements + traceability (18/18 mapped)
4. Read `.planning/research/SUMMARY.md` — architecture/pitfall context for Phase 13-17
5. Read `.planning/phases/16-boss-room-lifecycle/16-CONTEXT.md` — 리팩토링 배치(D-01~D-08) + 미논의 보스 룸 그레이 에어리어 목록
6. Read `.planning/phases/15-fsm/15-CONTEXT.md` D-11/D-12 — SUPERSEDED 표시 확인 (Room_Debug 삭제, 점수 상쇄 우회책 불필요화 — 16-02/16-03/15-02 완료로 이제 코드에도 반영됨)
7. Next action: 15-05-PLAN.md Task 3(checkpoint:human-action) 재개 — 먼저 `git merge worktree-agent-a6441b325656830c7 --ff-only`로 Task 1/2 커밋을 병합한 뒤, Unity 에디터에서 컴파일 확인 → Fast/Phase15/Build Room_BossFsmTest → Fast/Phase15/Wire Boss Into BossFsmTest Room 실행 (15-05-PLAN.md Task 3 how-to-verify 참고). 15-04-PLAN.md는 SUPERSEDED — 실행하지 않음

**Last session:** 2026-07-16T04:22:42.446Z
**Stopped at:** Checkpoint reached in 15-05-PLAN.md Task 3 (checkpoint:human-action) -- Task 1/2 complete

---
*State initialized: 2026-05-27*
*Last updated: 2026-07-16 — Plan 15-05 Task 1/2(RoomBossFsmTestBuilder.cs 신설 + BossEnemyPrefabBuilder.cs 재배선, D-11 RE-RESOLVED checker fix) 완료 — Task 3(checkpoint:human-action)에서 대기 중. 15-04-PLAN.md는 SUPERSEDED로 남고 15-05가 최종 실행 플랜.*
