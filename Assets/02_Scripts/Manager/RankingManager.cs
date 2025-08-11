using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using TMPro;

[System.Serializable]
public class RankingEntry
{
    public string userId;
    public int points;
}

[System.Serializable]
public class RankingResponse
{
    public string status;
    public List<RankingEntry> ranking;
}

public class RankingManager : MonoBehaviour
{
    private string scriptURL = "https://script.google.com/macros/s/AKfycbxbEbhCsVmqMWCuZOtPEcfGFperFw3nRjDw5OsECes9IFx2pbeXZMFmMAS0E20XUVy3/exec";

    [SerializeField] private List<GameObject> rankSlots; // RankSlot_1 ~ RankSlot_10 오브젝트 직접 드래그
    [SerializeField] private string nameObjectName = "RankNameText"; // 이름 텍스트 자식 오브젝트 이름
    [SerializeField] private string pointObjectName = "RankPointText"; // 포인트 텍스트 자식 오브젝트 이름

    public void LoadRankings()
    {
        StartCoroutine(LoadRankingRoutine());
    }

    private IEnumerator LoadRankingRoutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "getAllPoints");

        using (UnityWebRequest www = UnityWebRequest.Post(scriptURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var json = www.downloadHandler.text;
                var response = JsonUtility.FromJson<RankingResponse>(json);

                Debug.Log("[RAW JSON] " + json);
                Debug.Log($"[Parsed] Status: {response.status}");
                Debug.Log($"[Parsed] Ranking Count: {(response.ranking == null ? "null" : response.ranking.Count.ToString())}");

                if (response.status == "success")
                {
                    var sorted = response.ranking.OrderByDescending(r => r.points).Take(10).ToList();
                    Debug.Log("랭킹 로드 성공");

                    for (int i = 0; i < rankSlots.Count; i++)
                    {
                        if (i < sorted.Count)
                        {
                            var entry = sorted[i];
                            var nameText = rankSlots[i].transform.Find(nameObjectName)?.GetComponent<TextMeshProUGUI>();
                            var pointText = rankSlots[i].transform.Find(pointObjectName)?.GetComponent<TextMeshProUGUI>();

                            if (nameText != null) nameText.text = entry.userId;
                            if (pointText != null) pointText.text = entry.points.ToString() + "point";
                        }
                        else
                        {
                            // 랭킹이 부족한 경우 빈칸 처리
                            var nameText = rankSlots[i].transform.Find(nameObjectName)?.GetComponent<TextMeshProUGUI>();
                            var pointText = rankSlots[i].transform.Find(pointObjectName)?.GetComponent<TextMeshProUGUI>();

                            if (nameText != null) nameText.text = "-";
                            if (pointText != null) pointText.text = "-";
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("랭킹 로드 실패: " + json);
                }
            }
            else
            {
                Debug.LogError("랭킹 요청 실패: " + www.error);
            }
        }
    }
}
