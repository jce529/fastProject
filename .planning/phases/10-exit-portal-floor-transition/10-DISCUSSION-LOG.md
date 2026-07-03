# Phase 10: EXIT 포탈 & 층 전환 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-03
**Phase:** 10-exit-portal-floor-transition
**Areas discussed:** Todo 통합, 포탈 스폰 위치 마커, 층 전환 시퀀스 & ENT 텔레포트, WorldGenerator 리셋 범위, 미사용 포탈 소멸 처리

---

## Todo 통합

| Option | Description | Selected |
|--------|-------------|----------|
| 포함 | ENT 마커 기반 텔레포트 로직을 Phase 10에 통합 | ✓ |
| 제외 | 별도 처리 | |

**User's choice:** 포함
**Notes:** todo 작성 시점(2026-07-03)에 이미 "Phase 10 계획 시점에 통합해서 다룰 것"으로 명시되어 있었음.

---

## 포탈 스폰 위치 마커

| Option | Description | Selected |
|--------|-------------|----------|
| ExitSpawnPoint 컴포넌트, 수동 배치 | EnemySpawner와 동일 패턴, 사용자가 에디터에서 직접 배치 | ✓ |
| 에디터 자동 배치 도구 | RoomMarkerTool처럼 스크립트로 자동 생성 | |
| 기존 EnemySpawn 지점 재사용 | 새 마커 없이 기존 지점 랜덤 선택 | |

**User's choice:** ExitSpawnPoint 컴포넌트, 수동 배치

**Follow-up: 마커 범위**

| Option | Description | Selected |
|--------|-------------|----------|
| 6종 전부, 룸당 1개 | _maxExitsActive=1이므로 최소 구성 | |
| 6종 전부, 룸당 2~3개 | 복수 후보 지점 중 랜덤 선택 | ✓ |
| 사용자가 직접 결정 | 컴포넌트만 제공, 배치는 전적으로 사용자 재량 | |

**User's choice:** 6종 전부, 룸당 2~3개

---

## 층 전환 시퀀스 & ENT 텔레포트

| Option | Description | Selected |
|--------|-------------|----------|
| 동일 6단계 재사용 | 옛 FloorSpawner의 입력잠금→텔레포트→카메라→대기→적활성화→재개 그대로 | ✓ |
| 간소화된 시퀀스 | 적 활성화 단계 생략 | |

**User's choice:** 동일 6단계 재사용
**Notes:** 이미 검증된 패턴(Pitfall 회피)이므로 재사용이 안전하다는 설명에 동의.

**Follow-up: ENT 마커 없는 4개 룸 처리**

| Option | Description | Selected |
|--------|-------------|----------|
| 4개 룸에 RoomEntry 직접 추가 | 사용자가 에디터에서 4개 프리팹에 마커 배치 | ✓ |
| 코드 폴백 유지 | RoomEntry 없으면 기존 공식 폴백 사용 | |

**User's choice:** 4개 룸에 RoomEntry 직접 추가

---

## WorldGenerator 리셋 범위

| Option | Description | Selected |
|--------|-------------|----------|
| 즉시 전부 Destroy | 포탈 진입 순간 기존 체인 전체 정리, 새 체인 시작 | ✓ |
| 기존 GEN-02 lookbehind에 맡김 | 점진적 정리, 순간적 혼재 발생 가능 | |

**User's choice:** 즉시 전부 Destroy

---

## 미사용 포탈 소멸 처리

| Option | Description | Selected |
|--------|-------------|----------|
| 활성 카운트 감소 + 대기룸도 함께 Destroy | 포탈이 사라지면 대기룸도 정리, 카운트 복원 | ✓ |
| 대기룸 유지, 카운트만 감소 | 대기룸 재사용 목적 유지 — 구현 복잡도 증가 | |

**User's choice:** 활성 카운트 감소 + 대기룸도 함께 Destroy

---

## Claude's Discretion

- ExitPortal 트리거 콜라이더 크기/모양
- 포탈 스폰 확률 롤 발생 정확한 시점 (룸 스폰 직후 등)
- FloorSpawner.cs / RoomExit.cs 고아 코드는 삭제하지 않고 그대로 둠 (이번 Phase가 만든 코드가 아니므로)

## Deferred Ideas

없음
