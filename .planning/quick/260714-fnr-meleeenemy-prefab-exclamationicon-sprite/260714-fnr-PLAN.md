---
phase: quick
plan: 260714-fnr
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Editor/ExclamationIconBuilder.cs
  - Assets/Sprites/UI/ExclamationMark.png
  - Assets/Prefabs/Enemies/MeleeEnemy.prefab
autonomous: false
requirements: [D-05]

must_haves:
  truths:
    - "MeleeEnemy가 Telegraph 상태에 진입하면 머리 위에 노란색 '!' 아이콘이 실제로 화면에 보인다"
    - "Telegraph 종료(Attack 전환) 시 '!' 아이콘이 다시 사라진다"
    - "MeleeEnemy.cs의 Telegraph 타이밍/이동 로직(999.4-03 산출물)은 코드 한 줄도 변경되지 않는다"
  artifacts:
    - path: "Assets/Editor/ExclamationIconBuilder.cs"
      provides: "절차적 '!' 텍스처 생성 + MeleeEnemy.prefab ExclamationIcon SpriteRenderer에 배정하는 에디터 도구"
      contains: "MenuItem(\"Fast/Quick/Build MeleeEnemy Exclamation Icon\")"
    - path: "Assets/Sprites/UI/ExclamationMark.png"
      provides: "신규 생성된 '!' 모양 스프라이트 텍스처 (투명 배경, 흰색 도형 — SpriteRenderer 색상 틴트로 노란색 표시)"
    - path: "Assets/Prefabs/Enemies/MeleeEnemy.prefab"
      provides: "ExclamationIcon 자식 SpriteRenderer에 실제 스프라이트 할당 (m_WasSpriteAssigned: 1)"
      contains: "m_WasSpriteAssigned: 1"
  key_links:
    - from: "Assets/Editor/ExclamationIconBuilder.cs"
      to: "Assets/Prefabs/Enemies/MeleeEnemy.prefab"
      via: "PrefabUtility.LoadPrefabContents → ExclamationIcon 자식 SpriteRenderer.sprite 배정 → PrefabUtility.SaveAsPrefabAsset"
      pattern: "PrefabUtility\\.SaveAsPrefabAsset"
    - from: "Assets/Prefabs/Enemies/MeleeEnemy.prefab"
      to: "Assets/Sprites/UI/ExclamationMark.png"
      via: "SpriteRenderer.m_Sprite fileID/guid 참조"
      pattern: "m_Sprite: \\{fileID: [1-9]"
---

<objective>
MeleeEnemy.prefab의 ExclamationIcon(SpriteRenderer, fileID 8043105589779039711)에 스프라이트가 미할당(`m_Sprite: {fileID: 0}`, `m_WasSpriteAssigned: 0`)되어, MeleeEnemy.cs의 `TelegraphAndAttack()` 코루틴이 `_exclamationIcon.enabled = true`를 호출해도 "!" 아이콘이 화면에 나타나지 않는 문제를 수정한다.

Purpose: D-05("Telegraph 중 이동하며 예고, '! 아이콘만 뜨고 가만히 서있는' 정적인 느낌 제거")가 실제로 완성되려면 "!" 아이콘 자체가 시각적으로 보여야 한다 — 현재는 코드 로직은 정상이나 애셋 배정 누락으로 아이콘이 투명하게 렌더링된다.
Output: 절차적으로 생성된 "!" 스프라이트 애셋 1개, 이를 생성/배정하는 재사용 가능한 에디터 도구, 스프라이트가 실제 할당된 MeleeEnemy.prefab.
</objective>

<execution_context>
@D:\새 폴더\Fast\.claude\get-shit-done\workflows\execute-plan.md
@D:\새 폴더\Fast\.claude\get-shit-done\templates\summary.md
</execution_context>

<context>
@.planning/STATE.md
@Assets/Scripts/Enemy/MeleeEnemy.cs
@Assets/Editor/HitSparkBuilder.cs

<!--
정밀 변경 원칙: 이 quick task는 ExclamationIcon 스프라이트 미할당 문제만 해결한다.
MeleeEnemy.cs Telegraph 로직(999.4-03에서 방금 재작성됨, §188-245)은 절대 수정하지 않는다.
기존 코드베이스 패턴: 이 프로젝트의 모든 프리팹 생성/수정은 Assets/Editor/*.cs의 [MenuItem] 정적 클래스로 이루어지며,
실제 Unity 프리팹 저장(디스크 반영)은 Unity 에디터 세션 내 메뉴 실행으로만 가능하다 (14-04-PLAN.md 선례 — 코드 작성은
auto, 실제 메뉴 실행/저장은 checkpoint:human-action으로 분리).
-->
</context>

<tasks>

<task type="auto" tdd="false">
  <name>Task 1: ExclamationIconBuilder 에디터 도구 작성</name>
  <files>Assets/Editor/ExclamationIconBuilder.cs</files>
  <action>
다음 정확한 내용으로 `Assets/Editor/ExclamationIconBuilder.cs`를 생성한다. 기존 `Assets/Editor/HitSparkBuilder.cs`/`ExitPortalBuilder.cs`와 동일한 `[MenuItem]` 정적 클래스 패턴을 따른다.

이 도구는 두 가지 일을 한다:
1. 신규 아트 없이 절차적으로 "!" 모양 텍스처(투명 배경 + 흰색 도형, 24x48px)를 생성해 `Assets/Sprites/UI/ExclamationMark.png`로 저장하고 Sprite로 임포트 설정한다 (흰색으로 만드는 이유: ExclamationIcon SpriteRenderer의 기존 `m_Color`가 이미 노란색으로 설정되어 있어 틴트가 자동 적용됨 — 색상 값 자체는 건드리지 않는다).
2. `MeleeEnemy.prefab`을 `PrefabUtility.LoadPrefabContents`로 열어 `ExclamationIcon` 자식의 `SpriteRenderer.sprite`에 생성된 스프라이트만 배정하고 저장한다. 그 외 어떤 컴포넌트/필드도 건드리지 않는다.

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;

/// &lt;summary&gt;
/// Menu: Fast/Quick/Build MeleeEnemy Exclamation Icon
/// MeleeEnemy.prefab의 ExclamationIcon(SpriteRenderer)에 스프라이트가 미할당(m_Sprite: {fileID: 0})되어
/// Telegraph 상태에서 "!" 아이콘이 보이지 않던 문제 수정.
/// 신규 아트 없이 절차적으로 "!" 모양 텍스처를 생성해 Sprite로 임포트하고 프리팹에 배정한다.
/// MeleeEnemy.cs의 Telegraph 로직(999.4-03)은 변경하지 않음 — 프리팹 애셋만 수정.
/// &lt;/summary&gt;
public static class ExclamationIconBuilder
{
    private const string SpritePath = "Assets/Sprites/UI/ExclamationMark.png";
    private const string PrefabPath = "Assets/Prefabs/Enemies/MeleeEnemy.prefab";
    private const int TexWidth  = 24;
    private const int TexHeight = 48;

    [MenuItem("Fast/Quick/Build MeleeEnemy Exclamation Icon")]
    public static void Run()
    {
        GenerateExclamationTexture();
        AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
        ConfigureSpriteImporter(SpritePath);

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null)
        {
            Debug.LogError($"[ExclamationIconBuilder] Sprite load failed at {SpritePath}");
            return;
        }

        AssignSpriteToPrefab(sprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ExclamationIconBuilder] ExclamationMark sprite generated and assigned to MeleeEnemy.prefab ExclamationIcon.");
    }

    private static void GenerateExclamationTexture()
    {
        string fullDir = Path.Combine(Application.dataPath, "Sprites", "UI");
        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);

        var tex = new Texture2D(TexWidth, TexHeight, TextureFormat.RGBA32, false);
        var clear = new Color32(0, 0, 0, 0);
        var white = new Color32(255, 255, 255, 255);
        var pixels = new Color32[TexWidth * TexHeight];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        // Stem (윗부분): x 8..15, y 14..47 — Texture2D는 y=0이 하단
        for (int y = 14; y < TexHeight; y++)
            for (int x = 8; x < 16; x++)
                pixels[y * TexWidth + x] = white;

        // Dot (아랫부분): x 7..15, y 0..8
        for (int y = 0; y < 9; y++)
            for (int x = 7; x < 16; x++)
                pixels[y * TexWidth + x] = white;

        tex.SetPixels32(pixels);
        tex.Apply();

        string fullPath = Path.Combine(fullDir, "ExclamationMark.png");
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();
    }

    private static void ConfigureSpriteImporter(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.filterMode          = FilterMode.Point;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 48f;
        importer.mipmapEnabled       = false;
        importer.SaveAndReimport();
    }

    private static void AssignSpriteToPrefab(Sprite sprite)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var iconTransform = root.transform.Find("ExclamationIcon");
        if (iconTransform == null)
        {
            Debug.LogError("[ExclamationIconBuilder] ExclamationIcon child not found in MeleeEnemy.prefab");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        var sr = iconTransform.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("[ExclamationIconBuilder] SpriteRenderer not found on ExclamationIcon child");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        sr.sprite = sprite;
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
```

`&lt;summary&gt;`/`&lt;/summary&gt;`는 실제 파일에는 일반 XML 문서 주석(`/// <summary>` / `/// </summary>`)으로 작성한다 (이 PLAN.md 안에서만 이스케이프 표기).

주의:
- `MeleeEnemy.cs`는 이 태스크에서 절대 수정하지 않는다.
- 다른 프리팹(RangedEnemy.prefab 등)은 건드리지 않는다.
- 색상은 프리팹의 기존 `m_Color`(노란색)를 그대로 사용 — 텍스처 자체는 흰색으로 생성한다.
  </action>
  <verify>
    <automated>grep -q "MenuItem(\"Fast/Quick/Build MeleeEnemy Exclamation Icon\")" Assets/Editor/ExclamationIconBuilder.cs && grep -q "PrefabUtility.SaveAsPrefabAsset" Assets/Editor/ExclamationIconBuilder.cs && grep -q "ExclamationIcon" Assets/Editor/ExclamationIconBuilder.cs && echo OK</automated>
  </verify>
  <done>Assets/Editor/ExclamationIconBuilder.cs가 생성되고, MenuItem 경로/PrefabUtility 저장 호출/ExclamationIcon 참조를 모두 포함한다. 아직 프리팹은 수정되지 않은 상태(에디터 실행 전).</done>
</task>

<task type="checkpoint:human-action" gate="blocking">
  <name>Task 2: Unity 에디터 — 도구 실행 + "!" 아이콘 표시 확인</name>
  <files>Assets/Sprites/UI/ExclamationMark.png, Assets/Prefabs/Enemies/MeleeEnemy.prefab</files>
  <read_first>
    - Assets/Editor/ExclamationIconBuilder.cs (Task 1에서 작성된 도구, 실행할 메뉴 경로 확인)
  </read_first>
  <action>
    Unity 에디터 내부 메뉴 실행 및 프리팹 디스크 저장은 자동화 불가 — 사용자에게 아래 절차를 안내하고 결과를 대기한다. 컴파일 에러가 보고되면 Task 1 코드의 문법/시그니처 문제를 수정 후 재확인을 요청한다.
  </action>
  <what-built>ExclamationIconBuilder.cs — "!" 텍스처 절차적 생성 + Sprite 임포트 설정 + MeleeEnemy.prefab ExclamationIcon 자동 배정 도구 (Task 1 완료, 아직 미실행)</what-built>
  <how-to-verify>
    Unity 에디터(6000.3.11f1)에서 프로젝트를 열고 순서대로:
    1. **컴파일 확인**: Console에 컴파일 에러 0건 (경고는 허용)
    2. **도구 실행**: 상단 메뉴 → Fast → Quick → Build MeleeEnemy Exclamation Icon 실행 → Console에 "[ExclamationIconBuilder] ExclamationMark sprite generated and assigned to MeleeEnemy.prefab ExclamationIcon." 로그 확인, 에러 0건
    3. **애셋 확인**: `Assets/Sprites/UI/ExclamationMark.png`가 생성되었고 Inspector에서 Sprite (2D and UI) 타입으로 임포트되어 있는지 확인
    4. **프리팹 확인**: `Assets/Prefabs/Enemies/MeleeEnemy.prefab` 선택 → Hierarchy에서 ExclamationIcon 자식 오브젝트 선택 → Inspector의 SpriteRenderer에 Sprite 필드가 "ExclamationMark"로 채워져 있고 색상은 기존 노란색 그대로인지 확인
    5. **플레이테스트**: Play 모드 진입 → MeleeEnemy에게 접근해 공격 범위 안으로 들어가 Telegraph 상태를 유도 → 적 머리 위에 노란색 "!" 아이콘이 실제로 나타났다가, 공격 전환 시 사라지는지 확인
    6. **회귀 확인**: MeleeEnemy의 이동/추격/공격 타이밍이 기존과 동일하게 동작하는지 확인 (999.4-03 로직 변경 없음)
  </how-to-verify>
  <verify>
    <automated>test -f "Assets/Sprites/UI/ExclamationMark.png" && grep -A 45 "&8043105589779039711" Assets/Prefabs/Enemies/MeleeEnemy.prefab | grep -q "m_WasSpriteAssigned: 1"</automated>
  </verify>
  <acceptance_criteria>
    - Console 컴파일 에러 0건, 도구 실행 로그 정상 출력
    - `Assets/Sprites/UI/ExclamationMark.png` 파일이 존재하고 Sprite로 임포트됨
    - `MeleeEnemy.prefab`의 ExclamationIcon SpriteRenderer에 스프라이트가 실제 배정됨 (`m_WasSpriteAssigned: 1`, `automated` grep 통과)
    - 플레이테스트에서 Telegraph 상태 진입 시 "!" 아이콘이 시각적으로 확인됨
    - 기존 MeleeEnemy 이동/공격 타이밍 회귀 없음
  </acceptance_criteria>
  <done>MeleeEnemy.prefab의 ExclamationIcon에 실제 스프라이트가 배정되어 디스크에 저장되고, 플레이테스트로 "!" 아이콘 표시가 시각적으로 확인됨.</done>
  <resume-signal>"approved" 입력 또는 문제 항목 설명 (예: "컴파일 에러 발생" 또는 "아이콘이 여전히 안 보임")</resume-signal>
</task>

</tasks>

<verification>
- Assets/Editor/ExclamationIconBuilder.cs 존재 + MenuItem/PrefabUtility.SaveAsPrefabAsset/ExclamationIcon 문자열 포함 (grep)
- Assets/Sprites/UI/ExclamationMark.png 존재
- Assets/Prefabs/Enemies/MeleeEnemy.prefab의 ExclamationIcon SpriteRenderer 블록에서 m_WasSpriteAssigned: 1 (grep)
- 플레이테스트: Telegraph 상태에서 "!" 아이콘 시각적 확인, MeleeEnemy.cs 로직 회귀 없음
</verification>

<success_criteria>
- MeleeEnemy Telegraph 상태에서 "!" 아이콘이 실제로 화면에 렌더링된다 (D-05 완성)
- MeleeEnemy.cs는 한 줄도 수정되지 않는다 (정밀 변경 원칙 준수)
- 재사용 가능한 에디터 도구(ExclamationIconBuilder.cs)가 남아 향후 다른 적 타입에도 동일 패턴 적용 가능
</success_criteria>

<output>
After completion, create `.planning/quick/260714-fnr-meleeenemy-prefab-exclamationicon-sprite/260714-fnr-SUMMARY.md`
</output>
