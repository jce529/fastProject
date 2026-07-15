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
    public const int   BossKillScore    = 750; // D-09: KillScore(100) 대비 유의미하게 큰 값, 500~1000 권장 범위 내
    public const int   RespawnKillScore = 30;   // D-10(999.2): 리스폰 킬 감소 점수 — KillScore의 약 30%. 파밍을 점수 관점에서 비효율적으로 만들되 완전히 막지는 않는다(D-09: 무제한 리스폰과 일관).
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

    /// <summary>CombatController.ExecuteDash() — target.OnDashHit() 직후 호출.
    /// D-10(999.2): isRespawn=true면 RespawnKillScore(감소된 점수)를, 기본값 false면 기존 KillScore를 더한다.
    /// 기본 매개변수로 인자 없는 기존 호출 형태(AddKillScore())도 그대로 컴파일된다.</summary>
    public static void AddKillScore(bool isRespawn = false)
    {
        Score += isRespawn ? RespawnKillScore : KillScore;
    }

    /// <summary>BOSS-06/D-09: 보스 처치(7회째 피격) 순간 CombatController와 무관하게 BossEnemy.Die()가 직접 호출한다.</summary>
    public static void AddBossKillScore()
    {
        Score += BossKillScore;
    }

    /// <summary>
    /// D-12: BossEnemy.OnDashHit()의 비치명타(1~6회)가 CombatController.ExecuteDash()의
    /// 무조건적인 AddKillScore(false) 호출(+100)을 즉시 상쇄하기 위해 사용한다.
    /// CombatController/IEnemy 계약은 변경하지 않는다 — BossEnemy 내부에서만 소비.
    /// </summary>
    public static void SubtractScore(int amount)
    {
        Score -= amount;
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
