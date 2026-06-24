---
phase: quick
plan: 260624-fog
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/Player/PlayerController.cs
  - Assets/Scripts/Player/ChronoGaugeController.cs
autonomous: true
requirements: []
must_haves:
  truths:
    - "플레이어가 공중에서 두 번 점프할 수 있다"
    - "아래 방향 입력 + 점프 시 플랫폼을 아래로 통과한다"
    - "슬로우모션 중(Time.timeScale < 1)에는 2단점프가 차단된다"
    - "ChronoGauge 디버그 로그가 콘솔에 출력되지 않는다"
  artifacts:
    - path: Assets/Scripts/Player/PlayerController.cs
      provides: "2단점프 + 아래점프 로직"
      contains: "_jumpsRemaining, DropThrough"
    - path: Assets/Scripts/Player/ChronoGaugeController.cs
      provides: "디버그 로그 제거된 게이지 컨트롤러"
---

<objective>
PlayerController에 2단점프와 아래점프(DropThrough)를 추가하고,
ChronoGaugeController의 디버그 로그 블록을 제거한다.

Purpose: 플레이테스트 시 더블점프 메카닉 검증 + 콘솔 노이즈 제거
Output: 수정된 PlayerController.cs, ChronoGaugeController.cs
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/STATE.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: PlayerController — 2단점프 + 아래점프</name>
  <files>Assets/Scripts/Player/PlayerController.cs</files>
  <action>
아래 변경 사항을 정확히 적용한다. 기존 코드 구조와 주석 스타일을 유지할 것.

**1. 필드 추가** (기존 `_inputLocked` 아래에 추가):
```csharp
[SerializeField] private int maxJumps = 2;
[SerializeField] private float dropThroughDelay = 0.2f;
private int _jumpsRemaining;
private bool _isDropping;
```

**2. CheckGround() 수정** — `_isGrounded` 갱신 직후 jumpsRemaining 리셋:
```csharp
private void CheckGround()
{
    Vector2 origin = (Vector2)_transform.position + Vector2.down * 0.05f;
    _isGrounded = Physics2D.OverlapCircle(origin, groundCheckRadius, groundLayer);
    if (_isGrounded) _jumpsRemaining = maxJumps;
}
```

**3. OnJumpPerformed() 전면 교체**:
```csharp
private void OnJumpPerformed(InputAction.CallbackContext ctx)
{
    if (_inputLocked) return;

    // 아래입력 + 착지 상태 → 플랫폼 통과 낙하
    float vertical = _moveAction.ReadValue<Vector2>().y;
    if (_isGrounded && vertical < -0.5f)
    {
        if (!_isDropping) StartCoroutine(DropThrough());
        return;
    }

    // 잔여 점프 없으면 차단
    if (_jumpsRemaining <= 0) return;

    // 슬로우모션 중 2단점프 차단 (공중 점프일 때만)
    if (!_isGrounded && Time.timeScale < 1f) return;

    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
    _jumpHeld = true;
    _jumpsRemaining--;
}
```

**4. DropThrough() 코루틴 추가** (클래스 맨 아래, UnlockInput() 다음에 삽입):
```csharp
private IEnumerator DropThrough()
{
    _isDropping = true;
    int playerLayer = gameObject.layer;
    for (int i = 0; i < 32; i++)
        if ((groundLayer.value & (1 << i)) != 0)
            Physics2D.IgnoreLayerCollision(playerLayer, i, true);
    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -2f);
    yield return new WaitForSecondsRealtime(dropThroughDelay);
    for (int i = 0; i < 32; i++)
        if ((groundLayer.value & (1 << i)) != 0)
            Physics2D.IgnoreLayerCollision(playerLayer, i, false);
    _isDropping = false;
}
```
  </action>
  <verify>Unity Editor에서 컴파일 에러 없이 열리고, Player Inspector에서 Max Jumps(2), Drop Through Delay(0.2) 필드가 표시되면 완료.</verify>
  <done>
  - 지상에서 점프 후 공중에서 한 번 더 점프 가능
  - 두 번 공중점프 후 세 번째 점프 불가
  - 아래키 + 점프 시 플랫폼 통과 후 0.2초 뒤 충돌 복구
  - 슬로우모션(timeScale &lt; 1) 중 공중점프 차단
  </done>
</task>

<task type="auto">
  <name>Task 2: ChronoGaugeController — 디버그 로그 제거</name>
  <files>Assets/Scripts/Player/ChronoGaugeController.cs</files>
  <action>
아래 두 가지를 제거한다. 나머지 코드는 한 줄도 건드리지 않는다.

**제거 1 — 필드** (`private bool _isDraining;` 바로 아래에 있는):
```csharp
private float _debugLogTimer;
```

**제거 2 — Update() 내 debug 블록 전체** (드레인/리젠 로직 다음에 위치):
```csharp
// [DEBUG] 0.5초마다 상태 출력 — 확인 후 제거
_debugLogTimer += Time.unscaledDeltaTime;
if (_debugLogTimer >= 0.5f)
{
    _debugLogTimer = 0f;
    Debug.Log($"[ChronoGauge] isDraining={_isDraining}, Value={Value:F3}");
}
```

제거 후 Update()는 드레인/리젠 두 줄만 남는다.
  </action>
  <verify>Unity Editor 컴파일 에러 없음. 플레이 중 Console에 [ChronoGauge] 로그가 출력되지 않으면 완료.</verify>
  <done>
  - _debugLogTimer 필드 없음
  - Update()에 Debug.Log 없음
  - 게이지 드레인/리젠 동작은 그대로 유지
  </done>
</task>

</tasks>

<verification>
1. Unity Editor에서 두 파일 모두 컴파일 에러 없이 로드
2. SampleScene 플레이 — 점프 두 번 가능, 세 번째 불가
3. 아래키 + 점프 → 플랫폼 통과 확인
4. Console 창에 [ChronoGauge] 로그 미출력 확인
</verification>

<success_criteria>
- PlayerController: 2단점프 작동, 슬로우모션 공중점프 차단, DropThrough 0.2s 후 충돌 복구
- ChronoGaugeController: 디버그 로그 완전 제거, 게이지 기능 정상
</success_criteria>

<output>
완료 후 .planning/quick/260624-fog-playercontroller-2-chronogauge/ 에 SUMMARY.md 작성 불필요.
완료 사실을 STATE.md Quick Tasks 표에 추가할 것.
</output>
