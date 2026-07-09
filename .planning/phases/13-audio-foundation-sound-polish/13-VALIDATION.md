---
phase: 13
slug: audio-foundation-sound-polish
status: approved
nyquist_compliant: true
wave_0_complete: true
created: 2026-07-09
approved: 2026-07-09
---

# Phase 13 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | 없음 — Unity Test Framework 1.6.0 설치되어 있으나 테스트 어셈블리/asmdef 0개 (프로젝트 전체 단일 Assembly-CSharp) |
| **Config file** | none — 신설하지 않음 (아래 manual-only 정당화 참조) |
| **Quick run command** | grep 기반 정적 검증 (태스크별 `<automated>` 명령) |
| **Full suite command** | 없음 — 에디터 플레이테스트 체크리스트 (13-04 Task 2) |
| **Estimated runtime** | grep < 1s / 플레이테스트 ~10분 |

**Manual-only 정당화 (13-RESEARCH.md Validation Architecture):** 이 페이즈의 요구사항은 전부 "사운드가 들리는가 / 타이밍이 자연스러운가"라는 인간 청각 판정이다. 오디오 출력의 자동 검증은 Unity에서 실질적으로 불가능하며(파형 캡처 테스트는 프로토타입에 과잉), 기존 12개 페이즈도 전부 에디터 플레이테스트로 검증해 온 프로젝트다. 테스트 어셈블리 신설은 CLAUDE.md 단순성 원칙 위반. 자동화 가능한 게이트는 (a) grep 기반 코드/설정 존재 검증, (b) Unity 에디터 컴파일 에러 0 (13-04 체크포인트).

---

## Sampling Rate

- **After every task commit:** 해당 태스크의 `<automated>` grep 검증 실행 + (에디터 열림 시) 콘솔 에러 0 확인
- **After every plan wave:** Wave 2 완료 시 grep 4건(훅) + 팩 파일 수 재확인. Wave 3에서 3씬 전체 순회 + 사망 재시작 루프 1회 (Pitfall 4 중복 인스턴스 확인 포함)
- **Before `/gsd:verify-work`:** 13-04 플레이테스트 체크리스트(SC1~SC5) 전항목 통과
- **Max feedback latency:** grep < 1s / 플레이테스트는 13-04 단일 게이트로 집중 (검증 피로 방지)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 13-01-01 | 01 | 1 | SFX-01 | static | `grep -c "OnPreprocessAudio" Assets/Editor/AudioImportSettings.cs` | ✅ (grep) | ⬜ pending |
| 13-01-02 | 01 | 1 | SFX-01 | static | `grep -c "public static void PlaySfx" Assets/Scripts/Audio/AudioManager.cs` | ✅ (grep) | ⬜ pending |
| 13-01-03 | 01 | 1 | SFX-01 | static | `grep -c "m_RequestedDSPBufferSize: 512" ProjectSettings/AudioManager.asset` | ✅ (grep) | ⬜ pending |
| 13-02-01 | 02 | 2 | SFX-01 | static | `ls Assets/Audio/Kenney_SciFiSounds \| grep -ci "\.ogg\|\.wav"` (≥50) | ✅ (ls) | ⬜ pending |
| 13-02-02 | 02 | 2 | SFX-02/03/04 | static | 경로 상수 4개 실존 파일 검증 (`ls $(grep -oE 'Assets/Audio/[^"]+' ...)`) | ✅ (ls) | ⬜ pending |
| 13-03-01 | 03 | 2 | SFX-03, SFX-04 | static | `grep -n "PlaySfx(Sfx.Slash)" CombatController.cs && grep -n "PlaySfx(Sfx.EnemyDeathGlitch)" EnemyDeathEffect.cs` | ✅ (grep) | ⬜ pending |
| 13-03-02 | 03 | 2 | SFX-02 | static | `grep -n "PlaySfx(Sfx.PortalEnter)\|PlaySfx(Sfx.PortalExit)" FloorTransitionEffect.cs` | ✅ (grep) | ⬜ pending |
| 13-04-01 | 04 | 3 | SFX-01 | checkpoint | `ls Assets/Resources/AudioManager.prefab` + 사용자 에디터 확인 | ✅ (ls) | ⬜ pending |
| 13-04-02 | 04 | 3 | SFX-01~04, SFX-06 | manual-only | 플레이테스트 체크리스트 SC1~SC4 + A~D 리포트 | — | ⬜ pending |
| 13-04-03 | 04 | 3 | SFX-06 | static+manual | `git diff --stat` 리포트 1:1 대응 + 재청취 확인 | ✅ (git) | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. — 테스트 프레임워크 신설 없음 (manual-only 정당화 참조). Wave 0 태스크 불필요. 단, Wave 1의 AudioImportSettings.cs는 Wave 2 팩 임포트의 필수 선행물이라는 점에서 사실상의 게이트 역할을 한다.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| AudioManager 3씬 생존 + 단일 인스턴스 | SFX-01 | Hierarchy/DontDestroyOnLoad 상태는 에디터 런타임 관찰 필요 | MainMenu→AttackSelect→SampleScene→사망→재시작 순회, DontDestroyOnLoad 아래 인스턴스 1개 확인 |
| 포탈 진입 상승음 / 퇴장 하강음 | SFX-02 | 청각 판정 | EXIT 포탈 진입 플레이테스트 (13-04 Task 2 항목 3-4) |
| 처치 슬래시음 + 슬로우모션 피치 무결 | SFX-03 | 청각 판정 | 슬로우모션 유지 처치 → 음 왜곡 여부 청취 (항목 5-6) |
| 사망 글리치 노이즈 + 시간차 레이어 | SFX-04 | 청각 판정 | 처치 플레이테스트 — 슬래시→노이즈 인과 체감 (항목 7) |
| 타이밍·피드백 어색함 개선 | SFX-06 | 체감 판정 | 13-04 Task 2 항목 A~D 점검 → Task 3 레시피 적용 → 재청취 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies — 전 auto 태스크 grep/ls 검증, 청각 판정만 checkpoint (정당화 문서화)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references — MISSING 참조 없음
- [x] No watch-mode flags
- [x] Feedback latency < 1s (grep) / 플레이테스트는 Wave 3 단일 게이트
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-07-09 (plan-phase 13)
