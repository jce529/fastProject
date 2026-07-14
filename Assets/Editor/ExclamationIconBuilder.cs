using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Quick/Build MeleeEnemy Exclamation Icon
/// MeleeEnemy.prefab의 ExclamationIcon(SpriteRenderer)에 스프라이트가 미할당(m_Sprite: {fileID: 0})되어
/// Telegraph 상태에서 "!" 아이콘이 보이지 않던 문제 수정.
/// 신규 아트 없이 절차적으로 "!" 모양 텍스처를 생성해 Sprite로 임포트하고 프리팹에 배정한다.
/// MeleeEnemy.cs의 Telegraph 로직(999.4-03)은 변경하지 않음 — 프리팹 애셋만 수정.
/// </summary>
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
