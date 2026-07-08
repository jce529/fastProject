# Project Retrospective

*A living document updated after each milestone. Lessons feed forward into future planning.*

## Milestone: v3.0 — 무한 복도 층 시스템

**Shipped:** 2026-07-08
**Phases:** 5 (Phase 8-12) | **Plans:** 23 | **Commits:** 125

### What Was Built
- 룸-길 수평 체인 아키텍처 (RoomConnector 마커, Corridor 3종 전투 프리팹)
- WorldGenerator 기반 무한 양방향 생성 & 자동 정리
- EXIT 포탈 확률적 스폰 + ExitSpawnPoint 기반 층 전환
- 슬로우모션 면역 타이머, 층별 난이도 스케일링, 시간 비례 점수
- 포탈 SpriteMask 연출, Whiff/Roll 애니메이션 수정, 히트 임팩트(스파크/쉐이크/트레일), 적 사망 파티클+Destroy 폴리싱

### What Worked
- `Time.unscaledDeltaTime` / `WaitForSecondsRealtime` 원칙을 Phase 1부터 못박아 둔 덕분에 Phase 11 타이머, Phase 12 애니메이션 연출까지 슬로우모션 면역 문제가 한 번도 재발하지 않음
- Room/Corridor를 Tilemap 방식으로 통일한 것(Phase 8)이 WorldGenerator의 좌표 기반 체인 계산(Phase 9)을 크게 단순화함
- 에디터 도구(RoomMarkerTool, CorridorBuilder, PortalEffectBuilder, PlayerAnimatorPatcher)로 프리팹/애니메이터 수정을 코드화해 재실행 가능하게 만든 패턴이 반복 검증에 유리했음

### What Was Inefficient
- ExitPortal의 RoomEntry 마커 기반 텔레포트가 허공 스폰 버그를 낳아 Phase 10에서 ExitSpawnPoint 기반으로 재작업 — 마커 설계를 한 번에 정했으면 피할 수 있었던 재작업
- 프리팹 fileID 오버플로우(Int64.MaxValue 초과)로 CameraBound 컴포넌트가 깨지는 사고가 발생(quick-260701-sc7) — 프리팹 생성 스크립트에 fileID 상한 검증이 처음부터 없었음
- Phase 12 슬러그 자동 생성 오류(`12-10-transition-design-md`)로 디렉토리를 수동 개명해야 했음

### Patterns Established
- 프리팹/애니메이터 변경은 항상 idempotent 에디터 도구로 작성 후 수동 실행 — 재현 가능성과 리뷰 가능성 확보
- 신규 마커/스폰 포인트 도입 시 항상 "이미 안전한 위치인 기존 마커를 재사용할 수 있는가"부터 검토 (ExitSpawnPoint 재사용 사례)
- 프리팹 GUID/fileID 문자열 비교 검증을 CorridorBuilder/PortalEffectBuilder 등 프리팹 생성 도구에 필수로 포함

### Key Lessons
1. 텔레포트/스폰 지점을 설계할 때는 새 마커를 추가하기 전에 기존에 안전이 검증된 마커(스폰 포인트 등)를 재사용할 수 있는지 먼저 확인한다.
2. 프리팹을 코드로 생성하는 에디터 도구는 fileID처럼 엔진이 암묵적으로 요구하는 상한/포맷 제약을 명시적으로 검증해야 한다.
3. 슬로우모션(Time.timeScale) 기능이 있는 프로젝트는 모든 타이머/코루틴 설계 시점에 `Time.unscaledDeltaTime` 원칙을 프로젝트 제약으로 못박아두면 이후 Phase에서 반복 점검할 필요가 없어진다.

### Cost Observations
- Sessions: 다수 (2026-06-28 ~ 2026-07-08, 약 10일)
- Notable: 에디터 도구화 패턴 덕분에 Unity Editor 수동 실행이 필요한 Task도 재현 가능한 스크립트로 관리되어 반복 플레이테스트 비용이 낮았음

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Phases | Key Change |
|-----------|--------|------------|
| v1.0 | 5 | 핵심 전투 메카닉(슬로우모션/돌진/게이지/구르기) 및 단일 룸 프로토타입 확립 |
| v2.0 | 2 | MainMenu/AttackSelect 씬 플로우로 게임 시작 경험 완성 |
| v3.0 | 5 | 수직 단일 룸 → 룸+복도 수평 무한 체인으로 층 구조 재설계, 애니메이션 폴리싱 추가 |

### Top Lessons (Verified Across Milestones)

1. `Time.unscaledDeltaTime` / `WaitForSecondsRealtime`을 슬로우모션 면역이 필요한 모든 타이머·코루틴의 기본값으로 삼는 제약은 v1.0에서 시작해 v3.0까지 한 번도 어긴 적이 없고 매 Phase마다 재검증 비용을 없애줌
