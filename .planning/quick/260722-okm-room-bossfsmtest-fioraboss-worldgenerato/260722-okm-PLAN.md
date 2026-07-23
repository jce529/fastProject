---
phase: quick
plan: 260722-okm
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/World/DebugRoomTeleporter.cs
  - Assets/Scenes/SampleScene.unity
autonomous: false
requirements: []
must_haves:
  truths:
    - "Play 버튼을 누르면 플레이어가 별도 조작(걷기+Up Arrow) 없이 자동으로 Room_BossFsmTest(FioraBoss가 배치된 방)로 순간이동한다"
    - "WorldGenerator.cs, WorldGenerator._roomPrefabs 풀, SelectCorridor()/SpawnNextPair()/SpawnPrevPair() 로직은 전혀 수정되지 않는다"
    - "_autoTriggerOnStart 필드를 0으로 되돌리거나 이번에 추가한 코드 블록을 삭제하는 것만으로 이 임시 조치를 완전히 원복할 수 있다"
  artifacts:
    - path: "Assets/Scripts/World/DebugRoomTeleporter.cs"
      provides: "임시 자동 트리거 플래그 + 1프레임 지연 후 TeleportToRoom() 자동 호출 코루틴"
      contains: "_autoTriggerOnStart"
    - path: "Assets/Scenes/SampleScene.unity"
      provides: "BossFsmTest_Teleporter GameObject 인스턴스의 _autoTriggerOnStart를 1로 설정"
      contains: "_autoTriggerOnStart: 1"
  key_links:
    - from: "DebugRoomTeleporter.Start()"
      to: "TeleportToRoom()"
      via: "AutoTriggerAfterFrame() 코루틴 — yield return null(1프레임 대기, WorldGenerator.Start()의 시작 룸 텔레포트가 먼저 끝나도록 순서 보장)로 후 호출"
      pattern: "AutoTriggerAfterFrame"
    - from: "SampleScene.unity BossFsmTest_Teleporter 인스턴스"
      to: "DebugRoomTeleporter._autoTriggerOnStart"
      via: "직렬화된 MonoBehaviour 필드 값"
      pattern: "_autoTriggerOnStart: 1"
---

<objective>
Phase 18의 두 체크포인트(18-01: CombatController→OverclockModule 마이그레이션 손맛 확인, 18-02: BossEnemyBase/FioraBoss 추출 + BossUnlockManager 영속성 손맛 확인)를 사용자가 수동으로 플레이테스트할 수 있도록, 현재 필요한 "플레이어 스폰 지점 근처 오프셋까지 걸어가서 Up Arrow 키를 누르는" 불편한 절차 없이 FioraBoss(Room_BossFsmTest)에 자동으로 도달하게 만든다.

**중요 — 이 계획은 Phase 16(BOSS-01/02/07/09/10: WorldGenerator를 통한 확률적 보스 룸 스폰, 층 타이머 일시정지/재개, 입장 카메라 연출)의 정식 구현이 아니다.** Phase 16은 별도 discuss-phase를 거쳐 확률/밸런싱/카메라 연출/타이머 통합을 논의한 뒤 정식으로 계획된다. 이 계획은 그 전까지 Phase 18 체크포인트 플레이테스트만을 위한 **임시/가역적** 편의 조치다.

**조사 결과에 따른 설계 결정:** `Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab`을 직접 확인한 결과, 이 방에는 `RoomConnector`(Left/Right 커넥터)와 `CameraBound`가 없다 — `WorldGenerator.SpawnNextPair()`/`SpawnPrevPair()`가 사용하는 `AlignByEntry()`/`AlignByExit()`/`RecomputeCameraBounds()`는 전부 이 두 컴포넌트를 전제로 동작한다. 즉 이 방을 `_roomPrefabs` 풀이나 체인 생성 로직에 실제로 "편입"시키려면 RoomConnector/CameraBound 부착 및 정렬 로직 검증이 추가로 필요한데, 이는 정확히 Phase 16/17이 다뤄야 할 작업이며 사용자가 이미 금지한 "풀에 영구 편입"과 본질적으로 같은 리스크를 안는다.

대신, 이미 검증되어 동작 중인 `DebugRoomTeleporter.TeleportToRoom()`(Room_BossFsmTest + FioraBoss를 정확히 지금과 동일한 방식으로 Instantiate하고 RoomEntry 기준으로 플레이어를 이동시키는 경로, 15-05 Task 1/2에서 이미 완성되어 씬에 배선됨)를 그대로 재사용하되, "트리거 존 진입 + Up Arrow 키 입력"이라는 수동 트리거만 "씬 시작 후 자동 실행"으로 대체한다. WorldGenerator.cs, `_roomPrefabs` 풀, 확률 로직은 일절 건드리지 않는다.

Purpose: 사용자가 Play 버튼만 누르면 즉시 FioraBoss와 마주해 Phase 18의 두 파킹된 체크포인트를 바로 플레이테스트할 수 있게 한다.
Output: `DebugRoomTeleporter.cs`에 추가된 임시 자동 트리거 플래그+코루틴, `SampleScene.unity`의 `BossFsmTest_Teleporter` 인스턴스에서 그 플래그를 활성화한 씬 데이터.
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@D:/새 폴더/Fast/.planning/STATE.md
@D:/새 폴더/Fast/Assets/Scripts/World/DebugRoomTeleporter.cs
</context>

<interfaces>
<!-- DebugRoomTeleporter.cs 현재 구조 (핵심 발췌) — 아래 TeleportToRoom()은 그대로 재사용하고
     호출 경로만 추가한다. 수정하지 않음. -->

```csharp
public class DebugRoomTeleporter : MonoBehaviour
{
    [Header("Target Room")]
    [SerializeField] private GameObject targetRoomPrefab;
    [SerializeField] private float      offsetX         = 30f;
    [SerializeField] private float      offsetY         = 30f;
    [SerializeField] private bool       activateEnemies = false;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject _meleePrefab;
    [SerializeField] private GameObject _rangedPrefab;

    [Header("Boss (D-11)")]
    [SerializeField] private GameObject _bossPrefab;

    private PlayerController _player;
    private Transform        _playerTransform;
    private bool             _playerInZone;

    private void Awake()
    {
        _player          = FindFirstObjectByType<PlayerController>();
        _playerTransform = _player != null ? _player.transform : null;
    }

    private void TeleportToRoom() { /* ... 기존 그대로, 수정 없음 ... */ }
}
```

씬(SampleScene.unity)의 기존 인스턴스 — `BossFsmTest_Teleporter` GameObject:
```yaml
--- !u!114 &1613742383
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: b25dec5cdec7f81458133f9f86254afd, type: 3}
  m_EditorClassIdentifier: Assembly-CSharp::DebugRoomTeleporter
  targetRoomPrefab: {fileID: 1828846100013170834, guid: 15767b2141925b74c9368af8972ad95e, type: 3}
  offsetX: 30
  offsetY: 30
  activateEnemies: 0
  _meleePrefab: {fileID: 0}
  _rangedPrefab: {fileID: 0}
  _bossPrefab: {fileID: 7269809630814702082, guid: c56614db2e605934290c055d9de938b9, type: 3}
```
</interfaces>

<tasks>

<task type="auto">
  <name>Task 1: Add temporary auto-trigger flag + coroutine to DebugRoomTeleporter.cs</name>
  <files>Assets/Scripts/World/DebugRoomTeleporter.cs</files>
  <action>
Add `using System.Collections;` to the top of the file (needed for the new `IEnumerator` coroutine), alongside the existing `using` lines:

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
```

Add a new serialized field directly below the existing `[Header("Boss (D-11)")] [SerializeField] private GameObject _bossPrefab;` line:

```csharp
    [Header("Boss (D-11)")]
    [SerializeField] private GameObject _bossPrefab;

    [Header("TEMPORARY — Phase 18 체크포인트 플레이테스트 편의용 (Phase 16 정식 기능 아님, 완료 후 제거)")]
    [SerializeField] private bool _autoTriggerOnStart = false;
```

Add a new `Start()` method and `AutoTriggerAfterFrame()` coroutine directly below the existing `Awake()` method:

```csharp
    private void Awake()
    {
        _player          = FindFirstObjectByType<PlayerController>();
        _playerTransform = _player != null ? _player.transform : null;
    }

    // TEMPORARY — Phase 18 체크포인트 플레이테스트 편의용. 트리거 존 진입+Up Arrow 입력 없이
    // 씬 시작 시 자동으로 TeleportToRoom()을 1회 실행한다. Phase 16 정식 보스 룸 스폰 구현이
    // 완료되면 이 필드/메서드를 통째로 제거할 것 (또는 _autoTriggerOnStart를 false로 되돌릴 것).
    private void Start()
    {
        if (_autoTriggerOnStart) StartCoroutine(AutoTriggerAfterFrame());
    }

    private IEnumerator AutoTriggerAfterFrame()
    {
        // WorldGenerator.Start()가 시작 룸 텔레포트(플레이어를 Vector3.zero 기준 시작 룸 ExitSpawnPoint로
        // 이동)를 먼저 끝내도록 한 프레임 양보한다 — 그래야 이 자동 텔레포트가 그 위치를 덮어써서
        // 최종적으로 플레이어가 보스 룸에 있게 된다.
        yield return null;
        TeleportToRoom();
    }
```

Do not modify `TeleportToRoom()`, `OnTriggerEnter2D()`, `OnTriggerExit2D()`, `Update()`, or `OnDrawGizmos()` — those stay exactly as-is. This task only adds the flag + Start()/coroutine.
  </action>
  <verify>
    <automated>grep -c "_autoTriggerOnStart" "Assets/Scripts/World/DebugRoomTeleporter.cs"</automated>
  </verify>
  <done>DebugRoomTeleporter.cs compiles with the new `_autoTriggerOnStart` field, `Start()` method, and `AutoTriggerAfterFrame()` coroutine added; `TeleportToRoom()` and all other existing methods are byte-for-byte unchanged (diff limited to the new using line, field, and two new methods).</done>
</task>

<task type="auto">
  <name>Task 2: Enable auto-trigger on the scene's BossFsmTest_Teleporter instance</name>
  <files>Assets/Scenes/SampleScene.unity</files>
  <action>
In the `BossFsmTest_Teleporter` GameObject's `DebugRoomTeleporter` MonoBehaviour block (find via `m_EditorClassIdentifier: Assembly-CSharp::DebugRoomTeleporter`), add `_autoTriggerOnStart: 1` as a new line directly after the existing `_bossPrefab: {...}` line:

Before:
```yaml
  _bossPrefab: {fileID: 7269809630814702082, guid: c56614db2e605934290c055d9de938b9, type: 3}
```

After:
```yaml
  _bossPrefab: {fileID: 7269809630814702082, guid: c56614db2e605934290c055d9de938b9, type: 3}
  _autoTriggerOnStart: 1
```

Do not touch any other field on this GameObject (targetRoomPrefab/offsetX/offsetY/activateEnemies/_meleePrefab/_rangedPrefab stay exactly as they are), and do not touch any other GameObject/MonoBehaviour block in the scene file.
  </action>
  <verify>
    <automated>grep -A1 "_bossPrefab: {fileID: 7269809630814702082" "Assets/Scenes/SampleScene.unity" | grep -c "_autoTriggerOnStart: 1"</automated>
  </verify>
  <done>SampleScene.unity's BossFsmTest_Teleporter DebugRoomTeleporter instance has `_autoTriggerOnStart: 1` serialized immediately after `_bossPrefab`; no other line in the scene file changed.</done>
</task>

<task type="checkpoint:human-verify" gate="blocking">
  <name>Task 3: Confirm boss auto-teleport works, then playtest paused Phase 18 checkpoints</name>
  <files>없음 (검증 전용 — 파일 변경 없음)</files>
  <action>
자동화 테스트 프레임워크가 없어 이 임시 조치의 실효성(자동 텔레포트가 실제로 FioraBoss 앞에 플레이어를 데려다 놓는지)은 Play 모드 실행으로만 확인 가능하다 — 사용자에게 아래 how-to-verify 체크리스트를 요청하고 결과를 수집한다. 문제가 보고되면(예: WorldGenerator.Start()보다 먼저 실행되어 위치가 덮어써짐, 보스 미배치) AutoTriggerAfterFrame()의 yield 타이밍 또는 씬 배선을 재검토한다.
  </action>
  <what-built>
DebugRoomTeleporter.cs를 자동 트리거 가능하도록 확장(Task 1)했고, SampleScene.unity의 기존 BossFsmTest_Teleporter 인스턴스에서 그 자동 트리거를 활성화(Task 2)했다. WorldGenerator.cs와 `_roomPrefabs` 풀은 전혀 건드리지 않았다 — 여전히 정상 룸 체인은 기존과 동일하게 확률적으로 생성된다.
  </what-built>
  <how-to-verify>
1. Unity Editor에서 SampleScene을 Play 모드로 실행한다.
2. 별도 조작(걷기, Up Arrow 키 입력) 없이도 자동으로 Room_BossFsmTest로 순간이동하며 FioraBoss(BossEnemy)가 배치되어 있는지 확인한다.
3. 이 자동 텔레포트가 확인되면, 곧바로 이어서 Phase 18의 두 파킹된 체크포인트(18-01-PLAN.md Task 3: CombatController→OverclockModule 마이그레이션 손맛 확인, 18-02-PLAN.md Task 3: BossEnemyBase/FioraBoss 추출 + BossUnlockManager 영속성 손맛 확인)를 이 보스로 플레이테스트한다.
4. 확인 후 Play 모드를 종료해도 씬 에셋의 `_autoTriggerOnStart: 1` 값은 유지된다 — 이후 이 임시 조치를 제거하려면 Task 2에서 추가한 그 한 줄을 삭제(또는 0으로 변경)하고, Task 1에서 추가한 코드 블록(필드+Start()+코루틴)을 삭제하면 된다.
  </how-to-verify>
  <verify>
    <automated>MANUAL-ONLY — Play 모드 실제 실행 및 시각 확인이 필요해 자동화 불가. 통과 기준: how-to-verify 1~2번 확인(자동 텔레포트 성공)</automated>
  </verify>
  <acceptance_criteria>
    - 자동 텔레포트로 FioraBoss와 마주함을 확인
    - Phase 18의 두 파킹된 체크포인트(18-01/18-02 Task 3)를 이 보스로 플레이테스트 진행
  </acceptance_criteria>
  <done>Play 진입만으로 Room_BossFsmTest/FioraBoss에 도달함이 확인되고, 이 임시 조치가 WorldGenerator.cs/_roomPrefabs 풀을 건드리지 않았음이 재확인된다.</done>
  <resume-signal>Type "approved" once boss room auto-teleport is confirmed working, or describe any issue (e.g., teleport happens too early/before WorldGenerator finishes, boss missing, wrong room).</resume-signal>
</task>

</tasks>

<verification>
1. `grep -c "_autoTriggerOnStart" Assets/Scripts/World/DebugRoomTeleporter.cs` returns >= 2 (field declaration + usage in Start()).
2. `grep -c "_autoTriggerOnStart: 1" Assets/Scenes/SampleScene.unity` returns exactly 1.
3. `git diff --stat` for this change touches only `Assets/Scripts/World/DebugRoomTeleporter.cs` and `Assets/Scenes/SampleScene.unity` — `Assets/Scripts/World/WorldGenerator.cs` is untouched.
4. Manual Play-mode confirmation (checkpoint task) that the player auto-teleports to Room_BossFsmTest/FioraBoss with no manual walk/keypress.
</verification>

<success_criteria>
Pressing Play in the Unity Editor automatically lands the player in Room_BossFsmTest facing FioraBoss, with no manual walk-to-offset + Up Arrow keypress required — unblocking manual playtesting of the two paused Phase 18 checkpoints (18-01, 18-02). WorldGenerator.cs and the normal probabilistic room-chain generation (`_roomPrefabs` pool, SelectCorridor/SpawnNextPair/SpawnPrevPair) remain completely unmodified. The entire mechanism is confined to one temporary flag + one coroutine in an already-existing debug-only script, clearly commented as temporary, and reversible by deleting that one field/flag and the one serialized scene line — it is explicitly NOT the Phase 16 WorldGenerator boss-room integration feature.
</success_criteria>

<output>
No SUMMARY.md needed for quick tasks. State update: add row to STATE.md Quick Tasks Completed table after execution.
</output>
