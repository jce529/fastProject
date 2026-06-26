---
phase: quick-260626-jpm
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/Camera/CameraFollow.cs
autonomous: true
requirements: []
must_haves:
  truths:
    - "카메라 orthographicSize가 roomOrthoSize(7f)로 고정된다 — SnapToRoom(Bounds) 호출 후에도 리사이즈 없음"
    - "LateUpdate에서 플레이어를 추적하되 CameraBound Bounds 내부로 위치를 클램프한다"
    - "바운드가 카메라 뷰보다 좁은 X축은 bounds.center.x에 스냅, 좁은 Y축은 bounds.center.y에 스냅"
    - "SnapToRoom(Vector3) 폴백은 이전과 동일하게 해당 좌표로 즉시 스냅 후 플레이어 자유 추적"
    - "FloorSpawner.SnapCameraToRoom() 호출 시그니처 변경 없음"
  artifacts:
    - path: "Assets/Scripts/Camera/CameraFollow.cs"
      provides: "바운드 클램프 추적 카메라"
      contains: "_hasBounds, _activeBounds, LateUpdate clamp logic"
  key_links:
    - from: "FloorSpawner.SnapCameraToRoom()"
      to: "CameraFollow.SnapToRoom(Bounds)"
      via: "cb.GetWorldBounds() 전달"
      pattern: "_cameraFollow\\.SnapToRoom"
---

<objective>
CameraFollow 바운드 클램프 추적 방식으로 리팩터.

Purpose:
- SnapToRoom(Bounds)가 현재 orthographicSize를 Bounds 크기에 맞게 늘려 룸마다 카메라 크기가 달라지는 문제 해결
- CameraBound를 "카메라가 찍을 수 있는 영역"이 아닌 "카메라가 이동할 수 있는 최대 범위"로 재정의
- 플레이어 추적 + 경계 클램프 — 좁은 축은 중심 고정(스냅)

Output: CameraFollow.cs — _roomMode 제거, _hasBounds + _activeBounds + 클램프 LateUpdate
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@Assets/Scripts/Camera/CameraFollow.cs
@Assets/Scripts/Camera/CameraBound.cs
@Assets/Scripts/World/FloorSpawner.cs

<interfaces>
<!-- CameraBound (변경 없음) -->
```csharp
public class CameraBound : MonoBehaviour
{
    [SerializeField] private Vector2 _size = new Vector2(20f, 12f);
    public Bounds GetWorldBounds()  // center=transform.position, size=_size
}
```

<!-- FloorSpawner 호출 패턴 (변경 없음) -->
```csharp
// Bounds 경로
_cameraFollow.SnapToRoom(cb.GetWorldBounds());   // CameraBound 있을 때
// 폴백 경로
_cameraFollow.SnapToRoom(fallbackCenter);         // CameraBound 없을 때
```
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: CameraFollow — _roomMode 제거 + 바운드 클램프 추적 구현</name>
  <files>Assets/Scripts/Camera/CameraFollow.cs</files>
  <action>
CameraFollow.cs를 아래 설계대로 전면 교체한다. 호출 시그니처(SnapToRoom(Vector3), SnapToRoom(Bounds))는 유지하되 내부 동작만 변경한다.

**제거:**
- `bool _roomMode` 필드
- `LateUpdate()`의 `if (_roomMode) return;` 조기 반환

**추가:**
- `bool _hasBounds` 필드 (기본 false)
- `Bounds _activeBounds` 필드

**SnapToRoom(Vector3 worldCenter) — 폴백 경로:**
- `_hasBounds = false`
- `transform.position = new Vector3(worldCenter.x, worldCenter.y, offset.z)`
- `_camera.orthographicSize = roomOrthoSize`  (기존과 동일)

**SnapToRoom(Bounds worldBounds) — CameraBound 경로:**
- `_hasBounds = true`
- `_activeBounds = worldBounds`
- `_camera.orthographicSize = roomOrthoSize`  ← 핵심 변경: Bounds 기반 리사이즈 제거
- **position 스냅 없음** — LateUpdate가 첫 프레임부터 올바른 위치를 계산함

**LateUpdate() — 클램프 추적:**
```csharp
private void LateUpdate()
{
    if (target == null) return;
    Vector3 desired = target.position + offset;

    if (_hasBounds && _camera != null)
    {
        float halfH = _camera.orthographicSize;
        float halfW = halfH * _camera.aspect;

        float x = _activeBounds.size.x <= halfW * 2f
            ? _activeBounds.center.x
            : Mathf.Clamp(desired.x, _activeBounds.min.x + halfW, _activeBounds.max.x - halfW);

        float y = _activeBounds.size.y <= halfH * 2f
            ? _activeBounds.center.y
            : Mathf.Clamp(desired.y, _activeBounds.min.y + halfH, _activeBounds.max.y - halfH);

        transform.position = new Vector3(x, y, offset.z);
    }
    else
    {
        transform.position = desired;
    }
}
```

**주석 업데이트:** 클래스 summary에서 "_roomMode=true이면 플레이어 추적을 중단" 내용을 "CameraBound Bounds 내부로 클램프하며 플레이어를 추적" 으로 교체한다.
  </action>
  <verify>
    <automated>
      1. 컴파일 확인: Unity Editor Console에 CS 오류 없음
      2. _roomMode 심볼 잔존 여부: Assets/Scripts/Camera/CameraFollow.cs에 "_roomMode" 문자열이 없어야 함
      3. SnapToRoom(Bounds) 본문에 Mathf.Max(orthoH, orthoW) 또는 orthographicSize 계산식이 없어야 함 (리사이즈 코드 제거 확인)
    </automated>
  </verify>
  <done>
    - _roomMode 필드와 관련 분기가 제거됨
    - SnapToRoom(Bounds): orthographicSize = roomOrthoSize만 설정, 리사이즈 없음
    - LateUpdate: _hasBounds=true이면 플레이어 추적 + Bounds 클램프; false이면 자유 추적
    - 좁은 축(bounds.size &lt;= view size)은 bounds.center로 스냅
    - FloorSpawner 호출 시그니처 변경 없음
  </done>
</task>

</tasks>

<verification>
Unity Editor에서 Play 모드 진입 후:
1. CameraBound가 있는 룸 진입 → 카메라 orthographicSize가 roomOrthoSize(Inspector 값)로 유지됨
2. 플레이어가 룸 끝으로 이동 → 카메라가 Bounds 경계를 넘지 않음
3. CameraBound가 카메라 뷰보다 좁은 룸 → 해당 축에서 카메라 중심 고정
4. CameraBound 없는 룸(폴백) → 카메라가 fallbackCenter로 스냅 후 플레이어 자유 추적
</verification>

<success_criteria>
- CameraFollow.cs 컴파일 오류 없음
- SnapToRoom(Bounds) 내부에서 orthographicSize가 roomOrthoSize 외의 값으로 설정되는 코드가 없음
- LateUpdate에서 _hasBounds=true이면 Bounds 클램프 경로 실행
- FloorSpawner.cs 변경 없음, CameraBound.cs 변경 없음
</success_criteria>

<output>
After completion, update `.planning/STATE.md` quick tasks table with:
| 260626-jpm | CameraFollow 바운드 클램프 추적 리팩터 — _roomMode 제거, SnapToRoom(Bounds) ortho 고정, LateUpdate 클램프 | 2026-06-26 | — | [260626-jpm](./quick/260626-jpm-camerafollow-snaptoroom-bounds-ortho-roo/) |
</output>
