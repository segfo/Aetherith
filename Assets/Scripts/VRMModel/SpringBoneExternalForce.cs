using UniGLTF.SpringBoneJobs.Blittables;
using UnityEngine;
using UniVRM10;

public class SpringBoneExternalForce : MonoBehaviour
{
    private IVrm10SpringBoneRuntime springBoneRuntime;
    private Vector3 previousWindowPosition;
    private float forceMultiplier = 0.01f; // 外力の強さを調整するための係数
    private float movementThreshold = 0.5f; // 過剰な移動を無視するための閾値
    private float maxExpectedSpeed = 10000f;
    [SerializeField] CharacterController characterController;
    private bool onFocus = false;

    public void Start()
    {
        forceMultiplier = AppConfigManager.Instance.Config.vrm.springBone.ExternalForceMultiplier;
        movementThreshold = AppConfigManager.Instance.Config.vrm.springBone.MovementThreshold;
    }
    public void Initialize()
    {
        var vrmInstance = characterController.vrmInstance;
        if (vrmInstance != null)
        {
            springBoneRuntime = vrmInstance.Runtime.SpringBone;
        }
        previousWindowPosition = GetWindowPosition();
    }
    void Update()
    {
        if (!onFocus||springBoneRuntime == null) return;
        Vector3 currentWindowPosition = GetWindowPosition();
        Vector3 windowMovement = currentWindowPosition - previousWindowPosition;
        // ウィンドウの移動方向と逆向きの外力を計算
        // ウィンドウの移動量を1フレーム前の時間差で割って速度を求める（単位時間(=1ms)当たりの移動量）
        Vector3 velocity = windowMovement / Time.deltaTime;
        Vector3 externalForce = velocity * forceMultiplier;
        // 外力の大きさを制限する
        float speed = velocity.magnitude;
        speed = Mathf.Clamp(speed, 0f, maxExpectedSpeed);
        // ウィンドウの移動速度が遅いときは閾値を小さくすることで自然な動作にすることを目論んでいる
        float dynamicClamp = Mathf.Lerp(0.001f, movementThreshold, speed / maxExpectedSpeed);
        externalForce = Vector3.ClampMagnitude(externalForce, dynamicClamp);

        // BlittableModelLevel構造体を作成し、ExternalForceを設定
        var modelLevel = new BlittableModelLevel
        {
            ExternalForce = externalForce
        };
        // 外力を適用
        springBoneRuntime.SetModelLevel(characterController.vrmInstance.transform, modelLevel);
        previousWindowPosition = currentWindowPosition;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        previousWindowPosition = GetWindowPosition();
        onFocus = hasFocus;
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
