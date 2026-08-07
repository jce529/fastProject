# Phase 19: SAMURAI 보스 & 패링 모듈 & 모듈 선택 UI 확장 - Research

**Researched:** 2026-08-07
**Domain:** Unity 6/C# gameplay systems — new `IPlayerCombatModule`-adjacent realtime player module, new `BossEnemyBase` subclass FSM with a two-stage (accumulate→groggy→kill) defeat condition, N-way UI list extension. 100% internal codebase architecture question — no external library/ecosystem research needed (same conclusion as `.planning/research/ARCHITECTURE.md`/`PITFALLS.md` for this milestone).
**Confidence:** HIGH for architecture/pitfalls (grounded in direct reads of every file this phase touches or extends). MEDIUM for exact tuning numbers and UI layout (explicitly Claude's Discretion, requires playtest).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**패링 모듈 — 평시 공격 (탭 베기)**
- **D-01:** 탭 시 나가는 공격은 Overclock의 자동타겟팅 돌진이 아니라 **제자리 방향성 스윙(자유 타겟)** — 그 순간 스윙 범위(부채꼴/라인) 안의 적을 즉시 벤다. 방향은 **마우스 방향**으로 결정(`CombatController`의 기존 `GetMouseWorldDirection()` 계열 로직 재사용).
- **D-02:** 스윙은 **원샷킬**이며 **모든 적(MeleeEnemy/RangedEnemy/보스 공통)에게 통용** — 코어 밸류(원샷원킬)를 그대로 유지. SAMURAI 전용 메커닉이 아니다.
- **D-03:** 탭 공격 사이 락아웃은 **짧은 고정값**(히트/헛치기 무관, 스팸 난사 방지 목적) — Overclock의 `whiffLockout`/`postKillLockout` 개념을 재사용하되 값은 훨씬 짧게. 정확한 수치는 Claude's Discretion(플레이테스트 튜닝).

**패링 판정 & 반사**
- **D-04:** 패링 발동 입력은 **일반 스윙과 같은 Attack 탭**이다(새 버튼 없음). 단 SAMURAI의 "패링 전용 타이밍" 구간에서는 **타이밍 + 방향(공격 출처를 향해 조준) 둘 다** 맞아야 패링이 성공한다.
- **D-05:** 패링 전용 타이밍 구간에서 **무입력** 또는 **잘못된 타이밍/방향으로 Attack 입력** 시 플레이어는 **즉사**한다. 단, **`RollController`의 기존 무적 굴리기로도 이 구간을 회피 가능** — 즉 "정확한 패링" 또는 "구르기 회피" 둘 중 하나가 생존 수단이다(패링만이 유일한 생존 수단이 아님).
- **D-06:** 패링 성공은 **순수 방어**다 — 보스에게 직접 데미지(처치 카운트)를 주지 않는다. 반사된 투사체의 방향은 조준 방향으로 결정된다. 대신 패링 성공은 그로기 게이지를 채우는 데 기여한다(D-09).

**SAMURAI 보스 패턴 구조 (할로우나이트 스타일)**
- **D-07:** **평시 구간**은 MeleeEnemy/FioraBoss와 동일한 **예고(Telegraph)→공격 콤보** 구조 — 맞으면 **즉사**(프로젝트의 원샷원킬 코어 그대로 적용, HP/비치명타 개념 없음), 플레이어는 **구르기로 회피**해야 한다. "패링 전용" 타이밍은 이 평시 콤보와는 **별도의 간헐적 리듬 구간**으로 삽입된다.
- **D-08:** 패링 전용 타이밍 구간의 생존 수단은 D-05와 동일(패링 성공 또는 구르기 회피, 그 외엔 즉사).
- **D-09:** **그로기(Groggy) 게이지** 신설 — 평시 구간에서 보스에게 타격 성공 **및** 패링 성공 **둘 다** 이 게이지를 채운다. 게이지는 **여러 번 누적되어야 가득 참**(1회 성공 = 즉시 그로기가 아님). 정확한 누적 임계치/두 소스(평시 타격 vs 패링) 간 가중치 차등 여부는 Claude's Discretion(플레이테스트 튜닝).
- **D-10:** 게이지가 가득 차면 **그로기 상태**에 진입 — 이 상태에서 플레이어의 공격 1회가 **처치 진행 1회**로 카운트된다(FioraBoss의 `RequiredHits` 카운터와 유사한 역할, 단 트리거 메커니즘이 그로기 게이지를 경유한다는 점이 다름).
- **D-11:** 그로기→공격 사이클을 **총 7회** 반복해야 SAMURAI가 완전히 처치된다(FioraBoss의 `RequiredHits=7`과 동일한 최종 숫자를 채택하되, 도달 메커니즘은 그로기 게이지 경유로 다름).

**모듈 선택 UI 확장**
- **D-12:** 이번 Phase에서는 **현재 구현된 2개 모듈(Overclock/패링)만** 슬롯으로 노출한다. 단, 향후 DeadEye(Phase 20)/MAX(Phase 22)/NOVA(Phase 23) 슬롯이 추가되기 쉽도록 **목록/배열 기반의 확장 가능한 구조**로 설계할 것(2개를 하드코딩 나열하는 방식 지양) — 정확한 구현 방식은 Claude's Discretion.
- **D-13:** 잠금된 모듈은 **버튼 비활성화 + 자물쇠 아이콘**으로 표시한다(클릭 자체가 안 되도록).

**검증 방식**
- **D-14:** SAMURAI 보스 실전 검증은 **Phase 18/18.1 선례대로 DebugScene(`DebugRoomTeleporter`/`DebugSceneBuilder`) 확장**으로 진행한다. 실제 `WorldGenerator` 스폰 풀 통합은 이번 Phase 범위 밖 — v3.1 Phase 16/17 파킹 범위를 그대로 유지한다.

### Claude's Discretion
- 그로기 게이지의 정확한 누적 임계치/비율(평시 타격과 패링 성공의 가중치가 같은지 다른지 포함)
- 패링 판정 타이밍 윈도우 폭(SAMURAI-05, 반드시 실측 튜닝)
- 패링 전용 타이밍 발생 빈도/평시 콤보와의 교차 주기
- 탭 공격 사이 짧은 고정 락아웃의 정확한 값
- SAMURAI 보스 시각적 정체성 — FioraBoss 선례(D-10, 기존 스프라이트 재활용+크기/색조 변형)를 기본 출발점으로 사용
- `SamuraiParryModule`(또는 `ParryController`)의 파일 배치, `IEnemy` 확장 없이 `TryParry()` 등 사이드채널 메서드로 구현(research 권장안 그대로 채택)
- 모듈 선택 UI의 정확한 레이아웃/버튼 배치

### Deferred Ideas (OUT OF SCOPE)
- **실제 `WorldGenerator` 보스 스폰 풀 통합** — v3.1 Phase 16/17 파킹 범위 유지(D-14). v4.0 완료 후 재검토.
- **게임 모드/모드 선택 화면** — Phase 24 범위(이번 Phase는 모듈 선택 UI만 확장).
- **DeadEye/MAX/NOVA 모듈 UI 슬롯 실제 콘텐츠** — 각 보스가 구현되는 Phase 20/22/23에서 슬롯만 추가 연결(D-12로 구조는 미리 대비).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SAMURAI-01 | SAMURAI 보스 격파 시 패링 모듈이 최초로 해금된다 (튜토리얼 보스, 최우선 해금) | `BossEnemyBase.Die(...)` + `BossUnlockManager.Unlock(bossId)` reused verbatim from `FioraBoss` precedent (see Don't Hand-Roll, Code Examples). **Critical finding:** Overclock must NOT be gated behind `BossUnlockManager.IsUnlocked("Fiora")` in the module-select UI, or a fresh save has zero usable modules and cannot fight SAMURAI at all — see Open Questions #1. |
| SAMURAI-02 | 패링 모듈은 슬로우모션 없이 실시간으로 동작하며, 탭 입력 시 방향성 베기 공격을 수행한다 | `CombatController.Update()`'s hold-slowmo→release-resolve state machine is structurally incompatible with an instant-tap module — see Architecture Patterns §1 (host-hook design) and Common Pitfalls #1. `InputManager.AttackHeld` (one-frame tap flag) is already tap-shaped and directly reusable. |
| SAMURAI-03 | 적 공격과 타이밍이 겹치는 시점에 입력하면 패링이 발동해 공격을 무효화하고 투사체를 반사한다 | Recommend modeling SAMURAI's parry-only attack as an actual `ProjectileController`-style projectile (reuses existing invincibility-layer-respecting `OnTriggerEnter2D` kill delivery) rather than a boss-side timer+flag — see Architecture Patterns §3 and Code Examples. |
| SAMURAI-04 | SAMURAI 보스는 평시 전투와 간헐적 패링 전용 타이밍을 반복하며, 패링 전용 타이밍에 공격을 시도하면 플레이어는 즉사한다 | `MeleeEnemy.TelegraphAndAttack()` / `FioraBoss.PatternLoop()` give the two existing telegraph→lethal-hitbox loop shapes to copy for the "평시" phase; the "패링 전용" phase is a new, distinct coroutine segment — see Architecture Patterns §2. **Critical finding:** the death delivery must go through a physics trigger (respects `PlayerInvincible` layer, satisfying D-05's roll-escape) — never a raw `PlayerController.TriggerDeath()` call from a bare timer. See Common Pitfalls #2. |
| SAMURAI-05 | 패링 판정 타이밍은 입력 지연을 고려해 넉넉하게 설정되고 실측 튜닝된다 | No code precedent for a timing-window primitive exists yet in this codebase (Overclock has no reaction-timing mechanic). Must be built from scratch and tuned via `DebugScene` per D-14 — see Validation Architecture. |
| UNLOCK-02 | 플레이어는 해금된 모듈 중 하나를 게임 시작 전 선택할 수 있다 (기존 AttackSelect를 N-way로 확장) | `AttackSelectController`/`AttackTypeSelector` static-selection pattern is the direct precedent to extend into an array/registry-driven N-way selector — see Architecture Patterns §4. |
| UNLOCK-03 | 아직 해금되지 않은 모듈은 선택 화면에 잠금 상태로 표시된다 | `BossUnlockManager.IsUnlocked(bossId)` (already implemented, PlayerPrefs-backed) is the exact gate to query per module-registry entry — see Code Examples. |
</phase_requirements>

## Summary

Phase 19 adds three things on top of an already-migrated (Phase 18) pluggable-combat-module architecture: (1) a second `IPlayerCombatModule`-family player module (`SamuraiParryModule`/`ParryController`) that behaves nothing like the first one architecturally — it must run in real time, resolve instantly on tap, and never enter slow-motion — (2) a second `BossEnemyBase` subclass (`SamuraiBoss`) whose defeat condition is a genuinely new two-stage shape (accumulate → groggy → hit-to-progress, ×7) rather than FioraBoss's flat hit-counter, and (3) generalizing the currently 2-button-hardcoded `AttackSelectController` into a data-driven, lock-aware N-way selector.

The single highest-risk item, flagged explicitly in both `.planning/research/ARCHITECTURE.md` (Question 1's "Risk called out explicitly") and this phase's own CONTEXT.md canonical_refs, is that **`CombatController.Update()` is not a generic module host — it is Overclock's hold-to-slowmo/release-to-resolve state machine with no branch point for a module that wants neither.** This research recommends a small, additive host-hook (an `IRealtimeCombatModule` marker interface checked at the top of `Update()`) that lets the parry module own its entire per-frame logic while leaving every line of Overclock's existing, playtested code path byte-identical — mirroring this project's own established precedent of closed contracts + side-channel extension (`IEnemy` staying closed, `ISpawnGatable` as an additive interface, `SamuraiBoss.TryParry()` as a side-channel method per the already-locked CONTEXT.md decision).

A second load-bearing finding, not previously surfaced in any prior research doc: **SAMURAI's parry-only-timing lethal attack must be delivered through an actual physics trigger** (mirroring `ProjectileController.OnTriggerEnter2D`/`MeleeEnemy`'s hitbox pattern), not a bare coroutine timer that calls `PlayerController.TriggerDeath()` unconditionally when the window expires — otherwise D-05's explicit "구르기로 회피 가능" guarantee breaks, because a direct `TriggerDeath()` call does not consult the `PlayerInvincible` layer that `RollController`'s i-frames rely on. Modeling the parry-only attack as a genuine projectile (reusing the `ProjectileController` pattern) satisfies this for free, and also gives D-03/D-06 ("투사체 반사") a natural physical object to reflect.

A third finding requiring an explicit planning decision (not locked by CONTEXT.md): the story doc (`STORY.md`) frames Overclock as unlocked *by* defeating F.I.O.R.A, but SAMURAI-01 requires SAMURAI to be beatable as the player's very first boss encounter using only the starting module — meaning **Overclock must be unconditionally available in the module-select UI regardless of `BossUnlockManager.IsUnlocked("Fiora")`**, or a fresh save soft-locks with zero usable modules. See Open Questions #1.

**Primary recommendation:** Add `IRealtimeCombatModule` as an additive interface CombatController checks first in `Update()` (bypassing the entire Overclock-shaped branch for realtime modules); model SAMURAI's parry-only attack as a real `ProjectileController`-pattern projectile whose mere presence inside the swing's shape check *is* the timing+direction test (no separate boss-side "IsInParryWindow" flag needed); give `SamuraiBoss` its own protected "defeat" gate (`BossEnemyBase.Die()` reused verbatim, but the decision of *when* to call it is boss-owned per the Phase 16 Pitfall 2 precedent) driven by a private groggy-accumulator, not a reused `RequiredHits` constant; and extend `AttackSelectController` into an array/registry-driven loop over `(displayName, unlockBossId, moduleFactory)` entries so DeadEye/MAX/NOVA are pure data additions in later phases.

## Standard Stack

This phase is 100% internal architecture — no new external packages, no Context7/WebSearch findings apply (same conclusion as `.planning/research/ARCHITECTURE.md`/`PITFALLS.md` for this entire v4.0 milestone). All "stack" here is existing project code being extended or precedent being copied.

### Core (existing project code this phase extends)

| Component | File | Role for Phase 19 |
|-----------|------|--------------------|
| `IPlayerCombatModule` | `Assets/Scripts/Player/Combat/IPlayerCombatModule.cs` | Existing 3-member contract (`FindTarget`/`Resolve`/`Whiff`) — kept closed, NOT modified (mirrors `IEnemy`'s closed-contract precedent) |
| `CombatController` | `Assets/Scripts/Player/CombatController.cs` | Host — gets one new branch point at the top of `Update()`, plus real module-selection wiring in `Awake()` (currently hardcoded `new OverclockModule()`) |
| `BossEnemyBase` | `Assets/Scripts/Enemy/Boss/BossEnemyBase.cs` | Abstract base — `SamuraiBoss` inherits verbatim (`Die()`, defeat-guard, spawn-gate, player-death cleanup, highlight-tint) |
| `BossUnlockManager` | `Assets/Scripts/Progression/BossUnlockManager.cs` | Static, PlayerPrefs-backed — reused as-is, `SamuraiBoss.Die(...)` passes a new `BossId = "Samurai"` const |
| `InputManager` | `Assets/Scripts/Player/InputManager.cs` | `AttackHeld`/`AttackReleased`/`IsAttackDown`/`RollPressed` — sufficient for tap detection, no new input action needed (D-04) |
| `RollController` / `InvincibilityHandler` | `Assets/Scripts/Player/RollController.cs`, `InvincibilityHandler.cs` | Reused as-is for D-05's roll-escape — layer-swap i-frames, no new invincibility system |
| `ProjectileController` | `Assets/Scripts/Enemy/ProjectileController.cs` | Pattern to copy/extend for SAMURAI's parry-only-timing attack (D-06's "투사체 반사") |
| `AttackSelectController` / `AttackTypeSelector` | `Assets/Scripts/UI/AttackSelectController.cs`, `AttackTypeSelector.cs` | Direct precedent for the new N-way module selector's static-selection pattern |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `IRealtimeCombatModule` additive marker interface (recommended) | Add a `bool RequiresSlowMotion` property directly on `IPlayerCombatModule` and `if`-branch inside the existing Update() body | Simpler on paper, but forces every future module (including Overclock) to implement a property that's meaningless for it, and keeps Overclock's proven code path textually intertwined with new branching — higher regression risk than an early `return` short-circuit. Rejected. |
| Modeling parry-only attack as a real projectile (recommended) | Boss-side `bool IsInParryWindow` flag + player-side direct dot-product check against boss position | Works, but requires two independently-tuned "windows" (boss's flag duration vs. player's shape-check radius) to agree, and has no physical object for "무입력 시 즉사" to hang off of — the death delivery would need a manual `PlayerController.TriggerDeath()` call that (as flagged in Common Pitfalls #2) breaks D-05's roll-escape unless carefully gated. The projectile-based approach gets roll-escape "for free" via the existing Physics2D `PlayerInvincible` layer exclusion. |
| Registry/array-driven `AttackSelectController` (recommended, per D-12) | Keep 2 hardcoded `OnXClicked()` methods, add a 3rd `OnParryClicked()` for this phase | Exactly what D-12 explicitly says to avoid ("2개를 하드코딩 나열하는 방식 지양") — would need a 4th/5th/6th copy-pasted method in Phases 20/22/23. Rejected per locked decision. |

**Installation:** None — no new packages. `Assembly-CSharp` default assembly, no new asmdef needed (project has none currently — confirmed via `find *.asmdef` returning empty).

## Architecture Patterns

### §1. Host-hook for a realtime (non-slowmo) combat module

**Problem:** `CombatController.Update()` (lines ~141-200) is a single method that fuses slow-mo lifecycle (`EnterSlowMotion`/`ExitSlowMotion`), gauge draining, highlight updates, and release-triggered `DashOrWhiff()` — all built around "hold Attack → slow-mo → release → resolve." SAMURAI-02 requires the opposite: tap → resolve immediately, no slow-mo at all.

**Recommended pattern — additive marker interface, checked first, short-circuits the rest of Update():**

```csharp
// New file: Assets/Scripts/Player/Combat/IRealtimeCombatModule.cs
/// <summary>
/// Additive marker for combat modules that must NOT go through CombatController's
/// hold-slowmo→release-resolve state machine (SAMURAI-02: parry runs in real time).
/// Does not extend/replace IPlayerCombatModule — a module can implement both if it
/// needs CombatContext access, or only this one. Mirrors the project's existing
/// "closed IEnemy + additive ISpawnGatable" precedent — IPlayerCombatModule stays
/// closed at 3 members, this is a side-channel capability check instead.
/// </summary>
public interface IRealtimeCombatModule
{
    /// <summary>Called every CombatController.Update() frame instead of the
    /// Overclock-shaped hold/release logic. Owns its own input polling, timing,
    /// and resolution — CombatController does not manage _isSlowMo/_isAttackPending
    /// for this module at all.</summary>
    void Tick(CombatContext ctx);
}
```

```csharp
// CombatController.Update() — only the first 6 lines change, everything below is untouched:
private void Update()
{
    if (_player != null && _player.InputLocked) return;
    if (_isBusy) return; // still respects lockouts from OTHER systems (e.g., dash mid-flight)

    if (_activeModule is IRealtimeCombatModule realtimeModule)
    {
        realtimeModule.Tick(_ctx);
        return; // bypass Overclock's entire hold-slowmo-release state machine
    }

    // ---- existing Overclock-shaped logic below, byte-identical, zero diff ----
    var input = InputManager.Instance;
    ...
}
```

Because `OverclockModule` does not implement `IRealtimeCombatModule`, this `is` check is always `false` for the currently-shipped module — **zero behavior change for Overclock**, satisfying the same "verbatim, regression-proof" bar INFRA-01 already established in Phase 18.

`_activeModule`'s declared field type stays `IPlayerCombatModule` (unchanged). `SamuraiParryModule` implements both interfaces: `IPlayerCombatModule` (to satisfy the field's static type — its `FindTarget`/`Resolve`/`Whiff` members become dead code that is provably unreachable, since `Update()` returns before ever calling them for a realtime module) and `IRealtimeCombatModule` (its real logic, in `Tick()`). Flag the dead-but-required `IPlayerCombatModule` members explicitly in a code comment referencing this research doc, so a future reader doesn't mistake them for live logic.

### §2. SAMURAI's two-phase pattern loop (평시 combo ↔ 패링 전용 리듬 구간)

Copy `FioraBoss.PatternLoop()`'s coroutine shape (a `while(true)` loop alternating named phases, guarded by `if (_isDefeated) yield break;` at every yield point, `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` throughout) but with a **third, alternating phase inserted**, not present in FioraBoss:

```
PatternLoop() {
  while (true) {
    // Phase A — 평시 콤보 (D-07): telegraph→lethal-hitbox, same shape as MeleeEnemy.TelegraphAndAttack()
    //   - hit success -> _groggyGauge += onHitContribution (D-09)
    //   - player rolls/dashes through -> pattern continues, no groggy contribution
    yield return NormalComboSegment();

    // Phase B — 패링 전용 타이밍 (D-08): fires a parryable projectile (§3), NOT a melee hitbox
    //   - parried successfully -> _groggyGauge += onParryContribution (D-09), projectile destroyed/reflected
    //   - not parried (no input, wrong timing/direction, no roll) -> projectile physically hits player -> TriggerDeath()
    //     via its own OnTriggerEnter2D (respects PlayerInvincible layer automatically — see Pitfall #2)
    yield return ParryTimingSegment();

    // Groggy check (D-09/D-10): only after gauge is FULL does a "vulnerable" window open
    if (_groggyGauge >= groggyThreshold) {
      yield return GroggyVulnerableSegment(); // IsAlive = true here (targetable), same convention as FioraBoss.Vulnerable
      // a hit landed during this window -> _hitCount++ (D-10/D-11), _groggyGauge reset to 0
      // if _hitCount >= 7 -> Die(...) (D-11, exact same call shape as FioraBoss)
    }
  }
}
```

This is a **structurally new FSM**, not a parametrization of FioraBoss's Dash/Vulnerable loop — matches `ARCHITECTURE.md`'s Anti-Pattern 1 guidance ("do not force all bosses through one generalized Telegraph→Attack→Vulnerable machine"; SAMURAI genuinely needs a third phase FioraBoss doesn't have). `IsAlive` continues to mean "currently targetable" (only `true` during `GroggyVulnerableSegment()`), never "alive" — same overload FioraBoss already establishes and that `BossEnemyBase`'s doc comment locks in project-wide.

### §3. Parry-only attack as a real projectile (SAMURAI-03, D-04, D-06)

Recommend a new lightweight projectile type (either a second `ProjectileController`-alike component, or `ProjectileController` extended with a `parryable` flag) fired by `SamuraiBoss` during `ParryTimingSegment()`, with:

- **Timing+direction check unified with the shape check.** Instead of a separate boss-side `IsInParryWindow` bool the player module polls, the "window" **is** simply "a parryable-projectile instance currently overlaps the player's swing shape (fan/line, same dot-product math as `OverclockModule.IsInAttackShape`) at the moment of an Attack tap." This collapses D-04's "타이밍 + 방향 둘 다 맞아야" into a single overlap+shape query — no dual-flag synchronization risk between boss and player module.
- **Death delivery via `OnTriggerEnter2D`**, exactly like `ProjectileController.cs` today (`CompareTag("Player")` → `PlayerController.TriggerDeath()`) — this is what makes D-05's roll-escape work for free (see Common Pitfalls #2).
- **Successful parry:** the player-module's `Tick()` detects the projectile in-shape, calls a method on it (e.g. `parryProjectile.OnParried(aimDir)`) which destroys/redirects the projectile (visual "reflection" in the aim direction per D-06) and notifies `SamuraiBoss` (a small callback or direct reference) to add to the groggy gauge — **no damage to the boss** (D-06 is explicit: parry is pure defense).

### §4. N-way module-select UI (UNLOCK-02/03, D-12/D-13)

Extend, don't replace, `AttackSelectController`/`AttackTypeSelector`'s existing static-selection convention. Recommended shape:

```csharp
// New file, e.g. Assets/Scripts/UI/CombatModuleRegistry.cs
public readonly struct CombatModuleEntry
{
    public readonly string DisplayName;
    public readonly string RequiredBossId; // null/"" = always unlocked (Overclock — see Open Questions #1)
    public CombatModuleEntry(string displayName, string requiredBossId) { ... }
}

public static class CombatModuleRegistry
{
    // D-12: array-based, future DeadEye/MAX/NOVA phases append one line here, nothing else.
    public static readonly CombatModuleEntry[] All =
    {
        new CombatModuleEntry("Overclock", requiredBossId: null),        // always unlocked — see Open Questions #1
        new CombatModuleEntry("Parry",     requiredBossId: "Samurai"),   // SamuraiBoss.BossId
    };
}
```

```csharp
// New static, mirrors AttackTypeSelector's "static selection, MonoBehaviour-backed" pattern
public static class CombatModuleSelector
{
    public static int SelectedIndex { get; private set; } = 0; // Overclock default
    public static void SetSelected(int index) => SelectedIndex = index;
}
```

`AttackSelectController` becomes a small loop over `CombatModuleRegistry.All`, setting each Inspector-assigned `Button`'s `interactable` and a paired lock-icon `Image.enabled` from `entry.RequiredBossId == null || BossUnlockManager.IsUnlocked(entry.RequiredBossId)` (D-13), and wiring `OnClick` to `CombatModuleSelector.SetSelected(index)` + `SceneManager.LoadScene("SampleScene")`. For this phase's 2 entries, pre-placing 2 `Button` GameObjects in the Editor (one per entry, index-tagged) and driving their state from `Start()` is the pragmatic middle ground — avoids both the D-12-forbidden hardcoded-per-button-method pattern *and* a full runtime prefab-instantiation UI system that this 2-module phase doesn't yet need (YAGNI, consistent with project's prototype-scope discipline). `CombatController.Awake()` then reads `CombatModuleSelector.SelectedIndex` to construct `_activeModule` instead of the current hardcoded `new OverclockModule()`.

### Recommended file layout

```
Assets/Scripts/Player/Combat/
├── IPlayerCombatModule.cs        (existing, unmodified)
├── IRealtimeCombatModule.cs      (new — §1)
├── CombatContext.cs              (existing, unmodified — SamuraiParryModule/SamuraiBoss reuse via ctx, no new fields required unless swing shape needs its own tunables)
├── OverclockModule.cs            (existing, unmodified)
└── SamuraiParryModule.cs         (new — implements both interfaces, §1/§3)

Assets/Scripts/Enemy/Boss/
├── BossEnemyBase.cs               (existing, unmodified)
├── FioraBoss.cs                   (existing, unmodified)
└── SamuraiBoss.cs                 (new — §2/§3, TryParry()-style side-channel per ARCHITECTURE.md Q2)

Assets/Scripts/UI/
├── AttackSelectController.cs      (modified — §4)
├── AttackTypeSelector.cs          (existing, unmodified — still governs Overclock's own Linear/Fan shape choice, orthogonal to module selection)
├── CombatModuleRegistry.cs        (new — §4)
└── CombatModuleSelector.cs        (new — §4)
```

### Anti-Patterns to Avoid

- **Adding a 4th member to `IEnemy` for parry.** Explicitly forbidden by both `ARCHITECTURE.md` Anti-Pattern 2 and this phase's own CONTEXT.md canonical_refs — every other `IEnemy` implementor (`MeleeEnemy`, `RangedEnemy`, `DummyEnemy`, `FioraBoss`) would need a no-op stub. Use `SamuraiBoss.TryParry()` / a projectile callback instead.
- **Branching inside the existing Overclock-shaped `Update()` body with `if (currentModule == Parry) {...}` checks scattered through the method.** This is exactly `PITFALLS.md` Pitfall 3's warned-against shape (module-swap state leaks) and mixes two unrelated state machines in one method. Use the early-return host-hook (§1) instead.
- **Reusing `FioraBoss`'s flat `RequiredHits` constant/pattern for SAMURAI's defeat condition.** `PITFALLS.md` Pitfall 2 explicitly calls out that "defeated" must stay a boss-owned decision, not an assumed shared shape — SAMURAI's groggy-gauge-gated hit is a genuinely different mechanism even though the final count (7) matches by design (D-11).
- **A bare `WaitForSecondsRealtime(parryWindowDuration)` timer that calls `PlayerController.TriggerDeath()` directly when it expires without a successful parry.** Breaks D-05 (roll must be able to save the player) because it never checks the `PlayerInvincible` layer. See Common Pitfalls #2.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Player i-frames during roll-escape of the parry window (D-05) | A new invincibility flag/timer for the parry mechanic specifically | `RollController` + `InvincibilityHandler`'s existing layer-swap i-frames, unmodified | Already timeScale-immune, already the sole invincibility primitive in the project (`InvincibilityHandler.cs` doc comment: "NEVER use `Physics2D.IgnoreLayerCollision`"); the SAMURAI parry projectile's `OnTriggerEnter2D` will automatically respect it via the existing Physics2D collision matrix, no new code needed |
| Boss defeat-guard / death sequence / spawn-gating / hit-highlight (SAMURAI-01/04) | A second copy of `FioraBoss`'s plumbing with renamed fields | `BossEnemyBase` (already extracted in Phase 18, INFRA-03) | This is the exact reason `BossEnemyBase` exists — `PITFALLS.md` Pitfall 2 is specifically about avoiding a 2nd hand-copied boss FSM |
| Persistent module unlock (SAMURAI-01, UNLOCK-03) | A new save-file/PlayerPrefs key scheme for module unlocks | `BossUnlockManager.IsUnlocked/Unlock(bossId)` (already implemented, PlayerPrefs-backed, Phase 18 UNLOCK-01) | Already generic over `bossId` string — `SamuraiBoss.Die(...)` just needs to pass `"Samurai"` as the `bossId` argument, identical call shape to `FioraBoss.Die(...)` |
| Mouse-direction aim vector (D-01) | A new aim-direction helper for the parry module | `OverclockModule.GetMouseWorldDirection()`'s logic (currently `private`) | Same math needed twice now — extract to a small shared static helper (e.g. on `CombatContext` or a new tiny `AimUtil` static) rather than duplicating; this mirrors the project's own established precedent of extracting on 2nd occurrence (16-03's `GetMouseWorldDirection` unification comment: "Linear/Fan 분기가 거의 동일했던... 헬퍼로 통합") |
| Projectile physics/lifetime/kill-on-hit (D-03/D-06) | A new projectile MonoBehaviour from scratch | `ProjectileController.cs` (`Rigidbody2D` dynamic, gravity 0, continuous+interpolate, `Init(direction)`, distance-based self-destruct, `OnTriggerEnter2D` kill) | Already exactly the shape SAMURAI's parry-only attack needs; extend with a `parryable` flag/component rather than reinventing projectile plumbing |
| Camera shake / hit-spark / score bonus on boss defeat | New VFX/score code | `CameraFollow.Shake()`, `EnemyDeathEffect`, `ScoreManager.AddBossKillScore()` — all already wired into `BossEnemyBase.Die()` | Zero new code needed — inherited for free by any `BossEnemyBase` subclass |

**Key insight:** Nearly every primitive SAMURAI/Parry needs already exists somewhere in the codebase in a slightly different shape (melee telegraph, ranged projectile, boss defeat sequence, i-frames, unlock persistence, static UI selection). The actual new work is almost entirely *composition and one new coroutine phase*, not new primitives — consistent with this project's demonstrated build pattern across Phases 15-18.

## Common Pitfalls

### Pitfall 1: Building the parry module as `if (module is Parry)` branches inside the existing `Update()` instead of the host-hook in Architecture §1

**What goes wrong:** SAMURAI-02 requires zero slow-motion, instant resolution. If implemented as conditionals threaded through `CombatController.Update()`'s existing hold/release logic (`EnterSlowMotion`, `_isAttackPending`, `AttackReleased` handling), the parry module inherits slow-mo side effects it should never have (gauge draining reads, `_slowMoStartTime` safety-timeout checks, `RangeDisplay.Show()/Hide()` calls meant for Overclock's search-radius visualization) — and any future module swap (Boss Rush, Phase 24+) has to disentangle two state machines from one method.

**Why it happens:** The path of least resistance for "make the second module work" is adding conditionals to the one `Update()` that already exists, exactly as `PITFALLS.md` Pitfall 3 predicts for this exact milestone.

**How to avoid:** Use the additive `IRealtimeCombatModule` early-return pattern (§1) — new module owns its entire per-frame loop in its own `Tick()`, Overclock's code path is provably untouched (`is` check is `false` for it).

**Warning signs:** Any new `if (_activeModule is SamuraiParryModule)` (or similar) appearing *inside* the body of the existing hold/release logic rather than as an early-return guard at the top of `Update()`.

### Pitfall 2: Delivering the "패링 전용 타이밍 무입력/실패 시 즉사" via a bare coroutine timer calling `PlayerController.TriggerDeath()` directly

**What goes wrong:** D-05 explicitly requires that `RollController`'s existing i-frames can save the player during the parry window ("구르기로 이 구간을 회피 가능"). `RollController`/`InvincibilityHandler` implement invincibility via a **layer swap** (`gameObject.layer = LayerPlayerInvincible`) that only matters if the lethal signal arrives through a **Physics2D collision/trigger check** (the `PlayerInvincible` layer is excluded from relevant collision matrix rows — confirmed by `ProjectileController.cs`'s own doc comment: "D-16: PlayerInvincible layer excluded via Physics2D matrix — no code check needed"). A boss-side coroutine that just does `yield return new WaitForSecondsRealtime(parryWindowDuration); if (!_parriedThisWindow) PlayerController.TriggerDeath();` bypasses that layer check entirely — it would kill the player even mid-roll, silently breaking D-05's explicit guarantee.

**Why it happens:** It is the "obvious" way to implement "count down, then kill if nothing happened" — timer-driven death is the natural instinct, and nothing in `BossEnemyBase`/`FioraBoss` demonstrates the *correct* pattern because FioraBoss's own lethal hit is delivered via `OnTriggerEnter2D` too (see `FioraBoss.cs` line ~305-313), but that precedent is easy to overlook since it's not the "timer" part of the code.

**How to avoid:** Model the parry-only attack as a physical trigger object (Architecture §3's recommended projectile) whose `OnTriggerEnter2D` is the *only* path to `PlayerController.TriggerDeath()` — exactly mirroring `ProjectileController.cs`'s and `MeleeEnemy.cs`'s existing pattern. The "timer" only controls when the projectile is *fired* and how long it's *parryable*; the kill itself always goes through a collider.

**Warning signs:** Any direct call to `PlayerController.TriggerDeath()` from inside a `SamuraiBoss` coroutine that is not gated by an `OnTriggerEnter2D`/`OnCollisionEnter2D` callback.

### Pitfall 3: New SAMURAI/parry timers using `Time.deltaTime`/`WaitForSeconds` instead of the realtime equivalents

**What goes wrong:** Identical to `PITFALLS.md` Pitfall 1 (project-wide, HIGH confidence, already documented in depth) — any parry-window, groggy-gauge, or pattern-loop timer using scaled time will silently break the moment the player's *other* hand triggers Overclock's slow-mo or a `HitFreeze` from killing a regular enemy nearby, because `Time.timeScale` is global.

**How to avoid:** Copy `FioraBoss.PatternLoop()`'s literal `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` usage as the template for every new SAMURAI/parry timer, with zero exceptions. Grep the new files for `Time.deltaTime`/`WaitForSeconds(` (not `Realtime`) before considering any task done — this project's own Definition-of-Done checklist item from `PITFALLS.md`.

**Warning signs:** `while (elapsed < duration) { elapsed += Time.deltaTime; ... }` anywhere in `SamuraiBoss.cs`/`SamuraiParryModule.cs`.

### Pitfall 4: Copying `FioraBoss`'s `RequiredHits` shape verbatim for SAMURAI's groggy-gated defeat condition

**What goes wrong:** FioraBoss's `OnDashHit()` does `_hitCount++; if (_hitCount >= RequiredHits) Die(...)` — a single flat counter. SAMURAI needs a **two-stage** structure: hits/parries accumulate into a groggy gauge (D-09), only a *full* gauge opens a vulnerable window, and only a hit *during that window* counts toward the final 7 (D-10/D-11). If a plan naively reuses FioraBoss's `OnDashHit()` shape, the groggy-gauge stage gets silently skipped (every hit counts directly, defeating the entire point of D-09/D-10).

**How to avoid:** Treat `OnDashHit()` (called during the `GroggyVulnerableSegment()` window, when `IsAlive == true`) as *only* the "count-toward-7" step; the groggy-gauge accumulation from normal-phase hits and parry successes must be tracked via a **separate** code path that does not call `OnDashHit()` at all (since those hits happen while `IsAlive == false` and are never dash-targetable in the first place — `CombatController.FindTarget()` already skips `!IsAlive` enemies, so this separation is largely free/automatic as a side effect of the existing `IsAlive`-as-targetability overload).

**Warning signs:** A single counter field incremented from more than one call site without a clear "which call sites feed the groggy gauge vs. which feed the final hit-count" comment.

## Code Examples

### Verbatim precedent: reused mouse-direction + shape-check math (currently private in `OverclockModule`, candidate for extraction)

```csharp
// Source: Assets/Scripts/Player/Combat/OverclockModule.cs:155-178 (existing, read during this research)
private Vector2 GetMouseWorldDirection(Vector2 origin, CombatContext ctx)
{
    UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
    Vector2 mousePos = mouse != null ? mouse.position.ReadValue() : (Vector2)ctx.MainCamera.WorldToScreenPoint(origin);
    Vector3 mouseWorld = ctx.MainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(ctx.MainCamera.transform.position.z)));
    Vector2 dir = (Vector2)mouseWorld - origin;
    return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
}

private bool IsInAttackShape(Vector2 normalizedToTarget, float distance, Vector2 attackDir, float maxDistance, CombatContext ctx)
{
    if (distance > maxDistance) return false;
    float dot = Vector2.Dot(attackDir, normalizedToTarget);
    float thresholdAngle = (AttackTypeSelector.Selected == AttackType.Linear) ? ctx.LinearHalfAngleDeg : ctx.FanHalfAngleDeg;
    float cosHalf = Mathf.Cos(thresholdAngle * Mathf.Deg2Rad);
    return dot >= cosHalf;
}
```

Recommend `SamuraiParryModule` use its **own** shape tunables (a dedicated `swingRadius`/`swingHalfAngleDeg`, not `AttackTypeSelector.Selected`'s Linear/Fan toggle) — that toggle is thematically Overclock-specific (in-run zone-triggered range-display choice) and reusing it for Parry conflates two unrelated shape-selection axes. See Open Questions #2.

### Existing boss defeat/unlock call shape to replicate for `SamuraiBoss.Die()`

```csharp
// Source: Assets/Scripts/Enemy/Boss/FioraBoss.cs:258-263 (existing, read during this research)
_hitCount++;
if (_hitCount >= RequiredHits) // Pitfall 1: 증가 후 >= 비교 — 정확히 7회째에 처치
{
    Die(bossMaskRiseDuration, bossParticleColor, bossParticleBurstCount,
        bossDeathShakeDuration, bossDeathShakeAmplitude, BossId);
    return;
}
```

```csharp
// Source: Assets/Scripts/Enemy/Boss/BossEnemyBase.cs:90-108 (existing, read during this research)
protected virtual void Die(float maskRiseDuration, Color particleColor, int particleBurstCount,
    float shakeDuration, float shakeAmplitude, string bossId)
{
    _isDefeated = true;
    IsAlive = false;
    // ... rigidbody/collider/animator teardown, EnemyDeathEffect, camera shake ...
    ScoreManager.AddBossKillScore();
    BossUnlockManager.Unlock(bossId); // <- SamuraiBoss.Die(..., "Samurai") triggers UNLOCK-03's gate to open
}
```

`SamuraiBoss` calls this exact same protected `Die(...)` (inherited, unmodified) once its own groggy→hit×7 logic (Common Pitfalls #4) decides the boss is defeated — the *call shape* is identical to FioraBoss, only the *decision of when to call it* differs.

## Open Questions

1. **Is Overclock unconditionally unlocked in the module-select UI, independent of `BossUnlockManager.IsUnlocked("Fiora")`?**
   - What we know: `STORY.md` frames Overclock as *learned from* defeating F.I.O.R.A ("F.I.O.R.A 격파 → 예측형 순간이동 연쇄 해금... 현재 F.A.S.T.에 기본 탑재된 모듈"), and `FioraBoss.Die()` already calls `BossUnlockManager.Unlock("Fiora")` (Phase 18, code-complete). Meanwhile SAMURAI-01 requires SAMURAI to be beatable as the player's first-ever boss fight ("튜토리얼 보스, 최우선 해금"), which is impossible if the only starting module is itself locked behind a boss the player hasn't fought yet.
   - What's unclear: CONTEXT.md does not explicitly address this — it only says the UI shows "현재 구현된 2개 모듈(Overclock/패링)." It does not say whether Overclock's slot is gated.
   - Recommendation: Overclock's `CombatModuleRegistry` entry should use `requiredBossId: null` (always-unlocked sentinel), independent of the `"Fiora"` unlock flag — treat the story's framing as narrative flavor, not a mechanical gate. This must be confirmed explicitly during planning since getting it wrong soft-locks a fresh save.

2. **Does the parry swing reuse `AttackTypeSelector.Selected` (Linear/Fan) for its shape, or does it get its own independent shape tunables?**
   - What we know: D-01 describes the swing shape generically ("부채꼴/라인"), textually identical to Overclock's existing Linear/Fan vocabulary, but `AttackTypeSelector` is currently an in-run, zone-triggered toggle specific to Overclock's `RangeDisplay`-visualized search shape.
   - What's unclear: Whether reusing the same enum/selector for a structurally different, non-slow-mo, no-range-display module is intended, or coincidental wording overlap.
   - Recommendation: Give the parry module its own fixed (or Inspector-tunable) swing shape, independent of `AttackTypeSelector.Selected` — avoids conflating two orthogonal design axes and avoids a hidden dependency where changing Overclock's zone-based Fan/Linear toggle mid-run would unexpectedly also affect Parry's swing shape. Flag as Claude's Discretion territory already (CONTEXT.md doesn't lock this), but the planner should make the independence explicit rather than silently inheriting `AttackTypeSelector`.

3. **Exact groggy-gauge accumulation values and hit/parry weighting (D-09) — cannot be resolved by research, requires playtest.**
   - What we know: CONTEXT.md explicitly defers this to Claude's Discretion + playtest tuning, same as SAMURAI-05's timing window.
   - Recommendation: Ship with a simple placeholder (e.g., flat +1 per normal-phase hit, +1 per parry success, threshold = 3) behind `[SerializeField]` tunables (mirrors every other boss's tunable-exposure convention, e.g. `FioraBoss.vulnerableDuration`), then tune via the DebugScene per D-14. Do not hardcode magic numbers inline.

4. **Does `SamuraiParryModule`'s dead `IPlayerCombatModule` stub methods need a compile-time guard/comment, or should the interface split (§1's "Alternatives Considered" row 1) be reconsidered if a second realtime module (MAX, Phase 22) arrives with different needs?**
   - What we know: Phase 22 (MAX) is explicitly flagged in `ARCHITECTURE.md` Question 1 as "the single highest-uncertainty item" for the exact same reason (movement-is-the-attack, likely no hold-slowmo shape either).
   - Recommendation: If `IRealtimeCombatModule` proves to be the right shape for MAX too, no rework is needed (it's already generic — any module implementing it skips the Overclock branch). If MAX turns out to need something even more different (e.g., an always-on `FixedUpdate` hook independent of `CombatController.Update()` entirely), that is Phase 22's problem to solve, not this phase's — do not over-generalize `IRealtimeCombatModule` now for a need that isn't confirmed yet (YAGNI, consistent with project convention).

## Validation Architecture

> No automated test framework is wired into this project. `com.unity.test-framework` (1.6.0) is present in `Packages/manifest.json` as a transitive/default Unity package, but zero `*.Tests.cs`/NUnit files exist anywhere in `Assets/`, and no `.asmdef` files exist in the project at all (confirmed via `find Assets -iname "*.asmdef"` → empty). Every phase from 13 through 25 in this project's history has validated gameplay/timing feel through **manual playtest checklists mapped to lettered decisions (D-01, D-02, ...)**, executed in-Editor via `DebugScene`/`DebugRoomTeleporter` — not through NUnit. This phase should follow that same established convention rather than introducing a new, unprecedented automated-test layer for gameplay-feel mechanics (parry timing, groggy tuning) that are inherently playtest-driven by nature. `.planning/config.json`'s `workflow.nyquist_validation: true` is honored below in playtest-checklist form, consistent with this project's actual verification method.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None (manual playtest checklist — established project convention, see note above) |
| Config file | none |
| Quick run command | Enter Play mode in `DebugScene.unity` (`Fast/Debug/Build DebugScene` if not yet built/updated for SAMURAI) |
| Full suite command | Full D-01–D-14 checklist walkthrough in `DebugScene`, mirroring Phase 18/18.1's Task 3 checkpoint pattern |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Verification | File Exists? |
|--------|----------|-----------|---------------|-------------|
| SAMURAI-01 | SAMURAI 격파 → 패링 모듈 해금 | manual playtest | Defeat SAMURAI in DebugScene, confirm `PlayerPrefs.GetInt("boss_unlock_Samurai")==1` persists after Play restart (mirrors Phase 18-02's exact verification method) | ❌ Wave 0 — `SamuraiBoss.cs` doesn't exist yet |
| SAMURAI-02 | 탭 시 즉시 스윙, 슬로우모션 없음 | manual playtest | Tap Attack with Parry module selected — confirm `Time.timeScale` never leaves 1 during the swing | ❌ Wave 0 |
| SAMURAI-03 | 타이밍/방향 일치 시 패링 성공, 투사체 반사 | manual playtest | Tap Attack aimed at SAMURAI during its parry-window projectile flight — confirm projectile destroyed/redirected, no death | ❌ Wave 0 |
| SAMURAI-04 | 평시/패링 구간 교차 반복, 실패 시 즉사 | manual playtest | Let a parry window expire with no input — confirm death; roll through one instead — confirm survival (D-05) | ❌ Wave 0 |
| SAMURAI-05 | 패링 타이밍 실측 튜닝 | manual playtest, iterative | Multiple playtest passes adjusting window width until parry feels "fair but not trivial" per input-lag allowance | ❌ Wave 0 — requires the mechanic to exist first |
| UNLOCK-02 | N-way 모듈 선택 | manual playtest | Load module-select scene with both Overclock and Parry unlocked, confirm both selectable and correct module loads in `SampleScene` | ❌ Wave 0 |
| UNLOCK-03 | 잠금 모듈 표시 | manual playtest | Fresh `PlayerPrefs` (or explicitly cleared `boss_unlock_Samurai`) — confirm Parry slot shows lock icon + is non-interactable | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** Manual smoke check in `DebugScene` for the specific mechanic just implemented (mirrors the granularity of Phase 18.1's per-task checkpoint fixes).
- **Per wave merge:** Re-run the full D-01–D-14 checklist against the latest `DebugScene` build.
- **Phase gate:** All 6 Success Criteria + D-01 through D-14 confirmed via real play (not code-inspection-only) before `/gsd:verify-work`, matching the precedent set by every prior boss-phase (15, 18, 18.1) in this project's history.

### Wave 0 Gaps
- `SamuraiBoss.cs`, `SamuraiParryModule.cs` (or `ParryController.cs`), `IRealtimeCombatModule.cs`, `CombatModuleRegistry.cs`, `CombatModuleSelector.cs` — all net-new, do not exist yet.
- A SAMURAI-specific test room/prefab (e.g. `Room_SamuraiFsmTest.prefab` via a new `RoomSamuraiFsmTestBuilder.cs` editor tool, or extending the existing `Room_BossFsmTest.prefab`/`DebugSceneBuilder.cs` to place a `SamuraiBoss` instance) — mirrors the `RoomBossFsmTestBuilder.cs`/`BossEnemyPrefabBuilder.cs` precedent from Phase 15/18.
- No test framework install needed — this project does not use one; do not introduce NUnit/asmdefs for this phase (inconsistent with established convention, adds overhead disproportionate to a prototype validating gameplay feel).

## Sources

### Primary (HIGH confidence — direct source reads during this research session)
- `Assets/Scripts/Player/Combat/IPlayerCombatModule.cs`, `CombatContext.cs`, `OverclockModule.cs` — full reads
- `Assets/Scripts/Player/CombatController.cs` — full read
- `Assets/Scripts/Enemy/Boss/BossEnemyBase.cs`, `FioraBoss.cs` — full reads
- `Assets/Scripts/Enemy/IEnemy.cs`, `ISpawnGatable.cs`, `MeleeEnemy.cs`, `RangedEnemy.cs`, `ProjectileController.cs` — full reads
- `Assets/Scripts/Progression/BossUnlockManager.cs` — full read
- `Assets/Scripts/UI/AttackSelectController.cs`, `AttackTypeSelector.cs` — full reads
- `Assets/Scripts/Player/InputManager.cs`, `RollController.cs`, `InvincibilityHandler.cs`, `PlayerController.cs`, `ChronoGaugeController.cs` — full reads
- `Assets/Editor/DebugSceneBuilder.cs`, `Assets/Scripts/World/DebugRoomTeleporter.cs`, `Assets/Editor/RoomBossFsmTestBuilder.cs`, `Assets/Editor/BossEnemyPrefabBuilder.cs` — full reads (D-14 precedent)
- `.planning/research/ARCHITECTURE.md`, `.planning/research/PITFALLS.md` — full reads (v4.0 milestone-level research, directly informs this phase)
- `.planning/phases/19-samurai-ui/19-CONTEXT.md`, `.planning/REQUIREMENTS.md`, `.planning/STATE.md` — full reads
- `STORY.md` (grep for boss/module unlock mapping) — sourced Open Question #1
- `.planning/config.json` — confirmed `workflow.nyquist_validation: true`; confirmed no automated test framework present in `Assets/`

### Secondary / Tertiary
- None — no WebSearch/Context7/external sources used or needed for this phase (pure internal-architecture question, consistent with the rest of the v4.0 milestone's research).

## Metadata

**Confidence breakdown:**
- Standard stack (internal component reuse map): HIGH — every cited file was read in full this session.
- Architecture (host-hook, boss FSM shape, UI registry): HIGH for the *pattern* recommendations (directly extend proven precedents in this codebase); MEDIUM for the *exact* interface/class names (Claude's Discretion per CONTEXT.md, planner may reasonably choose different names/exact shapes).
- Pitfalls: HIGH — Pitfalls 1/3 are direct extensions of already-documented, HIGH-confidence project-wide pitfalls (`PITFALLS.md` #1/#2); Pitfall 2 (invincibility-layer/death-delivery) and Pitfall 4 (groggy-gauge vs. RequiredHits conflation) are new findings from this session's direct code reads, not previously documented anywhere.
- Open Questions: Open Question #1 (Overclock unlock gating) is a genuine, unresolved design ambiguity between `STORY.md` and mechanical requirements — flagged as needing an explicit planning-stage decision, not something research can resolve unilaterally.

**Research date:** 2026-08-07
**Valid until:** Valid for the lifetime of this phase's planning/execution (internal-codebase-only research is not time-sensitive the way library-version research would be) — re-verify only if `CombatController.cs`/`BossEnemyBase.cs`/`AttackSelectController.cs` are modified by a different phase before Phase 19 executes.
