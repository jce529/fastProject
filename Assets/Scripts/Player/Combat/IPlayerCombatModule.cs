using System.Collections;

public interface IPlayerCombatModule
{
    IEnemy FindTarget(UnityEngine.Vector2 origin, CombatContext ctx);
    IEnumerator Resolve(IEnemy target, CombatContext ctx);
    IEnumerator Whiff(CombatContext ctx);
}
