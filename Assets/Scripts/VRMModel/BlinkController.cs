using System.Collections;
using UnityEngine;
using UniVRM10;

public class BlinkControllerVrm10 : MonoBehaviour
{
    private Vrm10Instance vrmInstance;
    private float blinkInterval = 4f; // 人間の平均的な瞬きの回数はおおよそ3秒に1回なので、3を基準に2～4秒おきに瞬きするようにする。
    private float blinkDuration = 0.1f; // 瞬きの持続時間
    private bool isBlinkEnabled = true;

    void Start()
    {
        vrmInstance = GetComponent<Vrm10Instance>();
        if (vrmInstance != null)
        {
            StartCoroutine(BlinkLoop());
        }
        else
        {
            Debug.LogWarning("Vrm10Instance が見つかりません。");
        }
    }
    // <summary>
    // enabled: true=有効, false=無効   // 瞬きの有効/無効を設定する 
    // open: 0.0=閉じる, 1.0=開く   // 瞬きの開き具合を0～1の実数で設定する。
    // </summary>
    public void SetBlinkEnabled(bool enabled,float open)
    {
        isBlinkEnabled = enabled;
        if (!enabled)
        {
            vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Blink, open);
        }
    }
    IEnumerator BlinkLoop()
    {
        while (true)
        {
            if (!isBlinkEnabled)
            {
                yield return null;
                continue;
            }
            yield return new WaitForSeconds(Random.Range(blinkInterval - 2f, blinkInterval + 1f));

            // 瞬きON
            vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Blink, 1.0f);
            yield return new WaitForSeconds(blinkDuration);

            // 瞬きOFF
            vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Blink, 0.0f);
        }
    }
}
