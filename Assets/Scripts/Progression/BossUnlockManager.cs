using System.Collections.Generic;
using UnityEngine;

public static class BossUnlockManager
{
    private const string PrefsKeyPrefix = "boss_unlock_";
    private static readonly Dictionary<string, bool> _cache = new Dictionary<string, bool>();

    public static bool IsUnlocked(string bossId)
    {
        if (_cache.TryGetValue(bossId, out var cached)) return cached;
        bool value = PlayerPrefs.GetInt(PrefsKeyPrefix + bossId, 0) == 1;
        _cache[bossId] = value;
        return value;
    }

    public static void Unlock(string bossId)
    {
        PlayerPrefs.SetInt(PrefsKeyPrefix + bossId, 1);
        PlayerPrefs.Save();
        _cache[bossId] = true;
    }
}
