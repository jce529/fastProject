---
phase: quick-260605-rhs
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/Player/RangeDisplay.cs
autonomous: true
requirements: [ATCK-02]

must_haves:
  truths:
    - "LineRenderer 빔의 굵기가 lineWidth 값(0.12f)으로 눈에 보이게 렌더링된다"
    - "Linear 모드에서 단일 빔이 플레이어 → 마우스 커서 방향으로만 발사된다"
    - "_rightBeam은 Linear 모드에서 완전히 비활성화된다"
    - "Fan 모드에서 center → arc → center 닫힌 부채꼴이 그려진다"
  artifacts:
    - path: "Assets/Scripts/Player/RangeDisplay.cs"
      provides: "lineWidth 필드, 마우스 방향 단일 빔, 닫힌 부채꼴 Fan"
      contains: "lineWidth"
  key_links:
    - from: "UpdateLinearDisplay()"
      to: "Camera.main.ScreenToWorldPoint"
      via: "마우스 월드 좌표 방향 계산"
    - from: "UpdateFanDisplay()"
      to: "_arcLine.positionCount = arcSegments + 3"
      via: "center→arc→center 닫힘"
---

<objective>
RangeDisplay.cs 3가지 버그 수정:
1. lineWidth 필드 추가 및 모든 LineRenderer에 적용
2. UpdateLinearDisplay()를 마우스 방향 단일 빔으로 교체
3. UpdateFanDisplay()를 center→arc→center 닫힌 부채꼴로 교체

Purpose: 에디터 플레이테스트 시 범위 표시가 정확히 보이고 마우스 조준 방향이 반영되어야 전투 감각 검증이 가능하다.
Output: Assets/Scripts/Player/RangeDisplay.cs (수정)
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
@D:/새 폴더/Fast/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@Assets/Scripts/Player/RangeDisplay.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: RangeDisplay 3가지 수정 적용</name>
  <files>Assets/Scripts/Player/RangeDisplay.cs</files>
  <action>
아래 3가지를 순서대로 수정한다. 다른 코드는 건드리지 않는다.

**수정 1 — lineWidth 필드 추가 (line 15~16 근처, Linear beam settings 블록)**

```csharp
[SerializeField] private float lineWidth = 0.12f;  // beam thickness
```

**수정 2 — Show() 수정 (line 52-53)**

Linear 모드에서 _rightBeam은 비활성화한다. 기존:
```csharp
if (_leftBeam  != null) _leftBeam.enabled  = isLinear;
if (_rightBeam != null) _rightBeam.enabled  = isLinear;
```
교체 후:
```csharp
if (_leftBeam  != null) _leftBeam.enabled  = isLinear;
if (_rightBeam != null) _rightBeam.enabled  = false;   // single-beam mode — right beam unused
```

**수정 3 — UpdateLinearDisplay() 전체 교체 (line 79-99)**

두 방향 빔을 제거하고 마우스 방향 단일 빔으로 교체한다:
```csharp
private void UpdateLinearDisplay()
{
    if (_leftBeam == null) return;

    Vector2 origin = transform.position;

    // Mouse direction in world space
    Vector3 mouseScreen = Input.mousePosition;
    mouseScreen.z = Mathf.Abs(Camera.main.transform.position.z);
    Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
    Vector2 dir = (mouseWorld - origin);
    if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
    dir.Normalize();

    _leftBeam.positionCount = 2;
    _leftBeam.SetPosition(0, origin);
    _leftBeam.SetPosition(1, origin + dir * linearLength);
    _leftBeam.startWidth = _leftBeam.endWidth = lineWidth;
    _leftBeam.startColor = _leftBeam.endColor = ColorDefault;
}
```

**수정 4 — UpdateFanDisplay() 수정 (line 103-121)**

positionCount를 arcSegments+3으로 변경하고 index 0과 arcSegments+2에 origin을 추가한다:
```csharp
private void UpdateFanDisplay()
{
    if (_arcLine == null) return;

    Vector2 facing    = (_playerSprite != null && _playerSprite.flipX) ? Vector2.left : Vector2.right;
    float   baseAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
    Vector2 origin    = transform.position;

    _arcLine.positionCount = arcSegments + 3;
    _arcLine.SetPosition(0, origin);                          // center start
    for (int i = 0; i <= arcSegments; i++)
    {
        float t     = (float)i / arcSegments;
        float angle = (baseAngle - fanHalfAngleDeg + t * fanHalfAngleDeg * 2f) * Mathf.Deg2Rad;
        Vector2 pt  = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * fanRadius;
        _arcLine.SetPosition(i + 1, pt);                      // arc points at 1..arcSegments+1
    }
    _arcLine.SetPosition(arcSegments + 2, origin);            // center end — closes sector
    _arcLine.startWidth = _arcLine.endWidth = lineWidth;
    _arcLine.startColor = _arcLine.endColor = ColorDefault;
}
```

주의: _rightBeam 관련 로직(ResetColors, SetAllRenderers)은 건드리지 않는다 — Inspector 연결 해제 없이도 null-safe하게 동작한다.
  </action>
  <verify>
에디터에서 컴파일 오류 없음 확인:
1. Unity Editor Console에서 컴파일 에러 0개
2. Play 모드 진입 후 공격 버튼(마우스 클릭) 홀드 시:
   - Linear 모드: 마우스 방향으로 굵기 있는 빔 하나만 표시
   - Fan 모드: 플레이어 중심에서 부채꼴이 닫혀서 그려짐
  </verify>
  <done>
- 컴파일 오류 없음
- Linear: 단일 빔이 마우스 방향으로 발사, 굵기 보임
- Fan: center→arc→center 닫힌 부채꼴, 굵기 보임
- _rightBeam은 Linear 모드에서 비활성화
  </done>
</task>

</tasks>

<verification>
Unity Editor Console — 컴파일 에러 0개.
Play 모드: 슬로우모션 진입 시 두 모드 모두 굵기 있는 범위 표시 확인.
</verification>

<success_criteria>
- lineWidth = 0.12f 필드 존재, 모든 LineRenderer startWidth/endWidth에 적용
- Linear 모드: 마우스 방향 단일 빔 (_leftBeam만 활성), _rightBeam.enabled = false
- Fan 모드: positionCount = arcSegments + 3, 위치 0과 arcSegments+2가 origin
- 컴파일 및 런타임 에러 없음
</success_criteria>

<output>
완료 후 `.planning/quick/260605-rhs-rangedisplay-3-1-linewidth-2-linear-3-fa/260605-rhs-SUMMARY.md` 생성
</output>
