using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Logging;
using UniGLTF;
using UniVRM10;
using Console = System.Console;
using Object = UnityEngine.Object;
using Unity.Burst.CompilerServices;

namespace uDesktopMascot
{
    /// <summary>
    /// VRM�t�@�C����ǂݍ���
    /// </summary>
    public static class LoadVRM
    {
        /// <summary>
        /// �A�j���[�V�����R���g���[���[��ݒ�
        /// </summary>
        /// <param name="animator"></param>
        public static void UpdateAnimationController(Animator animator)
        {
            if (animator == null)
            {
                Log.Error("Animator �� null �ł��B�A�j���[�V�����R���g���[���[��ݒ�ł��܂���B");
                return;
            }

            var controller = Resources.Load<RuntimeAnimatorController>("CharacterAnimationController");
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                Log.Info("�A�j���[�V�����R���g���[���[��ݒ肵�܂����B");

                if (animator.avatar == null)
                {
                    Log.Warning("Animator �� avatar ���ݒ肳��Ă��܂���B�A�j���[�V�������������Đ�����Ȃ��\��������܂��B");
                }
            }
            else
            {
                Log.Error("CharacterAnimationController �� Resources �Ɍ�����܂���ł����B�A�j���[�V�����R���g���[���[���������ݒ肳��Ă��邩�m�F���Ă��������B");
            }
        }

        /// <summary>
        /// ���f�������[�h����
        /// </summary>
        public static async UniTask<LoadedVRMInfo> LoadModelAsync(string modelPath, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrEmpty(modelPath))
                {
                    Log.Info($"�w�肳�ꂽ���f���p�X: {modelPath}");

                    // StreamingAssets �t�H���_���̃t���p�X���쐬
                    var fullModelPath = Path.Combine(Application.streamingAssetsPath, modelPath);
                    
                    // ���f���t�@�C�������݂��邩�m�F
                    if (File.Exists(fullModelPath))
                    {
                        Log.Info($"�w�肳�ꂽ���f���t�@�C�������[�h���܂�: {fullModelPath}");
                        // �w�肳�ꂽ���f�������[�h
                        return await LoadAndDisplayModel(fullModelPath, cancellationToken);
                    }
                    else
                    {
                        Log.Warning($"�w�肳�ꂽ���f���t�@�C����������܂���ł���: {fullModelPath}");
                        // ���̌�A���̃��f���t�@�C����T���܂�
                    }
                }
                else
                {
                    Log.Info("���f���p�X���w�肳��Ă��܂���B");
                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Error($"���f���̓ǂݍ��݂܂��͕\�����ɃG���[���������܂���: {e.Message}");
                return null;
            }

            return null;
        }

        /// <summary>
        /// VRM�t�@�C����ǂݍ��݁A���f����\������
        /// </summary>
        /// <param name="path">���f���t�@�C���̃p�X</param>
        /// <param name="cancellationToken"></param>
        private static async UniTask<LoadedVRMInfo> LoadAndDisplayModel(string path,
            CancellationToken cancellationToken)
        {
            return await LoadAndDisplayModelFromPath(path, cancellationToken);
        }

        /// <summary>
        /// �t�@�C���p�X���烂�f�������[�h���ĕ\������
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async UniTask<LoadedVRMInfo> LoadAndDisplayModelFromPath(string path,
            CancellationToken cancellationToken)
        {
            Debug.Log($"���[�h���Ă��܂�: {path}");
            // �t�@�C���̊g���q���擾
            var extension = Path.GetExtension(path).ToLowerInvariant();

            GameObject model = null;
            string title = string.Empty;
            Texture2D thumbnailTexture = null;
            if (extension == ".vrm")
            {
                // VRM�t�@�C�������[�h�iVRM 0.x ����� 1.x �ɑΉ��j
                try
                {
                    Vrm10Instance instance = await Vrm10.LoadPathAsync(path, canLoadVrm0X: false, ct: cancellationToken);
                    title = instance.Vrm.Meta.Name;
                    thumbnailTexture = instance.Vrm.Meta.Thumbnail;
                    // ���f����GameObject���擾
                    model = instance.gameObject;
                    // MToon �V�F�[�_�[��K�p
                    //ApplyMToonShader(model);
                }
                catch (Exception e)
                {
                    Debug.Log($"VRM�t�@�C���̃��[�h���ɃG���[���������܂���: {e.Message}");
                }
            }
            else
            {
                Log.Error($"�T�|�[�g����Ă��Ȃ��t�@�C���`���ł�: {extension}");
                return null;
            }

            if (model == null)
            {
                Log.Error("���f���̃��[�h�Ɏ��s���܂����B");
                return null;
            }

            Log.Info("���f���̃��[�h�ƕ\�����������܂���: " + path);

            return new LoadedVRMInfo(model, title, thumbnailTexture);
        }

        /// <summary>
        /// VRM 1.0 ���f���� MToon �V�F�[�_�[��K�p����
        /// </summary>
        /// <param name="model">VRM���f����GameObject</param>
        private static void ApplyMToonShader(GameObject model)
        {
            // ���f�����̂��ׂĂ�Renderer���擾
            var renderers = model.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                // ���ׂẴ}�e���A���� MToon �V�F�[�_�[��K�p
                foreach (var material in renderer.sharedMaterials)
                {
                    // �����V�F�[�_�[������ MToon �łȂ��ꍇ
                    if (material.shader.name != "VRM10/Universal Render Pipeline/MToon10")
                    {
                        material.shader = Shader.Find("VRM10/Universal Render Pipeline/MToon10");
                    }

                    // �V�F�[�_�[�̐ݒ�
                    SetMToonShaderSettings(material);
                }
            }

            Log.Info("MToon �V�F�[�_�[�����ׂẴ}�e���A���ɓK�p���܂����B");
        }
        /// <summary>
        /// MToon �V�F�[�_�[�̐ݒ���s��
        /// </summary>
        /// <param name="material">MToon�V�F�[�_�[��K�p����}�e���A��</param>
        private static void SetMToonShaderSettings(Material material)
        {
            // ���ߐݒ�
            material.SetFloat("_CullMode", 2); // Back-face culling
            material.SetFloat("_RenderQueue", 3000); // �ʏ�̕s�����ȃ����_�����O�����ɐݒ�
            material.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); // �[�x�������݂�L���ɂ���

            // ��F (��)
            material.SetColor("_Color", new Color(1f, 1f, 1f, 1f)); // ���F
            // �e�̐ݒ�
            material.SetColor("_ShadeColor", new Color(0.5f, 0.5f, 0.5f, 1f)); // �e�F���D�F��

            // ���C�e�B���O�ݒ� (��)
            material.SetFloat("_ShadingToony", 0.1f); // Toony�ȃ��C�e�B���O
            material.SetFloat("_ShadingShift", 0.1f); // ���邢�����̋���
            material.SetFloat("_OutlineWidth", 0.0f); // �֊s���̑���
            material.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 0f)); // �֊s���̐F�i���j

            Log.Info("MToon �V�F�[�_�[�̐ݒ肪�K�p����܂����B");
        }
        /// <summary>
        /// VRM�t�@�C���̃��^�����擾
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async UniTask<(string title, Texture2D thumbnailTexture)> LoadVrmMetaAsync(string path)
        {
            // �L���b�V���̗L�������`�F�b�N
            if (ModelCacheUtility.IsCacheValid(path))
            {
                // �L���b�V������f�[�^��ǂݍ���
                var (cachedTitle, cachedThumbnail) = ModelCacheUtility.LoadFromCache(path);
                Log.Info($"�L���b�V�����烁�^����ǂݍ��݂܂���: {cachedTitle}");
                return (cachedTitle, cachedThumbnail);
            }

            try
            {
                // VRM�t�@�C�������[�h�iVRM 0.x ����� 1.x �ɑΉ��j
                Vrm10Instance instance = await Vrm10.LoadPathAsync(path, canLoadVrm0X: true);

                // Meta��񂩂�^�C�g���ƃT���l�C�����擾
                var meta = instance.Vrm.Meta;
                string title = meta.Name;
                Texture2D originalThumbnail = meta.Thumbnail;

                Texture2D thumbnailTexture = null;
                if (originalThumbnail != null)
                {
                    // �T���l�C���̍ő�T�C�Y�i�s�N�Z���P�ʁj
                    int maxThumbnailSize = 100;

                    // �I���W�i���̃e�N�X�`���T�C�Y���擾
                    int originalWidth = originalThumbnail.width;
                    int originalHeight = originalThumbnail.height;

                    // �A�X�y�N�g����ێ����Ȃ���A���T�C�Y��̕��ƍ������v�Z
                    int targetWidth = originalWidth;
                    int targetHeight = originalHeight;

                    if (originalWidth > originalHeight)
                    {
                        if (originalWidth > maxThumbnailSize)
                        {
                            targetWidth = maxThumbnailSize;
                            targetHeight = Mathf.RoundToInt((float)originalHeight * maxThumbnailSize / originalWidth);
                        }
                    }
                    else
                    {
                        if (originalHeight > maxThumbnailSize)
                        {
                            targetHeight = maxThumbnailSize;
                            targetWidth = Mathf.RoundToInt((float)originalWidth * maxThumbnailSize / originalHeight);
                        }
                    }

                    // ���T�C�Y�����e�N�X�`�����쐬
                    thumbnailTexture = ResizeTexture(originalThumbnail, targetWidth, targetHeight);
                }

                // ���^���̂ݎ擾�����̂ŁA�C���X�^���X��j��
                if (instance != null && instance.gameObject != null)
                {
                    Object.Destroy(instance.gameObject);
                }

                // �L���b�V���ɕۑ�
                ModelCacheUtility.SaveToCache(path, title, thumbnailTexture);
                Log.Info($"���^�����L���b�V���ɕۑ����܂���: {title}");

                return (title, thumbnailTexture);
            }
            catch (Exception e)
            {
                Log.Error($"VRM���^���̓ǂݍ��ݒ��ɃG���[���������܂���: {e.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// �e�N�X�`�������T�C�Y
        /// </summary>
        /// <param name="source"></param>
        /// <param name="targetWidth"></param>
        /// <param name="targetHeight"></param>
        /// <returns></returns>
        private static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            // RenderTexture���g�p���āA�e�N�X�`�������T�C�Y���܂�
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            rt.filterMode = FilterMode.Bilinear;

            // ���̃A�N�e�B�u��RenderTexture��ۑ�
            RenderTexture previous = RenderTexture.active;

            // RenderTexture���A�N�e�B�u�ɐݒ�
            RenderTexture.active = rt;

            // �\�[�X�e�N�X�`����RenderTexture�ɃR�s�[
            Graphics.Blit(source, rt);

            // �V�����e�N�X�`�����쐬
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);

            // RenderTexture����s�N�Z����ǂݍ���
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            // RenderTexture�����
            RenderTexture.ReleaseTemporary(rt);

            // �A�N�e�B�u��RenderTexture�����ɖ߂�
            RenderTexture.active = previous;

            return result;
        }

        /// <summary>
        /// ���[�h���ꂽVRM�̏���ێ�����N���X
        /// </summary>
        public class LoadedVRMInfo
        {
            /// <summary>
            /// �R���X�g���N�^
            /// </summary>
            /// <param name="model"></param>
            /// <param name="modelName"></param>
            /// <param name="thumbnailTexture"></param>
            public LoadedVRMInfo(GameObject model, string modelName, Texture2D thumbnailTexture)
            {
                Model = model;
                ModelName = modelName;
                ThumbnailTexture = thumbnailTexture;
            }

            /// <summary>
            /// ���[�h���ꂽ���f����GameObject
            /// </summary>
            public GameObject Model { get; private set; }

            /// <summary>
            /// ���f���̃^�C�g��
            /// </summary>
            public string ModelName { get; private set; }

            /// <summary>
            /// �T���l�C���摜
            /// </summary>
            public Texture2D ThumbnailTexture { get; private set; }
        }
    }
}