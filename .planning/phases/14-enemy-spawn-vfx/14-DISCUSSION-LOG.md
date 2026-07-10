# Phase 14: 적 등장 스폰 연출 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-10
**Phase:** 14-enemy-spawn-vfx
**Areas discussed:** 스폰 연출 트리거 타이밍, 스폰 연출 비주얼 스타일, 다수 적 동시 스폰 처리, 스폰 연출 사운드, 적 스폰 방식(포탈 개수/순서), 룸 재진입 시 중복 재생 방지, Corridor 적용 여부, 보스 재사용성 제약, 플레이어 조작, 플레이어 포탈 애니메이션 수정(범위 분리)

---

## 초기 선택: 논의 영역

| Option | Description | Selected |
|--------|-------------|----------|
| 스폰 연출 트리거 타이밍 | Activate()가 룸 사전 생성 시 즉시 호출되는 아키텍처 이슈 | ✓ |
| 스폰 연출 비주얼 스타일 | 포탈 프리팹 재사용 방식 + 지속시간 | ✓ |
| 다수 적 동시 스폰 처리 | 개별 포탈 vs 순차 vs 스태거 | ✓ |
| 스폰 연출 사운드 | 일반 적 스폰에 사운드 포함 여부 | ✓ |

**User's initial free-text answer (모든 항목 선택 시 함께 제공):**
> "적 스폰 방식: ExitPortal프리팹이 먼저 생성 이후 해당 포탈에서 적들이 차례대로 생성. 코드 상에서는 스폰이 필요한 적들을 배열안에 넣어놓고 하나씩 배출하기. 필요하면 ExitPortal이 아닌 다른 프리팹 생성도 가능"

---

## 스폰 연출 트리거 타이밍

| Option | Description | Selected |
|--------|-------------|----------|
| 플레이어가 해당 룸에 실제로 진입할 때 (추천) | `_playerCurrentIndex` 재사용, Spawn/Activate 분리 필요 | ✓ |
| 카메라 뷰포트에 룸이 들어오는 순간 | 스크롤 기반 감지 추가 필요 | |
| 현재처럼 룸 생성 즉시 (변경 없음) | 화면 밖에서 연출 종료 위험 | |

**User's choice:** 플레이어가 해당 룸에 실제로 진입할 때
**Notes:** 현재 `WorldGenerator.TrySpawnEnemies()`가 2룸 앞선 사전 생성 시점에 즉시 Activate()를 호출한다는 아키텍처 이슈를 먼저 제시하고 확인받음.

---

## 포탈 개수 (다수 적 처리)

| Option | Description | Selected |
|--------|-------------|----------|
| 룸당 하나의 포탈에서 모든 적이 순차 배출 (추천) | 단일 위치, 순차 배출 | |
| 각 EnemySpawner 마커 위치마다 개별 포탈 | 기존 마커 위치 정확히 재사용, 여러 포탈 | ✓ |

**User's choice:** 각 EnemySpawner 마커 위치마다 개별 포탈

---

## 스폰 순서

| Option | Description | Selected |
|--------|-------------|----------|
| 순차적 (약간의 시차를 두고 하나씩) — 추천 | 배열/큐 방식, 시각적 혼란 회피 | ✓ |
| 동시 | 모든 포탈이 한꺼번에 뜸 | |

**User's choice:** 순차적 (약간의 시차를 두고 하나씩)

---

## 비주얼 스타일

| Option | Description | Selected |
|--------|-------------|----------|
| FloorTransitionEffect.PlayExit와 동일한 패턴 재사용 (추천) | PortalEffect 프리팹 성장 + SpriteMask 페이드인, ~1.2초 | ✓ |
| 단순화된 버전 (포탈 페이드인 + 즉시 표시, 마스크 없음) | 구현 단순, "걸어나옴" 느낌 약함 | |

**User's choice:** FloorTransitionEffect.PlayExit와 동일한 패턴 재사용
**Notes:** 이후 "적 이동 여부" 논의에서 이 패턴에 실제 Rigidbody2D 이동을 추가하기로 확장 결정 (아래 참고).

---

## 스폰 사운드

| Option | Description | Selected |
|--------|-------------|----------|
| 포탈 진입/퇴장과 동일한 사운드 재사용 (추천) | AudioManager.PlaySfx(Sfx.PortalExit) 등, 신규 클립 불필요 | ✓ |
| 사운드 없음 (무음) | SFX-05는 보스 전용(Phase 16)이라 이번엔 범위 엄격히 유지 | |

**User's choice:** 포탈 진입/퇴장과 동일한 사운드 재사용

---

## 플레이어 조작 (스폰 연출 중)

| Option | Description | Selected |
|--------|-------------|----------|
| 제약 없음 — 자유롭게 이동 (추천) | 층 전환과 달리 입력 잠금 불필요 | ✓ |
| 스폰 중인 적만 타겟팅 차단, 이동/공격은 허용 | SPWN-02 요구사항과 사실상 동일 | |

**User's choice:** 제약 없음 — 플레이어는 계속 자유롭게 움직임

---

## 룸 재진입 시 중복 재생 방지 → 무한 리스폰 아이디어로 확장

**Claude's clarifying question:** "이미 스폰 연출을 재생한 룸은 재진입해도 연출을 재생하지 않아야 한다는 의미로 맞나요?"

**User's response (free text):**
> "가상 현실에서 무한히 싸우는 AI라는 게 플레이어라는 설정인데, 내가 생각하기에는 그냥 방에 진입하면 계속 생성되는게 맞다고 생각하거든 이미 왔던 방이든 아니든 간에 말이야. 대신에 ExitPortal은 좌우 두개방까지는 고정해주니까 적을 무한 리스폰 시켜도 괜찮지 않을까?"

**Scope creep flagged:** 무한 리스폰은 연출을 넘어선 새 게임플레이 메커니즘 — 별도 논의 필요.

**Follow-up:**

| Option | Description | Selected |
|--------|-------------|----------|
| 이번 Phase는 1회성 스폰 + 연출만, 무한 리스폰은 백로그로 (추천) | 기존 아키텍처(EnemySpawner당 1회 Instantiate) 유지 | ✓ |
| 다른 생각이 있음 | | |

**User's choice:** 네 — 이번 Phase는 1회성 스폰 + 연출만, 무한 리스폰은 백로그로
**Notes:** 무한 리스폰 아이디어는 `<deferred>`로 CONTEXT.md에 보존.

---

## Corridor 적용 여부

| Option | Description | Selected |
|--------|-------------|----------|
| 네 — Room과 동일하게 처리 (추천) | Corridor 3종에도 EnemySpawner 마커 존재, 구분 없이 처리 | ✓ |
| 다름 — Room과 Corridor를 다르게 처리 | | |

**User's choice:** 네 — Room과 동일하게 처리

---

## 보스 재사용성 제약

| Option | Description | Selected |
|--------|-------------|----------|
| 없음 — 크기 독립적(포탈 크기 자동 스케일)로 만들면 됨 (추천) | FloorTransitionEffect의 SpriteRenderer.bounds 기반 계산 방식 재사용 | ✓ |
| 신경쓰고 싶은 점이 있음 | | |

**User's choice:** 없음 — 크기 독립적으로 만들면 된다

---

## 플레이어 ExitPortal 애니메이션 수정 (범위 분리)

**User's initial mention (free text, "추가 영역" 선택 중 등장):**
> "플레이어와 적들의 ExitPortal 애니메이션 수정" — 기존 ExitPortal(층 전환용) 자체의 애니메이션도 함께 손보고 싶음

**Claude's clarifying question:** "구체적으로 어떤 부분이 마음에 안 드시나요?"

**User's detailed response (free text):**
> "일단 플레이어가 움직이지 않아. 분명 플레이어가 직접 들어가고 나오는 연출이 있는데 해당 부분이 반영이 안되어있어. 그리고 레이어 마스크가 동적으로 움직일 수 없는거야? 예를들어 포탈의 중심으로부터 걸어나오는 연출이니까 플레이어가 천천히 걸어서 나오면 좋겠는데 안되더라고.
>
> 아니면 한가지 생각하고 있는 연출이 있는데 처음 들어갈 때, 포탈의 중심을 기준으로 일정범위를 빨아들이는 연출을 하는거야. 그리고 나올때는 플레이어의 대쉬 애니메이션을 활용해서 한번에 포탈에서 나오는거지"

**Scope creep flagged:** 이미 완성된 Phase 12 기능(FloorTransitionEffect)의 재작업 — Phase 14(적 스폰) 범위 밖.

**Follow-up 1 — 범위 분리:**

| Option | Description | Selected |
|--------|-------------|----------|
| 네 — 플레이어 포탈 재작업은 백로그로, Phase 14는 적 스폰만 (추천) | | ✓ |
| 아니오 지금 같이 다뤄야 함 | | |

**User's choice:** 네 — 플레이어 포탈 재작업은 백로그로, Phase 14는 적 스폰만

**Follow-up 2 — 적 스폰 연출에 이 피드백을 반영할지:**

| Option | Description | Selected |
|--------|-------------|----------|
| 실제 걸어나오는 움직임 추가 (추천) | 포탈 중심에서 시작해 마스크 수축과 함께 실제 위치 이동 | ✓ |
| 기존처럼 제자리 고정 (마스크만) | 구현 단순화 | |

**User's choice:** 실제 걸어나오는 움직임 추가
**Notes:** 이 결정이 CONTEXT.md D-07로 반영됨 — 비주얼 스타일(D-06) 위에 실제 이동을 추가하는 형태로 통합.

---

## Claude's Discretion

- 포탈 간 스태거 간격 구체적 수치
- 감지/타겟팅 차단 구현 방식 (IsAlive 게이트 재사용 vs 신규 IsSpawning 플래그) — IEnemy 3-member 계약 불변 조건 하에
- 룸/Corridor 진입 감지 훅의 정확한 구현 위치
- 다중 적 배출 순서 (마커 순회 순서 vs 랜덤)

## Deferred Ideas

1. **적 무한 리스폰 메커니즘** — 룸 재진입 시 적이 계속 새로 생성되는 아이디어. 스토리(끝없이 싸우는 시뮬레이션 AI)와는 잘 맞으나 밸런스/트리거 규칙 설계가 필요해 별도 Phase 후보로 백로그.
2. **플레이어 포탈 연출 재작업** — 진입 시 흡입(suction) 이펙트, 퇴장 시 대쉬 애니메이션 활용. 이미 완성된 Phase 12 기능의 개선 작업이라 별도 Phase 필요.
