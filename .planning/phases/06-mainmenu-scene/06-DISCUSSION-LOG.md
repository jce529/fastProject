# Phase 6: MainMenu Scene - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-23
**Phase:** 06-mainmenu-scene
**Areas discussed:** Start 버튼 목적지, DeathScreen 텍스트, MainMenu.unity 재빌드

---

## Start 버튼 목적지 (MENU-02)

| Option | Description | Selected |
|--------|-------------|----------|
| LoadScene("AttackSelect") (기능 없음) | 코드를 정확하게 수정 (MENU-02 충족). AttackSelect 씬 생성 전에는 에러 발생. Phase 7에서 씬만 만들면 자동 동작. | ✓ |
| Placeholder AttackSelect.unity 생성 | Phase 6에서 빈 씬에 'Go to SampleScene' 버튼만 있는 최소한 씬 제작. 단대단 동작 보장 but Phase 7에서 전체 재작성. | |
| SampleScene 유지 (Phase 7에서 변경) | Phase 6 코드 수정 없음. Start 누르면 바로 SampleScene 로드. Phase 7에서 AttackSelect 변경 시 원라이너 수정. | |

**User's choice:** LoadScene("AttackSelect") (기능 없음)
**Notes:** 코드 정확성 우선. Phase 7 씬 생성 후 자동으로 동작하게 됨.

---

## DeathScreen 텍스트

| Option | Description | Selected |
|--------|-------------|----------|
| Phase 7에 맡기기 | Phase 7에서 어차피 '다시 선택'으로 교체함. Phase 6에서 수정 불필요. | ✓ |
| '메인 메뉴'로 수정 (Phase 6에서) | quick task t6i 의도대로 '메인 메뉴' 적용. 하지만 Phase 7에서 바로 교체될 임시 작업. | |

**User's choice:** Phase 7에 맡기기
**Notes:** 현재 "MAIN" 상태이나 Phase 7에서 "다시 선택"으로 변경 예정이므로 중간 수정 불필요.

---

## MainMenu.unity 재빌드

| Option | Description | Selected |
|--------|-------------|----------|
| 코드만 수정 (재빌드 불필요) | OnStartClicked() 코드 변경 시 씬 내부 버튼 onClick 메서드 참조가 자동으로 반영됨. | |
| EditorScript 다시 실행 | 씬을 완전히 재생성. 기존 MainMenu.unity를 다시 덮어씀. | ✓ |

**User's choice:** EditorScript 다시 실행
**Notes:** 코드 수정 후 EditorScript 재실행으로 씬 파일을 최신 상태로 갱신.

---

## Claude's Discretion

- Build Settings Phase 6 유지 (MainMenu=0, SampleScene=1): Phase 7에서 AttackSelect 추가 시 재정렬
- OnQuitClicked 수정 없음: 이미 올바르게 구현됨
- EditorScript 재실행 순서 결정: (1) 코드 수정 → (2) 컴파일 완료 → (3) 씬 재생성

## Deferred Ideas

- DeathScreen "다시 선택" 텍스트 — Phase 7
- Build Settings 3-씬 순서 재정렬 — Phase 7
- AttackSelect.unity 씬 구현 — Phase 7
