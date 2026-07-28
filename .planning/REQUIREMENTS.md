# Requirements: Fast (가칭)

**Defined:** 2026-07-08 (v3.1), updated 2026-07-20 (v4.0)
**Core Value:** 공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.

## v4.0 Requirements

Requirements for the 보스 캐릭터 확장 & 게임 모드 milestone. Each maps to roadmap phases.

### 공유 인프라 (INFRA)

- [x] **INFRA-01**: CombatController의 기존 Overclock(F.I.O.R.A) 로직이 IPlayerCombatModule 인터페이스로 무손상 마이그레이션된다 (기존 동작 100% 동일, 회귀 없음) — 코드 완료 2026-07-22, Task 3 human-verify 미기록
- ~~**INFRA-02**: 조준 방향 입력이 Mouse.current 대신 Pointer.current/EnhancedTouch 기반으로 동작해 Android 터치 기기에서도 정상 작동한다~~ **DESCOPED (2026-07-20, discuss-phase 18)** — 플랫폼 타겟이 PC로 재설정되어 터치 입력 요구사항 제거. 기존 Mouse.current 방식 유지.
- [x] **INFRA-03**: BossEnemy.cs에서 BossEnemyBase(EnemyBase와 별개의 형제 클래스)가 추출되어, 이후 신규 보스 4종이 이를 상속한다 — 코드 완료 2026-07-22, Task 3 human-verify 미기록

### 보스 언락 진행 (UNLOCK)

- [x] **UNLOCK-01**: 보스 격파 시 해당 보스의 전투 모듈이 영구 해금된다 (PlayerPrefs 기반, 앱 재시작 후에도 유지) — 코드 완료 2026-07-22, Task 3 human-verify 미기록
- [ ] **UNLOCK-02**: 플레이어는 해금된 모듈 중 하나를 게임 시작 전 선택할 수 있다 (기존 AttackSelect를 N-way로 확장)
- [ ] **UNLOCK-03**: 아직 해금되지 않은 모듈은 선택 화면에 잠금 상태로 표시된다

### SAMURAI 보스 + 패링 모듈 (SAMURAI)

- [ ] **SAMURAI-01**: SAMURAI 보스 격파 시 패링 모듈이 최초로 해금된다 (튜토리얼 보스, 최우선 해금)
- [ ] **SAMURAI-02**: 패링 모듈은 슬로우모션 없이 실시간으로 동작하며, 탭 입력 시 방향성 베기 공격을 수행한다
- [ ] **SAMURAI-03**: 적 공격과 타이밍이 겹치는 시점에 입력하면 패링이 발동해 공격을 무효화하고 투사체를 반사한다
- [ ] **SAMURAI-04**: SAMURAI 보스는 평시 전투와 간헐적 패링 전용 타이밍을 반복하며, 패링 전용 타이밍에 공격을 시도하면 플레이어는 즉사한다
- [ ] **SAMURAI-05**: 패링 판정 타이밍은 입력 지연을 고려해 넉넉하게 설정되고 실측 튜닝된다

### DeadEye 보스 + 탄약/재장전 모듈 (DEADEYE)

- [ ] **DEADEYE-01**: DeadEye 보스 격파 시 자원관리형 원거리 모듈이 해금된다
- [ ] **DEADEYE-02**: 홀드 시 슬로우모션 + 부채꼴 범위가 표시되고, 탭으로 범위 내 최대 6개 조준점을 지정한 뒤 별도 입력으로 일괄 발사한다
- [ ] **DEADEYE-03**: 발사한 탄수만큼 재장전이 필요하다 (부분 재장전 1초/발, 완전 소진 시 3초 전체 재장전)
- [ ] **DEADEYE-04**: DeadEye 보스는 플레이어를 추적하는 조준점 6개를 남긴 뒤 발사하며, 플레이어는 조준점을 피하며 보스를 타격해야 한다

### MAX 보스 + 순수 속도/관성 모듈 (MAXB)

- [ ] **MAXB-01**: MAX 보스 격파 시 순수 속도/관성 모듈이 해금된다
- [ ] **MAXB-02**: 이 모듈 사용 중 캐릭터는 멈출 수 없으며, 몸 자체가 적에게 닿으면 처치 판정이 발생한다
- [ ] **MAXB-03**: 홀드 시 슬로우모션이 발동해 동선을 미리 설계할 수 있다
- [ ] **MAXB-04**: 벽 또는 적의 공격에 충돌 시 플레이어가 즉사한다
- [ ] **MAXB-05**: MAX 보스는 멈추지 못하고 계속 돌진하며, 벽에 유도해 충돌시키면 스턴이 발생해 그 틈에 타격할 수 있다

### NOVA 보스 + 이원화 조작 모듈 (NOVA)

- [ ] **NOVA-01**: NOVA 보스 격파 시 이원화 조작(본체+드론) 모듈이 해금된다
- [ ] **NOVA-02**: 본체 이동과 공격 판정을 가진 드론의 조작이 동시에 독립적으로 가능하다
- [ ] **NOVA-03**: NOVA 보스는 본체가 회피 기동하는 동시에 드론을 조종해 플레이어의 진로를 막고 공격한다
- [ ] **NOVA-04**: 플레이어는 드론을 먼저 무력화하거나 본체를 직접 타격하는 두 가지 선택지를 가진다

### 게임 모드 (MODE)

- [ ] **MODE-01**: 한계 시험 모드에서는 해금된 모듈 중 단 하나만 선택해 로그라이크 층 등반에 진입한다
- [ ] **MODE-02**: 한계 시험 모드의 점수는 기존 ScoreManager 체계를 재사용한다

### WorldGenerator 보스룸 정리 예외 (WGEN)

- [ ] **WGEN-01**: 전투 중인 보스룸은 WorldGenerator의 앞뒤 정리(Destroy) 대상에서 예외 처리된다 (보스 타입 무관, Phase 16 BOSS-10 연장)

## v3.1 Requirements (파킹됨 — Phase 15 블로킹, 미완료)

Phase 15(fsm)이 Task 3(checkpoint:human-action)에서 블로킹된 채 v3.1이 파킹되어 완료되지 않은 항목. v4.0 완료 후 재검토 예정. 무효화된 것은 아니므로 그대로 보존.

### 보스 룸 (BOSS)

- [ ] **BOSS-01**: 보스 룸은 EXIT 포탈처럼 확률적으로 스폰된다 (동시 활성 1개 제한)
- [ ] **BOSS-02**: 보스 룸에는 일반 적이 스폰되지 않는다 (솔로 전투 보장)
- [x] **BOSS-03**: 보스는 공격 패턴(예고 → 빈틈) 루프를 반복하며, 빈틈 상태에서만 플레이어의 돌진 공격 대상이 된다
- [x] **BOSS-04**: 보스는 7회 피격 시 처치되며, 매 피격 후 공격 패턴이 처음부터 다시 시작된다
- [x] **BOSS-05**: 보스 처치 진행률(피격 횟수)은 플레이어에게 노출되지 않는다 (의도적 숨김)
- [x] **BOSS-06**: 보스 처치 시 점수 보너스가 지급된다 (ScoreManager 연동)
- [ ] **BOSS-07**: 보스 룸 입장 시 층 타이머가 일시정지되고, 보스방을 벗어나면(처치 또는 EXIT 이용) 재개된다
- [ ] **BOSS-08**: 보스 처치 후에도 층 진입은 기존 EXIT 포탈을 통해서만 가능하다 (보스 처치가 자동으로 층을 넘기지 않음)
- [ ] **BOSS-09**: 보스 룸 입장 시 카메라 연출(잠금/줄임)이 재생된다
- [ ] **BOSS-10**: 전투 중에는 WorldGenerator의 앞뒤 2개 유지 정리(Destroy) 로직에서 보스방이 예외된다 (전투 중 파괴 방지) — v4.0의 WGEN-01로 범위 확장되어 이어짐

### 적 등장 연출 (SPWN)

- [x] **SPWN-01**: 일반 적(근접/원거리)과 보스가 스폰될 때 플레이어처럼 포탈을 타고 등장하는 연출이 재생된다
- [x] **SPWN-02**: 스폰 연출이 끝나기 전까지 적은 감지/공격 대상이 되지 않는다

### 연출 개선 / 오디오 (SFX)

- [x] **SFX-01**: 기본 오디오 재생 인프라(AudioManager)가 추가된다 (현재 프로젝트에 오디오가 전혀 없음)
- [x] **SFX-02**: 포탈 전환에 사운드가 추가된다
- [x] **SFX-03**: 히트 임팩트에 사운드가 추가된다
- [x] **SFX-04**: 적 사망에 사운드가 추가된다
- [ ] **SFX-05**: 보스 스폰에 전용 사운드가 추가된다
- [x] **SFX-06**: 포탈 전환/히트/사망 연출의 타이밍·피드백 어색함이 개선된다 (v3.0 Phase 12 폴리싱 갭 해소)

## Future Requirements

Deferred to future release. Tracked but not in current roadmap.

### 보스 러시 모드

- **RUSH-01**: 보스전만 연속으로 이어지는 엔드리스 모드, 전투 중 해금된 모듈 자유 전환 (v4.1 후보 — endless 생성 로직 리서치에서 최고 위험 항목으로 지목, 4개 모듈이 개별 안정화된 후 착수)

### 온보딩

- **TUTO-01**: 신규 플레이어를 위한 조작법 설명 화면/플로우 (별도 마일스톤에서 다룸)

### 레이아웃 다양성

- **LAYOUT-01**: 프리셋 기반 고정 층 레이아웃 (현재 Complex_Room 6종 랜덤 풀 대체 검토)

### 컨트롤 스킴 대안

- **NOVA-05**: 실기 검증 후 NOVA 조작이 지나치게 어렵다고 판단되면 토글/포제션-스왑 대체 컨트롤 스킴 검토 (원안은 동시 듀얼 조작 유지가 v4.0 기본값)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| 보스 러시 모드 | v4.0에서 제외 — endless 보스 전용 생성 로직이 미설계 상태이며 4개 모듈이 개별 검증된 후 착수하는 것이 안전 (Future Requirements RUSH-01) |
| 모듈 업그레이드 티어 / 패시브 스킬트리 / 상점 | 핵심 검증과 무관, PROJECT.md Out of Scope 유지 |
| 클라우드 저장 / 기기 간 동기화 | 로컬 단일 기기 프로토타입 범위 밖 — PlayerPrefs로 충분 |
| 보스 HP바 / 멀티페이즈 전투 | 원샷원킬 코어 밸류와 충돌 — HP 시스템은 이 게임 어디에도 존재하지 않음 |
| 보스 다이얼로그 / 네임카드 컷신 | 6개 프로토타입 검증 목표와 무관, 내러티브 검증 목적 없음 |
| MAX 물리 기반 드리프트/바운스 시뮬레이션 고도화 | v4.0은 원안 스펙(정지 불가+충돌 즉사) 구현까지만, 물리 디테일 고도화는 플레이테스트 후 재검토 |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 18 | Code complete (2026-07-22), Task 3 human-verify not recorded |
| INFRA-02 | Phase 18 | Descoped (2026-07-20, PC 전환) |
| INFRA-03 | Phase 18 | Code complete (2026-07-22), Task 3 human-verify not recorded |
| UNLOCK-01 | Phase 18 | Code complete (2026-07-22), Task 3 human-verify not recorded |
| UNLOCK-02 | Phase 19 | Pending |
| UNLOCK-03 | Phase 19 | Pending |
| SAMURAI-01 | Phase 19 | Pending |
| SAMURAI-02 | Phase 19 | Pending |
| SAMURAI-03 | Phase 19 | Pending |
| SAMURAI-04 | Phase 19 | Pending |
| SAMURAI-05 | Phase 19 | Pending |
| DEADEYE-01 | Phase 20 | Pending |
| DEADEYE-02 | Phase 20 | Pending |
| DEADEYE-03 | Phase 20 | Pending |
| DEADEYE-04 | Phase 20 | Pending |
| MAXB-01 | Phase 22 | Pending |
| MAXB-02 | Phase 22 | Pending |
| MAXB-03 | Phase 22 | Pending |
| MAXB-04 | Phase 22 | Pending |
| MAXB-05 | Phase 22 | Pending |
| NOVA-01 | Phase 23 | Pending |
| NOVA-02 | Phase 23 | Pending |
| NOVA-03 | Phase 23 | Pending |
| NOVA-04 | Phase 23 | Pending |
| MODE-01 | Phase 24 | Pending |
| MODE-02 | Phase 24 | Pending |
| WGEN-01 | Phase 21 | Pending |

**Coverage:**
- v4.0 requirements: 27 total (26 active + 1 descoped)
- Mapped to phases: 26 active (Phase 18-24, roadmap created 2026-07-20); INFRA-02 descoped 2026-07-20 (platform reset to PC)
- Unmapped: 0

---
*Requirements defined: 2026-07-08 (v3.1)*
*Last updated: 2026-07-20 — v4.0 roadmap created (Phase 18-24). All 27 v4.0 requirements mapped, no orphans. v3.1's 8 pending requirements preserved as parked (not invalidated). Boss Rush deferred to Future Requirements (RUSH-01). Same-day discuss-phase 18 session: platform target reset from Android/mobile to PC — INFRA-02 descoped, SAMURAI-05 touch-latency wording generalized.*
