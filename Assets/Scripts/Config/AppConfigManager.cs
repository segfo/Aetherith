using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class AppConfigManager
{
    private static AppConfigManager _instance;
    public static AppConfigManager Instance => _instance ??= new AppConfigManager();

    private readonly string configPath;
    public AppConfig Config { get; private set; }

    private AppConfigManager()
    {
        string defConfig = JsonConvert.SerializeObject(new AppConfig());
        configPath = Path.Combine(Application.streamingAssetsPath, "appconfig.json");
        Config = JsonConvert.DeserializeObject<AppConfig>(SafeFileReader.ReadOrCreateTextFile(configPath, Encoding.UTF8, defConfig));
    }
}
