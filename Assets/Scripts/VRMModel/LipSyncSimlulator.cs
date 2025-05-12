using System.Collections;
using UnityEngine;
using UniVRM10;
using System.Collections.Generic;

public class LipSyncSimulator : MonoBehaviour
{
    [SerializeField] private CharacterController character;
    [SerializeField] private float durationPerSyllable = 0.12f;

    public void SpeakText(string text)
    {
        StartCoroutine(SimulateLipSync(text));
    }
    private Dictionary<ExpressionKey, float>  originalWeights;
    public void LipSyncStart()
    {
        var vrm = character?.vrmInstance;
        if (vrm == null)
        {
            Debug.LogWarning("[LipSync] VRM Instance が見つかりません。");
            return;
        }
        originalWeights = BackupCurrentMouthExpressions(vrm);
    }

    private IEnumerator SimulateLipSync(string text)
    {
        var vrm = character?.vrmInstance;
        if (vrm == null)
        {
            Debug.LogWarning("[LipSync] VRM Instance が見つかりません。");
            yield break;
        }
        foreach (char c in text)
        {
            string syllable = c.ToString();

            // 「ん」は特別に閉じるだけ
            if (syllable == "ん")
            {
                ResetAllMouthExpressions(vrm);
                yield return new WaitForSeconds(durationPerSyllable);
                continue;
            }

            ExpressionKey expression = GetExpressionKeyForSyllable(c);

            // ランダムな開き具合（自然な揺らぎ）
            float openWeight = Random.Range(0.6f, 1.0f);

            // 開く
            yield return AnimateMouthWeight(vrm, expression, 0f, openWeight, durationPerSyllable * 0.7f);

            // 閉じる
            yield return AnimateMouthWeight(vrm, expression, openWeight, 0f, durationPerSyllable * 0.5f);
        }
    }
    public void LipSyncEnd()
    {
        var vrm = character?.vrmInstance;
        Debug.Log("[LipSync] 終了時に完全に閉じる");
        ResetAllMouthExpressions(vrm);
        RestoreMouthExpressions(vrm, originalWeights);
    }

    private Dictionary<ExpressionKey, float> BackupCurrentMouthExpressions(Vrm10Instance vrm)
    {
        var backup = new Dictionary<ExpressionKey, float>();

        ExpressionKey[] keys = new ExpressionKey[]
        {
        ExpressionKey.Aa,
        ExpressionKey.Ih,
        ExpressionKey.Ou,
        ExpressionKey.Ee,
        ExpressionKey.Oh
        };

        foreach (var key in keys)
        {
            float weight = vrm.Runtime.Expression.GetWeight(key);
            backup[key] = weight;
        }

        return backup;
    }
    private void RestoreMouthExpressions(Vrm10Instance vrm, Dictionary<ExpressionKey, float> backup)
    {
        foreach (var pair in backup)
        {
            vrm.Runtime.Expression.SetWeight(pair.Key, pair.Value);
        }
    }


    private static readonly Dictionary<ExpressionKey, char[]> VowelMap = new Dictionary<ExpressionKey, char[]>
    {
        // 'ん'　は口閉じなのでここには書かない
        { ExpressionKey.Aa, new[] { 'あ', 'か', 'さ', 'た', 'な', 'は', 'ま','や' ,'ら', 'わ' } },
        { ExpressionKey.Ih, new[] { 'い', 'き', 'し', 'ち', 'に', 'ひ', 'み',      'り' } },
        { ExpressionKey.Ou, new[] { 'う', 'く', 'す', 'つ', 'ぬ', 'ふ', 'む','ゆ', 'る', 'を' } },
        { ExpressionKey.Ee, new[] { 'え', 'け', 'せ', 'て', 'ね', 'へ', 'め',      'れ' } },
        { ExpressionKey.Oh, new[]{  'お', 'こ', 'そ', 'と', 'の', 'ほ', 'も','よ', 'ろ' } }
    };
    private ExpressionKey GetExpressionKeyForSyllable(char ch)
    {
        // 母音判定（簡易ルール）
        foreach (var pair in VowelMap)
        {
            if (System.Array.IndexOf(pair.Value, ch) >= 0)
            {
                return pair.Key;
            }
        }
        return ExpressionKey.Aa; // デフォルト
    }

    private IEnumerator AnimateMouthWeight(Vrm10Instance vrm, ExpressionKey key, float from, float to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            float weight = Mathf.Lerp(from, to, t / duration);
            vrm.Runtime.Expression.SetWeight(key, weight);
            t += Time.deltaTime;
            yield return null;
        }

        vrm.Runtime.Expression.SetWeight(key, to);
    }
    private void ResetAllMouthExpressions(Vrm10Instance vrm)
    {
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Aa, 0f);
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Ih, 0f);
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Ou, 0f);
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Ee, 0f);
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Oh, 0f);
    }

}
