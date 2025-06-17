using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChatUIController : MonoBehaviour,ITextWriterTarget
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GameObject chatWindow;
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private InputField inputField;
    [SerializeField] private GameObject inputWindow;
    [SerializeField] private ScrollRect responseArea;
    [SerializeField] private TypewriterEffect typewriterSystem;
    [SerializeField] private TypewriterEffect typewriterBot;

    bool isMinimized = false;
    bool ime = false;
    Keyboard _keyboard;
    Vector2 localPos;

    private void Awake()
    {
        if (typewriterSystem == null||typewriterBot == null)
        {
            Debug.LogError("TypewriterEffect is not assigned in ChatUIController.");
        }
        else
        {
            typewriterSystem.Init(this);
            typewriterBot.Init(this);
        }
        ApplyConfig(AppConfigManager.Instance.Config);
        Input.imeCompositionMode = IMECompositionMode.On;
        _keyboard = Keyboard.current;
        _keyboard.SetIMEEnabled(true);
        AppConfigManager.Instance.OnConfigUpdated += (config) => {
            ApplyConfig(config);
        };
    }
    void ApplyConfig(AppConfig config)
    {
        typewriterBot.SetTypingInterval(AppConfigManager.Instance.Config.botChatTypingInterval);
        typewriterSystem.SetTypingInterval(AppConfigManager.Instance.Config.systemChatTypingInterval);
        // inputFieldの色を表示する
        responseArea.GetComponent<Image>().color = CharacterRender.ParseHexColor(
             config.chatWindowBgRGBA,
             AppConfigManager.FallbackInstance.Config.chatWindowBgRGBA
         );
        inputField.GetComponent<Image>().color = CharacterRender.ParseHexColor(
            config.chatInputWindowBgRGBA,
            AppConfigManager.FallbackInstance.Config.chatInputWindowBgRGBA
        );
    }

    void AdjustChatUi()
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

        // ワールド空間での水平軸のオフセット
        float horizontalOffset = 0.25f * scaleFactor;
        // ワールド空間での垂直軸のオフセット
        float verticalOffsetWorld = AppConfigManager.Instance.Config.vrm.VrmDisplayOffsetY;

        // 頭の位置からX方向に一定距離ずらす（カメラの右方向基準で）
        Vector3 baseWorldPos = headTransform.position + Camera.main.transform.right * horizontalOffset;

        // Yオフセットありの worldPos
        Vector3 worldPosWithYOffset = baseWorldPos + Camera.main.transform.up * verticalOffsetWorld;

        // それぞれスクリーン座標に変換
        Vector3 baseScreenPos = Camera.main.WorldToScreenPoint(baseWorldPos);
        Vector3 adjustedScreenPos = Camera.main.WorldToScreenPoint(worldPosWithYOffset);

        // Y軸の差分だけ補正して、常に「上辺にUIの上端がつく」構成に
        float deltaY = adjustedScreenPos.y - baseScreenPos.y;
        Vector3 finalScreenPos = baseScreenPos;
        finalScreenPos.y = Screen.height - deltaY;  // ← 最上部からの相対補正

        // スクリーン → RectTransformローカル座標へ変換
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, finalScreenPos, Camera.main, out localPos);
        localPos.x += AppConfigManager.Instance.Config.vrm.ChatInputOffsetX * scaleFactor; // X軸のオフセット
        localPos.y += AppConfigManager.Instance.Config.vrm.ChatInputOffsetY * scaleFactor; // Y軸のオフセット
        // ローカル座標を反映
        rectTransform.localPosition = localPos;

        // カメラの向きに合わせてUIを回転
        transform.rotation = Camera.main.transform.rotation;
    }
    void Update()
    {
        AdjustChatUi();
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
                // 先頭一致
                if (inputField.text.StartsWith("/exit")) {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;//ゲームプレイ終了
#else
                // 実行環境での終了処理
                    Debug.Log("Exiting application via /exit command.");
                    Application.Quit();//ゲームプレイ終了
#endif
                    return;
                }
                // Shift+Enter → 送信
                onInputTextSubmit(inputField.text);
                // 再フォーカス
                inputField.ActivateInputField();
            }
        }
    }

    // VRMモデルのバウンディングボックスからY座標を取得する
    float GetVrmVisualHeight(GameObject root)
    {
        // SkinnedMeshRendererをすべて取得
        var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (renderers.Length == 0)
            return 0f;

        // 全Rendererのバウンディングボックスを統合
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // 高さ（Y座標）を返す
        return bounds.max.y;
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
        Canvas.ForceUpdateCanvases(); // レイアウト更新
        responseArea.verticalNormalizedPosition = 0f; // スクロールを一番下に移動
    }
    public void AppendText(string text)
    {
        tmpText.text += text;
        Canvas.ForceUpdateCanvases(); // レイアウト更新
        responseArea.verticalNormalizedPosition = 0f; // スクロールを一番下に移動        
    }
    public void AppendTextLine(string text)
    {
        tmpText.text += text + "\n";
        Canvas.ForceUpdateCanvases(); // レイアウト更新
        responseArea.verticalNormalizedPosition = 0f; // スクロールを一番下に移動
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
    public Task StartTypingSystem(string text)
    {
        return typewriterSystem?.StartTyping(text);
    }


    public Task StartTypingAppendSystem(string text)
    {
        return typewriterSystem?.StartTypingAppend(text);
    }

    public void StopTypingSystem()
    {
        typewriterSystem?.StopTyping();
    }

    public Task StartTyping(string text)
    {
        return typewriterBot?.StartTyping(text);
    }

    public Task StartTypingAppend(string text)
    {
        return typewriterBot?.StartTypingAppend(text);
    }

    public void StopTyping()
    {
        typewriterBot?.StopTyping();
    }
}
