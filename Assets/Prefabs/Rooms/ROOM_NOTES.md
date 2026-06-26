# Room Prefab 수정 주의사항

> 이 문서는 Room_Chase / Room_Combat / Room_Dodge / Room_Gap / Room_Mixed 5개 프리팹 수정 시 반드시 읽어야 할 주의사항입니다.

---

## Git 관련 주의사항

### Claude Code worktree 에이전트 실행 전 반드시 커밋

Claude Code의 `/gsd:quick`, `/gsd:execute-phase` 등 **worktree 에이전트가 동작하는 명령은 `git reset --hard HEAD`를 실행**할 수 있음. 커밋되지 않은 프리팹 변경사항은 이 때 영구 삭제됨.

- **2026-06-24 사고 사례**: Room_Chase 외 4개 프리팹의 미커밋 작업이 `merge worktree-agent` + `reset: moving to HEAD` 순서로 실행되며 전부 소실됨. Unity 백업도 없었음.

**규칙**: Unity 에디터에서 프리팹 수정 완료 후 → Unity 에디터 저장 → **즉시 `git add` + `git commit`** → 그 다음에 Claude Code 명령 실행.

---

## Unity 에디터 관련 주의사항

### 타임스탬프 변경 ≠ 내용 변경

Unity 에디터가 씬을 열거나 빌드할 때 프리팹 파일을 읽으면서 **수정 시간만 갱신**되는 경우가 있음. `git diff`로 내용 변경 여부를 반드시 확인할 것.

### 프리팹 저장 방법

프리팹을 Hierarchy에서 수정한 뒤 저장하지 않으면 `.prefab` 파일에 반영되지 않음.
- Hierarchy 상단 프리팹 이름 → **Overrides → Apply All** 또는 Ctrl+S

---

## Door / ENT / EXIT 구조

모든 룸 프리팹은 다음 계층 구조를 가짐:

```
Room_XXX (root)
└── Door  (빈 GameObject, position 0,0,0)
    ├── ENT  (RoomEntry 컴포넌트, 플레이어 스폰 위치 마커)
    └── EXIT (SpriteRenderer + BoxCollider2D IsTrigger, 출구)
```

**컴포넌트 설정 규칙:**
| 오브젝트 | 컴포넌트 | 설정 |
|---------|---------|------|
| ENT | RoomEntry | - |
| EXIT | SpriteRenderer | DrawMode: Sliced, Size: 1×1 |
| EXIT | BoxCollider2D | Size: 1×1, IsTrigger: On |
| EXIT | Transform | **크기 조절은 Scale만 사용** (Size 수정 금지) |

---

## 각 룸별 설계 의도 및 수정 시 주의점

### Room_Combat (순수 전투)
- **검증 질문**: 공격 자체가 재미있는가?
- **적 배치**: 근접 적 **5마리 이상** 배치 (지속적인 전투 발생 유도)
- **지형**: 넓은 평지, 장애물 없음, 낙사 없음
- **핵심**: 돌진 공격의 최대 사거리가 P→E에 정확히 닿도록 X축 거리 조절

### Room_Chase (도망치기)
- **검증 질문**: 싸우지 않아도 재미있는가?
- **적 설정**: 절대 처치 불가능한 거대 적 (전투 시도 시 즉사 or 무의미한 결과)
- **주의**: 플레이어가 전투 포기를 자연스럽게 선택하도록 유도 — 적을 약하게 만들지 말 것

### Room_Dodge (회피 특화)
- **검증 질문**: 구르기 자체가 재미있는가?
- **구조**: 천장과 바닥이 막힌 공간, 점프/우회 불가
- **핵심**: 오직 정확한 타이밍의 구르기로만 판정 통과 가능하도록 공격 범위 설계

### Room_Gap (점프 챌린지)
- **검증 질문**: 이동 자체가 즐거운가?
- **구조**: 넓은 점프 간격, 낙사 가능 구역 존재
- **주의**: 적 배치 없음 — 순수 이동 감각 검증

### Room_Mixed (실전 테스트)
- **검증 질문**: 게임 전체가 재미있는가?
- **포함 요소**: 전투 / 연속 처치 / 등반 / 구르기 / 원거리 적 / 점프 / 낙사 / 경로 선택
- **주의**: 한 번에 너무 많이 바꾸지 말 것. 하나씩 추가하며 플레이테스트

---

## ENT / EXIT 위치 기준 (각 룸)

| 룸 | ENT 위치 (기준) | EXIT 위치 (기준) |
|----|----------------|----------------|
| Room_Chase | 바닥 좌측 끝 | 바닥 우측 끝 |
| Room_Combat | 바닥 좌측 끝 | 바닥 우측 끝 |
| Room_Dodge | 바닥 좌측 (Left Ground) | 바닥 우측 (Right Ground) |
| Room_Gap | 좌측 하단 발판 위 | 우측 최상단 발판 위 |
| Room_Mixed | 좌측 하단 시작점 | 우측 상단 Chain 구간 위 |

> 위 위치는 설계 기준값. Unity 에디터에서 실제 배치에 맞게 Transform Position으로 조정.
