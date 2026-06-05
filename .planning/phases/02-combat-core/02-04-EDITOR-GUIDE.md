# Phase 02-04 에디터 플레이테스트 가이드

Phase 2 요구사항을 Unity Editor Play 모드에서 직접 확인하는 수동 체크리스트.
**최신 구현 기준**: zone-based 공격 타입 선택, 버튼 누름 즉시 슬로우 모션, whiff = 검 휘두르기 + 이동 불가.

---

## 준비

1. Unity Editor에서 `Assets/Scenes/SampleScene` 열기
2. **Play** 버튼 클릭 (상단 중앙)
3. Game 뷰 포커스 확인

---

## Inspector 세팅 확인 포인트

Play 전에 Hierarchy에서 Player GameObject를 선택하고 Inspector에서 각 컴포넌트의 필드 값을 확인한다.

| 컴포넌트 | 필드 | 기대값 |
|----------|------|--------|
| CombatController | Slow Time Scale | 0.2 |
| CombatController | Max Slow Mo Duration | 5 |
| CombatController | Hit Freeze Duration | 0.075 |
| CombatController | Whiff Lockout | 0.5 |
| CombatController | Post Kill Lockout | 0.2 |
| CombatController | Search Radius | 10 |
| CombatController | Fan Half Angle Deg | 55 |
| GaugeController | Drain Per Second | 0.25 |
| GaugeController | Regen Per Second | 0.15 |
| GaugeController | Kill Bonus | 0.2 |
| RollController | Roll Cooldown | 1.0 |
| RollController | Roll Duration | 0.3 |
| RollController | I Frame Duration | 0.4 |
| RangeDisplay | Linear Length | 10 |
| RangeDisplay | Fan Radius | 7 |
| RangeDisplay | Fan Half Angle Deg | 55 |

RangeDisplay 컴포넌트에서 `_leftBeam`, `_rightBeam`, `_arcLine` 세 필드에 LineRenderer가 Inspector에서 할당되어 있는지 확인한다. 미할당 시 범위 표시가 나타나지 않는다.

---

## ATCK-01: 공격 타입 선택 존

선택 존(AttackTypeSelector 패널)은 항상 화면에 고정 표시된다. 공격 버튼을 누르지 않아도 보여야 한다.

**AttackTypeSelector 동작 원리:**
커서(PC) 또는 첫 번째 터치(모바일)가 존 내부에 있을 때, 커서 위치에 따라 타입이 실시간 전환된다.
- 존의 **왼쪽 절반**에 커서를 올리면 → **Linear** 하이라이트 (`local.x < 0`)
- 존의 **오른쪽 절반**에 커서를 올리면 → **Fan** 하이라이트 (`local.x >= 0`)
- 존 외부에서는 마지막으로 선택된 타입이 유지된다

**실행:** Game 뷰에서 AttackTypeSelector 패널 위로 마우스를 이동한다.

**확인:**
- [ ] 선택 존이 게임 시작과 동시에 항상 화면에 표시된다 (오버레이 팝업 방식 아님)
- [ ] 마우스를 존 **왼쪽 절반**으로 이동하면 Linear가 하이라이트된다
- [ ] 마우스를 존 **오른쪽 절반**으로 이동하면 Fan이 하이라이트된다
- [ ] 타입 전환 시 존이 사라지거나 게임이 멈추지 않는다

---

## ATCK-02: 슬로우 모션 발동 및 자동 해제

**공격 버튼**: InputSystem_Actions의 Attack 액션에 매핑된 버튼(에디터 기본: 마우스 왼쪽 버튼 또는 설정된 키).
버튼을 **누르는 순간** 슬로우 모션이 발동한다 (AttackHeld 즉시 반응, `slowTimeScale = 0.2`).

**실행 A — 즉시 발동:** 공격 버튼을 **누르는 순간** 확인.

**확인 A:**
- [ ] 타입 선택 완료를 기다리지 않고 버튼을 **누르는 즉시** 게임 속도가 느려진다 (정상 속도의 20%)
- [ ] 플레이어 주변에 범위 표시(RangeDisplay)가 나타난다
  - Linear 선택 시: 좌우 방향 빔 2줄 (노란색), 각 방향 10 units
  - Fan 선택 시: 플레이어 전면 110°(반각 55°) 부채꼴 와이어프레임 (노란색), 반지름 7 units
- [ ] 범위 안 가장 가까운 적의 스프라이트가 빨간색으로 강조된다
- [ ] 슬로우 모션 중에도 구르기(Shift)는 정상 속도로 반응한다

**실행 B — 자동 타임아웃:** 공격 버튼을 5초 이상 계속 누르고 있는다.

**확인 B:**
- [ ] 5초(`maxSlowMoDuration = 5`)가 지나면 슬로우 모션이 자동으로 해제되고 시간이 정상 속도로 복귀한다

---

## ATCK-03: 범위 내 적에게 대시 처치

**준비:** DummyEnemy를 플레이어 범위 안에 배치
- Linear 모드: 플레이어 좌우 10 units 이내
- Fan 모드: 플레이어 전면 7 units + 110° 이내

**실행:** 슬로우 모션 상태에서 공격 버튼 **뗌**

**확인:**
- [ ] 플레이어가 적 위치로 빠르게 이동한다 (약 3 FixedUpdate 프레임)
- [ ] 이동 중 잔상(TrailRenderer)이 표시된다
- [ ] 적이 즉시 제거된다 (DummyEnemy 사라짐)
- [ ] 대시 후 시간이 정상 속도로 복귀한다
- [ ] 벽/플랫폼이 경로를 막고 있으면 대시하지 않고 whiff로 전환된다 (Default 레이어 linecast)

---

## ATCK-04: 범위 밖 Whiff (검 휘두르기 + 이동 불가)

**준비:** DummyEnemy를 범위 밖에 배치하거나 씬에서 제거

**실행:** 슬로우 모션 상태에서 공격 버튼 **뗌**

**확인:**
- [ ] 플레이어가 적에게 대시하지 않는다
- [ ] Whiff 애니메이션(검 휘두르기)이 재생된다
- [ ] **원래 위치로 복귀하지 않는다** — 현재 위치에서 이동 불가 경직만 발생한다
- [ ] 약 0.5초(`whiffLockout = 0.5`) 경직 후 정상 조작이 가능해진다
- [ ] Whiff 경직(~0.5s)이 처치 후 경직(~0.2s)보다 명확히 길게 느껴진다

---

## ATCK-05: 게이지 드레인 및 킬 보너스

**실행 A — 드레인:** 공격 버튼을 2~4초 길게 누름

**확인 A:**
- [ ] 버튼을 누르는 동안 게이지 UI가 감소한다 (`drainPerSecond = 0.25` → ~4초에 완전 소진)
- [ ] 게이지가 0이 되면 슬로우 모션이 자동으로 해제된다

**실행 B — 회복:** 버튼을 누르지 않고 대기

**확인 B:**
- [ ] 게이지가 서서히 회복된다 (`regenPerSecond = 0.15` → ~6.7초에 완충)

**실행 C — 킬 보너스:** 슬로우 모션 상태에서 적을 처치한다

**확인 C:**
- [ ] 처치 직후 게이지가 즉시 일부 회복된다 (`killBonus = 0.2` → +20%)

---

## FEEL-01: Hit-freeze

**실행:** 적을 대시로 처치

**확인:**
- [ ] 처치 순간 화면이 약 1프레임 멈추는 느낌이 든다 (`hitFreezeDuration = 0.075` → ~75ms)
- [ ] 멈춤 후 정상적으로 게임이 재개된다
- [ ] 멈춤이 너무 길거나 짧지 않고 타격감이 있다

---

## MOVE-03: 구르기 (i-frame + 쿨다운 + 방향 고정)

**실행 A — i-frame 확인:**
1. 적이 공격하는 타이밍에 구르기(Shift) 입력
2. **확인:** 구르기 중 피격되지 않는다 (`iFrameDuration = 0.4s`, 이동 지속 시간 `rollDuration = 0.3s`보다 길다)

**실행 B — 방향 고정:**
1. 구르기를 시작하는 순간 반대 방향 키(A/D) 입력
2. **확인:** 구르기 중 방향이 바뀌지 않는다 (시작 시점 방향으로 끝까지 이동)

**실행 C — 쿨다운 확인:**
1. 구르기 직후 즉시 다시 Shift 입력
2. **확인:** 두 번째 구르기가 발동하지 않는다 (`rollCooldown = 1.0s` **1.0초 쿨다운**)

**실행 D — 슬로우 모션 중 구르기:**
1. 슬로우 모션 발동 중 Shift 입력
2. **확인:** 구르기 속도가 게임 속도에 영향받지 않고 정상 동작한다 (timeScale 보상 적용)
3. **확인:** 쿨다운은 실제 시간 기준 1.0초 (슬로우 모션 중에도 동일 — `unscaledDeltaTime` 사용)

---

## 전체 통과 기준

| 요구사항 | 통과 | 비고 |
|----------|------|------|
| ATCK-01 존 표시 및 타입 전환 (좌=Linear, 우=Fan) | ⬜ | |
| ATCK-02 슬로우 모션 즉시 발동 (20% 속도) | ⬜ | |
| ATCK-02 슬로우 모션 자동 타임아웃 (5s) | ⬜ | |
| ATCK-03 대시 처치 | ⬜ | |
| ATCK-04 whiff + 이동 불가 (원위치 복귀 없음) | ⬜ | |
| ATCK-05 게이지 드레인(~4s)/회복(~6.7s)/킬보너스(+20%) | ⬜ | |
| FEEL-01 hitFreeze (~75ms) | ⬜ | |
| MOVE-03 구르기 방향 고정 + 1.0s 쿨다운 | ⬜ | |

모든 항목 통과 → Phase 2 검증 완료.
