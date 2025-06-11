using System.Threading;
using System;
using uDesktopMascot;
using UnityEngine;
using System.IO;
using static uDesktopMascot.LoadVRM;
using Cysharp.Threading.Tasks;

public class VRMLoader : MonoBehaviour
{
    [SerializeField] private string vrmFileName = "";

    public GameObject LoadedModel { get; private set; }

    public event Action<GameObject> OnVrmLoaded; // ← 他クラスに通知

    // ここではモデルのロードとスケールの変更のみをする。
    // VRMのカメラ位置の設定はCharacterController.csで行う。
    private async void Start()
    {
        // 設定ファイルからVRMファイル名を取得する。
        LoadedVRMInfo loadedModelInfo = await LoadVrmModel();
        if (loadedModelInfo != null)
        {
            float scale = AppConfigManager.Instance.Config.vrm.Scale; 
            GameObject model = loadedModelInfo.Model;
            model.transform.position = Vector3.zero;
            Animator animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            LoadVRM.UpdateAnimationController(animator);
            model.transform.localScale = new Vector3(scale, scale, scale); // スケールを大きくする
            LoadedModel = model;

            Debug.Log($"モデル名: {loadedModelInfo.ModelName}");
            // ← 他のコンポーネントに通知
            OnVrmLoaded?.Invoke(model);
        }
        else
        {
            Debug.LogError("モデルの読み込みに失敗しました。");
        }
        //AppConfigManager.Instance.OnConfigUpdated += (config) =>
        //{
        //    LoadVrmModel();
        //};
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