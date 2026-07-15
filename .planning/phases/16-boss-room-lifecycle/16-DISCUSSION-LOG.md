# Phase 16: 보스 룸 콘텐츠 & 생명주기 게이팅 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-15
**Phase:** 16-boss-room-lifecycle
**Areas discussed:** 보스 룸 기능 그레이 에어리어(초기 제시, 미논의로 종료) → 코드 리팩토링(실제 논의 진행)

---

## 초기 그레이 에어리어 제시 (미논의)

Phase 16 도메인 분석 후 4개 후보를 AskUserQuestion(multiSelect)으로 제시:

| Option | Description | Selected |
|--------|-------------|----------|
| 보스 룸 스폰 아키텍처 | 체인 슬롯 교체 vs 브랜치 포탈 — BOSS-10이 `_chain` 노드 기준으로 동작하려면 이 결정이 구조를 좌우함 | |
| 입장 연출 (카메라+사운드) | BOSS-09 잠금/줄임 스타일, 입력 잠금 여부, 전용 스폰 사운드 재사용/신규 여부 | |
| 보스 룸 콘텐츠 & 아레나 구조 | PROJECT.md "아레나 구조도 고유" 목표 — 크기/레이아웃 차별화 | |
| 전투 판정 & 생명주기 게이팅 | BOSS-10 예외 트리거 조건, 타이머 재개 조건, 이탈/재진입 처리 | |

**사용자 응답 (Other/자유 텍스트):** "지금까지의 코드 흐름들을 살펴보고 이상한부분들에 대해 논의하고 리팩토링"

→ 4개 후보 모두 선택되지 않고 세션 전체가 리팩토링 트랙으로 전환됨. 위 4개 그레이 에어리어는 16-CONTEXT.md `<deferred>`에 "미논의, 다음 세션 필수"로 이관.

---

## 리팩토링 트랙 (실제 진행)

### WorldGenerator.cs
사용자가 범위를 명확히 요청: "정리 로직이 왜 3개나 있는지" → `RemoveTail()`/`RemoveHead()`/`FloorTransitionSequence()` 3중 중복 확인 → "공유하는 코드를 함수로 만들어서 빼고, 관리를 한번에 해" 지시.

**처리:** `/gsd:quick` (260715-kci)로 `CleanupSection(GameObject room, GameObject corridor, ExitPortal excludePortal = null)` 헬퍼 추출. 실행 중 발견된 주석 중복 1건은 별도 직접 수정(커밋 `4ec0928`).

**Phase 16 범위 재확인:** 이 리팩토링이 Phase 16의 "일부"인지 질문 → 사용자가 "Phase 16 자체를 리팩토링 포함 범위로 공식 확장"을 선택 → ROADMAP.md Phase 16 Goal/Requirements/Success Criteria 갱신(커밋 `0f5bee2`).

### CombatController.cs
`FindNearestEnemyInRange()`의 Linear/Fan 마우스 방향 계산 중복(`mousePos`/`mouseWorld` vs `mousePos2`/`mouseWorld2`), `DashOrWhiff()`/`ExecuteDash()`의 과도한 디버깅 `Debug.Log` 잔재 확인.

**사용자 지시:** "리스트업해서 한번에 처리하자" → 즉시 실행하지 않고 배치 목록에 편입.

### 점수 시스템 재설계 (사용자 제안)
"적들의 사망시 이벤트에 따라서 점수를 얻도록 바꾸고싶어... 보스를 만들 때도 별도의 점수 제거용 코드를 넣지 않아도 잘 작동할거야"

**조사 결과:** `CombatController.ExecuteDash()`가 무조건 `AddKillScore()` 호출 중 → `MeleeEnemy`/`RangedEnemy.OnDashHit()`은 항상 즉사이므로 타이밍 차이 없음 → 15-CONTEXT.md D-12(보스 점수 상쇄 우회책)가 이 재설계로 불필요해짐을 확인.

**사용자 지시:** "아까 리스트업한 리팩토링들과 함께 배치 처리"

### MeleeEnemy.cs / RangedEnemy.cs
`OnDashHit()`/`ClearHighlight()`/`IsPlayerInRange()`/`OnEnable()`/`OnDisable()`/`SetSpawnGate()` 거의 100% 동일 확인. `LayerPlayerHurtbox`/`LayerPlayerInvincible` 미사용 상수 발견.

**사용자 지시:** "죽은 코드는 삭제해줘. 그리고 EnemyBase로 상속해도되긴하는데 일단 서로 공유할 최소한의 내용들만 만들어줘."

### 죽은 파일 조사 (FloorSpawner / RoomExit / TestWorldGenerator / RoomEntry)
GUID 기반 씬/프리팹 교차 검증 진행:
- `TestWorldGenerator.cs` — 참조 0건, 완전히 죽음.
- `FloorSpawner.cs` — SampleScene.unity에 `m_IsActive: 0`인 GameObject 하나만 잔존.
- `RoomExit.cs` — 구형 `Room_*.prefab` 14종에 살아있는 상태로 부착. 처음엔 "죽었다"고 판단했으나, 재조사 결과 `Room_Debug.prefab`이 이 14개 프리팹 전부를 `targetRoomPrefab`으로 가리키는 `DebugRoomTeleporter` 14개짜리 테스트 허브였음이 밝혀짐(사용자가 처음에 "안 쓰는 걸로 알아"라고 했으나, 확인 결과 Room_Debug 허브 경유로 여전히 연결되어 있었음).
- `RoomEntry.cs` — 14개 프리팹 전부에 부착 + 현역 `DebugRoomTeleporter.cs`가 폴백으로 참조 중. **삭제 대상 아님**으로 확정.

**사용자 최종 결정:** "프리팹도 없어도 될것같고 Debug 텔레포터도 삭제해도 될것같은데" → 범위 확인 질문(AskUserQuestion: 텔레포터만 vs Room_Debug 통째) → **"Room_Debug.prefab 자체까지 통탵 삭제"** 선택.

**파생 이슈:** 15-CONTEXT.md D-11(Phase 15 FSM 테스트는 Room_Debug에서 진행 예정)이 무효화됨 → D-11 SUPERSEDED로 갱신, Phase 15 재계획 시 새 테스트 환경 필요.

### Unity MCP 삭제 가능 여부 확인
`mcp__unity-mcp__Unity_GetConsoleLogs` 호출 결과 `"Connection revoked. Go to Unity Editor > Project Settings > AI > Unity MCP to change approval."` — Unity Editor MCP 브릿지 미연결 확인.

**사용자 결정:** "그냥 유니티 에디터는 내가 수정할 테니까 너는 코드만 잘 수정해줘" → 씬/프리팹 삭제(비활성 FloorSpawner GameObject, Room_Debug.prefab, 구형 Room_*.prefab 14종)는 사용자가 직접 처리, Claude는 순수 코드(.cs)/문서(.md) 변경만 담당하도록 배치 범위 최종 확정.

### 최종 배치 확정 후 처리 방식
"무슨 일을 할 지 phase 16으로 문서화하고 clear한다음에 하자" → 즉시 실행 대신 16-CONTEXT.md/15-CONTEXT.md 갱신 후 세션 종료, 실행은 `/clear` 이후 별도 세션(`/gsd:quick --full` 예정)으로 이관.

---

## Claude's Discretion

- `EnemyBase` 추출 시 정확한 메서드 시그니처/protected 필드 이름
- `CombatController` 마우스 방향 헬퍼 메서드 이름
- Debug.Log 정리 범위(기본: 전부 삭제)

## Deferred Ideas

- 보스 룸 기능 자체의 4개 그레이 에어리어(스폰 아키텍처/입장 연출/아레나 콘텐츠/전투판정 게이팅) — 다음 세션에서 필수 논의
- Phase 15 재계획(새 보스 FSM 테스트 환경 결정)
