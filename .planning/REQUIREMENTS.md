# Requirements: Fast (가칭)

**Defined:** 2026-07-08
**Core Value:** 공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.

## v3.1 Requirements

Requirements for the 보스 룸 & 연출 고도화 milestone. Each maps to roadmap phases.

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
- [ ] **BOSS-10**: 전투 중에는 WorldGenerator의 앞뒤 2개 유지 정리(Destroy) 로직에서 보스방이 예외된다 (전투 중 파괴 방지)

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

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### 온보딩

- **TUTO-01**: 신규 플레이어를 위한 조작법 설명 화면/플로우 (별도 마일스톤에서 다룸)

### 레이아웃 다양성

- **LAYOUT-01**: 프리셋 기반 고정 층 레이아웃 (현재 Complex_Room 6종 랜덤 풀 대체 검토)

### 보스 콘텐츠 확장

- **BOSS-11**: 두 번째/세 번째 보스 타입 (프레임워크 확장 검증 후)
- **BOSS-12**: 아레나 환경 해저드 (기본 전투 루프 검증 후)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| 보스 HP바 / 멀티페이즈 전투 | 원샷원킬(빈틈 타겟팅 + 7회 피격) 코어 밸류와 충돌 — HP 시스템은 이 게임 어디에도 존재하지 않음 |
| 어댑티브/다이나믹 보스 음악 | 음악 시스템 자체가 전무 — 이번 마일스톤은 SFX 폴리싱까지만 |
| 보스 다이얼로그/네임카드 컷신 | 6개 프로토타입 검증 목표와 무관, 내러티브 검증 목적 없음 |
| 콤보/프레임 퍼펙트 히트스탑 시스템 | 콤보 시스템 자체가 Out of Scope (기존 PROJECT.md) |
| 튜토리얼 | 이번 마일스톤은 보스 룸+연출로 범위를 좁힘 — 별도 마일스톤으로 분리 |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| BOSS-01 | Phase 16 | Pending |
| BOSS-02 | Phase 16 | Pending |
| BOSS-03 | Phase 15 | Complete |
| BOSS-04 | Phase 15 | Complete |
| BOSS-05 | Phase 15 | Complete |
| BOSS-06 | Phase 15 | Complete |
| BOSS-07 | Phase 16 | Pending |
| BOSS-08 | Phase 17 | Pending |
| BOSS-09 | Phase 16 | Pending |
| BOSS-10 | Phase 16 | Pending |
| SPWN-01 | Phase 14 | Complete |
| SPWN-02 | Phase 14 | Complete |
| SFX-01 | Phase 13 | Complete |
| SFX-02 | Phase 13 | Complete |
| SFX-03 | Phase 13 | Complete |
| SFX-04 | Phase 13 | Complete |
| SFX-05 | Phase 16 | Pending |
| SFX-06 | Phase 13 | Complete |

**Coverage:**
- v3.1 requirements: 18 total
- Mapped to phases: 18
- Unmapped: 0

---
*Requirements defined: 2026-07-08*
*Last updated: 2026-07-08 — v3.1 roadmap created, all 18 requirements mapped to Phase 13-17*
