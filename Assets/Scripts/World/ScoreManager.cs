using UnityEngine;

/// <summary>
/// 점수 관리 정적 클래스. FloorManager 패턴 준수 (data-only, no scene lifecycle).
/// 적 처치 시 +100점, 방 클리어 속도에 따른 보너스 (+300/+150/+50).
/// 타이머: Time.unscaledTime 기반 — timeScale 변화(슬로우모션, 히트프리즈)에 면역.
/// </summary>
public static class ScoreManager
{
    // -- 보너스 기준 (Inspector 없음 — 프로토타입 상수로 충분) --------------------
    public const int   KillScore        = 100;
    public const int   FastClearBonus   = 300;
    public const int   NormalClearBonus = 150;
    public const int   SlowClearBonus   = 50;
    public const float FastClearTime    = 10f; // 초 이내 클리어 시 FastClearBonus
    public const float NormalClearTime  = 25f; // 초 이내 클리어 시 NormalClearBonus
    public const int   TimeBonusPerSecond = 10; // D-02(Phase 11): 남은 초 × 10점

    // -- 상태 -----------------------------------------------------------------------
    public static int Score { get; private set; }
    private static float _roomStartTime;

    // -- Public API ----------------------------------------------------------------

    /// <summary>씬 재로드 없이 점수 초기화. DeathScreenController.RestartGame()에서 호출.</summary>
    public static void Reset()
    {
        Score = 0;
        _roomStartTime = Time.unscaledTime;
    }

    /// <summary>CombatController.ExecuteDash() — target.OnDashHit() 직후 호출.</summary>
    public static void AddKillScore()
    {
        Score += KillScore;
    }

    /// <summary>
    /// SCORE-01(Phase 11): EXIT 포탈 진입 순간 남은 제한시간(초)에 비례한 점수를 더한다 (D-02).
    /// WorldGenerator.FloorTransitionSequence() 시작 시점(D-02b)에서 FloorTimer.RemainingSeconds를 인자로 호출.
    /// </summary>
    public static void AddTimeBonus(float remainingSeconds)
    {
        Score += Mathf.RoundToInt(remainingSeconds) * TimeBonusPerSecond;
    }

    /// <summary>방 입장 타이머 시작. FloorSpawner.ActivateEnemies() 완료 직후 호출.</summary>
    public static void StartRoomTimer()
    {
        _roomStartTime = Time.unscaledTime;
    }

    /// <summary>
    /// 방 클리어 보너스 계산 및 추가. RoomExit.OnTriggerEnter2D()에서 AdvanceFloor() 직전 호출.
    /// elapsed = unscaledTime 기반 — 슬로우모션 중 출구 진입 시에도 실제 경과 시간으로 계산.
    /// </summary>
    public static void AddRoomClearBonus()
    {
        float elapsed = Time.unscaledTime - _roomStartTime;
        if      (elapsed <= FastClearTime)   Score += FastClearBonus;
        else if (elapsed <= NormalClearTime) Score += NormalClearBonus;
        else                                 Score += SlowClearBonus;
    }
}
