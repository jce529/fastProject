---
phase: quick-260605-tss
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/Player/CombatController.cs
autonomous: true
requirements: [ATCK-02]
must_haves:
  truths:
    - "슬로우모션 유지 중 가장 가까운 적이 빨간색으로 강조 표시됨"
    - "대시 실행 후 적의 빨간색 강조가 화면에 보인 채로 대시가 완료됨 (대시 전 강조가 사라지지 않음)"
  artifacts:
    - path: "Assets/Scripts/Player/CombatController.cs"
      provides: "Update()에서 _isSlowMo 중 하이라이트 갱신 + ExitSlowMotion() 이중 호출 방지"
  key_links:
    - from: "Update() _isSlowMo 블록"
      to: "FindNearestEnemyInRange()"
      via: "_isSlowMo 조건 아래 매 프레임 호출"
    - from: "DashOrWhiff()"
      to: "ExecuteDash()"
      via: "FindNearestEnemyInRange() 호출 제거 — Update()가 이미 _lastHighlighted 설정"
---

<objective>
CombatController 두 가지 하이라이트 버그를 수정한다.

Purpose: 슬로우모션 중 타깃 적이 빨간색으로 강조되어야 "어느 적을 처치할지" 플레이어에게 명확하게 전달된다. 현재는 강조가 전혀 보이지 않아 핵심 피드백이 누락된 상태다.
Output: CombatController.cs 수정본 — 슬로우모션 중 Update() 하이라이트 + ExitSlowMotion() 이중 호출 제거
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@Assets/Scripts/Player/CombatController.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Update()에 슬로우모션 하이라이트 추가 + ExitSlowMotion() 이중 호출 제거</name>
  <files>Assets/Scripts/Player/CombatController.cs</files>
  <action>
두 버그를 한 번에 수정한다.

**버그 1 수정 — ExitSlowMotion() 이중 호출:**
현재 흐름: AttackReleased → ExitSlowMotion() (line 131) → _lastHighlighted = null → DashOrWhiff() → FindNearestEnemyInRange() → _lastHighlighted = target → ExecuteDash() → ExitSlowMotion() → ClearHighlight() → 하이라이트 소멸.

수정: DashOrWhiff() 내부 FindNearestEnemyInRange() 호출에서 하이라이트 업데이트를 분리한다.
FindNearestEnemyInRange()에서 하이라이트 업데이트 블록(line 299-310, "if (nearest != _lastHighlighted)" 블록)을 제거한다.
Update()의 _isSlowMo 구간에서만 하이라이트를 갱신하도록 한다.

**버그 2 수정 — 슬로우모션 중 하이라이트 없음:**
Update()에서 _isSlowMo가 true일 때 매 프레임 FindNearestEnemyInRange()를 호출하되, 반환값은 버린다(하이라이트 사이드이펙트만 사용).

구체적 변경 사항:

1. FindNearestEnemyInRange() 메서드에서 하이라이트 업데이트 블록을 제거한다:
   - line 299-310의 "// Update enemy highlight (D-04)" 주석과 if (nearest != _lastHighlighted) 블록 전체 삭제
   - 메서드는 nearest만 반환

2. 새로운 private void UpdateHighlight(DummyEnemy nearest) 메서드를 추가한다:
   ```csharp
   private void UpdateHighlight(DummyEnemy nearest)
   {
       if (nearest == _lastHighlighted) return;
       if (_lastHighlighted != null) _lastHighlighted.ClearHighlight();
       if (nearest != null)
       {
           var sr = nearest.GetComponent<SpriteRenderer>();
           if (sr != null) sr.color = Color.red;
       }
       _lastHighlighted = nearest;
   }
   ```

3. Update()의 _isSlowMo 유지 구간(EnterSlowMotion 호출 이후, ExitSlowMotion 호출 이전)에 추가:
   ```csharp
   // 슬로우모션 유지 중 — 가장 가까운 적 하이라이트 갱신 (D-04)
   if (_isSlowMo && !_isBusy)
       UpdateHighlight(FindNearestEnemyInRange());
   ```
   이 줄은 Update()에서 `if (_isSlowMo && _gauge.IsEmpty) ExitSlowMotion();` 블록 직전에 삽입한다.

4. ExitSlowMotion()의 하이라이트 클리어 로직은 그대로 유지한다 (슬로우모션 종료 시 정리 역할).

이 구조로 DashOrWhiff()의 FindNearestEnemyInRange()는 순수하게 가장 가까운 적 탐색만 수행하고, 하이라이트는 Update()의 UpdateHighlight()가 관리한다. ExecuteDash()가 ExitSlowMotion()을 호출해도 _lastHighlighted는 이미 null이거나 슬로우모션 종료와 함께 정상 정리된다.
  </action>
  <verify>
    <automated>Unity Editor에서 Play → 공격 버튼 홀드 → 적 방향으로 마우스 이동 → 슬로우모션 중 적이 빨간색인지 확인 → 버튼 릴리즈 → 대시 중 적 색상 확인 (화면 깜빡임 없이 빨간색 유지 후 처치 시 정리)</automated>
  </verify>
  <done>
    - 슬로우모션 유지 중 범위 내 가장 가까운 적이 빨간색으로 강조됨
    - 공격 버튼 릴리즈 후 대시 실행 시 하이라이트가 한 프레임만에 사라지지 않음
    - 슬로우모션 종료(ExitSlowMotion) 시 하이라이트가 정상적으로 정리됨
    - 컴파일 에러 없음
  </done>
</task>

</tasks>

<verification>
Unity Editor Play Mode에서 검증:
1. 공격 버튼 홀드 — 슬로우모션 진입 확인
2. 범위 내 DummyEnemy가 빨간색으로 변하는지 확인 (슬로우모션 유지 중)
3. 플레이어가 이동하며 다른 적이 더 가까워질 때 하이라이트 대상이 전환되는지 확인
4. 버튼 릴리즈 → 대시 → 처치 시 정상 흐름 (하이라이트 깜빡임 없음) 확인
5. 범위 밖이거나 적 없을 때 하이라이트 없음 확인
</verification>

<success_criteria>
슬로우모션 중 타깃 적이 빨간색으로 표시되고, 대시 실행 시 하이라이트가 즉시 사라지지 않는다.
</success_criteria>

<output>
완료 후 `.planning/quick/260605-tss-combatcontroller-1-update-findnearestene/260605-tss-SUMMARY.md` 생성
</output>
