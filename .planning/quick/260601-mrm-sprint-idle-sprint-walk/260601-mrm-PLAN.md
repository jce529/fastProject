---
quick_id: 260601-mrm
type: quick
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/Player/PlayerController.cs
  - Assets/Scripts/Player/PlayerAnimatorController.cs
  - Assets/Player/Resource/Animation/FastPlayerAnimator.controller
autonomous: true
---

<objective>
이동 방식을 즉각 속도 설정에서 가속도 기반(MoveTowards)으로 변경하고, 애니메이터에 Sprint 상태를 추가해 Idle→Walk→Sprint 전환 흐름을 구현한다.

Purpose: 버튼을 누를 때 Walk 애니메이션으로 시작해 최고속 도달 시 Sprint(SwordSprint.anim)로 자연스럽게 전환. 역방향 입력 시 Walk→Idle→Walk→Sprint 흐름도 자연스럽게 동작.
Output: 가속도 기반 이동 + Sprint 상태 포함 애니메이터 컨트롤러
</objective>

<context>
@Assets/Scripts/Player/PlayerController.cs
@Assets/Scripts/Player/PlayerAnimatorController.cs
@Assets/Player/Resource/Animation/FastPlayerAnimator.controller
</context>

<tasks>

<task type="auto">
  <name>Task 1: PlayerController — 즉각 속도 설정을 MoveTowards 가속도 방식으로 교체</name>
  <files>Assets/Scripts/Player/PlayerController.cs</files>
  <action>
ApplyMovement()의 즉각 linearVelocity 설정을 Mathf.MoveTowards 기반으로 교체한다.

추가할 필드 (기존 moveSpeed/jumpForce 선언 아래):
```csharp
[SerializeField] private float acceleration = 60f;
[SerializeField] private float deceleration = 80f;
```

public 프로퍼티 추가 (기존 IsGrounded 프로퍼티 아래):
```csharp
/// <summary>최고 이동 속도. PlayerAnimatorController가 스프린트 비율 계산에 사용.</summary>
public float MoveSpeed => moveSpeed;
```

ApplyMovement() 교체:
```csharp
private void ApplyMovement()
{
    float horizontal = _moveAction.ReadValue<Vector2>().x;

    // Time.timeScale 보정 — Phase 2 슬로우모션에서도 체감 속도 유지 (per D-04).
    float compensatedSpeed = moveSpeed * (1f / Time.timeScale);
    float targetVelocityX = horizontal * compensatedSpeed;

    // 입력이 있으면 acceleration, 없으면 deceleration
    float rate = (Mathf.Abs(horizontal) > 0.01f) ? acceleration : deceleration;

    float newVelocityX = Mathf.MoveTowards(
        _rb.linearVelocity.x,
        targetVelocityX,
        rate * Time.fixedDeltaTime
    );
    _rb.linearVelocity = new Vector2(newVelocityX, _rb.linearVelocity.y);
}
```

주의: 역방향 입력 시 목표 속도가 반전되므로 MoveTowards가 감속→재가속 흐름을 자연스럽게 처리한다. deceleration > acceleration이면 멈춤이 더 빠르다.
  </action>
  <verify>Unity Editor에서 Play Mode 진입 후 이동 입력 시 즉각 최고속이 아니라 가속 후 최고속에 도달하는지 확인. Console 에러 없음.</verify>
  <done>수평 이동이 acceleration 기반으로 부드럽게 증가하고, 입력 해제 시 deceleration으로 감속 후 정지한다.</done>
</task>

<task type="auto">
  <name>Task 2: PlayerAnimatorController — IsSprinting 파라미터 및 속도 비율 기반 전환 로직 추가</name>
  <files>Assets/Scripts/Player/PlayerAnimatorController.cs</files>
  <action>
기존 파라미터 해시 선언 블록에 IsSprinting 추가:
```csharp
private static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
```

Update() 내부 — 기존 `bool moving = ...` 라인 아래에 스프린트 판정 추가:
```csharp
// 현재 속도 / 최고속도 비율이 0.85 이상이면 Sprint 상태로 전환.
// 0.85 임계값: 가속 중 Walk가 너무 짧게 보이지 않도록 여유 제공.
bool isSprinting = moving && (Mathf.Abs(_rb.linearVelocity.x) / _controller.MoveSpeed > 0.85f);
```

Animator.SetBool 블록에 추가:
```csharp
_animator.SetBool(IsSprinting, isSprinting);
```

주의:
- `isSprinting`은 `moving`이 true일 때만 true가 될 수 있음 — Sprint 상태에서 Idle로 직접 튀는 것 방지.
- 역방향 입력 시 속도가 0 근처를 지나므로 isSprinting=false → IsMoving=false → Idle → IsMoving=true → Walk → isSprinting=true → Sprint 흐름이 자동으로 형성됨.
  </action>
  <verify>Play Mode에서 이동 시 Console 에러 없음. Animator 창에서 IsSprinting 파라미터가 보이고, 최고속 근처에서 true로 전환되는지 확인.</verify>
  <done>PlayerAnimatorController가 IsSprinting bool을 매 프레임 Animator에 전달한다.</done>
</task>

<task type="auto">
  <name>Task 3: FastPlayerAnimator.controller — Sprint 상태 및 전환 추가 (YAML 직접 편집)</name>
  <files>Assets/Player/Resource/Animation/FastPlayerAnimator.controller</files>
  <action>
FastPlayerAnimator.controller를 YAML 텍스트로 열어 아래 4곳을 수정한다.

**[1] m_AnimatorParameters 배열에 IsSprinting 파라미터 추가**
기존 IsRolling 파라미터 항목 바로 아래에 다음을 삽입:
```yaml
  - m_Name: IsSprinting
    m_Type: 4
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    controller: {fileID: 9100000, guid: <컨트롤러 자신의 GUID>, type: 3}
```
controller 참조는 파일 내 다른 파라미터 항목의 controller 필드와 동일한 값을 복사한다.

**[2] AnimatorStateMachine m_ChildStates 배열에 Sprint 상태 추가**
기존 Walk 상태 항목(fileID: 675861317272368295) 바로 아래에 삽입:
```yaml
      - serializedVersion: 6
        m_State: {fileID: 3500000000000000001}
        m_Position: {x: 500, y: 250, z: 0}
```

**[3] Sprint AnimatorState 블록 추가** (파일 내 Walk AnimatorState 블록 바로 아래에 삽입)
```yaml
--- !u!1102 &3500000000000000001
AnimatorState:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Sprint
  m_Speed: 1
  m_CycleOffset: 0
  m_Transitions:
  - {fileID: 3500000000000000003}
  - {fileID: 3500000000000000005}
  - {fileID: 3500000000000000004}
  - {fileID: 3500000000000000007}
  - {fileID: 3500000000000000006}
  m_StateMachineBehaviours: []
  m_Position: {x: 50, y: 50, z: 0}
  m_IKOnFeet: 0
  m_WriteDefaultValues: 1
  m_Mirror: 0
  m_SpeedParameterActive: 0
  m_MirrorParameterActive: 0
  m_CycleOffsetParameterActive: 0
  m_TimeParameterActive: 0
  m_Motion: {fileID: 7400000, guid: 479e4bb328e3dde469edb42b5d2ef911, type: 2}
  m_Tag: 
  m_SpeedParameter: 
  m_MirrorParameter: 
  m_CycleOffsetParameter: 
  m_TimeParameter: 
```

**[4] 전환(Transition) 블록 6개 추가** (파일 끝 또는 기존 전환 블록들 아래에 추가)

Walk→Sprint (Walk m_Transitions에 fileID 3500000000000000002 추가 필요):
```yaml
--- !u!1101 &3500000000000000002
AnimatorStateTransition:
  serializedVersion: 3
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  m_Conditions:
  - m_ConditionMode: 1
    m_ConditionEvent: IsSprinting
    m_EventTreshold: 0
  m_DstStateMachine: {fileID: 0}
  m_DstState: {fileID: 3500000000000000001}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0
  m_TransitionOffset: 0
  m_ExitTime: 0.75
  m_HasExitTime: 0
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1
```

Sprint→Walk (IsSprinting=false, ConditionMode 2):
```yaml
--- !u!1101 &3500000000000000003
AnimatorStateTransition:
  serializedVersion: 3
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  m_Conditions:
  - m_ConditionMode: 2
    m_ConditionEvent: IsSprinting
    m_EventTreshold: 0
  m_DstStateMachine: {fileID: 0}
  m_DstState: {fileID: 675861317272368295}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0
  m_TransitionOffset: 0
  m_ExitTime: 0.75
  m_HasExitTime: 0
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1
```

Sprint→JumpRise (!IsGrounded AND VelocityY > 0.1):
```yaml
--- !u!1101 &3500000000000000005
AnimatorStateTransition:
  serializedVersion: 3
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  m_Conditions:
  - m_ConditionMode: 2
    m_ConditionEvent: IsGrounded
    m_EventTreshold: 0
  - m_ConditionMode: 4
    m_ConditionEvent: VelocityY
    m_EventTreshold: 0.1
  m_DstStateMachine: {fileID: 0}
  m_DstState: {fileID: 1499318228524537993}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0
  m_TransitionOffset: 0
  m_ExitTime: 0.75
  m_HasExitTime: 0
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1
```

Sprint→JumpFall (!IsGrounded AND VelocityY < -0.1):
```yaml
--- !u!1101 &3500000000000000004
AnimatorStateTransition:
  serializedVersion: 3
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  m_Conditions:
  - m_ConditionMode: 2
    m_ConditionEvent: IsGrounded
    m_EventTreshold: 0
  - m_ConditionMode: 5
    m_ConditionEvent: VelocityY
    m_EventTreshold: -0.1
  m_DstStateMachine: {fileID: 0}
  m_DstState: {fileID: 8449769233455681851}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0
  m_TransitionOffset: 0
  m_ExitTime: 0.75
  m_HasExitTime: 0
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1
```

Sprint→Attack (IsAttacking=true):
```yaml
--- !u!1101 &3500000000000000007
AnimatorStateTransition:
  serializedVersion: 3
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  m_Conditions:
  - m_ConditionMode: 1
    m_ConditionEvent: IsAttacking
    m_EventTreshold: 0
  m_DstStateMachine: {fileID: 0}
  m_DstState: {fileID: -4835743232808300436}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0
  m_TransitionOffset: 0
  m_ExitTime: 0.75
  m_HasExitTime: 0
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1
```

Sprint→Roll (IsRolling=true):
```yaml
--- !u!1101 &3500000000000000006
AnimatorStateTransition:
  serializedVersion: 3
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  m_Conditions:
  - m_ConditionMode: 1
    m_ConditionEvent: IsRolling
    m_EventTreshold: 0
  m_DstStateMachine: {fileID: 0}
  m_DstState: {fileID: -3003845074570085542}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0
  m_TransitionOffset: 0
  m_ExitTime: 0.75
  m_HasExitTime: 0
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1
```

**Walk 상태 m_Transitions 업데이트:**
Walk AnimatorState 블록(fileID: 675861317272368295)의 m_Transitions 배열에 Walk→Sprint 전환을 마지막에 추가:
기존: `[-4630931641064473479, -1517577054080485071, 7074843466380918229, -7089666721959778489, -7192897646899364683]`
변경: `[-4630931641064473479, -1517577054080485071, 7074843466380918229, -7089666721959778489, -7192897646899364683, 3500000000000000002]`

즉, Walk 상태 m_Transitions 목록 끝에 `- {fileID: 3500000000000000002}` 를 추가한다.

**ConditionMode 값 참고:**
- 1 = If (bool true)
- 2 = IfNot (bool false)
- 4 = Greater (float >)
- 5 = Less (float <)
  </action>
  <verify>Unity Editor에서 FastPlayerAnimator.controller를 열었을 때 Sprint 상태가 그래프에 표시되고, Animator 창 Parameters 탭에 IsSprinting(bool)이 보이는지 확인. Play Mode에서 이동 후 속도가 최고속에 도달하면 Sprint 상태로 전환되는지 Animator 창에서 확인.</verify>
  <done>Animator에 Sprint 상태가 존재하고, Walk→Sprint (IsSprinting=true) / Sprint→Walk (IsSprinting=false) 전환이 작동한다. Sprint→Jump/Roll/Attack 전환도 연결되어 있다.</done>
</task>

</tasks>

<verification>
Play Mode 진입 시 Console 에러 없음.
이동 입력 시: Idle → Walk(가속 중) → Sprint(최고속 85% 이상) 순서로 Animator 상태 전환.
역방향 입력 시: Sprint → Walk → (속도 0 통과) → Idle → Walk → Sprint 자연스러운 흐름.
점프 중 Sprint: Sprint → JumpRise 또는 JumpFall 정상 전환.
</verification>

<success_criteria>
- PlayerController.ApplyMovement()가 MoveTowards를 사용하며 acceleration/deceleration SerializeField 노출
- PlayerAnimatorController가 IsSprinting bool을 매 프레임 Animator에 전달
- FastPlayerAnimator.controller에 Sprint 상태(SwordSprint.anim 참조)와 6개 전환이 존재
- Play Mode에서 Idle→Walk→Sprint 흐름이 시각적으로 확인됨
</success_criteria>

<output>
작업 완료 후 `.planning/quick/260601-mrm-sprint-idle-sprint-walk/260601-mrm-SUMMARY.md` 생성.
</output>
