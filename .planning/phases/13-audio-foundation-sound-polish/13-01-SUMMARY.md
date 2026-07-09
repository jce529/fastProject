---
phase: 13-audio-foundation-sound-polish
plan: 01
subsystem: audio
tags: [unity, audiosource, singleton, assetpostprocessor, adpcm, dsp-buffer, mobile]

# Dependency graph
requires:
  - phase: 12-animation-polish
    provides: 연출 훅 3곳 (FloorTransitionEffect, CombatController.ExecuteDash, EnemyDeathEffect) — Plan 13-03에서 사운드 배선 예정
provides:
  - AudioManager 싱글턴 (MonoBehaviour + RuntimeInitializeOnLoadMethod 부트스트랩, 3씬 생존)
  - Sfx enum 4종 (PortalEnter/PortalExit/Slash/EnemyDeathGlitch) + static PlaySfx(Sfx, volume) API
  - 2채널 AudioSource 풀 — 액션 8보이스(timeScale 독립) + 배경 2보이스(pitch = timeScale 추종 인프라)
  - Assets/Audio/ 대상 AssetPostprocessor (Force To Mono + ADPCM + Decompress On Load + Optimize Sample Rate)
  - DSP Buffer Size 512 (Good latency)
affects: [13-02 (audio pack import + AudioManager.prefab), 13-03 (sound hook wiring), 16 (boss spawn sound — Sfx enum 확장)]

# Tech tracking
tech-stack:
  added: []  # 신규 패키지 0 — com.unity.modules.audio 내장 모듈만 사용
  patterns:
    - "AudioManager.PlaySfx(Sfx.X) enum 키 static API — 호출부는 AudioClip 참조 없음"
    - "RuntimeInitializeOnLoadMethod + Resources 프리팹 부트스트랩 (GameBootstrapper 선례 재사용)"
    - "2채널 풀: 액션 채널 pitch 불변(랜덤 미세변주만), 배경 채널만 timeScale 추종"

key-files:
  created:
    - Assets/Editor/AudioImportSettings.cs
    - Assets/Scripts/Audio/AudioManager.cs
  modified:
    - ProjectSettings/AudioManager.asset

key-decisions:
  - "DSP Buffer 512 (Good latency) 선택 — 1024는 히트 사운드 체감 지연, 256은 에디터 스터터/저사양 Android 크래클 보고 (Pitfall 2)"
  - "동일 클립 30ms 내 재트리거는 AudioSettings.dspTime 기준 스킵 — 연속 처치 위상 중첩 클리핑 방지 (Pitfall 3)"
  - "배경 채널 minAmbientPitch 0.3 클램프 — HitFreeze(timeScale=0) 시 pitch=0 재생 정지 방지 (Pitfall 1)"

patterns-established:
  - "Sfx enum 확장 패턴: Phase 16 보스 스폰 사운드는 enum 값 + SerializeField + switch 분기 추가만으로 확장"
  - "Assets/Audio/ 폴더 규칙: 이 경로 하위 모든 오디오는 모바일 임포트 설정 자동 적용"

requirements-completed: [SFX-01]

# Metrics
duration: 3min
completed: 2026-07-09
---

# Phase 13 Plan 01: 오디오 기반 인프라 Summary

**AudioManager 싱글턴(enum API + 액션 8/배경 2보이스 2채널 풀 + Resources 부트스트랩), Assets/Audio/ 모바일 임포트 자동화 AssetPostprocessor, DSP 버퍼 512 설정 — 신규 패키지 0개로 SFX-01 코드 기반 완성**

## Performance

- **Duration:** 3 min
- **Started:** 2026-07-09T08:40:07Z
- **Completed:** 2026-07-09T08:42:59Z
- **Tasks:** 3
- **Files modified:** 5 (스크립트 2 + meta 3) + ProjectSettings 1

## Accomplishments

- `AudioManager.PlaySfx(Sfx.Slash)` 형태의 static 호출이 어느 스크립트에서든 가능한 싱글턴 인프라 완성 — Plan 13-02(클립 연결)와 13-03(훅 배선)이 병렬 진행 가능
- 오디오 팩 임포트 전 필수 선행물(AudioImportSettings AssetPostprocessor)이 팩 복사 이전에 커밋됨 — 최초 임포트부터 Force To Mono + ADPCM + Decompress On Load 자동 적용 (RESEARCH.md Pitfall 5 대응)
- DSP Buffer 1024 → 512로 "대시 도착 순간 슬래시" 코어 손맛의 히트 사운드 지연 제거 준비 완료

## Task Commits

Each task was committed atomically:

1. **Task 1: AudioImportSettings.cs — Assets/Audio/ 모바일 임포트 설정 자동화** - `578aae4` (feat)
2. **Task 2: AudioManager.cs — 싱글턴 + 2채널 풀 + enum API + 부트스트랩** - `02e8247` (feat)
3. **Task 3: DSP Buffer Size 512 설정** - `06f4251` (chore)

## Files Created/Modified

- `Assets/Editor/AudioImportSettings.cs` - Assets/Audio/ 하위 오디오 파일에 모바일 최적 임포트 설정(Mono/ADPCM/DecompressOnLoad/OptimizeSampleRate) 일괄 적용하는 AssetPostprocessor
- `Assets/Scripts/Audio/AudioManager.cs` - Sfx enum 4종 + PlaySfx static API + 2채널 AudioSource 풀 + RuntimeInitializeOnLoadMethod 부트스트랩 + 중복 인스턴스 가드 + dspTime 재트리거 가드
- `ProjectSettings/AudioManager.asset` - m_DSPBufferSize 1024→512, m_RequestedDSPBufferSize 0→512 (2라인만 변경, git diff 확인)

## Decisions Made

- 플랜 명세 그대로 실행 — RESEARCH.md 검증 코드를 변경 없이 사용 (아래 주석 1건 제외)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] AudioManager.cs 주석의 "AudioListener" 문자열이 수용 기준과 충돌**
- **Found during:** Task 2 (AudioManager.cs 생성)
- **Issue:** 플랜의 action 코드 블록에 포함된 주석 `// 2D — AudioListener 위치 무관`이 같은 태스크의 수용 기준("파일 어디에도 AudioListener 문자열이 없다")과 모순 — 플랜 내부 불일치
- **Fix:** 주석을 `// 2D — 리스너 위치 무관`으로 변경 (의미 동일, 코드 변경 없음)
- **Files modified:** Assets/Scripts/Audio/AudioManager.cs
- **Verification:** `grep "WaitForSeconds|Time.deltaTime|AudioListener|AudioMixer"` 무매치 확인
- **Committed in:** 02e8247 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 plan-internal inconsistency, comment-only)
**Impact on plan:** 코드 동작 변화 없음. 스코프 확대 없음.

## Issues Encountered

None — 참고: 이 repo는 .meta 파일을 커밋하는 컨벤션이므로 신규 에셋 3개(스크립트 2 + Audio 폴더)에 대해 기존 미니멀 meta 포맷(fileFormatVersion + guid)으로 meta 파일을 함께 생성/커밋함. Unity 에디터가 열리면 동일 GUID로 인식됨.

## Known Stubs

- `Assets/Scripts/Audio/AudioManager.cs` — 클립 SerializeField 4개(`_portalEnter` 등)는 현재 null이며 `PlayInternal`은 null 클립을 조용히 스킵. **의도된 스텁**: Plan 13-02가 `Resources/AudioManager.prefab`을 생성하고 CC0 팩 클립을 연결한다 (플랜 frontmatter key_links에 명시).
- `Bootstrap()`의 `Resources.Load<AudioManager>("AudioManager")`는 프리팹이 아직 없어 현재는 LogError 후 리턴. **의도된 스텁**: Plan 13-02가 프리팹 생성 시 해소. PlaySfx는 null 안전(`Instance?.`)이라 그 사이에도 컴파일/실행 안전.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 13-02 (오디오 팩 임포트 + AudioManager.prefab 생성) 진행 가능 — AssetPostprocessor 선행 조건 충족
- Plan 13-03 (연출 훅 3곳 사운드 배선) 병렬 진행 가능 — PlaySfx API 확정
- 컴파일 확인(콘솔 에러 0)과 DSP 설정 유지 확인은 13-04 체크포인트에서 수행 (플랜 명시 — Unity 에디터 필요)
- 주의: Unity 에디터가 열려 있었다면 종료 시 ProjectSettings/AudioManager.asset을 덮어쓸 수 있음 (13-04 체크포인트 확인 항목)

---
*Phase: 13-audio-foundation-sound-polish*
*Completed: 2026-07-09*
