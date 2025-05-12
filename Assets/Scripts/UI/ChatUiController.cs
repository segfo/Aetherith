using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ChatUIController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GameObject chatWindow;
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private InputField inputField;
    [SerializeField] private Vector3 offset = new Vector3(-0.25f, 1.6f, 0f); // ���f���̉�
    bool isMinimized = false;
    bool ime = false;
    Keyboard _keyboard;

    private void Awake()
    {
        Input.imeCompositionMode = IMECompositionMode.On;
        _keyboard = Keyboard.current;
        _keyboard.SetIMEEnabled(true);
    }
    void Update()
    {
        if (characterController == null || characterController.vrmInstance == null) return;

        Transform modelTransform = characterController.vrmInstance.transform;
        transform.position = modelTransform.position + modelTransform.rotation * offset;
        transform.rotation = Camera.main.transform.rotation; // UI�̓J�����Ɍ�����
        // InputTextField�̏���
        // �t�H�[�J�X���Ȃ� or IME�ϊ��� �͖���
        if (!inputField.isFocused || !string.IsNullOrEmpty(Input.compositionString))
        {
            return;
        }

        // Return �L�[�������ꂽ�u��
        if (Input.GetKeyDown(KeyCode.Return))
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (shift)
            {
                // Shift+Enter �� ���M
                onInputTextSubmit(inputField.text);
                // �ăt�H�[�J�X
                inputField.ActivateInputField();
            }
        }
    }
    public void ActivateInputField()
    {
        inputField.ActivateInputField();
    }
    public void ToggleMinimize()
    {
        isMinimized = !isMinimized;
        chatWindow.gameObject.SetActive(!isMinimized);
    }
    public bool IsImeEnabled()
    {
        return ime;
    }
    public void ImeDisabled()
    {
        ime = false;
    }
    public void SetEnable(bool en)
    {
        chatWindow.SetActive(en);
    }
    public void SetText(string text)
    {
        tmpText.text = text;
    }
    public void AppendText(string text)
    {
        tmpText.text += text;
    }
    public string GetInputField()
    {
        return inputField.text;
    }
    public void ClearInputField()
    {
        inputField.text = "";
    }
    public void SetInputText(string text)
    {
        inputField.text = text;
    }
    private UnityEngine.Events.UnityAction<string> onInputTextSubmit;
    public void AddInputFieldEventHandler(UnityEngine.Events.UnityAction<string> onSubmit)
    {
        Debug.Log("ChatUIController - OnEndEdit�C�x���g�n���h�����ݒ肳��܂����B");
        //inputField.onEndEdit.AddListener(onSubmit);
        onInputTextSubmit = onSubmit;

    }
}
