# Phase 1: Foundation & Movement - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-27
**Phase:** 01-foundation-movement
**Areas discussed:** 점프 & 이동 무게감, 테스트 씬 레이아웃, 낙사 복귀 연출, 카메라 방식

---

## 점프 & 이동 무게감

| Option | Description | Selected |
|--------|-------------|----------|
| 가볍고 탄력적 | 빠른 속도, 어느 정도 높이 나는 점프, 큰 공중 제어감 — Celeste류 | ✓ |
| 무겁고 착지감 | 낮은 중력, 떨어질 때 가속, 러닝 시 슬라이딩 — Dead Cells/Hollow Knight류 | |

**User's choice:** 가볍고 탄력적

---

| Option | Description | Selected |
|--------|-------------|----------|
| 점프 컷 적용 | 버튼을 떼면 상승 속도 즉시 감소 | ✓ |
| 점프 컷 미적용 | 자연스러운 포물선 | |

**User's choice:** 적용

---

| Option | Description | Selected |
|--------|-------------|----------|
| 완전 자유 공중 제어 | 공중에서도 지상과 같은 속도로 즉시 방향 전환 | ✓ |
| 부드러운 종류감 | 공중 가속에 관성 적용 | |

**User's choice:** 완전 자유

---

| Option | Description | Selected |
|--------|-------------|----------|
| 유니티 플레이스홀더 | Unity 기본 스프라이트 사용 | ✓ |
| 찾아오기 | 무료 실루엣 스프라이트 사용 | |

**User's choice:** 유니티 플레이스홀더

---

## 테스트 씬 레이아웃

| Option | Description | Selected |
|--------|-------------|----------|
| 단순 단일 플랫폼 + 낙사구역 | 넓은 피드백 플랫폼 하나, 양쪽 끝이 픽 다운으로 떨어짐 | ✓ |
| 여러 높이의 다중 플랫폼 | 2~3개 높이 다른 플랫폼 + 낙사구역 | |

**User's choice:** 단순 단일 플랫폼 + 낙사구역

---

| Option | Description | Selected |
|--------|-------------|----------|
| 보이지 않는 플로어 장당 | Trigger Collider2D로 낙사 감지 | ✓ |
| 실룔 낙사구덩(피트) | 보이는 피트 지형 배치 | |

**User's choice:** 보이지 않는 플로어 장당

---

| Option | Description | Selected |
|--------|-------------|----------|
| 그레이 스케일 구돃 스프라이트 | 백색 바탕에 아주 단순한 단색 플랫폼 | ✓ |
| 유니티 Tilemap | 타일맵 시스템으로 플랫폼 구성 | |

**User's choice:** 그레이 스케일 구돃 스프라이트

---

## 낙사 복귀 연출

| Option | Description | Selected |
|--------|-------------|----------|
| 없음 | 즉시 무적 상태로 위치 복귀 | ✓ |
| 짧은 페이드인 애니메이션 | 0.2초 페이드 효과 | |

**User's choice:** 없음

---

| Option | Description | Selected |
|--------|-------------|----------|
| 스프라이트 깜빡임 | 빠르게 원래↔투명도 0 반복 | ✓ |
| 색상 반짝임 (파란색이나 흰쑥임) | 다른 색조로 순간적 반짝 | |

**User's choice:** 스프라이트 깜빡임

---

| Option | Description | Selected |
|--------|-------------|----------|
| 1.5초 | 플레이어가 상황 파악하기에 충분한 시간 (권장) | |
| 1초 | 더 짧고 긴박함 | ✓ |
| 2초 | 여유 있는 시간 | |

**User's choice:** 1초

---

## 카메라 방식

| Option | Description | Selected |
|--------|-------------|----------|
| LateUpdate 직접 구현 | Camera.main이 LateUpdate에서 플레이어 위치 추적 | ✓ |
| Cinemachine 3.x | 가상카메라 + Virtual Camera 설정 | |

**User's choice:** LateUpdate 직접 구현

---

| Option | Description | Selected |
|--------|-------------|----------|
| 없음 — 정밀 추적 | 플레이어 움직임에 바로 중앙 정렬 | ✓ |
| 진행 방향 주식 적용 | 이동 방향으로 카메라가 스무스하게 앞으로 가서 시야 넘김 | |

**User's choice:** 없음 — 정밀 추적

---

## Claude's Discretion

- 이동 속도 수치 (권장 범위: 7~10 units/s)
- 점프 높이 및 중력 배율
- 깜빡임 주기 (권장: 0.1초 간격)

## Deferred Ideas

없음 — 논의가 Phase 1 범위 내에서만 이루어짐.
