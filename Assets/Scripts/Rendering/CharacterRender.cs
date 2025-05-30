using System;
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
        OnConfigUpdated(AppConfigManager.Instance.Config);
        AppConfigManager.Instance.OnConfigUpdated += OnConfigUpdated;
    }

    private void OnConfigUpdated(AppConfig config)
    {
        DirectionalLight(config);
        ToneMapping(config);
    }

    private void DirectionalLight(AppConfig config)
    {
        // ライトの色
        directionalLight.color = ParseHexColor(
            config.vrm.LightColorRGBA,
            config.vrm.LightColorRGBA
        );
        // ライトの強さ
        directionalLight.intensity = config.vrm.LightIntensity;
        // 影の強さ
        directionalLight.shadowStrength = config.vrm.ShadowStrength;
    }
    
    public static Color ParseHexColor(string hex,string fallback)
    {
        if (hex.Length != 8){
            hex = fallback;
        }

        if (hex.StartsWith("#")) { 
            hex = hex.Substring(1);
        }

        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        byte a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color32(r, g, b, a);
    }
    private void ToneMapping(AppConfig config)
    {
        Tonemapping tonemapping;
        if (volume != null && volume.profile.TryGet(out tonemapping))
        {
            // Tonemappingの設定を行う
            if (tonemapping != null)
            {
                int tmmode = config.vrm.ToneMappingMode;
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
