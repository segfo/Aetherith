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
    /// VRMファイルを読み込む
    /// </summary>
    public static class LoadVRM
    {
        /// <summary>
        /// アニメーションコントローラーを設定
        /// </summary>
        /// <param name="animator"></param>
        public static void UpdateAnimationController(Animator animator)
        {
            if (animator == null)
            {
                Log.Error("Animator が null です。アニメーションコントローラーを設定できません。");
                return;
            }

            var controller = Resources.Load<RuntimeAnimatorController>("CharacterAnimationController");
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                Log.Info("アニメーションコントローラーを設定しました。");

                if (animator.avatar == null)
                {
                    Log.Warning("Animator の avatar が設定されていません。アニメーションが正しく再生されない可能性があります。");
                }
            }
            else
            {
                Log.Error("CharacterAnimationController が Resources に見つかりませんでした。アニメーションコントローラーが正しく設定されているか確認してください。");
            }
        }

        /// <summary>
        /// モデルをロードする
        /// </summary>
        public static async UniTask<LoadedVRMInfo> LoadModelAsync(string modelPath, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrEmpty(modelPath))
                {
                    Log.Info($"指定されたモデルパス: {modelPath}");

                    // StreamingAssets フォルダ内のフルパスを作成
                    var fullModelPath = Path.Combine(Application.streamingAssetsPath, modelPath);
                    
                    // モデルファイルが存在するか確認
                    if (File.Exists(fullModelPath))
                    {
                        Log.Info($"指定されたモデルファイルをロードします: {fullModelPath}");
                        // 指定されたモデルをロード
                        return await LoadAndDisplayModel(fullModelPath, cancellationToken);
                    }
                    else
                    {
                        Log.Warning($"指定されたモデルファイルが見つかりませんでした: {fullModelPath}");
                        // この後、他のモデルファイルを探します
                    }
                }
                else
                {
                    Log.Info("モデルパスが指定されていません。");
                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Error($"モデルの読み込みまたは表示中にエラーが発生しました: {e.Message}");
                return null;
            }

            return null;
        }

        /// <summary>
        /// VRMファイルを読み込み、モデルを表示する
        /// </summary>
        /// <param name="path">モデルファイルのパス</param>
        /// <param name="cancellationToken"></param>
        private static async UniTask<LoadedVRMInfo> LoadAndDisplayModel(string path,
            CancellationToken cancellationToken)
        {
            return await LoadAndDisplayModelFromPath(path, cancellationToken);
        }

        /// <summary>
        /// ファイルパスからモデルをロードして表示する
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async UniTask<LoadedVRMInfo> LoadAndDisplayModelFromPath(string path,
            CancellationToken cancellationToken)
        {
            Debug.Log($"ロードしています: {path}");
            // ファイルの拡張子を取得
            var extension = Path.GetExtension(path).ToLowerInvariant();

            GameObject model = null;
            string title = string.Empty;
            Texture2D thumbnailTexture = null;
            if (extension == ".vrm")
            {
                // VRMファイルをロード（VRM 0.x および 1.x に対応）
                try
                {
                    Vrm10Instance instance = await Vrm10.LoadPathAsync(path, canLoadVrm0X: false, ct: cancellationToken);
                    title = instance.Vrm.Meta.Name;
                    thumbnailTexture = instance.Vrm.Meta.Thumbnail;
                    // モデルのGameObjectを取得
                    model = instance.gameObject;
                    // MToon シェーダーを適用
                    //ApplyMToonShader(model);
                }
                catch (Exception e)
                {
                    Debug.Log($"VRMファイルのロード中にエラーが発生しました: {e.Message}");
                }
            }
            else
            {
                Log.Error($"サポートされていないファイル形式です: {extension}");
                return null;
            }

            if (model == null)
            {
                Log.Error("モデルのロードに失敗しました。");
                return null;
            }

            Log.Info("モデルのロードと表示が完了しました: " + path);

            return new LoadedVRMInfo(model, title, thumbnailTexture);
        }

        /// <summary>
        /// VRM 1.0 モデルに MToon シェーダーを適用する
        /// </summary>
        /// <param name="model">VRMモデルのGameObject</param>
        private static void ApplyMToonShader(GameObject model)
        {
            // モデル内のすべてのRendererを取得
            var renderers = model.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                // すべてのマテリアルに MToon シェーダーを適用
                foreach (var material in renderer.sharedMaterials)
                {
                    // もしシェーダーが既に MToon でない場合
                    if (material.shader.name != "VRM10/Universal Render Pipeline/MToon10")
                    {
                        material.shader = Shader.Find("VRM10/Universal Render Pipeline/MToon10");
                    }

                    // シェーダーの設定
                    SetMToonShaderSettings(material);
                }
            }

            Log.Info("MToon シェーダーをすべてのマテリアルに適用しました。");
        }
        /// <summary>
        /// MToon シェーダーの設定を行う
        /// </summary>
        /// <param name="material">MToonシェーダーを適用するマテリアル</param>
        private static void SetMToonShaderSettings(Material material)
        {
            // 透過設定
            material.SetFloat("_CullMode", 2); // Back-face culling
            material.SetFloat("_RenderQueue", 3000); // 通常の不透明なレンダリング順序に設定
            material.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); // 深度書き込みを有効にする

            // 主色 (例)
            material.SetColor("_Color", new Color(1f, 1f, 1f, 1f)); // 白色
            // 影の設定
            material.SetColor("_ShadeColor", new Color(0.5f, 0.5f, 0.5f, 1f)); // 影色を灰色に

            // ライティング設定 (例)
            material.SetFloat("_ShadingToony", 0.1f); // Toonyなライティング
            material.SetFloat("_ShadingShift", 0.1f); // 明るい部分の強調
            material.SetFloat("_OutlineWidth", 0.0f); // 輪郭線の太さ
            material.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 0f)); // 輪郭線の色（黒）

            Log.Info("MToon シェーダーの設定が適用されました。");
        }
        /// <summary>
        /// VRMファイルのメタ情報を取得
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async UniTask<(string title, Texture2D thumbnailTexture)> LoadVrmMetaAsync(string path)
        {
            // キャッシュの有効性をチェック
            if (ModelCacheUtility.IsCacheValid(path))
            {
                // キャッシュからデータを読み込む
                var (cachedTitle, cachedThumbnail) = ModelCacheUtility.LoadFromCache(path);
                Log.Info($"キャッシュからメタ情報を読み込みました: {cachedTitle}");
                return (cachedTitle, cachedThumbnail);
            }

            try
            {
                // VRMファイルをロード（VRM 0.x および 1.x に対応）
                Vrm10Instance instance = await Vrm10.LoadPathAsync(path, canLoadVrm0X: true);

                // Meta情報からタイトルとサムネイルを取得
                var meta = instance.Vrm.Meta;
                string title = meta.Name;
                Texture2D originalThumbnail = meta.Thumbnail;

                Texture2D thumbnailTexture = null;
                if (originalThumbnail != null)
                {
                    // サムネイルの最大サイズ（ピクセル単位）
                    int maxThumbnailSize = 100;

                    // オリジナルのテクスチャサイズを取得
                    int originalWidth = originalThumbnail.width;
                    int originalHeight = originalThumbnail.height;

                    // アスペクト比を維持しながら、リサイズ後の幅と高さを計算
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

                    // リサイズしたテクスチャを作成
                    thumbnailTexture = ResizeTexture(originalThumbnail, targetWidth, targetHeight);
                }

                // メタ情報のみ取得したので、インスタンスを破棄
                if (instance != null && instance.gameObject != null)
                {
                    Object.Destroy(instance.gameObject);
                }

                // キャッシュに保存
                ModelCacheUtility.SaveToCache(path, title, thumbnailTexture);
                Log.Info($"メタ情報をキャッシュに保存しました: {title}");

                return (title, thumbnailTexture);
            }
            catch (Exception e)
            {
                Log.Error($"VRMメタ情報の読み込み中にエラーが発生しました: {e.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// テクスチャをリサイズ
        /// </summary>
        /// <param name="source"></param>
        /// <param name="targetWidth"></param>
        /// <param name="targetHeight"></param>
        /// <returns></returns>
        private static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            // RenderTextureを使用して、テクスチャをリサイズします
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            rt.filterMode = FilterMode.Bilinear;

            // 元のアクティブなRenderTextureを保存
            RenderTexture previous = RenderTexture.active;

            // RenderTextureをアクティブに設定
            RenderTexture.active = rt;

            // ソーステクスチャをRenderTextureにコピー
            Graphics.Blit(source, rt);

            // 新しいテクスチャを作成
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);

            // RenderTextureからピクセルを読み込む
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            // RenderTextureを解放
            RenderTexture.ReleaseTemporary(rt);

            // アクティブなRenderTextureを元に戻す
            RenderTexture.active = previous;

            return result;
        }

        /// <summary>
        /// ロードされたVRMの情報を保持するクラス
        /// </summary>
        public class LoadedVRMInfo
        {
            /// <summary>
            /// コンストラクタ
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
            /// ロードされたモデルのGameObject
            /// </summary>
            public GameObject Model { get; private set; }

            /// <summary>
            /// モデルのタイトル
            /// </summary>
            public string ModelName { get; private set; }

            /// <summary>
            /// サムネイル画像
            /// </summary>
            public Texture2D ThumbnailTexture { get; private set; }
        }
    }
}