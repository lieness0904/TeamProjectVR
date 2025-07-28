using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    private string scriptURL = "https://script.google.com/macros/s/AKfycbxbEbhCsVmqMWCuZOtPEcfGFperFw3nRjDw5OsECes9IFx2pbeXZMFmMAS0E20XUVy3/exec";

    [Header("XR Origin 프리팹 설정")]
    // --- [수정 1] XR Origin 프리팹을 할당받을 변수 ---
    public GameObject xrOriginPrefab;

    [Header("UI Elements")]
    public TMP_InputField idInputField;
    public TMP_InputField passwordInputField;
    public Button loginButton;
    public TextMeshProUGUI statusText;
    public Button customButton;

    private bool isLoggingIn = false;
    void Start()
    {
        // --- [수정 2] 씬 시작 시 XR Origin 프리팹이 할당되어 있으면 생성 ---
        if (xrOriginPrefab != null)
        {
            Instantiate(xrOriginPrefab);
        }
        // ---------------------------------------------------------

        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginButtonClick); // 리스너 중복 제거
            loginButton.onClick.AddListener(OnLoginButtonClick);
        }

        if (customButton != null)
        {
            customButton.onClick.RemoveListener(OnCustomButtonClick);
            customButton.onClick.AddListener(OnCustomButtonClick);
        }
    }


    public void OnLoginButtonClick()
    {
        if (isLoggingIn) return; // 중복 방지
        isLoggingIn = true;

        string userId = idInputField.text;
        string password = passwordInputField.text;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            statusText.text = "아이디와 비밀번호를 모두 입력하세요.";
            isLoggingIn = false;
            return;
        }

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

                    PlayerDataManager.Instance.UserID = response.data.userId;
                    PlayerDataManager.Instance.Points = response.data.points;

                    if (!string.IsNullOrEmpty(response.data.inventory))
                    {
                        PlayerDataManager.Instance.InventoryJson = response.data.inventory;
                        InventorySyncManager.Instance.LoadInventoryFromServer(response.data.userId);
                    }
                    else
                    {
                        Debug.LogWarning("서버에서 인벤토리 데이터가 비어 있음 (신규 유저 또는 초기 상태)");
                    }

                    // 외형 불러오기
                    CustomizationDataLoader.LoadCustomizationFromSheet(userId, data =>
                    {
                        PlayerDataManager.Instance.CustomizationData = data;
                        Debug.Log("커스터마이징 데이터 로드 완료");
                    });

                    yield return new WaitForSeconds(1);
                    SceneManager.LoadScene("HomeScene");
                }
                else
                {
                    statusText.text = "로그인 실패: " + response.message;
                }
            }
            else
            {
                statusText.text = "네트워크 오류: " + www.error;
                Debug.LogError("Web Request Error: " + www.error);
            }
        }

        isLoggingIn = false;
    }

    private void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginButtonClick);
        }
    }
    public void OnCustomButtonClick()
    {
        string userId = idInputField.text;

        if (string.IsNullOrEmpty(userId))
        {
            statusText.text = "아이디를 입력하세요.";
            return;
        }

        statusText.text = "외형 정보 불러오는 중...";

        CustomizationDataLoader.LoadCustomizationFromSheet(userId, data =>
        {
            PlayerDataManager.Instance.UserID = userId;
            PlayerDataManager.Instance.CustomizationData = data;
            Debug.Log("커스터마이징 데이터 로드 완료 (Custom 버튼)");

            SceneManager.LoadScene("CustomizationScene");
        });
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
    public string inventory;
}