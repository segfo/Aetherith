using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using System;
using Kirurobo;

public class AppConfigManager
{
    private FileSystemWatcher watcher;
    private static AppConfigManager _instance;
    public static AppConfigManager Instance => _instance ??= new AppConfigManager();
    // フォールバック先のデフォルト設定
    public static AppConfigManager FallbackInstance => new AppConfigManager();

    private readonly string configPath;
    public AppConfig Config { get; private set; }
    public event Action<AppConfig> OnConfigUpdated;
    private AppConfigManager()
    {
        string defConfig = JsonConvert.SerializeObject(new AppConfig());
        configPath = Path.Combine(Application.streamingAssetsPath, "appconfig.json");
        Config = JsonConvert.DeserializeObject<AppConfig>(SafeFileReader.ReadOrCreateTextFile(configPath, Encoding.UTF8, defConfig));
        InitWatcher(configPath);
    }

    private void _OnConfigUpdated(AppConfig config)
    {
        //シーンからUniWindowControllerを検索して取得する
        UniWindowController uniWindowController = GameObject.FindFirstObjectByType<UniWindowController>();
        if (uniWindowController != null)
        {
            uniWindowController.isTransparent = config.BackgroundWindowTransparent;
        }
    }

    private void InitWatcher(string watchTarget)
    {
        string dir = Path.GetDirectoryName(watchTarget);
        string fileName = Path.GetFileName(watchTarget);

        watcher = new FileSystemWatcher(dir, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Changed += (sender, e) =>
        {
            // メインスレッドで実行するようにディスパッチ
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                Debug.Log("設定ファイルが変更されました。再読み込みします。");
                try
                {
                    string text = File.ReadAllText(configPath, Encoding.UTF8);
                    Config = JsonConvert.DeserializeObject<AppConfig>(text);
                    // インスタンスも更新する
                    _instance.Config = Config;
                    OnConfigUpdated?.Invoke(Config);
                    _OnConfigUpdated(Config);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"設定ファイルの再読み込みに失敗: {ex.Message}");
                }
            });
        };
        watcher.EnableRaisingEvents = true;
    }
}
