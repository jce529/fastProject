# Milestones

## v1.0 — Combat Test Room (complete)

**Goal:** 핵심 전투 메카닉(슬로우모션 조준 → 돌진 처치)이 모바일에서 재미있는지 검증하는 단일 테스트 룸 프로토타입

**Completed:** 2026-06-23

**Phases:** 5 phases, 15 plans

| Phase | Name | Status |
|-------|------|--------|
| 01 | Foundation & Movement | complete |
| 02 | Combat Core | complete |
| 03 | Enemy System | complete |
| 04 | HUD & Game Loop | complete |
| 05 | 절차적 맵 생성 — 무한 스테이지 | complete |

**Key outcomes:**
- 플레이어 이동/점프/낙사 복귀 구현
- 슬로우모션 조준 → 돌진 처치 → 게이지 시스템 구현
- 근접/원거리 적 FSM 구현
- HUD + 사망 화면 + 재시작 구현
- 절차적 맵 생성 (5종 Room 프리팹, 층 전환 시퀀스) 구현

---

## v2.0 — 게임 시작 플로우 (in progress)

**Goal:** 개발자 도움 없이 앱 실행 → 공격 방식 선택 → 게임 진입까지 완전한 시작 흐름을 갖춘 플레이테스트 빌드

**Started:** 2026-06-23

**Target features:**
- MainMenu.unity — 타이틀 + Start / Quit 버튼
- AttackSelect.unity — Linear / Fan 선택 후 게임 진입
- 씬 플로우: MainMenu → AttackSelect → SampleScene
- 선택한 공격 방식이 SampleScene에서 유지
- 사망 후 MainMenu로 복귀
