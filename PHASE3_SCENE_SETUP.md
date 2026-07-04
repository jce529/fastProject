# Phase 3 씬 세팅 가이드

> Phase 3: Enemy System (ENMY-01 / ENMY-02)  
> 이 문서는 코드 작업(Plans 03-01 ~ 03-04 T1/T2)이 완료된 이후, Unity 에디터에서 직접 수행해야 하는 씬 세팅 절차를 정리합니다.

---

## 사전 확인

코드가 정상적으로 컴파일되는지 먼저 확인합니다.

- [x] `Assets/Scripts/Enemy/IEnemy.cs` 존재
- [x] `Assets/Scripts/Enemy/MeleeEnemy.cs` 존재
- [x] `Assets/Scripts/Enemy/RangedEnemy.cs` 존재
- [x] `Assets/Scripts/Enemy/ProjectileController.cs` 존재
- [x] `Assets/Scripts/Player/PlayerDeathHandler.cs` 존재
- [x] Unity 콘솔에 컴파일 에러 없음 ← Unity에서 직접 확인 필요

---

## Step 1: TagManager — EnemyProjectile 레이어 추가

`Edit > Project Settings > Tags and Layers`

| Layer Index | Name |
|---|---|
| 7 | PlayerHurtbox (기존) |
| 8 | PlayerInvincible (기존) |
| 9 | Platform (기존) |
| 10 | Enemy (기존) |
| **11** | **EnemyProjectile** ← 추가 |

저장: `File > Save Project`

---

## Step 2: Physics 2D 충돌 매트릭스 설정

`Edit > Project Settings > Physics 2D > Layer Collision Matrix`

아래 4쌍의 체크를 **해제(disable)** 합니다.

| Layer A | Layer B | 이유 |
|---|---|---|
| Enemy (10) | PlayerInvincible (8) | 구르기 중 무적 — 근접 공격 무효 |
| EnemyProjectile (11) | PlayerInvincible (8) | 구르기 중 무적 — 투사체 무효 |
| EnemyProjectile (11) | Enemy (10) | 투사체가 적에게 반응하지 않음 |
| EnemyProjectile (11) | EnemyProjectile (11) | 투사체끼리 충돌 없음 |

저장: `File > Save Project` (Physics2DSettings.asset 변경됨)

---

## Step 3: Player GameObject — PlayerDeathHandler 컴포넌트 추가

1. Hierarchy에서 **Player** 오브젝트 선택
2. `Add Component > PlayerDeathHandler`
3. `Ctrl+S` 저장

---

## Step 4: MeleeEnemy 오브젝트 생성

### 4-A. 루트 오브젝트

1. Hierarchy → 우클릭 → `Create Empty` → 이름: **MeleeEnemy**
2. 컴포넌트 추가:

| 컴포넌트 | 설정 |
|---|---|
| **MeleeEnemy** (스크립트) | — |
| **Rigidbody2D** | Body Type: Kinematic, Collision Detection: Continuous, Constraints: Freeze Rotation Z |
| **CapsuleCollider2D** | isTrigger: false, Size: (0.8, 1.2) |
| **SpriteRenderer** | Color: Red (placeholder) |

3. Layer: **Enemy** (10)
4. Tag: **Enemy**
5. 위치: 테스트 바닥 위, 플레이어 스폰에서 몇 유닛 떨어진 곳

### 4-B. ExclamationIcon 자식 오브젝트 (! 아이콘)

1. MeleeEnemy 우클릭 → `Create Empty` → 이름: **ExclamationIcon**
2. `Add Component > SpriteRenderer`
   - Sprite: 임의의 작은 스프라이트 (기본 Unity 스프라이트 가능)
   - Color: **Yellow** (RGB 1, 1, 0)
   - Order in Layer: **10**
3. 로컬 위치: `(0, 1.5, 0)` — 적 머리 위

### 4-C. MeleeHitbox 자식 오브젝트

1. MeleeEnemy 우클릭 → `Create Empty` → 이름: **MeleeHitbox**
2. `Add Component > BoxCollider2D`
   - isTrigger: **true**
   - Size: `(1.5, 1.0)`
   - Offset: `(0.75, 0)` — 전방 약간 오프셋
3. Layer: **Enemy** (10)

### 4-D. Inspector 필드 연결

MeleeEnemy 컴포넌트의 Inspector에서:

| 필드 | 연결 대상 |
|---|---|
| `_exclamationIcon` | ExclamationIcon의 SpriteRenderer |
| `_meleeHitbox` | MeleeHitbox의 BoxCollider2D |

`Ctrl+S` 저장

---

## Step 5: Projectile 프리팹 생성

### 5-A. Projectile 오브젝트 생성

1. Hierarchy → `Create Empty` → 이름: **Projectile**
2. 컴포넌트 추가:

| 컴포넌트 | 설정 |
|---|---|
| **Rigidbody2D** | Body Type: Dynamic, Gravity Scale: 0, Collision Detection: Continuous, Interpolation: Interpolate, Freeze Rotation Z |
| **CircleCollider2D** | Radius: 0.15, isTrigger: **true** |
| **ProjectileController** (스크립트) | speed: 10, maxDistance: 20 |
| **SpriteRenderer** | Color: Orange 또는 Yellow (placeholder) |

3. Layer: **EnemyProjectile** (11)

### 5-B. 프리팹으로 저장

1. Project 창에서 `Assets/Prefabs/` 폴더 생성 (없을 경우)
2. Hierarchy의 Projectile 오브젝트를 `Assets/Prefabs/` 폴더로 드래그
3. Hierarchy에서 Projectile 인스턴스 **삭제**

---

## Step 6: RangedEnemy 오브젝트 생성

### 6-A. 루트 오브젝트

1. Hierarchy → `Create Empty` → 이름: **RangedEnemy**
2. 컴포넌트 추가:

| 컴포넌트 | 설정 |
|---|---|
| **RangedEnemy** (스크립트) | — |
| **Rigidbody2D** | Body Type: Kinematic, Collision Detection: Continuous, Freeze Rotation Z |
| **CapsuleCollider2D** | isTrigger: false, Size: (0.8, 1.2) |
| **SpriteRenderer** | Color: Blue 또는 Purple (MeleeEnemy와 구분) |
| **LineRenderer** | 코드에서 자동 설정됨 — 컴포넌트만 추가 |

3. Layer: **Enemy** (10)
4. Tag: **Enemy**
5. 위치: 테스트 바닥 위, 플레이어 스폰에서 8~12 유닛 거리 (MeleeEnemy 반대편)

### 6-B. FirePoint 자식 오브젝트

1. RangedEnemy 우클릭 → `Create Empty` → 이름: **FirePoint**
2. 로컬 위치: `(0.5, 0, 0)` — 전방 약간 오프셋

### 6-C. Inspector 필드 연결

RangedEnemy 컴포넌트의 Inspector에서:

| 필드 | 연결 대상 |
|---|---|
| `projectilePrefab` | Assets/Prefabs/Projectile 프리팹 |
| `firePoint` | FirePoint 자식 Transform |

`Ctrl+S` 저장

---

## Step 7: 플레이 모드 검증

### MeleeEnemy 검증 (ENMY-01)

| # | 확인 항목 | 기대 결과 |
|---|---|---|
| 1 | Idle 순찰 | 스폰 위치 기준 좌우 ~3유닛 왕복 |
| 2 | Chase 전환 | 플레이어가 ~10유닛 이내 접근 시 추적 시작 |
| 3 | Telegraph (핵심) | `!` 아이콘이 **0.8초** 실시간 표시, 이 시간 안에 구르기로 피할 수 있음 |
| 4 | 공격 히트 | 구르기 없이 서 있으면 플레이어 비활성화 + 콘솔 "Player died" |
| 5 | 대시 킬 | 플레이어 대시로 적 즉사, FEEL-01 히트프리즈 (~75ms) 발동 |
| 6 | 플레이어 사망 반응 | 플레이어 사망 후 적이 Idle 복귀 |

### RangedEnemy 검증 (ENMY-02)

| # | 확인 항목 | 기대 결과 |
|---|---|---|
| 1 | 정지 상태 | moveSpeed=0이므로 움직이지 않음 |
| 2 | 조준선 (핵심) | 플레이어 접근 시 빨간 선이 0→1 알파로 **0.8초** 동안 페이드인 |
| 3 | 투사체 발사 | 0.8초 후 조준선 방향으로 투사체 발사 |
| 4 | 투사체 Platform 충돌 | Platform 레이어 접촉 시 투사체 소멸 |
| 5 | 투사체 플레이어 킬 | 투사체 맞으면 플레이어 비활성화 + "Player died" |
| 6 | 구르기 회피 | 구르기 중(PlayerInvincible) 투사체 통과 |
| 7 | 대시 킬 | 플레이어 대시로 적 즉사, FEEL-01 발동 |
| 8 | 플레이어 사망 반응 | 플레이어 사망 후 적이 Idle 복귀, 조준선 숨김 |

---

## 레이어 참조표

| Index | Name | 용도 |
|---|---|---|
| 7 | PlayerHurtbox | 플레이어 일반 히트박스 |
| 8 | PlayerInvincible | 구르기/대시 중 무적 상태 |
| 9 | Platform | 발판 |
| 10 | Enemy | 적 본체 및 근접 히트박스 |
| 11 | EnemyProjectile | 원거리 투사체 |

---

## 트러블슈팅

**`PlayerDeathHandler`가 두 번 발동한다**  
→ Domain Reload가 비활성화된 경우 발생할 수 있음. `OnDisable`에서 이벤트 구독 해제하는지 확인.

**MeleeEnemy가 플레이어를 감지하지 못한다**  
→ Player 오브젝트의 `PlayerHurtbox` 콜라이더 레이어가 7번인지 확인. Physics2D 매트릭스에서 Enemy × PlayerHurtbox 충돌이 활성화되어 있는지 확인.

**투사체가 적에게 반응한다**  
→ Physics2D 매트릭스에서 EnemyProjectile × Enemy 체크가 해제되어 있는지 확인.

**RangedEnemy 조준선이 보이지 않는다**  
→ LineRenderer 컴포넌트가 추가되어 있는지 확인. Material이 없으면 `Default-Line` 마테리얼 할당.

**`projectilePrefab not assigned` 경고**  
→ RangedEnemy Inspector에서 `projectilePrefab` 필드에 Projectile 프리팹이 할당되어 있는지 확인.
