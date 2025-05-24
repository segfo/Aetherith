using UnityEngine;
using UniVRM10;
using Kirurobo;

public class CharacterController : MonoBehaviour
{
    [SerializeField] private VRMLoader vrmLoader;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private SpringBoneExternalForce springBoneExternalForce;
    [SerializeField] private Vector3 offset = new Vector3(-1.5f, -0.15f, 0f);
    [SerializeField] private ShakeDetector shakeDetector;

    public Vrm10Instance vrmInstance { get; private set; }
    private BlinkController blinkController;
    private ArmMotionManager armMotionManager;

    private void Start()
    {
        offset.x = AppConfigManager.Instance.Config.vrm.VrmDisplayOffsetX;
        offset.y = AppConfigManager.Instance.Config.vrm.VrmDisplayOffsetY;
    }
    private void Awake()
    {
        vrmLoader.OnVrmLoaded += OnVrmLoaded;
        chatManager.AppendTextLine("SYSTEM - VRM Loader: VRMモデルを読み込んでいます...");
    }

    private void OnVrmLoaded(GameObject model)
    {
        // VRMモデルのインスタンスを保持しておく。外部から扱えるようにするため。
        vrmInstance = model.GetComponent<Vrm10Instance>();
        // モデルが読み込まれたときの処理
        armMotionManager = model.AddComponent<ArmMotionManager>();
        blinkController = model.AddComponent<BlinkController>();
        Debug.Log("ArmMotionManagerをモデルに追加しました。");
        int vrmLayer = LayerMask.NameToLayer("VRM");
        SetLayerRecursively(model, vrmLayer);
        AdjustCameraToVrm(model);
        chatManager.VrmLoadCompleted();
        springBoneExternalForce.Initialize();
        shakeDetector.OnShaken += OnShaken;
    }

    // キャラクターが振られたときの処理
    void OnShaken()
    {
        chatManager.AppendTextLine(AppConfigManager.Instance.Config.shakeMessage);
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
 
    void AdjustCameraToVrm(GameObject model)
    {
        var animator = model.GetComponent<Animator>();
        if (animator == null) return;
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (head == null || hips == null) return;
        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 1.5f;
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
        ///
        float modelScale = model.transform.lossyScale.y;
        float modelHeight = Mathf.Abs(head.position.y - hips.position.y) / modelScale;
        ///

        /// VRMの腰ボーンを画面の中央に調整する場合は以下をコメントアウトする
        // cam.transform.position = new Vector3(cam.transform.position.x + offset.x, cam.transform.position.y + offset.y, faceDir.z * 2);
        float scaleFactor = AppConfigManager.Instance.Config.vrm.Scale;
        /// VRMの頭の先端を画面の上部に調整する
        cam.transform.position = new Vector3(cam.transform.position.x + offset.x, cam.transform.position.y - cam.orthographicSize + modelHeight+0.5f* scaleFactor, faceDir.z * 2);
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
