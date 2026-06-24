using UnityEngine;

/// <summary>
/// 사다리 오르내리기 메카닉.
/// "Ladder" 태그가 붙은 Is Trigger 콜라이더와 겹치면 활성화.
/// 수직 입력(Move.y)을 감지하면 그래비티를 끄고 속도를 수직으로만 제어한다.
///
/// 씬 설정:
///   사다리 오브젝트 → BoxCollider2D (Is Trigger = true) + Tag = "Ladder"
/// 플레이어 설정:
///   Player GameObject에 이 컴포넌트 추가.
///   PlayerController의 SetLadderMode()와 연동되므로 PlayerController 필수.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerController))]
public class LadderController : MonoBehaviour
{
    [SerializeField] private float climbSpeed = 5f;

    private Rigidbody2D      _rb;
    private PlayerController _player;
    private float            _originalGravityScale;
    private int              _ladderOverlapCount;
    private bool             _isClimbing;

    private void Awake()
    {
        _rb      = GetComponent<Rigidbody2D>();
        _player  = GetComponent<PlayerController>();
        _originalGravityScale = _rb.gravityScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder")) _ladderOverlapCount++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Ladder")) return;
        _ladderOverlapCount = Mathf.Max(0, _ladderOverlapCount - 1);
        if (_ladderOverlapCount == 0) ExitLadder();
    }

    private void FixedUpdate()
    {
        if (_ladderOverlapCount == 0 || _player.InputLocked) return;

        float vertical = InputManager.Instance.MoveInput.y;

        // 사다리 영역 안에서 수직 입력이 있을 때 처음으로 등반 시작
        if (!_isClimbing && Mathf.Abs(vertical) > 0.1f)
            EnterLadder();

        if (_isClimbing)
        {
            // 슬로우모션 timeScale 보정 — RollController 패턴과 동일
            float speed = climbSpeed * (1f / Time.timeScale);
            _rb.linearVelocity = new Vector2(0f, vertical * speed);
        }
    }

    private void EnterLadder()
    {
        _isClimbing = true;
        _rb.gravityScale = 0f;
        _player.SetLadderMode(true);
    }

    /// <summary>
    /// 사다리 이탈 시 중력 복구. PlayerController.OnJumpPerformed에서도 호출.
    /// </summary>
    public void ExitLadder()
    {
        if (!_isClimbing) return;
        _isClimbing = false;
        _rb.gravityScale = _originalGravityScale;
        _player.SetLadderMode(false);
    }
}
