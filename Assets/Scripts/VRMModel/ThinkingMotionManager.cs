using System.Threading.Tasks;
using UnityEngine;

public class ThinkingMotionManager : MonoBehaviour
{
    private Animator animator;

    public void SetAnimator(Animator animator)
    {
        this.animator = animator;
        Debug.Log("ThinkingMotionManagerが初期化されました。");
    }

    const string startMotion = "IdleToThink1";
    const string endMotion = "Think1ToIdle";
    const int ANIMATION_LAYER = 3;

    public void DoThinking()
    {
        animator.SetLayerWeight(ANIMATION_LAYER, 1f);
        animator.Play(startMotion, ANIMATION_LAYER);
    }
    public async Task DoneThinking()
    {
        animator.Play(endMotion, ANIMATION_LAYER);
        float durationInSeconds = GetAnimationLength(endMotion);
        int durationInMilliseconds = Mathf.RoundToInt(durationInSeconds * 1000f);
        await Task.Delay(durationInMilliseconds);
        animator.SetLayerWeight(ANIMATION_LAYER, 0f);
    }
    float GetAnimationLength(string clipName)
    {
        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        foreach (AnimationClip clip in ac.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip.length; // 秒単位
            }
        }

        Debug.LogWarning($"Animation clip '{clipName}' が見つかりませんでした。");
        return 0f;
    }
}
