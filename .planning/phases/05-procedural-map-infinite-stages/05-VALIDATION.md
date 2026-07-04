---
phase: 5
slug: procedural-map-infinite-stages
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-17
---

# Phase 5 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (NUnit-based, PlayMode) |
| **Config file** | `Assets/Tests/PlayMode/` (Wave 0 may need to create PlayMode.asmdef) |
| **Quick run command** | Unity Editor → Window > General > Test Runner → Run All (PlayMode) |
| **Full suite command** | Unity Editor → Test Runner → Run All |
| **Estimated runtime** | ~30–60 seconds |

---

## Sampling Rate

- **After every task commit:** Manual playtest in Unity Editor (Enter Play mode, trigger floor transition)
- **After every plan wave:** Full manual walkthrough (spawn → exit → floor advance → death → restart)
- **Before `/gsd:verify-work`:** All manual verification steps must be confirmed
- **Max feedback latency:** ~60 seconds (enter play mode and reach floor exit)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Verification | Status |
|---------|------|------|-------------|-----------|--------------|--------|
| 05-01-T1 | 01 | 1 | FLOOR-01 | Manual | FloorSpawner 씬에서 Awake 시 Room_Combat 인스턴스 스폰 확인 | ⬜ pending |
| 05-01-T2 | 01 | 1 | FLOOR-02 | Manual | 출구 트리거 밟으면 6단계 시퀀스 발동 — 입력 잠금 후 순간이동 확인 | ⬜ pending |
| 05-01-T3 | 01 | 1 | FLOOR-03 | Manual | 새 층 스폰 시 적들이 비활성 상태, 카메라 스냅 후 SetActive(true) 확인 | ⬜ pending |
| 05-01-T4 | 01 | 1 | FLOOR-04 | Manual | 이전 층 GameObject가 전환 직후 씬 히어라키에서 사라짐 확인 | ⬜ pending |
| 05-02-T1 | 02 | 2 | FLOOR-01 | Manual | Unity Editor에서 Room 프리팹 4개 제작 — 플랫폼/트리거/스폰포인트 포함 | ⬜ pending |
| 05-02-T2 | 02 | 2 | FLOOR-01 | Manual | 2층 이상부터 가중치 랜덤으로 Room 선택 — 여러 번 실행 시 다양한 Room 등장 | ⬜ pending |
| 05-02-T3 | 02 | 2 | FLOOR-03 | Manual | D-07 난이도 스케일 확인 — 1~5층: 근접 위주, 11층+: 원거리 비율 확대 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `PlayerController`에 `_inputLocked` bool + `LockInput()` / `UnlockInput()` 메서드 추가
- [ ] `CombatController.Update()` 상단에 `if (_player.InputLocked) return;` 가드 추가
- [ ] `FloorSpawner.cs` 신규 MonoBehaviour 파일 생성
- [ ] `RoomExit.cs` 신규 트리거 감지 컴포넌트 파일 생성

*Note: Unity Test Framework PlayMode tests are optional for this phase — all critical behaviors verified via manual Editor playtest.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 층 전환 시퀀스 6단계 체감 | FLOOR-02 | 입력 잠금/재개 타이밍은 육안 확인 필요 | Play mode → 출구 트리거 밟기 → 입력 불가 → 순간이동 → 카메라 스냅 → 적 활성화 → 조작 재개 순서 확인 |
| Room 프리팹 레이아웃 품질 | FLOOR-01 | 플랫폼 배치 및 낙사 구역은 플레이어 경험 기반 판단 | 각 Room 수동 탐색 — 점프로 도달 가능한 플랫폼, 출구 접근성 확인 |
| 메모리 관리 시각 확인 | FLOOR-04 | 씬 히어라키에서 오브젝트 수 직접 확인 | 층 전환 후 Hierarchy 창에서 Room 오브젝트 2개(현재+다음)만 존재 확인 |
| 재시작 후 1층 리셋 | FLOOR-02 | SceneManager.LoadScene(0) 씬 리로드 후 상태 확인 | 죽음 → 재시작 버튼 → Floor 1 표시 + 최초 Room 스폰 확인 |

---

## Validation Architecture (from RESEARCH.md)

### Critical Integration Points
1. **`PlayerController._inputLocked`** — 전환 시퀀스 1단계/6단계. `CombatController`도 이 플래그를 읽어야 함.
2. **`GetComponentsInChildren<IEnemy>(true)`** — `includeInactive: true` 필수. 없으면 비활성 적 반환 0.
3. **카메라 Y스냅은 플레이어 순간이동으로 자동 완성** — `CameraFollow.LateUpdate`가 매 프레임 `target.position + offset`으로 갱신.
4. **`WaitForSecondsRealtime`** 사용 — `Time.timeScale`이 0이 될 수 있으므로 `WaitForSeconds` 금지.

### Anti-Patterns to Catch
- `GetComponentsInChildren<IEnemy>()` without `true` → 비활성 적 누락
- `WaitForSeconds` in transition coroutine → timeScale=0 시 무한 대기
- `FloorManager`에 MonoBehaviour 추가 → static class 패턴 위반
- 적 Instantiate 직후 즉시 SetActive(true) → FLOOR-03 위반

---

## Validation Sign-Off

- [ ] All tasks have manual verification steps
- [ ] Wave 0 file stubs documented
- [ ] Integration pitfalls documented in Anti-Patterns section
- [ ] `nyquist_compliant: true` set in frontmatter when all steps verified

**Approval:** pending
