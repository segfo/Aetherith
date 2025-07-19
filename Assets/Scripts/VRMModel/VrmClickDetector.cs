using UnityEngine;
using UnityEngine.EventSystems;
// VRMLoaderの姉妹クラス
// VRMのクリックに関する初期設定クラス
// FullBodyColliderSetup.csとセットで使用する
public class VrmClickDetector : MonoBehaviour
{
    public GameObject vrmRoot; // VRMのルート（Animatorがアタッチされている）
    [SerializeField] private float doubleClickThreshold = 0.3f;

    private float lastClickTime = 0f;

    private Transform leftFoot;
    private Transform rightFoot;

    public void Ready()
    {
        if (vrmRoot == null)
        {
            Debug.LogError("VRM Root is not assigned.");
            return;
        }

        var animator = vrmRoot.GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("Animator is missing or not humanoid.");
            return;
        }

        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;

            if (timeSinceLastClick < doubleClickThreshold)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (IsFoot(hit.transform))
                    {
                        OnFootDoubleClicked(hit.transform);
                    }
                }
            }
        }
    }

    private bool IsFoot(Transform clickedTransform)
    {
        // 足ボーンの子孫も含めてチェック（例: 足にMeshColliderなどがついている場合）
        return clickedTransform.IsChildOf(leftFoot) || clickedTransform.IsChildOf(rightFoot);
    }

    private void OnFootDoubleClicked(Transform footTransform)
    {
        int vrmListCont = AppConfigManager.Instance.Config.vrm.Length;
        if (!AppConfigManager.Instance.Config.MetamorphoseEnabled || vrmListCont==1 ) {
            return; // 「変身」機能が無効な場合は何もしない
        }
        Debug.Log($"Double clicked on foot: {footTransform.name}");
        // 足へのダブルクリックイベント発火
        // 必要に応じてイベント等にする
        // VRMの設定ファイルのインデックスを変更する（CharacterController）
        // Character1Controller GameObjectを得る
        var characterController = GameObject.Find("CharacterController");
        VRMLoader vrmLoader = characterController.GetComponent<VRMLoader>();

        vrmLoader.SetVrmFileConfigSelector((vrmLoader.vrmFileConfigSelector + 1) % vrmListCont);
        // VRMの再ロードを強制する。（設定ファイルが再読み込みされたように見せる）
        GameObject.Find("CharacterController").GetComponent<VRMLoader>().VRMReload();
    }
}
