using System.Threading;
using System;
using uDesktopMascot;
using UnityEngine;
using System.IO;

public class VRMLoader : MonoBehaviour
{
    [SerializeField] private string vrmFileName = "";

    public GameObject LoadedModel { get; private set; }

    public event Action<GameObject> OnModelLoaded; // �� ���N���X�ɒʒm

    private async void Start()
    {
        // �ݒ�t�@�C������VRM�t�@�C�������擾����B
        vrmFileName = AppConfigManager.Instance.Config.vrmFileName;
        Debug.Log("VRMLoader - VRM���f���̓ǂݍ��݂��J�n���܂��B");
        var cancellationToken = new CancellationTokenSource().Token;
        var loadedModelInfo = await LoadVRM.LoadModelAsync(Path.Combine("VRM",vrmFileName), cancellationToken);

        if (loadedModelInfo != null)
        {
            GameObject model = loadedModelInfo.Model;
            model.transform.position = Vector3.zero;
            Animator animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            LoadVRM.UpdateAnimationController(animator);
            //model.transform.localScale = new Vector3(2f, 2f,2f); // �X�P�[����傫������
            LoadedModel = model;

            Debug.Log($"���f����: {loadedModelInfo.ModelName}");

            // �� ���̃R���|�[�l���g�ɒʒm
            OnModelLoaded?.Invoke(model);
        }
        else
        {
            Debug.LogError("���f���̓ǂݍ��݂Ɏ��s���܂����B");
        }
    }
    // �[���K�w���疼�O��Transform��T��
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