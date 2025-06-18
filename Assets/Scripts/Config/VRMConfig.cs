using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[System.Serializable]
public class VRMConfig
{
    public ToneMappingMode ToneMapping = ToneMappingMode.None; // 0: None, 1: Neutral, 2: ACES
    public float LightIntensity = 1f;  // 明るさ: 0.0f ～ 1.0f
    public string LightColorRGBA = "#FFF0E0FF";  // ライトの色（肌が白くなりすぎないように）
    public float ShadowStrength = 0.3000000f; // 影の強さ: 0.0f ～ 1.0f
    public float Scale = 1.0f;  // VRMのスケール
    public float VrmDisplayOffsetX = 0f; // VRMの表示位置調整 X軸
    public float VrmDisplayOffsetY = 0f; // VRMの表示位置調整 Y軸
    public float ChatInputOffsetX = 0f; // チャット入力の表示位置調整 X軸
    public float ChatInputOffsetY = 0f; // チャット入力の表示位置調整 Y軸
    public string FileName = "Default.vrm"; // VRMファイル名
    public SpringBone springBone = new SpringBone(); // SpringBone = 揺れ物の設定
    // 瞬きを除外する表情のしきい値・blinkDisable = trueの時は許可する表情とする。
    public BlinkExclusionExpressionThreshold blinkExclusionExpressionThreshold = new BlinkExclusionExpressionThreshold();
    public bool blinkDisable = false; // 瞬きを無効にする（糸目キャラ用）
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ToneMappingMode
{
    None,Neutral,ACES
}

public class SpringBone
{
    // 外力の適用度合い（大きければ大きいほどすぐ動く）
    public float ExternalForceMultiplier = 0.01f;
    // 揺れ物に対する動きの適用最大値（揺れ物が動く最大値）
    public float MaximumMovementForce = 0.5f; 
}
// 瞬きを除外する表情のしきい値
// 1.0f = 100%の表情が出ていても瞬きはする
// 0.0f = 表情が出ている場合は瞬きを除外する
public class BlinkExclusionExpressionThreshold
{
    public float Happy = 1.0f; // 笑顔のしきい値
    public float Sad = 1.0f; // 悲しいのしきい値
    public float Angry = 1.0f; // 怒りのしきい値
    public float Neutral = 1.0f; // 無表情のしきい値
    public float Surprise = 1.0f; // 驚きのしきい値
    public float Relaxed = 1.0f; // リラックスのしきい値
}