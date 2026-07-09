# Phase 13: 오디오 기반 구축 & 연출 사운드 폴리싱 - Research

**Researched:** 2026-07-09
**Domain:** Unity 6 built-in audio (AudioSource pooling, timeScale interaction, mobile import settings) + CC0 SFX asset sourcing
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**이전 마일스톤/로드맵에서 승계된 확정 사항 (재논의 불필요)**
- **D-00a:** AudioManager = MonoBehaviour 싱글턴 + 풀링된 AudioSource 배열, `DontDestroyOnLoad`로 3개 씬(MainMenu/AttackSelect/SampleScene) 유지 — v3.1 로드맵 킥오프 확정 (STATE.md Key Decisions)
- **D-00b:** 모든 오디오 타이밍 코드는 `Time.unscaledDeltaTime` / `WaitForSecondsRealtime` — 슬로우모션 면역 (전 마일스톤 공통 기술 제약)
- **D-00c:** SFX 전용 — BGM/적응형 음악 시스템은 이번 마일스톤 명시적 제외 (research SUMMARY.md)
- **D-00d:** 오디오 미들웨어(FMOD/Wwise) 도입 안 함 — 내장 AudioSource로 충분 (research 확정)

**사운드 에셋 조달 (2026-07-08 논의 완료)**
- **D-01:** 사운드 에셋은 **무료 CC0 팩 다운로드**로 조달한다 (Kenney 등 저작권 자유 소스). 사용자 직접 제공/코드 생성 플레이스홀더 방식은 배제.
- **D-02:** 다운로드한 CC0 팩은 **통째로 프로젝트에 임포트**한다 — 이후 페이즈(Phase 16 보스 스폰 사운드 등)에서 재다운로드 없이 활용하기 위함. 선별 임포트 방식은 배제.
- **D-03:** 어떤 사운드 파일을 어느 연출에 쓸지는 **Claude 재량으로 선별하고, 플레이테스트 검수로 확정**한다 — 어색하면 교체. 사용자 사전 승인 절차는 두지 않는다.

**사운드 스타일/톤 (2026-07-09 논의 완료)**
- **D-04:** 전체 사운드 톤은 **SF/디지털** — 전자음/글리치/신시사이저 계열. HELIX 시뮬레이션 세계관 및 기존 포탈/오버클럭 비주얼과 일치. CC0 팩 선택 기준이 된다 (Kenney Sci-Fi Sounds 등).
- **D-05:** 히트 임팩트(SFX-03)와 적 사망(SFX-04)은 **2단 콤보 구성** — 대시 처치 순간 = **날카로운 슬래시(참격)음**, 이어서 적이 죽고 흩어질 때 = **글리치/디지털 노이즈**(시뮬레이션 NPC가 깨지는 느낌). 슬래시→노이즈가 시간차로 이어지는 레이어 설계.
- **D-06:** 포탈 사운드(SFX-02)는 진입/퇴장 구분 — **진입 = 상승하는 워프/텔레포트음, 퇴장(다음 층 등장) = 하강하며 마무리되는 음**. 방향감으로 층 전환 리듬을 표현.

**슬로우모션 중 오디오 처리 (2026-07-09 논의 완료)**
- **D-07:** AudioManager는 **2채널 구조**로 설계한다 (사용자 제안) — **액션 채널**: 타임스케일과 무관하게 항상 정상 피치/타이밍. **배경/주변 채널**: `Time.timeScale`을 추종해 슬로우모션 중 피치다운 연출(시간이 느려진 청각적 체감). 채널별로 AudioSource 그룹을 분리.
- **D-08:** 이번 페이즈의 사운드 3종(포탈, 슬래시, 사망 노이즈)은 **모두 액션 채널**에 배정 — 전부 정상 피치로 재생. 배경/주변 채널은 피치다운 인프라만 구축해 두고 비워둔다 (향후 BGM/주변음 마일스톤에서 사용).

### Claude's Discretion
- SFX-06(타이밍·피드백 어색함 개선)의 구체 대상 — 사용자가 논의 영역으로 선택하지 않음. 플레이테스트 기반으로 Claude가 판단해 개선 (이펙트-사운드 싱크, 지연 불일치 등)
- 오디오 파일 포맷/임포트 설정 (모바일 예산 고려한 압축 설정 등)
- 포탈 진입/퇴장 상승/하강음, 슬래시/노이즈의 구체 클립 선별 — D-03 원칙(재량 선별 + 플레이테스트 검수) 적용
- 배경 채널 피치다운 커브/최저 피치 하한 등 구현 세부 — 이번 페이즈에선 채널 인프라만 있으면 되므로 (D-08) 검증 가능한 최소 수준으로

### Deferred Ideas (OUT OF SCOPE)
- **배경/주변음(앰비언트) 및 BGM 콘텐츠** — 2채널 구조의 배경 채널을 실제로 사용하는 것은 향후 마일스톤 (이번 페이즈는 인프라만, D-08)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SFX-01 | 기본 오디오 재생 인프라(AudioManager)가 추가된다 | Architecture Patterns §1-3 (bootstrap + 2채널 풀 + enum API), Code Examples §AudioManager, Pitfall 1/4/5 |
| SFX-02 | 포탈 전환에 사운드가 추가된다 | Integration seam: `FloorTransitionEffect.PlayEntry()/PlayExit()` (Code Examples §Hook 2), D-06 클립 후보 (Standard Stack §클립 선별 가이드) |
| SFX-03 | 히트 임팩트에 사운드가 추가된다 | Integration seam: `CombatController.ExecuteDash()` step 6 (Code Examples §Hook 1), Pitfall 1 (HitFreeze 중 DSP 재생 계속됨 — 정상 동작) |
| SFX-04 | 적 사망에 사운드가 추가된다 | Integration seam: `EnemyDeathEffect.PlayDeathSequence()` 파티클 시점 (Code Examples §Hook 3), Pitfall 8 (슬래시→노이즈 시간차가 자연 발생하는 이유) |
| SFX-06 | 포탈전환/히트/사망 연출의 타이밍·피드백 어색함이 개선된다 | Architecture Patterns §4 (SFX-06 폴리싱 후보 목록 — 검증된 코드 리딩 기반), Pitfall 8 |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **GSD 워크플로우 필수** — 모든 파일 변경은 GSD 명령(`/gsd:execute-phase` 등)을 통해 진행
- **단순성 우선 / 오버엔지니어링 금지** — 프로토타입 검증 범위 밖 기능 추가 금지 (예: 볼륨 설정 UI, AudioMixer 덕킹 체인 등은 이번 페이즈 금지)
- **Phase 격리** — Phase 16(보스 스폰 사운드) 코드를 미리 작성하지 않음. 단, D-02에 따라 팩 전체 임포트는 허용(에셋은 상비, 코드는 격리)
- **정밀한 변경** — 기존 연출 컴포넌트 3종에는 사운드 호출 추가만, 임의 리팩토링 금지
- **기존 기술 제약 (STATE.md §Technical Constraints):** `Time.unscaledDeltaTime`/`WaitForSecondsRealtime`만 사용, Update 내 `FindObjectsOfType`/LINQ 금지, 프리앨록 버퍼 패턴 유지
- **플랫폼:** Android 우선 (ARM64, minSdk 25) — 모바일 메모리/CPU 예산 고려한 오디오 임포트 설정 필수
- **응답 언어:** 대화는 한국어, 코드/커밋은 기존 컨벤션 (영문 커밋 prefix 등)

## Summary

이 페이즈는 100% Unity 내장 오디오 모듈(`com.unity.modules.audio`, 이미 설치됨)로 구현 가능하며 신규 UPM 패키지가 전혀 필요 없다. 핵심 발견: **Unity의 오디오 DSP 클럭은 `Time.timeScale`의 영향을 전혀 받지 않는다** — 슬로우모션(0.2x)이나 HitFreeze(timeScale=0) 중에도 AudioSource는 정상 피치/속도로 계속 재생된다. 즉 D-07의 "액션 채널"은 아무 것도 하지 않아도 공짜로 얻어지며, 실제 구현 작업은 반대쪽인 "배경 채널"(매 프레임 `pitch = Time.timeScale` 추종, 하한 클램프)에만 존재한다. D-00b(unscaled time)는 오디오 재생 자체가 아니라 사운드를 트리거하는 코루틴/타이머 코드에 적용되는 제약이다.

에셋 조달은 Kenney의 **Sci-Fi Sounds(70개)** + **Digital Audio(60개)** 두 CC0 팩으로 확정한다 — D-04의 SF/디지털 톤에 정확히 부합하고(레이저/워프/엔진/전자음 계열), 상승·하강 톤(D-06 포탈), 레이저 슬래시(D-05 처치음), 글리치성 전자음(D-05 사망 노이즈)이 모두 한 세트 안에서 나온다. 통째 임포트(D-02) 시 약 130개 파일이 들어오므로, `Assets/Audio/` 폴더 대상 `AssetPostprocessor`로 모바일 임포트 설정(Force To Mono + Decompress On Load + ADPCM)을 일괄 자동 적용하는 것이 수작업 130회를 대체하는 표준 방법이다.

가장 주의할 함정 두 가지: (1) 프로젝트의 DSP Buffer Size가 현재 1024(Best Performance)로 설정되어 있어 히트 사운드에 체감 가능한 지연이 생긴다 — 512(Good Latency)로 낮춰야 한다. (2) `EnemyDeathEffect`는 Die 애니메이션(scaled time) 완주를 기다린 뒤 파티클을 재생하므로, HitFreeze 75ms 동안 애니메이션이 정지해 슬래시→사망 노이즈 간격이 자연히 0.5초 이상 벌어진다 — 이것이 D-05의 "시간차 레이어"를 공짜로 만들어 주지만, 간격이 너무 길게 느껴지면 SFX-06 폴리싱 대상이 된다.

**Primary recommendation:** 신규 패키지 없이 내장 AudioSource로 구현. `Resources/AudioManager.prefab` + `RuntimeInitializeOnLoadMethod` 부트스트랩(기존 `GameBootstrapper` 패턴 동일)으로 3개 씬 어디서든 싱글턴 보장, 액션 8보이스/배경 2보이스 라운드로빈 풀, Kenney Sci-Fi Sounds + Digital Audio 팩 임포트, DSP Buffer 512로 변경.

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `com.unity.modules.audio` | 1.0.0 (설치됨) | AudioSource/AudioClip/AudioSettings | Unity 내장, 프로토타입 SFX 4종에 충분. 신규 설치 불필요 |
| Kenney Sci-Fi Sounds | 현행 (CC0) | 워프/레이저/엔진/포스필드 계열 70개 | D-04 SF 톤 정확 부합, CC0 저작권 자유, OGG 포맷 |
| Kenney Digital Audio | 현행 (CC0) | 전자음/재프/톤 상승·하강 계열 60개 | D-06 상승·하강 포탈음 + D-05 글리치 노이즈 후보 다수 |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `AssetPostprocessor` (UnityEditor) | 내장 | `Assets/Audio/` 임포트 설정 일괄 자동화 | 팩 통째 임포트(D-02, ~130파일) 시 수작업 대체 — 임포트 전에 Editor 스크립트 먼저 커밋 |
| `AudioSettings.dspTime` | 내장 | 연속 처치 시 동일 클립 30ms 내 재트리거 가드 | AudioManager 내부 클리핑 방지 로직 |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| 내장 AudioSource | FMOD/Wwise | D-00d로 명시적 배제 — 프로토타입 SFX 4종에 과잉 |
| 씬 배치 싱글턴 (3개 씬 각각 배치) | `RuntimeInitializeOnLoadMethod` + Resources 프리팹 | 씬 3개를 전부 수정해야 하고 중복 파괴 가드 의존 — 부트스트랩 방식이 씬 수정 0회로 동일 결과 (기존 `GameBootstrapper` 선례 있음) |
| 2채널 = AudioMixer 그룹 2개 | 2채널 = AudioSource 배열 2개 (per-source pitch) | 피치다운은 per-source `pitch` 프로퍼티로 충분 — AudioMixer는 볼륨 설정 UI가 생기는 미래 마일스톤에서 도입 (지금은 오버엔지니어링) |
| Kenney Impact Sounds 팩 추가 | Sci-Fi + Digital 2팩만 | Impact Sounds는 목재/금속 물리 타격음 위주 — D-04 SF/디지털 톤과 불일치. 플레이테스트에서 슬래시음이 부족할 때만 추가 검토 |

**Installation:**
```
1. https://kenney.nl/assets/sci-fi-sounds  → kenney_sci-fi-sounds.zip 다운로드
2. https://kenney.nl/assets/digital-audio  → kenney_digital-audio.zip 다운로드
3. 압축 해제 → Assets/Audio/Kenney_SciFiSounds/, Assets/Audio/Kenney_DigitalAudio/ 로 복사
   (AudioImportSettings.cs Editor 스크립트를 먼저 넣은 뒤 복사해야 임포트 설정이 자동 적용됨)
```

### 클립 선별 가이드 (D-03: Claude 재량 + 플레이테스트 검수)

| 용도 | 결정 | 후보 계열 (팩 내 파일명 패턴) | 요구 특성 |
|------|------|------------------------------|-----------|
| 포탈 진입 (D-06) | 상승 워프음 | Digital Audio의 상승 톤(`*Up*`, `phaserUp*`, `powerUp*`), Sci-Fi의 `forceField*`/`doorOpen*` | 길이 ≈ 0.5–0.8s (PlayEntry 총 0.7s와 정합) |
| 포탈 퇴장 (D-06) | 하강 마무리음 | Digital Audio의 하강 톤(`*Down*`, `phaserDown*`), Sci-Fi의 `doorClose*` | 길이 ≈ 0.8–1.2s (PlayExit 총 ~1.2s와 정합) |
| 슬래시/처치 (D-05) | 날카로운 참격음 | Sci-Fi의 `laserSmall*`/`laserRetro*`, Digital Audio의 `laser*`/`zap*` | 어택 즉각적, 길이 ≤ 0.3s (HitFreeze 75ms 동안에도 DSP는 계속 재생됨) |
| 사망 글리치 (D-05) | 디지털 붕괴 노이즈 | Digital Audio의 `spaceTrash*`/저음 재프, Sci-Fi의 `computerNoise*` | 지글거리는 노이즈 질감, 길이 ≈ 0.3–0.6s (마스크 상승 0.6s와 정합) |

정확한 파일명은 다운로드 후 확정 (LOW confidence — 파일명 패턴은 학습 데이터 기반 추정, 팩 구성 자체는 공식 페이지로 확인됨). 선별 실패 리스크는 D-03의 "플레이테스트에서 교체" 원칙으로 흡수된다.

## Architecture Patterns

### Recommended Project Structure

```
Assets/
├── Audio/                          # 신규 — CC0 팩 통째 임포트 (D-02)
│   ├── Kenney_SciFiSounds/
│   └── Kenney_DigitalAudio/
├── Resources/
│   └── AudioManager.prefab         # 신규 — 부트스트랩이 로드하는 프리팹 (클립 SerializeField 연결)
├── Scripts/
│   └── Audio/
│       └── AudioManager.cs         # 신규 — 싱글턴 + 2채널 풀 + enum API
└── Editor/
    └── AudioImportSettings.cs      # 신규 — Assets/Audio/ 임포트 설정 자동화
```

### Pattern 1: RuntimeInitializeOnLoadMethod 부트스트랩 (씬 수정 0회로 3개 씬 생존 보장)

**What:** `Resources.Load`한 프리팹을 `BeforeSceneLoad` 시점에 Instantiate + `DontDestroyOnLoad`. 씬 파일을 하나도 건드리지 않고 MainMenu/AttackSelect/SampleScene 전부에서 싱글턴이 존재함을 보장.
**When to use:** 이 프로젝트의 표준 — `GameBootstrapper.cs`가 이미 동일 어트리뷰트로 MainMenu 강제 로드를 하고 있다 (검증된 프로젝트 선례). 사망 루프(SampleScene → AttackSelect → SampleScene 재로드)에서도 인스턴스가 유지된다.
**Why not 씬 배치:** 씬 3개 수정 + 중복 인스턴스 파괴 가드에 의존해야 하고, 에디터에서 SampleScene부터 Play해도 `GameBootstrapper`가 MainMenu로 리다이렉트하므로 부트스트랩 방식과 커버리지가 동일하다.

### Pattern 2: 2채널 라운드로빈 AudioSource 풀 (D-07)

**What:** AudioManager 자식으로 액션 채널 8보이스 + 배경 채널 2보이스의 AudioSource를 생성. `PlaySfx()`는 액션 풀에서 라운드로빈으로 다음 소스를 골라 재생 — 연속 처치 시 이전 사운드가 끊기지 않는다.
**핵심 사실 (검증됨):** AudioSource 재생은 `Time.timeScale`과 완전히 독립이다. 슬로우모션(0.2x)·HitFreeze(0f) 중에도 DSP 클럭은 실시간으로 돌며 사운드는 정상 피치로 계속 재생된다. **액션 채널은 추가 코드가 0줄** — timeScale을 건드리지 않는 것이 곧 구현이다. 배경 채널만 `LateUpdate()`에서 `pitch = Mathf.Max(minPitch, Time.timeScale)`로 추종시킨다 (D-07). `minPitch` 클램프가 필수인 이유: HitFreeze가 timeScale을 0으로 만들면 pitch=0이 되어 재생이 사실상 정지한다 (Pitfall 1).
**이번 페이즈 범위:** 배경 채널은 인프라(풀 생성 + pitch 추종 루프)만 만들고 소비 클립은 없음 (D-08). 검증은 에디터에서 배경 풀 소스의 pitch 값이 슬로우모션 중 0.2로 내려가는지 Inspector 확인으로 충분.

### Pattern 3: enum 키 + SerializeField 클립 매핑 API

**What:** `AudioManager.PlaySfx(Sfx.Slash)` 형태의 static 진입점. 클립은 프리팹의 SerializeField로 연결 — 호출부(3개 연출 컴포넌트)는 AudioClip 참조를 전혀 갖지 않는다.
**Why:** D-03(플레이테스트 중 클립 교체)을 프리팹 Inspector에서 코드 수정 없이 수행 가능. `Resources.Load` per-play 호출(GC/IO) 회피. Phase 16이 `Sfx.BossSpawn` enum 값 하나 추가로 확장 가능.

### Pattern 4: SFX-06 폴리싱 후보 (Claude 재량 — 코드 리딩으로 확인된 실제 심)

플레이테스트에서 아래 순서로 점검하고, 어색한 항목만 수정한다 (정밀 변경 원칙):

1. **슬래시→사망 노이즈 간격:** `EnemyDeathEffect.PlayDeathSequence()`는 Die 애니메이션(scaled time) 완주를 기다린 후 파티클을 재생한다. HitFreeze 75ms 동안 Animator가 정지하므로 실제 간격 = 75ms + Die 애니메이션 길이. 간격이 과하면 노이즈 훅을 파티클 시점이 아니라 애니메이션 대기 도중(예: normalizedTime 0.5)으로 앞당긴다.
2. **히트 스파크 vs HitFreeze:** `SpawnHitSpark()` 직후 timeScale=0이 되므로 ParticleSystem(scaled time 기본)이 스폰 프레임에 75ms 정지한다 — "임팩트 프레임"으로 긍정적일 수 있으나, 어색하면 스파크 프리팹의 ParticleSystem `useUnscaledTime = true` 검토.
3. **포탈음-마스크 싱크:** PlayEntry(0.4s 마스크 + 0.3s 수축), PlayExit(0.4s 성장 + 0.5s 마스크 + 0.3s 페이드) — 클립 길이가 구간과 안 맞으면 클립 교체가 1순위, 연출 duration 수치(SerializeField) 조정이 2순위.
4. **카메라 쉐이크:** 이미 `Time.unscaledDeltaTime` 기반으로 HitFreeze 중 정상 동작 확인됨 (CameraFollow.cs:89) — 수정 불필요.

### Anti-Patterns to Avoid

- **`AudioSource.pitch`를 액션 채널에서 timeScale과 연동:** D-07/D-08 위반. 액션 채널 pitch는 랜덤 미세 변주(0.95–1.05) 외에는 건드리지 않는다.
- **호출부에서 `GetComponent<AudioSource>()` + 개별 AudioSource 배치:** 풀링 결정(D-00a) 위반, 씬/프리팹마다 소스가 흩어져 2채널 정책을 강제할 수 없게 된다.
- **`WaitForSeconds`로 사운드 지연/시퀀싱:** HitFreeze(timeScale=0) 중 영원히 재개되지 않는다. 반드시 `WaitForSecondsRealtime` (D-00b, 프로젝트 전체 컨벤션).
- **AudioManager에 AudioListener 추가:** AudioListener는 각 씬 Main Camera에 이미 1개씩 존재 — 2개가 되면 콘솔 경고 스팸. 2D 사운드(spatialBlend=0)는 리스너 위치와 무관하므로 추가 불필요.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| 사운드 에셋 | 코드 생성 사인파/노이즈 플레이스홀더 | Kenney CC0 팩 (D-01로 확정) | D-01이 명시적으로 배제. 품질·톤 일관성 확보 불가 |
| 슬로우모션 중 오디오 유지 | 커스텀 실시간 오디오 스케줄러 | 내장 AudioSource 그대로 | DSP 클럭이 이미 timeScale 독립 — 문제 자체가 존재하지 않음 |
| 130개 파일 임포트 설정 | 파일별 수동 Inspector 설정 | `AssetPostprocessor.OnPreprocessAudio()` | 수작업 130회 + 신규 파일 추가 시 누락 위험 vs 폴더 규칙 1개 |
| 오디오 믹싱/덕킹/스냅샷 | AudioMixer 그래프 구축 | per-source `volume`/`pitch` | SFX 4종에 믹서는 오버엔지니어링 (CLAUDE.md 단순성 원칙) — 볼륨 UI 마일스톤에서 도입 |
| 3개 씬 싱글턴 생존 | 씬별 프리팹 배치 + 중복 가드 조합 | `RuntimeInitializeOnLoadMethod` + Resources 프리팹 | 씬 수정 0회, `GameBootstrapper` 선례와 동일 패턴 |

**Key insight:** Unity 오디오에서 "슬로우모션 대응"은 만드는 것이 아니라 *안 만드는 것*이다. timeScale에 영향받는 것은 사운드가 아니라 사운드를 트리거하는 게임 로직 쪽이며, 이 프로젝트는 이미 전면 unscaled 컨벤션이라 트리거 타이밍도 안전하다.

## Common Pitfalls

### Pitfall 1: 배경 채널 pitch=0 정지 (HitFreeze)
**What goes wrong:** 배경 채널이 `pitch = Time.timeScale`을 그대로 추종하면 HitFreeze(timeScale=0) 순간 pitch가 0이 되어 재생이 멈추고, 복귀 시 뚝 끊긴 지점부터 재개되어 청각적으로 거슬린다.
**Why it happens:** pitch 0은 재생 속도 0과 동일.
**How to avoid:** `pitch = Mathf.Max(_minAmbientPitch, Time.timeScale)` — 하한 0.3 권장 (구체 값은 Claude 재량 항목, 이번 페이즈는 소비 클립이 없으므로 SerializeField로 노출만 해두면 충분).
**Warning signs:** 슬로우모션 진입/처치 순간 배경음이 딸깍거리거나 침묵.

### Pitfall 2: DSP Buffer Size 1024 → 히트 사운드 체감 지연
**What goes wrong:** 현재 `ProjectSettings/AudioManager.asset`의 `m_DSPBufferSize: 1024`(Best Performance)는 모바일에서 수십 ms 추가 지연을 만든다. "대시 도착 순간 슬래시"라는 코어 손맛에서 20ms 이상 지연은 체감된다.
**Why it happens:** Unity 소프트웨어 믹서의 링 버퍼가 클수록 안정적이지만 지연이 커진다.
**How to avoid:** Project Settings → Audio → DSP Buffer Size = **Good latency (512)**. Best latency(256)는 Windows 에디터 스터터/저사양 Android 크래클 이슈가 보고되어 있어(Unity Issue Tracker) 512가 안전한 중간값. Android 실기기에서 지연이 여전히 크면 그때 256 실험.
**Warning signs:** 대시 도착 비주얼과 슬래시음 사이 인지 가능한 어긋남.

### Pitfall 3: 연속 처치 시 동일 클립 중첩 클리핑
**What goes wrong:** 좁은 방에서 2-3연속 처치 시 같은 슬래시 클립이 수십 ms 간격으로 겹쳐 위상 중첩으로 볼륨이 2배가 되고 기계적으로 들린다.
**How to avoid:** (a) 라운드로빈 풀로 보이스는 분리하되, (b) `AudioSettings.dspTime` 기준 동일 클립 30ms 내 재트리거는 스킵, (c) 재생마다 pitch 0.95–1.05 랜덤 변주.
**Warning signs:** 연속 처치 시 소리가 갑자기 커지거나 "기관총" 느낌.

### Pitfall 4: DontDestroyOnLoad 중복 인스턴스
**What goes wrong:** 부트스트랩과 별개로 씬에도 AudioManager를 배치하면 씬 재로드마다 인스턴스가 늘어 사운드가 이중 재생된다.
**How to avoid:** 생성 경로를 부트스트랩 단일화(씬 배치 금지) + `Awake()`에 `if (Instance != null && Instance != this) { Destroy(gameObject); return; }` 이중 안전장치 (InputManager.cs:11 기존 패턴 동일).
**Warning signs:** 사망 후 재시작 루프를 돌 때마다 사운드 볼륨이 커짐, Hierarchy DontDestroyOnLoad 아래 AudioManager 2개 이상.

### Pitfall 5: 임포트 설정 방치로 모바일 메모리/CPU 낭비
**What goes wrong:** 기본 임포트(Vorbis, 스테레오, Preserve Sample Rate)로 130개 파일을 들여오면 짧은 SFX마다 재생 시 디코딩 CPU를 쓰거나 스테레오로 메모리 2배를 차지한다.
**How to avoid:** `Assets/Audio/` 대상 AssetPostprocessor로 **Force To Mono ON + Decompress On Load + ADPCM + Optimize Sample Rate** 일괄 적용. 짧고(≤1s) 자주 재생되는 SFX에는 ADPCM(압축률 ~3.5:1, 디코딩 비용 극소)이 모바일 표준. Vorbis는 BGM처럼 긴 파일용 — 이번 페이즈에 해당 없음.
**Warning signs:** Profiler Audio 메모리 수 MB 초과, 재생 순간 CPU 스파이크.
**주의:** Editor 스크립트를 팩 복사 **이전에** 커밋해야 최초 임포트부터 적용된다 (이후 추가 시 Reimport 필요).

### Pitfall 6: 사운드 시퀀싱을 scaled time으로 작성
**What goes wrong:** 신규 서브시스템이라 복사할 기존 오디오 코드가 없어, 습관적으로 `WaitForSeconds`/`Time.deltaTime`을 쓰면 슬로우모션 중 트리거 타이밍이 5배 늘어지고 HitFreeze 중엔 영원히 멈춘다.
**How to avoid:** D-00b 절대 준수. 다행히 이번 페이즈의 사운드 3종은 모두 기존 연출 코루틴(이미 전부 unscaled)의 특정 지점에서 1회 호출되는 구조라 AudioManager 자체에는 시간 대기 코드가 거의 없다 — `LateUpdate` pitch 추종은 프레임 단위라 timeScale 무관.
**Warning signs:** 슬로우모션 중 사운드 시작이 눈에 띄게 늦음.

### Pitfall 7: 클립 참조를 Resources.Load로 매 재생마다 로드
**What goes wrong:** `PlaySfx` 내부에서 `Resources.Load<AudioClip>(name)` 호출 시 첫 재생 프레임에 IO 히치 + 문자열 키 오타 런타임 버그.
**How to avoid:** 클립은 AudioManager 프리팹의 SerializeField 4개로 사전 연결, enum 키로 접근. Resources.Load는 부트스트랩의 프리팹 1회 로드만 허용.

### Pitfall 8: 슬래시→사망 노이즈 간격이 의도보다 김 (SFX-06 연계)
**What goes wrong:** D-05의 "시간차 레이어"는 자연 발생하지만(HitFreeze 75ms + Die 애니메이션 완주 후 파티클), 총 간격이 0.8s를 넘으면 두 소리가 인과관계로 안 들리고 무관한 소리처럼 분리된다.
**How to avoid:** 플레이테스트에서 간격 체감 확인 → 과하면 글리치 노이즈 훅을 Die 애니메이션 완주 대기 이전(사망 확정 직후 + `WaitForSecondsRealtime(고정 지연)`)으로 이동. 지연값은 SerializeField로 노출해 튜닝 가능하게.
**Warning signs:** "적을 벤 소리"와 "적이 깨지는 소리"가 별개 사건처럼 들림.

## Code Examples

Verified patterns — 기존 코드베이스 관례(InputManager 싱글턴 가드, GameBootstrapper 어트리뷰트) + Unity 공식 API 기반.

### AudioManager 핵심 골격 (SFX-01)

```csharp
// Assets/Scripts/Audio/AudioManager.cs
using UnityEngine;

public enum Sfx
{
    PortalEnter,      // D-06: 상승 워프음
    PortalExit,       // D-06: 하강 마무리음
    Slash,            // D-05: 대시 처치 슬래시
    EnemyDeathGlitch, // D-05: 사망 글리치 노이즈
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Channel Pools (D-07)")]
    [SerializeField] private int _actionVoices = 8;
    [SerializeField] private int _ambientVoices = 2;
    [SerializeField, Range(0.05f, 1f)] private float _minAmbientPitch = 0.3f; // Pitfall 1

    [Header("Clips — 플레이테스트 중 Inspector에서 교체 (D-03)")]
    [SerializeField] private AudioClip _portalEnter;
    [SerializeField] private AudioClip _portalExit;
    [SerializeField] private AudioClip _slash;
    [SerializeField] private AudioClip _enemyDeathGlitch;

    private AudioSource[] _actionPool;
    private AudioSource[] _ambientPool;
    private int _actionCursor;
    private AudioClip _lastClip;
    private double _lastClipDspTime; // double 필수 — dspTime은 누적 증가

    // GameBootstrapper와 동일 패턴 — 씬 수정 없이 3개 씬 전부에서 생존 보장
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab == null) { Debug.LogError("[Audio] Resources/AudioManager.prefab 없음"); return; }
        DontDestroyOnLoad(Instantiate(prefab.gameObject));
    }

    private void Awake()
    {
        // InputManager.cs:11 기존 중복 가드 패턴 (Pitfall 4)
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _actionPool  = CreatePool("Action", _actionVoices);
        _ambientPool = CreatePool("Ambient", _ambientVoices);
    }

    private AudioSource[] CreatePool(string label, int size)
    {
        var pool = new AudioSource[size];
        for (int i = 0; i < size; i++)
        {
            var child = new GameObject($"{label}Voice{i}");
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.spatialBlend = 0f; // 2D — AudioListener 위치 무관
            pool[i] = src;
        }
        return pool;
    }

    private void LateUpdate()
    {
        // D-07 배경 채널: timeScale 추종 피치다운. 액션 채널은 절대 건드리지 않음.
        // AudioSource DSP 재생은 timeScale 독립이므로 액션 채널 코드는 0줄 (검증됨).
        float pitch = Mathf.Max(_minAmbientPitch, Time.timeScale);
        for (int i = 0; i < _ambientPool.Length; i++)
            _ambientPool[i].pitch = pitch;
    }

    /// <summary>모든 연출 훅의 단일 진입점. null 안전 — 부트스트랩 실패 시 조용히 무시.</summary>
    public static void PlaySfx(Sfx id, float volume = 1f) => Instance?.PlayInternal(id, volume);

    private void PlayInternal(Sfx id, float volume)
    {
        AudioClip clip = id switch
        {
            Sfx.PortalEnter      => _portalEnter,
            Sfx.PortalExit       => _portalExit,
            Sfx.Slash            => _slash,
            Sfx.EnemyDeathGlitch => _enemyDeathGlitch,
            _ => null,
        };
        if (clip == null) return;

        // Pitfall 3: 동일 클립 30ms 내 재트리거 스킵 (연속 처치 위상 중첩 방지)
        if (clip == _lastClip && AudioSettings.dspTime - _lastClipDspTime < 0.03) return;
        _lastClip = clip;
        _lastClipDspTime = AudioSettings.dspTime;

        var src = _actionPool[_actionCursor];
        _actionCursor = (_actionCursor + 1) % _actionPool.Length;
        src.pitch = Random.Range(0.95f, 1.05f); // 반복 재생 기계음 방지 — timeScale 연동 아님
        src.PlayOneShot(clip, volume);
    }
}
```

### Hook 1: 히트 임팩트 (SFX-03) — CombatController.ExecuteDash() step 6

```csharp
// CombatController.cs — ExecuteDash() 처치 분기 (기존 line ~307 인근). 추가 1줄.
target.OnDashHit();
AudioManager.PlaySfx(Sfx.Slash);          // ← HitFreeze 이전 호출 — DSP는 timeScale=0 중에도 계속 재생
SpawnHitSpark(destination);
_cameraFollow?.Shake(_cameraShakeDuration, _cameraShakeAmplitude);
ScoreManager.AddKillScore();
yield return StartCoroutine(HitFreeze(hitFreezeDuration));
```

### Hook 2: 포탈 전환 (SFX-02) — FloorTransitionEffect

```csharp
// FloorTransitionEffect.cs — PlayEntry() 첫 줄 (마스크 성장 시작과 동시에 상승음)
public IEnumerator PlayEntry(Transform portal)
{
    AudioManager.PlaySfx(Sfx.PortalEnter);   // D-06 진입 = 상승 워프음
    ...
}

// PlayExit() — 포탈 이펙트 성장 시작 시점 (플레이어 등장 리듬과 동기)
public IEnumerator PlayExit(Vector3 spawnWorldPos, GameObject portalEffectPrefab)
{
    AudioManager.PlaySfx(Sfx.PortalExit);    // D-06 퇴장 = 하강 마무리음
    ...
}
```

### Hook 3: 적 사망 (SFX-04) — EnemyDeathEffect.PlayDeathSequence()

```csharp
// EnemyDeathEffect.cs — step 2 파티클 재생 직전. Die 애니메이션 완주 + HitFreeze 지연이
// D-05의 슬래시→노이즈 시간차를 자연 형성한다. 간격 과다 시 Pitfall 8 참조.
// 2. 파티클 재생
AudioManager.PlaySfx(Sfx.EnemyDeathGlitch);  // D-05 글리치/디지털 노이즈
SpawnDeathParticles();
```

### 임포트 설정 자동화 (Pitfall 5)

```csharp
// Assets/Editor/AudioImportSettings.cs — 팩 복사 이전에 먼저 커밋할 것
using UnityEditor;
using UnityEngine;

public class AudioImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Audio/")) return;

        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;               // 2D 게임 SFX — 메모리 50% 절감

        var settings = importer.defaultSampleSettings;
        settings.loadType          = AudioClipLoadType.DecompressOnLoad; // 짧은 SFX — 재생 시 디코딩 CPU 0
        settings.compressionFormat = AudioCompressionFormat.ADPCM;       // 임팩트성 SFX 모바일 표준 (~3.5:1)
        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        importer.defaultSampleSettings = settings;
    }
}
```

### 프로젝트 설정 변경 (Pitfall 2)

```
Edit → Project Settings → Audio → DSP Buffer Size: Good latency
(ProjectSettings/AudioManager.asset의 m_RequestedDSPBufferSize가 512로 기록됨 — 현재 m_DSPBufferSize 1024)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| 오브젝트마다 AudioSource 부착 + `GetComponent` 재생 | 중앙 매니저 + 풀링된 보이스 | 모바일 표준 관행 (수년 전 정착) | 보이스 수 상한 통제, 정책(2채널) 일원화 |
| 슬로우모션 시 전 AudioSource pitch 수동 일괄 조정 | 채널 분리 — 필요한 채널만 pitch 추종 | 이 프로젝트의 D-07 설계 (사용자 제안) | 액션 사운드 손맛 보존 + 배경만 시간감 연출 |
| FMOD/Wwise 미들웨어 조기 도입 | 프로토타입은 내장 오디오, 미들웨어는 콘텐츠 규모 확장 시 | 커뮤니티 합의 (변화 없음) | D-00d와 일치 — 이번 마일스톤 해당 없음 |

**Deprecated/outdated:**
- 해당 없음 — Unity 6의 내장 오디오 API(AudioSource/PlayOneShot/AudioSettings.dspTime)는 레거시 대비 변경 없이 안정적. Unity 6에서 오디오 관련 신규 요구 API 없음.

## Open Questions

1. **Kenney 팩 내 개별 파일명/길이가 D-05/D-06 요구 특성과 정확히 맞는가**
   - What we know: 팩 구성(Sci-Fi 70개, Digital 60개, CC0)은 공식 페이지로 확인. 파일명 패턴은 학습 데이터 기반 추정.
   - What's unclear: 상승/하강 톤과 슬래시 후보의 정확한 파일명·재생 길이.
   - Recommendation: 다운로드 직후 파일 목록을 확인하고 후보 3개씩 선별 → 플레이테스트 교체 루프 (D-03이 이 불확실성을 설계적으로 흡수함). 계획 태스크에 "후보 선별" 단계를 명시.

2. **ZIP 다운로드의 스크립트 자동화 가능 여부**
   - What we know: kenney.nl 다운로드 버튼은 정적 zip URL로 연결되나 경로에 해시가 포함되어 사전 확정 불가. Windows 11에 curl 기본 탑재.
   - Recommendation: 계획에서 curl 다운로드를 1차 시도하고, 실패 시 사용자에게 수동 다운로드(브라우저 2클릭)를 요청하는 체크포인트로 설계.

3. **배경 채널 최저 피치 하한값 (Claude 재량)**
   - What we know: 이번 페이즈는 소비 클립이 없어 청각 검증 불가 (D-08).
   - Recommendation: SerializeField 기본값 0.3으로 두고 미래 마일스톤에서 실클립으로 튜닝 — 지금 커브/하한을 정교화하는 것은 오버엔지니어링.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity Editor | 전체 구현/검증 | ✓ | 6000.3.11f1 | — |
| `com.unity.modules.audio` | AudioSource/AudioClip | ✓ | 1.0.0 (manifest 확인) | — |
| 인터넷 (kenney.nl 다운로드) | D-01 에셋 조달 | ✓ (개발 머신) | — | 사용자 수동 다운로드 (브라우저) |
| curl | zip 스크립트 다운로드 시도 | ✓ (Windows 11 기본 탑재) | — | 사용자 수동 다운로드 |

**Missing dependencies with no fallback:** 없음.
**Missing dependencies with fallback:** kenney.nl zip URL 해시 문제로 스크립트 다운로드 실패 가능 → 수동 다운로드 폴백 (Open Question 2).

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.6.0 (패키지 설치됨 — 단, 테스트 어셈블리 0개, asmdef 0개) |
| Config file | none — 프로젝트 전체가 단일 Assembly-CSharp, 테스트 인프라 부재 |
| Quick run command | 없음 (아래 참조 — 본 페이즈는 수동 플레이테스트 검증) |
| Full suite command | 없음 |

**판단:** 이 페이즈의 요구사항은 전부 "사운드가 들리는가 / 타이밍이 자연스러운가"라는 인간 청각 판정이다. 오디오 출력의 자동 검증은 Unity에서 실질적으로 불가능하며(파형 캡처 테스트는 프로토타입에 과잉), 기존 12개 페이즈도 전부 에디터 플레이테스트로 검증해 온 프로젝트다. 테스트 어셈블리 신설은 CLAUDE.md 단순성 원칙 위반으로 판단 — **manual-only 정당화**. 자동화 가능한 유일한 게이트는 "컴파일 에러/콘솔 에러 0" (Unity 에디터 열림 상태에서 확인).

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SFX-01 | AudioManager가 3씬 전환 간 생존, PlaySfx 재생 | manual-only | MainMenu→AttackSelect→SampleScene→사망→재시작 순회, Hierarchy DontDestroyOnLoad 아래 인스턴스 1개 확인 | — |
| SFX-02 | 포탈 진입 상승음/퇴장 하강음 재생 | manual-only | EXIT 포탈 진입 플레이테스트 | — |
| SFX-03 | 대시 처치 순간 슬래시음, HitFreeze 중 피치 불변 | manual-only | 슬로우모션 유지 상태에서 처치 → 음 왜곡 여부 청취 | — |
| SFX-04 | 사망 시 글리치 노이즈, 슬래시와 시간차 레이어 | manual-only | 처치 플레이테스트 — 슬래시→노이즈 인과 체감 | — |
| SFX-06 | 타이밍·피드백 어색함 개선 | manual-only | Architecture Patterns §4 후보 목록 순서대로 점검 | — |

### Sampling Rate
- **Per task commit:** Unity 에디터 콘솔 에러 0 + 해당 훅 1회 청취 확인
- **Per wave merge:** 3씬 전체 순회 + 사망 재시작 루프 1회 (Pitfall 4 중복 인스턴스 확인 포함)
- **Phase gate:** 성공 기준 5개 전항목 플레이테스트 체크리스트 통과 후 `/gsd:verify-work`

### Wave 0 Gaps
None — 테스트 인프라 신설 없이 기존 프로젝트 검증 방식(에디터 플레이테스트) 유지. 자동화 테스트 부재는 위 manual-only 정당화 참조.

## Sources

### Primary (HIGH confidence)
- 직접 코드 리딩: `Assets/Scripts/Player/CombatController.cs` (ExecuteDash/HitFreeze/슬로우모션 수명주기), `Assets/Scripts/World/FloorTransitionEffect.cs` (PlayEntry/PlayExit duration), `Assets/Scripts/Enemy/EnemyDeathEffect.cs` (Die 애니메이션 대기 → 파티클 순서), `Assets/Scripts/World/GameBootstrapper.cs` (RuntimeInitializeOnLoadMethod 선례), `Assets/Scripts/Player/InputManager.cs` (싱글턴 가드 선례), `Assets/Scripts/Camera/CameraFollow.cs` (쉐이크 unscaled 확인), `ProjectSettings/AudioManager.asset` (DSP 1024 확인), `Packages/manifest.json` (audio 모듈 설치 확인)
- [kenney.nl/assets/sci-fi-sounds](https://kenney.nl/assets/sci-fi-sounds) — 70개 파일, CC0 확인
- [kenney.nl/assets/digital-audio](https://kenney.nl/assets/digital-audio) — 60개 파일, CC0 확인
- `.planning/research/SUMMARY.md` — 마일스톤 리서치 (풀링 근거, Pitfall 6/9 승계)

### Secondary (MEDIUM confidence)
- AudioSource는 Time.timeScale 비영향 + 슬로우모션 연출은 pitch 수동 조정: [Unity Discussions — How to fix the audio when using Time.timeScale](https://discussions.unity.com/t/how-to-fix-the-audio-when-using-time-timescale/843414), [Unity Discussions — TimeScale with AudioSource](https://discussions.unity.com/t/timescale-with-audiosource/205329), [AudioSource.pitch 공식 문서](https://docs.unity3d.com/ScriptReference/AudioSource-pitch.html) — 다수 독립 소스 합치 + 마일스톤 리서치와 교차 확인
- 모바일 임포트 설정 (ADPCM/Decompress On Load/Force To Mono): [Unity Manual — Audio Clip Import Settings](https://docs.unity3d.com/Manual/class-AudioClip.html), [Game Developer — Unity Audio Import Optimisation](https://www.gamedeveloper.com/audio/unity-audio-import-optimisation---getting-more-bam-for-your-ram), [Made Indrayana — Load Type 선택](https://medium.com/double-shot-audio/choosing-the-right-load-type-in-unitys-audio-import-settings-1880a61134c7)
- DSP Buffer Size 지연/안정성 트레이드오프: [Unity Support — Android sound latency](https://support.unity.com/hc/en-us/articles/206116316), [Unity Issue Tracker — Best Latency 에디터 스터터](https://issuetracker.unity3d.com/issues/when-dsp-buffer-size-is-set-to-best-latency-it-makes-audio-in-the-editor-stutter)

### Tertiary (LOW confidence)
- Kenney 팩 내 개별 파일명 패턴(`*Up*`/`*Down*`/`laser*`/`spaceTrash*` 등) — 학습 데이터 기반, 다운로드 후 확정 필요 (Open Question 1)
- `AudioSource.PlayDelayed`의 timeScale 독립성 — 공식 문서에 명시 없음. 본 리서치는 이를 사용하지 않는 방향(이벤트 훅 직접 호출 + WaitForSecondsRealtime)으로 권고하여 의존 제거

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — 신규 의존성 0, 내장 API만 사용, 팩 존재/라이선스는 공식 페이지 확인
- Architecture: HIGH — 전 패턴이 이 저장소의 기존 검증된 선례(GameBootstrapper/InputManager) 직접 재사용, 훅 지점 3곳 모두 소스 라인 단위로 확인
- Pitfalls: HIGH (코드베이스 유래 — timeScale/HitFreeze 상호작용은 소스 직접 확인) / MEDIUM (일반 Unity 오디오 관행 — 복수 커뮤니티+공식 문서 교차 확인, 단 timeScale-오디오 독립성을 명시한 단일 공식 문서는 부재)
- 클립 선별: LOW — 파일명 수준은 다운로드 후 확정 (D-03 플레이테스트 루프가 리스크 흡수)

**Research date:** 2026-07-09
**Valid until:** 2026-08-09 (안정 영역 — Unity 내장 오디오 API·Kenney CC0 팩 모두 저변동)
