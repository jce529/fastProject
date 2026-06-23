# Fast (가칭)

## What This Is

모바일 2D 가로 화면 플랫포머 액션 게임 프로토타입. 플레이어는 끝없이 이어지는 탑을 올라가며, 공격 버튼을 누르면 슬로우 모션이 발동하고 손을 떼면 범위 안 가장 가까운 적에게 돌진해 원샷으로 처치한다. 이 핵심 전투 메카닉과 구르기를 이용한 회피 시스템이 모바일에서 실제로 재미있는지 검증하는 것이 목적이다.

## Core Value

**공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.**

## Current Milestone: v2.0 게임 시작 플로우

**Goal:** 개발자 도움 없이 앱 실행 → 공격 방식 선택 → 게임 진입까지 완전한 시작 흐름을 갖춘 플레이테스트 빌드

**Target features:**
- MainMenu.unity — 타이틀 + Start / Quit 버튼
- AttackSelect.unity — Linear / Fan 선택 후 게임 진입
- 씬 플로우: MainMenu → AttackSelect → SampleScene
- 선택한 공격 방식이 SampleScene에서 유지 (씬 간 데이터 전달)
- 사망 후 MainMenu로 복귀

## Requirements

### Validated

(None yet — ship to validate)

### Active

**플레이어 조작**
- [ ] 플레이어가 빠르게 좌우 이동 및 점프할 수 있다
- [ ] 공격 버튼 누름 시 슬로우 모션 발동, 공격 범위 표시, 손을 떼면 돌진 공격
- [ ] 별도 버튼으로 구르기 발동 — 무적 판정 + 쿨타임, 슬로우 중에도 사용 가능
- [ ] 낙사 시 사망하지 않고 마지막 플랫폼으로 복귀 + 짧은 무적

**공격 시스템**
- [ ] 직선형 / 부채꼴형 두 가지 공격 타입 중 게임 시작 전 선택 (단순 버튼 2개)
- [ ] 공격 범위 안 가장 가까운 적 자동 선택 후 돌진 — 돌진 중 무적
- [ ] 범위 안 적 없으면 헛베기 (적 처치 성공보다 긴 딜레이)
- [ ] 시간정지 게이지: 자동 회복 + 적 처치 시 회복, 게이지 소진 시 슬로우 해제되나 공격 입력은 유지

**적**
- [ ] 근접형 적: 접근 후 공격 예고 모션 → 근접 공격 (원샷원킬 양방향)
- [ ] 원거리형 적: 조준선 표시 후 투사체 발사 (원샷원킬 양방향)
- [ ] 카메라 전환 완료 후에만 적 플레이어 인식 시작

**층 구조 및 전환**
- [ ] 프리셋 기반 층 생성 (3~5개 프리셋 — 플랫폼/사다리/계단/낙사/혼합)
- [ ] 위쪽 출구 도달 시 즉시 층 전환: 조작 불가 → 순간이동 → 카메라 상승 → 가림막 해제 → 적 인식 활성화 → 조작 재개
- [ ] 이전 층 제거/비활성화 (모바일 성능)

**UI / 피드백**
- [ ] HUD: 현재 층 수, 시간정지 게이지, 공격 타입 표시
- [ ] 사망 화면 + 재시작 버튼 (1층부터 재시작)
- [ ] 단순 실루엣 그래픽 스타일

### Out of Scope

- 복잡한 성장 시스템 (레벨업, 영구 강화, 상점) — 핵심 전투 검증이 목적, 성장은 검증 후 추가
- 보스전 — 프로토타입 범위 초과
- 이단 점프 / 벽점프 / 벽타기 / 공중 대시 — 기본 점프만으로 검증, 추후 업데이트 후보
- 별도 대시 버튼 (이동 목적의 대시) — 돌진은 공격에 귀속, 순수 이동 대시는 게임성 오염
- 랭킹, 광고, 과금 — 프로토타입 단계 외
- 콤보 시스템, 패링, 무기 강화 — 핵심 검증과 무관

## Context

**기술 환경:** Unity 6000.3.11f1 LTS, C#, Universal Render Pipeline 2D Renderer, Unity Input System 1.19.0. Android 우선 (minSdk 25 / ARM64), iOS 추후. 가로 화면(1920×1080 기본).

**프로토타입 검증 목표 (6개):**
1. 공격 버튼 누르기/떼기 전투가 재미있는가?
2. 직선형 vs 부채꼴형 — 어떤 공격 범위가 모바일에 더 적합한가?
3. 원샷원킬 구조가 모바일 조작에서 불쾌하지 않은가?
4. 무한 탑 등반 구조가 자연스럽게 이어지는가?
5. 적을 모두 죽이지 않아도 되는 등반 플레이가 재미있는가?
6. 낙사 복귀 방식이 액션감을 해치지 않는가?

**코드베이스 현황:** Unity 기본 씬(SampleScene) 외 게임 로직 없음. 패키지는 모두 설치된 상태.

## Constraints

- **Tech Stack**: Unity 6 LTS + C# — 이미 설정된 프로젝트 환경
- **Platform**: Android 우선 (ARM64, minSdk 25) — 성능 예산 고려 필요
- **Scope**: 핵심 메카닉 검증에만 집중 — 프로토타입 외 기능 추가 금지
- **Performance**: 현재 층 + 다음 층만 유지, 이전 층 제거 — 모바일 메모리 관리

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 구르기 추가 (별도 버튼, 무적, 쿨타임) | 공격 돌진(공격 귀속)과 달리 순수 회피기 — 공격 사이 생존 선택지 제공 | — Pending |
| 대시 버튼 미포함 (구르기와 구분) | 돌진은 공격 시스템의 일부, 이동 목적 대시는 전투 긴장감 희석 | — Pending |
| 슬로우 중 구르기 허용 | 공격 조준 취소 → 회피 선택 → 플레이어의 의사결정 깊이 증가 | — Pending |
| 시간정지 게이지 회복: 자동 + 적 처치 | 단순 자동 회복(테스트 용이) + 처치 보상 감각(공격적 플레이 유도) 동시 충족 | — Pending |
| 사망 후 1층부터 재시작 | 프로토타입 검증에 가장 단순, 테스트 중 피로도 높으면 현재 층 재시작으로 변경 가능 | — Pending |
| 원샷원킬 (플레이어·적 모두) | 전투 긴장감 유지, 체력 시스템 없이 간결한 난이도 표현 | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd:transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-06-23 — v2.0 milestone started*
