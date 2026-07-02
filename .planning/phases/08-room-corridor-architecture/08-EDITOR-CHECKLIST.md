# Phase 8 — Unity 에디터 작업 체크리스트

Phase 8에서 Unity 에디터 안에서 직접 실행/조작해야 하는 작업 전체 목록.

---

## 전체 순서 요약

```
[선택] Fast > Build Room Prefabs        ← 룸 프리팹 재생성이 필요한 경우만
  ↓
[필수] Fast > Phase8 > Add Room Connectors
  ↓
[필수] Fast > Phase8 > Build Corridors
  ↓
[필수] SampleScene 수동 배치 + 플레이테스트
```

---

## 1. `Fast > Build Room Prefabs` — 선택 실행

**스크립트:** `Assets/Editor/RoomPrefabBuilder.cs`  
**실행 조건:** Room 프리팹이 없거나 구조를 초기화하고 싶을 때만. 이미 프리팹이 있으면 건너뜀.

> ⚠️ 이 메뉴는 기존 프리팹을 **삭제 후 재생성**함. 프리팹에 수동으로 추가한 변경사항이 있으면 모두 사라짐.

### 생성/덮어쓰는 프리팹 (14개)

| 프리팹 | ENT 위치 | EXIT 위치 | 특이사항 |
|--------|----------|-----------|---------|
| Room_Combat | (-14, 0) | (14, 0) | Floor 30x1, WallLeft/Right, EnemySpawn 3개 |
| Room_Hunt | (-11, 0) | (11, 7) | 계단형 3단 플랫폼, EnemySpawn 3개 |
| Room_Ladder | (0, 0) | (0, 19) | 수직 터널, `[Ladder]` 마커 |
| Room_LadderDanger | (0, 0) | (0, 19) | 수직 터널 + 적 플랫폼 2개, PlatformRanged 분홍 |
| Room_Gap | (-14, 0) | (14, 1) | 플랫폼 4개 분리, KillZone |
| Room_Fall | (-11, 0) | (11, 2) | 플랫폼 3개, KillZone |
| Room_Sniper | (-14, 0) | (14, 5) | 엄폐물 + HighGround, EnemySpawn 3개 |
| Room_Stair | (-12, 0) | (12, 9) | 5단 계단 (Y=0→8) |
| Room_Crossroad | (-15, 0) | (15, 1) | 상·하 분기 경로, KillZone |
| Room_Chase | (-17, 0) | (17, 0) | 긴 복도형 (Floor 36x1), 천장 있음 |
| Room_Dodge | (-8, 0) | (8, 0) | 밀폐 공간 (Floor+Ceiling+WallLeft/Right) |
| Room_Chain | (-11, 0) | (11, 10) | 4단 계단, EnemySpawn 4개 |
| Room_Recovery | (-17, 0) | (17, 0) | 넓은 Floor, MovingPlatform 마커 2개 (파란색) |
| Room_Mixed | (-19, 0) | (19, 12) | 상하 분기 + 계단 혼합, KillZone |

### 확인 방법
- Console에 `[RoomPrefabBuilder] Room_Combat built at ...` × 14 로그
- `Assets/Prefabs/Rooms/` 아래 14개 폴더 각각에 `.prefab` 파일 존재
- 에러 없으면 완료

---

## 2. `Fast > Phase8 > Add Room Connectors` — 필수

**스크립트:** `Assets/Editor/RoomMarkerTool.cs`  
**대상:** Room_Combat, Room_Fall, Room_Gap, Room_Stair **4개만** (나머지 10개 룸은 Phase 9에서 처리)

### 각 프리팹에 일어나는 변경

| 자식 오브젝트 | 추가되는 컴포넌트 | 설정값 |
|--------------|----------------|--------|
| `ENT` | `RoomConnector` | `direction = Left` |
| `EXIT` | `RoomConnector` | `direction = Right` |

**변경되지 않는 것:** Transform, Collider, TileMap, 기타 컴포넌트, ENT/EXIT 외 자식들

**멱등성:** 이미 RoomConnector가 붙어 있으면 skip — 두 번 실행해도 안전

### 시각적 변화 (씬 뷰)
- ENT 위치에 **파란색 구체** (반지름 0.4) Gizmo 표시
- EXIT 위치에 **초록색 구체** (반지름 0.4) Gizmo 표시
- 오브젝트를 선택하지 않아도 항상 보임 (`OnDrawGizmos`)

### 확인 방법
1. Console에 다음 4개 로그 확인:
   ```
   [RoomMarkerTool] RoomConnector applied to Room_Combat
   [RoomMarkerTool] RoomConnector applied to Room_Fall
   [RoomMarkerTool] RoomConnector applied to Room_Gap
   [RoomMarkerTool] RoomConnector applied to Room_Stair
   [RoomMarkerTool] Done. RoomConnectors added to all target rooms.
   ```
2. Room_Combat 더블클릭 → 프리팹 편집 모드 → 씬 뷰에 파란/초록 구체 확인
3. ENT 자식 선택 → Inspector에 `RoomConnector` 컴포넌트, `Direction: Left` 확인
4. EXIT 자식 선택 → Inspector에 `RoomConnector` 컴포넌트, `Direction: Right` 확인

---

## 3. `Fast > Phase8 > Build Corridors` — 필수

**스크립트:** `Assets/Editor/CorridorBuilder.cs`  
**생성 위치:** `Assets/Prefabs/Corridors/`

### 생성되는 프리팹 3종

#### Corridor_Flat (직진)
```
플랫폼: Floor (12x1, Y=0)
ENT:  로컬 (-6, 0)  → Direction.Left  → 파란 Gizmo
EXIT: 로컬 ( 6, 0)  → Direction.Right → 초록 Gizmo
EnemySpawn_0: (0, 1) — 태그: EnemySpawnPoint
```
- ENT/EXIT가 같은 Y → 높이 차 없이 직진 연결용

#### Corridor_Up (상승, +4 높이)
```
플랫폼: Step_A (-4, 0, 5x1), Step_B (1, 2, 5x1), Step_C (5, 4, 5x1)
ENT:  로컬 (-6, 0)  → Direction.Left
EXIT: 로컬 ( 7, 4)  → Direction.Right
EnemySpawn_0: (5, 5)
```
- EXIT가 ENT보다 Y+4 → 다음 룸이 높이 있을 때 연결용

#### Corridor_Down (하강, -4 높이)
```
플랫폼: Step_A (-5, 4, 5x1), Step_B (-1, 2, 5x1), Step_C (4, 0, 5x1)
ENT:  로컬 (-7, 4)  → Direction.Left
EXIT: 로컬 ( 6, 0)  → Direction.Right
EnemySpawn_0: (-5, 5)
```
- ENT가 EXIT보다 Y+4 → 다음 룸이 낮을 때 연결용

### 각 Corridor 공통 구조
- `Geometry/` — 플랫폼들 (Layer=9 Platform, BoxCollider2D, SpriteRenderer)
- `ENT` — RoomConnector(Left) 부착된 마커
- `EXIT` — RoomConnector(Right) 부착된 마커
- `EnemySpawn_0` — 태그 `EnemySpawnPoint` (Phase 9 스폰 시스템용)

**멱등성:** 기존 프리팹 있으면 DeleteAsset 후 재생성

### 확인 방법
1. Console에 로그 3개:
   ```
   [CorridorBuilder] Corridor_Flat built at Assets/Prefabs/Corridors/...
   [CorridorBuilder] Corridor_Up built at ...
   [CorridorBuilder] Corridor_Down built at ...
   [CorridorBuilder] All 3 corridors built.
   ```
2. Project 창 `Assets/Prefabs/Corridors/` 아래 3개 폴더/프리팹 존재 확인
3. Corridor_Flat 더블클릭 → ENT 파란 구, EXIT 초록 구 Gizmo 확인
4. EnemySpawn_0 선택 → Inspector `Tag: EnemySpawnPoint` 확인

---

## 4. SampleScene 수동 배치 + 플레이테스트 — 필수

2, 3번 완료 후 ARCH-03 검증을 위한 수동 작업.

### 준비
1. `Assets/Scenes/SampleScene.unity` 열기
2. Hierarchy에 기존 FloorSpawner / Room 관련 오브젝트 있으면 **비활성화** (체크 해제) — 충돌 방지

### 배치 순서

| 단계 | 프리팹 | 씬 배치 위치 | 정렬 기준 |
|------|--------|------------|---------|
| 1 | Room_Combat | Position (0, 0, 0) | 기준점 |
| 2 | Corridor_Flat | X ≈ 20 | Room_Combat EXIT Gizmo(x≈14)에 Corridor ENT Gizmo(-6) 겹치도록 |
| 3 | Room_Fall | Corridor_Flat EXIT Gizmo에 Room_Fall ENT Gizmo 겹치도록 | |

> **정렬 팁:** 씬 뷰에서 Gizmo 구체가 겹쳐 보이면 정렬 완료. 정확한 계산:
> - Corridor_Flat 월드 X = Room_Combat EXIT X - Corridor ENT 로컬 X = 14 - (-6) = **20**
> - Room_Fall 월드 X = Corridor_Flat 월드 X + Corridor EXIT 로컬 X - Room_Fall ENT 로컬 X = 20 + 6 - (-11) = **37**

### 플레이어가 없는 경우
- Hierarchy에 Player 프리팹이 없으면 Player 프리팹을 씬에 추가

### 플레이테스트 합격 기준
- Play 버튼 → 우방향 이동: Room_Combat → Corridor_Flat → Room_Fall 순서로 **막힘 없이** 연속 통과
- 불합격 기준: 보이지 않는 벽, 높이 차로 올라가지 못하는 지점, 낙사 구간

### 테스트 후 씬 정리
1. 배치한 Room_Combat, Corridor_Flat, Room_Fall 오브젝트 **삭제**
2. 비활성화했던 기존 오브젝트 **재활성화**
3. SampleScene **저장** (Ctrl+S)

---

## Phase 8 최종 완료 기준 (ARCH 요건)

| 요건 | 확인 내용 | 담당 단계 |
|------|----------|---------|
| ARCH-01 | Room_Combat/Fall/Gap/Stair ENT/EXIT에 RoomConnector 부착 + Gizmo 표시 | 단계 2 |
| ARCH-02 | Corridor 3종 프리팹 존재, EnemySpawnPoint 자식 포함 | 단계 3 |
| ARCH-03 | SampleScene 배치 후 플레이어 물리적 막힘 없이 Room→Corridor→Room 통과 | 단계 4 |
