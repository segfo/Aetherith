using Kirurobo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class ChatWindowMinimize : MonoBehaviour
{
    [SerializeField] GameObject chatWindow;
    [SerializeField] ClickUtil clickUtil;
    [SerializeField] CharacterBinding characterInfo;
    private bool chatWindowEnabled = true;
    void Start()
    {
        var rect = GetComponent<RectTransform>().rect;
        clickUtil.DoubleClick += () => {
            // VRMがロードされてないなら隠さない。
            if (!characterInfo.ready) {
                return;
            }
            chatWindowEnabled = !chatWindowEnabled;
            chatWindow.SetActive(chatWindowEnabled);
            if (chatWindowEnabled) {
                // 元に戻す
                Vector3 angles = transform.localEulerAngles;
                angles.z = 0f;
                transform.localEulerAngles = angles;
            }
            else
            {
                // 非表示時の位置情報を覚えておく
                Vector3 angles = transform.localEulerAngles;
                // Z軸を-90度
                angles.z = -90f;
                // 回転を適用
                transform.localEulerAngles = angles;
            }
        };
    }
}
