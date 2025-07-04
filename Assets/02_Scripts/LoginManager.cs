using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro; // TextMeshPro를 사용하기 위해 필요합니다.
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{

    private string scriptURL = "https://script.google.com/macros/s/AKfycbxsVIFFP0aRSLTljhqnSI0KAso8jv3Hx3UPdIiWsl1UynSyyk1EVABCf2Fpz6WwzcNn/exec";


    [Header("UI Elements")]
    public TMP_InputField idInputField;
    public TMP_InputField passwordInputField;
    public Button loginButton;
    public TextMeshProUGUI statusText;

    void Start()
    {

        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClick);
        }
    }


    public void OnLoginButtonClick()
    {
        string userId = idInputField.text;
        string password = passwordInputField.text;

        // 아이디나 비밀번호가 비어있는지 확인합니다.
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            statusText.text = "아이디와 비밀번호를 모두 입력하세요.";
            return;
        }

        statusText.text = "로그인 중...";

        // 코루틴을 사용하여 비동기 웹 요청을 시작합니다.
        StartCoroutine(LoginRequest(userId, password));
    }


    /// <param name="userId">사용자 아이디</param>
    /// <param name="password">사용자 비밀번호</param>
    IEnumerator LoginRequest(string userId, string password)
    {
        // 웹에 보낼 데이터 양식을 만듭니다.
        WWWForm form = new WWWForm();
        form.AddField("action", "login"); // Apps Script의 어떤 기능을 호출할지 지정
        form.AddField("userId", userId);
        form.AddField("password", password);

        // POST 방식으로 웹 요청을 생성하고 보냅니다.
        using (UnityWebRequest www = UnityWebRequest.Post(scriptURL, form))
        {
            yield return www.SendWebRequest(); // 요청이 끝날 때까지 여기서 대기합니다.

            // 웹 요청에 성공했을 경우
            if (www.result == UnityWebRequest.Result.Success)
            {
                // 서버로부터 받은 JSON 형식의 응답 텍스트를 파싱(해석)합니다.
                string jsonResponse = www.downloadHandler.text;
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(jsonResponse);

                // 서버에서 "성공" 응답을 보냈을 경우
                if (response.status == "success")
                {
                    statusText.text = response.message; // "Login successful" 또는 "New user registered"
                    Debug.Log("로그인 성공! 데이터 로드 완료.");

                    // DontDestroyOnLoad로 설정된 PlayerData 싱글톤 인스턴스에 플레이어 정보를 저장합니다.

                    // 1초 후 로비 씬으로 이동합니다.
                    yield return new WaitForSeconds(1);
                    SceneManager.LoadScene("TestScene");
                }
                else // 서버에서 "실패" 응답을 보냈을 경우 (예: 비밀번호 오류)
                {
                    statusText.text = "로그인 실패: " + response.message;
                }
            }
            else // 네트워크 연결 자체에 실패했을 경우
            {
                statusText.text = "네트워크 오류: " + www.error;
                Debug.LogError("Web Request Error: " + www.error);
            }
        }
    }


    private void OnDestroy()
    {

        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginButtonClick);
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