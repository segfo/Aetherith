using System.Collections;
using UnityEngine;

public class ShakeDizzyAnimationPlayer : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private float fadeDuration = 2.0f;  // 徐々に戻す時間
    private Coroutine currentRoutine;
    private int layerIndex = 2; // DizzyLayerのインデックス（0がBase Layer）
    private float waitTime = 0.0f; // アニメーションの再生時間
    public float PlayDizzy(Animator animator)
    {
        this.animator = animator;
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }
        waitTime = Random.Range(4.0f, 6.0f);
        currentRoutine = StartCoroutine(PlayAndFadeCoroutine());
        return waitTime;
    }

    private IEnumerator PlayAndFadeCoroutine()
    {
        // レイヤー3が存在するか安全確認
        if (animator.layerCount <= layerIndex)
        {
            Debug.LogWarning("DizzyLayer が Animator に存在しません");
            yield break;
        }
        // 1. 再生開始（LayerWeight = 1）
        animator.SetLayerWeight(layerIndex, 1f);
        animator.Play("Dizzy", layerIndex);
        //Debug.Log("Dizzyアニメーションを再生中...");
        // 2. アニメーションの再生を待つ
        yield return new WaitForSeconds(waitTime);
        //Debug.Log($"再生終了しました。 {waitTime} s");
        // 3. フェードアウト処理
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            float weight = Mathf.Lerp(1f, 0f, t);
            animator.SetLayerWeight(layerIndex, weight);
            yield return null;
        }
        // 4. 最終的にWeightを0に
        animator.SetLayerWeight(layerIndex, 0f);
    }
}
