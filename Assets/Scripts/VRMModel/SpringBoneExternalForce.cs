using UniGLTF.SpringBoneJobs.Blittables;
using UnityEngine;
using UniVRM10;

public class SpringBoneExternalForce : MonoBehaviour
{
    private IVrm10SpringBoneRuntime springBoneRuntime;
    private Vector3 previousWindowPosition;
    private float forceMultiplier = 0.5f; // 外力の強さを調整するための係数

    void Start()
    {
        var vrmInstance = GetComponent<Vrm10Instance>();
        if (vrmInstance != null)
        {
            springBoneRuntime = vrmInstance.Runtime.SpringBone;
        }

        previousWindowPosition = GetWindowPosition();
    }

    void Update()
    {
        if (springBoneRuntime == null) return;

        Vector3 currentWindowPosition = GetWindowPosition();
        Vector3 windowMovement = currentWindowPosition - previousWindowPosition;

        // ウィンドウの移動方向と逆向きの外力を計算
        Vector3 externalForce = -windowMovement * forceMultiplier;

        // BlittableModelLevel構造体を作成し、ExternalForceを設定
        var modelLevel = new BlittableModelLevel
        {
            ExternalForce = externalForce
        };

        // 外力を適用
        springBoneRuntime.SetModelLevel(transform, modelLevel);

        previousWindowPosition = currentWindowPosition;
    }

    private Vector3 GetWindowPosition()
    {
        // Windows用APIでアクティブウィンドウの位置を取得
        RECT rect;
        GetWindowRect(GetActiveWindow(), out rect);
        return new Vector3(rect.Left, rect.Top, 0);
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();
}
