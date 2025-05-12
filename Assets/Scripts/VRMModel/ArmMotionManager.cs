using System.Collections;
using UnityEngine;

public class ArmMotionManager : MonoBehaviour
{
    private Animator animator;
    private bool isRunningSequence = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        Debug.Log("ArmMotionManager������������܂����B");
        StartCoroutine(RandomArmSequence());
    }

    IEnumerator RandomArmSequence()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(120f, 500f));

            if (!isRunningSequence)
            {
                StartCoroutine(PlayArmSequence());
            }
        }
    }

    const string startMotion = "IdleToHandOnChest";
    const string endMotion = "HandOnChestToIdle";
    IEnumerator PlayArmSequence()
    {
        isRunningSequence = true;

        // 1. �r���グ��
        animator.Play(startMotion, 1);
        yield return new WaitForSeconds(GetClipLength(startMotion, 1));

        // 2. �����_���ɐÎ~
        yield return new WaitForSeconds(Random.Range(2f, 5f));

        // 3. �r�����낷
        animator.Play(endMotion, 1);
        yield return new WaitForSeconds(GetClipLength(endMotion, 1));

        isRunningSequence = false;
    }

    float GetClipLength(string stateName, int layer)
    {
        var clipInfo = animator.GetCurrentAnimatorClipInfo(layer);
        if (clipInfo.Length > 0)
        {
            return clipInfo[0].clip.length;
        }

        // �t�H�[���o�b�N�Ƃ��ĉ�����
        return 2f;
    }
    public void PlayWaveHand()
    {
        //animator.Play("WaveHand", 1);
        return;
    }
}
