# Phase 11: 타이머 & 난이도 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-07
**Phase:** 11-timer-difficulty
**Areas discussed:** SCORE 범위 편입, 제한시간 & 점수 공식, 난이도 스케일링 커브, 적 스폰 활성화 타이밍, 시간초과 연출

---

## SCORE 범위 편입 (사전 스코프 질문)

| Option | Description | Selected |
|--------|-------------|----------|
| Phase 11에 포함 | 타이머 구현과 자연스럽게 묶임 — 남은 시간 값이 이미 존재하므로 점수 계산 추가 비용이 적음 | ✓ |
| 별도 Phase로 분리 | 로드맵 원안 그대로, SCORE는 다음 Phase로 | |

**사용자의 선택:** Phase 11에 포함
**Notes:** REQUIREMENTS.md에 SCORE-01/02가 어느 Phase에도 매핑되지 않은 상태였음(Unmapped: 2). Phase 11에서 타이머의 "남은 시간" 값이 SCORE-01 계산에 바로 필요하므로 함께 처리하기로 확정.

---

## 제한시간 & 점수 공식

| Option | Description | Selected |
|--------|-------------|----------|
| 60초 고정 | 모든 층 동일 — 단순, 테스트/밸런싱 쉬움 | ✓ |
| 90초 고정 | 여유 있게 | |
| 층수에 따라 감소 | 예: 60초에서 층당 -2초, 최소값 있음 | |

**사용자의 선택:** 60초 고정

| Option | Description | Selected |
|--------|-------------|----------|
| 남은 초 × 10점 | ScoreManager의 킬 점수(100점)보다 낮은 스케일 | ✓ |
| 남은 초 × 100점 | 킬 점수와 동일 스케일 | |
| 남은 비율(%) 기반 | 기존 FastClearBonus(300) 등 클리어 보너스 패턴과 통일 | |

**사용자의 선택:** 남은 초 × 10점

---

## 난이도 스케일링 커브

| Option | Description | Selected |
|--------|-------------|----------|
| 기존 계단식 테이블 재사용 | FloorSpawner.GetEnemyCount() 그대로: 1~5층/6~10층/11층+ | ✓ |
| 층수 선형 증가 | 예: 기본 2마리 + 층당 0.3마리씩 증가 | |
| 룸당 EnemySpawner 마커 비율 | 층수에 비례해 마커 중 몇 %를 활성화할지 결정 | |

**사용자의 선택:** 기존 계단식 테이블 재사용

---

## 적 스폰 활성화 타이밍

| Option | Description | Selected |
|--------|-------------|----------|
| 룸 생성 즉시 활성화 | WorldGenerator가 Room Instantiate 직후 Spawn()+Activate() 호출 | ✓ |
| 플레이어 진입 시점에 활성화 | Phase 5 ActivateEnemies() 패턴과 동일 — 룸 입장 전까지 비활성 | |

**사용자의 선택:** 룸 생성 즉시 활성화

---

## 시간초과 연출

| Option | Description | Selected |
|--------|-------------|----------|
| 없음 — 숫자만 표시 | 구현 비용 최소화 | |
| 임박 시 색상 변경(빨간색) | 간단한 시각 피드백 | (부분 선택 — Other로 확장) |

**사용자의 선택(Other, 자유 응답):** "남은 시간이 적을 수록 플레이어가 점점 빠르게 점멸했으면 좋겠어 빨간색으로"
**Notes:** 단순 색상 변경을 넘어, 점멸 속도가 남은 시간에 반비례해 빨라지는 동적 효과로 확장. InvincibilityHandler의 플리커 패턴을 참고하되 간격을 가변으로 구현.

| Option | Description | Selected |
|--------|-------------|----------|
| PlayerController.OnPlayerDeath 직접 호출 | 기존 사망 이벤트 재사용 — PlayerDeathHandler/DeathScreenController가 그대로 반응 | ✓ |
| 다른 연출(별도 사망 화면 문구 등) | 구현 범위 확대 | |

**사용자의 선택:** PlayerController.OnPlayerDeath 직접 호출

---

## Claude's Discretion

- 타이머 점멸 간격의 정확한 곡선(Lerp 함수, 최소/최대 간격 값)
- WorldGenerator에 `_meleeEnemyPrefab`/`_rangedEnemyPrefab` Inspector 필드 추가 방식
- HUD 타이머 표시 형식(초 단위 숫자 vs MM:SS)

## Deferred Ideas

없음.

## Process Note

세션 중 AskUserQuestion의 multiSelect 옵션이 의도대로 다중 선택되지 않는 문제가 발생 — 사용자가 재확인 요청 후, 이후 질문은 텍스트 번호 선택 방식으로 전환해 4개 영역(제한시간&점수공식/난이도/적활성화/시간초과연출)을 모두 정상적으로 논의함.
