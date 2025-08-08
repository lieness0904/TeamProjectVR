using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(RiggingManager))]
public class PlayerFishingController : NetworkBehaviour
{
    #region 변수 선언
    [Header("프리팹 연결")]
    [SerializeField] private NetworkObject fishingRodPrefab;
    [SerializeField] private NetworkObject bobberPrefab;

    [Header("컴포넌트 연결")]
    [SerializeField] private CastingHandler castingHandler;

    [Header("진동 설정")]
    [SerializeField] private float vibrationAmplitude = 0.7f;
    [SerializeField] private float vibrationDuration = 1.0f;

    [Header("릴 설정")]
    [Tooltip("찌가 이 거리 안으로 들어오면 회수된 것으로 간주합니다.")]
    [SerializeField] private float retrievalDistance = 1.5f;

    [Tooltip("찌를 1초에 감아들이는 거리(미터)입니다.")]
    public float fixedReelInSpeed = 2f;

    [Header("낚시 디테일 설정")]
    [Tooltip("낚싯대에 찌가 매달릴 때의 기본 거리입니다.")]
    [SerializeField] private float bobberHangOffset = 0.2f;

    [Header("챔질(Hooking) 설정")]
    [Tooltip("이 속도를 넘는 컨트롤러 움직임을 챔질로 인식합니다.")]
    [SerializeField] private float hookVelocityThreshold = 3.0f;

    [Header("게이지 설정")]
    [SerializeField] private float gaugeDecreasePerSec = 50f; // 초당 자연 감소량

    [Header("잡은 물고기 설정")]
    [Tooltip("잡은 후 보여줄 때 물고기가 천천히 회전하는 속도")]
    [SerializeField] private float caughtFishRotationSpeed = 50f;

    [Tooltip("물고기가 방향을 전환하는 속도입니다.")]
    [SerializeField] private float fishRotationSpeed = 5f;

    [Header("낚시 실패 조건")]
    [Tooltip("물고기가 이 거리 이상 멀어지면 낚시에 실패합니다.")]
    [SerializeField] private float maxFishDistance = 30f;

    [Header("사운드 설정")]
    [Tooltip("캐스팅 시 재생할 사운드")]
    public AudioClip castingSound;
    [Tooltip("플레이어가 압도적으로 이길 때 (netSpeed >= 2)")]
    public AudioClip reelingSoundWinningBig;
    [Tooltip("플레이어가 조금 이길 때 (0 < netSpeed < 2)")]
    public AudioClip reelingSoundWinningSmall;
    [Tooltip("플레이어가 지거나 비길 때 (netSpeed <= 0)")]
    public AudioClip reelingSoundLosing;
    [Tooltip("게이지 장력에 따라 재생 속도가 변할 릴링 사운드")]
    public AudioClip tensionReelingSound;

    [Tooltip("낚싯줄이 끊어질 때 재생할 사운드")]
    public AudioClip lineSnapSound;

    [Tooltip("찌를 회수했을 때 재생할 사운드")] 
    public AudioClip retrievalSound;            

    [Header("게이지 진동 설정")]
    [Tooltip("왼손 컨트롤러의 진동 세기 (낮음)")]
    [Range(0, 1)] public float lowTensionVibeAmplitude = 0.2f;
    [Tooltip("왼손 컨트롤러의 진동 세기 (중간)")]
    [Range(0, 1)] public float midTensionVibeAmplitude = 0.4f;
    [Tooltip("왼손 컨트롤러의 진동 세기 (높음)")]
    [Range(0, 1)] public float highTensionVibeAmplitude = 0.7f;
    [Tooltip("진동 시간")]
    [Range(0, 1)] public float vibeDuration = 0.1f;

    // --- 네트워크 동기화 변수 ---
    [Networked] private float tensionGauge { get; set; } = 0f;
    private const float maxGauge = 100f;
    [Networked] public NetworkBool IsReeling { get; set; }

    [Networked] public NetworkBool IsFishing { get; set; }
    [Networked] private NetworkObject SpawnedRod { get; set; }
    [Networked] public NetworkObject CurrentBobber { get; private set; }
    [Networked] private TickTimer CastingCooldown { get; set; }

    [Networked] public NetworkBool IsFighting { get; set; }
    [Networked] public NetworkObject HookedFish { get; set; }

    [Networked] private Vector3 FleeDirection { get; set; }
    [Networked] private TickTimer FleeStateTimer { get; set; }
    [Networked] private NetworkBool IsCurrentlyFleeing { get; set; }

    [Networked] private ReelingSoundState currentReelingSoundState { get; set; } = ReelingSoundState.None;
    [Networked] private TickTimer DistanceDisplayDelayTimer { get; set; }

    // --- 로컬 변수 (클라이언트측에서만 사용) ---
    private bool _missWindowActive = false;
    private float _missWindowEndTime = 0f;
    private RiggingManager _riggingManager;
    public static PlayerFishingController Local { get; private set; }
    private XRBaseController _rightHandXRController;
    private bool _fishJustBit = false;
    private Transform _rodTip;
    private Rigidbody _rodTipRb;
    private Vector3 _lastControllerPos;
    private Vector3 _currentControllerVel;
    private XRBaseController _leftHandXRController;
    private float _hapticTimer = 0f;
    private AudioSource sfxAudioSource; // 캐스팅 같은 단발성 효과음 용
    private AudioSource reelingAudioSource; // 릴링 같이 반복되는 소리 용
    private AudioSource tensionAudioSource;  // 게이지 장력 사운드 용
    private enum ReelingSoundState { None, WinningBig, WinningSmall, Losing }
    #endregion

    private void Awake()
    {
        _riggingManager = GetComponent<RiggingManager>();
        if (_riggingManager != null)
        {
            if (_riggingManager.rightHandController != null)
            {
                _rightHandXRController = _riggingManager.rightHandController.GetComponent<XRBaseController>();
            }
            if (_riggingManager.leftHandController != null)
            {
                _leftHandXRController = _riggingManager.leftHandController.GetComponent<XRBaseController>();
            }
            else
            {
                Debug.LogWarning("RiggingManager에 leftHandController가 연결되지 않아 릴링 진동이 작동하지 않을 수 있습니다.");
            }
        }

        
        InitializeAudioSources();
    }


    private void InitializeAudioSources()
    {
        // AudioSource가 3개 필요하므로, 없으면 추가해줍니다.
        while (GetComponents<AudioSource>().Length < 3)
        {
            gameObject.AddComponent<AudioSource>();
        }

        AudioSource[] sources = GetComponents<AudioSource>();

        // 1. 단발 효과음용 AudioSource 설정
        sfxAudioSource = sources[0];
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;

        // 2. 힘겨루기 릴링 사운드용 AudioSource 설정
        reelingAudioSource = sources[1];
        reelingAudioSource.playOnAwake = false;
        reelingAudioSource.loop = true; // 반복 재생

        // 3. 게이지 장력 사운드용 AudioSource 설정
        tensionAudioSource = sources[2];
        tensionAudioSource.playOnAwake = false;
        tensionAudioSource.loop = true; // 반복 재생
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        // 1. 오른쪽 컨트롤러의 현재 속도를 계속 계산합니다. (챔질 판정용)
        if (_riggingManager.rightHandController != null)
        {
            Vector3 currentPos = _riggingManager.rightHandController.position;
            if (Time.deltaTime > 0)
            {
                _currentControllerVel = (currentPos - _lastControllerPos) / Time.deltaTime;
            }
            _lastControllerPos = currentPos;
        }
        
        BobberController bobber = null;
        if (CurrentBobber != null)
        {
            CurrentBobber.TryGetComponent<BobberController>(out bobber);
        }
        
        if (bobber != null)
        {
            // 2. MISS 판정 로직
            if (_missWindowActive && Time.time <= _missWindowEndTime)
            {
                if (!bobber.HasFishOn && !IsFighting)
                {
                    if (_currentControllerVel.y > hookVelocityThreshold)
                    {
                        bobber.ShowMessageText("MISS", 1.2f);
                        _missWindowActive = false;
                    }
                }
            }

            // 3. 챔질(HIT) 판정 로직
            if (bobber.HasFishOn && !IsFighting)
            {
                if (_currentControllerVel.y > hookVelocityThreshold)
                {
                    if (bobber.HookedFish != null)
                    {
                        RPC_AttemptHook(bobber.HookedFish);
                    }
                }
            }
        }

        if (_missWindowActive && Time.time > _missWindowEndTime)
            _missWindowActive = false;

        // 4. 릴링 진동 처리 함수 호출
        HandleReelingHaptics();
        HandleReelingSounds();

        // 5. 전투 중 찌와의 남은 거리 표시
        if (IsFighting && DistanceDisplayDelayTimer.ExpiredOrNotRunning(Runner))
        {
            if (_rodTip != null && bobber != null)
            {
                float distance = Vector3.Distance(_rodTip.position, CurrentBobber.transform.position);
                bobber.UpdateDistanceText($"{distance:F1}m");
            }
        }

        HandleFishRotation();

        // 잡힌 물고기 회전 로직
        if (HasInputAuthority && HookedFish != null && !IsFighting)
        {
            var rodLine = SpawnedRod?.GetComponentInChildren<RodLineController>();
            var hookTransform = rodLine?.GetCurrentHookTransform();

            if (hookTransform != null)
            {
                hookTransform.Rotate(Vector3.up, caughtFishRotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }

    private void HandleReelingHaptics()
    {
        if (!IsFighting || !IsReeling) return;
        float normalizedGauge = tensionGauge / maxGauge;
        _hapticTimer -= Time.deltaTime;
        if (normalizedGauge >= 0.8f)
        {
            SendDualHapticImpulse(highTensionVibeAmplitude, vibeDuration);
        }
        else if (normalizedGauge >= 0.5f)
        {
            if (_hapticTimer <= 0f)
            {
                SendDualHapticImpulse(midTensionVibeAmplitude, vibeDuration);
                _hapticTimer = 0.5f;
            }
        }
        else
        {
            if (_hapticTimer <= 0f)
            {
                SendDualHapticImpulse(lowTensionVibeAmplitude, vibeDuration);
                _hapticTimer = 1.0f;
            }
        }
    }
    // --- [이 함수 전체를 클래스 내부에 추가하세요] ---
    private void HandleReelingSounds()
    {
        // 입력 권한이 없거나, 오디오 소스들이 준비 안됐으면 실행하지 않음
        if (!HasInputAuthority || reelingAudioSource == null || tensionAudioSource == null) return;

        // 전투 중일 때만 모든 사운드 로직을 실행
        if (IsFighting)
        {
            // --- 1. 힘겨루기 사운드 로직 (기존 로직) ---
            AudioClip clipToPlay = null;
            switch (currentReelingSoundState)
            {
                case ReelingSoundState.WinningBig:
                    clipToPlay = reelingSoundWinningBig;
                    break;
                case ReelingSoundState.WinningSmall:
                    clipToPlay = reelingSoundWinningSmall;
                    break;
                case ReelingSoundState.Losing:
                    clipToPlay = reelingSoundLosing;
                    break;
                case ReelingSoundState.None:
                default:
                    clipToPlay = null;
                    break;
            }

            if (reelingAudioSource.clip != clipToPlay || !reelingAudioSource.isPlaying)
            {
                reelingAudioSource.clip = clipToPlay;
                if (clipToPlay != null)
                {
                    reelingAudioSource.Play();
                }
                else
                {
                    reelingAudioSource.Stop();
                }
            }

            // --- 2. 게이지 장력 사운드 로직 (pitch 변경) ---
            float normalizedGauge = tensionGauge / maxGauge;

            // 게이지가 50% 이상이고, 릴을 감고 있을 때만 재생
            if (normalizedGauge >= 0.5f && IsReeling)
            {
                if (tensionAudioSource.clip != tensionReelingSound)
                {
                    tensionAudioSource.clip = tensionReelingSound;
                }

                float progress = (normalizedGauge - 0.5f) / 0.5f;
                tensionAudioSource.pitch = 1.0f + progress;

                if (!tensionAudioSource.isPlaying && tensionReelingSound != null)
                {
                    tensionAudioSource.Play();
                }
            }
            else
            {
                if (tensionAudioSource.isPlaying)
                {
                    tensionAudioSource.Stop();
                }
                tensionAudioSource.pitch = 1.0f;
            }
        }
        else
        {
            // 전투 중이 아니면 모든 릴링 관련 사운드를 끔
            if (reelingAudioSource.isPlaying)
            {
                reelingAudioSource.Stop();
            }
            if (tensionAudioSource.isPlaying)
            {
                tensionAudioSource.Stop();
                tensionAudioSource.pitch = 1.0f;
            }
        }
    }

    private void SendDualHapticImpulse(float amplitude, float duration)
    {
        if (_leftHandXRController != null) _leftHandXRController.SendHapticImpulse(amplitude, duration);
        if (_rightHandXRController != null) _rightHandXRController.SendHapticImpulse(amplitude, duration);
    }

    #region Fusion 콜백 함수
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            Local = this;
            if (_riggingManager.rightHandController != null)
                _lastControllerPos = _riggingManager.rightHandController.position;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this) Local = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // --- [새로 추가] 물고기와 싸우지 않을 때의 릴링 처리 ---
            if (IsReeling && !IsFighting && CurrentBobber != null)
            {
                // 1. 기본 속도로 찌와 바늘을 움직입니다.
                float moveAmount = fixedReelInSpeed * Runner.DeltaTime;
                if (CurrentBobber.TryGetComponent<Rigidbody>(out var bobberRigidbody) && _rodTip != null)
                {
                    Vector3 directionToRod = (_rodTip.position - bobberRigidbody.position).normalized;
                    bobberRigidbody.MovePosition(bobberRigidbody.position + directionToRod * moveAmount);

                    var rodLine = SpawnedRod?.GetComponentInChildren<RodLineController>();
                    var hookTransform = rodLine?.GetCurrentHookTransform();
                    if (hookTransform != null && hookTransform.TryGetComponent<Rigidbody>(out var hookRigidbody))
                    {
                        hookRigidbody.MovePosition(hookRigidbody.position + directionToRod * moveAmount);
                    }
                }

                // 2. 찌 회수 여부를 체크합니다.
                CheckAndHandleRetrieval();
            }

            // --- 물고기와 싸울 때의 로직 ---
            if (IsFighting)
            {
                // 1. 물고기 상태 결정 (도망 or 기절)
                if (FleeStateTimer.Expired(Runner))
                {
                    IsCurrentlyFleeing = !IsCurrentlyFleeing;
                    if (IsCurrentlyFleeing)
                    {
                        FleeStateTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(3f, 6f));
                        Vector3 awayDirection = (CurrentBobber.transform.position - transform.position);
                        awayDirection.y = 0;
                        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-45f, 45f), 0);
                        FleeDirection = randomRotation * awayDirection.normalized;
                    }
                    else
                    {
                        FleeStateTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(3f, 5f));
                    }
                }

                // 2. 힘겨루기 및 찌 움직임 계산
                var fishData = HookedFish.GetComponent<FishData>();
                if (fishData != null && CurrentBobber.TryGetComponent<Rigidbody>(out var bobberRigidbody))
                {
                    // 플레이어가 릴을 감고 있을 때
                    if (IsReeling)
                    {
                        float fishResistance = fishData.finalWeight * (2f / 3f);
                        if (IsCurrentlyFleeing)
                        {
                            fishResistance += fishData.strength;
                        }
                        float netSpeed = fixedReelInSpeed - fishResistance;
                        float moveAmount = netSpeed * Runner.DeltaTime;

                        // [수정] 다시 '찌'를 움직입니다.
                        if (_rodTip != null)
                        {
                            Vector3 directionToRod = (_rodTip.position - bobberRigidbody.position).normalized;
                            bobberRigidbody.MovePosition(bobberRigidbody.position + directionToRod * moveAmount);
                        }

                        if (IsCurrentlyFleeing)
                        {
                            float increasePerSecond = 10f * fishData.finalWeight;
                            tensionGauge = Mathf.Min(tensionGauge + increasePerSecond * Runner.DeltaTime, maxGauge);
                        }
                        else
                        {
                            tensionGauge = Mathf.Max(tensionGauge - gaugeDecreasePerSec * Runner.DeltaTime, 0f);
                        }

                        CheckAndHandleRetrieval();
                    }
                    else // 플레이어가 릴을 감고 있지 않을 때
                    {
                        tensionGauge = Mathf.Max(tensionGauge - gaugeDecreasePerSec * Runner.DeltaTime, 0f);

                        if (IsCurrentlyFleeing)
                        {
                            // [수정] 다시 '찌'를 움직입니다.
                            float fleeDistance = (fishData.strength * 2f) * Runner.DeltaTime; // 도망가는 힘 조절 (필요시 이 값을 변경)
                            bobberRigidbody.MovePosition(bobberRigidbody.position + FleeDirection * fleeDistance);
                        }
                    }
                }

                // --- [새로 추가된 사운드 상태 결정 로직] ---
                if (IsReeling && HookedFish != null)
                {
                    // fishData는 위에서 이미 찾아놨으므로 재사용합니다.
                    if (fishData != null)
                    {
                        float fishResistance = fishData.finalWeight * (2f / 3f);
                        if (IsCurrentlyFleeing)
                        {
                            fishResistance += fishData.strength;
                        }
                        float netSpeed = fixedReelInSpeed - fishResistance;

                        if (netSpeed >= 2f)
                        {
                            currentReelingSoundState = ReelingSoundState.WinningBig;
                        }
                        else if (netSpeed > 0f)
                        {
                            currentReelingSoundState = ReelingSoundState.WinningSmall;
                        }
                        else
                        {
                            currentReelingSoundState = ReelingSoundState.Losing;
                        }
                    }
                }
                else
                {
                    // 릴을 감고 있지 않으면 사운드 상태를 None으로 변경
                    currentReelingSoundState = ReelingSoundState.None;
                }
                // --- [여기까지] ---
            }
            else // IsFighting이 아닐 때
            {
                // 싸우고 있지 않을 때도 사운드 상태를 None으로 확실하게 변경
                currentReelingSoundState = ReelingSoundState.None;
            }

            // --- 낚시 실패 조건 체크 ---
            if (tensionGauge >= maxGauge && IsFighting)
            {
                RPC_OnFishingFailed();
            }
            if (CurrentBobber != null && IsFighting)
            {
                float distanceToBobber = Vector3.Distance(transform.position, CurrentBobber.transform.position);
                if (distanceToBobber > maxFishDistance)
                {
                    RPC_OnFishingFailed();
                }
            }
        }

        // --- 캐스팅 입력 처리 ---
        if (HasInputAuthority && castingHandler != null && castingHandler.castAction.action.WasReleasedThisFrame())
        {
            if (CastingCooldown.ExpiredOrNotRunning(Runner))
            {
                if (!IsFighting && HookedFish == null && CurrentBobber != null && CurrentBobber.GetComponent<ConfigurableJoint>() != null)
                {
                    if (sfxAudioSource != null && castingSound != null)
                    {
                        sfxAudioSource.PlayOneShot(castingSound);
                    }
                    RPC_CastBobber(castingHandler.LastCastVelocity);
                }
            }
        }
    }

    public override void Render()
    {
        // --- 1. 입질 감지 및 챔질 MISS 판정 로직 (기존과 동일) ---
        if (HasInputAuthority && CurrentBobber != null)
        {
            var bobber = CurrentBobber.GetComponent<BobberController>();
            if (bobber != null)
            {
                if (bobber.HasFishOn && !_fishJustBit)
                {
                    _fishJustBit = true;
                    SendHapticImpulse(vibrationAmplitude, vibrationDuration);
                }
                else if (!bobber.HasFishOn && _fishJustBit)
                {
                    _fishJustBit = false;
                    _missWindowActive = true;
                    _missWindowEndTime = Time.time + 2.0f;
                }
            }
        }

        // --- 2. 게이지 UI 업데이트 (기존과 동일) ---
        UpdateGaugeUI();


        // --- 3. [새로 추가된 로직] 물고기 기절 효과 UI 제어 ---
        if (CurrentBobber != null)
        {
            // 찌(Bobber)에 붙어있는 BobberController를 가져옵니다.
            var bobber = CurrentBobber.GetComponent<BobberController>();
            if (bobber != null)
            {
                // 현재 물고기와 싸우는 중인지 확인합니다.
                if (IsFighting)
                {
                    // IsCurrentlyFleeing이 false일 때(기절 상태일 때) 이미지를 활성화합니다.
                    bobber.SetStunEffectActive(!IsCurrentlyFleeing);
                }
                else
                {
                    // 싸움이 끝나면 이미지를 비활성화합니다.
                    bobber.SetStunEffectActive(false);
                }
            }
        }
    }
    #endregion

    #region 낚시 상태 및 RPC 함수
    public void ToggleFishingState()
    {
        if (HasInputAuthority) RPC_SetFishingState(!IsFishing);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
   
    private void RPC_SetFishingState(NetworkBool isFishing)
    {
        IsFishing = isFishing;
        if (isFishing)
        {
            CastingCooldown = TickTimer.CreateFromSeconds(Runner, 1.0f);
            if (fishingRodPrefab != null && SpawnedRod == null)
            {
                SpawnedRod = Runner.Spawn(fishingRodPrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority);
                if (SpawnedRod != null)
                {
                    _rodTip = SpawnedRod.GetComponentInChildren<RodInfo>()?.rodTip;
                    _rodTipRb = _rodTip?.GetComponent<Rigidbody>();
                }
            }
            if (bobberPrefab != null && CurrentBobber == null && _rodTip != null)
            {
                Vector3 spawnPos = _rodTip.position - (_rodTip.up * bobberHangOffset);
                CurrentBobber = Runner.Spawn(bobberPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);
                if (CurrentBobber.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
                AttachBobberWithJoint(CurrentBobber, _rodTipRb);
            }
        }
        else
        {
            // 기존 릴링 사운드 정지
            if (reelingAudioSource != null && reelingAudioSource.isPlaying)
            {
                reelingAudioSource.Stop();
            }

            // --- [추가된 부분] ---
            // 새로운 장력 사운드도 함께 정지
            if (tensionAudioSource != null && tensionAudioSource.isPlaying)
            {
                tensionAudioSource.Stop();
            }
            // --- [여기까지] ---

            if (SpawnedRod != null) Runner.Despawn(SpawnedRod);
            if (CurrentBobber != null) Runner.Despawn(CurrentBobber);
            if (HookedFish != null) Runner.Despawn(HookedFish);

            SpawnedRod = null;
            CurrentBobber = null;
            HookedFish = null;
            IsFighting = false;
            _rodTip = null;
            _rodTipRb = null;
            tensionGauge = 0f;
        }
        RPC_UpdateVisuals(isFishing, SpawnedRod);
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_AttemptHook(NetworkObject fishToHook)
    {
        if (IsFighting || fishToHook == null) return;

        BobberController bobber = null;
        if (CurrentBobber != null)
        {
            CurrentBobber.TryGetComponent(out bobber);
        }

        if (bobber == null || !bobber.BiteActiveTimer.IsRunning) return;

        float reactionTime = bobber.biteDuration - (bobber.BiteActiveTimer.RemainingTime(Runner) ?? bobber.biteDuration);

        HookedFish = fishToHook;
        if (HookedFish != null && HookedFish.TryGetComponent<FishData>(out var fishData))
        {
            fishData.InitializeFish();
            HookedFish.gameObject.SetActive(true);
            IsFighting = true;
            DistanceDisplayDelayTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);

            if (HookedFish != null)
            {
                int outlineLayer = LayerMask.NameToLayer("FishOutline");
                SetLayerRecursively(HookedFish.gameObject, outlineLayer);
            }

            if (reactionTime <= 0.2f)
            {
                bobber.RPC_ShowHookResultMessage("PERFECT!", 1.5f);
                bobber.RPC_ShowHookEffect(2); // [추가] PERFECT 이펙트(타입 2) 호출

                IsCurrentlyFleeing = false;
                FleeStateTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(1.5f, 2.5f));
            }
            else if (reactionTime <= 0.3f)
            {
                bobber.RPC_ShowHookResultMessage("GREAT!", 1.5f);
                bobber.RPC_ShowHookEffect(1); // [추가] GREAT 이펙트(타입 1) 호출

                IsCurrentlyFleeing = true;
                FleeStateTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(3f, 6f));
                Vector3 awayDirection = (CurrentBobber.transform.position - transform.position);
                awayDirection.y = 0;
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-45f, 45f), 0);
                FleeDirection = randomRotation * awayDirection.normalized;
            }
            else
            {
                bobber.RPC_ShowHookResultMessage("HIT!", 1.5f);
                bobber.RPC_ShowHookEffect(0); // [추가] HIT 이펙트(타입 0) 호출

                IsCurrentlyFleeing = true;
                FleeStateTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(3f, 6f));
                Vector3 awayDirection = (CurrentBobber.transform.position - transform.position);
                awayDirection.y = 0;
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-45f, 45f), 0);
                FleeDirection = randomRotation * awayDirection.normalized;
            }

            bobber.HasFishOn = false;
            bobber.HookedFish = null;

            if (CurrentBobber.TryGetComponent<ConfigurableJoint>(out var joint)) Destroy(joint);
            AttachFishToHook();
            tensionGauge = 0f;
        }
        else if (HookedFish != null)
        {
            Runner.Despawn(HookedFish);
            HookedFish = null;
        }
    }

    private void AttachFishToHook()
    {
        var rodLine = SpawnedRod?.GetComponentInChildren<RodLineController>();
        if (rodLine == null) return;
        Transform hookTransform = rodLine.GetCurrentHookTransform();
        if (hookTransform == null) return;
        Transform attachPoint = FindDeepChild(hookTransform, "FishAttachPoint");
        if (attachPoint == null) return;

        if (HookedFish != null)
        {
            // 부모 설정 및 물리 효과 비활성화만 수행
            var fishTransform = HookedFish.transform;
            fishTransform.SetParent(attachPoint);
            if (fishTransform.TryGetComponent<Rigidbody>(out var fishRb))
            {
                fishRb.isKinematic = true;
                fishRb.useGravity = false;
            }
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CastBobber(Vector3 force)
    {
        if (CurrentBobber == null) return;
        var joint = CurrentBobber.GetComponent<ConfigurableJoint>();
        if (joint != null) Destroy(joint);
        if (CurrentBobber.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(force, ForceMode.Impulse);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReelingState(NetworkBool state)
    {
        IsReeling = state;
    }

    private IEnumerator DelayedDespawnRoutine(NetworkObject fishToDespawn)
    {
        yield return new WaitForSeconds(10f);
        if (Runner != null && fishToDespawn != null && fishToDespawn.IsValid)
        {
            Runner.Despawn(fishToDespawn);
        }
        if (HookedFish == fishToDespawn)
        {
            HookedFish = null;
        }
    }

    private void AttachBobberWithJoint(NetworkObject bobber, Rigidbody rodTipRb)
    {
        if (bobber == null || rodTipRb == null) return;
        var existingJoint = bobber.GetComponent<ConfigurableJoint>();
        if (existingJoint != null) Destroy(existingJoint);
        var joint = bobber.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = rodTipRb;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = Vector3.zero;
        joint.anchor = Vector3.zero;
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;
        var limit = new SoftJointLimit { limit = bobberHangOffset };
        joint.linearLimit = limit;
    }
    #endregion

    #region 게이지 및 유틸리티 함수
    

   

    private void UpdateGaugeUI()
    {
        if (CurrentBobber == null) return;
        var slider = CurrentBobber.GetComponentInChildren<Slider>(true);
        if (slider != null)
        {
            if (IsFighting)
            {
                if (!slider.gameObject.activeSelf) slider.gameObject.SetActive(true);
                slider.value = tensionGauge / maxGauge;
            }
            else
            {
                if (slider.gameObject.activeSelf) slider.gameObject.SetActive(false);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]

    private void RPC_OnFishingFailed()
    {
        // 실패 효과 재생 RPC를 호출하여 클라이언트에서 사운드를 재생하도록 합니다.
        RPC_PlayFailureEffects();

        if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber))
        {
            // 찌가 메시지를 표시한 후 스스로 파괴되도록 마지막 인자를 'true'로 다시 변경합니다.
            bobber.RPC_ShowMessage("줄이 끊어졌습니다.", 2f, true);
        }
        if (HookedFish != null)
        {
            int fishLayer = LayerMask.NameToLayer("Fish");
            SetLayerRecursively(HookedFish.gameObject, fishLayer);
        }
        Debug.Log($"낚시 실패! 게이지 100% 초과. 플레이어: {Object.InputAuthority}");

        // 낚싯대와 물고기만 파괴하고, 찌(CurrentBobber)는 스스로 파괴되도록 남겨둡니다.
        if (SpawnedRod != null) Runner.Despawn(SpawnedRod);
        if (HookedFish != null) Runner.Despawn(HookedFish);

        SpawnedRod = null;
        CurrentBobber = null; // 참조는 제거하여 새로운 낚시를 준비합니다.
        HookedFish = null;
        IsFighting = false;
        tensionGauge = 0f;
        IsCurrentlyFleeing = false;
        FleeStateTimer = default;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_PlayFailureEffects()
    {
        // 로컬 플레이어에게만 실패 사운드를 재생합니다.
        if (sfxAudioSource != null && lineSnapSound != null)
        {
            sfxAudioSource.PlayOneShot(lineSnapSound);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_PlayRetrievalSound()
    {
        if (sfxAudioSource != null && retrievalSound != null)
        {
            sfxAudioSource.PlayOneShot(retrievalSound);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateVisuals(NetworkBool isFishing, NetworkObject rod)
    {
        AttachRodToHand(isFishing, rod);
        if (HasInputAuthority)
        {
            FishingUIManager.Instance?.UpdateFishingButton(isFishing);
        }
    }

    private void AttachRodToHand(bool isFishing, NetworkObject rod)
    {
        if (rod == null || _riggingManager?.rightHandController == null) return;
        if (rod.TryGetComponent<NetworkTransform>(out var networkTransform))
        {
            if (isFishing)
            {
                networkTransform.enabled = false;
                rod.transform.SetParent(_riggingManager.rightHandController);
                rod.transform.localPosition = new Vector3(0, 0, 0.05f);
                rod.transform.localRotation = Quaternion.Euler(0, 0, 90);
                rod.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
        }
    }

    private void SendHapticImpulse(float amplitude, float duration)
    {
        if (_rightHandXRController != null)
        {
            _rightHandXRController.SendHapticImpulse(amplitude, duration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasInputAuthority) return;
        if (other.CompareTag("CastingZone")) FishingUIManager.Instance?.ShowButton();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HasInputAuthority) return;
        if (other.CompareTag("CastingZone")) FishingUIManager.Instance?.HideButton();
    }
    #endregion

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    private void HandleSuccessfulCatch()
    {
        Debug.Log("낚시 성공! 물고기가 매달려 있습니다. 10초 후 사라집니다.");

        var caughtFishUI = CurrentBobber.GetComponent<CaughtFishUI>();
        var fishData = HookedFish.GetComponent<FishData>();
        if (caughtFishUI != null && fishData != null)
        {
            caughtFishUI.ShowFishInfo(fishData, 10f);
        }

        if (HookedFish != null)
        {
            int fishLayer = LayerMask.NameToLayer("Fish");
            SetLayerRecursively(HookedFish.gameObject, fishLayer);
        }

        IsFighting = false;

        if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber))
        {
            bobber.HideAllTexts();
        }

        tensionGauge = 0f;

        if (HookedFish != null)
        {
            var rodLineController = SpawnedRod?.GetComponentInChildren<RodLineController>();
            var hookTransform = rodLineController?.GetCurrentHookTransform();

            var fishTransform = HookedFish.transform;
            fishTransform.localRotation = Quaternion.Euler(-90, 0, 0);

            Transform headEnd = FindDeepChild(fishTransform, "HEAD_end");
            Transform attachPoint = (hookTransform != null) ? FindDeepChild(hookTransform, "FishAttachPoint") : null;
            if (headEnd != null && attachPoint != null)
            {
                Vector3 offset = attachPoint.position - headEnd.position;
                fishTransform.position += offset;
            }
            StartCoroutine(DelayedDespawnRoutine(HookedFish));
        }
    }

    private void CheckAndHandleRetrieval()
    {
        // [수정] 찌가 없거나, 이미 낚싯대에 Joint로 붙어있다면 더 이상 진행하지 않음 (중복 실행 방지)
        if (CurrentBobber == null || CurrentBobber.GetComponent<ConfigurableJoint>() != null)
        {
            return;
        }

        // 찌가 회수 거리 안으로 들어왔는지 확인
        if (_rodTip != null && Vector3.Distance(CurrentBobber.transform.position, _rodTip.position) < retrievalDistance)
        {
            // 찌를 낚싯대 끝에 다시 붙임
            AttachBobberWithJoint(CurrentBobber, _rodTipRb);
            RPC_PlayRetrievalSound();

            // 만약 물고기를 끌고 오는 중이었다면, '낚시 성공' 처리
            if (IsFighting)
            {
                HandleSuccessfulCatch();
            }
        }
    }
    // PlayerFishingController.cs 클래스 내부 아무 곳에나 추가 (맨 아래 추천)

    private void HandleFishRotation()
    {
        // 싸우는 중이 아니거나, 잡힌 물고기 또는 그 부모(AttachPoint)가 없으면 아무것도 하지 않음
        if (!IsFighting || HookedFish == null || HookedFish.transform.parent == null) return;

        // 목표 방향을 담을 변수
        Vector3 targetDirection;

        // 1. 물고기 상태에 따라 목표 방향 결정
        if (IsCurrentlyFleeing)
        {
            // 도망치는 중일 때: FleeDirection을 목표로 함
            targetDirection = FleeDirection;
        }
        else
        {
            // 기절 또는 끌려오는 중일 때: 낚싯대 끝(_rodTip)을 목표로 함
            if (_rodTip == null) return; // 낚싯대가 없으면 중단
            targetDirection = _rodTip.position - HookedFish.transform.position;
        }

        // y축(수직) 회전은 무시하여 물고기가 위아래로 뒤집히지 않게 함
        targetDirection.y = 0;

        // 2. 목표 방향으로 부드럽게 회전 (로컬 회전으로 수정)
        if (targetDirection.sqrMagnitude > 0.01f)
        {
            // a. 월드 공간에서의 목표 회전값을 계산
            Quaternion worldTargetRotation = Quaternion.LookRotation(targetDirection);

            // b. 월드 회전값을 부모 기준의 로컬 회전값으로 변환
            //    (부모의 회전값을 역으로 곱해줌으로써 월드 회전 효과를 상쇄)
            Quaternion localTargetRotation = Quaternion.Inverse(HookedFish.transform.parent.rotation) * worldTargetRotation;

            // c. 현재 로컬 각도에서 목표 로컬 각도로 부드럽게 회전 (Slerp)
            HookedFish.transform.localRotation = Quaternion.Slerp(
                HookedFish.transform.localRotation,
                localTargetRotation,
                Runner.DeltaTime * fishRotationSpeed
            );
        }
    }

    private void LateUpdate()
    {
        // 입력 권한이 있는 플레이어만, 그리고 전투 중에만 실행
        if (HasInputAuthority && IsFighting)
        {
            UpdateFishAttachment();
        }
    }

    // PlayerFishingController.cs 클래스 맨 아래에 새 함수 추가
    private void UpdateFishAttachment()
    {
        if (HookedFish == null || HookedFish.transform.parent == null) return;

        // 바늘의 부착 지점(부모)과 물고기의 입 위치를 가져옴
        Transform attachPoint = HookedFish.transform.parent;
        Transform headEnd = FindDeepChild(HookedFish.transform, "HEAD_end");

        if (headEnd != null)
        {
            // 매 프레임, 물고기 입(headEnd)이 바늘(attachPoint)에 오도록 위치를 강제로 고정
            Vector3 offset = HookedFish.transform.position - headEnd.position;
            HookedFish.transform.position = attachPoint.position + offset;
        }
    }
}