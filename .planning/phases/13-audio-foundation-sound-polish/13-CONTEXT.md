# Phase 13: 오디오 기반 구축 & 연출 사운드 폴리싱 - Context

**Gathered:** 2026-07-08
**Status:** ⏸ IN PROGRESS — 논의 미완료 (아래 "Remaining Discussion" 참고)

> **다음 discuss-phase 세션 안내:** 사용자가 논의 중간에 저장을 요청함.
> "사운드 에셋 조달" 영역은 논의 완료. **"사운드 스타일/톤"과 "슬로우모션 중
> 오디오 처리" 영역부터 이어서 논의할 것.** 완료 전까지 plan-phase로 넘어가지 말 것.

<domain>
## Phase Boundary

프로젝트 최초의 오디오 재생 인프라(AudioManager)를 신설하고, Phase 12에서 완성된 연출 훅 3곳(포탈 전환 `FloorTransitionEffect`, 히트 임팩트 `CombatController.ExecuteDash()`, 적 사망 `EnemyDeathEffect`)에 사운드를 추가하며, 기존 연출의 타이밍·피드백 어색함(SFX-06)을 개선한다.

**Requirements in scope:** SFX-01(AudioManager), SFX-02(포탈), SFX-03(히트), SFX-04(사망), SFX-06(타이밍 폴리싱)
**Not in scope:** SFX-05(보스 스폰 사운드 — Phase 16), BGM/음악 시스템(마일스톤 범위 외), 볼륨 설정 UI, FMOD/Wwise 미들웨어

</domain>

<decisions>
## Implementation Decisions

### 이전 마일스톤/로드맵에서 승계된 확정 사항 (재논의 불필요)
- **D-00a:** AudioManager = MonoBehaviour 싱글턴 + 풀링된 AudioSource 배열, `DontDestroyOnLoad`로 3개 씬(MainMenu/AttackSelect/SampleScene) 유지 — v3.1 로드맵 킥오프 확정 (STATE.md Key Decisions)
- **D-00b:** 모든 오디오 타이밍 코드는 `Time.unscaledDeltaTime` / `WaitForSecondsRealtime` — 슬로우모션 면역 (전 마일스톤 공통 기술 제약)
- **D-00c:** SFX 전용 — BGM/적응형 음악 시스템은 이번 마일스톤 명시적 제외 (research SUMMARY.md)
- **D-00d:** 오디오 미들웨어(FMOD/Wwise) 도입 안 함 — 내장 AudioSource로 충분 (research 확정)

### 사운드 에셋 조달 (2026-07-08 논의 완료)
- **D-01:** 사운드 에셋은 **무료 CC0 팩 다운로드**로 조달한다 (Kenney 등 저작권 자유 소스). 사용자 직접 제공/코드 생성 플레이스홀더 방식은 배제.
- **D-02:** 다운로드한 CC0 팩은 **통째로 프로젝트에 임포트**한다 — 이후 페이즈(Phase 16 보스 스폰 사운드 등)에서 재다운로드 없이 활용하기 위함. 선별 임포트 방식은 배제.
- **D-03:** 어떤 사운드 파일을 어느 연출에 쓸지는 **Claude 재량으로 선별하고, 플레이테스트 검수로 확정**한다 — 어색하면 교체. 사용자 사전 승인 절차는 두지 않는다.

### Claude's Discretion
- SFX-06(타이밍·피드백 어색함 개선)의 구체 대상 — 사용자가 논의 영역으로 선택하지 않음. 플레이테스트 기반으로 Claude가 판단해 개선 (이펙트-사운드 싱크, 지연 불일치 등)
- 오디오 파일 포맷/임포트 설정 (모바일 예산 고려한 압축 설정 등)

</decisions>

<remaining_discussion>
## ⏸ Remaining Discussion (다음 세션에서 이어서)

사용자가 논의하기로 선택했으나 아직 다루지 않은 영역:

1. **사운드 스타일/톤** — 레트로 8비트 vs 현실적 임팩트 vs SF/디지털(HELIX 시뮬레이션 세계관). CC0 팩 선택 기준이 됨 (D-01~D-03과 직결).
2. **슬로우모션 중 오디오 처리** — 히트 임팩트가 슬로우모션/히트프리즈 한가운데 재생됨. 정상 피치 유지(성공 기준 3의 기본 해석) vs 슬로우모션 체감 강화용 피치 다운 연출.

논의하지 않기로 한 영역 (Claude 재량 확정):
- SFX-06 어색함 구체화 — 사용자가 선택하지 않음 → Claude's Discretion에 반영됨

</remaining_discussion>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 마일스톤 결정/제약
- `.planning/STATE.md` §Key Decisions Locked for v3.1 — AudioManager 아키텍처 확정 사항
- `.planning/STATE.md` §Technical Constraints to Enforce Every Phase — unscaled time 컨벤션
- `.planning/ROADMAP.md` §Implementation Notes — AudioManager 풀링/싱글턴 노트, 빌드 순서
- `.planning/research/SUMMARY.md` — 오디오 시스템 리서치 (풀링 근거, Pitfall 6/9: deltaTime 데싱크, 연속 처치 SFX 클리핑/GC)

### 사운드를 붙일 연출 훅 (Phase 12 산출물)
- `Assets/Scripts/World/FloorTransitionEffect.cs` — 포탈 진입/퇴장 연출 (SFX-02 훅 지점)
- `Assets/Scripts/Player/CombatController.cs` — `ExecuteDash()` 처치 분기: 히트 스파크+카메라 쉐이크+히트프리즈 (SFX-03 훅 지점)
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` — 적 사망 연출 (SFX-04 훅 지점)
- `.planning/phases/12-animation-polish/12-CONTEXT.md` — Phase 12 연출 결정사항 (타이밍 수치 등)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- 오디오 파일 **0개**, `AudioSource`/`AudioClip` 사용 코드 **0건** — 완전 신규 구축 (D-01로 조달 확정)
- Phase 12 연출 컴포넌트 3종이 모두 완성·안정 상태 — 사운드는 순수 추가(additive) 작업

### Established Patterns
- 매니저 패턴 이원화: 순수 static(`ScoreManager`/`FloorTimer`) vs MonoBehaviour(`WorldGenerator`) — AudioManager는 실제 AudioSource 컴포넌트를 소유해야 하므로 MonoBehaviour 싱글턴 (D-00a)
- 타이머/코루틴 전부 `WaitForSecondsRealtime`/`Time.unscaledDeltaTime` 기반
- 이펙트 컴포넌트는 단일 책임 독립 파일로 분리 (`FloorTransitionEffect`, `EnemyDeathEffect` 등)

### Integration Points
- `FloorTransitionEffect` 진입/퇴장 재생 시점 → 포탈 사운드
- `CombatController.ExecuteDash()` 처치 성공 분기 (HitFreeze 호출부 인근) → 히트 임팩트 사운드
- `EnemyDeathEffect` 연출 시작 시점 → 사망 사운드
- 3개 씬 전환 흐름(MainMenu → AttackSelect → SampleScene) → AudioManager DontDestroyOnLoad 생존 검증 지점

</code_context>

<specifics>
## Specific Ideas

- 사용자가 CC0 팩 "통째 임포트"를 선택한 이유: 이후 페이즈에서 고를 수 있도록 — 사운드 라이브러리를 프로젝트 안에 상비하는 방향
- 선별은 Claude에게 맡기되 플레이테스트가 최종 검수 — 진행이 막히지 않는 것을 우선시함

</specifics>

<deferred>
## Deferred Ideas

없음 — 현재까지 논의가 Phase 범위 내에 머무름.

</deferred>

---

*Phase: 13-audio-foundation-sound-polish*
*Context gathered: 2026-07-08 (부분 — 스타일/톤, 슬로우모션 오디오 처리 미논의)*
