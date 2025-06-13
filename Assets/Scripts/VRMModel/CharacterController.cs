using UnityEngine;
using UniVRM10;
using Kirurobo;
using System.Threading.Tasks;
using UnityEngine.Localization.Settings;
using System;

public class CharacterController : MonoBehaviour
{
    [SerializeField] private VRMLoader vrmLoader;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private SpringBoneExternalForce springBoneExternalForce;
    [SerializeField] private ShakeDetector shakeDetector;
    [SerializeField] private ShakeDizzyAnimationPlayer shakeDizzyAnimationPlayer;
    [SerializeField] private LipSyncSimulator lipSyncSimulator;

    public Vrm10Instance vrmInstance { get; private set; }
    private BlinkController blinkController;
    private ArmMotionManager armMotionManager;
    private MainThreadDispatcher mainThreadDispatcher;
    private GameObject model;
    private void Start()
    {
        mainThreadDispatcher = MainThreadDispatcher.Instance;
    }

    private void Awake()
    {
        vrmLoader.OnVrmLoaded += OnVrmLoaded;
        chatManager.TypingAppendTextSystem("SYSTEM: VRMモデルを読み込んでいます...\n");
        AppConfigManager.Instance.OnConfigUpdated += OnConfigUpdated;
    }

    private void OnConfigUpdated(AppConfig config)
    {
        if (model == null) { return; }
        // スケールの調整
        float scale = config.vrm.Scale;
        model.transform.localScale = new Vector3(scale, scale, scale);
        // カメラの調整
        AdjustCameraFromConfig(config);
    }

    private void AdjustCameraFromConfig(AppConfig config)
    {
        var animator = model.GetComponent<Animator>();
        if (animator == null) return;
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (head == null || hips == null) return;
        var cam = Camera.main;
        // カメラの設定を初期化する
        cam.transform.position = camTransformPos;
        // カメラの中央を補正する
        Vector3 centerPos = new Vector3(camTransformPos.x, hips.position.y, camTransformPos.z);
        hips.position -= hips.position - centerPos;
        float scaleFactor = AppConfigManager.Instance.Config.vrm.Scale;
        float modelScale = model.transform.lossyScale.y;
        float modelHeight = Mathf.Abs(head.position.y - hips.position.y) / modelScale;
        Vector3 faceDir = head.forward.normalized;
        // 実際のカメラ調整処理
        CameraConfigApply(cam, modelHeight, scaleFactor, faceDir);
    }

    private void OnVrmLoaded(GameObject model)
    {
        // VRMモデルのインスタンスを保持しておく。外部から扱えるようにするため。
        vrmInstance = model.GetComponent<Vrm10Instance>();
        // モデルが読み込まれたときの処理
        armMotionManager = model.AddComponent<ArmMotionManager>();
        blinkController = model.AddComponent<BlinkController>();
        // 瞬きの設定を読み込む（BlinkDisabledがfalseの時は瞬きをするので論理反転しておく）
        Debug.Log("ArmMotionManagerをモデルに追加しました。");
        int vrmLayer = LayerMask.NameToLayer("VRM");
        SetLayerRecursively(model, vrmLayer);
        AdjustCameraToVrmInit(model);
        chatManager.VrmLoadCompleted();
        springBoneExternalForce.Initialize();
        shakeDetector.OnShaken += OnShaken;
    }

    // キャラクターが振られたときの処理
    void OnShaken()
    {
        // 表情を全部戻す
        ExpressionController.Instance.ResetVrmExpression();
        // 瞬きを停止して、目を閉じる
        blinkController?.SetBlinkEnabled(false, 1f);

        float waitTime = shakeDizzyAnimationPlayer.PlayDizzy(vrmInstance.GetComponent<Animator>());
        // 初期化が済んでいなければ追記モードで書く
        if (chatManager.Initialized)
        {
            chatManager.TypingText(AppConfigManager.Instance.Config.shakeMessage + "\n");
        } else { 
            chatManager.TypingAppendText(AppConfigManager.Instance.Config.shakeMessage + "\n"); 
        }
        mainThreadDispatcher.Enqueue(() =>
        {
            lipSyncSimulator.LipSyncStart();
            lipSyncSimulator.SpeakText(AppConfigManager.Instance.Config.shakeMessage);
            lipSyncSimulator.LipSyncEnd();
        });
        // ゆっくり目を開ける
        Task.Run(async () =>
        {
            await Task.Delay((int)(waitTime * 1000));

            mainThreadDispatcher.Enqueue(() =>
            {
                ExpressionController.Instance.StartExpressionFadeout(0.0f,0.15f);
            });
            await Task.Delay(1600); // 1.6秒待つ
            blinkController?.SetBlinkEnabled(true, 0f);
        });
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
    void AdjustCameraToVrmInit(GameObject model)
    {
        this.model = model;
        var animator = model.GetComponent<Animator>();
        if (animator == null) return;
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
        ///

        /// VRMの腰ボーンを画面の中央に調整する場合は以下をコメントアウトする
        // cam.transform.position = new Vector3(cam.transform.position.x + offset.x, cam.transform.position.y + offset.y, faceDir.z * 2);
        float scaleFactor = AppConfigManager.Instance.Config.vrm.Scale;

        /// VRMの頭の先端を画面の上部に調整する
        CameraConfigApply(cam,modelHeight,scaleFactor,faceDir);
    }

    void CameraConfigApply(Camera cam,float modelHeight,float scaleFactor,Vector3 faceDir)
    {
        cam.transform.position = new Vector3(cam.transform.position.x + -1.5f + AppConfigManager.Instance.Config.vrm.VrmDisplayOffsetX,
            cam.transform.position.y - cam.orthographicSize + 0.25f * scaleFactor + modelHeight  * scaleFactor + AppConfigManager.Instance.Config.vrm.VrmDisplayOffsetY,
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

    // 外部から呼べる制御メソッド
    public void SetBlinking(bool enabled,float open)
    {
        blinkController?.SetBlinkEnabled(enabled, open);
    }
    public void PlayThinkMotion(bool enabled)
    {
        // 思考モーションを再生する
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
