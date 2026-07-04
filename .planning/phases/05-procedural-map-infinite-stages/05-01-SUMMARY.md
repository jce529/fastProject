---
plan: 05-01
phase: 05-procedural-map-infinite-stages
status: complete
completed: 2026-06-19
---

## Summary

FloorSpawner MonoBehaviour, RoomExit 트리거, PlayerController 입력 잠금 API를 구현하여 6단계 층 전환 시퀀스의 C# 코어를 완성했다.

## What Was Built

- **PlayerController.cs** — `_inputLocked` 필드, `LockInput()` / `UnlockInput()` / `InputLocked` API 추가. FixedUpdate와 OnJumpPerformed에 잠금 가드 적용.
- **CombatController.cs** — `[SerializeField] private PlayerController _player;` 필드 추가. `Update()` 첫 줄에 `_player.InputLocked` 가드 추가 — 층 전환 시퀀스 중 슬로우모션 진입 차단.
- **FloorSpawner.cs** (신규) — 씬 싱글톤 MonoBehaviour. `AdvanceFloor()` → `FloorTransitionSequence()` 6단계 코루틴. `SpawnRoom()` D-07 난이도 테이블 (1층 적 없음, 2-5층 근접 위주, 6-10층 혼합, 11층+ 원거리 확대). `WaitForSecondsRealtime` 전용 사용 (timeScale=0 안전). `_transitioning` 이중 발동 가드.
- **RoomExit.cs** (신규) — `[RequireComponent(typeof(Collider2D))]`. `OnTriggerEnter2D`에서 `CompareTag("Player")` 확인 후 `FloorSpawner.Instance?.AdvanceFloor()` 호출.
- **TagManager.asset** — `EnemySpawnPoint` 태그 추가.

## Key Decisions

- `WaitForSecondsRealtime` 전용 사용: CombatController의 HitFreeze가 `timeScale=0`으로 설정할 수 있으므로 `WaitForSeconds` 사용 시 층 전환이 영구 중단됨 (Pitfall 3).
- `GetComponentsInChildren<MonoBehaviour>(true)` 경유로 IEnemy 비활성 적 탐색: `SetActive(false)` 상태 오브젝트는 `includeInactive: true` 없이는 반환되지 않음 (Pitfall 2).
- `_transitioning = false`를 FloorTransitionSequence() 마지막 줄에 배치: 없으면 첫 전환 후 FloorSpawner가 영구 잠금.

## Verification

- Unity Editor 컴파일 에러 없음 (human-verified ✓)
- `_inputLocked` 필드, `LockInput/UnlockInput/InputLocked` API 존재 ✓
- `WaitForSecondsRealtime` 사용 ✓
- `GetComponentsInChildren<MonoBehaviour>(true)` 호출 ✓
- `enemy.SetActive(false)` SpawnRoom 내부 존재 ✓
- `_transitioning = false` FloorTransitionSequence 마지막 줄 존재 ✓
