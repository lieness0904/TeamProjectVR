using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class CustomizationDataLoader 
{
    public static void LoadCustomizationFromSheet(string userId, Action<CustomizationData> onLoaded)
    {
        GameManager.Instance.StartCoroutine(LoadRoutine(userId, onLoaded));
    }

    private static IEnumerator LoadRoutine(string userId, Action<CustomizationData> onLoaded)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "getCustomizationData");
        form.AddField("userId", userId);

        using UnityWebRequest www = UnityWebRequest.Post("https://script.google.com/macros/s/AKfycbxbEbhCsVmqMWCuZOtPEcfGFperFw3nRjDw5OsECes9IFx2pbeXZMFmMAS0E20XUVy3/exec", form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var response = www.downloadHandler.text;
            Debug.Log("커스터마이징 응답: " + response);

            CustomizationDataWrapper wrapper = null;
            try
            {
                wrapper = JsonUtility.FromJson<CustomizationDataWrapper>(response);
            }
            catch
            {
                Debug.LogWarning("Wrapper 파싱 실패! 응답: " + response);
                onLoaded?.Invoke(new CustomizationData()); // 디폴트라도 넘겨줘
                yield break;
            }

            if (wrapper == null || string.IsNullOrEmpty(wrapper.customizationData))
            {
                Debug.LogWarning("wrapper.customizationData 비어있음 (신규 유저일 수 있음)");
                onLoaded?.Invoke(new CustomizationData()); // 디폴트 값으로 진입
                yield break;
            }

            CustomizationData data = default;
            try
            {
                data = JsonUtility.FromJson<CustomizationData>(wrapper.customizationData);

                CustomizationDataStore.LatestDataJson = wrapper.customizationData;
            }
            catch
            {
                Debug.LogWarning("CustomizationData 파싱 실패! 내용: " + wrapper.customizationData);
                onLoaded?.Invoke(new CustomizationData()); // 혹시 파싱 실패해도 기본값
                yield break;
            }

            onLoaded?.Invoke(data);
        }
        else
        {
            Debug.LogError("외형 불러오기 실패: " + www.error);
            onLoaded?.Invoke(new CustomizationData()); // 네트워크 실패시도 기본값
        }
    }
}
