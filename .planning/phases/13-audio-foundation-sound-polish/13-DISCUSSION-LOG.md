# Phase 13: 오디오 기반 구축 & 연출 사운드 폴리싱 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-08
**Phase:** 13-audio-foundation-sound-polish
**Areas discussed:** 사운드 에셋 조달 (완료) / 사운드 스타일·톤, 슬로우모션 중 오디오 처리 (미논의 — 다음 세션 이어서)
**Session status:** ⏸ 사용자 요청으로 중간 저장 — 논의 미완료

---

## 사전 조치: ROADMAP.md 파서 충돌 수정

discuss-phase 시작 시 GSD 도구가 Phase 13을 인식하지 못함. 원인: v3.1 섹션 내부 헤딩들(`# Roadmap: Fast (가칭) — v3.1`, `## v3.1 Phases` 등)이 "vN.N 포함 H1/H2 = 다음 마일스톤 시작" 파서 규칙에 걸려 섹션이 시작 직후 잘림. 사용자 승인 후 헤딩에서 v3.1 접두어 제거 + 중복 H1 삭제 (커밋 cf5f37f). 내용 변경 없음.

---

## 논의 영역 선택

| Option | Description | Selected |
|--------|-------------|----------|
| 사운드 에셋 조달 | 오디오 파일 0개 — CC0 다운로드 vs 직접 제공 vs 플레이스홀더 | ✓ |
| 사운드 스타일/톤 | 레트로 8비트 vs 현실적 vs SF/디지털 | ✓ (미논의) |
| 슬로우모션 중 오디오 처리 | 정상 피치 유지 vs 피치 다운 연출 | ✓ (미논의) |
| SFX-06 어색함 구체화 | 어떤 타이밍이 어색한지 구체화 | — (미선택 → Claude 재량) |

---

## 사운드 에셋 조달

### Q1. 사운드 조달 방식

| Option | Description | Selected |
|--------|-------------|----------|
| 무료 CC0 팩 다운로드 (권장) | Kenney 등 CC0 팩 임포트 — 라이선스 걱정 없음, 즉시 진행 가능 | ✓ |
| 직접 제공 | 사용자가 파일 준비 | |
| 임시 플레이스홀더 | 코드 생성 톤으로 배선만 — 손맛 검증 불가 | |

### Q2. 임포트 범위

| Option | Description | Selected |
|--------|-------------|----------|
| 필요한 것만 선별 (권장) | 4~6개 파일만 임포트 — 모바일 용량 유리 | |
| 팩 통째로 임포트 | 향후 페이즈(보스 사운드 등)에서 재활용 — 재다운로드 불필요 | ✓ |

### Q3. 사운드 최종 선별 주체

| Option | Description | Selected |
|--------|-------------|----------|
| Claude 재량 + 플레이테스트 검수 (권장) | Claude가 골라 배선, 어색하면 교체 | ✓ |
| 사용자 사전 승인 | 후보를 먼저 확인 후 배선 | |

**User's choice:** CC0 팩 통째 임포트, Claude 재량 선별 + 플레이테스트 검수
**Notes:** 진행이 막히지 않는 방향 선호. 팩 통째 임포트는 이후 페이즈 활용 목적.

---

## Claude's Discretion

- SFX-06 타이밍·피드백 어색함 개선의 구체 대상 (사용자가 논의 영역으로 미선택)
- 오디오 파일 포맷/모바일 임포트 설정

## Deferred Ideas

없음

## ⏸ 다음 세션에서 이어서

1. **사운드 스타일/톤** — CC0 팩 선택 기준이 되므로 먼저 논의 권장
2. **슬로우모션 중 오디오 처리** — 히트 임팩트 피치 정책

재개 방법: `/gsd:discuss-phase 13` → "Update it" 선택 → CONTEXT.md의 Remaining Discussion부터 진행
