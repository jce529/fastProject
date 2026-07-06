---
created: 2026-07-03T09:45:28.749Z
resolved: 2026-07-06T16:36:00+09:00
title: Complex_Room ENT 기반 세로 스폰 순간이동
area: planning
files:
  - Assets/Scripts/World/WorldGenerator.cs
  - Assets/Scripts/World/RoomEntry.cs
  - Assets/Scripts/World/RoomConnector.cs
  - Assets/Scripts/World/FloorSpawner.cs
resolved-by: 10-exit-portal-floor-transition/10-03-PLAN.md
---

## Resolved

10-TRANSITION-DESIGN.md에서 RoomEntry 대신 ExitSpawnPoint를 재사용하는 방향으로 결정, Phase 10 Plan 03에서
`WorldGenerator.FloorTransitionSequence()` Step 2를 ExitSpawnPoint 랜덤 선택으로 교체하고 Complex_Room 6종
전부에 ExitSpawnPoint 마커를 배치 완료했다. RoomEntry를 4개 룸에 별도로 추가할 필요가 없어졌다 — 허공 스폰
버그의 근본 원인이 해소됨.

## Problem

컴플렉스룸(Complex_Room 6종) 기반으로 월드를 생성할 때 일부 룸에서 시작 시 플레이어가 허공에서 스폰된다.

현재 `WorldGenerator.AlignByEntry()`는 `RoomConnector`(Left/Right)의 월드 위치를 기준으로 룸 오브젝트 자체만 정렬할 뿐, 플레이어를 명시적으로 이동시키지 않는다 — 플레이어는 이전 룸/Corridor에서 걸어서 자연스럽게 다음 룸으로 진입한다. Complex_Room처럼 내부 구조가 복잡하거나 수직 낙차가 있는 룸은 Left 커넥터 지점 아래 바닥이 없어 플레이어가 걸어 들어가는 순간 허공에 놓일 수 있다.

과거 Room 버전(Phase 5, `FloorSpawner.FloorTransitionSequence()`)에서는 `RoomEntry`(ENT) 자식 오브젝트가 있으면 그 위치로 플레이어를 명시적으로 순간이동(teleport)시키고, 없으면 고정 공식으로 폴백했다. `DebugRoomTeleporter.cs`도 동일 패턴 사용.

조사 결과 — Complex_Room 6종 중 `RoomEntry` 컴포넌트 보유 현황:
- 있음: Room_GaugeOutpost(1개), Room_RiskCrossing(1개)
- 없음: Room_AllInOne, Room_EdgeRun, Room_LastStand, Room_Vertical_Gauntlet (0개) — 허공 스폰 의심 대상

## Solution

사용자 제안 방향: 룸 간 가로 이동(체인 진행, 같은 층 내 좌우 이동)은 지금처럼 플레이어가 직접 걸어서 이동하되, 세로(층/Y) 이동만 ENT(RoomEntry) 마커 위치로 순간이동시킨다.

TBD — Phase 10(EXIT 포탈 & 층 전환)이 이미 층 전환(세로 이동) 로직을 다루므로, 이 논의를 Phase 10 계획 시점에 통합해서 다룰 것. 구체적 구현(트리거 방식 vs AlignByEntry 확장 vs 별도 텔레포트 스텝)은 Phase 10 논의에서 결정.
