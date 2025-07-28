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

    public override void Spawned()
    {
        assetLoader = GetComponentInChildren<IAssetLoader>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // InputAuthority: 본인일 때 적용
        if (Object.HasInputAuthority || Object.HasStateAuthority)
        {
            var data = !string.IsNullOrEmpty(CustomizationDataStore.LatestDataJson)
                ? CustomizationDataConverter.FromJson(CustomizationDataStore.LatestDataJson)
                : PlayerDataManager.Instance.CustomizationData;

            if (Object.HasInputAuthority)
            {
                ApplyCustomizationFromData(data);
            }

            if (Object.HasStateAuthority)
            {
                CustomData = data;
            }

            CustomizationDataStore.Clear();
        }

        Debug.Log($"[CustomPlayer] Spawned 완료 - gender: {CustomData.gender} / body: {CustomData.body}");
    }

    public override void Render()
    {
        if (isApplying) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(CustomData))
            {
                Debug.Log("[CustomPlayer] CustomData 변경 감지됨 -> 적용 시작");
                StartCoroutine(ApplyRoutine(CustomData));
            }
        }
    }

    public void ApplyCustomizationFromData(CustomizationData data)
    {
        Debug.Log($"[CustomPlayer] ApplyCustomizationFromData 호출 - {data.gender}, {data.body}");
        StartCoroutine(ApplyRoutine(data));
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

        if (category == "body")
        {
            GameObject prefab = Resources.Load<GameObject>(fullPath);
            if (prefab == null)
            {
                Debug.LogError($"[LoadAndEquip] body prefab 로드 실패 - {fullPath}");
                yield break;
            }

            if (customizationTargetRoot == null)
            {
                customizationTargetRoot = Instantiate(prefab, transform);
                customizationTargetRoot.name = "CharacterAvatar";
            }
            else
            {
                ClearEquippedParts(); // 기존 파츠 제거
            }

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
                Equip(category, path, asset);
            }

            isDone = true;
        });

        yield return new WaitUntil(() => isDone);
    }

    private void ClearEquippedParts()
    {
        foreach (var list in equippedObjects.Values)
        {
            foreach (var go in list)
            {
                if (go != null)
                    Destroy(go);
            }
        }

        equippedObjects.Clear();
    }

    private string GetFullResourcePath(string gender, string path)
    {
        if (string.IsNullOrEmpty(path))
            return "";

        if (path.StartsWith("Customization/") && Resources.Load(path) != null)
            return path;

        string genderPath = $"Customization/{gender.ToUpper()}/{path}";
        if (Resources.Load(genderPath) != null)
            return genderPath;

        string sharedPath = $"Customization/Shared/{path}";
        if (Resources.Load(sharedPath) != null)
            return sharedPath;

        return "";
    }

    private void Equip(string category, string path, CustomizationItemAsset asset)
    {
        if (customizationTargetRoot == null)
            return;

        if (equippedObjects.TryGetValue(category, out var existingList))
        {
            foreach (var go in existingList)
                if (go != null) Destroy(go);
            equippedObjects.Remove(category);
        }

        List<GameObject> newEquipped = new();

        var animator = customizationTargetRoot.GetComponentInChildren<Animator>();
        referenceSMR ??= customizationTargetRoot.GetComponentInChildren<SkinnedMeshRenderer>();

        if (animator == null || referenceSMR == null || referenceSMR.rootBone == null)
            return;

        // 메시 처리
        foreach (var mesh in asset.meshes)
        {
            if (mesh?.sharedMesh == null) continue;

            GameObject go = new GameObject($"{category}_Mesh");
            go.transform.SetParent(customizationTargetRoot.transform, false);

            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.rootBone = referenceSMR.rootBone;
            smr.bones = referenceSMR.bones;
            smr.sharedMesh = mesh.sharedMesh;
            smr.sharedMaterials = mesh.sharedMaterials;

            if (Object.HasInputAuthority && (category == "head" || category == "hairstyle" || category == "acc_head"))
                smr.enabled = false;

            newEquipped.Add(go);
        }

        // 본 오브젝트 처리
        for (int i = 0; i < asset.objects.Length; i++)
        {
            var obj = asset.objects[i];
            if (obj.prefab == null) continue;

            var bone = animator.GetBoneTransform(obj.targetBone);
            if (bone == null) continue;

            string slotName = $"{category}_{i}";
            var slot = bone.Find(slotName) ?? new GameObject(slotName).transform;
            slot.SetParent(bone);
            slot.localPosition = Vector3.zero;
            slot.localRotation = Quaternion.identity;
            slot.localScale = Vector3.one;

            for (int j = slot.childCount - 1; j >= 0; j--)
                Destroy(slot.GetChild(j).gameObject);

            var go = Instantiate(obj.prefab, slot);
            go.name = $"{category}_Obj";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            if (Object.HasInputAuthority && (category == "head" || category == "hairstyle" || category == "acc_head"))
            {
                foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                    renderer.enabled = false;
            }

            newEquipped.Add(go);
        }

        equippedObjects[category] = newEquipped;
    }
}


