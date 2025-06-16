using UnityEngine;
using System.Collections;

class AnimationUtil
{
    public static IEnumerator AnimationFade(Animator animator, int layerIndex,float fadeDuration, float from, float to)
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            float weight = Mathf.Lerp(from, to, t);
            animator.SetLayerWeight(layerIndex, weight);
            yield return null;
        }
    }
}