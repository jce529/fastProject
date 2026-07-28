# Phase 19: SAMURAI 보스 & 패링 모듈 & 모듈 선택 UI 확장 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-28
**Phase:** 19-samurai-ui
**Areas discussed:** 패링 모듈 전투 방식, 패링 판정 & 반사, SAMURAI 보스 패턴 구조, 모듈 선택 UI 확장 & 테스트 범위

---

## 패링 모듈 전투 방식

| Option | Description | Selected |
|--------|-------------|----------|
| 자동 타겟팅 (Overclock 재사용) | 탭 시 범위 내 가장 가까운 적으로 순간이동+원샷 처치 | |
| 제자리 방향성 스윙(자유 타겟) | 이동 없이 마우스 방향 부채꼴/직선 히트박스로 그 순간 범위 안의 적을 즉시 벤다 | ✓ |
| 돌진형 슬래시(자동 타겟 + 이동) | 대상까지 순간이동하되, 판정 모션이 대시가 아니라 베기로 표현 | |

**User's choice:** 제자리 방향성 스윙(자유 타겟)
**Notes:** 슬로우모션이 없어 자동타겟팅 돌진 방식이 근본적으로 안 맞음 — 실시간 반응 게임처럼 즉석 스윙으로 결정.

| Option | Description | Selected |
|--------|-------------|----------|
| 원샷킬 + 모든 적에게 통용 | 코어 밸류(원샷원킬) 유지, 일반 적/보스 구분 없이 동일 판정 | ✓ |
| 원샷킬 + SAMURAI 룸 한정 | 평상시 층 전투는 여전히 Overclock류로만 검증, 일반 적 상호작용은 범위 밖 | |
| 기존과 다른 판정(다단히트/체력 등) | 자유 서술 | |

**User's choice:** 원샷킬 + 모든 적에게 통용

| Option | Description | Selected |
|--------|-------------|----------|
| 마우스 방향 (Overclock과 동일) | GetMouseWorldDirection() 재사용 — 조준 방향으로 스윙 | ✓ |
| 마지막 이동 방향(좌/우) | 플레이어가 바라보는 대로(FlipX)만 반영 — 마우스 조준 불필요 | |

**User's choice:** 마우스 방향 (Overclock과 동일)

| Option | Description | Selected |
|--------|-------------|----------|
| 짧은 고정 락아웃(히트/헬 무관) | Overclock의 whiffLockout/postKillLockout 개념을 재사용하되 값만 짧게 | ✓ |
| 히트 vs 헬 락아웃 차등 | Overclock과 동일하게 헬(타격 실패) 락아웃이 히트보다 길게 | |

**User's choice:** 짧은 고정 락아웃(히트/헬 무관)

---

## 패링 판정 & 반사

| Option | Description | Selected |
|--------|-------------|----------|
| 같은 Attack 탭, 타이밍만 다름 | 새 버튼 불필요, 타이밍만 맞으면 패링 판정 | |
| 방향 입력도 맞아야 함(투사체 반사 방향) | 타이밍뿐 아니라 마우스 방향이 공격 출처를 향해야 함 | ✓ |

**User's choice:** 방향 입력도 맞아야 함(투사체 반사 방향)
**Notes:** 후속 확인 — 방향 조건은 "조준 방향=반사 방향만 결정, 성공 여부 무관"이 아니라 "공격 출처를 향해야 패링 성공(방향도 판정 조건)"으로 확정.

**후속 확인 — 무입력 처리:**
초기 제시한 두 옵션(안전 vs 치명적) 모두 사용자가 명시적으로 거부하고 "둘 다"로 답변 → 재질의 끝에 확정: 무입력=사망, 오입력=사망, 정확한 타이밍+방향 패링 성공만 생존 — **단, 별도로 RollController의 기존 무적 굴리기로도 회피 가능**(패링이 유일한 생존 수단은 아님).

| Option | Description | Selected |
|--------|-------------|----------|
| 순수 방어 — 데미지 없음 | 패링은 생존 수단일 뿐, 보스 타격은 평시 Vulnerable 창에서 별도로 | ✓ |
| 패링 성공 = 히트 1회 | 반사된 투사체가 보스에게 적중하면 히트로 카운트 | |

**User's choice:** 순수 방어 — 데미지 없음
**Notes:** 이후 SAMURAI 보스 패턴 구조 논의에서 "패링 성공은 그로기 게이지를 채우는 데는 기여한다"로 보완(직접 데미지는 아니지만 처치 진행에 간접 기여).

---

## SAMURAI 보스 패턴 구조

| Option | Description | Selected |
|--------|-------------|----------|
| FioraBoss와 동일(7회 피격, Telegraph→Attack→Vulnerable) | 기존 패턴 그대로 재사용 | |
| 다른 피격 조건 | 자유 서술 | ✓ |

**User's choice:** 다른 피격 조건
**Notes:** 사용자가 할로우나이트 스타일을 명시적으로 언급 — "평시엔 근접공격 위주로 싸우다가 간헐적으로 패링 위주 구간 사용."

| Option | Description | Selected |
|--------|-------------|----------|
| 패링 성공 누적 N회 시 처치 | 패링만이 공격 수단 | |
| 패링 성공 후 짧은 빈틈 창이 열림(거기서 타격) | 패링은 빈틈을 여는 트리거, 실제 타격은 별도 입력 | ✓ |

**User's choice:** 패링 성공 후 짧은 빈틈 창이 열림(거기서 타격)

| Option | Description | Selected |
|--------|-------------|----------|
| FioraBoss와 동일하게 7회 | 기존 컨벤션 유지 | |
| 다른 횟수(더 적게/많게) | 자유 서술 | ✓ (최종 확정: 총 7회 — 단 메커니즘은 그로기 게이지 경유) |

**User's choice:** 다른 횟수 → 이후 재질의로 "그렇게 7번을 채우기"로 최종 확정(FioraBoss와 동일한 최종 숫자, 그로기 게이지 경유 메커니즘은 다름)

| Option | Description | Selected |
|--------|-------------|----------|
| 이동/접근만(치명타 없음) | 평시엔 실제 공격 없음, 긴장감은 패링 타이밍에서만 발생 | |
| MeleeEnemy처럼 일반 거리 공격도 있음(다만 치명적이지는 않음) | 초기 답변 — 이후 사용자가 할로우나이트 언급으로 정정 | ✓ (초기, 이후 수정됨) |

**User's choice(초기):** MeleeEnemy처럼 일반 거리 공격도 있음(치명적이지 않음)
**Notes:** 사용자가 직접 "할로우나이트같이 근접공격을 위주로 플레이어와 싸우고 그러다가 간헐적으로 패링위주를 사용하는거야"로 재설명 — 재질의 결과 아래로 최종 확정.

| Option | Description | Selected |
|--------|-------------|----------|
| 네 — 평시 공격도 치명타(굴러서 피함) | 원샷원킬 코어 그대로, MeleeEnemy/FioraBoss와 동일한 예고→공격 구조 | ✓ |
| 아니오, 이전에 고른 '비치명적' 유지 | 스턴/밀침만 주는 별도 상태 | |

**User's choice:** 네 — 평시 공격도 치명타(굴러서 피함)
**Notes:** 이 답변으로 이전의 "치명적이지 않음" 선택을 대체(D-07 최종 확정).

| Option | Description | Selected |
|--------|-------------|----------|
| 평시도 타격 가능(두 경로 모두 데미지) | FioraBoss식 Vulnerable 창이 평시 콤보 뒤에도 열림 | |
| 데미지는 오직 패링 성공해야만 | 평시 콤보는 순수 생존 구간 | |

**User's choice:** 둘 다 아님 — 자유 서술로 **그로기 게이지** 메커니즘 제안: "평시에 때릴때와 패링을 통해서 그로기 게이지를 채우고, 그로기 시에 한번씩 공격. 그렇게 7번을 채우기"

| Option | Description | Selected |
|--------|-------------|----------|
| 1회씩 = 즉시 그로기(단순) | FioraBoss의 "히트=카운트"와 거의 동일 구조 | |
| 여러 번 누적 필요(Claude 재량 튜닝) | 게이지가 여러 조각으로 나뉘어 2~3번 정도 성공해야 가득 참 | ✓ |

**User's choice:** 여러 번 누적 필요(Claude 재량 튜닝)

---

## 모듈 선택 UI 확장 & 테스트 범위

| Option | Description | Selected |
|--------|-------------|----------|
| 5개 슬롯 전체 미리 준비(3개는 잠금) | F.I.O.R.A/패링/DeadEye/MAX/NOVA 5자리를 지금 배치, 미구현 3개는 자동 잠금 표시 | |
| 지금 구현된 2개만(확장 가능하게) | Overclock+패링만 우선 넣고, 이후 Phase가 각자 슬롯 추가하는 구조 | ✓ |

**User's choice:** 지금 구현된 2개만(확장 가능하게)

| Option | Description | Selected |
|--------|-------------|----------|
| 버튼 비활성화 + 자물쇠 아이콘 | 클릭 자체가 안 되도록 비활성화하고 자물쇠/회색톤으로 잠금 표시 | ✓ |
| 버튼은 활성화되어 있지만 클릭 시 안내 문구 | 클릭은 되지만 "아직 해금되지 않음" 메시지만 뜨고 실제 진입은 막힌다 | |

**User's choice:** 버튼 비활성화 + 자물쇠 아이콘

| Option | Description | Selected |
|--------|-------------|----------|
| 네, DebugScene 확장으로 검증(Phase 18 선례 유지) | DebugSceneBuilder/DebugRoomTeleporter에 SAMURAI 테스트 룸 추가 — WorldGenerator는 건드리지 않음 | ✓ |
| 아니오, 이번엔 실제 WorldGenerator 스폰 풀에도 통합하고 싶다 | v3.1 Phase 16/17이 필요로 하는 보스룸 생명주기까지 이번 Phase에 함께 구현 | |

**User's choice:** 네, DebugScene 확장으로 검증(Phase 18 선례 유지)

---

## Claude's Discretion

- 그로기 게이지의 정확한 누적 임계치/비율(평시 타격 vs 패링 성공 가중치 차등 여부 포함)
- 패링 판정 타이밍 윈도우 폭(SAMURAI-05, 실측 튜닝 필수)
- 패링 전용 타이밍 발생 빈도/평시 콤보와의 교차 주기
- 탭 공격 사이 짧은 고정 락아웃의 정확한 값
- SAMURAI 보스 시각적 정체성(FioraBoss 선례 — 기존 스프라이트 재활용 우선)
- `SamuraiParryModule`/`ParryController` 파일 배치, `TryParry()` 사이드채널 메서드 시그니처
- 모듈 선택 UI 정확한 레이아웃/버튼 배치

## Deferred Ideas

- 실제 `WorldGenerator` 보스 스폰 풀 통합 — v3.1 Phase 16/17 파킹 범위 유지
- 게임 모드/모드 선택 화면 — Phase 24 범위
- DeadEye/MAX/NOVA 모듈 UI 슬롯 실제 콘텐츠 — 각 보스 구현 Phase에서 추가
