---
phase: quick
plan: 260624-lre
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/World/FloorSpawner.cs
autonomous: true
requirements: []
must_haves:
  truths:
    - "층 전환 시 플레이어가 nextRoom의 RoomEntry 위치(X, Y 모두)로 텔레포트된다"
    - "RoomEntry가 없는 룸에서는 기존 공식 (floor-1)*roomHeight+2f 로 폴백한다"
  artifacts:
    - path: Assets/Scripts/World/FloorSpawner.cs
      provides: "FloorTransitionSequence Step 2 — RoomEntry 기반 텔레포트"
  key_links:
    - from: FloorSpawner.FloorTransitionSequence (Step 2)
      to: RoomEntry component on nextRoom
      via: nextRoom.GetComponentInChildren<RoomEntry>()
---

<objective>
FloorSpawner.FloorTransitionSequence() Step 2의 플레이어 텔레포트 위치를 고정 Y 공식에서 nextRoom의 RoomEntry 컴포넌트 위치로 변경한다.

Purpose: 각 Room 프리팹마다 ENT 오브젝트를 에디터에서 자유롭게 배치해 진입점을 지정할 수 있게 한다.
Output: 수정된 FloorSpawner.cs — RoomEntry 기반 텔레포트 + 폴백 로직
</objective>

<execution_context>
@D:/새 폴더/Projeect_A.E/Projeect_A.E/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/STATE.md

RoomEntry.cs — 현재 구현:
```csharp
public class RoomEntry : MonoBehaviour { }
```
빈 마커 컴포넌트. GetComponentInChildren<RoomEntry>() 로 찾아 .transform.position 사용.

FloorSpawner.cs Step 2 현재 코드 (line 80-87):
```csharp
float newY = (FloorManager.CurrentFloor - 1) * _roomHeight + 2f;
var rb = _playerTransform.GetComponent<Rigidbody2D>();
if (rb != null) rb.linearVelocity = Vector2.zero;
_playerTransform.position = new Vector3(
    _playerTransform.position.x,
    newY,
    0f
);
```
</context>

<tasks>

<task type="auto">
  <name>Task 1: Step 2 텔레포트를 RoomEntry 위치 기반으로 변경</name>
  <files>Assets/Scripts/World/FloorSpawner.cs</files>
  <action>
FloorTransitionSequence() 의 Step 2 블록(line 78-87)을 아래 로직으로 교체한다.

변경 전:
```csharp
// [Step 2] 순간이동 — 플레이어를 새 층 바닥 위 2유닛으로 즉시 이동
float newY = (FloorManager.CurrentFloor - 1) * _roomHeight + 2f;
var rb = _playerTransform.GetComponent<Rigidbody2D>();
if (rb != null) rb.linearVelocity = Vector2.zero;
_playerTransform.position = new Vector3(
    _playerTransform.position.x,
    newY,
    0f
);
```

변경 후:
```csharp
// [Step 2] 순간이동 — nextRoom의 RoomEntry(ENT 마커) 위치로 이동
// RoomEntry가 없으면 고정 공식으로 폴백 (Pitfall: 마커 미배치 룸 호환성)
RoomEntry entry = nextRoom.GetComponentInChildren<RoomEntry>(true);
Vector3 teleportPos;
if (entry != null)
{
    teleportPos = entry.transform.position;
}
else
{
    float fallbackY = (FloorManager.CurrentFloor - 1) * _roomHeight + 2f;
    teleportPos = new Vector3(_playerTransform.position.x, fallbackY, 0f);
}
var rb = _playerTransform.GetComponent<Rigidbody2D>();
if (rb != null) rb.linearVelocity = Vector2.zero;
_playerTransform.position = teleportPos;
```

주의:
- GetComponentInChildren의 인수 `true` — SetActive(false) 자식도 검색 (Pitfall 2 패턴 일관성 유지)
- X좌표도 ENT 위치 기준으로 변경 (teleportPos는 entry.transform.position 전체)
- 폴백 시 X는 기존처럼 _playerTransform.position.x 유지
- 기존 주석 스타일(한국어 + Pitfall 참조) 유지
  </action>
  <verify>
Unity Editor에서 컴파일 오류 없음 확인. Play 모드에서 RoomEntry가 있는 룸으로 전환 시 플레이어가 ENT 오브젝트 위치로 텔레포트됨을 확인.
  </verify>
  <done>
- FloorSpawner.cs 컴파일 성공
- RoomEntry 있는 룸: 플레이어가 entry.transform.position(X, Y 모두)으로 텔레포트
- RoomEntry 없는 룸: 기존 공식 Y = (floor-1)*roomHeight+2f 로 정상 동작
  </done>
</task>

</tasks>

<verification>
1. Unity Editor Console에 컴파일 오류 없음
2. Room 프리팹에 RoomEntry 자식을 배치 후 Play → 층 전환 시 ENT 위치로 텔레포트 확인
3. RoomEntry 없는 룸에서 층 전환 시 폴백 공식으로 동작(콘솔 에러 없음) 확인
</verification>

<success_criteria>
- FloorSpawner.cs Step 2: GetComponentInChildren&lt;RoomEntry&gt;(true) 로 ENT 마커 조회
- entry != null → teleportPos = entry.transform.position (X, Y 모두 ENT 기준)
- entry == null → fallback Y 공식 유지, X는 플레이어 현재 X 유지
- 기존 rb.linearVelocity = Vector2.zero 초기화 코드 보존
</success_criteria>

<output>
After completion, create `.planning/quick/260624-lre-floorspawner-step-2-y-nextroom-roomentry/260624-lre-SUMMARY.md`
</output>
