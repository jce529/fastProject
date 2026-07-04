# Unity Editor 작업 체크리스트

Unity Editor에서 직접 수행해야 하는 작업만 모아놓은 문서.
코드 작업은 포함하지 않음 — 코드는 이미 완성되어 있음.

---

## Phase 3: 적 시스템

### ✅ 완료된 코드
- `MeleeEnemy.cs` — 4-state FSM 완성
- `RangedEnemy.cs` — LineRenderer 전신 + FSM 완성
- `ProjectileController.cs` — 발사체 완성

---

### 03-03: Play Mode 검증 (T3)

Play Mode 진입 후 아래 6개 체크:

| # | 확인 사항 | 예상 결과 |
|---|-----------|-----------|
| 1 | MeleeEnemy 관찰 | 좌우 순찰 (스폰 위치 ±3유닛 내 왕복) |
| 2 | 플레이어를 10유닛 이내로 접근 | MeleeEnemy가 플레이어를 향해 이동 |
| 3 | 1.5유닛 이내 접근 후 대기 | "!" 아이콘이 0.8초간 표시 |
| 4 | 전신 0.8초 중 구르기 | 구르기 성공, 피격 없음 (i-frame 레이어) |
| 5 | 전신 중 가만히 대기 | 0.8초 후 플레이어 사망 (Console: "Player died") |
| 6 | 플레이어 대시 공격으로 처치 | MeleeEnemy 비활성화, 히트프리즈(~75ms) 발생 |

---

### 03-04: Play Mode 검증 (T4)

Play Mode 진입 후 아래 8개 체크:

| # | 확인 사항 | 예상 결과 |
|---|-----------|-----------|
| 1 | RangedEnemy 관찰 | moveSpeed=0이므로 정지 상태 |
| 2 | 플레이어를 12유닛 이내로 접근 | 빨간 조준선이 서서히 나타남 (0~1 알파, 0.8초) |
| 3 | 0.8초 후 | 조준선 사라지고 발사체 발사됨 |
| 4 | 발사체가 플랫폼에 충돌 | 발사체 소멸 |
| 5 | 발사체 경로 안에서 대기 | 플레이어 사망 (Console: "Player died") |
| 6 | 조준선 나타날 때 구르기 | 발사체 통과 (PlayerInvincible 레이어 면역) |
| 7 | 플레이어 대시 공격으로 처치 | RangedEnemy 비활성화, 히트프리즈 발생 |
| 8 | 발사체에 플레이어 사망 후 | RangedEnemy가 Idle 복귀 (죽은 플레이어 추적 중단) |

---

## Phase 4: HUD & 게임 루프

### 04-01-T3: TMP 임포트 + Canvas HUD 계층 구조 생성

**사전 조건:** `FloorManager.cs`, `HUDController.cs` 코드 작업 완료 후 진행.

---

#### Step 0 — TMP Essential Resources 임포트 (필수 최우선)

`Window > TextMeshPro > Import TMP Essential Resources`

임포트 완료 후 `Assets/TextMesh Pro/` 폴더 생성 확인.
이 작업 없이 진행하면 모든 TMP 컴포넌트가 마젠타 오류 사각형으로 표시됨.

---

#### Step 1 — Canvas 생성

Hierarchy 우클릭 → UI → Canvas.

| 속성 | 값 |
|------|----|
| Render Mode | Screen Space - Overlay |
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | 1920 x 1080 |
| Match Width Or Height | 0.5 |

GameObject 이름: `Canvas`

---

#### Step 2 — HUDPanel 생성

Canvas 우클릭 → Create Empty → 이름: `HUDPanel`

- RectTransform: anchor min=(0,0), max=(1,1), 오프셋 전부 0 (전체 스트레치)
- Image 컴포넌트 없음 — 레이아웃 컨테이너 역할만

---

#### Step 3 — FloorGroup (좌상단)

HUDPanel 우클릭 → Create Empty → 이름: `FloorGroup`

| 속성 | 값 |
|------|----|
| Anchor | Top-Left (min=(0,1), max=(0,1)) |
| Pivot | (0, 1) |
| Pos X / Y | 24, -24 |
| Width / Height | 160, 44 |
| Image Color | (0, 0, 0, 0.55) |

FloorGroup 우클릭 → UI → Text - TextMeshPro → 이름: `FloorLabel`

| 속성 | 값 |
|------|----|
| Text | `Floor 1` |
| Font Size | 28, Bold |
| Color | White (1,1,1,1) |
| Alignment | Middle Left |
| RectTransform | FloorGroup 내 8px 패딩 |

---

#### Step 4 — GaugeGroup (상단 중앙)

HUDPanel 우클릭 → Create Empty → 이름: `GaugeGroup`

| 속성 | 값 |
|------|----|
| Anchor | Top-Center (min=(0.5,1), max=(0.5,1)) |
| Pivot | (0.5, 1) |
| Pos X / Y | 0, -24 |
| Width / Height | 216, 32 |
| Image Color | (0, 0, 0, 0.55) |

GaugeGroup 우클릭 → UI → Image → 이름: `GaugeTrack`
- Color: #222222, Width 200, Height 16, 중앙 정렬

GaugeGroup 우클릭 → UI → Image → 이름: `GaugeFill`

| 속성 | 값 |
|------|----|
| Image Type | Filled |
| Fill Method | Horizontal |
| Fill Origin | Left |
| Fill Amount | 1 |
| Color | White |
| RectTransform | GaugeTrack과 동일 (Width 200, Height 16) |

---

#### Step 5 — AttackTypeGroup (우상단)

HUDPanel 우클릭 → Create Empty → 이름: `AttackTypeGroup`

| 속성 | 값 |
|------|----|
| Anchor | Top-Right (min=(1,1), max=(1,1)) |
| Pivot | (1, 1) |
| Pos X / Y | -24, -24 |
| Width / Height | 140, 44 |
| Image Color | (0, 0, 0, 0.55) |

AttackTypeGroup 우클릭 → UI → Text - TextMeshPro → 이름: `AttackTypeLabel`

| 속성 | 값 |
|------|----|
| Text | `LINEAR` |
| Font Size | 28, Bold |
| Color | White (1,1,1,1) |
| Alignment | Middle Right |
| RectTransform | AttackTypeGroup 내 8px 패딩 |

---

#### Step 6 — HUDController 연결

HUDPanel 선택 → Add Component → HUDController

| 필드 | 연결 대상 |
|------|-----------|
| `_floorLabel` | FloorLabel (TextMeshProUGUI) |
| `_gaugeFill` | GaugeFill (Image) |
| `_attackTypeLabel` | AttackTypeLabel (TextMeshProUGUI) |
| `_gauge` | Player 오브젝트의 GaugeController 컴포넌트 |

씬 저장 (Ctrl+S)

---

#### Step 7 — Play Mode 검증

| # | 확인 사항 | 예상 결과 |
|---|-----------|-----------|
| 1 | Game 뷰 좌상단 | "Floor 1" 레이블 + 반투명 배경 표시 |
| 2 | Game 뷰 상단 중앙 | 게이지 바 꽉 찬 상태로 표시 |
| 3 | Game 뷰 우상단 | "LINEAR" 레이블 표시 |
| 4 | Attack 버튼 홀드 | 게이지 바가 실시간으로 감소 |
| 5 | Game 뷰 전체 | 마젠타 오류 사각형 없음 |
| 6 | Console | TMP 관련 오류 없음 |

---

### 04-02-T2: DeathPanel 계층 구조 생성 + DeathScreenController 연결

**사전 조건:** 04-01 완료 (Canvas 존재, TMP 임포트 완료), `DeathScreenController.cs` 코드 작업 완료 후 진행.

---

#### Step 1 — DeathPanel 생성

Hierarchy에서 Canvas 우클릭 → Create Empty → 이름: `DeathPanel`

**Inspector에서 GameObject 이름 옆 활성 체크박스 해제 (비활성 상태로 시작)**

RectTransform: anchor min=(0,0), max=(1,1), 오프셋 전부 0 (전체 스트레치)

---

#### Step 2 — DeathOverlay 생성 (전체화면 어두운 배경)

DeathPanel 우클릭 → UI → Image → 이름: `DeathOverlay`

| 속성 | 값 |
|------|----|
| Color | (0, 0, 0, 0.70) — R=0, G=0, B=0, A=179 |
| RectTransform | anchor min=(0,0), max=(1,1), 오프셋 전부 0 |

---

#### Step 3 — RestartButton 생성

DeathPanel 우클릭 → UI → Button - TextMeshPro → 이름: `RestartButton`

Button 컴포넌트:
| 속성 | 값 |
|------|----|
| Transition | Color Tint |
| Normal Color | #FFFFFF |
| Highlighted Color | #CCCCCC |
| Pressed Color | #999999 |
| Navigation | None |

RectTransform:
| 속성 | 값 |
|------|----|
| Anchor | Middle-Center (min=(0.5,0.5), max=(0.5,0.5)) |
| Pivot | (0.5, 0.5) |
| Pos X / Y | 0, 0 |
| Width / Height | 240, 56 |

---

#### Step 4 — RestartLabel 설정

RestartButton 하위의 Text (TMP) 자식 → 이름: `RestartLabel`

| 속성 | 값 |
|------|----|
| Text | `RESTART` |
| Font Size | 32, Bold |
| Color | Black (0,0,0,1) |
| Alignment | Center Middle |
| RectTransform | 부모 전체 스트레치 (오프셋 전부 0) |

---

#### Step 5 — DeathScreenController 연결

Canvas 선택 → Add Component → DeathScreenController

| 필드 | 연결 대상 |
|------|-----------|
| `_deathPanel` | DeathPanel GameObject |
| `_restartButton` | RestartButton의 Button 컴포넌트 |

씬 저장 (Ctrl+S)

---

#### Step 6 — Play Mode 검증 (게임 루프 5회 반복)

| # | 확인 사항 | 예상 결과 |
|---|-----------|-----------|
| 1 | 게임 시작 | DeathPanel 비표시 (Hierarchy에서 비활성 확인) |
| 2 | MeleeEnemy 또는 RangedEnemy에 피격 | DeathPanel 나타남 — 어두운 오버레이 + RESTART 버튼 중앙 표시 |
| 3 | 피격 후 게임 월드 | 물리 정지 (Time.timeScale=0) |
| 4 | 피격 후 HUDPanel | 오버레이 뒤에서 계속 표시 |
| 5 | RESTART 버튼 클릭 | 씬 리로드, "Floor 1", 게이지 꽉 참, DeathPanel 비표시 |
| 6 | 5회 사망→재시작 반복 | 오류 없이 완료, 개발자 개입 불필요 |

---

*작성: 2026-06-16*
*03-03 T1, 03-04 T1+T2 코드 완성 기준으로 작성*
