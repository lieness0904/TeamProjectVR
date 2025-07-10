using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    private string scriptURL = "https://script.google.com/macros/s/AKfycbxsVIFFP0aRSLTljhqnSI0KAso8jv3Hx3UPdIiWsl1UynSyyk1EVABCf2Fpz6WwzcNn/exec";

    [Header("XR Origin 프리팹 설정")]
    public GameObject xrOriginPrefab;

    [Header("UI Elements")]
    public TMP_InputField idInputField;
    public TMP_InputField passwordInputField;
    public Button loginButton;
    public TextMeshProUGUI statusText;

    void Start()
    {
        if (xrOriginPrefab != null)
        {
            // TitleScene에서는 기본적인 XR Origin만 사용하므로,
            // 이 부분은 이제 필요 없습니다. 주석 처리하거나 삭제합니다.
            // Instantiate(xrOriginPrefab);
        }

        if (loginButton != null)
        {
            // [수정 1] 리스너 중복 추가를 막기 위해 먼저 모든 리스너를 제거합니다.
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(OnLoginButtonClick);
        }
    }


    public void OnLoginButtonClick()
    {
        string userId = idInputField.text;
        string password = passwordInputField.text;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            statusText.text = "아이디와 비밀번호를 모두 입력하세요.";
            return;
        }

        // [수정 2] 연속 클릭을 막기 위해 버튼을 비활성화합니다.
        loginButton.interactable = false;
        statusText.text = "로그인 중...";
        StartCoroutine(LoginRequest(userId, password));
    }

    IEnumerator LoginRequest(string userId, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "login");
        form.AddField("userId", userId);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(scriptURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(jsonResponse);

                if (response.status == "success")
                {
                    statusText.text = response.message;
                    Debug.Log("로그인 성공! 데이터 로드 완료.");

                    Debug.Log($"[LoginManager] 저장할 UserID: '{response.data.userId}'");
                    PlayerDataManager.Instance.UserID = response.data.userId;
                    Debug.Log($"[LoginManager] 저장된 UserID: '{PlayerDataManager.Instance.UserID}'");

                    yield return new WaitForSeconds(1);
                    SceneManager.LoadScene("Lobby");
                }
                else
                {
                    statusText.text = "로그인 실패: " + response.message;
                    // [수정 3] 로그인 실패 시 버튼을 다시 활성화합니다.
                    loginButton.interactable = true;
                }
            }
            else
            {
                statusText.text = "네트워크 오류: " + www.error;
                Debug.LogError("Web Request Error: " + www.error);
                // [수정 3] 네트워크 오류 시 버튼을 다시 활성화합니다.
                loginButton.interactable = true;
            }
        }
    }

    private void OnDestroy()
    {
        // OnDestroy에서도 리스너를 제거해주는 것이 좋습니다.
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
        }
    }
}

[System.Serializable]
public class LoginResponse
{
    public string status;
    public string message;
    public PlayerDataFields data;
}

[System.Serializable]
public class PlayerDataFields
{
    public string userId;
    public float maxFishSize;
    public string fishCaughtList;
    public int points;
}