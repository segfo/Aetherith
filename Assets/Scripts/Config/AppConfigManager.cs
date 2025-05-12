using System.IO;
using System.Text;
using UnityEngine;

public class AppConfigManager
{
    private static AppConfigManager _instance;
    public static AppConfigManager Instance => _instance ??= new AppConfigManager();

    private readonly string configPath;
    public AppConfig Config { get; private set; }

    private AppConfigManager()
    {
        string defConfig = JsonUtility.ToJson(new AppConfig(), prettyPrint: true);
        Debug.Log($"AppConfigManager - デフォルト設定: {defConfig}");
        configPath = Path.Combine(Application.streamingAssetsPath, "appconfig.json");
        Config = JsonUtility.FromJson<AppConfig>(SafeFileReader.ReadOrCreateTextFile(configPath, Encoding.UTF8, defConfig));
    }
}
