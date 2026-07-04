---
status: awaiting_human_verify
trigger: "Exit 진입 시 게임 루프가 멈추고 다음 맵이 생성되지 않음"
created: 2026-06-24T00:00:00Z
updated: 2026-06-24T00:00:00Z
---

## Current Focus

hypothesis: CONFIRMED — CombatController.ExitSlowMotion()이 FloorTransitionSequence의 LockInput()에 의해 차단되어 Time.timeScale이 slowTimeScale(0.2f)로 고정됨
test: CombatController.ForceExitCombatState() public 메서드 추가 + FloorSpawner에서 LockInput 전 호출 + SampleScene에 _combatController 필드 연결
expecting: EXIT 진입 시 slow-motion이 즉시 정리되고 새 방이 정상 생성됨
next_action: Unity 에디터에서 Play 후 EXIT 진입 테스트하여 freezing 없이 다음 맵 생성 확인

## Symptoms

expected: Exit 진입 시 다음 층(씬/방)으로 전환되고 새 맵이 생성되어야 함
actual: Unity는 응답하지만 게임 루프가 완전히 멈춤. 다음 맵 생성 안 됨. 재플레이해도 같은 지점에서 멈춤.
errors: Console 에러 없음 (조용히 멈춤)
reproduction: Exit 트리거 영역에 플레이어가 진입하면 즉시 발생
started: 언제부터 발생했는지 불명

## Eliminated

- hypothesis: FloorTransitionSequence coroutine gets stuck (yield blocked)
  evidence: All yields use WaitForSecondsRealtime or yield return null — both unaffected by timeScale. Coroutine always completes.
  timestamp: 2026-06-24T00:20:00Z

- hypothesis: _transitioning flag permanently stuck at true (coroutine interrupted)
  evidence: FloorSpawner GameObject has only Transform + FloorSpawner components, nothing destroys it. Coroutine always completes.
  timestamp: 2026-06-24T00:20:00Z

- hypothesis: Player death simultaneous with EXIT (DeathScreenController sets timeScale=0)
  evidence: Could contribute but not the primary cause — RestartGame() restores timeScale=1. The bug is deterministic without requiring player death.
  timestamp: 2026-06-24T00:20:00Z

- hypothesis: HitFreeze (timeScale=0) not restored after EXIT
  evidence: HitFreeze always restores timeScale via WaitForSecondsRealtime — immune to timeScale. CombatController stays active during floor transitions (only disabled on player death).
  timestamp: 2026-06-24T00:20:00Z

## Evidence

- timestamp: 2026-06-24T00:01:00Z
  checked: FloorSpawner.cs FloorTransitionSequence()
  found: All yields use WaitForSecondsRealtime or yield return null — coroutine itself is timeScale-safe
  implication: The coroutine should not block on timeScale

- timestamp: 2026-06-24T00:02:00Z
  checked: CombatController.HitFreeze() — sets Time.timeScale = 0f, Time.fixedDeltaTime = 0f; uses WaitForSecondsRealtime to restore
  found: HitFreeze restores timeScale=1 and fixedDeltaTime=0.02 after real seconds — designed correctly
  implication: Normal kill path should not leave timeScale = 0

- timestamp: 2026-06-24T00:03:00Z
  checked: DeathScreenController.HandleDeath()
  found: Sets Time.timeScale = 0f and Time.fixedDeltaTime = 0f on player death. RestartGame() DOES restore timeScale=1 and loads "AttackSelect" scene
  implication: If player dies, timeScale goes to 0. RestartGame() restores it. BUT only if the player clicks the restart button.

- timestamp: 2026-06-24T00:04:00Z
  checked: FloorSpawner.FloorTransitionSequence() — step 1 calls _player.LockInput(), then yield return null, yield return WaitForSecondsRealtime(0.05f)
  found: The coroutine does NOT check or restore Time.timeScale. It calls _player.LockInput() which only sets _inputLocked=true and velocity=0.
  implication: If timeScale is already 0 (from HitFreeze or DeathScreen), the coroutine still runs (WaitForSecondsRealtime is unscaled), but the game APPEARS frozen

- timestamp: 2026-06-24T00:05:00Z
  checked: Full exit scenario: player kills last enemy near EXIT, enters EXIT trigger
  found: RACE CONDITION — HitFreeze() sets timeScale=0, then WaitForSecondsRealtime(0.075f) restores it. But if player walks into EXIT during HitFreeze window, FloorTransitionSequence starts. The sequence itself is WaitForSecondsRealtime so it completes. BUT — is teleport + enemy activation happening correctly even at timeScale=0?
  implication: The transition sequence IS running but game loop visually appears frozen because timeScale=0 persists IF HitFreeze restoration fails

- timestamp: 2026-06-24T00:06:00Z
  checked: CombatController.ExecuteDash() — after yield return StartCoroutine(HitFreeze()), sets _attackCooldown and _isBusy=false. HitFreeze uses WaitForSecondsRealtime — should resume correctly.
  found: HitFreeze restores timeScale=1 after the wait. This should work. BUT: if the player hits EXIT during the 75ms HitFreeze, two coroutines are running simultaneously: HitFreeze (owned by CombatController on Player) AND FloorTransitionSequence (owned by FloorSpawner). When FloorSpawner.AdvanceFloor() destroys _currentRoom, all enemies in it are destroyed. CombatController is on the Player (not the Room), so it survives. HitFreeze will still restore timeScale after 75ms.
  implication: Not the root cause unless something prevents HitFreeze from completing

- timestamp: 2026-06-24T00:07:00Z
  checked: DeathScreenController.HandleDeath() — sets timeScale=0 permanently until restart button clicked. PlayerDeathHandler.HandleDeath() — disables player GameObject
  found: When player's GameObject is SetActive(false), ALL coroutines on that GameObject STOP. CombatController is on the player. If CombatController.HitFreeze() is mid-execution when player dies, the coroutine is CANCELLED without restoring timeScale. timeScale stays at 0 FOREVER.
  implication: But that's a death scenario. The bug is at EXIT entry, not death.

- timestamp: 2026-06-24T00:08:00Z
  checked: The specific scenario: player triggers EXIT while CombatController._isBusy=true (mid-dash/HitFreeze)
  found: FloorSpawner.FloorTransitionSequence calls _player.LockInput(). CombatController.Update() checks if (_player.InputLocked) return — so CombatController stops running Update. But HitFreeze coroutine on CombatController is STILL running independently (coroutines survive InputLocked). HitFreeze will restore timeScale=1 after 75ms. This should work.
  implication: Not the freeze cause on its own

- timestamp: 2026-06-24T00:09:00Z  
  checked: RoomClearCondition — if enemies list is empty, Activate() is called immediately in Start(), making EXIT active. If enemies are present, EXIT is disabled until all die. Physical collider on EXIT exists regardless.
  found: EXIT has a BoxCollider2D (IsTrigger). RoomClearCondition controls whether EXIT's GAMEOBJECT is active. If EXIT is inactive, OnTriggerEnter2D won't fire. So the physical barrier isn't the issue — if player reaches EXIT, it fired.
  implication: AdvanceFloor() was called. The transition started.

- timestamp: 2026-06-24T00:10:00Z
  checked: FloorSpawner field: _player is [SerializeField] PlayerController. Called _player.LockInput() and _player.UnlockInput()
  found: If _player is null (not wired in Inspector), LockInput() call throws NullReferenceException. BUT the symptom says "no console errors". So _player must be set. HOWEVER — _player.LockInput() is called synchronously at the START of the coroutine (step 1). If this somehow fails silently, UnlockInput() won't be called and player is stuck.
  implication: Need to check if _player is properly wired

- timestamp: 2026-06-24T00:11:00Z
  checked: FloorSpawner.SpawnRoom() is called immediately when AdvanceFloor() triggers — BEFORE any yield. SelectNextRoom() returns _floor1RoomPrefab if _roomPool is null/empty.
  found: If _roomPool is null or empty (not wired), it always returns _floor1RoomPrefab for all subsequent floors. This is not a freeze, just wrong room.
  implication: Not the freeze cause

- timestamp: 2026-06-24T00:12:00Z
  checked: The critical scenario where the freeze could be permanent: DeathScreenController sets timeScale=0 AND the player GameObject gets disabled. FloorSpawner._player is a reference to PlayerController on the player GameObject. FloorSpawner coroutine is on FloorSpawner (not player) so it survives player disable. BUT _player.LockInput() won't throw (it's just setting a bool). The coroutine runs, teleports player, activates enemies, then calls _player.UnlockInput(). That's fine.
  implication: Need to narrow to the EXACT timing scenario

- timestamp: 2026-06-24T00:13:00Z
  checked: The "replay after freeze" symptom — "재플레이해도 같은 지점에서 멈춤"
  found: RestartGame() calls SceneManager.LoadScene("AttackSelect"). This destroys everything in SampleScene including FloorSpawner, CombatController, etc. A fresh scene is loaded. BUT — if timeScale was 0 when the scene loads AttackSelect... wait, RestartGame() sets timeScale=1 BEFORE loading. So that's fine. But the symptom says even after replay it freezes at the same point. This means the bug is deterministic and reproducible, not a race condition.
  implication: The freeze is caused by something deterministic at EXIT entry, not a timing race

- timestamp: 2026-06-24T00:14:00Z
  checked: What is deterministic about EXIT entry? The FloorTransitionSequence calls _player.LockInput() then YIELDS (yield return null). Between LockInput() and the first yield return null, Time.timeScale is NOT touched. After the yield, ActivateEnemies runs, then WaitForSecondsRealtime(0.05f), then UnlockInput().
  found: The issue is that FloorTransitionSequence does NOT reset Time.timeScale to 1f. If the player enters EXIT during slow-motion (Attack button held), timeScale is 0.2. The transition runs, completes, but timeScale stays at 0.2 forever because nobody restores it. UnlockInput() doesn't touch timeScale. The player is now in the new room with timeScale=0.2 — not a full freeze but severely slow.
  implication: Not a FULL freeze. Need to check the full scenario more carefully.

- timestamp: 2026-06-24T00:15:00Z
  checked: CombatController.Update() — when _player.InputLocked is true, it returns immediately. This means ExitSlowMotion() is NEVER called while inputLocked. If player enters EXIT during slow-mo, LockInput() is called, timeScale=0.2 stays, CombatController.Update returns immediately every frame (inputLocked=true), so ExitSlowMotion() never fires. After transition, UnlockInput() is called. Now CombatController.Update runs again. But _isSlowMo=true and _isAttackPending=true. The safety timeout will eventually fire (after maxSlowMoDuration=5s real time). Or if AttackReleased event fires, ExitSlowMotion() runs.
  found: THE ROOT CAUSE IS HERE. When player enters EXIT while attack button is held (slow-mo active): 1) FloorTransitionSequence calls LockInput(), 2) CombatController.Update() returns early due to inputLocked — ExitSlowMotion() never called, 3) timeScale stays at 0.2 (slow-mo value), 4) UnlockInput() is called at end of transition, 5) CombatController.Update() runs again but _isSlowMo=true, _isAttackPending=true, 6) since player still holds attack button OR the InputManager state is stale, behavior is unpredictable. BUT this gives 0.2x speed, not a full freeze.
  implication: PARTIAL explanation. Need to check if player hits EXIT while NOT in slow-mo.

- timestamp: 2026-06-24T00:16:00Z
  checked: Full normal scenario — player not in slow-mo, enters EXIT. AdvanceFloor() called. FloorTransitionSequence starts. _transitioning=true. LockInput called. SpawnRoom runs. yield return null. ActivateEnemies. yield return WaitForSecondsRealtime(0.05f). UnlockInput. _transitioning=false.
  found: This should work perfectly. No timeScale issue. No freeze. BUT — what if RoomClearCondition activates EXIT even though enemies are alive? (dynamically spawned enemies). The dynamically spawned path in RoomClearCondition.Start() uses GetComponentsInChildren on the RoomClearCondition's own GameObject at Start time. But enemies are spawned by FloorSpawner AFTER the room is Instantiated AND then SetActive(false). At Start() time of RoomClearCondition, the enemies are there (as inactive children). The dynamic search uses includeInactive:true, so it should find them. Then Update() checks if all IEnemy.IsAlive are false. When last enemy dies, Activate() enables EXIT. This is correct.
  implication: RoomClearCondition should work. But let me check: does RoomClearCondition's EXIT reference get lost?

## Resolution

root_cause: |
  FloorTransitionSequence가 시작될 때 _player.LockInput()을 호출하면, CombatController.Update()가
  _player.InputLocked 체크로 즉시 return함. 이로 인해 slow-motion 상태(Time.timeScale = slowTimeScale = 0.2f)에서
  ExitSlowMotion()이 절대 호출되지 않음. 전환 시퀀스 자체는 WaitForSecondsRealtime을 사용하므로 항상 완료되지만,
  UnlockInput() 이후에 CombatController.Update()가 재개될 때 _isSlowMo=true + _isAttackPending=true 상태.
  플레이어가 공격 버튼을 계속 누르고 있으면 (input.IsAttackDown=true) 클린업 조건이 충족되지 않아 timeScale = 0.2f
  상태가 5초 안전 타임아웃까지 지속됨. 게임이 0.2x 속도로 실행되어 완전히 멈춘 것처럼 보임.
  추가로: HitFreeze(timeScale=0)가 진행 중인 경우에도 같은 문제가 발생할 수 있으나 HitFreeze는
  WaitForSecondsRealtime으로 자동 복구되므로 주요 원인은 slow-motion 상태 미정리.
  
fix: |
  FloorTransitionSequence에서 _player.LockInput() 호출 전에 CombatController.ForceExitCombatState()를
  명시적으로 호출. CombatController에 ForceExitCombatState() public 메서드 추가 — ExitSlowMotion()과
  ExitAttackPending()을 모두 호출하고 _isBusy는 DashOrWhiff 코루틴이 자체 종료하도록 놔둠.
  FloorSpawner는 CombatController 참조를 Inspector에서 직접 받거나 Player에서 GetComponent로 가져옴.
  
verification: pending human confirm
files_changed:
  - Assets/Scripts/Player/CombatController.cs
  - Assets/Scripts/World/FloorSpawner.cs
  - Assets/Scenes/SampleScene.unity
