---
phase: quick
plan: 260629-jmp
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/World/TestWorldGenerator.cs
autonomous: true
requirements: [ARCH-03]

must_haves:
  truths:
    - "Start()를 실행하면 Scene에 Room→Corridor→Room 체인이 수평으로 배치된다"
    - "각 오브젝트의 연결부(Right connector와 다음 오브젝트의 Left connector)가 공간적으로 일치한다"
    - "Inspector에서 배열만 연결하면 다른 코드 수정 없이 동작한다"
  artifacts:
    - path: "Assets/Scripts/World/TestWorldGenerator.cs"
      provides: "Room→Corridor→Room 수평 체인 자동 배치 MonoBehaviour"
      exports: [TestWorldGenerator]
  key_links:
    - from: "TestWorldGenerator.Start()"
      to: "RoomConnector.Direction.Right"
      via: "GetComponentsInChildren<RoomConnector>()"
      pattern: "FindConnector.*Direction\\.Right"
    - from: "AlignByEntry()"
      to: "go.transform.position"
      via: "targetPos - entryConnector.transform.position"
      pattern: "transform\\.position.*targetPos"
---

<objective>
08-03 플레이테스트용 최소 WorldGenerator — Inspector에서 Room/Corridor 프리팹 배열을 지정하면
Start()가 Room[0] → Corridor[0] → Room[last] 수평 체인을 RoomConnector 마커 기준으로 자동 배치한다.

Purpose: Phase 9 WorldGenerator 없이도 08-03 플레이테스트를 즉시 진행할 수 있도록 한다.
Output: Assets/Scripts/World/TestWorldGenerator.cs (단일 파일)
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/phases/08-room-corridor-architecture/08-CONTEXT.md

<interfaces>
<!-- RoomConnector API — 이 타입을 그대로 사용할 것 -->
From Assets/Scripts/World/RoomConnector.cs:
```csharp
public class RoomConnector : MonoBehaviour
{
    public enum Direction { Left, Right }
    [SerializeField] public Direction direction;
    [SerializeField] public GameObject connectedObject;  // Phase 8에서는 null 허용
}
```

연결 규칙:
- Direction.Left  = ENT 마커 (체인에서 이 오브젝트의 입구)
- Direction.Right = EXIT 마커 (체인에서 이 오브젝트의 출구)

기존 커넥터 위치 (로컬, 원점 기준):
- Corridor_Flat  : ENT(-6,0)  → EXIT(6,0)   (높이 변화 없음)
- Corridor_Up   : ENT(-6,0)  → EXIT(7,4)   (4유닛 상승)
- Corridor_Down : ENT(-7,4)  → EXIT(6,0)   (4유닛 하강)
- Room 프리팹   : ENT/EXIT 위치는 RoomMarkerTool 실행 결과에 따라 다름

FloorSpawner 패턴 (참고 — 건드리지 않음):
- GetComponentsInChildren<T>(true) 패턴 사용
- 씬 싱글톤 패턴: public static Instance { get; private set; }
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: TestWorldGenerator.cs 작성</name>
  <files>Assets/Scripts/World/TestWorldGenerator.cs</files>
  <action>
아래 스펙으로 TestWorldGenerator.cs를 신규 작성한다. 기존 FloorSpawner.cs는 절대 수정하지 않는다.

**클래스 구조:**

```csharp
/// <summary>
/// 08-03 플레이테스트 전용 간이 WorldGenerator.
/// Inspector에서 Room/Corridor 프리팹 배열을 지정하면 Start()에서
/// _roomPrefabs[0] → _corridorPrefabs[0] → _roomPrefabs[last] 수평 체인을 자동 배치한다.
///
/// IMPORTANT: 이 컴포넌트를 씬에 배치할 때는 FloorSpawner 컴포넌트(또는 해당 GameObject)를
/// Inspector에서 비활성화(disable)해야 한다. 두 시스템이 동시에 동작하면 충돌한다.
/// </summary>
public class TestWorldGenerator : MonoBehaviour
```

**Inspector 필드 (SerializeField, private):**
- `_roomPrefabs` : `GameObject[]` — 최소 1개 필요. [0] = Room1, [last] = Room2. 1개뿐이면 [0]을 양쪽에 사용.
- `_corridorPrefabs` : `GameObject[]` — 최소 1개 필요. [0]을 사용.

**Start() 로직:**

1. 유효성 검사:
   - `_roomPrefabs`가 null이거나 길이 0이면 `Debug.LogError("TestWorldGenerator: _roomPrefabs is empty")` 후 return
   - `_corridorPrefabs`가 null이거나 길이 0이면 `Debug.LogError("TestWorldGenerator: _corridorPrefabs is empty")` 후 return

2. Room1 스폰:
   - `Instantiate(_roomPrefabs[0], Vector3.zero, Quaternion.identity)`
   - `prevExitPos = FindConnector(room1, RoomConnector.Direction.Right)?.transform.position ?? Vector3.zero`
   - Right 커넥터가 없으면 `Debug.LogWarning("Room1 has no Right RoomConnector")`

3. Corridor 스폰 및 정렬:
   - `Instantiate(_corridorPrefabs[0], Vector3.zero, Quaternion.identity)`
   - `AlignByEntry(corridor, prevExitPos)`
   - Right 커넥터 위치를 `prevExitPos`로 업데이트

4. Room2 스폰 및 정렬:
   - 프리팹: `_roomPrefabs.Length > 1 ? _roomPrefabs[_roomPrefabs.Length - 1] : _roomPrefabs[0]`
   - `Instantiate(...)`, `AlignByEntry(room2, prevExitPos)`

**헬퍼 메서드 2개 (private):**

```csharp
// go의 자식 중 direction과 일치하는 첫 번째 RoomConnector를 반환. 없으면 null.
private RoomConnector FindConnector(GameObject go, RoomConnector.Direction direction)
{
    foreach (RoomConnector rc in go.GetComponentsInChildren<RoomConnector>(true))
    {
        if (rc.direction == direction) return rc;
    }
    return null;
}

// go를 원점(Vector3.zero)에 Instantiate한 후 Left 커넥터가 targetWorldPos에 오도록 이동.
// 원점 기준이므로 Left 커넥터의 transform.position = 루트 기준 로컬 오프셋과 동일.
private void AlignByEntry(GameObject go, Vector3 targetWorldPos)
{
    RoomConnector entry = FindConnector(go, RoomConnector.Direction.Left);
    if (entry == null)
    {
        Debug.LogWarning($"TestWorldGenerator: {go.name} has no Left RoomConnector — placed at {targetWorldPos}");
        go.transform.position = targetWorldPos;
        return;
    }
    // entry.transform.position은 현재 원점 기준 — 즉 루트에서의 오프셋
    go.transform.position = targetWorldPos - entry.transform.position;
}
```

**금지사항:**
- FloorSpawner.cs 수정 금지
- Awake(), Update(), 기타 Unity lifecycle 추가 금지 — Start() 하나만 사용
- 적 스폰 로직 추가 금지 (테스트용이므로 배치만)
- 불필요한 using 문 추가 금지 (`using UnityEngine;` 하나면 충분)
  </action>
  <verify>
    <automated>
      1. 파일 존재 확인: Assets/Scripts/World/TestWorldGenerator.cs
      2. Unity Editor에서 컴파일 오류 없이 로드 확인 (Console 창 오류 0개)
      3. 빈 GameObject에 TestWorldGenerator 컴포넌트 부착 → Inspector에 _roomPrefabs, _corridorPrefabs 배열 노출 확인
      4. _roomPrefabs[0]=Room_Combat, _corridorPrefabs[0]=Corridor_Flat 지정 → Play 버튼 → Scene에 3개 오브젝트 배치, Room_Combat EXIT 위치와 Corridor_Flat ENT 위치가 일치하는지 Scene View Gizmo(초록/파란 구)로 확인
    </automated>
  </verify>
  <done>
    - TestWorldGenerator.cs가 컴파일 오류 없이 로드된다
    - Start() 실행 시 3개 오브젝트(Room1, Corridor, Room2)가 Scene에 생성된다
    - 각 연결부에서 이전 오브젝트의 초록 구(Right)와 다음 오브젝트의 파란 구(Left)가 같은 위치에 겹친다
    - FloorSpawner.cs는 변경되지 않는다
  </done>
</task>

</tasks>

<verification>
- `Assets/Scripts/World/TestWorldGenerator.cs` 존재
- Unity Console 컴파일 오류 0개
- Play Mode에서 Hierarchy에 3개 GameObject 생성됨
- Scene View에서 RoomConnector Gizmo(구)가 연결부에서 겹쳐 보임
</verification>

<success_criteria>
Inspector에서 Room/Corridor 프리팹만 연결하면 Start()가 수평 체인을 자동 배치하고,
RoomConnector Gizmo로 정렬 정확도를 육안 확인할 수 있다.
</success_criteria>

<output>
완료 후 `.planning/quick/260629-jmp-worldgenerator/260629-jmp-SUMMARY.md`를 작성한다.
</output>
