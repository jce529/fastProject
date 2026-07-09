using UnityEngine;

public enum Sfx
{
    PortalEnter,      // D-06: 상승 워프음
    PortalExit,       // D-06: 하강 마무리음
    Slash,            // D-05: 대시 처치 슬래시
    EnemyDeathGlitch, // D-05: 사망 글리치 노이즈
}

/// <summary>
/// SFX-01: 프로젝트 유일 오디오 재생 진입점. MonoBehaviour 싱글턴 + 2채널 AudioSource 풀 (D-00a/D-07).
/// - 액션 채널(8보이스): timeScale 완전 독립 — Unity DSP 클럭은 timeScale 영향을 받지 않으므로 추가 코드 없음.
/// - 배경 채널(2보이스): LateUpdate에서 pitch = Time.timeScale 추종 (슬로우모션 피치다운 인프라, 이번 페이즈 소비 클립 없음 — D-08).
/// GameBootstrapper와 동일한 RuntimeInitializeOnLoadMethod 부트스트랩 — 씬 수정 0회로 3개 씬 전부에서 생존.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Channel Pools (D-07)")]
    [SerializeField] private int _actionVoices = 8;
    [SerializeField] private int _ambientVoices = 2;
    [SerializeField, Range(0.05f, 1f)] private float _minAmbientPitch = 0.3f; // Pitfall 1: pitch=0 정지 방지

    [Header("Clips — 플레이테스트 중 Inspector에서 교체 (D-03)")]
    [SerializeField] private AudioClip _portalEnter;
    [SerializeField] private AudioClip _portalExit;
    [SerializeField] private AudioClip _slash;
    [SerializeField] private AudioClip _enemyDeathGlitch;

    private AudioSource[] _actionPool;
    private AudioSource[] _ambientPool;
    private int _actionCursor;
    private AudioClip _lastClip;
    private double _lastClipDspTime; // double 필수 — dspTime은 누적 증가

    // GameBootstrapper.cs와 동일 패턴 — 씬 수정 없이 3개 씬 전부에서 생존 보장
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab == null) { Debug.LogError("[Audio] Resources/AudioManager.prefab 없음"); return; }
        DontDestroyOnLoad(Instantiate(prefab.gameObject));
    }

    private void Awake()
    {
        // InputManager.cs:11 기존 중복 가드 패턴 (Pitfall 4: DontDestroyOnLoad 중복 인스턴스)
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _actionPool  = CreatePool("Action", _actionVoices);
        _ambientPool = CreatePool("Ambient", _ambientVoices);
    }

    private AudioSource[] CreatePool(string label, int size)
    {
        var pool = new AudioSource[size];
        for (int i = 0; i < size; i++)
        {
            var child = new GameObject($"{label}Voice{i}");
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.spatialBlend = 0f; // 2D — 리스너 위치 무관
            pool[i] = src;
        }
        return pool;
    }

    private void LateUpdate()
    {
        // D-07 배경 채널: timeScale 추종 피치다운. 액션 채널은 절대 건드리지 않음.
        // AudioSource DSP 재생은 timeScale 독립이므로 액션 채널 코드는 0줄 (RESEARCH.md 검증).
        float pitch = Mathf.Max(_minAmbientPitch, Time.timeScale);
        for (int i = 0; i < _ambientPool.Length; i++)
            _ambientPool[i].pitch = pitch;
    }

    /// <summary>모든 연출 훅의 단일 진입점. null 안전 — 부트스트랩 실패 시 조용히 무시.</summary>
    public static void PlaySfx(Sfx id, float volume = 1f) => Instance?.PlayInternal(id, volume);

    private void PlayInternal(Sfx id, float volume)
    {
        AudioClip clip = id switch
        {
            Sfx.PortalEnter      => _portalEnter,
            Sfx.PortalExit       => _portalExit,
            Sfx.Slash            => _slash,
            Sfx.EnemyDeathGlitch => _enemyDeathGlitch,
            _ => null,
        };
        if (clip == null) return;

        // Pitfall 3: 동일 클립 30ms 내 재트리거 스킵 (연속 처치 위상 중첩 클리핑 방지)
        if (clip == _lastClip && AudioSettings.dspTime - _lastClipDspTime < 0.03) return;
        _lastClip = clip;
        _lastClipDspTime = AudioSettings.dspTime;

        var src = _actionPool[_actionCursor];
        _actionCursor = (_actionCursor + 1) % _actionPool.Length;
        src.pitch = Random.Range(0.95f, 1.05f); // 반복 재생 기계음 방지 — timeScale 연동 아님
        src.PlayOneShot(clip, volume);
    }
}
