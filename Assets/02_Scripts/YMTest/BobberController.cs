using Fusion;
using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class BobberController : NetworkBehaviour
{
    [Header("입질 설정")]
    [SerializeField] private float minWaitTime = 5f;
    [SerializeField] private float maxWaitTime = 15f;

    [Header("시각 효과")]
    [SerializeField] private MeshRenderer bobberRenderer;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color biteColor = Color.red;

    [Tooltip("입질이 지속되는 시간입니다. 이 시간 안에 챔질해야 합니다.")]
    [SerializeField] public float biteDuration = 0.5f; // 2.5초의 챔질 시간

    [Header("UI 표시")]
    [Tooltip("'HIT!' 메시지를 표시할 UI")]
    [SerializeField] private TextMeshProUGUI hitText;
    [Tooltip("MISS, 실패 등 다른 메시지를 표시할 UI")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("사운드")]
    [Tooltip("물이 튀는 '퐁당' 사운드")]
    [SerializeField] private AudioClip splashSound;

    [Tooltip("'HIT' 시 재생될 사운드")] // <-- 이 줄을 추가하세요
    [SerializeField] private AudioClip hitSound;
    private AudioSource audioSource;

    [Networked] public NetworkBool IsInFishingZone { get; set; }
    [Networked] public NetworkBool HasFishOn { get; set; }
    [Networked] public NetworkObject HookedFish { get; set; }
    [Networked] private TickTimer BitingTimer { get; set; }
    [Networked] public TickTimer BiteActiveTimer { get; set; }


    private FishingZone currentZone;

    public override void Spawned()
    {
        // 시작할 때 모든 텍스트를 숨깁니다.
        HideAllTexts();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // 3D 사운드로 설정하여 찌의 위치에서 소리가 나게 합니다.
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
    }

    public override void Render()
    {
        if (bobberRenderer != null)
        {
            bobberRenderer.material.color = HasFishOn ? biteColor : defaultColor;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (HasFishOn && BiteActiveTimer.Expired(Runner))
        {
            HasFishOn = false; // 입질 상태를 종료합니다.
            BiteActiveTimer = default; // 타이머를 리셋합니다.

            // 챔질하지 않아 놓친 물고기를 디스폰시킵니다.
            if (HookedFish != null)
            {
                Runner.Despawn(HookedFish);
                HookedFish = null;
            }
        }

        if (IsInFishingZone && !HasFishOn && !BitingTimer.IsRunning)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            BitingTimer = TickTimer.CreateFromSeconds(Runner, waitTime);
        }

        if (BitingTimer.Expired(Runner))
        {
            if (currentZone != null && currentZone.availableFishPrefabs.Count > 0)
            {
                GameObject fishPrefab = SelectFishByWeight(currentZone.availableFishPrefabs);
                if (fishPrefab != null)
                {
                    HookedFish = Runner.Spawn(fishPrefab, transform.position, Quaternion.identity, Object.InputAuthority);
                    if (HookedFish != null)
                    {
                        HookedFish.gameObject.SetActive(false);
                        HasFishOn = true;
                        BiteActiveTimer = TickTimer.CreateFromSeconds(Runner, biteDuration);                        
                        Debug.Log($"[Server] 입질 감지! 물고기 {fishPrefab.name} 스폰 및 비활성화.");
                    }
                }
            }
            BitingTimer = default;
        }
    }

    // --- [새로운 공개 함수들] ---

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowHookResultMessage(string message, float duration)
    {
        // 모든 클라이언트에서 'HIT' 사운드를 재생합니다.
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound, 5.0f);
        }

        // 전달받은 메시지를 UI에 보여줍니다.
        StartCoroutine(ShowTextRoutine(hitText, message, duration));
    }

    public void ShowMessageText(string message, float duration)
    {
        StartCoroutine(ShowTextRoutine(messageText, message, duration));
    }

    public void UpdateDistanceText(string text)
    {
        if (hitText == null) return;
        if (!hitText.gameObject.activeSelf) hitText.gameObject.SetActive(true);
        if (messageText != null && messageText.gameObject.activeSelf) messageText.gameObject.SetActive(false); // 다른 메시지는 숨김
        hitText.text = text;
    }

    public void HideAllTexts()
    {
        if (hitText != null) hitText.gameObject.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    // --- [RPC 수정] ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowMessage(string message, float duration, NetworkBool destroyAfter)
    {
        ShowMessageText(message, duration);
        // 이 RPC를 호출한 서버(호스트)만 아래 코드를 실행하여, 일정 시간 후 스스로를 파괴합니다.
        if (HasStateAuthority && destroyAfter)
        {
            StartCoroutine(DelayedDespawnRoutine(duration));
        }
    }

    private IEnumerator DelayedDespawnRoutine(float delay)
    {
        // 메시지가 보이는 시간 + 0.1초 여유를 두고 파괴
        yield return new WaitForSeconds(delay + 0.1f);
        if (Runner != null && Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }

    private IEnumerator ShowTextRoutine(TextMeshProUGUI textUI, string message, float duration)
    {
        if (textUI == null) yield break;
        textUI.gameObject.SetActive(true);
        textUI.text = message;
        yield return new WaitForSeconds(duration);
        textUI.gameObject.SetActive(false);
    }

    // --- 기존 로직들 ---
    private GameObject SelectFishByWeight(System.Collections.Generic.List<GameObject> fishPrefabs)
    {
        // ... (내용 변경 없음)
        if (fishPrefabs == null || fishPrefabs.Count == 0) return null;
        int totalWeight = 0;
        foreach (var prefab in fishPrefabs)
        {
            if (prefab.TryGetComponent<FishData>(out var fishData))
            {
                totalWeight += fishData.appearanceWeight;
            }
        }
        if (totalWeight <= 0) return fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        int randomPoint = Random.Range(0, totalWeight);
        foreach (var prefab in fishPrefabs)
        {
            if (prefab.TryGetComponent<FishData>(out var fishData))
            {
                if (randomPoint < fishData.appearanceWeight) return prefab;
                else randomPoint -= fishData.appearanceWeight;
            }
        }
        return fishPrefabs[fishPrefabs.Count - 1];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<FishingZone>(out var zone))
        {
            if (audioSource != null && splashSound != null)
            {
                audioSource.PlayOneShot(splashSound, 5f);
            }

            currentZone = zone;
            if (HasStateAuthority) IsInFishingZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<FishingZone>(out var zone))
        {
            if (zone == currentZone)
            {
                currentZone = null;
                if (HasStateAuthority)
                {
                    IsInFishingZone = false;
                    HasFishOn = false;
                    BitingTimer = default;
                    if (HookedFish != null)
                    {
                        Runner.Despawn(HookedFish);
                        HookedFish = null;
                    }
                }
            }
        }
    }
}