using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using TMPro;

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

    [Header("낚시 디테일 설정")]
    [Tooltip("낚싯대에 찌가 매달릴 때의 기본 거리입니다.")]
    [SerializeField] private float bobberHangOffset = 0.2f;
    [Tooltip("찌가 낚싯대를 따라오는 부드러운 정도입니다. 값이 높을수록 빨리 따라옵니다.")]
    [SerializeField] private float bobberDampingSpeed = 10f;

    [Header("챔질(Hooking) 설정")]
    [Tooltip("이 속도를 넘는 컨트롤러 움직임을 챔질로 인식합니다.")]
    [SerializeField] private float hookVelocityThreshold = 3.0f;

    [Header("전투 UI")]
    [Tooltip("'HIT!' 메시지를 표시할 UI 텍스트")]
    [SerializeField] private TextMeshProUGUI hitText;


    [Networked] public NetworkBool IsFishing { get; set; }
    [Networked] private NetworkObject SpawnedRod { get; set; }
    [Networked] public NetworkObject CurrentBobber { get; private set; }
    [Networked] private TickTimer CastingCooldown { get; set; }

    [Networked] public NetworkBool IsFighting { get; set; }
    [Networked] public NetworkObject HookedFish { get; set; } // 이 변수는 챔질 후 '실제로' 낚싯대에 걸린 물고기입니다.

    private RiggingManager _riggingManager;
    public static PlayerFishingController Local { get; private set; }
    private XRBaseController _rightHandXRController;
    private bool _fishJustBit = false;
    private Transform _rodTip;
    private Rigidbody _rodTipRb;

    private Vector3 _lastControllerPos;
    private Vector3 _currentControllerVel;
    #endregion

    private void Awake()
    {
        _riggingManager = GetComponent<RiggingManager>();
        if (_riggingManager != null && _riggingManager.rightHandController != null)
        {
            _rightHandXRController = _riggingManager.rightHandController.GetComponent<XRBaseController>();
        }
    }

    /// <summary>
    /// Update는 매 프레임 호출되며, 로컬 플레이어의 입력을 감지하기에 적합합니다.
    /// </summary>
    private void Update()
    {
        if (!HasInputAuthority) return;

        // 1. 오른쪽 컨트롤러의 현재 속도를 계속 계산합니다.
        if (_riggingManager.rightHandController != null)
        {
            Vector3 currentPos = _riggingManager.rightHandController.position;
            _currentControllerVel = (currentPos - _lastControllerPos) / Time.deltaTime;
            _lastControllerPos = currentPos;
        }

        // 2. 챔질 조건을 확인합니다: (입질이 왔고 AND 아직 전투 중이 아닐 때)
        if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber) && bobber.HasFishOn && !IsFighting)
        {
            // 3. 컨트롤러를 위로 빠르게 잡아챘는지 확인합니다.
            if (_currentControllerVel.y > hookVelocityThreshold)
            {
                Debug.Log("Hook attempt successful!");
                // 4. 조건이 맞으면 서버에 챔질을 시도했다고 알립니다.
                // ▼▼▼ [수정된 부분] NetworkPrefabId 대신 bobber.HookedFish 자체를 전달합니다. ▼▼▼
                if (bobber.HookedFish != null) // 물고기가 스폰되어 있어야만 시도
                {
                    RPC_AttemptHook(bobber.HookedFish);
                }
                // ▲▲▲ [수정된 부분] ▲▲▲
            }
        }
    }

    #region Fusion 콜백 함수 (Spawned, Despawned, FixedUpdateNetwork, Render)
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            Local = this;
            // 컨트롤러 위치 초기화
            if (_riggingManager.rightHandController != null)
                _lastControllerPos = _riggingManager.rightHandController.position;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this)
        {
            Local = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority && castingHandler != null && castingHandler.castAction.action.WasReleasedThisFrame())
        {
            if (CastingCooldown.ExpiredOrNotRunning(Runner))
            {
                if (!IsFighting && CurrentBobber != null && CurrentBobber.GetComponent<ConfigurableJoint>() != null)
                {
                    RPC_CastBobber(castingHandler.LastCastVelocity);
                }
            }
        }
    }

    public override void Render()
    {
        if (HasInputAuthority && CurrentBobber != null)
        {
            var bobber = CurrentBobber.GetComponent<BobberController>();
            if (bobber != null)
            {
                if (bobber.HasFishOn && !_fishJustBit)
                {
                    _fishJustBit = true;
                    SendHapticImpulse(vibrationAmplitude, vibrationDuration);
                    // 입질 시 "HIT!" 메시지 표시 (InputAuthority에서만 RPC 호출)
                    if (Object.HasInputAuthority)
                    {
                        RPC_ShowHitMessage();
                    }
                }
                else if (!bobber.HasFishOn && _fishJustBit)
                {
                    _fishJustBit = false;
                    // 물고기 놓쳤을 때 "HIT!" 메시지 비활성화 (모든 클라이언트)
                    if (hitText != null && hitText.gameObject.activeSelf)
                    {
                        hitText.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
    #endregion

    #region 낚시 상태 및 RPC 함수
    public void ToggleFishingState()
    {
        if (HasInputAuthority)
        {
            RPC_SetFishingState(!IsFishing);
        }
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
                _rodTip = SpawnedRod?.GetComponentInChildren<RodInfo>()?.rodTip;
                _rodTipRb = _rodTip?.GetComponent<Rigidbody>();
            }
            if (bobberPrefab != null && CurrentBobber == null && _rodTip != null)
            {
                Vector3 spawnPos = _rodTip.position - (_rodTip.up * bobberHangOffset);
                CurrentBobber = Runner.Spawn(bobberPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);
                if (CurrentBobber.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                }
                AttachBobberWithJoint(CurrentBobber, _rodTipRb);
            }
        }
        else
        {
            if (SpawnedRod != null) Runner.Despawn(SpawnedRod);
            if (CurrentBobber != null) Runner.Despawn(CurrentBobber);
            if (HookedFish != null) Runner.Despawn(HookedFish);
            SpawnedRod = null;
            CurrentBobber = null;
            HookedFish = null;
            IsFighting = false;
            _rodTip = null;
            _rodTipRb = null;
        }
        RPC_UpdateVisuals(isFishing, SpawnedRod);
    }

    // ▼▼▼ [수정된 함수] 챔질 시도 RPC - NetworkObject를 인수로 받도록 변경 ▼▼▼
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_AttemptHook(NetworkObject fishToHook) // NetworkObject를 직접 인수로 받습니다.
    {
        // 이미 전투 중이거나, 물고기 오브젝트가 유효하지 않으면 리턴
        if (IsFighting || fishToHook == null) return;

        // HookedFish 변수에 BobberController에서 받아온 물고기 오브젝트를 직접 할당합니다.
        HookedFish = fishToHook;

        if (HookedFish != null && HookedFish.TryGetComponent<FishData>(out var fishData))
        {
            fishData.InitializeFish(); // 스폰된 물고기의 스탯(크기,무게 등) 결정
            HookedFish.gameObject.SetActive(true); // 물고기를 활성화하여 보이게 합니다.

            IsFighting = true; // 전투 상태로 전환

            // 찌의 입질 상태는 리셋
            if (CurrentBobber.TryGetComponent<BobberController>(out var bobber))
            {
                bobber.HasFishOn = false;
                bobber.HookedFish = null; // 찌에 연결된 물고기 참조도 끊어줍니다.
            }

            // 챔질 성공 시 찌에 연결된 조인트 제거 (물고기에게 컨트롤 넘기기 위함)
            if (CurrentBobber.TryGetComponent<ConfigurableJoint>(out var joint))
            {
                Destroy(joint);
            }

            RPC_ShowHitMessage(); // 모든 클라이언트에게 "HIT!" 메시지 표시 요청
            Debug.Log($"[Server] 챔질 성공! 물고기 {HookedFish.name}와 전투 시작.");
        }
        else if (HookedFish != null)
        {
            // 스폰은 됐는데 FishData가 없는 등 예외상황 처리
            Runner.Despawn(HookedFish);
            HookedFish = null;
        }
    }
    // ▲▲▲ [수정된 함수] 챔질 시도 RPC ▲▲▲


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowHitMessage()
    {
        if (hitText != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowHitTextRoutine());
        }
    }

    private IEnumerator ShowHitTextRoutine()
    {
        hitText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        hitText.gameObject.SetActive(false);
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
    private void RPC_ReelIn(float reelAmount)
    {
        if (CurrentBobber == null || _rodTip == null) return;
        if (CurrentBobber.TryGetComponent<Rigidbody>(out var bobberRigidbody))
        {
            if (CurrentBobber.GetComponent<ConfigurableJoint>() == null)
            {
                Vector3 newPosition = Vector3.MoveTowards(bobberRigidbody.position, _rodTip.position, reelAmount);
                bobberRigidbody.MovePosition(newPosition);
                if (Vector3.Distance(bobberRigidbody.position, _rodTip.position) < retrievalDistance)
                {
                    AttachBobberWithJoint(CurrentBobber, _rodTipRb);
                }
            }
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
        var limit = new SoftJointLimit();
        limit.limit = bobberHangOffset;
        joint.linearLimit = limit;
    }
    #endregion

    #region 기타 유틸리티 함수
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
        if (rod == null) return;
        if (rod.TryGetComponent<NetworkTransform>(out var networkTransform))
        {
            if (isFishing)
            {
                if (_riggingManager.rightHandController != null)
                {
                    networkTransform.enabled = false;
                    rod.transform.SetParent(_riggingManager.rightHandController);
                    rod.transform.localPosition = new Vector3(0, 0, 0.05f);
                    rod.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    rod.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
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
    public void ReelIn(float reelAmount)
    {
        if (CurrentBobber == null || !HasInputAuthority) return;
        RPC_ReelIn(reelAmount);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!HasInputAuthority) return;
        if (other.CompareTag("CastingZone"))
        {
            FishingUIManager.Instance?.ShowButton();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (!HasInputAuthority) return;
        if (other.CompareTag("CastingZone"))
        {
            FishingUIManager.Instance?.HideButton();
        }
    }
    #endregion
}