using UnityEngine;
using UniVRM10;
using Kirurobo;

public class CharacterController : MonoBehaviour
{
    [SerializeField] private VRMLoader vrmLoader;

    public Vrm10Instance vrmInstance { get; private set; }
    private BlinkControllerVrm10 blinkController;
    private ArmMotionManager armMotionManager;

    private void Start()
    {
        Debug.Log("CharacterController - VRMLoaderからの通知を待機中...");
    }
    private void Awake()
    {
        vrmLoader.OnModelLoaded += OnModelReady;
    }

    private void OnModelReady(GameObject model)
    {
        // VRMモデルのインスタンスを保持しておく。外部から扱えるようにするため。
        vrmInstance = model.GetComponent<Vrm10Instance>();
        // モデルが読み込まれたときの処理
        armMotionManager = model.AddComponent<ArmMotionManager>();
        blinkController = model.AddComponent<BlinkControllerVrm10>();
        Debug.Log("ArmMotionManagerをモデルに追加しました。");
        // モデルの位置を調整(Y軸を中心に180度回転する)
        int vrmLayer = LayerMask.NameToLayer("VRM");
        SetLayerRecursively(model, vrmLayer);
        //AdjustModelScaleAndPosition(model);
        AdjustCameraToVrm(model);
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void AdjustCameraToVrm(GameObject model)
    {
        var animator = model.GetComponent<Animator>();
        if (animator == null) return;
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null) return;
        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 1.5f;
        Debug.Log(head.forward.normalized);

        Vector3 faceDir = head.forward.normalized;

        // カメラの位置を調整する
        cam.transform.position = head.position + faceDir * 2 + Vector3.up * 0.2f;
        Vector3 pos = cam.transform.position;

        // ２）カメラをVRMの方向へ向ける
        Vector3 flatDir = (head.position - cam.transform.position);
        flatDir.y = 0;
        cam.transform.rotation = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
        // ３）カメラのX/Y座標を調整する
        pos.y = head.position.y - cam.orthographicSize + 0.3f;
        pos.x = head.position.x - cam.orthographicSize + 0.3f;
        cam.transform.position = pos;
    }

    // モデルの全体の高さを推定する
    float GetModelHeight(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (var rend in renderers)
        {
            bounds.Encapsulate(rend.bounds);
        }
        return bounds.size.y;
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

    void AdjustModelScaleAndPosition(GameObject model)
    {
        float referenceHeight = 768f;
        float currentHeight = Screen.height;

        float scaleFactor = referenceHeight / currentHeight;

        model.transform.localScale = Vector3.one * scaleFactor;

        // 位置調整（中央から左寄り）
        model.transform.position = new Vector3(-0.5f, -1.0f, 0); // 適宜調整
    }

    // 外部から呼べる制御メソッド
    public void SetBlinking(bool enabled,float open)
    {
        blinkController?.SetBlinkEnabled(enabled, open);
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
