using UnityEngine;

/// <summary>
/// D-01/D-09 -- SpriteMask 런타임 생성 유틸리티. FloorTransitionEffect(D-01)와
/// EnemyDeathEffect(D-09, Plan 12-08)가 공유하는 공용 정적 클래스. 캐시를 통해
/// 반복 호출 시 Texture2D/Sprite 재생성을 막아 모바일 GC 압박을 줄인다.
/// </summary>
public static class RuntimeMaskSprite
{
    private static Sprite _cached;

    public static Sprite CreateMaskSprite()
    {
        if (_cached != null) return _cached;

        var tex = new Texture2D(4, 4);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _cached = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _cached;
    }
}
