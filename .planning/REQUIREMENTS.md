# Requirements: Fast (가칭) — v3.0 무한 복도 층 시스템

**Defined:** 2026-06-28
**Core Value:** 공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.

## v3.0 Requirements

### 룸-길 아키텍처 (ARCH)

- [ ] **ARCH-01**: Room 프리팹에 END_Left/END_Right 마커가 있어 좌우 방향으로 Corridor와 연결될 수 있다
- [ ] **ARCH-02**: Corridor 프리팹 3종(상승/직진/하강)이 존재하며 각각 적+장애물이 있는 전투 구간이다
- [ ] **ARCH-03**: Corridor 시작점(ENT)에서 플레이어가 진입하고 끝점(END)에서 다음 Room이 연결된다

### 무한 생성 & 정리 (GEN)

- [ ] **GEN-01**: 플레이어 이동 방향 기준 앞 2개의 Room+Corridor가 자동으로 미리 생성된다
- [ ] **GEN-02**: 플레이어가 지나간 지점 기준 2개 초과 뒤의 Room+Corridor가 자동으로 Destroy된다
- [ ] **GEN-03**: Room은 룸 풀에서, Corridor는 3종 중 랜덤 선택된다

### EXIT 포탈 (EXIT)

- [ ] **EXIT-01**: 각 Room 스폰 시 정해진 스폰 포인트 중 하나에 낮은 확률(기본 15%)로 EXIT 포탈이 생성된다
- [ ] **EXIT-02**: 포탈 스폰 확률과 최대 동시 활성 개수를 FloorSpawner 인스펙터에서 조절할 수 있다
- [ ] **EXIT-03**: 플레이어가 EXIT 포탈에 진입하면 다음 층으로 전환되고 WorldGenerator가 초기화된다

### 타이머 & 게임오버 (TIMER)

- [ ] **TIMER-01**: 층 진입 시 HUD에 남은 제한 시간이 카운트다운으로 표시된다
- [ ] **TIMER-02**: 제한 시간 초과 시 게임오버가 발생한다

### 난이도 스케일링 (DIFF)

- [ ] **DIFF-01**: 층 번호가 올라갈수록 스포너에서 생성되는 몬스터 수가 증가한다

## Future Requirements

*(현재 없음 — 프로토타입 검증 완료 후 결정)*

## Out of Scope

| Feature | Reason |
|---------|--------|
| 보스 룸 / 특수 룸 이벤트 | 핵심 구조 검증 이후 추가 고려 |
| EXIT 포탈 연출 (이펙트/사운드) | 프로토타입 범위 초과; 기능 검증이 우선 |
| 층별 고정 레이아웃 (큐레이션 맵) | 랜덤 생성으로 리플레이어빌리티 검증이 목적 |
| 멀티플레이어 | 프로토타입 단계 외 |

## Traceability

*(로드맵 생성 후 채워짐)*

| Requirement | Phase | Status |
|-------------|-------|--------|
| ARCH-01 | — | Pending |
| ARCH-02 | — | Pending |
| ARCH-03 | — | Pending |
| GEN-01 | — | Pending |
| GEN-02 | — | Pending |
| GEN-03 | — | Pending |
| EXIT-01 | — | Pending |
| EXIT-02 | — | Pending |
| EXIT-03 | — | Pending |
| TIMER-01 | — | Pending |
| TIMER-02 | — | Pending |
| DIFF-01 | — | Pending |

**Coverage:**
- v3.0 requirements: 12 total
- Mapped to phases: 0 (roadmap pending)
- Unmapped: 12 ⚠️

---
*Requirements defined: 2026-06-28*
*Last updated: 2026-06-28 after initial definition*
