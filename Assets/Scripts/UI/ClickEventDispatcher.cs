using UnityEngine;

public class ClickEventDispatcher : MonoBehaviour
{
    [SerializeField] private CharacterController character;
    [SerializeField] private LipSyncSimulator lipSyncSimulator;
    private bool blink = false;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            blink = !blink; // blinkの状態をトグル
            //character?.SetBlinking(blink, 0); // 瞬きを開始
            lipSyncSimulator?.SpeakText("こんにちは、私はバーチャルキャラクターです。");
        }
    }
}
