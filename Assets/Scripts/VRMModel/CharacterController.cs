using LLMUnity;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UniVRM10;

public class CharacterController : MonoBehaviour, IVRMCharacter
{
    [SerializeField] private VRMLoader vrmLoader;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private SpringBoneExternalForce springBoneExternalForce;
    [SerializeField] private ShakeDetector shakeDetector;
    [SerializeField] private ShakeDizzyAnimationPlayer shakeDizzyAnimationPlayer;
    [SerializeField] private ThinkingMotionManager thinkingAnimation;
    [SerializeField] private CharacterBinding characterBinding;

    public Vrm10Instance vrmInstance { get; private set; } = null;
    public string Name { get; private set; } = string.Empty;
    public Animator Animator { get; private set; } = null;
    public bool ready { get; private set; } = false;
    public int vrmFileConfigSelector => GetComponent<VRMLoader>().vrmFileConfigSelector;

    public BlinkController blinkController { get; private set; } = null;
    private ArmMotionManager armMotionManager;
    private MainThreadDispatcher mainThreadDispatcher;
    private GameObject model;
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    private void Start()
    {
        mainThreadDispatcher = MainThreadDispatcher.Instance;
        initialCameraPosition = Camera.main.transform.position;
        initialCameraRotation = Camera.main.transform.rotation;
        // まずはこいつを初期化しておく
        characterBinding.SetCharacter(this);
        AppConfigManager.Instance.OnConfigUpdated += OnConfigUpdated;
        vrmLoader.BeforeVrmUnload += BeforeVrmUnload;
        vrmLoader.OnVrmUnload += OnVrmUnload;
        vrmLoader.OnVrmUnload += characterBinding.GetOnVrmUnload();
        vrmLoader.BeforeVrmUnload += characterBinding.GetBeforeVrmUnload();
        vrmLoader.OnVrmLoaded += OnVrmLoaded;
        vrmLoader.OnVrmLoadError += (path) =>
        {
            chatManager.TypingAppendTextSystem($"SYSTEM: VRMモデルのロードに失敗しました。設定を見直してください。\n---パス---\n{path.Replace("\\", "/")}\n-----\n");
        };
        chatManager.TypingAppendTextSystem("SYSTEM: VRMモデルを読み込んでいます...\n");
    }
    private void OnConfigUpdated(AppConfig config)
    {
        if (model == null) { return; }
        // スケールの調整
        float scale = config.vrm[vrmFileConfigSelector].Scale;
        model.transform.localScale = new Vector3(scale, scale, scale);
        // カメラの調整
        AdjustCameraFromConfig(config);
        // 瞬きの設定を更新する
        blinkController.SetBlinkEnabled(!AppConfigManager.Instance.Config.vrm[vrmFileConfigSelector].blinkDisable, 0);
    }

    private void AdjustCameraFromConfig(AppConfig config)
    {
        var animator = model.GetComponent<Animator>();
        if (animator == null) { Debug.LogError("animator is null"); return; }
        this.Animator = animator;
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (head == null || hips == null) return;
        var cam = Camera.main;
        // カメラの設定を初期化する
        cam.transform.position = camTransformPos;
        // カメラの中央を補正する
        Vector3 centerPos = new Vector3(camTransformPos.x, hips.position.y, camTransformPos.z);
        hips.position -= hips.position - centerPos;
        float scaleFactor = config.vrm[vrmFileConfigSelector].Scale;
        float modelScale = model.transform.lossyScale.y;
        float modelHeight = Mathf.Abs(head.position.y - hips.position.y) / modelScale;
        Vector3 faceDir = head.forward.normalized;
        // 実際のカメラ調整処理
        CameraConfigApply(cam, modelHeight, scaleFactor, faceDir);
    }
    public void BeforeVrmUnload()
    {
        Debug.Log("VRMモデルのアンロードを開始します。");
        chatManager.TypingTextSystem($"SYSTEM: VRMモデルのリロードを行います。\n");
        ready = false;
    }
    public void OnVrmUnload()
    {
        Transform cameraTransform = Camera.main.transform;
        cameraTransform.position = initialCameraPosition;
        cameraTransform.rotation = initialCameraRotation;
    }

    private void OnVrmLoaded(GameObject model)
    {
        // VRMモデルのインスタンスを保持しておく。外部から扱えるようにするため。
        vrmInstance = model.GetComponent<Vrm10Instance>();
        // モデルが読み込まれたときの処理
        armMotionManager = model.AddComponent<ArmMotionManager>();
        blinkController = model.AddComponent<BlinkController>();
        blinkController.InitController(AppConfigManager.Instance.Config.vrm[vrmFileConfigSelector].blinkDisable);
        // 瞬きの設定を読み込む（BlinkDisabledがfalseの時は瞬きをするので論理反転しておく）
        Debug.Log("ArmMotionManagerをモデルに追加しました。");
        int vrmLayer = LayerMask.NameToLayer("VRM");
        SetLayerRecursively(model, vrmLayer);

        // animatorを設定して保存する
        var animator = model.GetComponent<Animator>();
        Animator = animator;
        if (animator == null) return;
        AdjustCameraToVrmInit(model,animator);
        shakeDetector.OnShaken += OnShaken;
        thinkingAnimation.SetAnimator(Animator);
        chatManager.VrmLoadCompleted();
        springBoneExternalForce.Initialize();
        ready = true;
    }

    // キャラクターが振られたときの処理
    void OnShaken()
    {
        // 初期化が済んでいなければ追記モード+リップシンクなしで動作させる
        if (chatManager.InitializeState == ChatManagerInitializeState.Ready)
        {
            chatManager.TypingText(AppConfigManager.Instance.Config.chat.shakeMessage + "\n");
        }
        else if (chatManager.InitializeState == ChatManagerInitializeState.InitializePending)
        {
            chatManager.TypingAppendTextSystem(AppConfigManager.Instance.Config.chat.shakeMessage + "\n");
        }
        if (chatManager.InitializeState != ChatManagerInitializeState.Initialized) {
            // 表情を全部戻す
            ExpressionController.Instance.ResetVrmExpression();
            // 瞬きを停止して、目を閉じる
            blinkController?.SetBlinkEnabled(false, 1f);
            float waitTime = shakeDizzyAnimationPlayer.PlayDizzy(vrmInstance.GetComponent<Animator>());
            // ゆっくり目を開ける
            Task.Run(async () =>
            {
                await Task.Delay((int)(waitTime * 1000));

                mainThreadDispatcher.Enqueue(() =>
                {
                    ExpressionController.Instance.StartExpressionFadeout(0.0f, 0.15f);
                });
                await Task.Delay(1600); // 1.6秒待つ
                blinkController?.SetBlinkEnabled(true, 0f);
            });
        }
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    Vector3 camTransformPos = Vector3.zero;
    void AdjustCameraToVrmInit(GameObject model,Animator animator)
    {
        this.model = model;
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (head == null || hips == null) return;
        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 1.5f;
        camTransformPos = cam.transform.position;
        Vector3 centerPos = new Vector3(cam.transform.position.x, cam.transform.position.y, hips.position.z);
        hips.position -= hips.position - centerPos;
        // VRMの向いている方向を取得
        Vector3 faceDir = head.forward.normalized;
        // カメラの向いている方向をVRMの逆向きにして、2m程度引く
        cam.transform.rotation = Quaternion.LookRotation(-faceDir, Vector3.up);
        Vector3 angles = cam.transform.rotation.eulerAngles;
        angles.x = 0f;
        angles.z = 0f;
        cam.transform.rotation = Quaternion.Euler(angles);
        // ここからVRMの頭の先端を画面の上部に調整するための計算
        float modelScale = model.transform.lossyScale.y;
        float modelHeight = Mathf.Abs(head.position.y - hips.position.y) / modelScale;
        /// VRMの腰ボーンを画面の中央に調整する場合は以下をコメントアウトする
        // cam.transform.position = new Vector3(cam.transform.position.x + offset.x, cam.transform.position.y + offset.y, faceDir.z * 2);
        float scaleFactor = AppConfigManager.Instance.Config.vrm[vrmFileConfigSelector].Scale;

        /// VRMの頭の先端を画面の上部に調整する
        CameraConfigApply(cam,modelHeight,scaleFactor,faceDir);
    }

    void CameraConfigApply(Camera cam,float modelHeight,float scaleFactor,Vector3 faceDir)
    {
        cam.transform.position = new Vector3(cam.transform.position.x + -1.5f + AppConfigManager.Instance.Config.vrm[vrmFileConfigSelector].VrmDisplayOffsetX,
            cam.transform.position.y - cam.orthographicSize + 0.25f * scaleFactor + modelHeight  * scaleFactor + AppConfigManager.Instance.Config.vrm[vrmFileConfigSelector].VrmDisplayOffsetY,
            faceDir.z * 2);
    }

    // 深い階層から名前でTransformを探す
    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name)) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    // 外部から呼べる制御メソッド(API)
    public void SetBlinking(bool enabled,float open)
    {
        blinkController?.SetBlinkEnabled(enabled, open);
    }
    public void DoThinking()
    {
        // 思考モーションを再生する
        thinkingAnimation?.DoThinking();
    }
    public async Task DoneThinking()
    {
        // 思考モーションを停止する
        await thinkingAnimation?.DoneThinking();
    }
    // 手を振るAPI（現在未実装）
    public void PlayWaveHand()
    {
        armMotionManager?.PlayWaveHand();
    }

    public void SetExpression(ExpressionKey key, float weight)
    {
        var vrm = vrmLoader.LoadedModel.GetComponent<Vrm10Instance>();
        vrm?.Runtime.Expression.SetWeight(key, weight);
    }

}
