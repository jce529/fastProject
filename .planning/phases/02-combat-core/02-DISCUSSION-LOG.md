# Phase 2: Combat Core - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-02
**Phase:** 02-combat-core
**Areas discussed:** 공격 범위 시각화, 돌진 연출 & 카메라, 더미 적 구성

---

## 공격 범위 시각화

| Option | Description | Selected |
|--------|-------------|----------|
| 레이저 빔 (Laser Line) | LineRenderer로 양쪽 방향 직선 범위 표시 | ✓ |
| 착색 사각형 (Flat Box) | GL.Lines 기반 색상 화사각형 | |

**직선형 선택:** 레이저 빔 (LineRenderer)

| Option | Description | Selected |
|--------|-------------|----------|
| 와이어프레임 (Fan Wireframe) | LineRenderer로 부채꼴 윤곽선만 표시 | ✓ |
| 반투명 채운 메시 (Fan Fill) | Mesh 직접 생성, 반투명 리수 표시 | |

**부채꼴형 선택:** 와이어프레임

| Option | Description | Selected |
|--------|-------------|----------|
| 아웃라인 + 색상 변경 | 가장 가까운 적 아웃라인/스프라이트 빨간색 변경 | ✓ |
| 조준선 (Target Line) | 플레이어 → 적 라인 표시 | |
| Claude가 결정 | | |

**적 감지 강조:** 아웃라인/스프라이트 빨간색 변경

| Option | Description | Selected |
|--------|-------------|----------|
| 노란색 범위 + 빨간색 (감지 시) | 상태 전환 직관적 | ✓ |
| 흰색 범위 | 실루엣 배경에서 잘 보임 | |
| Claude가 결정 | | |

**색상:** 노란색 기본 → 적 감지 시 빨간색

---

## 돌진 연출 & 카메라

| Option | Description | Selected |
|--------|-------------|----------|
| Trail Renderer | 돌진 중 잔상 표시, 속도감 명확 | ✓ |
| 즉시 이동 (이펙트 없음) | MovePosition만으로 빠르게 이동 | |
| Claude가 결정 | | |

**돌진 연출:** Trail Renderer 잔상

| Option | Description | Selected |
|--------|-------------|----------|
| 반응 없음 (LateUpdate 유지) | Phase 1 결정과 일관성 | ✓ |
| 미세한 줌인 | 돌진 순간 카메라 약간 확대 | |
| Claude가 결정 | | |

**카메라:** LateUpdate 추적 유지, 별도 반응 없음

---

## 더미 적 구성

| Option | Description | Selected |
|--------|-------------|----------|
| 회색 실루엣 캡슐/사각형 | Phase 1 스타일과 동일 | ✓ |
| 빨간 실루엣 | 범위 감지 테스트 시 즉시 구분 | |
| Claude가 결정 | | |

**더미 시각:** 회색 실루엣 캡슐 placeholder

| Option | Description | Selected |
|--------|-------------|----------|
| 3~5개 고정 배치 | 직선/부채꼴 패턴 테스트에 충분 | ✓ |
| 1개만 | 핵심 루프만 빠르게 테스트 | |
| Claude가 결정 | | |

**더미 수량:** 3~5개 고정 배치

| Option | Description | Selected |
|--------|-------------|----------|
| 일정 시간 후 제자리 부활 (~2초) | 연속 테스트 가능 | ✓ |
| 부활 없음 | 처치 후 사라짐, 재시작 필요 | |
| Claude가 결정 | | |

**더미 부활:** 처치 후 ~2초 뒤 제자리 부활

---

## Claude's Discretion

- 직선형 레이저 빔 길이 초기값
- 부채꼴형 각도/반지름 초기값
- Trail Renderer 길이 및 페이드아웃 시간
- 슬로우모션 timeScale 초기값 (0.15~0.25x 범위 내)
- 게이지 드레인/회복 속도 초기값

## Deferred Ideas

없음 — 논의가 Phase 2 범위 안에서만 진행됨.
