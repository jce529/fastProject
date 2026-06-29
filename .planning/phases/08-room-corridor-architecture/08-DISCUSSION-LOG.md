# Phase 8: 룸-길 아키텍처 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-29
**Phase:** 08-room-corridor-architecture
**Areas discussed:** 기존 Room 프리팹 처리, Corridor 물리 레이아웃, RoomConnector 직렬화 구조, Phase 8 검증 씬

---

## 기존 Room 프리팹 처리

| Option | Description | Selected |
|--------|-------------|----------|
| 기존 프리팹에 마커 추가 | 이미 있는 15개 Room 프리팹에 END_Left/END_Right 마커 추가 | ✓ |
| 신규 Room 2-3개만 제작 | v3.0 전용 신규 프리팹 2-3개 제작 후 기존은 차차 전환 | |
| 기존과 완전 분리 | v3.0 전용 프리팹 세트를 별도 폴더에 신규 제작 | |

**User's choice:** 기존 프리팹에 마커 추가

| Option | Description | Selected |
|--------|-------------|----------|
| 전체 15개 전부 | Phase 8에서 15개 모두 마커 추가 | |
| 4-5개 먼저 | Phase 8에서 4-5개만, 나머지는 Phase 9에서 | ✓ |

**User's choice:** 4-5개 먼저

---

## Corridor 물리 레이아웃

| Option | Description | Selected |
|--------|-------------|----------|
| 계단 플랫폼 | 단차 이어진 여러 플랫폼으로 상승/하강 | ✓ |
| 경사로 | Tilemap 또는 메시로 만든 경사로 | |
| 허공 점프 발판 | 지면 없이 발판 간 점프로 이동 | |

**User's choice:** 계단 플랫폼

| Option | Description | Selected |
|--------|-------------|----------|
| 최소: 플랫폼 + 스폰 포인트만 | 계단 플랫폼 + EnemySpawnPoint 자리만 | ✓ |
| 일반: 플랫폼 + 스폰 포인트 + 간단한 장애물 | 추가 두께별 플랫폼이나 엄폐 구조 포함 | |

**User's choice:** 최소: 플랫폼 + 스폰 포인트만

---

## RoomConnector 직렬화 구조

| Option | Description | Selected |
|--------|-------------|----------|
| Transform 2개 필드 | [SerializeField] Transform leftConnector, rightConnector (Room 루트에 하나) | |
| Direction enum + 리스트 | ConnectorSlot 배열 (다방향 확장 가능) | |
| 가장자리 오브젝트 직접 부착 | 기존 끝 오브젝트에 RoomConnector 컴포넌트를 직접 붙임 | ✓ |

**User's choice:** 가장자리 오브젝트에 직접 부착
**Notes:** "현재 구조가 타일맵이 아니라 개별 오브젝트로 바닥을 구성하는 형태라, 오른쪽/왼쪽 끝 오브젝트 자체에 컴포넌트를 붙여서 거기서부터 연결하려 한다." Direction(Left/Right) 열거형과 연결 GameObject 필드를 직렬화.

---

## Phase 8 검증 씬

| Option | Description | Selected |
|--------|-------------|----------|
| SampleScene에 수동 배치 | 기존 SampleScene에 Room+Corridor+Room 수동 배치 후 플레이테스트 | ✓ |
| 신규 아키텍처 테스트 씬 | ArchTest.unity 등 별도 씬 생성 | |

**User's choice:** SampleScene에 수동 배치

---

## Claude's Discretion

- RoomConnector Gizmo 시각 표현 방식 (구/화살표, 색상 코드)
- Corridor ENT 마커 컴포넌트 구현 방법 (RoomConnector 재사용 vs 별도 CorridorEntry)
- Corridor 너비 및 높이차 수치
- Phase 8에서 먼저 마커 추가할 4-5개 Room 선택

## Deferred Ideas

- Corridor 장애물/함정 콘텐츠 확장 — 플레이테스트 후
- 나머지 10개 Room 프리팹 마커 추가 — Phase 9에서
- Corridor 종류 추가 (분기형 등) — 프로토타입 완료 후
- 양방향 이동 시 Corridor 카메라 처리 — Phase 9 범위
