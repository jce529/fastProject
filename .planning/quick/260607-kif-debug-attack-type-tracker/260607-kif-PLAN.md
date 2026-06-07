---
phase: quick-260607-kif
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/Debug/AttackTypeDebugOverlay.cs
autonomous: true
requirements:
  - DEBUG-OVERLAY
must_haves:
  truths:
    - "런타임 중 현재 AttackType(Linear/Fan)이 화면에 표시된다"
    - "Zone에 진입할 때 표시값이 즉시 갱신된다"
    - "스크립트를 비활성화하면 오버레이가 완전히 사라진다"
  artifacts:
    - path: "Assets/Scripts/Debug/AttackTypeDebugOverlay.cs"
      provides: "OnGUI 기반 디버그 오버레이"
      exports: []
  key_links:
    - from: "AttackTypeDebugOverlay.cs"
      to: "AttackTypeSelector.Selected"
      via: "매 OnGUI 프레임 폴링"
      pattern: "AttackTypeSelector\\.Selected"
---

<objective>
디버그용 공격방식(AttackType) 화면 오버레이 스크립트를 생성하고 씬에 부착한다.

Purpose: 플레이테스트 중 Linear/Fan Zone 전환이 정상 동작하는지 육안으로 즉시 확인할 수 있어야 한다. 별도 Canvas/UI 프리팹 없이 OnGUI만 사용해 최소 의존성을 유지한다.
Output: Assets/Scripts/Debug/AttackTypeDebugOverlay.cs 생성 + Player GameObject에 컴포넌트 부착
</objective>

<execution_context>
@C:/Users/MSI/Projeect_A.E/fastProject/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/MSI/Projeect_A.E/fastProject/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@C:/Users/MSI/Projeect_A.E/fastProject/.planning/STATE.md

<!-- 핵심 인터페이스 — executor가 탐색 없이 바로 사용 가능 -->
<interfaces>
From Assets/Scripts/UI/AttackTypeSelector.cs:
```csharp
public enum AttackType { Linear, Fan }

public class AttackTypeSelector : MonoBehaviour
{
    // 현재 공격 타입 — Zone 진입 시 AttackTypeZone이 SetType()으로 변경
    public static AttackType Selected { get; private set; } = AttackType.Linear;

    // Zone에서 호출 — 값이 같으면 아무것도 하지 않음
    public static void SetType(AttackType type) { ... }
}
```

From Assets/Scripts/World/AttackTypeZone.cs:
```csharp
// Player 태그를 가진 Collider2D가 진입하면 AttackTypeSelector.SetType(zoneType) 호출
[RequireComponent(typeof(Collider2D))]
public class AttackTypeZone : MonoBehaviour
{
    [SerializeField] private AttackType zoneType; // Inspector에서 Linear/Fan 설정
    private void OnTriggerEnter2D(Collider2D other) { ... }
}
```
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: AttackTypeDebugOverlay 스크립트 작성</name>
  <files>Assets/Scripts/Debug/AttackTypeDebugOverlay.cs</files>
  <action>
Assets/Scripts/Debug/ 디렉토리를 생성하고 아래 사양으로 스크립트를 작성한다.

**클래스명:** `AttackTypeDebugOverlay` (MonoBehaviour)
**네임스페이스:** 없음 (프로토타입 일관성 유지)

**동작 사양:**

1. `OnGUI()` 에서 매 프레임 `AttackTypeSelector.Selected` 를 읽어 현재 값을 화면 좌상단(x=10, y=10)에 표시한다.
   - 표시 포맷: `"[DEBUG] Attack Type: Linear"` 또는 `"[DEBUG] Attack Type: Fan"`
   - Linear일 때 글자색: `Color.cyan`
   - Fan일 때 글자색: `Color.yellow`
   - 폰트 크기: 22
   - 배경 없음 (GUI.Label만 사용)

2. 변경 감지: `Update()` 에서 이전 프레임 값과 비교해 변경 시 `Debug.Log("[AttackTypeDebug] Type changed → {type}")` 출력.
   - `Debug.Log` 는 변경 순간 1회만 출력 (매 프레임 X)

3. 클래스 상단에 명확한 디버그 마커 주석 추가:
   ```csharp
   // ============================================================
   // DEBUG ONLY — Remove before release build
   // AttackTypeSelector.Selected 변경을 화면에 표시한다.
   // ============================================================
   ```

4. `[System.Diagnostics.Conditional("UNITY_EDITOR")]` 어트리뷰트는 사용하지 않는다
   (런타임 Android 플레이테스트에서도 보여야 함).
   대신 컴포넌트 자체를 비활성화하면 OnGUI/Update가 호출되지 않으므로,
   제거는 컴포넌트 Disable 또는 GameObject Destroy로 충분하다.

**금지 사항:**
- Canvas, UnityEngine.UI, TextMeshPro 의존성 추가 금지
- GUIStyle 캐싱을 OnGUI 내부에서 매 프레임 new로 생성 — 디버그 툴이므로 GC 허용
- `FindObjectOfType`, LINQ, GetComponent를 Update에서 호출 금지
  (AttackTypeSelector.Selected 는 static이므로 참조 불필요)
  </action>
  <verify>
    <automated>Unity 에디터에서 컴파일 오류 없음 확인: Assets/Scripts/Debug/AttackTypeDebugOverlay.cs 저장 후 Unity Console에 compile error 없어야 함</automated>
  </verify>
  <done>
    - AttackTypeDebugOverlay.cs 파일이 Assets/Scripts/Debug/ 에 존재한다
    - 컴파일 오류 없음
    - OnGUI, Update 메서드 모두 포함
    - AttackTypeSelector.Selected 폴링 로직 포함
    - 디버그 마커 주석 포함
  </done>
</task>

<task type="auto">
  <name>Task 2: Player GameObject에 컴포넌트 부착</name>
  <files>Assets/Scenes/SampleScene.unity</files>
  <action>
Unity MCP RunCommand를 사용해 씬의 Player GameObject에 AttackTypeDebugOverlay 컴포넌트를 부착한다.

**절차:**
1. Unity MCP `find_gameobject_by_name` 또는 `get_gameobjects_in_scene` 으로 "Player" GameObject를 찾는다.
2. `add_component` RunCommand로 `AttackTypeDebugOverlay` 컴포넌트를 Player에 추가한다.
3. 씬 저장 (Unity MCP `save_scene` 또는 에디터 Ctrl+S).

**주의:**
- Player GameObject가 없으면 "DebugManager" 라는 이름의 빈 GameObject를 생성해 부착한다.
- 기존에 AttackTypeDebugOverlay가 이미 부착된 경우 중복 추가하지 않는다.
- SampleScene.unity 파일을 직접 텍스트 편집하지 않는다 (YAML 구조 손상 위험).
  </action>
  <verify>
    <automated>Unity 에디터 Play Mode 진입 후 화면 좌상단에 "[DEBUG] Attack Type: Linear" 텍스트가 cyan 색으로 표시되면 성공</automated>
  </verify>
  <done>
    - SampleScene의 Player(또는 DebugManager) GameObject에 AttackTypeDebugOverlay 컴포넌트가 부착됨
    - Play Mode에서 화면 좌상단에 현재 AttackType이 표시됨
    - AttackTypeZone 진입 시 텍스트 색상과 값이 즉시 변경됨
    - Console에 "[AttackTypeDebug] Type changed → Fan/Linear" 로그가 Zone 진입 시 출력됨
  </done>
</task>

</tasks>

<verification>
1. Unity Play Mode 실행
2. 화면 좌상단에 "[DEBUG] Attack Type: Linear" (cyan) 표시 확인
3. 플레이어를 Fan Zone으로 이동 → "[DEBUG] Attack Type: Fan" (yellow) 로 즉시 변경 확인
4. Console에 "Type changed → Fan" 로그 1회 출력 확인
5. 컴포넌트 Disable → 오버레이 텍스트 사라짐 확인
</verification>

<success_criteria>
- Play Mode에서 AttackType 값이 화면에 실시간 표시된다
- Zone 진입 시 색상과 텍스트가 즉시 전환된다
- Canvas/UI 프리팹 의존성 없음
- 컴포넌트 비활성화로 완전 제거 가능
</success_criteria>

<output>
완료 후 `.planning/quick/260607-kif-debug-attack-type-tracker/260607-kif-SUMMARY.md` 를 생성한다.

SUMMARY 포맷:
- 생성된 파일 목록
- 부착된 GameObject 이름
- 사용 방법 (1-2줄)
- 제거 방법 (컴포넌트 Disable/Destroy)
</output>
