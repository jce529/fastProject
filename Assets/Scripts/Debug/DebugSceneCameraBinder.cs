using UnityEngine;

/// <summary>
/// DebugScene(Assets/Scenes/DebugScene.unity) 전용 최소 바인더.
/// WorldGenerator/DebugRoomTeleporter가 이미 쓰는 CameraFollow.SnapToRoom(Bounds) 배선 패턴
/// (CameraBound가 있으면 GetWorldBounds(), 없으면 폴백 위치로 스냅)을 DebugScene에도 그대로 적용한다.
/// DebugScene은 WorldGenerator/DebugRoomTeleporter를 전혀 거치지 않는 고정좌표 격리 씬이라
/// Start() 1회 호출로 카메라 바운드를 연결해줄 존재가 없었다 — 이 스크립트가 그 역할만 한다.
/// SampleScene/WorldGenerator/DebugRoomTeleporter의 기존 동작에는 전혀 영향을 주지 않는다.
/// </summary>
public class DebugSceneCameraBinder : MonoBehaviour
{
    [SerializeField] private CameraFollow _cameraFollow;
    [SerializeField] private Transform    _roomRoot;       // Room 인스턴스 루트 — CameraBound 자식 탐색용
    [SerializeField] private Transform    _fallbackTarget;  // CameraBound 없을 시 폴백 스냅 위치

    private void Start()
    {
        if (_cameraFollow == null) return;

        CameraBound bound = _roomRoot != null ? _roomRoot.GetComponentInChildren<CameraBound>(true) : null;
        if (bound != null)
            _cameraFollow.SnapToRoom(bound.GetWorldBounds());
        else if (_fallbackTarget != null)
            _cameraFollow.SnapToRoom(_fallbackTarget.position);
    }
}
