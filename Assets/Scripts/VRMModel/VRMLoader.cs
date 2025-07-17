using System.Threading;
using System;
using uDesktopMascot;
using UnityEngine;
using System.IO;
using static uDesktopMascot.LoadVRM;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class VRMLoader : MonoBehaviour
{
    [SerializeField] private string vrmFileName = "";
    private bool vrmLoaded = false; // 初期化済みフラグ
    private bool vrmLoadError = false; // ロードエラーが発生したかどうかのフラグ

    public GameObject LoadedModel { get; private set; }
    static public VRMLoader Instance { get; private set; } // シングルトンインスタンス

    public event Action<GameObject> OnVrmLoaded; // ロード完了時の通知
    public event Action<string> OnVrmLoadError; // ロード不能時の通知
    public event Action BeforeVrmUnload; // VRMアンロード前の通知ata
    public event Action OnVrmUnload; // VRMアンロード開始時の通知
    // ここではモデルのロードとスケールの変更のみをする。
    // VRMのカメラ位置の設定はCharacterController.csで行う。
    private async void Start()
    {
        if(Instance == null) {
            Instance = this; // シングルトンインスタンスの設定
        }
        AppConfigManager.Instance.OnConfigUpdated += OnConfigUpdated;
        vrmFileName = AppConfigManager.Instance.Config.vrm.FileName;
        await LoadVrm();
    }

    void OnConfigUpdated(AppConfig config)
    {
        if (config.vrm.FileName != vrmFileName && vrmLoaded) {
            vrmLoaded = false;
            vrmFileName = config.vrm.FileName;
            LoadVrm();
        }
        else
        {
            Debug.Log("VRMのロードが終わっていないのでVRMの再ロードをキャンセルしました。");
        }
    }

    private async Task LoadVrm()
    {
        // VRMのゲームコンテナを削除する。
        GameObject obj = GameObject.Find("VRM1");
        // obj!=nullの場合又はvrmLoadErrorがtrueの場合は、VRMのアンロード前の通知を行う。
        if (obj!=null||vrmLoadError)
        {
            Debug.Log("VRMモデルのアンロードを開始します。");
            BeforeVrmUnload?.Invoke(); // VRMアンロード前の通知
        }
        // 設定ファイルからVRMファイル名を取得する。
        LoadedVRMInfo loadedModelInfo = await LoadVrmModel();
        if (obj != null)
        {
            OnVrmUnload?.Invoke(); // VRMアンロード開始の通知
            obj.SetActive(false); // 一旦非アクティブにする
        }
        if (loadedModelInfo != null)
        {
            float scale = AppConfigManager.Instance.Config.vrm.Scale;
            GameObject model = loadedModelInfo.Model;
            model.transform.position = Vector3.zero;
            Animator animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            LoadVRM.UpdateAnimationController(animator);
            model.transform.localScale = new Vector3(scale, scale, scale); // スケールを大きくする
            LoadedModel = model;
            if (obj != null)
            {
                Debug.Log("古いVRMオブジェクトを削除します。");
                Destroy(obj); // 古いVRMオブジェクトを削除する
            }
            else
            {
                Debug.LogWarning("VRM_Oldオブジェクトが見つかりませんでした。");
            }
            Debug.Log($"モデル名: {loadedModelInfo.ModelName}");
            OnVrmLoaded?.Invoke(model);
        }
        else
        {
            vrmLoadError = true;
            if (obj != null)
            {
                OnVrmLoaded?.Invoke(LoadedModel);
                obj.SetActive(true);
                vrmLoaded = true; // モデルがロード済みであることを示す
                return;
            }
            Debug.LogError("モデルの読み込みに失敗しました。");
            vrmFileName = AppConfigManager.Instance.Config.vrm.FileName;
            string basePath = Path.Combine(Application.streamingAssetsPath, "VRM");
            string path = SafeFileReader.PathVerifier(basePath, Path.Combine(basePath, vrmFileName));
            OnVrmLoadError?.Invoke(path);
        }
        vrmLoaded = true;
    }
    UniTask<LoadedVRMInfo> LoadVrmModel()
    {
        vrmFileName = AppConfigManager.Instance.Config.vrm.FileName;
        Debug.Log("VRMLoader - VRMモデルの読み込みを開始します。");
        var cancellationToken = new CancellationTokenSource().Token;
        string basePath = Path.Combine(Application.streamingAssetsPath, "VRM");
        string path = SafeFileReader.PathVerifier(basePath, Path.Combine(basePath, vrmFileName));
        return LoadVRM.LoadModelAsync(path, cancellationToken);
    }
}