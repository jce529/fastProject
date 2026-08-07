using UnityEngine;

/// <summary>
/// D-04/D-06: 패링 가능한 오브젝트(SAMURAI의 패링 전용 타이밍 투사체)의 side-channel 계약.
/// IEnemy를 4번째 멤버로 확장하지 않는다(ARCHITECTURE.md Anti-Pattern 2, 19-CONTEXT.md canonical_refs).
/// </summary>
public interface IParryable
{
    Vector2 Position { get; }

    /// <summary>패링 성공 시 호출된다. reflectDirection은 플레이어의 조준 방향(D-06: 반사 방향).</summary>
    void OnParried(Vector2 reflectDirection);
}
