using UnityEngine;

public class MToonMaterialUpdater : MonoBehaviour
{
    void Start()
    {
        // VRM�̃I�u�W�F�N�g���擾�i��F�q������}�e���A�����擾�j
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader.name.Contains("MToon"))
                {
                    // RenderingMode �� Transparent ��
                    mat.SetFloat("_BlendMode", 2); // 0: Opaque, 1: Cutout, 2: Transparent

                    // �K�v�ɉ�����RenderQueue���ݒ�
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    // �A���t�@��0�ɂ������ꍇ�͐F��A��ύX
                    //Color color = mat.color;
                    //color.a = 0.5f; // �������Ȃ�
                    //mat.color = color;
                }
            }
        }
    }
}