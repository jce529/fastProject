using UnityEngine;

/// <summary>
/// D-01: 마우스→월드 방향 계산 공용 헬퍼. TapSwingCombatModuleBase(기본전투모듈/사무라이 전투형 모듈)가
/// 사용한다. OverclockModule.cs의 기존 private GetMouseWorldDirection과 동일한 계산이지만 별도
/// 파일로 존재한다 — 이미 실플레이 검증된 Overclock 경로(INFRA-01)를 리팩토링 대상에서 제외하기 위함.
/// </summary>
public static class AimUtil
{
    public static Vector2 GetMouseWorldDirection(Vector2 origin, Camera mainCamera)
    {
        UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
        Vector2 mousePos = mouse != null ? mouse.position.ReadValue() : (Vector2)mainCamera.WorldToScreenPoint(origin);
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(mainCamera.transform.position.z)));
        Vector2 dir = (Vector2)mouseWorld - origin;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
    }
}
