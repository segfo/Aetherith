using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChatUIController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GameObject chatWindow;
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private InputField inputField;
    [SerializeField] private GameObject inputWindow;
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

        var animator = characterController.vrmInstance.GetComponent<Animator>();
        var headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
        if (headTransform == null) return;

        RectTransform rectTransform = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float scaleFactor = AppConfigManager.Instance.Config.vrm.Scale; // スケールを取得

        // UIの高さ（Canvas基準のピクセル単位）
        float windowHeight = rectTransform.rect.height;
        float windowWidth = rectTransform.rect.width;

        // UIの上辺を頭の上辺に合わせるため、スクリーンY座標で高さの半分だけずらす
        float screenYOffset = windowHeight * 0.5f;

        // ワールド空間での横方向のずれ（スケールに依存しない距離）
        float horizontalOffset = 0.2f*scaleFactor; // 10cm右へ

        // 頭の位置からX方向に一定距離ずらす（カメラの右方向基準で）
        Vector3 worldPos = headTransform.position + Camera.main.transform.right * horizontalOffset;

        // ワールド → スクリーン空間へ
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // スクリーンYをウィンドウ高さ分だけ上にオフセット（上辺合わせ）
        screenPos.y += screenYOffset;

        // スクリーン → RectTransformローカル座標へ変換
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, Camera.main, out localPos);

        // ローカル座標を反映
        rectTransform.localPosition = localPos;

        // カメラの向きに合わせてUIを回転
        transform.rotation = Camera.main.transform.rotation;
        // InputTextFieldの処理
        // フォーカスがない or IME変換中 は無視
        if (!inputField.isFocused || !string.IsNullOrEmpty(Input.compositionString))
        {
            return;
        }

        // Return キーが押された瞬間
        if (Input.GetKeyDown(KeyCode.Return))
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (shift)
            {
                // Shift+Enter → 送信
                onInputTextSubmit(inputField.text);
                // 再フォーカス
                inputField.ActivateInputField();
            }
        }
    }
    public void OnDestroy()
    {
        inputField.DeactivateInputField();
    }
    public void ActivateInputField()
    {
        inputField.ActivateInputField();
    }
    public void InputFieldSetEnable(bool enable)
    {
        // inputFieldの親ゲームオブジェクトの表示・非表示
        inputWindow.gameObject.GetComponent<Canvas>().gameObject.SetActive(enable);
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
    public void AppendTextLine(string text)
    {
        tmpText.text += text + "\n";
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
        onInputTextSubmit = onSubmit;

    }
}
