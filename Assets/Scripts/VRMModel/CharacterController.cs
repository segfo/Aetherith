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
        Debug.Log("CharacterController - VRMLoader����̒ʒm��ҋ@��...");
    }
    private void Awake()
    {
        vrmLoader.OnModelLoaded += OnModelReady;
    }

    private void OnModelReady(GameObject model)
    {
        // VRM���f���̃C���X�^���X��ێ����Ă����B�O�����爵����悤�ɂ��邽�߁B
        vrmInstance = model.GetComponent<Vrm10Instance>();
        // ���f�����ǂݍ��܂ꂽ�Ƃ��̏���
        armMotionManager = model.AddComponent<ArmMotionManager>();
        blinkController = model.AddComponent<BlinkControllerVrm10>();
        Debug.Log("ArmMotionManager�����f���ɒǉ����܂����B");
        // ���f���̈ʒu�𒲐�(Y���𒆐S��180�x��]����)
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

        // �J�����̈ʒu�𒲐�����
        cam.transform.position = head.position + faceDir * 2 + Vector3.up * 0.2f;
        Vector3 pos = cam.transform.position;

        // �Q�j�J������VRM�̕����֌�����
        Vector3 flatDir = (head.position - cam.transform.position);
        flatDir.y = 0;
        cam.transform.rotation = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
        // �R�j�J������X/Y���W�𒲐�����
        pos.y = head.position.y - cam.orthographicSize + 0.3f;
        pos.x = head.position.x - cam.orthographicSize + 0.3f;
        cam.transform.position = pos;
    }

    // ���f���̑S�̂̍����𐄒肷��
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

    // �[���K�w���疼�O��Transform��T��
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

        // �ʒu�����i�������獶���j
        model.transform.position = new Vector3(-0.5f, -1.0f, 0); // �K�X����
    }

    // �O������Ăׂ鐧�䃁�\�b�h
    public void SetBlinking(bool enabled,float open)
    {
        blinkController?.SetBlinkEnabled(enabled, open);
    }

    // ���U��API�i���ݖ������j
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
