---
phase: quick
plan: 260604-sou
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Prefabs/DummyEnemy.prefab
autonomous: true
requirements: []

must_haves:
  truths:
    - "DummyEnemy가 씬에서 현재의 2배 크기로 렌더링된다"
    - "DummyEnemy의 물리 콜라이더가 시각적 크기와 일치한다"
  artifacts:
    - path: "Assets/Prefabs/DummyEnemy.prefab"
      provides: "Transform scale 2배 + 콜라이더 정합"
      contains: "m_LocalScale: {x: 1.6, y: 2.4, z: 1}"
  key_links:
    - from: "Transform.m_LocalScale"
      to: "CapsuleCollider2D world size"
      via: "Unity scales collider by Transform automatically"
      pattern: "m_LocalScale.*1.6.*2.4"
---

<objective>
DummyEnemy 프리팹의 Transform scale을 2배(0.8→1.6, 1.2→2.4)로 키운다.

Purpose: 플레이어 대비 적의 시각적 존재감을 높여 전투 타깃으로서 인식성 개선.
Output: Assets/Prefabs/DummyEnemy.prefab — scale 2배 적용된 프리팹.
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/STATE.md
@Assets/Prefabs/DummyEnemy.prefab
@Assets/Scripts/Enemy/DummyEnemy.cs

현재 프리팹 값:
- Transform.m_LocalScale: {x: 0.8, y: 1.2, z: 1}
- CapsuleCollider2D.m_Size: {x: 0.8, y: 1.2}  (로컬 공간)
- CapsuleCollider2D.m_Offset: {x: 0, y: 0}

핵심 이해: Unity의 CapsuleCollider2D.m_Size는 로컬 공간 값이다.
Transform scale이 (0.8, 1.2)일 때 콜라이더 로컬 size (0.8, 1.2)는
세계 공간에서 (0.64, 1.44) 크기로 표현된다.
Transform scale을 2배로 올리면 세계 공간 콜라이더도 자동으로 2배가 된다.
따라서 콜라이더 m_Size는 변경하지 않아도 정합이 유지된다.
</context>

<tasks>

<task type="auto">
  <name>Task 1: DummyEnemy 프리팹 Transform scale 2배 적용</name>
  <files>Assets/Prefabs/DummyEnemy.prefab</files>
  <action>
    Assets/Prefabs/DummyEnemy.prefab YAML에서 Transform 섹션(&1000005)의
    m_LocalScale 값을 수정한다.

    변경 전:
      m_LocalScale: {x: 0.8, y: 1.2, z: 1}

    변경 후:
      m_LocalScale: {x: 1.6, y: 2.4, z: 1}

    CapsuleCollider2D(m_Size, m_Offset)는 수정하지 않는다.
    이유: Unity는 Collider2D의 m_Size를 로컬 공간 기준으로 저장하므로,
    Transform scale이 2배가 되면 세계 공간 콜라이더 크기도 자동으로 2배가 된다.
    별도로 m_Size를 수정하면 콜라이더가 시각적 크기의 4배가 되어버린다.

    SpriteRenderer의 m_Size도 수정하지 않는다(스프라이트 draw size는 Transform에 영향받음).
  </action>
  <verify>
    파일에서 확인:
    grep "m_LocalScale" Assets/Prefabs/DummyEnemy.prefab
    → {x: 1.6, y: 2.4, z: 1} 출력 확인

    Unity Editor에서 DummyEnemy 프리팹 열었을 때:
    - Transform Scale X=1.6, Y=2.4 표시
    - CapsuleCollider2D 초록 경계선이 스프라이트 크기와 일치
  </verify>
  <done>
    프리팹의 Transform scale이 (1.6, 2.4, 1)이고,
    씬에서 DummyEnemy가 기존 대비 2배 크기로 표시되며,
    CapsuleCollider2D 경계가 스프라이트 외곽과 일치한다.
  </done>
</task>

</tasks>

<verification>
Unity Editor에서 DummyEnemy 프리팹을 열고:
1. Transform Inspector: Scale X=1.6, Y=2.4 확인
2. Scene view에서 초록 콜라이더 경계선이 스프라이트 크기와 겹치는지 확인
3. Play 모드에서 플레이어가 더 커진 DummyEnemy를 정상 타깃으로 인식하는지 확인
</verification>

<success_criteria>
- DummyEnemy.prefab의 m_LocalScale이 {x: 1.6, y: 2.4, z: 1}
- CapsuleCollider2D 세계 공간 크기가 스프라이트 시각 크기와 일치
- 기존 씬 배치 인스턴스들이 자동으로 새 크기 반영 (프리팹 오버라이드 없는 경우)
</success_criteria>

<output>
작업 완료 후 .planning/quick/260604-sou-dummyenemy/ 에 SUMMARY.md 생성 불필요.
STATE.md의 Quick Tasks Completed 테이블에 이 작업 항목 추가.
</output>
