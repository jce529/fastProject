# Phase 12: 포탈 진입/퇴장 애니메이션 구현 및 공격 애니메이션 개선 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-07
**Phase:** 12-animation-polish
**Areas discussed:** 포탈 전환 연출 구현 방식, 공격/구르기 애니메이션 버그 수정, 히트 임팩트 개선, 적 공격 애니메이션 포함 여부

---

## 포탈 전환 연출 구현 방식

| Option | Description | Selected |
|--------|-------------|----------|
| 10-TRANSITION-DESIGN.md 원안 그대로 (코드 기반) | SpriteMask 런타임 생성 + 스케일 애니메이션, WorldGenerator.cs에 전부 구현 | |
| ExitPortal Animator 활용 재설계 | 이미 연결된 ExitPortal.controller에 트리거/상태 추가, WorldGenerator는 트리거만 호출 | |
| (Other) | "1안 처럼 코드를 이용해서 할 건데, WorldGenerator가 아니라 다른 코드로 구현해야지. Animator는 알아봐야할 것 같아" | ✓ |

**사용자 선택:** 코드 기반(SpriteMask) 방식 유지하되 WorldGenerator가 아닌 별도 코드로 구현. Animator 활용 가능성은 추가 검토가 필요하다고 언급했으나 최종적으로는 코드 기반으로 확정(아래 후속 질문 참고).

**후속 질문 — 코드 위치:**

| Option | Description | Selected |
|--------|-------------|----------|
| 신규 컴포넌트 | FloorTransitionEffect.cs 같은 별도 MonoBehaviour, WorldGenerator는 메서드만 호출 | ✓ |
| ExitPortal.cs에 추가 | 포탈 자체가 담당 — 단, ExitPortal은 전환 중 Destroy되는 타이밍이라 위험 | |

**Notes:** WorldGenerator.cs가 이미 492줄이라 비대해지지 않도록 신규 컴포넌트 분리 결정.

---

## PortalEffect 비주얼 소스

| Option | Description | Selected |
|--------|-------------|----------|
| 기존 ExitPortal 스프라이트 재활용 | 새 아트 없이 scale(0,0,0)→(1,1,1) 애니메이션만 적용 | ✓ |
| 새 스프라이트/이펙트 제작 | 별도 비주얼(파티클/그라데이션 등) 신규 준비 | |

**사용자 선택:** 기존 ExitPortal 스프라이트 재활용

---

## 연출 속도(지속시간)

| Option | Description | Selected |
|--------|-------------|----------|
| 원안 수치 그대로 | 입장 0.4s / 포탈 수축 0.3s / 퇴장 성장 0.4s / 퇴장 마스크 0.5s | ✓ |
| 실행자 재량 | 느낌 위주로 조정(빠르거나 짧게) | |

**사용자 선택:** 원안 수치 그대로 사용

---

## 공격/구르기 애니메이션 버그 수정

| Option | Description | Selected |
|--------|-------------|----------|
| 예, 둘 다 수정 | Whiff 트리거+상태, Roll 트리거 모두 컨트롤러에 추가 | ✓ |
| Whiff만 수정 | Roll은 IsRolling bool로 이미 시각적으로 동작하므로 그대로 둠 | |
| 둘 다 이번엔 안 건드림 | 버그는 인지했지만 범위 밖 | |

**사용자 선택:** 둘 다 수정

**후속 질문 — 추가 수정 사항:**

| Option | Description | Selected |
|--------|-------------|----------|
| 특별히 없음 | 버그 수정이 전부 | |
| 직접 설명 | 자유 입력으로 구체적 요구사항 설명 | ✓ |

**사용자 응답(자유 입력):** "공격 모션이 너무 밋밋해서, 히트 순간에 임팩트가 더 확 느껴졌으면 좋겠어. 그리고 좀 더 빠르다는 느낌을 주기위해서 플레이어의 공격모션에 잔상을 넣고싶어"

---

## 히트 임팩트 — 스파크 소스

첫 번째 질문 시도(카메라 쉐이크/스프라이트 플래시/히트프리즈 연장/스파크 파티클 복수선택)는 사용자가 거부하고 직접 명확화를 요청 — 기존 에셋 조사 후 재질문.

| Option | Description | Selected |
|--------|-------------|----------|
| GuardImpact 재활용 | 검 맞부딪침/재링 스파크 3프레임, 검 공격에 가장 자연스러움 | ✓ |
| Hit 재활용 | 범용 히트 플래시 — 원래 피격용이라 의미가 약간 어긋남 | |
| 실행자 재량 | 플래너/실행자가 플레이테스트하며 결정 | |

**사용자 선택:** GuardImpact 재활용

**Notes:** 조사 과정에서 Hit01~03.png(HitDamage.anim), GuardImpact01~03.png(SwordGuardImpact.anim), GunBulletImpactFX.anim(9프레임, 총알 피탄용) 3가지 후보 확인. GunBulletImpactFX는 총기 이펙트라 검 공격과 스타일 불일치로 배제.

## 히트 임팩트 — 추가 요소

| Option | Description | Selected |
|--------|-------------|----------|
| 카메라 쉐이크 | 처치 순간 짧은 카메라 흔들림 | ✓ |
| 적 스프라이트 플래시(흰색 번쩍임) | Material/Shader 토글 | |
| 히트프리즈 시간 연장 | 기존 50~100ms보다 길게 | |
| (Other, 자유 입력) | "적이 파티클이 되어서 사라지거나, 갈라지는 연출을 하고싶어" | ✓ |

**사용자 선택:** 카메라 쉐이크 + 적 사망 파티클/분해 연출(신규 아이디어)

---

## 적 사망 연출 구현 방식

| Option | Description | Selected |
|--------|-------------|----------|
| Unity ParticleSystem 파편화 | 사망 순간 스프라이트 숨기고 파티클 버스트 | (자유 입력으로 대체) |
| 스프라이트 분할 연출 | 스프라이트를 조각내어 흩어지는 연출 — 구현 복잡도 높음 | |

**사용자 응답(자유 입력):** "찾아보니까 Death애니메이션도 있던데 애니메이션 이후에 파티클이 재생되면서 스프라이트를 아래서부터 감추고 파괴하면 될것같아"

**후속 질문 — 감춤 기법:**

| Option | Description | Selected |
|--------|-------------|----------|
| SpriteMask 방식 | 포탈 전환과 동일한 SpriteMask scale 패턴 재사용 | ✓ |
| 셰이더 디졸브 방식 | 신규 셰이더 작성 필요(URP 2D 호환) | |

**사용자 선택:** SpriteMask 방식 — 포탈 전환(D-01)과 동일 코드 패턴 재사용 결정

---

## 공격 모션 잔상(속도감)

| Option | Description | Selected |
|--------|-------------|----------|
| 기존 TrailRenderer 강화 | 두께/길이/색상 조정만, 새 기법 없음 | ✓ |
| 스프라이트 고스트 잔상 | 반투명 스프라이트 복사본 여러 개 — 신규 구현 필요 | |
| 둘 다 | TrailRenderer + 고스트 이중 효과 | |

**사용자 선택:** 기존 TrailRenderer 강화

**Notes:** 히트 임팩트 논의 중 처음 언급됐던 "잔상" 요청이 누락될 뻔했으나, 컨텍스트 작성 전 재확인하여 별도 질문으로 명확화함.

---

## 적 공격 애니메이션 포함 여부

| Option | Description | Selected |
|--------|-------------|----------|
| 플레이어만 | 적 공격 애니메이션은 이미 정상 작동 중 — 이번 phase는 플레이어 측만 | ✓ |
| 적도 함께 개선 | MeleeEnemy/RangedEnemy 공격 모션도 함께 수정 | |

**사용자 선택:** 플레이어만

---

## Claude's Discretion

- FloorTransitionEffect(신규 컴포넌트) 정확한 파일명/클래스명, WorldGenerator와의 인터페이스 시그니처
- Whiff/Roll 애니메이터 상태 전환 세부(Transition Duration = 0 유지)
- GuardImpact 스파크의 정확한 스폰 위치/크기/지속시간
- 카메라 쉐이크의 정확한 진폭/지속시간/감쇠 곡선
- 적 사망 SpriteMask 기법의 파티클 시스템 세부(색상/개수/수명), 마스크 이동 속도
- TrailRenderer 강화의 정확한 수치(두께, Time, 색상 그라데이션)

## Deferred Ideas

- 적(MeleeEnemy/RangedEnemy) 공격 애니메이션/이펙트 개선 — 사용자가 범위를 플레이어로 명확히 좁힘
- ExitPortal.prefab에 이미 연결된 미사용 Animator 활용 — 코드 기반 방식 채택으로 논의 종료
