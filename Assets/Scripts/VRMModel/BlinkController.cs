using System.Collections;
using UnityEngine;
using UniVRM10;

public class BlinkControllerVrm10 : MonoBehaviour
{
    private Vrm10Instance vrmInstance;
    private float blinkInterval = 4f; // lŠÔ‚Ì•½‹Ï“I‚Èu‚«‚Ì‰ñ”‚Í‚¨‚¨‚æ‚»3•b‚É1‰ñ‚È‚Ì‚ÅA3‚ğŠî€‚É2`4•b‚¨‚«‚Éu‚«‚·‚é‚æ‚¤‚É‚·‚éB
    private float blinkDuration = 0.1f; // u‚«‚Ì‘±ŠÔ
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
            Debug.LogWarning("Vrm10Instance ‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñB");
        }
    }
    // <summary>
    // enabled: true=—LŒø, false=–³Œø   // u‚«‚Ì—LŒø/–³Œø‚ğİ’è‚·‚é 
    // open: 0.0=•Â‚¶‚é, 1.0=ŠJ‚­   // u‚«‚ÌŠJ‚«‹ï‡‚ğ0`1‚ÌÀ”‚Åİ’è‚·‚éB
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

            // u‚«ON
            vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Blink, 1.0f);
            yield return new WaitForSeconds(blinkDuration);

            // u‚«OFF
            vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Blink, 0.0f);
        }
    }
}
