using System.Collections;
using UnityEngine;
using UniVRM10;

public class BlinkControllerVrm10 : MonoBehaviour
{
    private Vrm10Instance vrmInstance;
    private float blinkInterval = 4f; // �l�Ԃ̕��ϓI�ȏu���̉񐔂͂����悻3�b��1��Ȃ̂ŁA3�����2�`4�b�����ɏu������悤�ɂ���B
    private float blinkDuration = 0.1f; // �u���̎�������
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
            Debug.LogWarning("Vrm10Instance ��������܂���B");
        }
    }
    // <summary>
    // enabled: true=�L��, false=����   // �u���̗L��/������ݒ肷�� 
    // open: 0.0=����, 1.0=�J��   // �u���̊J�����0�`1�̎����Őݒ肷��B
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

            // �u��ON
            vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Blink, 1.0f);
            yield return new WaitForSeconds(blinkDuration);

            // �u��OFF
            vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Blink, 0.0f);
        }
    }
}
