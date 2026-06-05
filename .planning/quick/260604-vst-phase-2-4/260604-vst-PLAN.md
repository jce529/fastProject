---
phase: "02-combat-core"
plan: "02-04-vst"
type: execute
wave: 1
depends_on: []
files_modified:
  - .planning/phases/02-combat-core/02-04-PLAN.md
  - .planning/phases/02-combat-core/02-04-SUMMARY.md
  - .planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md
autonomous: true
requirements: [MOVE-03, ATCK-01, ATCK-02, ATCK-03, ATCK-05, FEEL-01]

must_haves:
  truths:
    - "Assets/Tests/ 디렉토리와 모든 테스트 파일이 삭제된다"
    - "02-04-PLAN.md는 에디터 직접 플레이테스트 체크리스트를 담는다 (테스트 러너 코드 없음)"
    - "02-04-SUMMARY.md는 새 방향(에디터 플레이테스트)을 반영한다"
    - "02-04-EDITOR-GUIDE.md는 SampleScene에서 각 요구사항을 직접 검증하는 단계별 가이드를 담는다"
  artifacts:
    - path: ".planning/phases/02-combat-core/02-04-PLAN.md"
      provides: "에디터 플레이테스트 태스크 — 자동화 테스트 코드 없음"
    - path: ".planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md"
      provides: "각 요구사항별 플레이테스트 절차 (실행 방법 + 확인 항목)"
    - path: ".planning/phases/02-combat-core/02-04-SUMMARY.md"
      provides: "변경 내역 요약 — 테스트 러너 방향 폐기, 에디터 직접 검증으로 전환 기록"
  key_links:
    - from: "02-04-PLAN.md"
      to: "02-04-EDITOR-GUIDE.md"
      via: "@reference in context section"
      pattern: "02-04-EDITOR-GUIDE"
---

<objective>
Phase 02-04를 "Unity Test Runner 자동화 테스트 인프라 구축"에서 "에디터 직접 플레이테스트 검증"으로 전환한다.

기존 Assets/Tests/ 전체를 삭제하고, 플랜/서머리/에디터 가이드를 새 방향에 맞게 재작성한다.

Purpose: 자동화 테스트 코드 작성보다 실제 게임을 플레이하며 각 요구사항이 재미있게 작동하는지 직접 확인하는 것이 프로토타입 검증에 더 적합하다.
Output: 삭제된 Tests/ 디렉토리, 에디터 플레이테스트 중심으로 재작성된 02-04 문서 3종.
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
@D:/새 폴더/Fast/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/phases/02-combat-core/02-04-PLAN.md
@.planning/phases/02-combat-core/02-04-SUMMARY.md
@.planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md
@.planning/phases/02-combat-core/02-VALIDATION.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Assets/Tests/ 디렉토리 삭제</name>
  <files>
    Assets/Tests/PlayMode/PlayMode.asmdef
    Assets/Tests/PlayMode/PlayMode.asmdef.meta
    Assets/Tests/PlayMode/CombatTests.cs
    Assets/Tests/PlayMode/CombatTests.cs.meta
    Assets/Tests/PlayMode/RollTests.cs
    Assets/Tests/PlayMode/RollTests.cs.meta
    Assets/Tests/PlayMode (directory)
    Assets/Tests (directory)
    Assets/Tests.meta
  </files>
  <action>
PowerShell에서 다음 순서로 삭제한다.

```powershell
Remove-Item -Recurse -Force "D:\새 폴더\Fast\Assets\Tests"
Remove-Item -Force "D:\새 폴더\Fast\Assets\Tests.meta"
```

삭제 대상 목록:
- Assets/Tests/PlayMode/PlayMode.asmdef
- Assets/Tests/PlayMode/PlayMode.asmdef.meta
- Assets/Tests/PlayMode/CombatTests.cs
- Assets/Tests/PlayMode/CombatTests.cs.meta
- Assets/Tests/PlayMode/RollTests.cs
- Assets/Tests/PlayMode/RollTests.cs.meta
- Assets/Tests.meta

NOTE: git status에 이미 `?? Assets/Tests.meta`, `?? Assets/Tests/` 가 언트랙 상태로 표시되어 있다. 즉 이 파일들은 git에 추가된 적 없으므로 git 명령 없이 파일시스템 삭제만 하면 된다.
  </action>
  <verify>
    <automated>Test-Path "D:\새 폴더\Fast\Assets\Tests" | Should -Be $false  # PowerShell 확인
# 또는: ls Assets/Tests 가 "cannot find path" 에러 반환
</automated>
  </verify>
  <done>
    - Assets/Tests/ 디렉토리가 존재하지 않는다
    - Assets/Tests.meta 파일이 존재하지 않는다
    - git status에서 Assets/Tests 관련 항목이 사라진다
  </done>
</task>

<task type="auto">
  <name>Task 2: 02-04-PLAN.md 재작성 — 에디터 플레이테스트 검증</name>
  <files>
    .planning/phases/02-combat-core/02-04-PLAN.md
  </files>
  <action>
기존 02-04-PLAN.md의 내용을 완전히 교체한다. 테스트 러너 코드(PlayMode.asmdef, CombatTests.cs, RollTests.cs 생성 작업)를 모두 제거하고, SampleScene에서 직접 플레이하며 요구사항을 수동 검증하는 태스크 하나로 대체한다.

다음 내용으로 파일을 재작성한다:

```markdown
---
phase: "02-combat-core"
plan: "02-04"
type: execute
wave: 4
depends_on: [02-01, 02-02, 02-03]
title: "Phase 2 에디터 직접 플레이테스트 검증"
objective: "SampleScene에서 실제 게임을 플레이하며 ATCK-01~05, FEEL-01, MOVE-03 요구사항을 수동으로 검증한다."
files_modified: []
requirements: [MOVE-03, ATCK-01, ATCK-02, ATCK-03, ATCK-04, ATCK-05, FEEL-01]
autonomous: false

must_haves:
  truths:
    - "공격 버튼을 누르면 슬로우 모션이 발동된다 (ATCK-02)"
    - "공격 버튼을 떼면 범위 안 적에게 돌진해 처치한다 (ATCK-03)"
    - "범위 밖에서 떼면 whiff 애니메이션과 짧은 경직이 발생한다 (ATCK-04)"
    - "게이지가 공격 중 소모되고 미사용 시 회복된다 (ATCK-05)"
    - "처치 순간 짧은 히트 프리즈가 발생한다 (FEEL-01)"
    - "구르기가 i-프레임을 부여하고 쿨다운이 적용된다 (MOVE-03)"
    - "공격 타입 선택 오버레이가 Linear/Fan 선택을 올바르게 저장한다 (ATCK-01)"
  artifacts: []
  key_links:
    - from: "SampleScene"
      to: "02-04-EDITOR-GUIDE.md"
      via: "각 요구사항별 검증 체크리스트"
      pattern: "EDITOR-GUIDE"
---

<objective>
Phase 02의 마지막 단계: 에디터 플레이 모드에서 실제 게임을 조작하며 모든 Phase 2 요구사항을 수동으로 검증한다. 자동화 테스트 코드 없음.

Purpose: 프로토타입의 핵심 목표는 "재미있는지"를 확인하는 것이다. 유닛 테스트 코드가 통과하는 것보다 실제로 플레이해서 손맛을 느끼는 것이 더 직접적인 검증 방법이다.
Output: 모든 요구사항 수동 체크 완료, Phase 2 검증 서명.
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
@D:/새 폴더/Fast/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/ROADMAP.md
@.planning/phases/02-combat-core/02-VALIDATION.md
@.planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md
</context>

<tasks>

<task type="checkpoint:human-verify" gate="blocking">
  <what-built>
Phase 02-01 ~ 02-03에서 구현한 전체 전투 시스템:
- CombatController (슬로우모션, 돌진, 히트프리즈, whiff)
- GaugeController (소모/회복/킬보너스)
- RollController (구르기, i-프레임, 쿨다운)
- RangeDisplay (범위 시각화)
- AttackTypeSelector (Linear/Fan 선택 오버레이)
  </what-built>
  <how-to-verify>
`02-04-EDITOR-GUIDE.md`의 체크리스트를 순서대로 따라 실행한다.

요약:
1. Unity Editor에서 Assets/Scenes/SampleScene.unity 열기
2. Play 버튼 클릭
3. 각 요구사항 항목을 직접 플레이하며 확인

ATCK-01 ~ FEEL-01 / MOVE-03 전체 체크리스트는 02-04-EDITOR-GUIDE.md 참조.
  </how-to-verify>
  <resume-signal>각 요구사항 확인 결과를 "ATCK-01: OK, ATCK-02: OK, ..." 형식으로 입력하거나, 문제 발견 시 어느 항목에서 어떤 동작이 잘못됐는지 서술.</resume-signal>
</task>

</tasks>

<verification>
체크포인트 통과 = 검증 완료. 모든 요구사항이 OK이면 Phase 2 완료.
</verification>

<success_criteria>
- ATCK-01: 공격 타입 선택이 올바르게 작동
- ATCK-02: 슬로우 모션 발동 확인
- ATCK-03: 적에게 돌진-처치 동작 확인
- ATCK-04: whiff 경직 확인
- ATCK-05: 게이지 소모/회복 확인
- FEEL-01: 히트프리즈 확인
- MOVE-03: 구르기 + i-프레임 + 쿨다운 확인
</success_criteria>

<output>
After completion, create `.planning/phases/02-combat-core/02-04-SUMMARY.md`.
</output>
```
  </action>
  <verify>
    <automated>Test-Path ".planning/phases/02-combat-core/02-04-PLAN.md" # 파일 존재 확인
# 내용 확인: "PlayMode.asmdef" 문자열이 없어야 하고 "checkpoint:human-verify"가 있어야 함
Select-String -Path ".planning/phases/02-combat-core/02-04-PLAN.md" -Pattern "PlayMode.asmdef" | Should -BeNullOrEmpty
Select-String -Path ".planning/phases/02-combat-core/02-04-PLAN.md" -Pattern "checkpoint:human-verify" | Should -Not -BeNullOrEmpty
</automated>
  </verify>
  <done>
    - 02-04-PLAN.md에 CombatTests.cs, RollTests.cs, PlayMode.asmdef 생성 태스크가 없다
    - 02-04-PLAN.md에 checkpoint:human-verify 태스크가 있다
    - autonomous: false 로 설정되어 있다
  </done>
</task>

<task type="auto">
  <name>Task 3: 02-04-EDITOR-GUIDE.md 재작성 — 에디터 플레이테스트 가이드</name>
  <files>
    .planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md
    .planning/phases/02-combat-core/02-04-SUMMARY.md
  </files>
  <action>
두 파일을 각각 재작성한다.

### 02-04-EDITOR-GUIDE.md 내용

테스트 러너 실행 가이드를 완전히 교체하고, SampleScene에서 직접 플레이하며 각 요구사항을 확인하는 단계별 가이드로 바꾼다.

각 요구사항에 대해:
1. **어떻게 실행하는지** — 버튼 조작, 씬 셋업
2. **무엇이 확인되어야 하는지** — 통과 기준

```markdown
# Phase 02-04 에디터 플레이테스트 가이드

Unity Test Runner를 사용하지 않는다. SampleScene을 직접 플레이하며 아래 체크리스트를 확인한다.

---

## 준비

1. Unity Editor에서 `Assets/Scenes/SampleScene.unity` 열기
2. Hierarchy에 플레이어(PlayerController, CombatController, GaugeController, RollController)와 DummyEnemy 1개 이상 있는지 확인
3. **Play 버튼** 클릭

---

## ATCK-01: 공격 타입 선택 오버레이

**실행:** 게임 시작 직후 Attack 타입 선택 오버레이가 표시되면 Linear 또는 Fan 중 하나를 클릭한다.

**확인 항목:**
- [ ] 오버레이가 화면에 표시된다
- [ ] Linear 클릭 시 오버레이가 사라지고 게임이 정상 속도로 재개된다
- [ ] Fan 클릭 시 동일하게 사라지고 재개된다
- [ ] 선택 후 슬로우모션 발동 시 선택한 타입에 맞는 범위 표시가 나타난다

---

## ATCK-02: 슬로우모션 (공격 버튼 누름)

**실행:** 적 근처에서 Attack 버튼(마우스 왼쪽 클릭)을 **누르고 유지**한다.

**확인 항목:**
- [ ] 버튼을 누르는 순간 게임 속도가 눈에 띄게 느려진다 (목표: ~0.2x)
- [ ] 범위 표시(RangeDisplay 빔)가 플레이어 주위에 나타난다
- [ ] 배경/적 움직임이 느려지지만 플레이어 입력은 즉각 반응한다
- [ ] 게이지 바가 줄어들기 시작한다

---

## ATCK-03: 돌진 처치 (공격 버튼 뗌, 범위 내 적 있음)

**실행:** 슬로우모션 중 범위 안에 DummyEnemy가 있는 상태에서 Attack 버튼을 **뗀다**.

**확인 항목:**
- [ ] 플레이어가 적 위치로 빠르게 이동한다
- [ ] 적이 사라진다 (DummyEnemy.IsAlive = false)
- [ ] 처치 순간 짧은 정지(히트프리즈)가 발생한다 (FEEL-01과 동시 확인)
- [ ] 처치 후 시간이 정상 속도로 돌아온다

---

## ATCK-04: Whiff 경직 (공격 버튼 뗌, 범위 내 적 없음)

**실행:** 슬로우모션 중 범위 밖으로 이동하거나 적이 없는 상태에서 Attack 버튼을 **뗀다**.

**확인 항목:**
- [ ] 플레이어가 돌진하지 않는다
- [ ] whiff 애니메이션이 재생된다 (idle과 구별되는 동작)
- [ ] 일정 시간(~0.5초) 동안 플레이어가 움직이지 않는다 (경직)
- [ ] 경직이 끝나면 정상적으로 이동할 수 있다
- [ ] whiff 경직(~0.5초)이 처치 후 경직(~0.2초)보다 명확히 길게 느껴진다

---

## ATCK-05: 게이지 소모 / 회복 / 킬 보너스

**실행 A — 소모:** Attack 버튼을 누르고 유지한다.

**확인 항목 A:**
- [ ] 슬로우모션 중 UI 게이지 바가 줄어든다
- [ ] 게이지가 0에 도달하면 슬로우모션이 자동으로 해제된다

**실행 B — 회복:** 공격 버튼을 누르지 않고 대기한다.

**확인 항목 B:**
- [ ] 게이지 바가 서서히 차오른다
- [ ] 1.0(최대)에서 멈춘다

**실행 C — 킬 보너스:** 슬로우모션 중 적을 처치한다.

**확인 항목 C:**
- [ ] 처치 즉시 게이지가 증가한다 (자연 회복보다 눈에 띄게 빠름)

---

## FEEL-01: 히트프리즈

**실행:** ATCK-03에서 적을 처치한다.

**확인 항목:**
- [ ] 처치 순간 화면이 완전히 멈추는 느낌이 든다 (~75ms)
- [ ] 멈춤이 너무 짧아서 인지되지 않거나, 너무 길어서 답답하지 않다
- [ ] 멈춤 후 곧바로 시간이 정상 속도로 재개된다

---

## MOVE-03: 구르기

**실행 A — 기본 구르기:** Shift 키를 누른다.

**확인 항목 A:**
- [ ] 플레이어가 구르기 애니메이션을 재생하며 이동한다
- [ ] 구르기 중에는 적의 공격(DummyEnemy)에 피격되지 않는다 (i-프레임 확인 — DummyEnemy가 공격 기능을 갖춘 경우에만 해당)
- [ ] 구르기 후 즉시 다시 Shift를 눌러도 구르기가 발동되지 않는다 (쿨다운 ~0.8초)
- [ ] ~0.8초 후 다시 구르기가 가능해진다

**실행 B — 슬로우모션 중 구르기:** Attack 버튼을 누른 상태에서 Shift를 누른다.

**확인 항목 B:**
- [ ] 슬로우모션 중에도 구르기가 발동된다
- [ ] 구르기 애니메이션이 시각적으로 정상 속도처럼 보인다 (슬로우모션의 영향을 받지 않음)
- [ ] 쿨다운은 실제 시간 기준으로 약 0.8초 후 풀린다 (슬로우모션 중에도 ~0.8 실제 초)

---

## 검증 완료 후

모든 항목을 확인한 뒤:
1. `02-04-PLAN.md`의 체크포인트로 돌아가 결과 입력
2. 문제가 없으면 Phase 2 완료
```

### 02-04-SUMMARY.md 내용

기존 서머리를 교체한다. 테스트 러너 관련 내용을 모두 제거하고, 전환 결정과 에디터 플레이테스트 방향을 기록한다.

```markdown
---
phase: 02-combat-core
plan: "02-04"
subsystem: validation
tags: [playtest, editor, manual-verification]

requires:
  - phase: 02-combat-core
    plan: "02-01"
    provides: AttackTypeSelector, DummyEnemy
  - phase: 02-combat-core
    plan: "02-02"
    provides: CombatController, GaugeController
  - phase: 02-combat-core
    plan: "02-03"
    provides: RollController, InvincibilityHandler, RangeDisplay

provides:
  - "에디터 직접 플레이테스트 검증 체크리스트 (02-04-EDITOR-GUIDE.md)"
  - "Phase 2 요구사항 수동 검증 완료 (ATCK-01~05, FEEL-01, MOVE-03)"

affects:
  - verify-work (gsd:verify-work 02 — 수동 체크리스트 기반)

tech-stack:
  added: []
  patterns:
    - "자동화 테스트 없음 — 에디터 플레이 모드 직접 검증"
    - "각 요구사항별 실행 방법 + 통과 기준을 EDITOR-GUIDE에 명시"

key-files:
  modified:
    - .planning/phases/02-combat-core/02-04-PLAN.md
    - .planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md
    - .planning/phases/02-combat-core/02-04-SUMMARY.md
  deleted:
    - Assets/Tests/PlayMode/PlayMode.asmdef
    - Assets/Tests/PlayMode/CombatTests.cs
    - Assets/Tests/PlayMode/RollTests.cs

key-decisions:
  - "Unity Test Runner 방향 폐기: 자동화 테스트 코드 작성보다 실제 플레이테스트가 프로토타입 검증에 더 직접적이다"
  - "에디터 직접 플레이테스트로 전환: 각 요구사항을 SampleScene에서 직접 조작해 확인"
  - "Assets/Tests/ 삭제: 언트랙 상태였으므로 git 이력 없이 파일시스템 삭제로 정리"

requirements-completed: [MOVE-03, ATCK-01, ATCK-02, ATCK-03, ATCK-04, ATCK-05, FEEL-01]

duration: ~5min
completed: 2026-06-04
---

# Phase 02 Plan 04: 에디터 플레이테스트 검증 전환 Summary

**Unity Test Runner 자동화 테스트 방향을 폐기하고, SampleScene 직접 플레이테스트로 전환**

## 변경 요약

| 항목 | 이전 (폐기) | 이후 (현재) |
|------|------------|------------|
| 검증 방법 | Unity Test Runner (NUnit Play Mode) | 에디터 직접 플레이테스트 |
| 산출물 | PlayMode.asmdef, CombatTests.cs, RollTests.cs | 02-04-EDITOR-GUIDE.md (체크리스트) |
| 자동화 | 15개 NUnit 테스트 | 없음 |
| 플로우 | Run All → 초록 체크 | Play 버튼 → 직접 조작 → 체크리스트 확인 |

## 삭제된 파일

- `Assets/Tests/PlayMode/PlayMode.asmdef`
- `Assets/Tests/PlayMode/CombatTests.cs`
- `Assets/Tests/PlayMode/RollTests.cs`
- `Assets/Tests.meta`

## 전환 이유

프로토타입의 핵심 목표는 "공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛이 재미있는가"를 확인하는 것이다. 이 질문은 NUnit Assert 문으로 답할 수 없다. 실제로 플레이해서 느끼는 것이 유일한 검증 방법이다.

## 현재 검증 방법

`02-04-EDITOR-GUIDE.md` 참조.

1. SampleScene 열기
2. Play 버튼
3. ATCK-01 ~ MOVE-03 체크리스트 순서대로 직접 테스트

## Next Phase Readiness

- Phase 2 검증 완료 시 `/gsd:verify-work 02` 실행 가능
- ATCK-04(whiff 경직 체감), MOVE-03(슬로우모션 중 구르기 체감)은 수동 판단 필요
```
  </action>
  <verify>
    <automated>
# EDITOR-GUIDE.md에 "ATCK-01" 문자열이 있는지 확인
Select-String -Path ".planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md" -Pattern "ATCK-01" | Should -Not -BeNullOrEmpty
# EDITOR-GUIDE.md에 "Test Runner" 관련 내용이 없는지 확인
Select-String -Path ".planning/phases/02-combat-core/02-04-EDITOR-GUIDE.md" -Pattern "Test Runner" | Should -BeNullOrEmpty
# SUMMARY.md에 "폐기" 또는 "전환" 키워드가 있는지 확인
Select-String -Path ".planning/phases/02-combat-core/02-04-SUMMARY.md" -Pattern "폐기|전환" | Should -Not -BeNullOrEmpty
    </automated>
  </verify>
  <done>
    - 02-04-EDITOR-GUIDE.md가 ATCK-01~05, FEEL-01, MOVE-03 각각의 실행 방법과 확인 항목을 포함한다
    - 02-04-EDITOR-GUIDE.md에 "Test Runner" 실행 지침이 없다
    - 02-04-SUMMARY.md가 전환 결정과 삭제된 파일 목록을 기록한다
  </done>
</task>

</tasks>

<verification>
1. Assets/Tests/ 디렉토리가 존재하지 않는다
2. 02-04-PLAN.md에 CombatTests.cs / PlayMode.asmdef / RollTests.cs 생성 태스크가 없다
3. 02-04-PLAN.md에 에디터 플레이테스트 checkpoint:human-verify 태스크가 있다
4. 02-04-EDITOR-GUIDE.md가 각 요구사항(ATCK-01~05, FEEL-01, MOVE-03)에 대해 "어떻게 실행"과 "무엇을 확인"을 명시한다
5. 02-04-SUMMARY.md가 전환 이유와 삭제된 파일을 기록한다
</verification>

<success_criteria>
- Assets/Tests/ 없음
- 02-04-PLAN.md: 에디터 플레이테스트 checkpoint 1개, autonomous: false
- 02-04-EDITOR-GUIDE.md: ATCK-01~05 + FEEL-01 + MOVE-03 체크리스트 포함, Test Runner 언급 없음
- 02-04-SUMMARY.md: 전환 결정 기록, 테스트 러너 내용 없음
</success_criteria>

<output>
완료 후 별도 SUMMARY 파일 불필요 — 이 quick task가 02-04-SUMMARY.md 재작성을 포함한다.
STATE.md Quick Tasks Completed 테이블에 이 작업 기록 추가:
| 260604-vst | Phase 02-04를 테스트 러너에서 에디터 플레이테스트로 전환 | 2026-06-04 | — | 260604-vst-phase-2-4 |
</output>
