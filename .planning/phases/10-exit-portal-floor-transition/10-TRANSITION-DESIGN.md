# Phase 10 — FloorTransitionSequence 재설계

**작성일:** 2026-07-04  
**작성 맥락:** 기존 10-03-PLAN(RoomEntry 기반 ENT 텔레포트)을 대화 중 결정사항으로 대체

---

## 결정 사항 요약

### 1. RoomEntry → ExitSpawnPoint 기반 플레이어 스폰

| 항목 | 기존 계획 (10-03-PLAN) | 변경 후 |
|------|----------------------|---------|
| 플레이어 스폰 기준 | `RoomEntry` 컴포넌트 1개 | `ExitSpawnPoint[]` 중 랜덤 1개 |
| 필요 컴포넌트 | ExitSpawnPoint + **RoomEntry** | ExitSpawnPoint만 |
| 10-03 에디터 작업 | ExitSpawnPoint 배치 + RoomEntry 배치 | ExitSpawnPoint 배치만 |

**이유:**
- `ExitSpawnPoint`는 이미 바닥 위 안전한 위치에 배치해야 하는 마커 — 동일 지점이 플레이어 스폰에도 적합
- `RoomEntry`를 별도로 배치하는 중복 에디터 작업 제거
- 랜덤 선택으로 매 층 진입 위치가 달라져 탐색 다양성 증가
- 폴백: ExitSpawnPoint가 없을 경우 `newRoom.transform.position` 사용

**코드 변경 위치:** `WorldGenerator.cs` — `FloorTransitionSequence()` Step 2

```csharp
// 기존
RoomEntry entry = newRoom.GetComponentInChildren<RoomEntry>(true);
Vector3 teleportPos = entry != null ? entry.transform.position : newRoom.transform.position;

// 변경 후
var spawnPoints = newRoom.GetComponentsInChildren<ExitSpawnPoint>(true);
Vector3 teleportPos = spawnPoints.Length > 0
    ? spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position
    : newRoom.transform.position;
```

---

### 2. FloorTransitionSequence 전면 재설계 — 포탈 입출 애니메이션

#### 기존 시퀀스 (6단계)

```
1. LockInput
2. ENT 텔레포트
3. 카메라 스냅
4. yield return null
5. WaitForSecondsRealtime(0.05f)
6. UnlockInput
```

#### 새 시퀀스

```
[ENTRY — 입장 애니메이션]
E1. 입력 잠금 + ForceExitCombatState
E2. [플레이어] SpriteMask 생성 @ 포탈 X 위치
    maskInteraction = VisibleOutsideMask
    마스크 scale.x: 0 → 플레이어 전체 너비 커버
    → 플레이어가 포탈 경계선 너머로 사라짐 (~0.4s)
E3. [포탈] ExitPortal localScale: (1,1,1) → (0,0,0) (~0.3s)
E4. SpriteRenderer.enabled = false, SpriteMask Destroy
    → 플레이어 완전 비가시 상태

[FLOOR SETUP — 층 전환]
F1. FloorManager.CurrentFloor++
F2. standbyRoom의 ExitSpawnPoint[] 중 랜덤 1개 선택 → spawnPos 저장
F3. 기존 체인 전부 Destroy (포탈 포함, D-07)
F4. standbyRoom 활성화 → _chain = {(newRoom, null)}
F5. 플레이어 spawnPos 텔레포트 + Rigidbody2D velocity = 0
F6. 카메라 스냅 (새 룸 CameraBound)
F7. 상태 초기화: _currentYDrift=0, _playerCurrentIndex=0, _activeExitCount=0

[EXIT — 퇴장 애니메이션]
X1. [포탈] spawnPos에 PortalEffect 프리팹 Instantiate (scale 0, 트리거 없음)
    localScale: (0,0,0) → (1,1,1) (~0.4s)
X2. [플레이어] 새 SpriteMask 생성 @ 새 포탈 X (플레이어 너비 이상으로 시작)
    SpriteRenderer.enabled = true (마스크가 덮고 있어 비가시)
    마스크 scale.x: 넓음 → 0 (포탈 방향에서 바깥쪽으로 수축)
    → 플레이어가 포탈에서 걸어나오는 효과 (~0.5s)
X3. SpriteMask Destroy, maskInteraction = None 리셋
X4. [포탈] PortalEffect localScale: (1,1,1) → (0,0,0) → Destroy (~0.3s)

[POST — 게임플레이 시작]
P1. 적 스폰 — newRoom의 EnemySpawner[] 전부 Spawn() + Activate()
P2. 다음 룸 세팅 — SpawnNextPair() × _lookaheadCount 명시적 호출
P3. _isTransitioning = false
P4. UnlockInput
```

---

## 구현 계획

### 신규/변경 필드 (WorldGenerator.cs)

```csharp
[Header("Transition Animation")]
[SerializeField] private GameObject _portalEffectPrefab;  // 비주얼 전용, Collider 없음
[SerializeField] private float _entryMaskDuration  = 0.4f;
[SerializeField] private float _portalShrinkDuration = 0.3f;
[SerializeField] private float _exitPortalGrowDuration = 0.4f;
[SerializeField] private float _exitMaskDuration   = 0.5s;
[SerializeField] private float _portalFadeDuration = 0.3f;

[Header("Enemy Prefabs")]
[SerializeField] private GameObject _meleePrefab;
[SerializeField] private GameObject _rangedPrefab;

// Runtime
private bool _isTransitioning;
```

### SpriteMask 스프라이트

에셋 없이 런타임에 생성:

```csharp
private static Sprite CreateMaskSprite()
{
    var tex = new Texture2D(4, 4);
    var px = new Color[16];
    for (int i = 0; i < 16; i++) px[i] = Color.white;
    tex.SetPixels(px);
    tex.Apply();
    return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
}
```

### SpriteMask 방향 로직

포탈 X 기준으로 마스크가 플레이어 방향으로 성장:

```
direction = player.position.x > portalX ? +1 : -1
maskCenter.x = portalX + (maskWidth * 0.5f * direction)
scale.x: 0 → Abs(player.x - portalX) + playerSpriteHalfWidth
```

### _isTransitioning 플래그

```csharp
private void Update()
{
    if (_isTransitioning || _playerTransform == null || _chain.Count == 0) return;
    // ... 기존 Update 로직
}
```

### PortalEffect 프리팹 구성

```
PortalEffect (GameObject)
├── SpriteRenderer (포탈 스프라이트, No trigger)
└── (Animator 선택사항 — 없어도 코드 스케일 애니메이션으로 충분)
```

---

## 10-03 에디터 작업 변경점

| 기존 10-03 Task 2 요구사항 | 변경 후 |
|--------------------------|---------|
| ExitSpawnPoint 2~3개 배치 | 동일 (유지) |
| RoomEntry(ENTRY_Bottom) 4개 룸에 신규 배치 | **제거** — 불필요 |
| 대상 6종 모두에 ExitSpawnPoint 배치 | 동일 (유지) |

`RoomEntry.cs`는 코드에서 참조가 없어지지만 파일 자체는 유지 (기존 사용 흔적 보존).

---

## 영향받는 파일

| 파일 | 변경 내용 |
|------|----------|
| `Assets/Scripts/World/WorldGenerator.cs` | FloorTransitionSequence 전면 교체, 신규 필드/코루틴 추가 |
| `Assets/Prefabs/World/PortalEffect.prefab` | **신규** — 퇴장 포탈 비주얼 전용 프리팹 |
| `Assets/Scripts/World/RoomEntry.cs` | 참조만 남음, 삭제 보류 |
| `.planning/phases/10-exit-portal-floor-transition/10-03-PLAN.md` | RoomEntry 배치 조항 제거 필요 |
