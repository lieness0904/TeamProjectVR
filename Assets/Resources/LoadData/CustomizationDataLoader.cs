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
                onLoaded?.Invoke(new CustomizationData());
                yield break;
            }

            if (wrapper == null || string.IsNullOrEmpty(wrapper.customizationData))
            {
                Debug.LogWarning("wrapper.customizationData 비어있음 (신규 유저일 수 있음)");
                onLoaded?.Invoke(new CustomizationData());
                yield break;
            }

            try
            {
                var dto = JsonUtility.FromJson<CustomizationDataDTO>(wrapper.customizationData);
                var data = CustomizationDataConverter.FromDTO(dto);

                CustomizationDataStore.LatestDataJson = wrapper.customizationData;
                onLoaded?.Invoke(data);
            }
            catch
            {
                Debug.LogWarning("CustomizationDataDTO 파싱 실패! 내용: " + wrapper.customizationData);
                onLoaded?.Invoke(new CustomizationData());
            }
        }
        else
        {
            Debug.LogError("외형 불러오기 실패: " + www.error);
            onLoaded?.Invoke(new CustomizationData());
        }
    }
}
