using UnityEngine;
using TMPro; // TextMeshPro의 InputField를 사용하기 위해 필요

// TMP_InputField에 이 스크립트를 추가하면 됩니다.
[RequireComponent(typeof(TMP_InputField))]
public class OculusKeyboardManager : MonoBehaviour
{
    private TMP_InputField inputField;
    private TouchScreenKeyboard virtualKeyboard;

    void Start()
    {
        // 이 스크립트가 붙어있는 게임 오브젝트의 TMP_InputField 컴포넌트를 가져옵니다.
        inputField = GetComponent<TMP_InputField>();

        // InputField가 선택되었을 때(클릭되었을 때) 키보드를 여는 함수를 연결합니다.
        inputField.onSelect.AddListener(x => OpenKeyboard());
    }

    // 키보드를 여는 함수
    public void OpenKeyboard()
    {
        // Oculus 시스템 키보드를 엽니다.
        // TouchScreenKeyboard.Open(기존 텍스트, 키보드 타입, ...);
        virtualKeyboard = TouchScreenKeyboard.Open(inputField.text, TouchScreenKeyboardType.Default);
    }

    void Update()
    {
        // 키보드가 열려있다면, 키보드에 입력된 내용을 InputField에 실시간으로 반영합니다.
        if (virtualKeyboard != null)
        {
            inputField.text = virtualKeyboard.text;

            // 키보드가 닫혔는지 확인하고, 닫혔으면 참조를 null로 만들어줍니다.
            if (virtualKeyboard.status != TouchScreenKeyboard.Status.Visible)
            {
                virtualKeyboard = null;
            }
        }
    }
}