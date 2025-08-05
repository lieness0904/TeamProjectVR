using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class OnScreenKeyboardInputfield : MonoBehaviour, IPointerDownHandler
{
    public OnScreenKeyboard targetOnScreenKeyboard;
    private TMP_InputField _inputField;
    public string inputtedString = "";

    private void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();
        _inputField.shouldHideMobileInput = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetOnScreenKeyboard)
        {
            targetOnScreenKeyboard.ShowKeyboard(_inputField, this);
        }
    }

    public void SaveInputedString(string value)
    {
        inputtedString = value;
    }
}
