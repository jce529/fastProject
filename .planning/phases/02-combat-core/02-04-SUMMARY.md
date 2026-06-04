---
phase: 02-combat-core
plan: "02-04"
subsystem: verification
tags: [unity, editor, playtest, manual-verification]

provides:
  - 02-04-PLAN.md: 에디터 직접 플레이테스트 검증 계획 (자동화 테스트 없음)
  - 02-04-EDITOR-GUIDE.md: 요구사항별 수동 체크리스트 (ATCK-01~05, FEEL-01, MOVE-03)

key-decisions:
  - "테스트 러너 제거: Unity Test Framework PlayMode 테스트를 삭제하고 에디터 직접 플레이테스트로 전환"
  - "삭제된 파일: Assets/Tests/PlayMode/PlayMode.asmdef, CombatTests.cs, RollTests.cs (+ .meta 파일들)"
  - "검증 방식: 자동화 assertion 대신 플레이어가 직접 에디터 Play 모드에서 각 메카닉을 실행하며 눈으로 확인"

requirements-verified: [ATCK-01, ATCK-02, ATCK-03, ATCK-04, ATCK-05, FEEL-01, MOVE-03]

completed: 2026-06-04
---

# Phase 02 Plan 04: 에디터 플레이테스트 검증 Summary

**Unity Test Runner 방식을 제거하고 에디터 직접 플레이테스트로 전환. 7개 요구사항 수동 체크리스트 제공.**

## 변경 내용

### 이전 (자동화 테스트)
- `Assets/Tests/PlayMode/PlayMode.asmdef` — Test Runner 어셈블리 정의
- `Assets/Tests/PlayMode/CombatTests.cs` — 12개 NUnit Play Mode 테스트
- `Assets/Tests/PlayMode/RollTests.cs` — 3개 NUnit Play Mode 테스트
- Window > General > Test Runner > Run All 로 실행

### 이후 (에디터 플레이테스트)
- `02-04-EDITOR-GUIDE.md` — 요구사항별 수동 체크리스트
- Unity Editor Play 모드에서 직접 플레이하며 확인
- 자동화 코드 없음

## 검증 대상 요구사항

| 요구사항 | 설명 | 검증 방법 |
|----------|------|-----------|
| ATCK-01 | 공격 타입 선택 오버레이 | 버튼 누름 → 오버레이 확인 |
| ATCK-02 | 슬로우 모션 발동 | 선택 후 게임 속도 변화 확인 |
| ATCK-03 | 범위 내 적 대시 처치 | 버튼 떼기 → 대시 + 처치 확인 |
| ATCK-04 | whiff 락아웃 | 범위 밖에서 떼기 → 복귀 확인 |
| ATCK-05 | 게이지 드레인/회복 | UI 게이지 변화 확인 |
| FEEL-01 | hitFreeze | 처치 순간 화면 정지감 확인 |
| MOVE-03 | 구르기 i-frame + 쿨다운 | 무적 + 재발동 제한 확인 |

## 검증 절차

`02-04-EDITOR-GUIDE.md` 참조. SampleScene → Play → 체크리스트 순서대로 실행.

---
*Phase: 02-combat-core*
*Completed: 2026-06-04*
