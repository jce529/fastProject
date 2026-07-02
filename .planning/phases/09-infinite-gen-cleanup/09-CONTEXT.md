# Phase 9: 무한 양방향 생성 & 정리 - Context

**Gathered:** 2026-06-29
**Status:** Pre-planning (Phase 8 실행 중)

<domain>
## Phase Boundary

플레이어가 이동하는 방향으로 Room+Corridor 쌍이 자동 생성되고, 뒤에 남겨진 구간은 자동 Destroy된다. 별도 Y 좌표에 다음 층 대기룸을 미리 스폰하는 pre-load 구조도 함께 구현한다.

**Requirements in scope:** GEN-01, GEN-02, GEN-03
**Not in scope:** EXIT 포탈 트리거 로직(Phase 10), 타이머/난이도(Phase 11)

</domain>

<chain_structure>
## 런타임 체인 구조

```
                                        [다음 층 대기룸]
                                             ↑
                                        (EXIT 포탈 생성 시 별도 Y에 비활성 스폰)

[이전룸1] - [길] - [이전룸2] - [길] - [현재룸] - [길] - [대기룸1] - [길] - [대기룸2]
```

**규칙:**
- 룸과 룸 사이에는 반드시 길(Corridor)이 하나 존재한다
- 현재룸 앞: 길+대기룸 2쌍 미리 생성 (GEN-01)
- 현재룸 뒤: 길+이전룸 2쌍 유지, 초과분 Destroy (GEN-02)
- 각 길은 Flat/Up/Down 중 Y 범위 내에서 랜덤 선택 (GEN-03)
- 다음 층 대기룸: 수평 체인과 별개로 다음 층 Y 좌표에 1개 비활성 스폰

</chain_structure>

<decisions>
## Implementation Decisions (Pre-planning)

### Y 드리프트 범위 제한
- **D-01:** WorldGenerator는 `_currentYDrift` (float)를 추적한다. Corridor_Up 선택 시 +4, Corridor_Down 선택 시 -4 누적.
- **D-02:** 범위: `_minYDrift = -12f`, `_maxYDrift = +12f`. ±3회 Up/Down 허용.
  - 근거: Corridor_Up/Down의 ENT→EXIT ΔY = ±4 units (CorridorBuilder.cs 기준).
  - 고형 룸(Room_Ladder, Room_LadderDanger) CameraBound Y = 22.5 — 3회 적층 시 12 units로 고형 룸 높이 내 유지.
  - Inspector에 `_minYDrift`, `_maxYDrift` 노출 → 플레이테스트 후 조정 가능.
- **D-03:** 랜덤 선택 시 현재 드리프트가 maxYDrift에 도달하면 Corridor_Up 제외, minYDrift에 도달하면 Corridor_Down 제외. 범위 내 옵션 중 Random.

### 다음 층 대기룸 (Next Floor Standby Room)
- **D-04:** EXIT 포탈이 Room에 스폰될 때, 다음 층 대기룸을 `nextFloorBaseY = currentFloorBaseY + _floorHeight` 위치에 동시 스폰한다. 비활성(SetActive(false)) 상태로 대기.
- **D-05:** 대기룸은 수평 체인(WorldGenerator의 체인 리스트)에 포함되지 않는다 — 별도 `_nextFloorRoom` 참조로 관리.
- **D-06:** Phase 10에서 ExitPortal이 트리거될 때 `_nextFloorRoom`을 활성화하고, WorldGenerator가 이 룸을 새 시작점으로 삼아 수평 체인을 초기화한다. Phase 9에서는 스폰까지만 구현; 트리거 연동은 Phase 10 범위.
- **D-07:** 대기룸 파괴: 층 전환 없이 EXIT 포탈이 소멸(확률 재계산 등)되면 `_nextFloorRoom` Destroy. Phase 10에서 결정.

### WorldGenerator 컴포넌트
- **D-08:** `FloorSpawner.cs`는 Phase 9에서 건드리지 않는다. WorldGenerator는 신규 MonoBehaviour로 작성, SampleScene에 FloorSpawner 대신 배치.
- **D-09:** 체인은 `List<(GameObject room, GameObject corridor)>` 로 관리. corridor는 해당 room의 왼쪽(ENT 방향) 길을 의미.

</decisions>

<derived_from>
## 수치 근거 (Phase 8 산출물 기반)

| 항목 | 수치 | 출처 |
|------|------|------|
| Corridor_Up ΔY | +4 units | CorridorBuilder.cs: ENT(−6,0) → EXIT(7,4) |
| Corridor_Down ΔY | −4 units | CorridorBuilder.cs: ENT(−7,4) → EXIT(6,0) |
| Corridor_Flat ΔY | 0 units | CorridorBuilder.cs: ENT(−6,0) → EXIT(6,0) |
| 표준 룸 CameraBound Y | 11.25~16 | Room_Combat(11.25), Room_Stair(16) |
| 고형 룸 CameraBound Y | 22~22.5 | Room_Ladder(22.5), Room_LadderDanger(22.5), Room_Hunt(22) |
| 허용 Up/Down 연속 횟수 | 최대 3회 | _maxYDrift(12) ÷ ΔY(4) |

</derived_from>

<canonical_refs>
## Canonical References

- `.planning/ROADMAP.md` §Phase 9 — GEN-01, GEN-02, GEN-03 요구사항 정의
- `.planning/STATE.md` §Key Decisions for v3.0 — WorldGenerator 신규 MonoBehaviour 결정
- `Assets/Editor/CorridorBuilder.cs` — Corridor 3종 ENT/EXIT 좌표 원본
- `Assets/Scripts/World/RoomConnector.cs` — 체인 연결 마커 컴포넌트
- `.planning/phases/08-room-corridor-architecture/08-CONTEXT.md` — Phase 8 아키텍처 결정

</canonical_refs>

---

*Phase: 09-infinite-gen-cleanup*
*Context gathered: 2026-06-29 (pre-planning, Phase 8 실행 중)*
