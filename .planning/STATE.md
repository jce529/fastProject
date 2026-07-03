---
gsd_state_version: 1.0
milestone: v3.0
milestone_name: Phases
status: executing
stopped_at: Completed 10-02-PLAN.md
last_updated: "2026-07-03T11:34:38.438Z"
last_activity: 2026-07-03
progress:
  total_phases: 4
  completed_phases: 2
  total_plans: 10
  completed_plans: 8
---

# Project State: Fast (가칭)

*Single source of truth for project memory across sessions.*

---

## Project Reference

**Core Value:** 공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.

**Prototype Goal:** 층을 룸+길 수평 체인으로 재설계 — 양방향 무한 생성, 전투 Corridor, 확률적 EXIT 포탈, 제한 시간으로 "빠른 탈출" 긴장감 검증.

**Current Milestone:** v3.0 — 무한 복도 층 시스템

---

## Current Position

Phase: 10 (exit-portal-floor-transition) — EXECUTING
Plan: 3 of 4
Next: Phase 10 (EXIT 포탈 & 층 전환)
Status: Ready to execute
Last activity: 2026-07-03

```
Progress: [██████████████████░░] 9/11 phases complete (v3.0: Phase 8, 9 complete)
```

---

## Accumulated Context

### Key Decisions Locked (from v1.0/v2.0)

| Decision | Rationale |
|----------|-----------|
| Infrastructure merged into Phase 1 | No v1 requirements map to pure infrastructure; merging keeps phase count honest for prototype scope |
| MOVE-03 (roll) in Phase 2 not Phase 1 | Roll is a combat tool (slow-mo usable, i-frames), not a movement primitive — belongs with the combat system it interacts with |
| FEEL-01 (hit-freeze) in Phase 2 | Must ship with the attack system — hit-freeze is the punctuation of the kill, cannot be validated separately |
| jumpCutMultiplier = 0.4 (D-02) | Drops ascending velocity to 40% on button release — clear tap-vs-hold arc difference |
| Time.timeScale compensation in Phase 1 | 1f / Time.timeScale in ApplyMovement baked in now — Phase 2 slow-mo requires no PlayerController rewrite |
| PlayerInput notification = SendMessages (0) | PlayerController reads actions directly via playerInput.actions[], behavior mode is irrelevant |
| WaitForSecondsRealtime for i-frames (01-03) | Phase 2 sets timeScale ~0.2; WaitForSeconds would extend 1s to 5s. WaitForSecondsRealtime is timeScale-immune. |
| Vector3 _lastSafePosition value type (01-03) | Storing Transform reference would become stale null ref when floor objects recycled in v2. Vector3 copy is immune (Pitfall 14). |
| Layer constants hardcoded 7/8 (01-03) | Matches TagManager.asset from Plan 01. Avoids LayerMask.NameToLayer() string lookup overhead each call. |
| RangedEnemy moveSpeed=0f default (03-04) | Stationary at start per D-10 — Chase state immediately telegraphs (Risk 6 mitigation: no distance-check needed). Inspector-adjustable post-playtest. |
| TelegraphAndFire uses yield return null + unscaledDeltaTime (03-04) | Frame-by-frame alpha accumulation matches RangeDisplay pattern, fully timeScale-immune for slow-mo compatibility. |
| FloorManager as static class, not MonoBehaviour (04-01) | Data-only int needs no scene lifecycle; static field is sufficient and avoids scene coupling |
| (AttackType)(-1) dirty-check sentinel (04-01) | Forces first-frame label update; -1 is never a valid AttackType so it always differs on first Update() |
| SetText("{0}", int) over string interpolation (04-01) | TMP's int overload uses internal char buffer — zero allocation per frame vs. $"" allocating new string every frame |

### Key Decisions for v3.0

| Decision | Rationale |
|----------|-----------|
| EXIT 포탈 기본 확률 0.15f (15%) | "낮음" 기준값 — 인스펙터에서 조절 가능. 너무 낮으면 플레이어 답답, 너무 높으면 긴장감 소멸 |
| EXIT 최대 동시 활성 1개 | 한 층에 포탈이 여러 개면 탐색 동기가 분산됨; 1개가 명확한 목표를 제공 |
| Corridor = 전투 구간 | 단순 통로가 아닌 적+장애물 있는 구간 — 이동 자체가 위험한 느낌 유지 |
| Corridor 3타입 (상승/직진/하강) | 수직 변화로 탐색 경로에 다양성 제공, 플랫포머 감각 유지 |
| 뒤 2개 룸+길 유지 후 Destroy | 모바일 메모리 관리; 플레이어가 뒤로 돌아갈 수 있는 범위 제한 |
| WorldGenerator = 신규 MonoBehaviour (FloorSpawner 대체) | 수평 양방향 체인 생성은 기존 수직 FloorSpawner 로직과 구조가 달라 대체가 가장 깔끔 |
| FloorTimer = 정적 클래스 | ScoreManager 패턴 답습 — 씬 수명 불필요, 데이터 전용 |
| Room 14종 → Tilemap 방식 전환 (08-04, ARCH-04) | Corridor가 이미 TilemapCollider2D 방식으로 전환됨(quick-260701-k1e) — Room도 통일해 WorldGenerator가 타일 좌표(정수)로 크기/연결점을 계산 가능하게 함. 사용자가 Unity Editor에서 직접 실행 |
| WorldGenerator._roomPrefabs = Complex_Room 6종 (AllInOne/EdgeRun/GaugeOutpost/LastStand/RiskCrossing/Vertical_Gauntlet) | 09-03-PLAN 원안의 기본 Room_* 13종 대신 다방향 연결 지원하는 신규 Complex_Room 풀로 교체 — 사용자 확정 결정 |
| Corridor/Complex_Room 프리팹 fileID는 반드시 Int64.MaxValue(9223372036854775807) 이하로 생성 | quick-260701-sc7에서 19자리 무작위 fileID가 오버플로우해 CameraBound 컴포넌트가 3개 프리팹에서 깨짐 — 향후 유사 작업 시 문자열 비교로 검증 필수 |

### Technical Constraints to Enforce Every Phase

- `Time.unscaledDeltaTime` for ALL i-frame timers, cooldowns, coroutines
- `Physics2D.OverlapCircleNonAlloc()` — never `FindObjectsOfType` or LINQ in Update
- Animator Transition Duration = 0 for all action-state transitions
- `Rigidbody2D`: Continuous collision detection + Interpolate mode
- Dash: `MovePosition()` over 2-3 frames, never a velocity spike
- Invincibility: layer swap (PlayerHurtbox / PlayerInvincible), never IgnoreLayerCollision
- Floor transition: `WaitForSecondsRealtime` only — `Time.timeScale` may be 0 at transition start
- Timer: `Time.unscaledDeltaTime` only — timer must be immune to slow-motion timeScale

### Quick Tasks Completed (v1.0/v2.0)

40개+ Quick 태스크 완료 (2026-06-01 ~ 2026-06-26) — 전체 목록은 `git log` 참조.

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260701-k1e | CorridorBuilder.cs를 BoxCollider2D 오브젝트 방식에서 TilemapCollider2D 타일맵 방식으로 전환 | 2026-07-01 | 9b97b23 | [260701-k1e-corridorbuilder-cs-boxcollider2d-tilemap](./quick/260701-k1e-corridorbuilder-cs-boxcollider2d-tilemap/) |
| 260701-j76 | RoomCreator.cs 3-way patch — SpawnMarker, EnsureLadderTile, ladder single-col | 2026-07-01 | ee94cce | [260701-j76-roomcreator-cs-3-spawnmarker-ensureladde](./quick/260701-j76-roomcreator-cs-3-spawnmarker-ensureladde/) |
| 260701-sc7 | Corridor 3종 + Complex_Room 6종에 CameraBound 추가, fileID 오버플로우 버그 수정 | 2026-07-01 | (uncommitted) | [260701-sc7-corridor-complex-room](./quick/260701-sc7-corridor-complex-room/) |

### Pending Todos

| Date | Title | Area | File |
|------|-------|------|------|
| 2026-07-03 | Complex_Room ENT 기반 세로 스폰 순간이동 | planning | [2026-07-03-complex-room-ent](./todos/pending/2026-07-03-complex-room-ent.md) |

---

## Session Continuity

**How to resume after /clear:**

1. Read `.planning/STATE.md` (this file) — current position and decisions
2. Read `.planning/ROADMAP.md` — phase goals and success criteria
3. Read `.planning/REQUIREMENTS.md` — requirement details and traceability
4. Continue from Current Phase listed above

**Last session:** 2026-07-03T11:34:38.432Z
**Stopped at:** Completed 10-02-PLAN.md

---
*State initialized: 2026-05-27*
*Last updated: 2026-07-01 — Phase 9 complete, Phase 8 ARCH-04 (Room tilemap) done manually, quick-sc7 CameraBound fix*
