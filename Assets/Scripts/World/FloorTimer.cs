using UnityEngine;

/// <summary>
/// Phase 11 — 층 제한시간 정적 클래스. FloorManager/ScoreManager 패턴 준수 (data-only, no scene lifecycle).
/// D-01: 모든 층 60초 고정. D-07: Time.unscaledTime 기반 — 슬로우모션(Time.timeScale) 면역.
/// D-06/TIMER-02: RemainingSeconds가 0에 도달하면 Tick()이 1회만 PlayerController.OnPlayerDeath를 발동한다.
/// </summary>
public static class FloorTimer
{
    public const float Duration = 60f; // D-01: 층당 고정 제한시간(초)

    private static float _floorStartTime;
    private static bool  _expired;

    /// <summary>층 진입마다 호출 — 60초로 리셋한다 (D-08). WorldGenerator.Start()/FloorTransitionSequence()에서 호출.</summary>
    public static void Reset()
    {
        _floorStartTime = Time.unscaledTime;
        _expired = false;
    }

    /// <summary>남은 시간(초). Time.unscaledTime 기반 — 슬로우모션 중에도 실시간으로 감소한다.</summary>
    public static float RemainingSeconds => Mathf.Max(0f, Duration - (Time.unscaledTime - _floorStartTime));

    /// <summary>
    /// 매 프레임 호출 — 남은 시간이 0에 도달하면 1회만 PlayerController.OnPlayerDeath를 발동한다 (D-06, TIMER-02).
    /// WorldGenerator.Update()에서 호출 (Plan 02).
    /// </summary>
    public static void Tick()
    {
        if (_expired) return;
        if (RemainingSeconds <= 0f)
        {
            _expired = true;
            PlayerController.TriggerDeath();
        }
    }
}
