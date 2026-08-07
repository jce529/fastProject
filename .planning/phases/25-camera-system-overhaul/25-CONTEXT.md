# Phase 25: 카메라 시스템 고도화 - Context

**Gathered:** 2026-08-07
**Status:** Ready for planning

<domain>
## Phase Boundary

기존 `CameraFollow.cs`(LateUpdate 직접 추적, lead-ahead 없음 — 원래 D-11/D-12/D-13에서 의도적으로 배제)와 `CameraBound.cs`(하드 클램프) 위에 다음 5개 기능을 추가한다:

1. 에임 기반 카메라 오프셋 (마우스 방향으로 선제 이동)
2. SmoothDamp 부드러운 추적
3. 속도/거리 기반 다이나믹 줌아웃 (비대칭 댐핑, 최대 배율 제한)
4. 거리 비례 텐션 스케일링 (캐치업 안전장치)
5. 히트스탑 중 독립 카메라 셰이크 (`Time.unscaledDeltaTime` 기반)

목표: 연쇄 돌진(반복 대시 처치) 전투에서 화면 이탈 없이 속도감과 타격감을 극대화.

이 5개는 ROADMAP.md에 이미 확정된 항목 — WHETHER가 아니라 HOW만 논의 대상.

</domain>

<decisions>
## Implementation Decisions

### 에임 리드 오프셋 (Aim Lead Offset)
- **D-01:** 항상 적용 — 평상시 이동 중에도 마우스 방향으로 카메라가 은근히 밀림 (슬로우모션/조준 중에만 적용하는 방식은 채택하지 않음)
- **D-02:** 강도는 뚜렷하게 — 화면 크기의 약 15~25%
- **D-03:** 리드 오프셋 자체에 별도의 SmoothDamp 파라미터를 두고 인스펙터에서 조정 가능하게 구현. 기본값은 0(=즉시 반응, 지연 없음). 카메라 위치 추적용 SmoothDamp(전체 카메라 이동)와는 독립된 파라미터.
- **D-04:** 대시(돌진) 실행 시작과 동시에 리드 오프셋 해제(0으로) — 대시 중에는 목적지 방향으로만 자연스럽게 추적. 대시 종료 후 리드 오프셋 재개.

### 다이나믹 줌아웃 (Dynamic Zoom-out)
- **D-05:** 트리거 기준은 거리 + 속도 결합 — 다음 타겟까지의 거리와 순간 이동 속도를 모두 반영해 줌아웃 정도를 계산.
- **D-06:** 최대 배율 제한은 약간 — 현재 `roomOrthoSize = 7f` → 최대 약 9까지만 확장. 룸 뷰 규격을 크게 벗어나지 않는 범위.
- **D-07:** 비대칭 댐핑 — 줌아웃(넓어짐)은 빠르게, 줌인(원상 복귀)은 느리게.
- **D-08:** 복귀 타이밍 — 히트프리즈(`OverclockModule.HitFreeze`) 종료 직후 즉시 줌인 시작.

### 텐션 캐치업 안전장치 (Tension Catch-up Safety)
- **D-09:** 항상 개입하는 배경 세이프티가 아니라, 임계 조건 초과 시에만 개입. 구체적으로: 화면을 좌→우 4등분(1~4구간)했을 때, 추적 대상이 1구간(왼쪽 바깥쪽 25%) 또는 4구간(오른쪽 바깥쪽 25%)에 진입하는 순간부터 텐션 캐치업이 개입 시작.
- **D-10:** 강도 곡선은 지수형/가속 — 화면 경계에 가까워질수록 캐치업 강도가 급격히 강해짐 (선형이 아님).

### Claude's Discretion
- 히트스탑 중 독립 카메라 셰이크의 강도/스타일 재조정 여부 — 이번 논의에서 3개 영역만 선택되어 이 영역은 다루지 않음. 기존 `CameraFollow.Shake()`(`BossEnemyBase.cs:105`, `OverclockModule.cs:95`에서 호출)는 이미 `Time.unscaledDeltaTime` 기반으로 구현되어 있어 즉시 재사용 가능 — 값 재조정 필요 여부는 researcher/planner 판단.
- ROADMAP.md 문구에 등장하는 `Time.timeScale=0.01` — 현재 `OverclockModule.HitFreeze()`는 `Time.timeScale=0f`를 사용 중(D-04, 18-CONTEXT.md 계승). 0f→0.01f로 실제 변경할지, 로드맵 문구가 개념적 표현이었는지는 이번 논의에서 다루지 않음 — researcher가 기존 HitFreeze 계약을 건드리지 않는 방향을 기본으로 검토.
- `DebugSceneCameraBinder.cs`(DebugScene 전용 최소 `SnapToRoom` 바인더)가 신규 API(SmoothDamp/줌/리드오프셋)와 호환되도록 갱신이 필요한지 — 명시적 논의 없음. Phase 18.1에서 이미 한 차례 카메라 미배선 버그가 있었으므로, 신규 `CameraFollow` API 변경 시 `DebugSceneCameraBinder` 호출부가 깨지지 않는지 planner가 확인 필요.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase 정의
- `.planning/ROADMAP.md` (Phase 25 section) — 5개 확정 기능 목록, 의존성(Phase 18)

No external ADR/spec docs referenced during discussion — 결정 사항은 위 `<decisions>`에 전부 캡처됨.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Assets/Scripts/Camera/CameraFollow.cs` — 현재 LateUpdate 직접 추적(SmoothDamp 아님), `offset` 필드(0,1,-10), `roomOrthoSize=7f`, `SnapToRoom(Vector3)`/`SnapToRoom(Bounds)` 두 오버로드, `Shake(duration, amplitude)`가 이미 `Time.unscaledDeltaTime` 기반으로 감쇠 흔들림 구현.
- `Assets/Scripts/Camera/CameraBound.cs` — 룸 프리팹 자식에 부착, `GetWorldBounds()` 반환, `_size` 필드(기본 20x12).
- `Assets/Scripts/Player/Combat/CombatContext.cs` — `CameraFollow`, `CameraShakeDuration`, `CameraShakeAmplitude` 필드 이미 존재.

### Established Patterns
- 모든 타이머/감쇠는 `Time.unscaledDeltaTime` 또는 `WaitForSecondsRealtime` — 슬로우모션/히트프리즈(`Time.timeScale=0`) 면역이 전 마일스톤 공통 제약.
- `OverclockModule.HitFreeze()`(`Assets/Scripts/Player/Combat/OverclockModule.cs:111`)가 `Time.timeScale=0f`/`fixedDeltaTime=0f`를 설정하고 `WaitForSecondsRealtime`로 복원 — 이 계약을 건드리지 않는 것이 기본.
- `BossEnemyBase.cs:105`, `OverclockModule.cs:95`에서 `CameraFollow.Shake()` 호출 — 신규 기능 추가 시 이 호출부와 충돌하지 않아야 함.

### Integration Points
- `CombatController.cs`가 `_mainCamera.GetComponent<CameraFollow>()`로 참조 캐싱 후 `CombatContext`에 주입 — 신규 API도 동일 경로로 노출되어야 함.
- `Assets/Scripts/Debug/DebugSceneCameraBinder.cs` — DebugScene 전용 최소 바인더, `SnapToRoom` 1회 호출. Phase 18.1에서 카메라 미배선 버그가 발생했던 지점이므로 신규 API 도입 시 회귀 위험 지점.

</code_context>

<specifics>
## Specific Ideas

- 리드 오프셋 반응성(SmoothDamp)은 반드시 인스펙터에서 조정 가능한 별도 필드로 노출하고, 기본값은 0(즉시 반응)으로 시작 — 사용자가 플레이테스트하며 값을 올려볼 수 있도록.
- 텐션 캐치업 개입 기준은 정확히 "화면을 좌→우 4등분했을 때 1구간(왼쪽 바깥 25%) 또는 4구간(오른쪽 바깥 25%) 진입" — 거리 비율이나 orthographicSize 기반 계산이 아니라 화면 4분할 구간 기준.

</specifics>

<deferred>
## Deferred Ideas

None — 논의가 phase 범위 내에서 유지됨, scope creep 없음.

</deferred>

---

*Phase: 25-camera-system-overhaul*
*Context gathered: 2026-08-07*
