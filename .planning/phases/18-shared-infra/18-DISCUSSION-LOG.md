# Phase 18: 공유 인프라 — 전투 모듈 추상화 & 보스 베이스 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-20
**Phase:** 18-shared-infra
**Areas discussed:** 플랫폼 범위(비계획), 터치 조준 입력 방식, F.I.O.R.A 보스 처리, INFRA-01 회귀 검증 깊이

---

## 사전 이슈 — GSD 로드맵 파싱 버그

`/gsd:discuss-phase 18` 시작 직후 `gsd-tools init phase-op`가 `phase_found: false`를 반환 — Phase 18이 ROADMAP.md에 명백히 존재함에도 발생. 원인 조사 결과 `core.cjs`의 `extractCurrentMilestone()`이 (1) v3.1 헤딩 문구에 우연히 포함된 "v4.0" 문자열에 잘못 매칭되고, (2) v4.0 섹션 자체의 서브헤딩("## v4.0 Phases" 등)도 같은 이유로 조기 종료를 유발하는 이중 버그로 확인됨. 사용자 승인 후 ROADMAP.md의 v3.1 헤딩 문구 수정 + v4.0 서브헤딩을 언프리픽스 컨벤션으로 통일하여 우회. 상세는 `.planning/phases/18-shared-infra/18-CONTEXT.md`에는 미포함(도구 버그이지 Phase 결정이 아님) — 별도 메모리(`fix_gsd_roadmap_milestone_parsing_bug.md`)에 기록.

---

## 터치 조준(Aim) 입력 방식 → 플랫폼 재검토로 전환

| Option | Description | Selected |
|--------|-------------|----------|
| 탭한 지점 방향 (추천) | 플레이어에서 탭한 화면 좌표로의 방향 — 마우스 로직과 가장 유사 | |
| 드래그 방향 | 손가락 굵다가 놓은 지점 반대 방향 | |
| 자동 조준만 | 방향 입력 자체를 생략, 범위 표시만 플레이어 앞방으로 고정 | |

**사용자의 선택:** 위 옵션 중 선택하지 않음 — 첫 질문에 "근데 갑자기 터치는 왜 나오는거야? 지금 모바일 버전도 논의되고 있어?"라고 반문.
**Notes:** INFRA-02가 이전 `/gsd:new-milestone` 세션(같은 날 더 이른 시각)에서 이미 확정된 요구사항임을 설명. 이어진 확인 질문("INFRA-02를 그대로 진행할까요, 재검토할까요?")에 "한국어로 말하고 일단, PC를 기반으로 플랫폼을 정하자. 그리고 혹시라도 다른 문서에서 모바일을 플랫폼으로 정한 문서가 있다면 수정해줘"라고 응답 — 이는 Phase 18 범위를 넘어선 프로젝트 전체 피벗 요청으로 확대됨.

---

## 플랫폼 전환 범위 확인

| Option | Description | Selected |
|--------|-------------|----------|
| v4.0만 PC 우선 | 모바일은 나중에 별도 Phase로 | |
| 프로젝트 자체를 PC로 영구 재설정 | CLAUDE.md/PROJECT.md의 "모바일" 자체를 PC로 수정, Android는 더 이상 목표 플랫폼 아님 | ✓ |

**사용자의 선택:** 프로젝트 자체를 PC로 영구 재설정
**Notes:** 이어서 적용 범위(기획 문서만 vs Unity 엔진 설정까지)를 재확인.

## 적용 범위 확인

| Option | Description | Selected |
|--------|-------------|----------|
| 기획/로드맵 문서만 (추천) | CLAUDE.md, PROJECT.md, 기획서.md, ROADMAP.md/REQUIREMENTS.md(INFRA-02 제거), research 문서 수정. Unity ProjectSettings는 미변경 | ✓ |
| Unity 엔진 설정까지 함께 | ProjectSettings.asset의 빌드 타겟/화면 방향/해상도까지 PC 기준으로 변경 | |

**사용자의 선택:** 기획/로드맵 문서만 (추천)
**Notes:** 이 선택에 따라 CLAUDE.md, `.planning/PROJECT.md`, `.planning/codebase/STACK.md`, `기획서.md`, `.planning/ROADMAP.md`, `.planning/REQUIREMENTS.md`, `.planning/research/ARCHITECTURE.md`, `.planning/research/PITFALLS.md` 수정 완료. Unity ProjectSettings.asset은 손대지 않음.

---

## F.I.O.R.A 보스 처리 방식

| Option | Description | Selected |
|--------|-------------|----------|
| FioraBoss로 정체성 부여 (추천) | BossEnemyBase 추출 후 기존 BossEnemy.cs를 `FioraBoss : BossEnemyBase`로 명명 | ✓ |
| 순수 테스트 스캐폴드로만 유지 | 이름/식별 부여 없이 그대로 둠, 실제 보스 정체성 부여는 다음 기회로 미룸 | |

**사용자의 선택:** FioraBoss로 정체성 부여 (추천)
**Notes:** STORY.md/PROJECT.md가 F.I.O.R.A를 "이미 구현된 Overclock Mode의 원본"으로 이미 명시하고 있어 나중에 다시 이름 붙이는 것보다 지금 일관되게 가져가는 편이 낫다고 판단.

---

## INFRA-01 회귀 검증 깊이

| Option | Description | Selected |
|--------|-------------|----------|
| 수동 플레이테스트만 (추천) | 지금까지 모든 Phase가 이 방식 — 마이그레이션 전/후 동작을 직접 비교 | ✓ |
| 자동화 PlayMode 테스트도 함께 구축 | Phase 2에서 미완료로 남은 02-04-PLAN(CombatTests/RollTests)을 이번에 드디어 실행 | |

**사용자의 선택:** 수동 플레이테스트만 (추천)
**Notes:** verbatim move 리팩토링이라 리스크가 낮다고 판단 — 자동 테스트 인프라 구축은 범위를 불필요하게 늘림.

---

## Claude's Discretion

- `BossEnemyBase`/`FioraBoss` 파일 배치(폴더 구조)
- `IPlayerCombatModule`/`OverclockModule`/`CombatContext`의 정확한 메서드 시그니처
- `BossUnlockManager`의 정확한 API 형태
- 수동 플레이테스트 체크리스트 구성

## Deferred Ideas

- 자동화 PlayMode 회귀 테스트(02-04-PLAN 완성) — 향후 재검토
- Android/모바일 재지원 — 재검토 시점 미정
- 보스 러시 모드의 모듈 스왑 안전장치 — v4.0 범위 밖(RUSH-01)
