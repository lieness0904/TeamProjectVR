using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rukha93.ModularAnimeCharacter.Customization;
using Fusion;
using System.IO;
using System.Linq;
using System;

public class CustomPlayer : NetworkBehaviour
{
    [Networked] public CustomizationData CustomData { get; set; }

    [SerializeField] private GameObject customizationTargetRoot;
    private IAssetLoader assetLoader;
    private ChangeDetector _changeDetector;

    private Dictionary<string, List<GameObject>> equippedObjects = new();
    private SkinnedMeshRenderer referenceSMR;
    private bool isApplying = false;
    private HeadHider headHider;

    public override void Spawned()
    {
        assetLoader = GetComponentInChildren<IAssetLoader>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasInputAuthority)
        {
            Debug.Log("내 플레이어니까 커스터마이징 불러올 준비 가능");

            if (!string.IsNullOrEmpty(CustomizationDataStore.LatestDataJson))
            {
                var data = JsonUtility.FromJson<CustomizationData>(CustomizationDataStore.LatestDataJson);
                ApplyCustomizationFromData(data);
                CustomizationDataStore.Clear();
            }
        }

        Debug.Log($"CustomData 현재 값: {CustomData.gender} / {CustomData.body}");
    }

    public override void Render()
    {
        if (isApplying) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(CustomData))
            {
                Debug.Log("커스터마이징 데이터 변경 감지됨 -> 적용");
                StartCoroutine(ApplyRoutine(CustomData));
            }
        }
    }

    public void ApplyCustomizationFromData(CustomizationData data)
    {
        Debug.Log($"ApplyCustomizationFromData 호출됨 - {data.gender} / {data.body}");
        CustomData = data;
    }
    

    private IEnumerator ApplyRoutine(CustomizationData data)
    {
        isApplying = true;

        string gender = data.gender.Value.ToLower();
        assetLoader ??= GetComponentInChildren<IAssetLoader>();

        yield return LoadAndEquip(gender, "body", data.body.Value);
        yield return LoadAndEquip(gender, "head", data.head.Value);
        yield return LoadAndEquip(gender, "top", data.top.Value);
        yield return LoadAndEquip(gender, "bottom", data.bottom.Value);
        yield return LoadAndEquip(gender, "shoes", data.shoes.Value);
        yield return LoadAndEquip(gender, "outfit", data.outfit.Value);
        yield return LoadAndEquip(gender, "hairstyle", data.hairstyle.Value);
        yield return LoadAndEquip(gender, "acc_head", data.acc_head.Value);

        isApplying = false;
    }

    private IEnumerator LoadAndEquip(string gender, string category, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"[LoadAndEquip] path가 비어있음 - category: {category}");
            yield break;
        }

        string fullPath = GetFullResourcePath(gender, path);
        Debug.Log($"[LoadAndEquip] 경로: {fullPath}");

        if (assetLoader == null)
        {
            Debug.LogError("[LoadAndEquip] assetLoader가 null임");
            yield break;
        }

        if (category == "body")
        {
            GameObject prefab = Resources.Load<GameObject>(fullPath);
            if (prefab == null)
            {
                Debug.LogError($"[LoadAndEquip] body prefab 로드 실패 - {fullPath}");
                yield break;
            }

            if (customizationTargetRoot != null)
            {
                Destroy(customizationTargetRoot);
            }

            customizationTargetRoot = Instantiate(prefab, transform);
            customizationTargetRoot.name = "CharacterAvatar";

            equippedObjects.Clear();
            referenceSMR = null;

            yield break;
        }

        bool isDone = false;

        yield return assetLoader.LoadAsset<CustomizationItemAsset>(fullPath, asset =>
        {
            if (asset == null)
            {
                Debug.LogError($"[LoadAndEquip] asset 로드 실패 - {fullPath}");
            }
            else
            {
                Debug.Log($"[LoadAndEquip] asset 로드 성공 - {fullPath}");
                Equip(category, path, asset);
            }

            isDone = true;
        });

        yield return new WaitUntil(() => isDone);
    }

    private string GetFullResourcePath(string gender, string filename)
    {
        if (string.IsNullOrEmpty(gender) || string.IsNullOrEmpty(filename))
        {
            Debug.LogWarning("[GetFullResourcePath] gender 또는 filename이 비어있음");
            return "";
        }

        string genderUpper = gender.ToUpper();
        string genderPath = $"Customization/{genderUpper}/{filename}";
        var genderAsset = Resources.Load<CustomizationItemAsset>(genderPath);
        if (genderAsset != null)
        {
            Debug.Log($"[GetFullResourcePath] Gender 경로 사용됨: {genderPath}");
            return genderPath;
        }

        string sharedPath = $"Customization/Shared/{filename}";
        var sharedAsset = Resources.Load<CustomizationItemAsset>(sharedPath);
        if (sharedAsset != null)
        {
            Debug.Log($"[GetFullResourcePath] Shared 경로 사용됨: {sharedPath}");
            return sharedPath;
        }

        Debug.LogError($"[GetFullResourcePath] ❌ 에셋 없음 - Gender: {genderPath} / Shared: {sharedPath}");
        return "";
    }

    private void Equip(string category, string path, CustomizationItemAsset asset)
    {
        if (customizationTargetRoot == null)
        {
            Debug.LogError("[Equip] customizationTargetRoot가 null임");
            return;
        }

        if (equippedObjects.TryGetValue(category, out var existingList))
        {
            foreach (var go in existingList)
            {
                if (go != null) Destroy(go);
            }
            equippedObjects.Remove(category);
        }

        List<GameObject> newEquipped = new();

        var animator = customizationTargetRoot.GetComponentInChildren<Animator>() ?? GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("[Equip] Animator 못 찾음");
            return;
        }

        referenceSMR ??= customizationTargetRoot.GetComponentInChildren<SkinnedMeshRenderer>();
        if (referenceSMR == null || referenceSMR.bones == null || referenceSMR.rootBone == null)
        {
            Debug.LogError("[Equip] referenceSMR 또는 bones, rootBone이 없음");
            return;
        }

        // 메시 파츠 처리
        foreach (var mesh in asset.meshes)
        {
            if (mesh == null || mesh.sharedMesh == null)
            {
                Debug.LogError($"[Equip] mesh 또는 sharedMesh가 null - category: {category}");
                continue;
            }

            GameObject go = new GameObject($"{category}_Mesh");
            go.transform.SetParent(customizationTargetRoot.transform, false);

            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.rootBone = referenceSMR.rootBone;
            smr.bones = referenceSMR.bones;
            smr.sharedMesh = mesh.sharedMesh;
            smr.sharedMaterials = mesh.sharedMaterials;

            if (Object.HasInputAuthority && (category == "head" || category == "hairstyle" || category == "acc_head"))
            {
                smr.enabled = false;
            }

            newEquipped.Add(go);
        }

        // 본에 붙이는 오브젝트 처리 (인덱스 기반)
        for (int i = 0; i < asset.objects.Length; i++)
        {
            var obj = asset.objects[i];

            if (obj.prefab == null)
            {
                Debug.LogWarning($"[Equip] prefab이 null - category: {category}");
                continue;
            }

            var bone = animator.GetBoneTransform(obj.targetBone);
            if (bone == null)
            {
                Debug.LogWarning($"[Equip] Bone {obj.targetBone} 찾을 수 없음 - category: {category}");
                continue;
            }

            string uniqueSlotName = $"{category}_{i}";
            Transform slot = bone.Find(uniqueSlotName);
            if (slot == null)
            {
                GameObject slotGO = new GameObject(uniqueSlotName);
                slotGO.transform.SetParent(bone);
                slotGO.transform.localPosition = Vector3.zero;
                slotGO.transform.localRotation = Quaternion.identity;
                slotGO.transform.localScale = Vector3.one;
                slot = slotGO.transform;
            }

            for (int j = slot.childCount - 1; j >= 0; j--)
            {
                Destroy(slot.GetChild(j).gameObject);
            }

            var go = Instantiate(obj.prefab, slot);
            go.name = $"{category}_Obj";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            if (Object.HasInputAuthority && (category == "head" || category == "hairstyle" || category == "acc_head"))
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                    renderer.enabled = false;
            }

            newEquipped.Add(go);
        }

        equippedObjects[category] = newEquipped;
    }
}

