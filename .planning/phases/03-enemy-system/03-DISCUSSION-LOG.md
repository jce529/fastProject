# Phase 3: Enemy System - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-08
**Phase:** 03-enemy-system
**Areas discussed:** 적 FSM & CombatController 연결, 근접 예고(Telegraph) 설계, 원거리 조준선 표현, 플레이어 즉사 흐름

---

## 적 FSM & CombatController 연결

| Option | Description | Selected |
|--------|-------------|----------|
| IEnemy 인터페이스 | IsAlive, OnDashHit(), ClearHighlight()를 IEnemy로 추출. CombatController는 IEnemy만 참조 | ✓ |
| EnemyBase 기반 클래스 | MonoBehaviour 상속 추상 클래스. 공통 로직 상속. | |
| DummyEnemy 확장 | DummyEnemy를 MeleeEnemy로 리네이밍 + RangedEnemy 별도 추가 (CombatController 수정 최소) | |

**User's choice:** IEnemy 인터페이스

| Option | Description | Selected |
|--------|-------------|----------|
| Idle → Chase → Telegraph → Attack | 4상태. 탐지 시 Chase, 공격 사거리 진입 시 Telegraph(예고), 이후 Attack | ✓ |
| Patrol → Alert → Chase → Telegraph → Attack | 5상태. Alert는 감지 직후 짧은 리액션 시간 | |
| Idle → Chase → Attack | 3상태. Telegraph를 Chase 애니메이션에 통합 | |

**User's choice:** Idle → Chase → Telegraph → Attack

| Option | Description | Selected |
|--------|-------------|----------|
| 순찰 (Patrol) — 좌우 왕복 | Idle에서 일정 범위 좌우 이동. 탐지 시 Chase 전환 | ✓ |
| 제자리 대기 (Stand) | 고정 위치 대기, 감지 시만 반응 | |

**User's choice:** 순찰 (Patrol) — 좌우 왕복

---

## 근접 예고(Telegraph) 설계

| Option | Description | Selected |
|--------|-------------|----------|
| 색상 점멸 + 크기 평한 | 공격 직전 빨간색 점멸. 애니메이션 없이도 명확 | |
| 둘기 애니메이션 (Wind-up) | Wind-up 애니메이션 클립 (ROADMAP에서 언급). Placeholder 스프라이트라 실제 구현 어려움 | |
| 경고 마크 (물음표/아이콘) | 적 머리 위 '!' 아이콘. UI 요소 추가 필요하지만 언제나 명확 | ✓ |

**User's choice:** 경고 마크 (물음표/아이콘)

| Option | Description | Selected |
|--------|-------------|----------|
| 0.8초 | 충분하지만 짧지 않음. unscaledDeltaTime 기준 — 슬로우모션 중에도 0.8초 그대로 유지 | ✓ |
| 1.2초 | 더 느린 페이스. 노슈/평가 상황에서도 피할 수 있는 여유 | |
| Claude 결정 | 0.7~1.2초 범위에서 플레이테스트 후 조정 | |

**User's choice:** 0.8초

---

## 원거리 조준선 표현

| Option | Description | Selected |
|--------|-------------|----------|
| LineRenderer 빨간 실선 + 점진 | 발사 전 알파 0→1 점진. Phase 2 RangeDisplay 패턴 재사용 | ✓ |
| LineRenderer 빨간 실선 (보항 없이) | 단순 실선. 예고 시작 시 바로 표시, 발사 시 사라짐 | |
| 오브젝트 파단한 처럼 점선 패턴 | 대시 실선 패턴으로 더 시각적 눈에 띄임. LineRenderer로 구현 가능하지만 코드 약간 복잡 | |

**User's choice:** LineRenderer 빨간 실선 + 점진

| Option | Description | Selected |
|--------|-------------|----------|
| 제자리 유지 + 조준 | 원거리 적은 이동하지 않고 플레이어를 향해 조준선만 표시 후 발사 | |
| 느리게 접근 (Slow Chase) + 조준 | 원거리 적도 느리게 이동하면서 범위 유지 | |

**User's choice (free text):** "이동 코드는 만들어 놓지만 일단 이동속도를 0으로 만들어서 안움직이게끔 만들어"
**Notes:** moveSpeed = 0f 직렬화 필드, 플레이테스트 후 Inspector에서 조정

| Option | Description | Selected |
|--------|-------------|----------|
| 단순 직선 등속 | Rigidbody2D 일정 속도 직선 발사. Trigger Collider2D로 충돌 감지 | ✓ |
| 이동 방향 점진 + 별 없음 | 투사체 독독 색이 점진되면서 능속. 효과는 있지만 구현 복잡도 올라감 | |

**User's choice:** 단순 직선 등속

---

## 플레이어 즉사 흐름

| Option | Description | Selected |
|--------|-------------|----------|
| IsDead 플래그 + 정지 상태까지 | PlayerController에 IsDead bool 추가. Phase 4에서 사망화면/재시작 로직 연결 | |
| OnPlayerDeath 이벤트/델리게이트 | static event Action으로 사망 이벤트 발행. Phase 4 UIManager가 구독 | ✓ |
| Phase 4로 문 닫기 | Phase 3에서는 피격만 처리, 사망화면 로직은 Phase 4에서만 | |

**User's choice:** OnPlayerDeath 이벤트/델리게이트

| Option | Description | Selected |
|--------|-------------|----------|
| 플레이어 비활성화 + 콘솔 로그 | SetActive(false) + Debug.Log("Player died"). Phase 3 테스트는 수동 재시작 | ✓ |
| 자동 재시작 (3초 후 재시작) | Phase 3에서 간단한 재시작 로직 내장. Phase 4에서 이 로직을 덮어씀 | |

**User's choice:** 플레이어 비활성화 + 콘솔 로그

---

## Claude's Discretion

- 탐지 반경 수치 — 플레이테스트 후 조정
- 근접 적 Chase 이동 속도
- 원거리 적 탐지/공격 사거리
- 투사체 속도
- `!` 아이콘 구현 방법 (WorldSpace Canvas vs 자식 SpriteRenderer)

## Deferred Ideas

- 원거리 적 이동(속도 > 0) — v2 또는 플레이테스트 후
- 복잡한 웨이포인트 기반 순찰 — v2
- 적 사망 이펙트 (파티클) — Phase 4 polish 또는 v2
