---
phase: quick
plan: 260624-mcv
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/World/FloorSpawner.cs
autonomous: true
requirements: [FLOOR-03]
must_haves:
  truths:
    - "적 오브젝트가 씬 루트가 아닌 Room GameObject의 자식 계층에 생성된다"
    - "Destroy(_currentRoom) 호출 시 Room 자식인 적도 함께 파괴된다"
  artifacts:
    - path: "Assets/Scripts/World/FloorSpawner.cs"
      provides: "room.transform를 parent로 전달하는 Instantiate 호출"
      contains: "Instantiate(enemyPrefab, child.position, Quaternion.identity, room.transform)"
  key_links:
    - from: "FloorSpawner.SpawnRoom()"
      to: "room GameObject"
      via: "Instantiate parent parameter"
      pattern: "Instantiate.*room\\.transform"
---

<objective>
FloorSpawner.cs의 SpawnRoom() 내 적 Instantiate 호출(157번째 줄)에 room.transform을 parent로 추가한다.

Purpose: 현재 적이 씬 루트에 생성되어 Destroy(_currentRoom) 호출 시 적이 함께 파괴되지 않는다. Room 계층 안에 생성하면 Room 파괴 시 적도 자동으로 정리된다(FLOOR-04).
Output: Assets/Scripts/World/FloorSpawner.cs (1줄 수정)
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@Assets/Scripts/World/FloorSpawner.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: SpawnRoom() Instantiate에 room.transform parent 추가</name>
  <files>Assets/Scripts/World/FloorSpawner.cs</files>
  <action>
    FloorSpawner.cs의 SpawnRoom() 메서드 내 157번째 줄을 수정한다.

    수정 전:
    ```csharp
    GameObject enemy = Instantiate(enemyPrefab, child.position, Quaternion.identity);
    ```

    수정 후:
    ```csharp
    GameObject enemy = Instantiate(enemyPrefab, child.position, Quaternion.identity, room.transform);
    ```

    이 한 줄만 변경한다. 다른 코드는 건드리지 않는다.

    근거: Unity의 Instantiate(prefab, position, rotation, parent) 오버로드는 월드 좌표계 position/rotation을 그대로 유지하면서 parent 계층에 편입한다. child.position은 이미 월드 좌표이므로 적의 실제 위치는 변하지 않는다.
  </action>
  <verify>
    파일 내 `Instantiate(enemyPrefab, child.position, Quaternion.identity, room.transform)` 문자열이 존재하는지 확인:
    `grep -n "room.transform" Assets/Scripts/World/FloorSpawner.cs`
  </verify>
  <done>
    - FloorSpawner.cs 157번째 줄이 4-인수 Instantiate 호출로 변경됨
    - 적 GameObject가 Room의 자식으로 생성되어 Destroy(room) 시 함께 파괴됨
    - SetActive(false) 로직(FLOOR-03)은 그대로 유지됨
  </done>
</task>

</tasks>

<verification>
Unity Editor에서 Play → FloorTransitionSequence 발동 후 Hierarchy 창 확인:
- nextRoom 하위에 MeleeEnemy / RangedEnemy 오브젝트가 자식으로 나타나야 함
- 층 전환 완료 후 이전 Room과 그 자식 적들이 Hierarchy에서 사라져야 함
</verification>

<success_criteria>
적 Instantiate 호출이 room.transform을 parent로 전달하여, 생성된 적이 Room 계층의 자식으로 편입되고 Room 파괴 시 자동으로 정리된다.
</success_criteria>

<output>
완료 후 `.planning/quick/260624-mcv-floorspawner-cs-157-instantiate-room-tra/260624-mcv-SUMMARY.md` 생성
</output>
