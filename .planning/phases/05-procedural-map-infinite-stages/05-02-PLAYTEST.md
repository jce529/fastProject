# Phase 05-02 플레이 테스트 체크리스트

## 사전 확인

- [ ] Hierarchy에 `FloorSpawner` 오브젝트 존재
- [ ] `Platform`, `MeleeEnemy`, `RangedEnemy` 오브젝트 비활성화(회색) 상태
- [ ] Console 에러 없음 (MCP WebSocket 에러는 무시)

---

## Test 1: 1층 스폰 확인

**절차**
1. Play 버튼 클릭
2. Hierarchy에서 `Room_Combat(Clone)` 인스턴스 생성 확인
3. 1층에는 적이 없어야 함 (튜토리얼 층)

**PASS 기준:** Room_Combat 스폰 확인, 적 없음

- [ ] PASS
- [ ] FAIL — 메모:

---

## Test 2: 층 전환 시퀀스 (1층 → 2층)

**절차**
1. 플레이어를 Room_Combat 최상단(Y ≈ 17)까지 점프해서 올라감
2. RoomExit 트리거 접촉 순간 확인:
   - 플레이어 입력이 즉시 잠김 (이동/점프 불가)
   - 플레이어가 2층 위치로 순간이동
   - 카메라가 즉시 2층을 표시
   - 2층 Room이 Hierarchy에 나타남
   - 2층 적들이 활성화됨
   - 조작 재개됨

**PASS 기준:** 위 6단계가 순서대로 발동

- [ ] PASS
- [ ] FAIL — 메모:

---

## Test 3: 이전 층 파괴 확인 (FLOOR-04)

**절차**
1. 2층으로 이동 후 Hierarchy 창 확인
2. `Room_Combat(Clone)` 인스턴스가 사라져야 함
3. 현재 층 Room 인스턴스만 남아 있어야 함

**PASS 기준:** Hierarchy에 Room 인스턴스 1개만 존재

- [ ] PASS
- [ ] FAIL — 메모:

---

## Test 4: 이중 발동 방지

**절차**
1. RoomExit 트리거를 빠르게 여러 번 통과 시도
2. 층 번호가 1씩만 증가하는지 확인 (2가 되어야 하며 3이 되면 FAIL)

**PASS 기준:** 층 번호 1씩 증가, 이중 발동 없음

- [ ] PASS
- [ ] FAIL — 메모:

---

## Test 5: 사망 후 재시작 (선택 — Phase 4 컴포넌트 필요)

> DeathScreenController / HUDController 미연결 시 SKIP 가능.
> Test 1~4 통과 시 Phase 5 핵심 검증 완료로 간주.

**절차**
1. 적에게 죽거나 낙사 → DeathScreen 표시
2. Restart 버튼 클릭
3. `Floor: 1` 표시 및 `Room_Combat` 재스폰 확인

**PASS 기준:** 1층으로 정상 리셋

- [ ] PASS
- [ ] FAIL
- [ ] SKIP — 이유:

---

## 실패 시 체크포인트

| 증상 | 확인 항목 |
|------|----------|
| 이동이 잠기지 않음 | Player > CombatController > `_player` 필드 연결 여부 |
| 카메라 스냅 안 됨 | FloorSpawner > `_playerTransform` 필드 연결 여부 |
| 적이 나타나지 않음 | EnemySpawnPoint 태그 확인 + `_meleeEnemyPrefab` 연결 |
| Floor 번호 갱신 안 됨 | FloorManager.CurrentFloor++ 위치 확인 |
| 이전 층이 안 사라짐 | FloorSpawner `_currentRoom` Destroy 로직 확인 |

---

## 최종 결과

| Test | 결과 |
|------|------|
| Test 1 (1층 스폰) | |
| Test 2 (층 전환) | |
| Test 3 (이전 층 파괴) | |
| Test 4 (이중 발동 방지) | |
| Test 5 (재시작) | SKIP / PASS / FAIL |

> Test 1~4 모두 PASS → `passed` 입력 후 플랜 완료 진행
