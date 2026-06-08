# Requirements: Fast (가칭)

**Defined:** 2026-05-27
**Core Value:** 공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.

## v1 Requirements

전투 핵심 루프 검증 우선. 단일 테스트 씬에서 이동/전투/적/UI를 완성 후, 층 시스템은 v2에서 추가한다.

### 이동 (Movement)

- [x] **MOVE-01**: 플레이어가 빠른 속도로 좌우 이동 및 점프할 수 있다 (가속감 있음, 조작 즉각 반응, 공중 방향 제어 가능)
- [x] **MOVE-02**: 낙사 시 사망하지 않고 마지막으로 밟은 플랫폼 위치로 복귀하며, 복귀 직후 짧은 무적이 부여된다
- [x] **MOVE-03**: 별도 버튼으로 구르기가 발동된다 — 구르기 중 무적 판정, 쿨타임 있음, 슬로우 모션 중에도 사용 가능

### 전투 (Combat)

- [x] **ATCK-01**: 게임 시작 전 직선형 / 부채꼴형 공격 타입을 선택할 수 있다 (단순 버튼 2개)
- [x] **ATCK-02**: 공격 버튼을 누르고 있으면 슬로우 모션이 발동되고 공격 범위가 표시된다 (시간정지 게이지 소모)
- [x] **ATCK-03**: 공격 버튼을 떼면 공격 범위 내 가장 가까운 적에게 돌진하여 원샷 처치한다 (돌진 중 무적, 처치 후 짧은 딜레이)
- [x] **ATCK-04**: 공격 범위 내 적이 없으면 헛베기 애니메이션 재생 후 더 긴 페널티 딜레이가 발생한다
- [x] **ATCK-05**: 시간정지 게이지는 시간이 지나면 자동 회복되고, 적 처치 시에도 일부 회복된다

### 타격감 (Game Feel)

- [x] **FEEL-01**: 적 처치 시 히트프리즈 발생 (50-100ms `Time.timeScale = 0`) — 킬의 타격감 핵심

### 적 (Enemies)

- [x] **ENMY-01**: 근접형 적이 플레이어를 감지하면 접근하고, 공격 전 예고 모션 후 근접 공격한다 (원샷원킬 양방향)
- [x] **ENMY-02**: 원거리형 적이 플레이어를 감지하면 조준선을 표시 후 투사체를 발사한다 (원샷원킬 양방향)

### UI

- [ ] **UI-01**: HUD에 현재 층 번호, 시간정지 게이지, 선택한 공격 타입이 표시된다
- [ ] **UI-02**: 플레이어 사망 시 사망 화면과 재시작 버튼이 표시되며, 재시작 시 1층부터 시작한다

## v2 Requirements

### 층 시스템 (Floor System)

- **FLOOR-01**: 프리셋 기반 층 생성 (3~5개 프리셋 — 플랫폼/사다리/계단/낙사/혼합)
- **FLOOR-02**: 위쪽 출구 도달 시 층 전환 시퀀스 발동 (조작 불가 → 순간이동 → 카메라 상승 → 가림막 해제 → 적 인식 활성화 → 조작 재개)
- **FLOOR-03**: 층 전환 중 적 비활성화 — 카메라 전환 완료 후에만 플레이어 인식 시작
- **FLOOR-04**: 이전 층 제거/비활성화 (모바일 성능 — 현재+다음 층만 유지)

### 모바일 컨트롤 (Mobile Controls)

- **MOBI-01**: 온스크린 조이스틱(좌측) + 점프/공격/구르기 버튼(우측) 표시 및 동작
- **MOBI-02**: SafeArea 적용 (노치/펀치홀 대응)

### 난이도 진행 (Difficulty Progression)

- **DIFF-01**: 층이 높아질수록 적 수 증가, 원거리 적 비율 증가, 낙사 구역 증가

## Out of Scope

| Feature | Reason |
|---------|--------|
| 별도 대시 버튼 (이동용) | 돌진은 공격 귀속 — 이동 대시는 전투 긴장감 희석 |
| 이단 점프 / 벽점프 / 벽타기 | 기본 점프만으로 검증, 추후 업데이트 후보 |
| 복잡한 성장 시스템 (레벨업, 영구 강화, 상점) | 프로토타입은 핵심 전투 검증 목적 |
| 보스전 | 프로토타입 범위 초과 |
| 랭킹, 광고, 과금 | 프로토타입 단계 외 |
| 콤보 시스템, 패링, 무기 강화 | 핵심 검증과 무관 |
| HP/체력 바 (플레이어·적 모두) | 원샷원킬 구조 — 체력 시스템 없음 |
| 스토리, 컷씬 | 검증 목적 프로토타입에 불필요 |
| Visual Scripting (Bolt) | IL2CPP 빌드 시 스트리핑 문제 위험 |

## Traceability

*Updated: 2026-05-27 after roadmap creation*

| Requirement | Phase | Status |
|-------------|-------|--------|
| MOVE-01 | Phase 1 | Complete |
| MOVE-02 | Phase 1 | Complete |
| MOVE-03 | Phase 2 | Complete |
| ATCK-01 | Phase 2 | Complete |
| ATCK-02 | Phase 2 | Complete |
| ATCK-03 | Phase 2 | Complete |
| ATCK-04 | Phase 2 | Complete |
| ATCK-05 | Phase 2 | Complete |
| FEEL-01 | Phase 2 | Complete |
| ENMY-01 | Phase 3 | Complete |
| ENMY-02 | Phase 3 | Complete |
| UI-01 | Phase 4 | Pending |
| UI-02 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 13 total
- Mapped to phases: 13/13
- Unmapped: 0

---
*Requirements defined: 2026-05-27*
*Last updated: 2026-05-27 after roadmap creation*
