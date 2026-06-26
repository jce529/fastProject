---
phase: quick-260624-u3w
plan: 01
subsystem: world/floor-spawner
tags: [enemy-spawn, difficulty-scaling, floor-system]
dependency_graph:
  requires: [FloorSpawner.SpawnRoom]
  provides: [formula-based GetEnemyCount]
  affects: [enemy count per floor, difficulty progression]
tech_stack:
  added: []
  patterns: [Mathf.Clamp for range-clamped progression, integer division for tier steps]
key_files:
  modified:
    - Assets/Scripts/World/FloorSpawner.cs
decisions:
  - "공식 기반 단조 증가 (Clamp + Min)를 3단계 랜덤 테이블 대신 채택 - 예측 가능한 난이도 곡선"
metrics:
  duration: "~5min"
  completed: "2026-06-24T12:44:11Z"
  tasks_completed: 1
  files_modified: 1
---

# Phase quick-260624-u3w Plan 01: GetEnemyCount 공식 교체 Summary

**One-liner:** Mathf.Clamp 공식으로 1층 1마리 ~ 11층+ 6마리 단조 증가, 원거리는 5층마다 +1 (total/2 상한)

## What Was Built

FloorSpawner.GetEnemyCount(int floor) 메서드의 3단계 if-else 랜덤 테이블을 4줄 공식으로 교체했다.

공식 동작 검증:

| 층  | total | ranged | melee |
|-----|-------|--------|-------|
| 1   | 1     | 0      | 1     |
| 3   | 2     | 0      | 2     |
| 5   | 3     | 1      | 2     |
| 7   | 4     | 1      | 3     |
| 10  | 5     | 2      | 3     |
| 11+ | 6     | 2      | 4     |

## Commits

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | GetEnemyCount 공식 교체 | 2d0f44c | Assets/Scripts/World/FloorSpawner.cs |

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- Assets/Scripts/World/FloorSpawner.cs modified with Mathf.Clamp formula
- Commit 2d0f44c exists in git log
