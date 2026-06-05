---
phase: quick-260605-mbm
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/UI/AttackTypeSelector.cs
  - Assets/Scripts/World/AttackTypeZone.cs
  - Assets/Scripts/Player/CombatController.cs
  - .planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md
autonomous: true
requirements: [ATCK-01, ATCK-03, MOVE-03]

must_haves:
  truths:
    - "플레이어가 월드 존에 진입하면 AttackTypeSelector UI 하이라이트가 전환된다"
    - "슬로우모션 중 Roll 입력 시 슬로우모션이 즉시 취소되고 대시는 발동하지 않는다"
    - "적 처치 후 이동은 자유롭고, 공격만 0.2초간 쿨다운된다"
  artifacts:
    - path: "Assets/Scripts/UI/AttackTypeSelector.cs"
      provides: "SetType(AttackType) static API + singleton, 마우스 폴링 제거"
    - path: "Assets/Scripts/World/AttackTypeZone.cs"
      provides: "Collider2D 진입 감지 → AttackTypeSelector.SetType 호출"
    - path: "Assets/Scripts/Player/CombatController.cs"
      provides: "Roll 취소 슬로우모션 + 처치 후 공격 쿨다운만"
  key_links:
    - from: "Assets/Scripts/World/AttackTypeZone.cs"
      to: "Assets/Scripts/UI/AttackTypeSelector.cs"
      via: "AttackTypeSelector.SetType(zoneType)"
      pattern: "AttackTypeSelector\\.SetType"
    - from: "Assets/Scripts/Player/CombatController.cs"
      to: "_attackCooldown"
      via: "Update() 쿨다운 카운트다운 + EnterSlowMotion 진입 조건"
      pattern: "_attackCooldown <= 0f"
---

<objective>
AttackTypeSelector UI를 마우스 폴링 방식에서 월드 트리거 존 방식으로 전환하고,
슬로우모션 중 구르기로 슬로우모션 취소 기능과 처치 후 이동 자유(공격 쿨다운만) 기능을 추가한다.

Purpose: 마우스 폴링은 모바일에서 동작하지 않는다. 월드 존은 터치와 무관하게 동작하며
이 게임의 '탑을 올라가는' 레벨 디자인과 자연스럽게 결합된다. Roll 취소와 쿨다운 변경은
슬로우모션의 몰입을 깨지 않고 회피 수단을 주기 위한 메카닉 개선이다.
Output: 리팩터된 AttackTypeSelector.cs, 신규 AttackTypeZone.cs, 수정된 CombatController.cs,
업데이트된 02-04-EDITOR-GUIDE.md
</objective>

<execution_context>
@C:\Users\MSI\Projeect_A.E\fastProject\.claude\get-shit-done\workflows\execute-plan.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md

<interfaces>
<!-- AttackTypeSelector.cs 현재 구현 (리팩터 대상) -->
<!-- 제거 대상: zoneRect SerializeField, Update() 마우스 폴링 로직 전체 -->
<!-- 유지 대상: AttackType enum, Selected property, linearHighlight, fanHighlight, RefreshHighlights() -->

현재 파일에서 제거되는 부분:
```csharp
[SerializeField] private RectTransform zoneRect;
private void Update() { /* 마우스/터치 폴링 전체 */ }
private void Select(AttackType type) { ... }
```

추가되는 API:
```csharp
private static AttackTypeSelector _instance;
public static void SetType(AttackType type);  // 외부 호출용
```

<!-- CombatController.cs 현재 상태 요약 -->
현재 Update() 진입:
  - _isBusy → return
  - _gauge.SetDraining(input.IsAttackDown)
  - AttackHeld && !_isSlowMo → EnterSlowMotion()
  - [타임아웃/게이지 empty/IsAttackDown 없음] → ExitSlowMotion()
  - AttackReleased → StartCoroutine(DashOrWhiff())

ExecuteDash() 내 postKill 처리:
  line 221: yield return new WaitForSecondsRealtime(postKillLockout);

<!-- InputManager.cs 관련 API -->
public bool RollPressed => _rollPressedThisFrame;   // 단일 프레임 true
public bool IsAttackDown => _attackAction.IsPressed();
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: AttackTypeSelector 리팩터 + AttackTypeZone 신규 생성</name>
  <files>Assets/Scripts/UI/AttackTypeSelector.cs, Assets/Scripts/World/AttackTypeZone.cs</files>
  <action>
**AttackTypeSelector.cs 전체 교체 (Write 사용):**

```csharp
using UnityEngine;

public enum AttackType { Linear, Fan }

// Zone-based attack type selector — always visible on screen.
// Type is set externally by AttackTypeZone triggers in the world.
// Not affected by timeScale.
//
// Inspector setup:
//   - Optionally assign linearHighlight / fanHighlight Image for visual feedback
//   - Place AttackTypeZone components on world colliders to drive type changes
public class AttackTypeSelector : MonoBehaviour
{
    public static AttackType Selected { get; private set; } = AttackType.Linear;

    public static bool IsSelecting => false;

    [SerializeField] private UnityEngine.UI.Image linearHighlight;
    [SerializeField] private UnityEngine.UI.Image fanHighlight;

    private static AttackTypeSelector _instance;

    private void Awake()
    {
        _instance = this;
    }

    private void Start() => RefreshHighlights();

    /// <summary>
    /// Called by AttackTypeZone when player enters a zone.
    /// Updates Selected and refreshes UI highlights.
    /// </summary>
    public static void SetType(AttackType type)
    {
        if (_instance == null) return;
        if (Selected == type) return;
        Selected = type;
        _instance.RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        if (linearHighlight != null)
            linearHighlight.color = Selected == AttackType.Linear ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        if (fanHighlight != null)
            fanHighlight.color = Selected == AttackType.Fan ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }
}
```

**AttackTypeZone.cs 신규 생성 (경로: Assets/Scripts/World/AttackTypeZone.cs):**
Assets/Scripts/World/ 폴더가 없으면 생성한다.

```csharp
using UnityEngine;

/// <summary>
/// World-space trigger zone that sets the active attack type when the player enters.
/// Place on a GameObject with a Collider2D (IsTrigger = true).
/// Set zoneType in Inspector to Linear or Fan.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AttackTypeZone : MonoBehaviour
{
    [SerializeField] private AttackType zoneType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        AttackTypeSelector.SetType(zoneType);
    }
}
```

주의사항:
- zoneRect SerializeField 및 Update() 폴링 로직은 완전 제거 (코드에 남기지 않음)
- _instance는 static이므로 씬에 AttackTypeSelector가 여러 개 있어도 마지막 Awake가 덮어쓴다 (프로토타입에서 인스턴스는 1개이므로 허용)
- AttackTypeZone은 Assets/Scripts/World/ 에 배치 (UI 폴더 아님)
  </action>
  <verify>
    <automated>
      Unity Editor에서 컴파일 오류 없이 스크립트 임포트 완료.
      Console 창에 "error CS" 없음.
      AttackTypeSelector.cs에 "zoneRect" 문자열 없음 (grep으로 확인 가능).
    </automated>
  </verify>
  <done>
    AttackTypeSelector.cs: zoneRect/Update() 제거, static _instance + SetType() API 추가.
    AttackTypeZone.cs: 신규 파일 존재, OnTriggerEnter2D → SetType 호출.
    두 스크립트 모두 컴파일 오류 없음.
  </done>
</task>

<task type="auto">
  <name>Task 2: CombatController — Roll 취소 슬로우모션 + 처치 후 공격 쿨다운</name>
  <files>Assets/Scripts/Player/CombatController.cs</files>
  <action>
CombatController.cs를 다음 순서로 수정한다. Write가 아닌 Edit 도구로 정밀 변경.

**변경 A: 필드 추가 (State 섹션, _isSlowMo 아래에 삽입)**

```csharp
private bool  _slowMoCancelledByRoll; // true: 이번 슬로우모션이 Roll로 취소됨
private float _attackCooldown;        // 처치 후 공격 재사용 대기 (unscaledDeltaTime)
```

**변경 B: Update() — _isBusy return 직후, _gauge.SetDraining 호출 전에 삽입**

```csharp
// 공격 쿨다운 카운트다운 (처치 후 공격만 제한, 이동은 자유)
if (_attackCooldown > 0f)
    _attackCooldown -= Time.unscaledDeltaTime;

// Roll 입력이 있으면 슬로우모션 취소 (대시는 발동하지 않음)
if (_isSlowMo && input.RollPressed)
{
    ExitSlowMotion();
    _slowMoCancelledByRoll = true;
    return;
}
```

**변경 C: Update() — _gauge.SetDraining 호출 수정**

기존:
```csharp
_gauge.SetDraining(input.IsAttackDown);
```
변경:
```csharp
_gauge.SetDraining(input.IsAttackDown && _attackCooldown <= 0f);
```

**변경 D: Update() — EnterSlowMotion 진입 조건 수정**

기존:
```csharp
if (input.AttackHeld && !_isSlowMo)
    EnterSlowMotion();
```
변경:
```csharp
if (input.AttackHeld && !_isSlowMo && _attackCooldown <= 0f)
    EnterSlowMotion();
```

**변경 E: Update() — AttackReleased 분기에서 Roll 취소 처리**

기존:
```csharp
if (input.AttackReleased)
{
    if (_isSlowMo)
        ExitSlowMotion();
    StartCoroutine(DashOrWhiff());
}
```
변경:
```csharp
if (input.AttackReleased)
{
    if (_isSlowMo)
        ExitSlowMotion();
    // Roll로 슬로우모션이 취소된 경우 대시/whiff 발동 안 함
    if (_slowMoCancelledByRoll)
    {
        _slowMoCancelledByRoll = false;
        return;
    }
    StartCoroutine(DashOrWhiff());
}
```

**변경 F: ExecuteDash() — postKill yield 교체**

기존 (line ~221):
```csharp
// 8. Post-kill lockout in real time (player cannot act during this window)
yield return new WaitForSecondsRealtime(postKillLockout);
```
변경:
```csharp
// 8. Post-kill cooldown: 공격만 제한, 이동은 자유 (WaitForSecondsRealtime 제거)
_attackCooldown = postKillLockout;
```

주의사항:
- `_slowMoCancelledByRoll` 리셋은 AttackReleased 분기에서만 처리 (Update 상단에서 리셋하면 동일 프레임 내 AttackReleased 분기가 이미 return 했을 수 있음)
- `_attackCooldown`은 unscaledDeltaTime으로 감소 — 슬로우모션 도중에도 실제 0.2초
- `postKillLockout` SerializeField 필드 자체는 그대로 유지 (Inspector 조정 가능)
- ExecuteDash의 `yield return` 1줄만 제거하고 `_attackCooldown = postKillLockout` 1줄로 교체 — 코루틴 구조는 변경하지 않음
  </action>
  <verify>
    <automated>
      Unity Editor 컴파일 오류 없음.
      수동 플레이테스트:
      1. 슬로우모션 중 Shift → 슬로우모션 즉시 종료, 대시 발동 안 함
      2. 적 처치 → 이동 즉시 가능, 0.2초 내 공격 버튼 눌러도 슬로우모션 미발동
    </automated>
  </verify>
  <done>
    _slowMoCancelledByRoll 플래그: Roll 입력 시 슬로우모션 취소 + 대시 차단.
    _attackCooldown: 처치 후 0.2초 공격 쿨다운 (이동 자유).
    WaitForSecondsRealtime postKill yield 제거 완료.
    컴파일 오류 없음.
  </done>
</task>

<task type="auto">
  <name>Task 3: 02-04-EDITOR-GUIDE.md 업데이트</name>
  <files>.planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md</files>
  <action>
Edit 도구로 네 섹션을 수정한다. 나머지 섹션은 건드리지 않는다.

**변경 1: ATCK-01 섹션 전체 교체**

기존 ATCK-01 섹션 (AttackTypeSelector 동작 원리 + 마우스 기반 설명)을 다음으로 교체:

```markdown
## ATCK-01: 공격 타입 선택 존

선택 존(AttackTypeSelector 패널)은 항상 화면에 고정 표시된다.

**AttackTypeSelector 동작 원리:**
플레이어가 월드에 배치된 AttackTypeZone 트리거에 진입하면 공격 타입이 전환된다.
- **Linear 존** 진입 → Linear 하이라이트 (전방 좌우 직선)
- **Fan 존** 진입 → Fan 하이라이트 (전방 110° 부채꼴)
- 어떤 존에도 없으면 마지막 선택 타입 유지

**씬 배치:** 각 존은 Collider2D(IsTrigger=true) + `AttackTypeZone` 컴포넌트가 있는 GameObject.
Inspector에서 `Zone Type` 필드를 Linear 또는 Fan으로 설정.

**실행:** 플레이어를 Linear 존과 Fan 존 사이를 넘나들며 이동한다.

**확인:**
- [ ] 선택 존이 게임 시작과 동시에 항상 화면에 표시된다 (오버레이 팝업 방식 아님)
- [ ] Linear 존에 진입하면 Linear가 하이라이트된다
- [ ] Fan 존에 진입하면 Fan이 하이라이트된다
- [ ] 타입 전환 시 존이 사라지거나 게임이 멈추지 않는다
```

**변경 2: MOVE-03 슬로우모션 중 구르기 실행 D 내용 수정**

기존 "실행 D — 슬로우 모션 중 구르기" 항목 내 확인 목록을 다음으로 교체:

```markdown
**실행 D — 슬로우 모션 중 구르기:**
1. 슬로우 모션 발동 중 Shift 입력
2. **확인:** 구르기 속도가 게임 속도에 영향받지 않고 정상 동작한다 (timeScale 보상 적용)
3. **확인:** 쿨다운은 실제 시간 기준 1.0초 (슬로우 모션 중에도 동일 — `unscaledDeltaTime` 사용)
4. **확인:** Shift 입력 시 슬로우 모션이 즉시 취소된다 (Roll이 슬로우모션 탈출 수단으로 동작)
5. **확인:** Roll로 슬로우모션 취소 후 공격 버튼을 떼어도 대시가 발동하지 않는다
```

**변경 3: ATCK-03 처치 후 경직 설명 수정**

기존 "전체 통과 기준" 표와 ATCK-03 확인 항목에서 "이동 불가 경직" 표현을 수정.

ATCK-03 확인 목록 마지막에 다음 항목 추가 (기존 항목 뒤에):
```
- [ ] 처치 후 이동(WASD)은 즉시 가능하다 — 이동 경직 없음
- [ ] 처치 후 0.2초(`postKillLockout = 0.2`) 내 공격 버튼을 눌러도 슬로우모션이 발동하지 않는다 (공격만 쿨다운)
```

**변경 4: Inspector 세팅 테이블 — AttackTypeZone 배치 안내 추가**

Inspector 세팅 확인 포인트 섹션 맨 아래 (RangeDisplay 관련 단락 아래)에 다음 추가:

```markdown
**AttackTypeZone 배치 안내:**
씬에 빈 GameObject를 생성하고 다음 컴포넌트를 추가한다:
- `BoxCollider2D` (또는 다른 2D 콜라이더) — Inspector에서 **Is Trigger = true** 체크
- `AttackTypeZone` 스크립트 — Inspector에서 `Zone Type` 선택 (Linear 또는 Fan)

최소 두 개의 존(Linear 하나, Fan 하나)을 플레이어 이동 경로 양쪽에 배치하여 타입 전환 동작을 확인한다.
```

**변경 5: 전체 통과 기준 표 갱신**

기존 표에서 ATCK-01 행 비고를 업데이트:
```
| ATCK-01 존 표시 및 타입 전환 (존 진입 기반) | ⬜ | AttackTypeZone 트리거 필요 |
```

MOVE-03 행에 Roll 슬로우모션 취소 추가:
```
| MOVE-03 구르기 방향 고정 + 1.0s 쿨다운 + 슬로우모션 취소 | ⬜ | |
```

ATCK-03 행 비고 업데이트:
```
| ATCK-03 대시 처치 + 처치 후 이동 자유(공격 쿨다운만 0.2s) | ⬜ | |
```
  </action>
  <verify>
    <automated>
      파일 존재 확인: .planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md
      파일 내 "마우스를 존" 문자열 없음 (마우스 폴링 설명 제거 확인).
      파일 내 "월드에 배치된 AttackTypeZone" 문자열 존재.
      파일 내 "Roll이 슬로우모션 탈출 수단" 문자열 존재.
      파일 내 "이동 경직 없음" 문자열 존재.
    </automated>
  </verify>
  <done>
    ATCK-01: 마우스 위치 기반 설명 → 월드 존 진입 기반으로 교체.
    MOVE-03: Roll 슬로우모션 취소 확인 항목 추가.
    ATCK-03: 처치 후 이동 자유 확인 항목 추가.
    Inspector 테이블: AttackTypeZone 배치 안내 추가.
    전체 통과 기준 표 갱신.
  </done>
</task>

</tasks>

<verification>
1. Unity Editor 컴파일 오류 없음 — Console 창 error CS 없음
2. AttackTypeSelector.cs: "zoneRect" 문자열 없음, "SetType" 메서드 존재
3. AttackTypeZone.cs: 신규 파일 존재, OnTriggerEnter2D 구현
4. CombatController.cs: "_slowMoCancelledByRoll" 필드 존재, "_attackCooldown" 필드 존재,
   "WaitForSecondsRealtime(postKillLockout)" 없음
5. 02-04-EDITOR-GUIDE.md: "AttackTypeZone" 언급 존재, "월드에 배치" 언급 존재
</verification>

<success_criteria>
- AttackTypeSelector.cs: 마우스/터치 폴링 완전 제거, static SetType API 동작
- AttackTypeZone.cs: Player 태그 진입 시 AttackTypeSelector.SetType 호출
- CombatController.cs: 슬로우모션 중 Roll → 슬로우모션 취소 + 대시 차단
- CombatController.cs: 처치 후 이동 즉시 가능, 공격 0.2초 쿨다운
- 02-04-EDITOR-GUIDE.md: 4개 섹션 실제 구현 반영
- 컴파일 오류 없음
</success_criteria>

<output>
After completion, create `.planning/quick/260605-mbm-attacktypeselector/260605-mbm-SUMMARY.md`
</output>
