---
phase: 9
slug: infinite-gen-cleanup
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-07-01
---

# Phase 9 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.6.0 (NUnit-based) |
| **Config file** | 없음 (별도 설정 파일 없음) |
| **Quick run command** | Unity Test Runner → Edit Mode Tests 실행 |
| **Full suite command** | Unity Test Runner → All Tests (Edit + Play Mode) |
| **Estimated runtime** | ~30 seconds (Edit Mode) + 수동 플레이테스트 |

---

## Sampling Rate

- **After every task commit:** Unity Console 오류 0개 확인 (컴파일)
- **After every plan wave:** Unity Play Mode에서 씬 실행, Hierarchy에서 체인 GO 수 확인
- **Before `/gsd:verify-work`:** Success Criteria 3항목 전부 충족

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 9-W0-01 | 01 | 0 | GEN-01/02/03 | Edit Mode | RoomMarkerTool 실행 후 프리팹 저장 | ❌ W0 | ⬜ pending |
| 9-01-01 | 01 | 1 | GEN-01/02/03 | Edit Mode Unit | SelectCorridor() 경계 조건 | ❌ W0 | ⬜ pending |
| 9-01-02 | 01 | 1 | GEN-01/02 | Play Mode (수동) | Hierarchy에서 체인 GO 수 확인 | N/A | ⬜ pending |
| 9-02-01 | 02 | 2 | GEN-01/02 | Play Mode (수동) | 플레이어 이동 시 스폰/Destroy 확인 | N/A | ⬜ pending |
| 9-03-01 | 03 | 3 | GEN-03 | Play Mode (수동) | 5회 반복 시 Corridor 3종 모두 출현 | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Editor/RoomMarkerTool.cs` 업데이트 — 전체 14개 룸의 Door/ENT(Left) + Door/EXIT(Right) 멱등 추가
- [ ] 업데이트된 RoomMarkerTool 에디터 실행 → 모든 Room 프리팹 저장 확인
- [ ] `Assets/Tests/EditMode/WorldGeneratorTests.cs` — GEN-03 SelectCorridor() 경계 조건 (optional)

*기존 테스트 인프라(com.unity.test-framework 1.6.0)는 이미 설치됨 — 추가 설치 불필요.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 앞 2개 Room+Corridor 자동 생성 | GEN-01 | Play Mode에서 GameObject 생성 확인이 씬 셋업 비용 높음 | Play 후 Hierarchy에서 chain GO가 시작 3개 + 스폰 2쌍 = 5개 이상인지 확인 |
| 뒤 2개 초과 Room+Corridor Destroy | GEN-02 | 플레이어 실제 이동 필요 | 플레이어를 오른쪽으로 이동해 뒤쪽 룸이 Hierarchy에서 사라지는지 확인 |
| 5회 Play 반복 시 Corridor 랜덤 분포 | GEN-03 | 확률적 동작 — 1회 확인으로 불충분 | 5회 Play 시 Corridor_Up/Flat/Down 최소 각 1회 이상 출현하는지 Scene View에서 체인 형태 확인 |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
