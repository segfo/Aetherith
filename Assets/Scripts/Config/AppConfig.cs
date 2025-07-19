[System.Serializable]
public class AppConfig
{
    public LLMConfig characterLlm = new LLMConfig();
    public LLMConfig emotionLlm = new LLMConfig("emotion_systemprompt.txt",0.0f, "HandoverConversationIdKeyEmotion");
    public VRMConfig[] vrm = new VRMConfig[] { new VRMConfig(), new VRMConfig() };
    public ChatWindowConfig chat = new ChatWindowConfig();
    public bool BackgroundWindowTransparent = true; // 背景ウィンドウを透明にする
    public WindowStackOrders WindowStackOrder = WindowStackOrders.Topmost; // 0: 通常 1: 必ず最前面に表示する 2: 必ず最背面に表示する
    public float doubleClickThreshold = 0.25f; // ダブルクリック判定をする間隔
    public float shakeForceThreshold = 0.15f; // シェイクの閾値
    public bool MetamorphoseEnabled = false; // メタモルフォーゼ機能を有効にする
    public enum WindowStackOrders
    {
        Normal = 0, Topmost = 1, Bottommost = 2,
    }
}

