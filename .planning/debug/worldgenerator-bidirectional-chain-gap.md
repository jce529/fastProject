---
status: investigating
trigger: "이제 시작되도 양쪽으로 생기게끔 바꿔줘 그리고 카메라는 Bound합치기로 가자"
created: 2026-07-06T09:00:00Z
updated: 2026-07-06T10:05:00Z
---

## Current Focus

hypothesis: 462f558("RemoveTail/RemoveHead 직후 _chainTailEntryPos/_chainHeadExitPos를 새 경계 room 기준으로 갱신")이 간격(gap) 버그의 근본 원인 자체는 맞게 짚었으나, 매 Remove 호출마다 필드를 무조건 갱신하는 방식이 다른 실행 경로와 충돌해 "왼쪽 생성이 아예 안 됨"이라는 더 심각한 회귀를 만들었다. 정확한 충돌 지점은 아직 미확인.
test: (다음 세션에서) Play Mode에서 _chainTailEntryPos/_chainHeadExitPos 값을 SpawnPrevPair/SpawnNextPair/RemoveTail/RemoveHead 호출마다 Debug.Log로 프레임 단위 추적 — 어느 시점에 값이 기대와 달라지는지 직접 확인 필요.
expecting: 로그에서 "Remove 호출 직후 갱신된 앵커 값"과 "그 다음 Spawn 호출 시 실제로 읽히는 앵커 값"이 어긋나는 지점을 찾으면 근본 원인 특정 가능.
next_action: 462f558의 아이디어(트림 후 앵커 재계산)를 필드 캐시 방식이 아니라 **매 SpawnPrevPair()/SpawnNextPair() 호출 시점에 현재 체인 경계 room에서 커넥터 위치를 즉시(realtime) 재조회**하는 방식으로 다시 구현하는 대안을 우선 검토할 것. 필드 캐시(`_chainTailEntryPos`/`_chainHeadExitPos`) 자체가 "갱신 시점을 하나라도 놓치면 즉시 stale해지는" 구조적 취약점을 안고 있어, 같은 종류의 버그가 반복 재발할 위험이 있음.

## Symptoms

expected: 플레이어가 어느 방향으로 걷든(왼쪽/오른쪽) Room+Corridor 체인이 끊김이나 간격 없이 계속 이어져서 생성되고, 플레이어가 멀어진 반대쪽 끝은 lookahead/lookbehind 범위를 벗어나면 정리(trim)된다. 복도 중간에서 방향을 되돌려도 어떤 room도 건너뛰거나 중복 생성되지 않는다.
actual: (여러 단계로 진화한 증상 — 최신 상태가 가장 아래)
  1. [해결됨] Start()에서 왼쪽 초기 생성이 전혀 안 됨 — SpawnPrevPair 자체 미호출.
  2. [해결됨] 복도 중간에서 방향을 되돌리면 room이 한 칸 건너뛰어 생성됨(인덱스가 "현재 room 이탈" 시점에 너무 일찍 바뀌어서 발생).
  3. [462f558로 수정 시도했으나 부작용 발생] 왼쪽→오른쪽(또는 반대) 방향 전환 시 체인에 시각적 간격(gap) 발생 — Scene뷰 스크린샷으로 확인(CameraBound gizmo들이 이어지다가 마지막 하나가 간격을 두고 분리됨).
  4. [현재 미해결, 462f558 revert로 롤백됨] 462f558 적용 후 재테스트 결과 "왼쪽이 아예 생성이 안 됨" — 3번보다 심각한 신규 회귀. b4ef259로 462f558을 revert해 3번 상태(간격 버그 재발, 1·2번은 해결 유지)로 되돌림.
errors: 없음 — Console에 에러/경고 없이 순수 위치·타이밍 로직 버그.
reproduction: SampleScene Play → MainMenu → 공격 타입 선택(AttackSelect) → SampleScene 진입 (GameBootstrapper가 항상 MainMenu로 부트스트랩하므로 SampleScene을 직접 열고 Play해도 이 경로를 거쳐야 함) → 플레이어를 왼쪽/오른쪽으로 왕복 이동.
started: 이번 세션 quick-260706-oxp(WorldGenerator 양방향 생성 + CameraFollow Bounds 합치기 추가) 작업 중 발생.

## Eliminated

- hypothesis: 카메라 Bounds 병합(RecomputeCameraBounds, d33c071)이 간격/생성 문제의 원인
  evidence: 사용자가 "카메라 추적은 연속적인게 확인됐어"로 명시적으로 확인 — 카메라 로직은 정상, 문제는 WorldGenerator의 체인 생성/트리밍 로직에 국한됨.
  timestamp: 2026-07-06T09:20:00Z

- hypothesis: Start()의 SpawnPrevPair() 루프 자체가 실행되지 않음(왼쪽 초기 생성 실패)
  evidence: 사용자가 "스폰할 때 좌우로 방이 두 칸 생성되는 건 확인했다"고 확인 — Start() 시점 초기 양방향 생성은 정상 동작. 문제는 Update()에서의 "연속" 생성/트리밍 로직에 있었음.
  timestamp: 2026-07-06T09:30:00Z

- hypothesis: UpdatePlayerIndex()가 "현재 room의 EXIT/ENT" 기준으로 인덱스를 전진/후퇴시켜서 복도 중간 방향전환 시 인덱스가 너무 일찍 바뀜
  evidence: 9b259dc에서 "다음/이전 room의 ENT/EXIT" 기준으로 변경 후 사용자가 이 문제를 다시 언급하지 않고 새로운 증상(간격)으로 넘어감 — 이 가설은 확인되었고 수정도 유효한 것으로 보임.
  timestamp: 2026-07-06T09:50:00Z

## Evidence

- timestamp: 2026-07-06T09:15:00Z
  checked: WorldGenerator.Update()의 GEN-01/02(기존, 오른쪽 전용)와 UpdatePlayerIndex()(기존, 전진만 가능)
  found: _playerCurrentIndex가 오른쪽 이동에서만 증가하고 왼쪽 이동에서는 전혀 감소하지 않음. Update()에도 왼쪽 방향 연속 생성/트리밍 로직이 없었음.
  implication: "복도까지는 생기는데 계속 걸으면 왼쪽이 안 생긴다"는 최초 증상의 직접 원인 — GEN-05(SpawnPrevPair 연속 호출)/GEN-06(RemoveHead 신규)과 UpdatePlayerIndex() 후퇴 로직 추가로 해결.

- timestamp: 2026-07-06T09:40:00Z
  checked: UpdatePlayerIndex()가 어떤 커넥터를 기준으로 인덱스를 전진시키는지
  found: 기존 코드는 `_chain[i].room`(현재 room)의 Right/Left 커넥터를 기준으로 사용 — 즉 "현재 room을 이탈하는 순간" 인덱스가 바뀜. 복도 전체를 아직 안 건넜는데도 인덱스가 다음 room으로 넘어가버림.
  implication: 복도 중간에서 되돌아가면 이미 트리밍/생성이 발생한 뒤라 room이 건너뛰어 보임 — 9b259dc에서 "다음/이전 room 자신의" 커넥터 기준으로 변경.

- timestamp: 2026-07-06T09:55:00Z
  checked: 사용자 제공 Scene뷰 스크린샷(왼쪽→오른쪽 방향 전환 후)
  found: CameraBound gizmo 박스들이 계단식으로 이어지다가 마지막 하나가 나머지와 떨어진 채(간격을 두고) 우측에 별도로 존재.
  implication: 방향을 전환해 되돌아갈 때 SpawnNextPair()/SpawnPrevPair()가 "현재 실제 체인 경계"가 아닌 다른 위치를 기준점으로 사용하고 있다는 강한 증거.

- timestamp: 2026-07-06T09:58:00Z
  checked: RemoveTail()/RemoveHead()가 _chainTailEntryPos/_chainHeadExitPos를 갱신하는지 여부
  found: 갱신하지 않음 — 두 필드는 SpawnPrevPair()/SpawnNextPair() 호출 시에만 갱신됨. Remove 계열 함수가 반대쪽 끝의 앵커를 그대로 방치.
  implication: 트림 이후 방향을 바꾸면 이미 파괴된 room의 옛 위치를 기준점으로 새 room을 이어붙이게 되어 간격 발생 — 스크린샷 증상과 정확히 일치. → 462f558로 수정 시도.

- timestamp: 2026-07-06T10:00:00Z
  checked: 462f558 적용 후 사용자 재테스트 결과
  found: "다시 왼쪽이 아예 생성이 안되는데" — 앵커를 Remove 시점마다 갱신하는 방식이 예상 못한 다른 상호작용을 일으켜 왼쪽 생성 자체가 막힘. 구체적으로 어떤 조건(예: RemoveTail이 호출되는 순간 _chain.Count가 아직 갱신 전이라 잘못된 인덱스를 참조하는지, 혹은 GEN-05/06 while 루프와의 순서 문제인지)에서 막히는지는 미조사 상태.
  implication: 462f558을 b4ef259로 revert. 근본 아이디어(트림 후 앵커가 stale해짐)는 맞지만 구현 방식을 재검토해야 함.

## Resolution

root_cause: |
  부분적으로만 확인됨. 확정된 원인 2건:
  1. Update()에 왼쪽 방향 연속 생성/트리밍 로직 자체가 없었음 (27289f7로 수정).
  2. UpdatePlayerIndex()가 "현재 room 이탈" 시점에 인덱스를 바꿔 복도 중간 방향전환 시 조기 트리밍/생성 발생 (9b259dc로 수정).
  아직 미확정: 방향 전환 시 체인에 간격이 생기는 근본 원인 — RemoveTail()/RemoveHead()가 _chainTailEntryPos/_chainHeadExitPos를 갱신하지 않아 stale해진다는 가설은 유력하지만, 이를 "Remove 호출 시점마다 필드 갱신"으로 구현한 462f558이 왼쪽 생성을 완전히 막는 신규 회귀를 일으켜 revert됨 — 필드 캐시 갱신 타이밍 자체보다 더 근본적인 설계 문제(캐시 방식의 구조적 취약점)일 가능성을 다음 세션에서 검토 필요.

fix: |
  1건 미해결. 현재 코드는 9b259dc 상태로 롤백되어 있음 (462f558 및 그 이전 커밋들은 유지, 462f558만 b4ef259로 revert됨):
  - 8297aeb, d33c071, 27289f7, 9b259dc: 유지 (양방향 초기 생성, 카메라 Bounds 병합, 연속 생성/트리밍, 인덱스 타이밍 수정 — 모두 정상 동작 확인됨)
  - 462f558: revert됨 (b4ef259) — "간격 버그"가 재발한 상태.
  다음 세션 제안: _chainTailEntryPos/_chainHeadExitPos를 필드에 캐시하지 않고, SpawnPrevPair()/SpawnNextPair() 호출 시점에 `_chain[0]`/`_chain[^1]`의 실제 커넥터 위치를 그때그때 조회하는 방식으로 전환 검토.

verification: 미해결 — 간격 버그 재현 확인 필요, 왼쪽 생성 회귀는 revert로 해소됨(재확인 필요).
files_changed:
  - Assets/Scripts/World/WorldGenerator.cs
