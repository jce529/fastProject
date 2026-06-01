# Phase 1: Foundation & Movement - Context

**Gathered:** 2026-05-27
**Status:** Ready for planning

<domain>
## Phase Boundary

플레이어 캐릭터가 정적 테스트 플로어 위에서 반응성 있게 이동하고, 낙사 시 마지막 플랫폼으로 복귀한다.
사망 없음. 전투 없음. 이동/점프/낙사복귀만 검증.

**Requirements in scope:** MOVE-01, MOVE-02
**Not in scope:** MOVE-03 (roll — Phase 2), 모든 전투 시스템, 적, HUD

</domain>

<decisions>
## Implementation Decisions

### 이동 무게감
- **D-01:** 가볍고 탄력적인 이동감 — Celeste류 빠르고 정밀한 조작감
- **D-02:** 점프 컷(Jump Cut) 적용 — 버튼을 떼면 상승 속도 즉시 감소. 탭 점프 vs. 롱 점프 차이를 명확하게 만듦
- **D-03:** 공중 방향 전환 완전 자유 — 지상과 동일한 속도로 즉시 방향 전환 가능
- **D-04:** 방향 전환은 1프레임 내 즉각 반응 (momentum slide 없음) — Roadmap SC-1 명시 조건

### 테스트 씬 레이아웃
- **D-05:** 단일 넓은 플랫폼 + 양쪽 낙사구역 — Phase 2~4 전투 테스트 배경으로 사용
- **D-06:** 낙사 감지: 보이지 않는 플로어 트리거(Trigger Collider2D) — 맵 하단에 배치
- **D-07:** 비주얼: 회색 단색 스프라이트 placeholder — 실루엣 스타일 개발 전 임시

### 낙사 복귀 연출
- **D-08:** 낙사 시 별도 이펙트 없음 — 즉시 마지막 발판 위치로 텔레포트
- **D-09:** 무적 시각 표현: 스프라이트 깜빡임 (Coroutine으로 알파 0↔1 반복)
- **D-10:** 무적 지속 시간: 1초 (`Time.unscaledDeltaTime` 사용 필수)

### 카메라
- **D-11:** LateUpdate 직접 구현 — `Camera.main`이 LateUpdate에서 플레이어 위치 추적
- **D-12:** 카메라 주식(lead-ahead) 없음 — 플레이어 위치에 정밀 추적
- **D-13:** Cinemachine 미사용 (Unity 6에서 API 변경 불확실성 — STATE.md 권장사항)

### 그래픽
- **D-14:** Unity 기본 placeholder 스프라이트 사용 — 실루엣 아트 개발 전까지 임시

### Claude's Discretion
- 이동 속도 수치 (권장 범위: 7~10 units/s), 점프 높이, 중력 배율 — 플레이테스트 후 조정
- 깜빡임 주기 (권장: 0.1초 간격)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` §이동(Movement) — MOVE-01, MOVE-02 상세 정의 및 수용 기준

### Roadmap & State
- `.planning/ROADMAP.md` §Phase 1 — 성공 기준(SC 1~4) 및 Stack Constraints 섹션
- `.planning/STATE.md` — 기술 제약사항 목록 (Time.unscaledDeltaTime, Rigidbody2D 설정 등)

### Technical Constraints (from ROADMAP.md Stack Constraints)
- `Rigidbody2D`: Continuous collision detection + Interpolate mode
- 무적 타이머: `Time.unscaledDeltaTime` 사용 필수 (timeScale 영향 없음)
- `Physics2D.OverlapCircleNonAlloc()` — Update 내 LINQ/FindObjectsOfType 금지
- Animator Transition Duration = 0 (모든 액션 상태 전환)

No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Assets/InputSystem_Actions.inputactions` — Move(Vector2), Jump(Button), Attack(Button) 액션 이미 정의됨. Phase 1에서 Move와 Jump만 사용.
- `Assets/Scenes/SampleScene.unity` — 테스트 씬으로 사용. 기존 오브젝트 정리 후 테스트 레이아웃 구성.

### Established Patterns
- Unity Input System 1.19.0 New Input System — `PlayerInput` 컴포넌트 또는 `InputAction` 직접 사용
- URP 2D Renderer 설정 완료 — 별도 렌더 설정 불필요
- C# 9.0, .NET Standard 2.1 — 최신 C# 문법 사용 가능 (pattern matching, nullable, records 등)

### Integration Points
- `Assets/Scripts/` 폴더 없음 — 새로 생성 필요
- Layer Matrix 설정 필요 (코딩 시작 전): PlayerHurtbox, PlayerInvincible, Platform (Phase 1에서 필요한 것만)

</code_context>

<specifics>
## Specific Ideas

- 이동감 레퍼런스: Celeste류 — 빠르고 탄력적, 즉각 반응
- 낙사 복귀: 텔레포트 즉시 (이펙트 없음) + 스프라이트 깜빡임 1초
- 테스트 씬: 단순하게 — 하나의 넓은 플랫폼, 양쪽에 낙사 트리거

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 01-foundation-movement*
*Context gathered: 2026-05-27*
