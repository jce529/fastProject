# Phase 25: 카메라 시스템 고도화 - Research

**Researched:** 2026-08-07
**Domain:** Unity 6 URP 2D orthographic camera control (SmoothDamp-based follow, look-ahead, dynamic zoom, screen-space catch-up, hitstop-immune shake)
**Confidence:** HIGH (all findings grounded in direct reads of the actual project code + verified Unity API behavior)

## Summary

This phase extends the existing `CameraFollow.cs` (currently a bare LateUpdate direct-follow with hard clamp) with five layered behaviors: aim-lead offset, SmoothDamp tracking, distance/speed-driven dynamic zoom, screen-quartile tension catch-up, and hitstop-immune shake (already implemented). All five decisions (D-01 through D-10) are locked in `25-CONTEXT.md` — this research resolves *how* to implement them inside the existing single-file architecture without introducing new component types, and identifies the exact integration points in `OverclockModule.cs` / `CombatController.cs` needed to drive the aim-lead suppression during dashes and the zoom trigger.

The critical architectural finding is that **all five systems can be composed inside `CameraFollow.LateUpdate()` in a fixed order without fighting each other**: (1) aim-lead offset is smoothed first via its own `SmoothDamp` producing a "virtual look target", (2) tension catch-up modifies the *effective smoothTime* fed into the main position `SmoothDamp` for that frame only (not a second position write), (3) the main position `SmoothDamp` runs once using the look target, (4) zoom is a fully independent `Mathf.SmoothDamp` on `orthographicSize` that MUST run *before* the existing bounds-clamp block (since clamp math reads `_camera.orthographicSize`), (5) shake stays last and additive, exactly as today. This avoids two systems writing to `transform.position` in the same frame.

A second critical finding: the player's `Rigidbody2D.linearVelocity` is **not usable** as the "instantaneous speed" signal during a dash, because `OverclockModule.Resolve()` explicitly zeroes it (`ctx.Rb.linearVelocity = Vector2.zero`) and moves the player via `Rigidbody2D.MovePosition()` in a coroutine — velocity never reflects dash speed. The zoom trigger must be driven explicitly from `OverclockModule.Resolve()` (the same call site that already calls `ctx.CameraFollow?.Shake(...)`), using the known dash distance and `ctx.DashDuration` to derive speed, not a per-frame velocity read.

A third finding, backed by actual `CameraBound` prefab data (see Runtime State Inventory-equivalent table below): most rooms are already wider than the current 7-unit `orthoSize` view in at least one axis, so parts of the level already show background clear-color beyond room bounds today. Raising `roomOrthoSize` toward 9 makes this modestly worse but does **not** introduce a new failure class — it is a matter of degree, not a new bug.

**Primary recommendation:** Add all new fields/methods directly to `CameraFollow.cs` (no new MonoBehaviour types). Drive aim-lead entirely inside `CameraFollow` using its own cached `Camera` + `Mouse.current` (no CombatContext dependency). Add exactly two new public hook methods to `CameraFollow` — `SetAimLeadSuppressed(bool)` and `RequestDynamicZoom(float distance, float speed)` (name TBD by planner) — called from `OverclockModule.Resolve()` at dash start / dash-movement-end / post-HitFreeze. Leave `SnapToRoom(Vector3)` / `SnapToRoom(Bounds)` signatures untouched so `DebugSceneCameraBinder.cs` requires zero changes.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**에임 리드 오프셋 (Aim Lead Offset)**
- **D-01:** 항상 적용 — 평상시 이동 중에도 마우스 방향으로 카메라가 은근히 밀림 (슬로우모션/조준 중에만 적용하는 방식은 채택하지 않음)
- **D-02:** 강도는 뚜렷하게 — 화면 크기의 약 15~25%
- **D-03:** 리드 오프셋 자체에 별도의 SmoothDamp 파라미터를 두고 인스펙터에서 조정 가능하게 구현. 기본값은 0(=즉시 반응, 지연 없음). 카메라 위치 추적용 SmoothDamp(전체 카메라 이동)와는 독립된 파라미터.
- **D-04:** 대시(돌진) 실행 시작과 동시에 리드 오프셋 해제(0으로) — 대시 중에는 목적지 방향으로만 자연스럽게 추적. 대시 종료 후 리드 오프셋 재개.

**다이나믹 줌아웃 (Dynamic Zoom-out)**
- **D-05:** 트리거 기준은 거리 + 속도 결합 — 다음 타겟까지의 거리와 순간 이동 속도를 모두 반영해 줌아웃 정도를 계산.
- **D-06:** 최대 배율 제한은 약간 — 현재 `roomOrthoSize = 7f` → 최대 약 9까지만 확장. 룸 뷰 규격을 크게 벗어나지 않는 범위.
- **D-07:** 비대칭 댐핑 — 줌아웃(넓어짐)은 빠르게, 줌인(원상 복귀)은 느리게.
- **D-08:** 복귀 타이밍 — 히트프리즈(`OverclockModule.HitFreeze`) 종료 직후 즉시 줌인 시작.

**텐션 캐치업 안전장치 (Tension Catch-up Safety)**
- **D-09:** 항상 개입하는 배경 세이프티가 아니라, 임계 조건 초과 시에만 개입. 구체적으로: 화면을 좌→우 4등분(1~4구간)했을 때, 추적 대상이 1구간(왼쪽 바깥쪽 25%) 또는 4구간(오른쪽 바깥쪽 25%)에 진입하는 순간부터 텐션 캐치업이 개입 시작.
- **D-10:** 강도 곡선은 지수형/가속 — 화면 경계에 가까워질수록 캐치업 강도가 급격히 강해짐 (선형이 아님).

### Claude's Discretion
- 히트스탑 중 독립 카메라 셰이크의 강도/스타일 재조정 여부 — 이번 논의에서 3개 영역만 선택되어 이 영역은 다루지 않음. 기존 `CameraFollow.Shake()`(`BossEnemyBase.cs:105`, `OverclockModule.cs:95`에서 호출)는 이미 `Time.unscaledDeltaTime` 기반으로 구현되어 있어 즉시 재사용 가능 — 값 재조정 필요 여부는 researcher/planner 판단.
- ROADMAP.md 문구에 등장하는 `Time.timeScale=0.01` — 현재 `OverclockModule.HitFreeze()`는 `Time.timeScale=0f`를 사용 중(D-04, 18-CONTEXT.md 계승). 0f→0.01f로 실제 변경할지, 로드맵 문구가 개념적 표현이었는지는 이번 논의에서 다루지 않음 — researcher가 기존 HitFreeze 계약을 건드리지 않는 방향을 기본으로 검토.
- `DebugSceneCameraBinder.cs`(DebugScene 전용 최소 `SnapToRoom` 바인더)가 신규 API(SmoothDamp/줌/리드오프셋)와 호환되도록 갱신이 필요한지 — 명시적 논의 없음. Phase 18.1에서 이미 한 차례 카메라 미배선 버그가 있었으므로, 신규 `CameraFollow` API 변경 시 `DebugSceneCameraBinder` 호출부가 깨지지 않는지 planner가 확인 필요.

### Deferred Ideas (OUT OF SCOPE)
None — 논의가 phase 범위 내에서 유지됨, scope creep 없음.
</user_constraints>

<phase_requirements>
## Phase Requirements

No formal REQ-IDs exist for this phase (ROADMAP.md lists `Requirements: TBD`). Per the phase brief, the `<decisions>` section of `25-CONTEXT.md` (D-01 through D-10) IS the requirements anchor. Mapping below:

| ID | Description | Research Support |
|----|-------------|-------------------|
| D-01 | Aim-lead always active, not gated by slow-mo/aim state | Self-contained in `CameraFollow` — reads `Mouse.current` directly, no combat-state dependency needed. See Architecture Patterns → Pattern 1. |
| D-02 | Lead magnitude ≈ 15–25% of screen size | Formula derived from `orthographicSize * aspect` (half-width) in Code Examples. Expose as inspector `[Range]` percent field. |
| D-03 | Lead offset has its own SmoothDamp, independent of position SmoothDamp, default 0 (instant) | Verified: Unity's `Vector3.SmoothDamp`/`Mathf.SmoothDamp` internally clamps `smoothTime` to `Mathf.Max(0.0001f, smoothTime)` — a 0 value is safe, produces near-instant response, no NaN/divide-by-zero. See Sources. |
| D-04 | Lead offset suppressed at dash start, resumed at dash end | Requires new hook from `OverclockModule.Resolve()` — no existing dash-state signal is exposed today. See Don't Hand-Roll / Code Examples → Dash Integration Hook. |
| D-05 | Zoom trigger = distance + speed combined | `Rigidbody2D.linearVelocity` is NOT usable during dash (explicitly zeroed, movement is `MovePosition`-driven). Must compute from dash distance/duration at the `Resolve()` call site. See Common Pitfalls → Pitfall 1. |
| D-06 | Max zoom ≈ 9 (from `roomOrthoSize=7`) | Verified against actual `CameraBound` prefab sizes across 26 rooms/corridors — see Runtime State Inventory-equivalent table. |
| D-07 | Asymmetric damping (zoom-out fast, zoom-in slow) | Two separate `smoothTime` constants fed into one `Mathf.SmoothDamp` call, branch on `target > current` vs `target < current`. See Code Examples. |
| D-08 | Zoom-in starts immediately after HitFreeze ends | `OverclockModule.Resolve()` already `yield return HitFreeze(...)` synchronously — natural call site for an explicit "release zoom" hook right after the yield returns. |
| D-09 | Tension catch-up triggers only outside screen quartiles 1/4 | Compute via `Camera.WorldToViewportPoint(target.position).x` against 0.25/0.75 thresholds — fully self-contained in `CameraFollow`, no external dependency. |
| D-10 | Catch-up strength curve is exponential, not linear | Standard technique: exponentiate the normalized overshoot distance (e.g. `Mathf.Pow(overshoot01, exponent)` or `1 - Mathf.Exp(-k * overshoot01)`) to bias `smoothTime` down as target nears/crosses the boundary. |

</phase_requirements>

## Standard Stack

No new packages required. This phase is pure `UnityEngine` API usage on top of already-installed packages.

### Core
| API | Version | Purpose | Why Standard |
|-----|---------|---------|---------------|
| `Vector3.SmoothDamp` / `Mathf.SmoothDamp` | UnityEngine 6000.3.11f1 (stable since Unity 2.x) | Critically-damped spring-like interpolation for position and orthoSize | Industry-standard Unity camera smoothing primitive — deterministic, frame-rate independent, no external dependency, matches D-03/D-07 language ("SmoothDamp") verbatim |
| `UnityEngine.InputSystem.Mouse.current` | com.unity.inputsystem 1.19.0 | Aim direction source | Already the exact pattern used in `OverclockModule.GetMouseWorldDirection()` and `RangeDisplay.cs` — reuse, don't reinvent |
| `Camera.WorldToViewportPoint` | UnityEngine 6000.3.11f1 | Screen-quartile detection for tension catch-up (D-09) | Returns normalized [0,1] viewport coords regardless of resolution/aspect — exactly matches "화면을 좌→우 4등분" requirement without hand-rolled screen math |

### Supporting
None — no additional libraries needed.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hand-rolled `SmoothDamp` composition (this research's recommendation) | Cinemachine (`com.unity.cinemachine`) | Cinemachine's `CinemachineFollow` + `CinemachineTargetGroup`/damping + `Impulse` extension could deliver look-ahead, damping, dynamic zoom (via `CinemachineOrbitalFollow`/lens FOV rigs) and shake out of the box. **Not adopted**: package is not currently installed (would require a new dependency + `manifest.json` change), the project has a hard existing constraint of "no Cinemachine" baked into `CameraFollow.cs`'s own doc comment ("Direct LateUpdate camera follow -- no Cinemachine"), and CLAUDE.md mandates minimal surface area / no premature abstraction for a prototype. Flagged as a future option if camera work expands beyond this phase, not for this phase. |

**Installation:** None — no `npm`/`upm` package changes needed for this phase.

**Version verification:** Not applicable (no new package versions to verify) — `com.unity.inputsystem` 1.19.0 already locked in `Packages/packages-lock.json`, used unchanged.

## Architecture Patterns

### Recommended Project Structure
No new files. All changes land in the two existing files:
```
Assets/Scripts/Camera/
├── CameraFollow.cs   # + aim-lead fields/logic, zoom fields/logic, tension catch-up logic, 2 new public hooks
└── CameraBound.cs    # unchanged (GetWorldBounds() already sufficient)
```
Two call sites gain new (additive) calls:
```
Assets/Scripts/Player/Combat/OverclockModule.cs   # Resolve(): dash-start suppress hook, dash-end zoom-trigger + lead-resume, post-HitFreeze zoom-release hook
```

### Pattern 1: Self-Contained Aim-Lead (no CombatContext dependency)
**What:** `CameraFollow` reads `Mouse.current` and its own cached `_camera` directly in `LateUpdate()`, exactly mirroring the existing `OverclockModule.GetMouseWorldDirection()` technique — no reference to `CombatContext`/`CombatController` needed for D-01/D-02/D-03.
**When to use:** Always (D-01: aim-lead is unconditional).
**Example:**
```csharp
// Pattern already proven in this codebase — OverclockModule.cs:134-141 and RangeDisplay.cs:98-99
private Vector2 GetMouseWorldDirection()
{
    var mouse = UnityEngine.InputSystem.Mouse.current;
    if (mouse == null || _camera == null) return Vector2.zero;
    Vector2 mouseScreen = mouse.position.ReadValue();
    Vector3 mouseWorld = _camera.ScreenToWorldPoint(
        new Vector3(mouseScreen.x, mouseScreen.y, Mathf.Abs(_camera.transform.position.z)));
    Vector2 dir = (Vector2)mouseWorld - (Vector2)target.position;
    return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.zero;
}
```
Only D-04 (suppress during dash) requires an external signal — everything else is self-contained.

### Pattern 2: Two-Stage SmoothDamp Composition (lead offset → position)
**What:** Smooth the *lead offset* itself first (its own velocity ref, own smoothTime, default 0), producing a "virtual look target" (`target.position + baseOffset + smoothedLeadOffset`). Then feed that virtual target into the existing main position `SmoothDamp`. This satisfies D-03's explicit requirement of two independent SmoothDamp params without two competing writes to `transform.position`.
**When to use:** Every `LateUpdate()`, replacing the current direct assignment `transform.position = desired`.
**Example:**
```csharp
// Source: Unity Vector3.SmoothDamp docs (https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Vector3.SmoothDamp.html)
// Stage 1: smooth the lead offset toward its target (own velocity ref + own smoothTime)
Vector2 leadTarget = _leadSuppressed ? Vector2.zero : GetMouseWorldDirection() * ComputeLeadMagnitude();
_smoothedLead = Vector2.SmoothDamp(_smoothedLead, leadTarget, ref _leadVelocity, leadSmoothTime);

// Stage 2: main tracking SmoothDamp toward target + base offset + smoothed lead
Vector3 desired = target.position + offset + (Vector3)_smoothedLead;
float effectiveSmoothTime = ComputeTensionAdjustedSmoothTime(desired); // D-09/D-10 hook — see Pattern 3
transform.position = Vector3.SmoothDamp(transform.position, desired, ref _positionVelocity, effectiveSmoothTime);
// NOTE: bounds clamp (existing _hasBounds branch) must still run AFTER this, operating on the
// SmoothDamp'd position rather than overwriting it outright with Mathf.Clamp on `desired`.
```

### Pattern 3: Tension Catch-up as a smoothTime Modifier, Not a Second Position Write
**What:** Rather than adding a second corrective position offset (which would fight the main SmoothDamp), compute a per-frame `effectiveSmoothTime` that shrinks (faster catch-up) as the *target* (not the camera) crosses into viewport quartile 1 or 4, using an exponential curve on the overshoot amount.
**When to use:** Every `LateUpdate()`, feeding into Pattern 2's Stage 2 call.
**Example:**
```csharp
// D-09: quartile check via WorldToViewportPoint — resolution/aspect independent
private float ComputeTensionAdjustedSmoothTime(Vector3 desiredWorldPos)
{
    if (_camera == null) return positionSmoothTime;
    Vector3 vp = _camera.WorldToViewportPoint(target.position); // NOTE: use target, not desired-camera-pos
    float overshoot = 0f;
    if (vp.x < 0.25f) overshoot = (0.25f - vp.x) / 0.25f;       // 0..1 as target approaches/exceeds left edge
    else if (vp.x > 0.75f) overshoot = (vp.x - 0.75f) / 0.25f;  // 0..1 for right edge
    overshoot = Mathf.Clamp01(overshoot);
    if (overshoot <= 0f) return positionSmoothTime;

    // D-10: exponential/accelerating curve, not linear
    float catchUpStrength = Mathf.Pow(overshoot, tensionExponent); // e.g. tensionExponent = 2-3
    return Mathf.Lerp(positionSmoothTime, minCatchUpSmoothTime, catchUpStrength);
}
```

### Pattern 4: Independent Asymmetric Zoom SmoothDamp — Must Run Before Bounds Clamp
**What:** `orthographicSize` is smoothed via its own `Mathf.SmoothDamp` call with two different `smoothTime` constants depending on zoom direction (D-07). This MUST execute before the existing `_hasBounds` clamp block in `LateUpdate()`, because that block reads `_camera.orthographicSize` (via `halfH`/`halfW`) to compute the clamp rectangle — using last frame's stale size would make the clamp lag one frame behind the actual rendered view.
**When to use:** Every `LateUpdate()`, before the position-clamp block.
**Example:**
```csharp
// D-06/D-07: asymmetric damping — zoom-out fast, zoom-in slow
float smoothTimeForDirection = (_zoomTargetSize > _camera.orthographicSize) ? zoomOutSmoothTime : zoomInSmoothTime;
_camera.orthographicSize = Mathf.SmoothDamp(
    _camera.orthographicSize, _zoomTargetSize, ref _zoomVelocity, smoothTimeForDirection);
// ^ must happen BEFORE: float halfH = _camera.orthographicSize; (existing clamp block)
```

### Anti-Patterns to Avoid
- **Two separate `transform.position` writes in the same `LateUpdate` (e.g. main SmoothDamp followed by a corrective tension-catchup offset added afterward):** causes visible jitter/fighting between systems on frames where both are active. Use Pattern 3's smoothTime-modifier approach instead — one write per frame.
- **Reading `Rigidbody2D.linearVelocity` for dash speed:** returns stale/zero data during `OverclockModule.Resolve()` because the coroutine explicitly zeroes velocity and moves via `MovePosition`. See Common Pitfalls → Pitfall 1.
- **New MonoBehaviour type for zoom/lead/tension:** unnecessary indirection for a prototype-scope phase per CLAUDE.md — all state fits naturally as private fields on the existing single-camera `CameraFollow`.
- **Coupling `CameraFollow` to `CombatContext`/`CombatController` for aim direction:** unnecessary — `Mouse.current` + the camera's own `Camera` reference is sufficient and matches D-01 (always active, not combat-state-gated).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|--------------|-----|
| Position/zoom easing | Custom Lerp-with-a-fixed-t-per-frame or exponential decay math | `Vector3.SmoothDamp` / `Mathf.SmoothDamp` | Frame-rate independent, critically-damped (no overshoot/oscillation unlike naive spring math), already the exact primitive named in D-03/D-07 |
| Screen-quartile detection | Manual `Screen.width`/`Screen.height` pixel math against camera-to-target vector projection | `Camera.WorldToViewportPoint` | Built-in normalized [0,1] viewport space handles orthographic size, aspect ratio, and resolution changes automatically — manual pixel math would break if `defaultScreenOrientation`/resolution changes |
| Mouse-to-world aim direction | New conversion logic | Copy the exact pattern already in `OverclockModule.GetMouseWorldDirection()` / `RangeDisplay.cs:98-99,123-124` | Third implementation of the same 5-line conversion in the codebase would be inconsistent risk — these two existing call sites already handle the `Mouse.current == null` edge case correctly |

**Key insight:** Every "new" capability this phase needs (smoothing, viewport-space geometry, mouse aim) already has either a Unity built-in or an existing in-repo pattern. The only genuinely new code is the *composition* of these primitives inside one `LateUpdate`, and the two small hook methods needed to signal dash start/end across from `OverclockModule` to `CameraFollow`.

## Common Pitfalls

### Pitfall 1: Rigidbody2D velocity is not a valid dash-speed signal
**What goes wrong:** If the zoom trigger reads `ctx.Rb.linearVelocity.magnitude` (or `target.GetComponent<Rigidbody2D>().linearVelocity`) expecting it to reflect the dash's speed, the zoom will never trigger during dashes (or will show stale pre-dash velocity).
**Why it happens:** `OverclockModule.Resolve()` line 72 explicitly does `ctx.Rb.linearVelocity = Vector2.zero;` immediately before moving the player via `ctx.Rb.MovePosition(...)` inside a `while` loop keyed on `elapsed/ctx.DashDuration`. `MovePosition` does not update `linearVelocity` — it's a kinematic-style teleport-with-interpolation, not a physics-integrated movement.
**How to avoid:** Compute dash "speed" explicitly at the `Resolve()` call site as `Vector2.Distance(startPos, destination) / ctx.DashDuration`, and pass both distance and this derived speed into the new `RequestDynamicZoom(distance, speed)` hook. Also note: `CombatController.Update()` (where continuous player-walking velocity IS meaningful, e.g. `_rb.linearVelocity` while not attacking) is itself skipped entirely while `_isBusy == true` (line 145 early-return) — so there is no frame during the dash where `CombatController.Update()` runs at all. This confirms the dash-resolve call site is the only viable trigger point, not a per-frame poll.
**Warning signs:** Zoom never engages during actual dash chains, or engages based on unrelated player-walk-speed instead of dash intensity.

### Pitfall 2: Zoom SmoothDamp ordering relative to the existing bounds-clamp block
**What goes wrong:** If the zoom `SmoothDamp` on `orthographicSize` runs *after* the existing clamp block (current lines 67-81 of `CameraFollow.cs`), the clamp rectangle (`halfW`/`halfH`) is computed from the *previous* frame's `orthographicSize`, causing the clamped camera position to visibly lag/pop relative to the actual rendered zoom level by one frame — most noticeable during the fast zoom-out (D-07 asymmetric damping makes this transition rapid, so the lag is more visible than it would be with slow uniform damping).
**Why it happens:** `LateUpdate()` executes top-to-bottom; `_camera.orthographicSize` is read, not just written, by the clamp math.
**How to avoid:** Perform the zoom `SmoothDamp` update as the *first* operation in `LateUpdate()`, before the existing `desired`/clamp block.
**Warning signs:** Visible one-frame "pop" or jitter in the clamp boundary specifically during fast zoom transitions.

### Pitfall 3: `roomOrthoSize` increase interacts with rooms/corridors already at or near their bound size
**What goes wrong:** Assuming a uniform "room view fits camera" invariant everywhere, then being surprised when max-zoom (9) shows background clear-color beyond level geometry.
**Why it happens:** Confirmed by direct inspection of all 26 `CameraBound` prefab instances in the project (see table below) — most rooms are already wider than the *current* 7-unit view in at least one axis, meaning the existing `SnapToRoom(Bounds)` clamp branch (`size.x <= halfW*2` → center-lock, no panning) already activates in many rooms today at `orthoSize=7`. Raising toward 9 makes the center-locked axis's overshoot larger (more background visible past room art), but does not newly break anything that wasn't already partially exposed.
**How to avoid:** Do not treat this as a blocking bug to "fix" (out of scope — would require re-authoring room art/backgrounds, which the phase does not call for). Instead: (a) confirm the camera's `m_BackgroundColor` (currently `(0.192, 0.302, 0.475, 0)` — a dark blue, likely acceptable as "void" framing rather than an obviously broken visual), and (b) consider whether `RequestDynamicZoom` should clamp its target against a per-room ceiling derived from `CameraBound`'s own `_size` (e.g. never zoom past what keeps the *room's* actual half-extent in view) — flagged as an **Open Question** below for planner/user judgment, not a research-resolved decision, since D-06 already sets a global cap of ~9 and doesn't mention per-room capping.
**Warning signs:** Playtest feedback describing zoom-out as "showing the edge of the world" or "background bleeding through" in small rooms/corridors — most likely in `Corridor_Flat` (12×10), `Corridor_Up`/`Corridor_Down` (14×10), and `Room_Debug` (20×5).

### Pitfall 4: `DebugSceneCameraBinder` breakage repeat (Phase 18.1 regression class)
**What goes wrong:** Phase 18.1 already hit a bug where `DebugScene.unity`'s camera had no `CameraFollow` wiring at all — silently placing new dash patterns off-screen. Any signature change to `SnapToRoom(Vector3)` / `SnapToRoom(Bounds)`, or a requirement that new fields be non-default to function, would silently break `DebugSceneCameraBinder.cs`'s single `Start()` call (`_cameraFollow.SnapToRoom(bound.GetWorldBounds())` or the fallback).
**Why it happens:** `DebugSceneCameraBinder` is a minimal, rarely-touched binder (one `Start()` call) that is easy to forget when `CameraFollow`'s public surface grows.
**How to avoid:** Keep `SnapToRoom(Vector3)`/`SnapToRoom(Bounds)` signatures and behavior byte-for-byte unchanged. All five new behaviors should default to inert/no-op values (lead SmoothDamp=0 already required by D-03; zoom target defaults to `roomOrthoSize`; tension/aim-lead need no explicit enable — they're always-on but harmless with a static target) so that a scene wiring only `SnapToRoom` (like `DebugSceneCameraBinder`) continues to look correct without any additional Inspector wiring.
**Warning signs:** DebugScene camera showing wrong `orthographicSize` (stuck at zoomed value) or `NullReferenceException` from a new field that assumes a `CombatContext`/`CombatController` link DebugScene doesn't provide.

## Code Examples

### Dash Integration Hook (OverclockModule.Resolve)
```csharp
// Source: existing Assets/Scripts/Player/Combat/OverclockModule.cs:53-100 (Resolve), annotated with new calls
public IEnumerator Resolve(IEnemy target, CombatContext ctx)
{
    // ...existing null-check...
    Vector2 startPos    = ctx.Rb.position;
    Vector2 destination = (Vector2)((MonoBehaviour)target).transform.position;
    float   dashDistance = Vector2.Distance(startPos, destination);
    float   dashSpeed    = ctx.DashDuration > 0f ? dashDistance / ctx.DashDuration : 0f;

    ctx.CameraFollow?.SetAimLeadSuppressed(true);          // D-04: suppress at dash start

    // ...existing sprite flip / animator / invincibility / MovePosition loop unchanged...

    ctx.Rb.MovePosition(destination);
    ctx.CameraFollow?.SetAimLeadSuppressed(false);          // D-04: resume once movement itself is done
    ctx.CameraFollow?.RequestDynamicZoom(dashDistance, dashSpeed); // D-05: trigger zoom-out with real dash data

    // ...existing OnDashHit / SFX / SpawnHitSpark / Shake unchanged...
    ctx.CameraFollow?.Shake(ctx.CameraShakeDuration, ctx.CameraShakeAmplitude);
    yield return HitFreeze(ctx.HitFreezeDuration);
    ctx.CameraFollow?.ReleaseDynamicZoom();                  // D-08: zoom-in starts immediately after HitFreeze ends

    ctx.SetAttackCooldown(ctx.PostKillLockout);
    ctx.Gauge.AddKillBonus();
}
```
This keeps `CombatContext`/`OverclockModule` as the only integration point (matches the existing `Shake()` call pattern exactly — same call site, same nullable-chaining style, same "camera is optional" contract already established for DebugScene/tests where `CameraFollow` may be null).

## State of the Art

| Old Approach | Current Approach (this phase) | When Changed | Impact |
|--------------|-------------------------------|---------------|--------|
| Direct `transform.position = desired` (instant, no easing) | `Vector3.SmoothDamp` toward a lead-adjusted, tension-adjusted target | Phase 25 | Camera motion becomes velocity-continuous instead of teleport-following; must verify existing `Shake()` still reads as an *additive* post-SmoothDamp offset (unchanged in this research's recommendation) |
| Fixed `roomOrthoSize` (7, constant) | `roomOrthoSize` becomes the *rest* zoom target; a second dynamic target (up to 9) SmoothDamp's on top | Phase 25 | `SnapToRoom` calls should still set both the rest-target AND the *current* orthoSize instantly (as today) — dynamic zoom should not fight room transitions; recommend `SnapToRoom` also reset `_zoomTargetSize = roomOrthoSize` and snap `_camera.orthographicSize` directly (bypass SmoothDamp) exactly as today, to avoid a slow zoom creep across room-to-room teleports |

**Deprecated/outdated:** None — this is additive extension of a young (Phase 18.1, 2026-07-24) implementation, not a replacement of a legacy pattern.

## Open Questions

1. **Should dynamic zoom target be capped per-room by `CameraBound._size`, in addition to the global D-06 cap of ~9?**
   - What we know: D-06 sets a single global ceiling (~9). Actual room `CameraBound` sizes range from 12×10 (`Corridor_Flat`) to 175×75 (`Room_Chain`) — see Pitfall 3 table.
   - What's unclear: CONTEXT.md D-06 doesn't mention per-room capping, and the user explicitly scoped this discussion to "룸 뷰 규격을 크게 벗어나지 않는 범위" (global, not per-room).
   - Recommendation: Implement the simple global cap (D-06, no per-room logic) first — matches the locked decision literally and is the minimal-surface option. If playtesting flags specific small rooms/corridors as visually broken at max zoom, address as a follow-up tuning pass rather than pre-emptively adding per-room clamp logic now (avoids the CLAUDE.md over-engineering trap of solving a problem not yet confirmed to be visually bad in practice).

2. **Exact timing of "대시 종료" for D-04's lead-offset resume — before or after `OnDashHit()`/`HitFreeze`?**
   - What we know: D-04 says lead resumes "대시 종료 후" (after dash ends). The dash's physical movement ends at `ctx.Rb.MovePosition(destination)` (line 84); `OnDashHit()`, SFX, hit-spark, shake, and `HitFreeze` all happen afterward, still inside the same `Resolve()` coroutine, before control returns to the player.
   - What's unclear: Whether "대시 종료" means the moment movement physically stops (my recommendation, shown in Code Examples above) or the moment the whole `Resolve()` coroutine finishes (i.e., after `HitFreeze` and lockout).
   - Recommendation: Resume immediately after `MovePosition(destination)` (movement-complete), since the player has no camera-relevant input during `HitFreeze`/lockout anyway (input is locked via `_isBusy`) — resuming early vs. late during a frozen/uncontrollable window is visually indistinguishable to the player. This is the simpler implementation and avoids adding a third call site.

3. **`Time.timeScale=0.01` vs `0f` in `HitFreeze` — does the camera work require changing this?**
   - What we know: ROADMAP.md phrase says `Time.timeScale=0.01`; actual code (`OverclockModule.HitFreeze()`) uses `0f`; CONTEXT.md explicitly defers this to researcher/planner with a bias toward "don't touch the existing contract."
   - What's unclear: Whether `0.01` was a literal roadmap typo/rounding of intent, or a real requirement (e.g. so a coroutine keyed on `Time.deltaTime` rather than `WaitForSecondsRealtime` could still advance almost-imperceptibly).
   - Recommendation: **No code change needed for this phase.** All five camera systems in this research are designed around `Time.unscaledDeltaTime` (matches the existing `Shake()` implementation and the project-wide "슬로우모션 면역" convention in CLAUDE.md/STATE.md). `Time.timeScale` being `0f` vs `0.01f` is irrelevant to `Time.unscaledDeltaTime`-driven code — camera SmoothDamp calls should use `Time.unscaledDeltaTime` (not `Time.deltaTime`) as their delta-time argument precisely so they remain unaffected by whichever value `HitFreeze` uses, now or if it's changed later independent of this phase.

## Environment Availability

Skipped — this phase has no external tool/service dependencies beyond the already-installed Unity Editor + packages (`com.unity.inputsystem`, core `UnityEngine`), which are confirmed present via `Packages/manifest.json`/`packages-lock.json` (already read in `STATE.md`/prior phases). No new installs required.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None — no `.asmdef`/PlayMode or EditMode test assembly exists under `Assets/` (verified: only `com.unity.test-framework` package-cache placeholder tests exist, no project-authored tests). `com.unity.test-framework` 1.6.0 is installed but unused by this project. |
| Config file | none — see Wave 0 |
| Quick run command | N/A — no automated harness |
| Full suite command | N/A — no automated harness |

This matches the project's established verification pattern for every prior camera/feel-sensitive phase (Phase 18, 18.1, 999.4): structured **manual playtest checklists**, executed and reported by the user, referenced in `STATE.md`/`*-SUMMARY.md`/`*-VERIFICATION.md`. Camera "feel" (lead intensity, zoom snappiness, catch-up aggressiveness) is inherently a subjective/tunable-by-playtest property, not a unit-testable one — introducing an automated test harness for this phase would be scope creep beyond what CLAUDE.md's "단순성 우선" principle allows for a prototype-stage feel-tuning phase.

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|---------------------|--------------|
| D-01/D-02/D-03 | Aim-lead always active, ~15-25% screen size, own SmoothDamp, default-0 = instant | manual-only | — (visual/feel judgment) | N/A |
| D-04 | Lead offset suppressed during dash, resumes after | manual-only | — (observe during repeated dash-chain playtest) | N/A |
| D-05/D-06/D-07/D-08 | Dynamic zoom triggers on distance+speed, caps ~9, asymmetric in/out, zoom-in starts post-HitFreeze | manual-only | — (chain-dash playtest, watch for screen-edge exposure per Pitfall 3) | N/A |
| D-09/D-10 | Tension catch-up only outside quartile 1/4, exponential curve | manual-only | — (deliberately outrun camera near room edges, observe catch-up onset/strength) | N/A |
| Regression: `DebugSceneCameraBinder` compatibility | Existing `SnapToRoom` calls still work unmodified | manual-only | — (Play DebugScene, confirm camera frames boss room correctly, per Pitfall 4) | N/A |
| Regression: `CameraBound` clamp still functions at both zoom extremes | Camera doesn't escape room bounds at orthoSize 7 or 9 | manual-only | — (playtest in `Room_Combat`/`Room_Dodge` at both zoom extremes) | N/A |

### Sampling Rate
- **Per task commit:** N/A (no automated command exists)
- **Per wave merge:** manual playtest checklist covering all rows above
- **Phase gate:** Full manual checklist pass before `/gsd:verify-work`, consistent with Phase 18.1's `18.1-VERIFICATION.md` precedent

### Wave 0 Gaps
- [ ] No test framework exists for this project — **do not introduce one for this phase**. Recommend the planner author a manual playtest checklist (mirroring `18.1-VERIFICATION.md`'s structure) as the phase's verification artifact instead of automated tests.

*(If a future phase needs automated camera-math unit tests — e.g. asserting the tension-catchup exponential formula's output range — that would require adding a first EditMode test assembly to the project, which is out of scope for Phase 25.)*

## Sources

### Primary (HIGH confidence — direct project file reads)
- `Assets/Scripts/Camera/CameraFollow.cs` — current implementation, all fields/methods/LateUpdate logic
- `Assets/Scripts/Camera/CameraBound.cs` — `GetWorldBounds()`, `_size` field
- `Assets/Scripts/Player/Combat/CombatContext.cs` — existing camera-related fields
- `Assets/Scripts/Player/Combat/OverclockModule.cs` — `Resolve()`, `HitFreeze()`, `GetMouseWorldDirection()` (mouse-aim pattern to reuse)
- `Assets/Scripts/Player/CombatController.cs` — `Update()` early-return on `_isBusy` (confirms no per-frame signal during dash), `Awake()` CombatContext wiring
- `Assets/Scripts/Enemy/Boss/BossEnemyBase.cs` — second `Shake()` call site, confirms nullable-chaining contract
- `Assets/Scripts/Debug/DebugSceneCameraBinder.cs` — exact API surface that must remain stable
- 26 `CameraBound` prefab instances under `Assets/Prefabs/Rooms/` and `Assets/Prefabs/Corridors/` — actual `_size` values (grep, direct read)
- `Assets/Scenes/DebugScene.unity` — Main Camera `m_ClearFlags: 1`, `m_BackGroundColor: {r: 0.192, g: 0.302, b: 0.475, a: 0}`
- `.planning/config.json` — confirmed `nyquist_validation: true`, `commit_docs: true`
- `.planning/phases/25-camera-system-overhaul/25-CONTEXT.md` — all locked decisions

### Secondary (MEDIUM confidence — WebSearch, cross-referenced against Unity official docs)
- [Unity Vector3.SmoothDamp docs (6000.0)](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Vector3.SmoothDamp.html) / [Mathf.SmoothDamp docs](https://docs.unity3d.com/ScriptReference/Mathf.SmoothDamp.html) — confirms `smoothTime` is internally clamped to `Mathf.Max(0.0001f, smoothTime)`, so a 0 default (D-03) is safe and produces near-instant response, not NaN/divide-by-zero
- [Prototyping a Dynamic Camera System (Game Developer)](https://www.gamedeveloper.com/design/prototyping-a-dynamic-camera-system) — general confirmation that `orthographicSize` + `SmoothDamp` is the standard 2D dynamic-zoom technique

### Tertiary (LOW confidence)
- None used as load-bearing claims — all architectural recommendations trace to either direct project code reads or the Unity official API docs above.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new dependencies, pure `UnityEngine`/`InputSystem` API already in use elsewhere in this exact codebase
- Architecture: HIGH — composition pattern derived directly from reading the existing `LateUpdate` and reasoning about actual field read/write order; verified `SmoothDamp` edge-case behavior via official docs
- Pitfalls: HIGH — Pitfall 1 (Rigidbody2D velocity) and Pitfall 2 (SmoothDamp/clamp ordering) are derived from direct code reads, not speculation. Pitfall 3 (room-size exposure) is backed by actual measured `CameraBound` values across all 26 room/corridor prefabs.

**Research date:** 2026-08-07
**Valid until:** No hard expiry — this research is tied to the current `CameraFollow.cs`/`OverclockModule.cs` implementation state (as of commit history through 2026-08-07). Re-validate if either file changes materially before planning begins.
