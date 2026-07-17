using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase15/Swap WorldGenerator Pool To BossFsmTest Only,
///       Fast/Phase15/Restore WorldGenerator Original Room Pool
/// D-13 (2026-07-17): BossFsmTest_Teleporter를 걸어서 찾아가는 방식이 번거롭다는 사용자 피드백으로,
/// SampleScene의 WorldGenerator._roomPrefabs 풀 자체를 Room_BossFsmTest.prefab 하나만 담도록 임시
/// 교체한다 — Play만 누르면 첫 룸이 항상 Room_BossFsmTest로 스폰된다(WorldGenerator.Start() 참고).
/// 원본 6개 룸 풀은 이 파일에 하드코딩된 GUID로 언제든 복원 가능 — 영구 변경이 아니라 테스트 기간
/// 동안의 가역적 스왑이다. 이 플랜은 복원을 자동 실행하지 않는다 — 사용자가 보스 FSM 테스트를 마치고
/// 일반 플레이테스트로 돌아갈 때 수동으로 Restore 메뉴를 실행하는 것이 후속 작업이다.
/// gsd-plan-checker blocker 이슈 1 (revision, 2026-07-17): 룸 풀이 1개뿐인 상태에서
/// _lookaheadCount/_lookbehindCount가 원래 값(2/2)으로 남아있으면 Start()가 동일 룸(및 그 안의
/// nested BossEnemy)을 5번 스폰해버린다 — Swap은 이 두 필드도 0으로 강제하고, Restore는 2/2로 되돌린다.
/// gsd-plan-checker blocker 이슈 (revision, 2026-07-17, iteration 2 재확인): ExitSpawnPoint 마커 추가
/// (RoomBossFsmTestBuilder.cs Task 1)로 TrySpawnExitPortal()의 조기 반환 조건이 더 이상 성립하지 않게
/// 되어, 씬의 실제 _exitSpawnChance(1 = 100%)가 그대로면 플레이어 스폰 지점에 포탈이 거의 항상 생성되고
/// 부수적으로 두 번째 Room_BossFsmTest(대기룸)와 중복 BossEnemy까지 생긴다 — Swap은 _exitSpawnChance도
/// 0으로 강제하고, Restore는 원래 값(1)으로 되돌린다. _maxExitsActive(3)는 건드릴 필요 없다.
/// </summary>
public static class BossFsmTestPoolSwapTool
{
    private const string TargetSceneName     = "SampleScene";
    private const string BossFsmTestRoomPath = "Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab";

    // 2026-07-17 기준 SampleScene.unity WorldGenerator._roomPrefabs 원본 6개 룸 풀(D-13 스왑 이전) —
    // 순서 그대로 보존. GUIDToAssetPath로 로드하면 각 프리팹의 루트 GameObject를 정확히 가리킨다.
    private static readonly string[] OriginalRoomPrefabGuids =
    {
        "c0f6fea8d7a78ce43ac944ec03d891dc", // fileID 2564265763709976335
        "ed2d93b531268124382b9403fce5ec7b", // fileID 7737809153690431726
        "89dc91aa481422a498c78a73e7f6a4d2", // fileID 3114153540849030163
        "844b014bfac621a44b5ab3524880a63a", // fileID 6386340677169604563
        "0aa738cd2b8fc6a45be52c4403bc2e80", // fileID 8226199497095424007
        "b6ff0323bb78a7b4ba8eaebdaa67a684", // fileID 4532260002168211988
    };

    // 2026-07-17 기준 SampleScene.unity WorldGenerator._lookaheadCount/_lookbehindCount 원본 값
    // (gsd-plan-checker blocker 이슈 1 수정 — Restore가 되돌릴 정확한 소스).
    private const int OriginalLookaheadCount  = 2;
    private const int OriginalLookbehindCount = 2;

    // 2026-07-17 기준 SampleScene.unity WorldGenerator._exitSpawnChance 원본 값 (실측값, 추측 아님) —
    // gsd-plan-checker blocker 이슈 iteration 2 재확인 — Restore가 되돌릴 정확한 소스.
    private const float OriginalExitSpawnChance = 1f;

    [MenuItem("Fast/Phase15/Swap WorldGenerator Pool To BossFsmTest Only")]
    public static void SwapToBossFsmTestOnly()
    {
        var wg = GetWorldGeneratorInActiveScene();
        if (wg == null) return;

        var testRoomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossFsmTestRoomPath);
        if (testRoomPrefab == null)
        {
            Debug.LogError($"[BossFsmTestPoolSwapTool] {BossFsmTestRoomPath} not found — run 'Fast/Phase15/Build Room_BossFsmTest' first.");
            return;
        }

        LogCurrentPool(wg, "Swap 직전");

        var so = new SerializedObject(wg);
        var prop = so.FindProperty("_roomPrefabs");
        prop.arraySize = 1;
        prop.GetArrayElementAtIndex(0).objectReferenceValue = testRoomPrefab;

        // gsd-plan-checker blocker 이슈 1 수정: 룸 풀이 1개뿐인 상태에서 lookahead/lookbehind가
        // 2/2로 남아있으면 Start()가 시작 룸 1개 + SpawnNextPair()x2 + SpawnPrevPair()x2 =
        // 동일 프리팹을 5번 스폰하고, 그 안의 nested BossEnemy도 5마리 동시에 등장한다(거리 게이팅
        // 없이 전원이 즉시 Telegraph를 시작한다). Play만 누르면 격리된 단일 보스 룸만 남도록 0으로 강제한다.
        var lookaheadProp = so.FindProperty("_lookaheadCount");
        var lookbehindProp = so.FindProperty("_lookbehindCount");
        lookaheadProp.intValue = 0;
        lookbehindProp.intValue = 0;

        // gsd-plan-checker blocker 이슈 (revision, 2026-07-17, iteration 2 재확인): ExitSpawnPoint
        // 추가(Task 1)로 TrySpawnExitPortal()의 조기 반환 조건(ExitSpawnPoint 없음)이 더 이상 성립하지
        // 않는다. 씬의 실제 _exitSpawnChance(1 = 100%)가 그대로면 Start()가 시작 룸에서 무조건 호출하는
        // TrySpawnExitPortal(startRoom)이 플레이어 스폰 지점과 동일 위치에 포탈을 거의 항상 생성하고,
        // 부수적으로 두 번째 Room_BossFsmTest 대기룸(및 그 안의 중복 BossEnemy)까지 생성한다. 0으로
        // 강제해 Random.value > _exitSpawnChance 체크가 항상 참이 되도록 해 TrySpawnExitPortal() 자체를
        // 조기 반환시킨다 — 포탈 생성과 대기룸 생성 양쪽 모두 막힌다. _maxExitsActive(3)는 건드릴 필요 없음.
        var exitSpawnChanceProp = so.FindProperty("_exitSpawnChance");
        exitSpawnChanceProp.floatValue = 0f;

        so.ApplyModifiedProperties();

        SaveActiveScene(wg);
        Debug.Log($"[BossFsmTestPoolSwapTool] _roomPrefabs swapped to 1-entry pool ({BossFsmTestRoomPath}), _lookaheadCount/_lookbehindCount forced to 0, _exitSpawnChance forced to 0 (single-boss isolation, no EXIT portal). Run 'Fast/Phase15/Restore WorldGenerator Original Room Pool' when done testing.");
    }

    [MenuItem("Fast/Phase15/Restore WorldGenerator Original Room Pool")]
    public static void RestoreOriginalPool()
    {
        var wg = GetWorldGeneratorInActiveScene();
        if (wg == null) return;

        var so = new SerializedObject(wg);
        var prop = so.FindProperty("_roomPrefabs");
        prop.arraySize = OriginalRoomPrefabGuids.Length;

        for (int i = 0; i < OriginalRoomPrefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(OriginalRoomPrefabGuids[i]);
            var prefab = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[BossFsmTestPoolSwapTool] Could not resolve original room prefab #{i} (guid {OriginalRoomPrefabGuids[i]}) — restore aborted, _roomPrefabs left unchanged.");
                return;
            }
            prop.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
        }

        // gsd-plan-checker blocker 이슈 수정: Swap이 0으로 강제한 lookahead/lookbehind/exitSpawnChance를
        // 원래 값(2026-07-17 SampleScene.unity 확인)으로 되돌린다.
        so.FindProperty("_lookaheadCount").intValue = OriginalLookaheadCount;
        so.FindProperty("_lookbehindCount").intValue = OriginalLookbehindCount;
        so.FindProperty("_exitSpawnChance").floatValue = OriginalExitSpawnChance;

        so.ApplyModifiedProperties();
        SaveActiveScene(wg);
        Debug.Log($"[BossFsmTestPoolSwapTool] _roomPrefabs restored to original {OriginalRoomPrefabGuids.Length}-room pool, _lookaheadCount/_lookbehindCount restored to {OriginalLookaheadCount}/{OriginalLookbehindCount}, _exitSpawnChance restored to {OriginalExitSpawnChance}.");
    }

    private static WorldGenerator GetWorldGeneratorInActiveScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != TargetSceneName)
        {
            Debug.LogError($"[BossFsmTestPoolSwapTool] '{TargetSceneName}'이 열려있지 않습니다 (현재: '{activeScene.name}'). Assets/Scenes/SampleScene.unity를 열고 다시 실행하세요.");
            return null;
        }

        var wg = Object.FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Include);
        if (wg == null)
        {
            Debug.LogError("[BossFsmTestPoolSwapTool] WorldGenerator not found in active scene.");
            return null;
        }
        return wg;
    }

    private static void LogCurrentPool(WorldGenerator wg, string label)
    {
        var so = new SerializedObject(wg);
        var prop = so.FindProperty("_roomPrefabs");
        var names = new System.Text.StringBuilder();
        for (int i = 0; i < prop.arraySize; i++)
        {
            var obj = prop.GetArrayElementAtIndex(i).objectReferenceValue;
            names.Append(obj != null ? obj.name : "null");
            if (i < prop.arraySize - 1) names.Append(", ");
        }
        Debug.Log($"[BossFsmTestPoolSwapTool] {label} _roomPrefabs ({prop.arraySize}): {names}");
    }

    private static void SaveActiveScene(WorldGenerator wg)
    {
        EditorUtility.SetDirty(wg);
        EditorSceneManager.MarkSceneDirty(wg.gameObject.scene);
        EditorSceneManager.SaveScene(wg.gameObject.scene);
    }
}
