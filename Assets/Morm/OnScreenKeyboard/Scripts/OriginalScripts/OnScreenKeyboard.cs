using System;
using System.Collections;
using System.Text;
using HangulVirtualKeynoard;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OnScreenKeyboard : MonoBehaviour
{
    public enum CurLang { KR, EN }
    public enum Caps { Caps, Uncaps }

    private string inputtedString = ""; // 여기에 추가
    private string currentString = "";  // 여기에 추가

    private OnScreenKeyboardInputfield currentOskInputfield;
    private TMP_InputField targetInputField;

    [SerializeField] private CurLang curLang;
    private CurLang beforeLang;
    [SerializeField] private Caps curCaps;
    private Caps beforeCaps;

    [SerializeField] private Button bgCloseBtn;
    [SerializeField] private GameObject KrNormal, KrNormalCpas, EnNormal, EnNormalCpas;
    [SerializeField] private Button exitBtn, korBtn, engBtn, capsBtn;

    private Event fakeEvent;

    private void Awake()
    {
        AddListenerToButtons();

        curLang = CurLang.EN;
        beforeLang = CurLang.EN;
        curCaps = Caps.Uncaps;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (targetInputField == null) return;

        // Detect Language
        if (beforeLang != curLang)
        {
            beforeLang = curLang;
            ChangeKeyboardType(curLang, curCaps);
        }

        // Detect Caps
        if (beforeCaps != curCaps)
        {
            beforeCaps = curCaps;
            ChangeKeyboardType(curLang, curCaps);
        }
    }

    private void AddListenerToButtons()
    {
        exitBtn.onClick.AddListener(CloseKeyboard);
        korBtn.onClick.AddListener(() => { curLang = CurLang.KR; });
        engBtn.onClick.AddListener(() => { curLang = CurLang.EN; });
        capsBtn.onClick.AddListener(() =>
        {
            curCaps = (curCaps == Caps.Caps) ? Caps.Uncaps : Caps.Caps;
        });
    }

    public void SendKey(string value)
    {
        if (currentOskInputfield == null || targetInputField == null) return;

        switch (value)
        {
            case "backspace":
                currentOskInputfield.inputtedString =
                    currentOskInputfield.inputtedString.Length > 0
                        ? currentOskInputfield.inputtedString.Substring(0, currentOskInputfield.inputtedString.Length - 1)
                        : "";
                if (curLang == CurLang.KR)
                {
                    fakeEvent = null;
                }
                else
                {
                    fakeEvent = Event.KeyboardEvent("backspace");
                    fakeEvent.keyCode = KeyCode.Backspace;
                }
                break;

            case "space":
                currentOskInputfield.inputtedString += " ";
                fakeEvent = Event.KeyboardEvent("space");
                fakeEvent.keyCode = KeyCode.Space;
                break;

            default:
                if (value.Length == 1)
                {
                    currentOskInputfield.inputtedString += value;
                    fakeEvent = Event.KeyboardEvent(value);
                    fakeEvent.character = value[0];
                    if (char.IsUpper(value[0])) fakeEvent.modifiers |= EventModifiers.Shift;
                }
                else
                {
                    Debug.LogError("Ignoring invalid key: " + value);
                    return;
                }
                break;
        }

        UpdateInputField();
    }

    public void ShowKeyboard(TMP_InputField inputField, OnScreenKeyboardInputfield oskInputField)
    {
        bool wasActive = gameObject.activeSelf;

        targetInputField = inputField;
        currentOskInputfield = oskInputField;

        // 이전에 저장된 값 복원
        inputtedString = currentOskInputfield.inputtedString;
        currentString = currentOskInputfield.inputtedString;
        targetInputField.text = currentOskInputfield.inputtedString;

        if (!wasActive) // 꺼져있던 경우에만 키보드 열기
        {
            gameObject.SetActive(true);
            ChangeKeyboardType(curLang, curCaps);
        }
    }

    public void CloseKeyboard()
    {
        if (currentOskInputfield != null)
            currentOskInputfield.SaveInputedString(inputtedString); // 문자열 저장

        targetInputField = null;
        currentOskInputfield = null;
        gameObject.SetActive(false);
    }

    private void UpdateInputField()
    {
        if (targetInputField == null || currentOskInputfield == null) return;

        if (curLang == CurLang.KR)
        {
            StartCoroutine(HangulDelayUpdate(currentOskInputfield.inputtedString));
        }
        else
        {
            targetInputField.text = currentOskInputfield.inputtedString;
        }

        targetInputField.ForceLabelUpdate();
    }

    private IEnumerator HangulDelayUpdate(string input)
    {
        HangulHelper helper = new HangulHelper();
        StringBuilder sb = new StringBuilder();

        foreach (char c in input)
            helper.Input(sb, c);

        targetInputField.text = sb.ToString();
        yield return null;
    }

    private void ChangeKeyboardType(CurLang curLang, Caps caps)
    {
        KrNormal.SetActive(false);
        KrNormalCpas.SetActive(false);
        EnNormal.SetActive(false);
        EnNormalCpas.SetActive(false);

        this.curLang = curLang;
        this.curCaps = caps;

        switch (curLang)
        {
            case CurLang.KR:
                korBtn.gameObject.SetActive(false);
                engBtn.gameObject.SetActive(true);
                if (caps == Caps.Caps) KrNormalCpas.SetActive(true);
                else KrNormal.SetActive(true);
                break;

            case CurLang.EN:
                korBtn.gameObject.SetActive(true);
                engBtn.gameObject.SetActive(false);
                if (caps == Caps.Caps) EnNormalCpas.SetActive(true);
                else EnNormal.SetActive(true);
                break;
        }
    }
}
