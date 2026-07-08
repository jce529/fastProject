# Milestones

## v3.0 — 무한 복도 층 시스템 (complete)

**Goal:** 층을 룸+길 수평 체인으로 재설계 — 양방향 무한 생성, 전투 Corridor, 확률적 EXIT 포탈, 제한 시간으로 "빠른 탈출" 긴장감을 검증한다.

**Completed:** 2026-07-08

**Phases:** 5 phases, 23 plans, 36 tasks (Phase 8-12)

| Phase | Name | Status |
|-------|------|--------|
| 08 | 룸-길 아키텍처 | complete |
| 09 | 무한 양방향 생성 & 정리 | complete |
| 10 | EXIT 포탈 & 층 전환 | complete |
| 11 | 타이머 & 난이도 | complete |
| 12 | 포탈 애니메이션 & 히트 임팩트 폴리싱 | complete |

**Key accomplishments:**

- RoomConnector 마커(END_Left/END_Right) + RoomMarkerTool 에디터 도구로 Room 프리팹 체인 연결 아키텍처 구축, Corridor 3종(상승/직진/하강) 전투 프리팹 제작
- WorldGenerator: 플레이어 이동 방향 앞 2개 Room+Corridor 자동 생성, 뒤 2개 초과분 자동 Destroy — 무한 양방향 체인 완성
- ExitPortal 확률적 스폰(기본 15%) + ExitSpawnPoint 기반 랜덤 텔레포트로 층 전환, 허공 스폰 버그 근본 해결
- FloorTimer(슬로우모션 면역 카운트다운) + 층 번호 기반 난이도 스케일링 + 시간 비례 점수 시스템(ScoreManager)
- 포탈 SpriteMask 입/퇴장 연출, Whiff/Roll 애니메이터 트리거 수정, 히트 스파크+카메라 쉐이크+대시 트레일, 적 사망 파티클+페이드 연출 및 실제 Destroy 처리

**Stats:** 125 commits, 303 files changed, +57,045/-11,604 lines, 10-day timeline (2026-06-29 → 2026-07-08)

---

## v2.0 — 게임 시작 플로우 (complete)

**Goal:** 개발자 도움 없이 앱 실행 → 공격 방식 선택 → 게임 진입까지 완전한 시작 흐름을 갖춘 플레이테스트 빌드

**Completed:** 2026-06-28 (ROADMAP.md 기준 Phase 6-7 완료일 최신값)

**Phases:** 2 phases (Phase 6-7)

| Phase | Name | Status |
|-------|------|--------|
| 06 | MainMenu Scene | complete |
| 07 | AttackSelect Scene & Scene Flow | complete |

**Key outcomes:**

- MainMenu.unity — 타이틀 + Start / Quit 버튼
- AttackSelect.unity — Linear / Fan 선택 후 게임 진입
- 씬 플로우: MainMenu → AttackSelect → SampleScene
- 선택한 공격 방식이 SampleScene에서 유지
- 사망 후 AttackSelect로 복귀

---

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
