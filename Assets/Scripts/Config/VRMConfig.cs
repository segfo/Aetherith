[System.Serializable]
public class VRMConfig
{
    public int ToneMappingMode = 0; // 0: None, 1: Neutral, 2: ACES
    public float LightIntensity = 0.60000000f;
    public string LightColorRGBA = "#FFF0E0FF";
    public float ShadowStrength = 0.3000000f;
    public float Scale = 1.0f;
    public float VrmDisplayOffsetY = 0.15f;
    public float VrmDisplayOffsetX = -1.5f;
    public bool BackgroundWindowTransparent = true;
    public string FileName = "Default.vrm";
}