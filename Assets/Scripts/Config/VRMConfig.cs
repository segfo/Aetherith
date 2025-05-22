[System.Serializable]
public class VRMConfig
{
    public int ToneMappingMode = 0; // 0: None, 1: Neutral, 2: ACES
    public float LightIntensity = 1f;  // 明るさ: 0.0f ～ 1.0f
    public string LightColorRGBA = "#FFF0E0FF";  // ライトの色（肌が白くなりすぎないように）
    public float ShadowStrength = 0.3000000f; // 影の強さ: 0.0f ～ 1.0f
    public float Scale = 1.0f;  // VRMのスケール
    public float VrmDisplayOffsetX = -1.5f; // VRMの表示位置調整 X軸
    public float VrmDisplayOffsetY = 0.15f; // VRMの表示位置調整 Y軸
    public bool BackgroundWindowTransparent = true; // 背景ウィンドウを透明にする
    public string FileName = "Default.vrm"; // VRMファイル名
    public SpringBone springBone = new SpringBone(); // SpringBone = 揺れ物の設定
}
public class SpringBone
{
    // 外力の適用度合い
    public float ExternalForceMultiplier = 0.01f;
    public float MovementThreshold = 0.5f;
}