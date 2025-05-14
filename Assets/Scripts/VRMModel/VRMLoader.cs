using System.Threading;
using System;
using uDesktopMascot;
using UnityEngine;
using System.IO;

public class VRMLoader : MonoBehaviour
{
    [SerializeField] private string vrmFileName = "";

    public GameObject LoadedModel { get; private set; }

    public event Action<GameObject> OnModelLoaded; // ← 他クラスに通知

    private async void Start()
    {
        // 設定ファイルからVRMファイル名を取得する。
        vrmFileName = AppConfigManager.Instance.Config.vrmFileName;
        Debug.Log("VRMLoader - VRMモデルの読み込みを開始します。");
        var cancellationToken = new CancellationTokenSource().Token;
        var loadedModelInfo = await LoadVRM.LoadModelAsync(Path.Combine("VRM",vrmFileName), cancellationToken);

        if (loadedModelInfo != null)
        {
            GameObject model = loadedModelInfo.Model;
            model.transform.position = Vector3.zero;
            Animator animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            LoadVRM.UpdateAnimationController(animator);
            //model.transform.localScale = new Vector3(2f, 2f,2f); // スケールを大きくする
            LoadedModel = model;

            Debug.Log($"モデル名: {loadedModelInfo.ModelName}");

            // ← 他のコンポーネントに通知
            OnModelLoaded?.Invoke(model);
        }
        else
        {
            Debug.LogError("モデルの読み込みに失敗しました。");
        }
    }
    // 深い階層から名前でTransformを探す
    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name)) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}