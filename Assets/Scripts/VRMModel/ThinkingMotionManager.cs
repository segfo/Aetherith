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
    public void DoneThinking()
    {
        animator.Play(endMotion, ANIMATION_LAYER);
        animator.SetLayerWeight(ANIMATION_LAYER, 0f);
    }
}
