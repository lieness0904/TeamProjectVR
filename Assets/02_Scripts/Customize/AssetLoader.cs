using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rukha93.ModularAnimeCharacter.Customization;
using System;

public class AssetLoader : MonoBehaviour, IAssetLoader
{
    public IEnumerator LoadAsset<T>(string path, Action<T> onLoaded)
    {
        var req = Resources.LoadAsync(path); 
        yield return req;

        if (req.asset is T loaded)
        {
            onLoaded?.Invoke(loaded);
        }
        else
        {
            Debug.LogError($"[AssetLoader] Load 실패 또는 타입 불일치 - 경로: {path}");
            onLoaded?.Invoke(default);
        }
    }

    public IEnumerator LoadAssetList(string path, Action<string[]> onLoaded)
    {
        // 필요 시 구현
        onLoaded?.Invoke(new string[0]);
        yield break;
    }
}
