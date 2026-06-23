# Phase 7: AttackSelect Scene & Scene Flow - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-23
**Phase:** 07-attackselect-scene-scene-flow
**Areas discussed:** 씬 간 데이터 전달, AttackTypeSelector 처리, AttackSelect 씬 UI 스타일, AttackSelect 씬 생성 방식

---

## 씬 간 데이터 전달

| Option | Description | Selected |
|--------|-------------|----------|
| static 재사용 | AttackTypeSelector.SetType() 호출 후 씬 로드. 코드 추가 없음. | ✓ |
| PlayerPrefs | 디스크 저장, 앱 재시작 후에도 유지. 프로토타입에 과도. | |
| 새 GameManager 싱글턴 | GameManager.cs 신규 생성. CombatController/HUDController 마이그레이션 필요. | |

**User's choice:** static 재사용
**Notes:** AttackTypeSelector.Selected가 이미 static으로 씬 전환 후에도 유지됨을 확인. CombatController, HUDController 모두 코드 변경 불필요.

---

## AttackTypeSelector 처리

| Option | Description | Selected |
|--------|-------------|----------|
| GameObject 씬에서 삭제 | Canvas GameObject 제거. static Selected는 인스턴스 불필요. | ✓ |
| Canvas 비활성화 | 눈에 안 보이는 오버레이로 유지. | |
| Awake에서 자동 비활성화 | AttackTypeSelector.Awake()에 SetActive(false) 추가. | |

**User's choice:** GameObject 삭제
**Notes:** ATKS-03 요건 충족.

| Option | Description | Selected |
|--------|-------------|----------|
| AttackTypeZone 제거 | 게임 중 타입 전환 불필요 — Phase 7 이후 고정 방식으로 전환. | ✓ |
| AttackTypeZone 유지 | 일부 Room 프리팹에 남겨둠. 스코프 외 기능. | |

**User's choice:** 제거
**Notes:** Phase 7 이후 공격 타입은 씬 시작 전에 결정됨.

---

## AttackSelect 씬 UI 스타일

| Option | Description | Selected |
|--------|-------------|----------|
| 버튼 2개만 | MainMenu와 동일 스타일, LINEAR / FAN 버튼만. | ✓ |
| 타이틀 + 버튼 2개 | 짧은 커지말 + 버튼. | |
| 범위 형태 다이어그램 포함 | 형태 그림 + 버튼. | |

**User's choice:** 버튼 2개만

| Option | Description | Selected |
|--------|-------------|----------|
| 동일한 배경 | MainMenu와 동일한 단색 어두운 배경. | ✓ |
| 다른 배경 | AttackSelect 전용 비주얼. | |

**User's choice:** 동일한 배경

---

## AttackSelect 씬 생성 방식

| Option | Description | Selected |
|--------|-------------|----------|
| EditorScript | AttackSelectSceneBuilder.cs 신규 생성, 메뉴로 씬 자동 생성. | |
| Unity Editor 수동 제작 | Unity Editor에서 직접 Canvas/Button 배치. | ✓ |

**User's choice:** Unity Editor 수동 제작
**Notes:** 한 번만 만들면 되는 씬이라 EditorScript 오버헤드 불필요.

---

## Claude's Discretion

- AttackSelectController.cs 구조 (MainMenuController 패턴 그대로)
- AttackSelect 씬 EventSystem 설정 (InputSystemUIInputModule)
- 사망 후 AttackSelect 복귀 시 이전 선택값 유지 여부 (기본 Linear 고정)

## Deferred Ideas

- 범위 형태 다이어그램 — 플레이테스트 후 필요 시 추가
- AttackSelectSceneBuilder EditorScript — 씬 구조 변경 빈번 시 고려
- 게임 중 AttackTypeZone 트리거 — Phase 7에서 제거, 향후 재고
