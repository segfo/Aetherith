using UnityEngine;

public class MToonMaterialUpdater : MonoBehaviour
{
    void Start()
    {
        // VRMのオブジェクトを取得（例：子孫からマテリアルを取得）
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader.name.Contains("MToon"))
                {
                    // RenderingMode を Transparent に
                    mat.SetFloat("_BlendMode", 2); // 0: Opaque, 1: Cutout, 2: Transparent

                    // 必要に応じてRenderQueueも設定
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    // アルファを0にしたい場合は色のAを変更
                    //Color color = mat.color;
                    //color.a = 0.5f; // 半透明など
                    //mat.color = color;
                }
            }
        }
    }
}