
using LLMUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVRM10;

public class ExpressionController : MonoBehaviour
{
    public static ExpressionController Instance { get; private set; }
    private CharacterController characterController;
    public readonly static Dictionary<string, ExpressionKey> ExpressionList = new Dictionary<string, ExpressionKey> {
        { "Happy", ExpressionKey.Happy },
        { "Sad", ExpressionKey.Sad },
        { "Angry", ExpressionKey.Angry },
        { "Surprised", ExpressionKey.Surprised },
        { "Neutral", ExpressionKey.Neutral },
        { "Relaxed" , ExpressionKey.Relaxed },
        { "Blink", ExpressionKey.Blink }
    };

    private void Awake()
    {
        Instance = this;
        // 依存チェック
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterControllerが見つかりません。ExpressionControllerはCharacterControllerに依存しています。");
        }
    }
    private float waitFadeTime = 0.0f; // フェードアウトまでの時間
    private float playFadeoutTime = 0.0f; // フェードアウトにかける時間
    public void StartExpressionFadeout(float waitFadeTime,float playFadeoutTime)
    {
        this.waitFadeTime = waitFadeTime;
        this.playFadeoutTime = playFadeoutTime;
        // フェードアウトを開始
        StartCoroutine(ExpressionFadeout());
    }

    private IEnumerator ExpressionFadeout()
    {
        CancelFade(); // 既存のフェードをキャンセル
        yield return FadeOutExpressionsAfterDelay(
                waitFadeTime, // フェードアウトまでの時間
                playFadeoutTime  // フェードアウトにかける時間
        );
    }

    // 表情周りの処理
    // 後々表情専用のクラスを作ってそこに追い出す予定
    private Coroutine fadeCoroutine;
    /// 表情を取得する
    public Dictionary<ExpressionKey, float> GetCurrentExpressionWeights(Vrm10RuntimeExpression expression)
    {
        var result = new Dictionary<ExpressionKey, float>();

        foreach (var key in ExpressionList.Keys)
        {
            float weight = expression.GetWeight(ExpressionList[key]);
            if (Mathf.Abs(weight) > 0.001f) // 0に近いものは省略（必要に応じて）
            {
                result[ExpressionList[key]] = weight;
            }
        }
        return result;
    }

    /// <summary>
    /// 現在進行中のフェードをキャンセル
    /// </summary>
    public void CancelFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }
    // 表情を徐々にフェードアウトさせていく処理
    private IEnumerator FadeOutExpressionsAfterDelay(float delay, float fadeTime)
    {
        Vrm10Instance vrmInstance = characterController.vrmInstance;
        yield return new WaitForSeconds(delay);
        Dictionary<ExpressionKey, float> presets = GetCurrentExpressionWeights(vrmInstance.Runtime.Expression);
        var expressionKeys = new List<ExpressionKey>();
        var startWeights = new Dictionary<ExpressionKey, float>();
        foreach (var preset in presets)
        {
            var key = preset.Key;
            expressionKeys.Add(key);
            float currentWeight = vrmInstance.Runtime.Expression.GetWeight(key);
            startWeights[key] = currentWeight;
        }
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeTime);

            foreach (var key in expressionKeys)
            {
                float start = startWeights[key];
                float current = Mathf.Lerp(start, 0f, t);
                vrmInstance.Runtime.Expression.SetWeight(key, current);
            }

            yield return null;
        }

        foreach (var key in expressionKeys)
        {
            vrmInstance.Runtime.Expression.SetWeight(key, 0f);
        }
        fadeCoroutine = null; // フェード完了
    }
    public void ResetVrmExpression()
    {
        foreach (KeyValuePair<string, ExpressionKey> kvp in ExpressionController.ExpressionList)
        {
            characterController.SetExpression(kvp.Value, 0);
        }
    }
}
