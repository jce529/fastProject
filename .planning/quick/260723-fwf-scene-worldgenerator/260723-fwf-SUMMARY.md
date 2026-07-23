# Quick Task 260723-fwf — Summary

**Task:** 디버그 전용 Scene 신설 (WorldGenerator 흐름과 분리, 고정좌표 FioraBoss 디버깅)
**Status:** Complete — verified end-to-end via Unity MCP (RunCommand + Console + Play mode)

## What was built

- `Assets/Editor/DebugSceneBuilder.cs` — 메뉴 `Fast/Debug/Build DebugScene`. `Room_BossFsmTest.prefab`(바닥+RoomEntry)을 원점에 배치하고, `BossEnemy.prefab`(FioraBoss)을 RoomEntry 기준 우측 6유닛에 별도 Instantiate(D-11 규칙 재사용), SampleScene에서 Player를 읽기 전용으로 복제, 고정 카메라 + 우측 하단 "메인 씬으로" 버튼을 절차적으로 생성해 `Assets/Scenes/DebugScene.unity`로 저장하고 Build Settings에 등록(멱등적).
- `Assets/Scripts/Debug/ReturnToMainSceneButton.cs` — `SceneManager.LoadScene("SampleScene")` 한 줄.
- `Assets/Scripts/World/GameBootstrapper.cs` (기존 파일 수정) — "어느 씬에서 Play해도 항상 MainMenu로" 강제하던 로직에 `DebugScene` 예외 추가. 이 프로젝트에 이미 존재하던 전역 부트스트랩 메커니즘을 활용해, 원래 계획했던 "폴백(버튼만)" 대신 사용자가 원한 "이상적인" 동작(디버그 씬 Play → 디버그 씬 격리 실행 / 그 외 씬 Play → 기존처럼 MainMenu로)을 실제로 구현.
- `ProjectSettings/EditorBuildSettings.asset` — DebugScene이 MainMenu/AttackSelect/SampleScene 뒤 4번째 항목으로 추가.

## Plan deviations

1. **Boss 스폰 방식 정정 (Rule 2 - 계획 가정 오류):** 계획 문서는 `Room_BossFsmTest.prefab`에 FioraBoss가 nested되어 있다고 가정했으나, 실제로는 `DebugRoomTeleporter.TeleportToRoom()`이 런타임에 `BossEnemy.prefab`을 별도 Instantiate하는 구조였다(prefab 자체엔 없음). `DebugSceneBuilder.cs`에 동일한 D-11 스폰 규칙(RoomEntry 우측 6유닛)으로 별도 Instantiate 로직 추가.
2. **GameBootstrapper.cs 발견 및 수정 (범위 확장, 사용자 승인된 "이상적 옵션" 달성):** Unity MCP로 Play 모드를 직접 재현하던 중 DebugScene에서 Play해도 MainMenu로 강제 이동되는 것을 발견 — 프로젝트에 기존부터 있던 `[RuntimeInitializeOnLoadMethod]` 기반 전역 부트스트랩(`GameBootstrapper.cs`)이 원인이었다. DebugScene을 예외 처리해 원래 계획서에서 "구현 난이도가 높아 채택하지 않는다"고 명시했던 옵션 3(씬별 Play 격리)을 한 줄 수정으로 실제 구현. 옵션 4(버튼)는 보험으로 그대로 유지.

## Verification (Unity MCP 직접 실행 — 사람 확인 절차를 프로그램적으로 대체)

Task 2(checkpoint:human-action)의 메뉴 실행/Play 모드 테스트를 Unity MCP(`Unity_RunCommand`, `Unity_GetConsoleLogs`)로 직접 재현:

- `Fast/Debug/Build DebugScene` 실행 → 에러 없이 `DebugScene.unity` 생성 확인
- Hierarchy 검사 → Player, Room_BossFsmTest(+FioraBoss nested-instantiated), Main Camera, Canvas, EventSystem 존재, **WorldGenerator 없음** 확인
- Play 모드 진입 → Active Scene이 `DebugScene`으로 유지됨 확인 (GameBootstrapper 수정 전에는 `MainMenu`로 강제 전환되는 회귀를 먼저 재현해서 근본 원인 파악)
- `ReturnToMainSceneButton.ReturnToMain()` 직접 호출 → Active Scene이 `SampleScene`으로 전환 확인
- Play 모드 종료 후 `git status` → `SampleScene.unity` 변경 없음 확인
- `ProjectSettings/EditorBuildSettings.asset` → DebugScene이 4번째 항목으로 등록됨 확인
- Console 로그 전체 확인 → 관련 없는 MCP WebSocket 에러 외 컴파일/런타임 에러 없음

**자동화 불가 항목:** 공격 버튼 슬로우모션/대시 "손맛"은 입력 시뮬레이션 없이는 판단 불가 — 사용자가 직접 1회 플레이 확인 권장(기능적으로는 로직 무변경이라 회귀 없을 것으로 예상).

## Commit

Task 1 (코드 작성): `6ccaa34` — worktree에서 커밋 후 main에 fast-forward merge.
이 SUMMARY와 함께 최종 커밋에 DebugSceneBuilder.cs 수정분, GameBootstrapper.cs 수정, DebugScene.unity 산출물, EditorBuildSettings.asset, STATE.md 갱신 포함.
