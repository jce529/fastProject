---
phase: 13-audio-foundation-sound-polish
plan: 03
subsystem: audio
tags: [unity, audiomanager, sfx-hooks, combat, enemy, world]

# Dependency graph
requires:
  - phase: 13-01
    provides: "AudioManager singleton + Sfx enum (PortalEnter, PortalExit, Slash, EnemyDeathGlitch) + static PlaySfx(Sfx, float) entry point"
provides:
  - "CombatController.ExecuteDash 처치 확정 순간 슬래시 SFX 훅 (HitFreeze 이전)"
  - "EnemyDeathEffect.PlayDeathSequence 파티클 재생 직전 사망 글리치 SFX 훅"
  - "FloorTransitionEffect.PlayEntry/PlayExit 포탈 진입/퇴장 SFX 훅 (연출 첫 실행문)"
affects: ["13-04"]

# Tech tracking
tech-stack:
  added: []
  patterns: ["연출 코루틴 첫 실행문/핵심 지점에 정적 AudioManager.PlaySfx 호출을 1줄 추가하는 배선 패턴 — 리팩토링 없이 기존 연출 타이밍에 사운드 트리거를 얹음"]

key-files:
  created: []
  modified:
    - "Assets/Scripts/Player/CombatController.cs"
    - "Assets/Scripts/Enemy/EnemyDeathEffect.cs"
    - "Assets/Scripts/World/FloorTransitionEffect.cs"

key-decisions:
  - "슬래시 SFX는 target.OnDashHit() 직후, HitFreeze(timeScale=0) 이전에 호출 — DSP 클럭은 timeScale 독립이라 임팩트 프레임과 사운드 어택이 정확히 일치"
  - "사망 글리치는 SpawnDeathParticles() 직전 호출 — Die 애니메이션 완주 후 트리거되어 슬래시와 자연 시간차 레이어 형성 (D-05 2단 콤보)"
  - "포탈 진입/퇴장 SFX는 각 코루틴의 첫 실행문으로 배치 — 마스크/포탈 성장 시작과 동시 트리거 (D-06)"

patterns-established:
  - "연출 컴포넌트에 사운드 훅을 추가할 때는 기존 로직을 건드리지 않고 핵심 타이밍 지점에 AudioManager.PlaySfx 1줄만 삽입 (CLAUDE.md 정밀 변경 원칙)"

requirements-completed: [SFX-02, SFX-03, SFX-04]

# Metrics
duration: 5min
completed: 2026-07-09
---

# Phase 13 Plan 03: 처치/사망/포탈 SFX 배선 Summary

**Phase 12 연출 컴포넌트 3개 파일(CombatController, EnemyDeathEffect, FloorTransitionEffect)에 AudioManager.PlaySfx 호출 4줄을 정확한 타이밍 지점에 추가 — 기존 코드 변경 0건.**

## Performance

- **Duration:** 5 min
- **Started:** 2026-07-09T10:04:00Z (approx)
- **Completed:** 2026-07-09T10:09:37Z
- **Tasks:** 2 completed
- **Files modified:** 3

## Accomplishments
- 대시 처치 확정 순간(HitFreeze 이전)에 슬래시 SFX 트리거 배선 (SFX-03)
- 적 사망 파티클 재생 직전에 글리치 노이즈 SFX 트리거 배선 — 슬래시와 자연 시간차 레이어 성립 (SFX-04, D-05)
- 포탈 진입 마스크 성장 시작과 동시에 상승음, 퇴장 포탈 성장 시작과 동시에 하강음 트리거 배선 (SFX-02, D-06)

## Task Commits

Each task was committed atomically:

1. **Task 1: 처치 체인 훅 — 슬래시(CombatController) + 사망 글리치(EnemyDeathEffect)** - `5029bb7` (feat)
2. **Task 2: 포탈 전환 훅 — 진입 상승음 / 퇴장 하강음** - `f3ae8aa` (feat)

**Plan metadata:** (this commit, follows)

## Files Created/Modified
- `Assets/Scripts/Player/CombatController.cs` - ExecuteDash step 6에 `AudioManager.PlaySfx(Sfx.Slash);` 1줄 추가
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` - PlayDeathSequence step 2에 `AudioManager.PlaySfx(Sfx.EnemyDeathGlitch);` 1줄 추가
- `Assets/Scripts/World/FloorTransitionEffect.cs` - PlayEntry/PlayExit 첫 실행문에 각각 `AudioManager.PlaySfx(Sfx.PortalEnter);` / `AudioManager.PlaySfx(Sfx.PortalExit);` 추가 (총 2줄)

## Decisions Made
- 계획대로 진행 — 별도 아키텍처 결정 없음. 코드 배치 위치(HitFreeze 이전, 파티클 직전, 코루틴 첫 줄)는 계획에 명시된 그대로 적용.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- SFX-02/03/04 코드 배선 100% 완료. 이 플랜만으로는 클립이 아직 연결되지 않아 무음이 정상(PlaySfx는 null 안전, RESEARCH.md 검증).
- 13-04(체크포인트 포함 사운드 폴리싱 플랜)에서 클립 연결 + 프리팹 생성 + 청취 검증 예정.
- 13-02(병렬 실행 — 오디오 에셋/AudioManagerPrefabBuilder)와 파일 충돌 없음 확인.

---
*Phase: 13-audio-foundation-sound-polish*
*Completed: 2026-07-09*

## Self-Check: PASSED

- FOUND: Assets/Scripts/Player/CombatController.cs
- FOUND: Assets/Scripts/Enemy/EnemyDeathEffect.cs
- FOUND: Assets/Scripts/World/FloorTransitionEffect.cs
- FOUND: .planning/phases/13-audio-foundation-sound-polish/13-03-SUMMARY.md
- FOUND: 5029bb7 (feat(13-03): wire slash and death glitch SFX hooks)
- FOUND: f3ae8aa (feat(13-03): wire portal enter/exit SFX hooks)
