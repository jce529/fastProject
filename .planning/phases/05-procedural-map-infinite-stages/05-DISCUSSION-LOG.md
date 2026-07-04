# Phase 5: 절차적 맵 생성 — 무한 스테이지 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-17
**Phase:** 05-procedural-map-infinite-stages
**Areas discussed:** 층 전환 트리거, Room 프리팹 구성, 적 스폰 방식, 층 전환 연출 복잡도, 1층 구성, 씬 재시작 호환

---

## 층 전환 트리거

| Option | Description | Selected |
|--------|-------------|----------|
| 출구 트리거 | Room 위쪽에 Trigger Collider2D 배치, 플레이어가 밟으면 전환 | ✓ |
| Y좌표 임계값 | 플레이어 고도 기준 자동 전환 | |

**User's choice:** 출구 트리거
**Notes:** 적을 모두 처치하지 않아도 언제든 전환 가능(언제든 전환 방식 선택).

---

## Room 프리팹 구성

| Option | Description | Selected |
|--------|-------------|----------|
| Unity Editor 수동 배치 | 각 Room 프리팹에 플랫폼/스폰포인트/출구 직접 배치 | ✓ |
| 코드 프로시저럴 생성 | 런타임에 플랫폼/벽/적을 코드로 생성 | |

**Room 수:** 4~5개 (14개 중 선택)
**출구 배치:** 프리팹 안에 직접 자식 오브젝트로 배치

---

## 적 스폰 방식

| Option | Description | Selected |
|--------|-------------|----------|
| 프리팹 내 미리 배치 | Unity Editor에서 적 인스턴스를 직접 선치 | |
| 스폰 포인트 기반 런타임 스폰 | 빈 스폰 포인트에 코드가 층 번호에 따라 동적 생성 | ✓ |

**난이도 스케일링:** 적 수 + 원거리 비율 증가 방식 선택
**Notes:** 층 번호가 올라갈수록 RangedEnemy 비율이 커지는 방식.

---

## 층 전환 연출 복잡도

| Option | Description | Selected |
|--------|-------------|----------|
| 6단계 완전 구현 | 조작 불가→순간이동→카메라 스냅→가림막→적 인식→조작 재개 | ✓ |
| 단순화 | 순간이동 + 적 비활성화만 | |

**카메라 방식:** 순간 Y스냅 (Coroutine 애니메이션 없음 — Phase 1 D-11 일관성 유지)

---

## 1층 구성

| Option | Description | Selected |
|--------|-------------|----------|
| 고정 Room 사용 | 1층은 항상 동일한 단순 Room | ✓ |
| 랜덤 풀에서 선택 | 1층도 랜덤 선택 | |

**Notes:** 2층부터 가중치 랜덤 선택.

---

## 씬 재시작 호환

| Option | Description | Selected |
|--------|-------------|----------|
| SceneManager.LoadScene(0) 유지 | 전체 씬 재로드가 Room들 자동 파괴 | ✓ |
| 별도 Room 리스트 관리 | 스폰된 Room을 추적해 개별 Destroy | |

**User's notes:** "이제 별도의 리스타트 없이 끝나고 새로 시작" — SceneManager.LoadScene(0)이 모든 것을 처리하므로 추가 코드 불필요.

---

## Claude's Discretion

- Room 높이 통일 수치
- 스폰 Y 오프셋 계산 방식
- 가중치 랜덤 알고리즘
- 다음 층 사전 스폰 타이밍
- 층별 적 수 구체적 수치 (D-07 범위 내)

## Deferred Ideas

- 모바일 온스크린 컨트롤 — v2
- Room 14개 완전 채우기 — v2 콘텐츠
- 층 난이도 커브 세밀 조정 — 플레이테스트 후
