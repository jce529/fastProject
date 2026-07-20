# Fast (가칭)

## What This Is

모바일 2D 가로 화면 플랫포머 액션 게임 프로토타입. 플레이어는 끝없이 이어지는 탑을 올라가며, 공격 버튼을 누르면 슬로우 모션이 발동하고 손을 떼면 범위 안 가장 가까운 적에게 돌진해 원샷으로 처치한다. 이 핵심 전투 메카닉과 구르기를 이용한 회피 시스템이 모바일에서 실제로 재미있는지 검증하는 것이 목적이다.

## Core Value

**공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.**

## Current State (v3.0 shipped 2026-07-08)

층 구조가 수직 단일 룸에서 룸+복도 수평 체인으로 재설계되었다. 플레이어가 좌우로 진행하면 앞쪽 2개 Room+Corridor가 자동 생성되고 뒤쪽은 자동 정리되며, 룸 스폰 시 확률적으로 나타나는 EXIT 포탈로 진입하면 층 번호가 오르고 60초 카운트다운 타이머(슬로우모션 면역)와 층별 난이도 스케일링, 시간 비례 점수가 적용된다. 포탈 전환과 전투에는 SpriteMask 연출, 히트 스파크, 카메라 쉐이크, 대시 트레일, 적 사망 페이드 등 애니메이션 폴리싱이 적용된 상태. `MainMenu → AttackSelect → SampleScene` 전체 플로우가 개발자 개입 없이 반복 가능하다.

전체 상세: `.planning/MILESTONES.md`, `.planning/milestones/v3.0-ROADMAP.md`

## Current Milestone: v4.0 보스 캐릭터 확장 & 게임 모드

**Goal:** F.I.O.R.A(이미 구현된 Overclock Mode의 원본)를 제외한 4개 신규 보스(DeadEye/SAMURAI/MAX/NOVA)의 고유 전투 메커니즘과 보스 패턴을 구현하고, 보스 격파→능력 해금 진행 시스템과 게임 모드 2종(한계 시험/보스 러시)을 추가한다.

**Target features:**
- DeadEye 보스: 6연발 조준/재장전 자원관리 메커니즘 + 보스 패턴(조준점 6개 남기고 발사)
- SAMURAI 보스: 패링(반사/정타이밍) 메커니즘 + 보스 패턴 (튜토리얼 보스, 최우선 해금)
- MAX 보스: 순수 속도/관성 이동=공격, 벽과 충돌 시 즉사 리스크 + 보스 패턴(벽 유도→스턴→타격)
- NOVA 보스: 이원화 조작(본체+드론) 메커니즘 + 보스 패턴(본체 회피 + 드론 견제)
- 보스 격파 → 능력 해금 진행 시스템 (해금된 모듈 저장/선택)
- 게임 모드: 한계 시험(단일 모듈 로그라이크 층 등반), 보스 러시(자유 전환 엔드리스)

**Key context:** v3.1(보스 룸 & 연출 고도화)은 Phase 15(fsm) Task 3(human-action 체크포인트)에서 블로킹된 채 미완료 상태로 파킹됨. Phase 16(boss-room-lifecycle)/17 미착수. v4.0은 v3.1이 구축한 보스 룸 프레임워크(BossEnemy, FSM) 위에서 이어감.

<details>
<summary>이전 Current Milestone 섹션 (v3.1, 미완료 파킹 — Phase 15 블로킹)</summary>

## Current Milestone: v3.1 보스 룸 & 연출 고도화

**Goal:** 보스 룸 콘텐츠(1종, 확장 가능한 프레임워크)를 추가하고, 기존 포탈/히트/사망 연출에 사운드와 타이밍 개선을 더하고, 적 등장(스폰) 연출을 신설한다.

**Target features:**
- 보스 룸: EXIT 포탈처럼 확률적 스폰, 보스 1종(공격 패턴+스프라이트+아레나 구조 모두 고유), 솔로 전투, 처치 시 점수 보너스, 층 진입은 기존 EXIT 포탈 그대로 필요
- 적 등장 연출: 플레이어처럼 포탈을 타고 나오는 스폰 연출 신설
- 기존 연출 개선: 포탈전환/히트/사망에 사운드 추가 + 타이밍·피드백 어색함 수정

</details>

<details>
<summary>이전 Current Milestone 섹션 (v3.0 진행 중 스냅샷, 아카이브)</summary>

## Current Milestone: v3.0 무한 복도 층 시스템

**Goal:** 층을 룸+길 수평 체인으로 재설계 — 양방향 무한 생성, 전투 Corridor, 확률적 EXIT 포탈, 제한 시간으로 "빠른 탈출" 긴장감을 검증한다.

**Target features:**
- 룸-길 아키텍처: Room에 END_Left/END_Right 마커, Corridor 3종 (상승/직진/하강, 모두 전투 구간)
- 무한 양방향 생성 & 정리: 플레이어 기준 앞 2개 룸+길 미리 생성, 2개 초과 Destroy
- EXIT 포탈: 룸 내 정해진 스폰 포인트에 확률적 스폰 (FloorSpawner 인스펙터로 확률/최대 개수 조절)
- 타이머 & 게임오버: 층별 제한 시간 카운트다운 HUD, 초과 시 게임오버 (완료 — Phase 11)
- 난이도 스케일링: 층 번호에 따라 스포너 몬스터 수 증가 (완료 — Phase 11)

</details>

## Requirements

### Validated

**플레이어 조작 (Validated in v1.0 Phase 1-2)**
- [x] 플레이어가 빠르게 좌우 이동 및 점프할 수 있다 — v1.0 (MOVE-01)
- [x] 공격 버튼 누름 시 슬로우 모션 발동, 공격 범위 표시, 손을 떼면 돌진 공격 — v1.0 (ATCK-02/03)
- [x] 별도 버튼으로 구르기 발동 — 무적 판정 + 쿨타임, 슬로우 중에도 사용 가능 — v1.0 (MOVE-03)
- [x] 낙사 시 사망하지 않고 마지막 플랫폼으로 복귀 + 짧은 무적 — v1.0 (MOVE-02)

**공격 시스템 (Validated in v1.0 Phase 2)**
- [x] 직선형 / 부채꼴형 두 가지 공격 타입 중 게임 시작 전 선택 — v1.0/v2.0 (ATCK-01, 이후 AttackSelect 씬으로 이전)
- [x] 공격 범위 안 가장 가까운 적 자동 선택 후 돌진 — 돌진 중 무적 — v1.0 (ATCK-03)
- [x] 범위 안 적 없으면 헛베기 (적 처치 성공보다 긴 딜레이) — v1.0 (ATCK-04)
- [x] 시간정지 게이지: 자동 회복 + 적 처치 시 회복, 게이지 소진 시 슬로우 해제되나 공격 입력은 유지 — v1.0 (ATCK-05)

**적 (Validated in v1.0 Phase 3)**
- [x] 근접형 적: 접근 후 공격 예고 모션 → 근접 공격 (원샷원킬 양방향) — v1.0 (ENMY-01)
- [x] 원거리형 적: 조준선 표시 후 투사체 발사 (원샷원킬 양방향) — v1.0 (ENMY-02)

**층 구조 및 전환 (Validated in v1.0 Phase 5, 재설계 v3.0 Phase 8-12)**
- [x] 출구 도달 시 층 전환 시퀀스 (조작 불가 → 이동 → 카메라 → 적 활성화 → 재개) — v1.0 (FLOOR-02), v3.0에서 EXIT 포탈 + SpriteMask 연출로 재구현 (EXIT-03, Phase 12)
- [x] 이전 층 제거/비활성화 (모바일 성능) — v1.0 (FLOOR-04), v3.0에서 룸+길 단위로 재구현 (GEN-02)
- [x] 룸-길 수평 체인 아키텍처 + 양방향 무한 생성/정리 — v3.0 (ARCH-01/02/03, GEN-01/02/03)
- [x] EXIT 포탈 확률적 스폰 + 층 전환 — v3.0 (EXIT-01/02/03)
- [x] 층별 제한 시간 카운트다운 + 시간 초과 게임오버 — v3.0 (TIMER-01/02)
- [x] 층 번호 기반 난이도 스케일링 — v3.0 (DIFF-01)
- [x] 시간 비례 점수 시스템 — v3.0 (SCORE-01/02)

**UI / 피드백 (Validated in v1.0 Phase 4, v2.0 Phase 6-7, v3.0 Phase 11-12)**
- [x] HUD: 현재 층 수, 시간정지 게이지, 공격 타입 표시, 타이머, 점수 — v1.0 (UI-01), v3.0 확장 (TIMER-01, SCORE-02)
- [x] 사망 화면 + 재시작 버튼 — v1.0 (UI-02)
- [x] MainMenu → AttackSelect → SampleScene 게임 시작 플로우, 사망 후 AttackSelect 복귀 — v2.0 (MENU-01/02/03, ATKS-01/02/03, FLOW-01)
- [x] 포탈 전환/히트 임팩트/적 사망 애니메이션 폴리싱 — v3.0 (Phase 12)

**오디오 (Validated in v3.1 Phase 13)**
- [x] AudioManager 싱글턴 신설 (2채널 풀, DSP 512, 3씬 전환 간 생존) — v3.1 (SFX-01)
- [x] 포탈 진입/퇴장, 대시 히트, 적 사망 사운드 재생 + 슬로우모션 피치 무결성 — v3.1 (SFX-02/03/04)
- [x] 포탈/히트/사망 연출 타이밍·피드백 어색함 개선 — v3.1 (SFX-06, 플레이테스트 결과 전 항목 OK로 폴리싱 불요 판정)

**적 등장 연출 (Validated in v3.1 Phase 14)**
- [x] 근접형/원거리형 적이 스폰될 때 포탈을 타고 등장하는 연출 재생 (Room+Corridor 3종 동등) — v3.1 (SPWN-01, 보스 스폰 연출은 Phase 16에서 별도 검증)
- [x] 스폰 연출이 끝나기 전까지 적은 감지/공격 대상이 되지 않는다 (IsAlive 게이팅, IEnemy 계약 불변) — v3.1 (SPWN-02)

### Active

*(비어 있음 — `/gsd:new-milestone`으로 다음 마일스톤 요구사항 정의 예정)*

### Out of Scope

- 복잡한 성장 시스템 (레벨업, 영구 강화, 상점) — 핵심 전투 검증이 목적, 성장은 검증 후 추가
- 이단 점프 / 벽점프 / 벽타기 / 공중 대시 — 기본 점프만으로 검증, 추후 업데이트 후보
- 별도 대시 버튼 (이동 목적의 대시) — 돌진은 공격에 귀속, 순수 이동 대시는 게임성 오염
- 랭킹, 광고, 과금 — 프로토타입 단계 외
- 콤보 시스템, 무기 강화 — 핵심 검증과 무관 (패링은 v4.0에서 SAMURAI 보스 메커니즘으로 In Scope 전환)

## Context

**기술 환경:** Unity 6000.3.11f1 LTS, C#, Universal Render Pipeline 2D Renderer, Unity Input System 1.19.0. Android 우선 (minSdk 25 / ARM64), iOS 추후. 가로 화면(1920×1080 기본).

**프로토타입 검증 목표 (6개):**
1. 공격 버튼 누르기/떼기 전투가 재미있는가?
2. 직선형 vs 부채꼴형 — 어떤 공격 범위가 모바일에 더 적합한가?
3. 원샷원킬 구조가 모바일 조작에서 불쾌하지 않은가?
4. 무한 탑 등반 구조가 자연스럽게 이어지는가?
5. 적을 모두 죽이지 않아도 되는 등반 플레이가 재미있는가?
6. 낙사 복귀 방식이 액션감을 해치지 않는가?

*(위 6개 목표는 여전히 유효한 검증 기준 — v3.0까지는 기능 구현/플레이테스트 통과 위주로 진행되었고, 정식 사용자 플레이테스트 데이터 수집은 아직 진행 전)*

**코드베이스 현황 (v3.0 shipped 기준):** MainMenu/AttackSelect/SampleScene 3씬 구성. PlayerController/CombatController/GaugeController/RollController(전투), MeleeEnemy/RangedEnemy FSM(적), WorldGenerator/RoomConnector/ExitPortal/FloorTimer/ScoreManager(룸-길 무한 생성 + 층 전환 + 타이머/점수), HUDController/DeathScreenController(UI), FloorTransitionEffect/PortalEffectBuilder/HitSparkBuilder 등 애니메이션 폴리싱 컴포넌트. Room 프리팹은 Tilemap 기반(Complex_Room 6종), Corridor 3종은 TilemapCollider2D 기반.

**v4.0 이후 후보 (이번 마일스톤에서 제외):**
- 튜토리얼 (온보딩 UI/조작법 설명) — 별도 마일스톤으로 분리
- 프리셋 기반 고정 층 레이아웃 (현재 Complex_Room 6종 랜덤 풀)
- 실제 플레이테스트로 6개 프로토타입 검증 목표 결과 수집
- v3.1 잔여 범위(연출 사운드/타이밍 개선, 보스 룸 라이프사이클)는 v4.0 완료 후 재검토

## Constraints

- **Tech Stack**: Unity 6 LTS + C# — 이미 설정된 프로젝트 환경
- **Platform**: Android 우선 (ARM64, minSdk 25) — 성능 예산 고려 필요
- **Scope**: 핵심 메카닉 검증에만 집중 — 프로토타입 외 기능 추가 금지
- **Performance**: 현재 층 + 다음 층만 유지, 이전 층 제거 — 모바일 메모리 관리

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 구르기 추가 (별도 버튼, 무적, 쿨타임) | 공격 돌진(공격 귀속)과 달리 순수 회피기 — 공격 사이 생존 선택지 제공 | ✓ Good — v1.0 구현, i-frame은 WaitForSecondsRealtime으로 슬로우모션 면역 |
| 대시 버튼 미포함 (구르기와 구분) | 돌진은 공격 시스템의 일부, 이동 목적 대시는 전투 긴장감 희석 | ✓ Good — v1.0 이후 재검토 없음 |
| 슬로우 중 구르기 허용 | 공격 조준 취소 → 회피 선택 → 플레이어의 의사결정 깊이 증가 | ✓ Good — v1.0 구현 |
| 시간정지 게이지 회복: 자동 + 적 처치 | 단순 자동 회복(테스트 용이) + 처치 보상 감각(공격적 플레이 유도) 동시 충족 | ✓ Good — v1.0 구현 |
| 사망 후 재시작 지점 | 1층 재시작(v1.0) → AttackSelect 복귀(v2.0)로 조정 | ✓ Good — 씬 플로우 정착 후 변경 없음 |
| 원샷원킬 (플레이어·적 모두) | 전투 긴장감 유지, 체력 시스템 없이 간결한 난이도 표현 | ✓ Good — v1.0~v3.0 전 구간 유지 |
| Time.timeScale 보정을 Phase 1에 선반영 | 1f/Time.timeScale을 ApplyMovement에 미리 넣어 Phase 2 슬로우모션 도입 시 PlayerController 재작성 불필요 | ✓ Good |
| WaitForSecondsRealtime로 i-frame/타이머/층전환 통일 | Time.timeScale이 슬로우모션·전환 중 0에 가까워질 수 있어 실시간 타이머만 안전 | ✓ Good — v1.0부터 v3.0까지 일관 적용된 핵심 제약 |
| WorldGenerator 신규 MonoBehaviour로 FloorSpawner 대체 | 수평 양방향 체인 생성은 기존 수직 로직과 구조가 달라 대체가 가장 깔끔 | ✓ Good — v3.0 Phase 9 |
| Room 14종 → Tilemap 방식 전환, Complex_Room 6종으로 축소 | Corridor가 이미 TilemapCollider2D 전환됨 — Room도 통일해 좌표 기반 연결 계산 가능하게 함 | ✓ Good — v3.0 Phase 8 |
| ExitSpawnPoint 기반 랜덤 텔레포트로 교체 (RoomEntry 마커 폐기) | ExitSpawnPoint가 이미 안전 위치이므로 재사용 — 허공 스폰 버그 근본 해결 | ✓ Good — v3.0 Phase 10 |
| 대기룸(StandbyRoom) 난이도는 FloorManager.CurrentFloor + 1 기준 계산 | 대기룸은 미래 층에서 플레이될 방이므로 그 미래 층 번호로 난이도 조회해야 정합성 유지 | ✓ Good — v3.0 Phase 11 |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd:transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-07-20 — 마일스톤 v4.0(보스 캐릭터 확장 & 게임 모드) 시작. v3.1은 Phase 15 블로킹 상태로 파킹. Out of Scope에서 패링 제외 항목 제거(v4.0에서 SAMURAI 메커니즘으로 In Scope 전환).*
