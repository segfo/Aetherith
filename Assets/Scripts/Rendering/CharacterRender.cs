using UniHumanoid;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CharacterRender : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private Light directionalLight;
    private void Start()
    {
        DirectionalLight();
        ToneMapping();
    }
    private void DirectionalLight()
    {
        // ライトの色
        directionalLight.color = ParseHexColor(AppConfigManager.Instance.Config.vrm.LightColorRGBA);
        // ライトの強さ
        directionalLight.intensity = AppConfigManager.Instance.Config.vrm.LightIntensity;
        // 影の強さ
        directionalLight.shadowStrength = AppConfigManager.Instance.Config.vrm.ShadowStrength;
    }
    
    public static Color ParseHexColor(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length != 8)
        {
            throw new System.ArgumentException("16進カラー表記は 8 文字で記載してください: RRGGBBAA");
        }

        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        byte a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color32(r, g, b, a);
    }
    private void ToneMapping()
    {
        Tonemapping tonemapping;
        if (volume != null && volume.profile.TryGet(out tonemapping))
        {
            // Tonemappingの設定を行う
            if (tonemapping != null)
            {
                int tmmode = AppConfigManager.Instance.Config.vrm.ToneMappingMode;
                switch(tmmode){
                    case 0:
                        tonemapping.mode.value = TonemappingMode.None;
                        break;
                    case 1:
                        tonemapping.mode.value = TonemappingMode.Neutral;
                        break;
                    case 2:
                        tonemapping.mode.value = TonemappingMode.ACES;
                        break;
                    default:
                        tonemapping.mode.value = TonemappingMode.None;
                        break;
                }
            }
        }
        else
        {
            Debug.LogError("Tonemapping が Volume Profile に見つかりません。");
        }
    }
}
