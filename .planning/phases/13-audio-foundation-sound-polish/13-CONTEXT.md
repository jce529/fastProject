# Phase 13: 오디오 기반 구축 & 연출 사운드 폴리싱 - Context

**Gathered:** 2026-07-08 ~ 2026-07-09
**Status:** Ready for planning

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

### 사운드 스타일/톤 (2026-07-09 논의 완료)
- **D-04:** 전체 사운드 톤은 **SF/디지털** — 전자음/글리치/신시사이저 계열. HELIX 시뮬레이션 세계관 및 기존 포탈/오버클럭 비주얼과 일치. CC0 팩 선택 기준이 된다 (Kenney Sci-Fi Sounds 등).
- **D-05:** 히트 임팩트(SFX-03)와 적 사망(SFX-04)은 **2단 콤보 구성** — 대시 처치 순간 = **날카로운 슬래시(참격)음**, 이어서 적이 죽고 흩어질 때 = **글리치/디지털 노이즈**(시뮬레이션 NPC가 깨지는 느낌). 슬래시→노이즈가 시간차로 이어지는 레이어 설계.
- **D-06:** 포탈 사운드(SFX-02)는 진입/퇴장 구분 — **진입 = 상승하는 워프/텔레포트음, 퇴장(다음 층 등장) = 하강하며 마무리되는 음**. 방향감으로 층 전환 리듬을 표현.

### 슬로우모션 중 오디오 처리 (2026-07-09 논의 완료)
- **D-07:** AudioManager는 **2채널 구조**로 설계한다 (사용자 제안) — **액션 채널**: 타임스케일과 무관하게 항상 정상 피치/타이밍. **배경/주변 채널**: `Time.timeScale`을 추종해 슬로우모션 중 피치다운 연출(시간이 느려진 청각적 체감). 채널별로 AudioSource 그룹을 분리.
- **D-08:** 이번 페이즈의 사운드 3종(포탈, 슬래시, 사망 노이즈)은 **모두 액션 채널**에 배정 — 전부 정상 피치로 재생. 배경/주변 채널은 피치다운 인프라만 구축해 두고 비워둔다 (향후 BGM/주변음 마일스톤에서 사용).

### Claude's Discretion
- SFX-06(타이밍·피드백 어색함 개선)의 구체 대상 — 사용자가 논의 영역으로 선택하지 않음. 플레이테스트 기반으로 Claude가 판단해 개선 (이펙트-사운드 싱크, 지연 불일치 등)
- 오디오 파일 포맷/임포트 설정 (모바일 예산 고려한 압축 설정 등)
- 포탈 진입/퇴장 상승/하강음, 슬래시/노이즈의 구체 클립 선별 — D-03 원칙(재량 선별 + 플레이테스트 검수) 적용
- 배경 채널 피치다운 커브/최저 피치 하한 등 구현 세부 — 이번 페이즈에선 채널 인프라만 있으면 되므로 (D-08) 검증 가능한 최소 수준으로

</decisions>

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
- `FloorTransitionEffect` 진입/퇴장 재생 시점 → 포탈 사운드 (진입 상승음/퇴장 하강음, D-06)
- `CombatController.ExecuteDash()` 처치 성공 분기 (HitFreeze 호출부 인근) → 슬래시 사운드 (D-05)
- `EnemyDeathEffect` 연출 시작 시점 → 글리치/디지털 노이즈 (D-05, 슬래시와 시간차 레이어)
- 3개 씬 전환 흐름(MainMenu → AttackSelect → SampleScene) → AudioManager DontDestroyOnLoad 생존 검증 지점
- 오버클럭(슬로우모션) 진입/해제 시점 → 배경 채널 피치다운 인프라의 타임스케일 추종 지점 (D-07, 이번 페이즈엔 소비 클립 없음)

</code_context>

<specifics>
## Specific Ideas

- 사용자가 CC0 팩 "통째 임포트"를 선택한 이유: 이후 페이즈에서 고를 수 있도록 — 사운드 라이브러리를 프로젝트 안에 상비하는 방향
- 선별은 Claude에게 맡기되 플레이테스트가 최종 검수 — 진행이 막히지 않는 것을 우선시함
- 히트 사운드에 대한 사용자 직접 묘사: "플레이어가 썰어버릴 때는 슬래시, 이후 적이 죽고 흩어질 때 노이즈로" — 처치의 인과(벤다 → 깨진다)를 소리로 표현
- 2채널 구조는 사용자 본인의 설계 제안: "두 개의 오디오 재생 오브젝트를 나눠놓고 배경음/주변음은 피치다운 연출과 타임스케일을 따라가고 액션음은 타임스케일과 별도로"

</specifics>

<deferred>
## Deferred Ideas

- **배경/주변음(앰비언트) 및 BGM 콘텐츠** — 2채널 구조의 배경 채널을 실제로 사용하는 것은 향후 마일스톤 (이번 페이즈는 인프라만, D-08)

</deferred>

---

*Phase: 13-audio-foundation-sound-polish*
*Context gathered: 2026-07-08 (에셋 조달) + 2026-07-09 (스타일/톤, 슬로우모션 오디오) — 논의 완료*
