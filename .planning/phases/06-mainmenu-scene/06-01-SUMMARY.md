---
plan: 06-01
phase: 06
status: complete
completed: 2026-06-23
commits:
  - 06d0efe  # feat(06-01): OnStartClicked → SceneManager.LoadScene("AttackSelect")
  - 432e56b  # fix(06-01): EventSystem에 InputSystemUIInputModule 사용
  - 2d32404  # fix(06-01): AddPersistentListener로 버튼 onClick 직렬화 수정
---

## Summary

**MainMenuController.OnStartClicked() 씬 대상을 "AttackSelect"로 교체하고, MainMenuSceneBuilder EditorScript 버그 2종을 수정해 MainMenu.unity를 재빌드했다.**

### What was built

- `MainMenuController.OnStartClicked()` → `SceneManager.LoadScene("AttackSelect")`
- `MainMenuSceneBuilder`: `StandaloneInputModule` → `InputSystemUIInputModule` (New Input System 호환)
- `MainMenuSceneBuilder`: `AddListener()` → `UnityEventTools.AddPersistentListener()` (씬 직렬화 수정)
- `MainMenu.unity` 재생성 완료 (Build Settings: MainMenu=0, SampleScene=1)

### Deviations

두 개의 추가 버그가 Task 2 checkpoint 중 발견·수정됨:
1. **EventSystem 에러** — EditorScript가 `StandaloneInputModule`을 추가하고 있었음. New Input System 전용 프로젝트이므로 `InputSystemUIInputModule`로 교체.
2. **버튼 onClick 미작동** — `AddListener()`는 런타임 리스너라 씬 저장 시 유실됨. `UnityEventTools.AddPersistentListener()`로 교체.

### Human Verification Result

- ✓ 에디터 Play → MainMenu 씬이 첫 화면
- ✓ Start 버튼 → "AttackSelect couldn't be loaded" 에러 (코드 정확성 확인)
- ✓ Quit 버튼 → Play 모드 종료
- ✓ 컴파일 에러 없음
