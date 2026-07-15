---
phase: quick
plan: 260715-kci
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/World/WorldGenerator.cs
autonomous: true
requirements: []
must_haves:
  truths:
    - "RemoveTail() destroys the same objects, decrements _activeExitCount by the same amount, and removes the same _activatedSections/_pendingSpawns entries as before the refactor"
    - "RemoveHead() destroys the same objects, decrements _activeExitCount by the same amount, and removes the same _activatedSections/_pendingSpawns entries as before the refactor"
    - "FloorTransitionSequence() cleans up the entire old chain (excluding the entered portal's standby room) exactly as before, then still resets _activeExitCount = 0 unconditionally"
    - "Unity compiles WorldGenerator.cs with zero new errors or warnings"
  artifacts:
    - path: "Assets/Scripts/World/WorldGenerator.cs"
      provides: "Shared CleanupSection(GameObject room, GameObject corridor, ExitPortal excludePortal = null) private helper"
      contains: "private void CleanupSection(GameObject room, GameObject corridor, ExitPortal excludePortal = null)"
  key_links:
    - from: "RemoveTail()"
      to: "CleanupSection(room, corridor)"
      via: "direct call, followed by _chain.RemoveFirst()"
      pattern: "CleanupSection\\(room, corridor\\);\\s*\\n\\s*_chain\\.RemoveFirst\\(\\);"
    - from: "RemoveHead()"
      to: "CleanupSection(room, corridor)"
      via: "direct call, followed by _chain.RemoveLast()"
      pattern: "CleanupSection\\(room, corridor\\);\\s*\\n\\s*_chain\\.RemoveLast\\(\\);"
    - from: "FloorTransitionSequence()"
      to: "CleanupSection(chainRoom, chainCorridor, excludePortal: portal)"
      via: "foreach loop body replacement, _chain.Clear() and _activeExitCount = 0 kept after the loop"
      pattern: "CleanupSection\\(chainRoom, chainCorridor, excludePortal: portal\\);"
---

<objective>
Extract the duplicated "room+corridor 노드 정리" logic from RemoveTail(), RemoveHead(), and the foreach cleanup loop inside FloorTransitionSequence() in WorldGenerator.cs into a single private helper `CleanupSection(GameObject room, GameObject corridor, ExitPortal excludePortal = null)`, and update all three call sites to use it.

Purpose: Pure refactor to remove duplication before Phase 16 (boss room WorldGenerator integration, BOSS-10) needs to add a fourth exception case to this cleanup logic. No behavior change.
Output: WorldGenerator.cs with CleanupSection() helper; RemoveTail()/RemoveHead()/FloorTransitionSequence() all delegate to it while keeping their own _chain node removal and (for FloorTransitionSequence) the final _activeExitCount = 0 reset untouched.
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@D:/새 폴더/Fast/.planning/STATE.md
@D:/새 폴더/Fast/Assets/Scripts/World/WorldGenerator.cs
</context>

<interfaces>
<!-- Exact target shape agreed with user prior to planning. Executor must match this exactly
     unless a concrete correctness reason requires deviation — if so, stop and flag it rather
     than silently diverging. -->

```csharp
/// <summary>
/// RemoveTail/RemoveHead/FloorTransitionSequence가 공유하는 "room+corridor 노드 정리" 로직.
/// 포탈 StandbyRoom 파괴(+_activeExitCount 감소, excludePortal이 아닌 경우만) → _activatedSections/
/// _pendingSpawns에서 제거 → corridor/room GameObject Destroy까지 수행한다. _chain에서의 실제 노드 제거
/// (RemoveFirst/RemoveLast/Clear)는 호출자 책임으로 남긴다 — 호출자마다 다른 LinkedList 연산이 필요하기 때문.
/// </summary>
private void CleanupSection(GameObject room, GameObject corridor, ExitPortal excludePortal = null)
{
    ExitPortal portal = room.GetComponentInChildren<ExitPortal>(true);
    if (portal != null && portal != excludePortal && portal.StandbyRoom != null)
    {
        Destroy(portal.StandbyRoom);
        _activeExitCount--;
        Debug.Log($"[WorldGenerator] _activeExitCount = {_activeExitCount}");
    }

    _activatedSections.Remove(room);
    _pendingSpawns.Remove(room);
    if (corridor != null)
    {
        _activatedSections.Remove(corridor);
        _pendingSpawns.Remove(corridor);
    }

    if (corridor != null) Destroy(corridor);
    Destroy(room);
}
```
</interfaces>

<tasks>

<task type="auto">
  <name>Task 1: Add CleanupSection() helper and refactor RemoveTail()/RemoveHead()</name>
  <files>Assets/Scripts/World/WorldGenerator.cs</files>
  <action>
Add the private `CleanupSection` helper method (exact shape shown in the `<interfaces>` block above — including its XML doc comment) directly above `RemoveTail()` (currently starting at line 212).

Then replace the body of `RemoveTail()` (currently lines 212-237):

```csharp
    private void RemoveTail()
    {
        var (room, corridor) = _chain.First.Value;

        // D-08: 이 room이 보유한 포탈의 대기룸을 함께 정리 — 대기룸 메모리 누수 방지 + 포탈 스폰 기회 복원
        ExitPortal portal = room.GetComponentInChildren<ExitPortal>(true);
        if (portal != null && portal.StandbyRoom != null)
        {
            Destroy(portal.StandbyRoom);
            _activeExitCount--;
            Debug.Log($"[WorldGenerator] _activeExitCount = {_activeExitCount}");
        }

        // Phase 14: 정리되는 section의 대기/활성 스폰 기록도 함께 제거 (참조 누수 방지)
        _activatedSections.Remove(room);
        _pendingSpawns.Remove(room);
        if (corridor != null)
        {
            _activatedSections.Remove(corridor);
            _pendingSpawns.Remove(corridor);
        }

        if (corridor != null) Destroy(corridor);
        Destroy(room);
        _chain.RemoveFirst();
    }
```

with:

```csharp
    private void RemoveTail()
    {
        var (room, corridor) = _chain.First.Value;
        CleanupSection(room, corridor);
        _chain.RemoveFirst();
    }
```

Then replace the body of `RemoveHead()` (currently lines 244-268, keep the existing XML doc comment above it untouched):

```csharp
    private void RemoveHead()
    {
        var (room, corridor) = _chain.Last.Value;

        // D-08: 이 room이 보유한 포탈의 대기룸을 함께 정리 — RemoveTail()과 동일 패턴
        ExitPortal portal = room.GetComponentInChildren<ExitPortal>(true);
        if (portal != null && portal.StandbyRoom != null)
        {
            Destroy(portal.StandbyRoom);
            _activeExitCount--;
            Debug.Log($"[WorldGenerator] _activeExitCount = {_activeExitCount}");
        }

        _activatedSections.Remove(room);
        _pendingSpawns.Remove(room);
        if (corridor != null)
        {
            _activatedSections.Remove(corridor);
            _pendingSpawns.Remove(corridor);
        }

        if (corridor != null) Destroy(corridor);
        Destroy(room);
        _chain.RemoveLast();
    }
```

with:

```csharp
    private void RemoveHead()
    {
        var (room, corridor) = _chain.Last.Value;
        CleanupSection(room, corridor);
        _chain.RemoveLast();
    }
```

Do not touch anything else in the file at this point (no changes to FloorTransitionSequence yet — that is Task 2). Do not rename any fields or change any other method.
  </action>
  <verify>
    <automated>MISSING — no automated test harness exists for WorldGenerator.cs (MonoBehaviour, no EditMode/PlayMode tests). Verification is Unity Editor compile check: open Unity Editor (or run `Unity.exe -batchmode -quit -projectPath "D:\새 폴더\Fast" -logFile -` if available) and confirm zero new Console errors/warnings referencing WorldGenerator.cs.</automated>
  </verify>
  <done>
CleanupSection() helper exists above RemoveTail() with the exact signature `private void CleanupSection(GameObject room, GameObject corridor, ExitPortal excludePortal = null)`. RemoveTail() and RemoveHead() are each reduced to 3 lines (destructure, call CleanupSection, remove from chain) and compile with no new errors. A manual read-through confirms the extracted helper reproduces byte-for-byte identical logic (same GetComponentInChildren call, same Destroy calls, same _activeExitCount arithmetic and Debug.Log, same _activatedSections/_pendingSpawns removals, same order) to what RemoveTail()/RemoveHead() did before.
  </done>
</task>

<task type="auto">
  <name>Task 2: Refactor FloorTransitionSequence() cleanup loop to use CleanupSection()</name>
  <files>Assets/Scripts/World/WorldGenerator.cs</files>
  <action>
In `FloorTransitionSequence()`, replace the foreach loop body (originally lines 656-679, now shifted down by the Task 1 insertion — locate by the comment `// D-07 — 기존 체인(현재 room+corridor 전부) 즉시 Destroy` immediately above it):

```csharp
        foreach (var (chainRoom, chainCorridor) in _chain)
        {
            var orphanPortal = chainRoom.GetComponentInChildren<ExitPortal>(true);
            if (orphanPortal != null && orphanPortal != portal && orphanPortal.StandbyRoom != null)
            {
                Destroy(orphanPortal.StandbyRoom);
            }

            // Phase 14: 정리되는 옛 체인 section의 대기/활성 스폰 기록만 개별 제거한다 (RemoveTail/RemoveHead와
            // 동일 패턴). standbyRoom(= 다음 Step의 newRoom)은 _chain에 속한 적이 없어 이 루프가 건드리지
            // 않으므로, 블랭킷 _activatedSections.Clear()/_pendingSpawns.Clear()는 절대 호출하지 않는다 —
            // 호출 시 standbyRoom의 사전 등록된 _pendingSpawns 엔트리까지 함께 지워져 Step 4의
            // TryActivateSection(newRoom)이 등록을 찾지 못해 새 층 시작 Room의 적이 영원히 비활성 상태로 남는다.
            _activatedSections.Remove(chainRoom);
            _pendingSpawns.Remove(chainRoom);
            if (chainCorridor != null)
            {
                _activatedSections.Remove(chainCorridor);
                _pendingSpawns.Remove(chainCorridor);
            }

            if (chainCorridor != null) Destroy(chainCorridor);
            Destroy(chainRoom);
        }
        _chain.Clear();
        _activeExitCount = 0; // 옛 체인에 있던 모든 포탈(입장한 포탈 + 정리된 미사용 포탈)이 함께 사라짐
        Debug.Log($"[WorldGenerator] _activeExitCount = {_activeExitCount}");
```

with:

```csharp
        // 리뷰 수정: 입장한 portal 외 다른 미사용 포탈의 대기룸도 함께 Destroy (excludePortal: portal).
        // Phase 14: 정리되는 옛 체인 section의 대기/활성 스폰 기록만 개별 제거한다 — standbyRoom(= 다음
        // Step의 newRoom)은 _chain에 속한 적이 없어 이 루프가 건드리지 않는다. CleanupSection() 내부의
        // 개별 _activeExitCount-- 는 아래 무조건적인 = 0 리셋으로 덮어써지므로 원래 동작과 동등하다.
        foreach (var (chainRoom, chainCorridor) in _chain)
        {
            CleanupSection(chainRoom, chainCorridor, excludePortal: portal);
        }
        _chain.Clear();
        _activeExitCount = 0; // 옛 체인에 있던 모든 포탈(입장한 포탈 + 정리된 미사용 포탈)이 함께 사라짐
        Debug.Log($"[WorldGenerator] _activeExitCount = {_activeExitCount}");
```

Keep the loop variable names `chainRoom`/`chainCorridor` exactly as in the original (do not rename to `room`/`corridor` at this call site — that would shadow nothing here but deviates from the agreed design). Keep `_chain.Clear()`, `_activeExitCount = 0`, and the `Debug.Log` line immediately after the loop exactly as they are — do not remove or move them, even though CleanupSection() already logs/decrements per-section internally. Do not touch any other part of FloorTransitionSequence() (Steps 1-6, ENTRY/EXIT effect calls, teleport logic, etc.).
  </action>
  <verify>
    <automated>MISSING — no automated test harness exists for WorldGenerator.cs (MonoBehaviour, no EditMode/PlayMode tests). Verification is Unity Editor compile check: open Unity Editor and confirm zero new Console errors/warnings referencing WorldGenerator.cs.</automated>
  </verify>
  <done>
FloorTransitionSequence()'s old-chain cleanup loop now calls `CleanupSection(chainRoom, chainCorridor, excludePortal: portal)` per iteration instead of inlining the destroy/decrement/removal logic. `_chain.Clear()` and `_activeExitCount = 0` (with its Debug.Log) remain immediately after the loop, unchanged. Compiles with no new errors. A manual read-through confirms: for every chain entry except the one holding the entered `portal`, the standby room is destroyed and _activeExitCount is decremented exactly as before (with the final `= 0` reset producing the same end state as before regardless of the intermediate per-portal decrements); _activatedSections/_pendingSpawns entries are removed per-entry identically to before; corridor/room GameObjects are destroyed in the same order as before.
  </done>
</task>

</tasks>

<verification>
After both tasks, confirm via manual read-through of Assets/Scripts/World/WorldGenerator.cs (no PlayMode test exists for WorldGenerator, which is why this is a read-through rather than a runtime assertion):
1. CleanupSection() exists once, matches the agreed signature and body exactly.
2. RemoveTail() and RemoveHead() are each 3 lines, no leftover duplicated cleanup logic.
3. FloorTransitionSequence()'s loop calls CleanupSection(chainRoom, chainCorridor, excludePortal: portal); _chain.Clear() and _activeExitCount = 0 remain after the loop, unchanged from before.
4. No other method in WorldGenerator.cs was touched (diff should be limited to: new CleanupSection() method + 3 call sites).
5. Unity Editor Console shows zero new compile errors or warnings for WorldGenerator.cs.
</verification>

<success_criteria>
WorldGenerator.cs compiles cleanly. RemoveTail(), RemoveHead(), and FloorTransitionSequence() all delegate their room+corridor cleanup to the single CleanupSection() helper. Runtime behavior (Destroy calls, _activeExitCount arithmetic, _activatedSections/_pendingSpawns bookkeeping, order of operations) is unchanged from before the refactor — this is a pure structural extraction with zero behavior diff, verified by manual read-through since no automated test harness covers WorldGenerator.
</success_criteria>

<output>
No SUMMARY.md needed for quick tasks. State update: add row to STATE.md Quick Tasks Completed table.
</output>
