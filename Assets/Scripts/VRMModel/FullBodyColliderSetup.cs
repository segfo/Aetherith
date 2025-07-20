using UnityEngine;
// VRMLoaderの姉妹クラス
// VRMClickDetectorとセットで使用する
public class FullBodyColliderSetup : MonoBehaviour
{
    public GameObject vrmRoot;

    // Humanoidボーンとそのコライダー設定
    private readonly (HumanBodyBones bone, Vector3 center, float radius, float height, int direction)[] colliderSettings = new[]
    {
        (HumanBodyBones.Head,         new Vector3(0, 0.1f, 0), 0.1f, 0.2f, 1),
        (HumanBodyBones.Neck,         Vector3.zero,            0.05f, 0.1f, 1),
        (HumanBodyBones.Chest,        Vector3.zero,            0.12f, 0.3f, 1),
        (HumanBodyBones.Spine,        Vector3.zero,            0.12f, 0.25f, 1),
        (HumanBodyBones.Hips,         Vector3.zero,            0.15f, 0.25f, 1),
        (HumanBodyBones.LeftUpperArm, Vector3.zero,            0.05f, 0.25f, 0),
        (HumanBodyBones.LeftLowerArm, Vector3.zero,            0.04f, 0.25f, 0),
        (HumanBodyBones.RightUpperArm,Vector3.zero,            0.05f, 0.25f, 0),
        (HumanBodyBones.RightLowerArm,Vector3.zero,            0.04f, 0.25f, 0),
        (HumanBodyBones.LeftUpperLeg, Vector3.zero,            0.06f, 0.3f, 1),
        (HumanBodyBones.LeftLowerLeg, Vector3.zero,            0.05f, 0.45f, 1),
        (HumanBodyBones.LeftFoot,     Vector3.zero,            0.04f, 0.1f, 1),
        (HumanBodyBones.RightUpperLeg,Vector3.zero,            0.06f, 0.3f, 1),
        (HumanBodyBones.RightLowerLeg,Vector3.zero,            0.05f, 0.45f, 1),
        (HumanBodyBones.RightFoot,    Vector3.zero,            0.04f, 0.1f, 1),
    };

    public void AddFullBodyColliders()
    {
        if (vrmRoot == null)
        {
            Debug.LogWarning("VRM Root is not assigned.");
            return;
        }

        Animator animator = vrmRoot.GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("Animator is not set or not humanoid.");
            return;
        }

        foreach (var setting in colliderSettings)
        {
            AddColliderToBone(animator, setting.bone, setting.center, setting.radius, setting.height, setting.direction);
        }

        Debug.Log("全身のコライダーを追加しました。");
    }

    private void AddColliderToBone(Animator animator, HumanBodyBones bone, Vector3 center, float radius, float height, int direction)
    {
        Transform boneTransform = animator.GetBoneTransform(bone);
        if (boneTransform == null)
        {
            Debug.LogWarning($"{bone} not found, skipping.");
            return;
        }

        if (boneTransform.GetComponent<Collider>() != null) return;

        CapsuleCollider collider = boneTransform.gameObject.AddComponent<CapsuleCollider>();
        collider.center = center;
        collider.radius = radius;
        collider.height = height;
        collider.direction = direction;
    }
}
