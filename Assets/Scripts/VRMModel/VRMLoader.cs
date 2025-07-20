using System.Threading;
using System;
using uDesktopMascot;
using UnityEngine;
using System.IO;
using static uDesktopMascot.LoadVRM;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class VRMLoader : MonoBehaviour
{
    public int vrmFileConfigSelector { get; private set; } =  0; // ロード中のVRMファイルの設定ファイルのインデックス
    private String vrmFileName = "";
    private bool vrmLoaded  = false; // 初期化済みフラグ
    private bool vrmLoadError = false; // ロードエラーが発生したかどうかのフラグ
    public GameObject LoadedModel { get; private set; }
    public event Action<GameObject> OnVrmLoaded; // ロード完了時の通知
    public event Action<string> OnVrmLoadError; // ロード不能時の通知
    public event Action BeforeVrmUnload; // VRMアンロード前の通知ata
    public event Action OnVrmUnload; // VRMアンロード開始時の通知
    [SerializeField] private string VRMGameObjectName = String.Empty;
    // ここではモデルのロードとスケールの変更のみをする。
    // VRMのカメラ位置の設定はCharacterController.csで行う。
    private async void Start()
    {
        AppConfigManager.Instance.OnConfigUpdated += OnConfigUpdated;
        await LoadVrm();
    }

    public void SetVrmFileConfigSelector(int selector)
    {
        vrmFileConfigSelector = selector % AppConfigManager.Instance.Config.vrm.Length;
    }

    void OnConfigUpdated(AppConfig config)
    {
        if (config.vrm[vrmFileConfigSelector].FileName != vrmFileName && vrmLoaded) {
            vrmLoaded = false;
            vrmFileName = config.vrm[vrmFileConfigSelector].FileName;
            LoadVrm();
        }
        else
        {
            Debug.Log("VRMのロードが終わっていないのでVRMの再ロードをキャンセルしました。");
        }
    }

    public void VRMReload()
    {
        OnConfigUpdated(AppConfigManager.Instance.Config);
    }

    private async Task LoadVrm()
    {
        // VRMのゲームコンテナを削除する。
        GameObject obj = GameObject.Find(VRMGameObjectName);
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
            // 初回ロード時
            float scale = AppConfigManager.Instance.Config.vrm[vrmFileConfigSelector].Scale;
            GameObject model = loadedModelInfo.Model;
            model.transform.position = Vector3.zero;
            VrmClickDetector footClickDetector = model.GetComponent<VrmClickDetector>() ?? model.AddComponent<VrmClickDetector>();
            Animator animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            footClickDetector.vrmRoot = model;
            LoadVRM.UpdateAnimationController(animator);
            footClickDetector.Ready();
            // Colliderの設定を追加する
            var colliderSetup = model.GetComponent<FullBodyColliderSetup>() ?? model.AddComponent<FullBodyColliderSetup>();
            colliderSetup.vrmRoot = model;
            colliderSetup.AddFullBodyColliders();

            model.transform.localScale = new Vector3(scale, scale, scale); // スケールを大きくする
            LoadedModel = model;
            if (obj != null)
            {
                Debug.Log("古いVRMオブジェクトを削除します。");
                Destroy(obj); // 古いVRMオブジェクトを削除する
            }
            Debug.Log($"モデル名: {loadedModelInfo.ModelName}");
            // ロード出来たらVRM1という名前から変更する
            GameObject.Find("VRM1").name = VRMGameObjectName;
            OnVrmLoaded?.Invoke(model);
        }
        else
        {
            vrmLoadError = true;
            if (obj != null)
            {
                // ロード出来たらVRM1という名前から変更する
                GameObject.Find("VRM1").name = VRMGameObjectName;
                OnVrmLoaded?.Invoke(LoadedModel);
                obj.SetActive(true);
                vrmLoaded = true; // モデルがロード済みであることを示す
                return;
            }
            Debug.LogError("モデルの読み込みに失敗しました。");
            vrmFileName = AppConfigManager.Instance.Config.vrm[vrmFileConfigSelector].FileName;
            string basePath = Path.Combine(Application.streamingAssetsPath, "VRM");
            string path = SafeFileReader.PathVerifier(basePath, Path.Combine(basePath, vrmFileName));
            OnVrmLoadError?.Invoke(path);
        }
        vrmLoaded = true;
    }
    UniTask<LoadedVRMInfo> LoadVrmModel()
    {
        vrmFileName = AppConfigManager.Instance.Config.vrm[vrmFileConfigSelector].FileName;
        Debug.Log("VRMLoader - VRMモデルの読み込みを開始します。");
        var cancellationToken = new CancellationTokenSource().Token;
        string basePath = Path.Combine(Application.streamingAssetsPath, "VRM");
        string path = SafeFileReader.PathVerifier(basePath, Path.Combine(basePath, vrmFileName));
        return LoadVRM.LoadModelAsync(path, cancellationToken);
    }
}