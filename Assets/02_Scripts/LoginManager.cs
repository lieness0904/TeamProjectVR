using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro; // TextMeshPro¸¦ »ç¿ëÇÏ±â À§ÇØ ÇÊ¿äÇÕ´Ï´Ù.
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

        // ¾ÆÀÌµð³ª ºñ¹Ð¹øÈ£°¡ ºñ¾îÀÖ´ÂÁö È®ÀÎÇÕ´Ï´Ù.
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            statusText.text = "¾ÆÀÌµð¿Í ºñ¹Ð¹øÈ£¸¦ ¸ðµÎ ÀÔ·ÂÇÏ¼¼¿ä.";
            return;
        }

        statusText.text = "·Î±×ÀÎ Áß...";

        // ÄÚ·çÆ¾À» »ç¿ëÇÏ¿© ºñµ¿±â À¥ ¿äÃ»À» ½ÃÀÛÇÕ´Ï´Ù.
        StartCoroutine(LoginRequest(userId, password));
    }


    /// <param name="userId">»ç¿ëÀÚ ¾ÆÀÌµð</param>
    /// <param name="password">»ç¿ëÀÚ ºñ¹Ð¹øÈ£</param>
    IEnumerator LoginRequest(string userId, string password)
    {
        // À¥¿¡ º¸³¾ µ¥ÀÌÅÍ ¾ç½ÄÀ» ¸¸µì´Ï´Ù.
        WWWForm form = new WWWForm();
        form.AddField("action", "login"); // Apps ScriptÀÇ ¾î¶² ±â´ÉÀ» È£ÃâÇÒÁö ÁöÁ¤
        form.AddField("userId", userId);
        form.AddField("password", password);

        // POST ¹æ½ÄÀ¸·Î À¥ ¿äÃ»À» »ý¼ºÇÏ°í º¸³À´Ï´Ù.
        using (UnityWebRequest www = UnityWebRequest.Post(scriptURL, form))
        {
            yield return www.SendWebRequest(); // ¿äÃ»ÀÌ ³¡³¯ ¶§±îÁö ¿©±â¼­ ´ë±âÇÕ´Ï´Ù.

            // À¥ ¿äÃ»¿¡ ¼º°øÇßÀ» °æ¿ì
            if (www.result == UnityWebRequest.Result.Success)
            {
                // ¼­¹ö·ÎºÎÅÍ ¹ÞÀº JSON Çü½ÄÀÇ ÀÀ´ä ÅØ½ºÆ®¸¦ ÆÄ½Ì(ÇØ¼®)ÇÕ´Ï´Ù.
                string jsonResponse = www.downloadHandler.text;
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(jsonResponse);

                // ¼­¹ö¿¡¼­ "¼º°ø" ÀÀ´äÀ» º¸³ÂÀ» °æ¿ì
                if (response.status == "success")
                {
                    statusText.text = response.message; // "Login successful" ¶Ç´Â "New user registered"
                    Debug.Log("·Î±×ÀÎ ¼º°ø! µ¥ÀÌÅÍ ·Îµå ¿Ï·á.");

                    // DontDestroyOnLoad·Î ¼³Á¤µÈ PlayerData ½Ì±ÛÅæ ÀÎ½ºÅÏ½º¿¡ ÇÃ·¹ÀÌ¾î Á¤º¸¸¦ ÀúÀåÇÕ´Ï´Ù.

                    // 1ÃÊ ÈÄ ·Îºñ ¾ÀÀ¸·Î ÀÌµ¿ÇÕ´Ï´Ù.
                    yield return new WaitForSeconds(1);
                    SceneManager.LoadScene("Lobby");
                }
                else // ¼­¹ö¿¡¼­ "½ÇÆÐ" ÀÀ´äÀ» º¸³ÂÀ» °æ¿ì (¿¹: ºñ¹Ð¹øÈ£ ¿À·ù)
                {
                    statusText.text = "·Î±×ÀÎ ½ÇÆÐ: " + response.message;
                }
            }
            else // ³×Æ®¿öÅ© ¿¬°á ÀÚÃ¼¿¡ ½ÇÆÐÇßÀ» °æ¿ì
            {
                statusText.text = "³×Æ®¿öÅ© ¿À·ù: " + www.error;
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