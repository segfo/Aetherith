[System.Serializable]
public class AppConfig
{
    public LLMConfig characterLlm = new LLMConfig();
    public LLMConfig emotionLlm = new LLMConfig("emotion_systemprompt.txt",0.0f);
    public VRMConfig vrm = new VRMConfig();
    public bool BackgroundWindowTransparent = true; // 背景ウィンドウを透明にする
    public WindowStackOrders WindowStackOrder = WindowStackOrders.Topmost; // 0: 通常 1: 必ず最前面に表示する 2: 必ず最背面に表示する
    public string chatWindowBgRGBA = "#FFFFFF88";   // チャットウィンドウの背景色
    public string chatInputWindowBgRGBA = "#FFFFFFFF";  // チャット入力ウィンドウの背景色
    public string welcomeMessage = "何でも聞いてくださいね！"; // 起動完了時のメッセージ
    public string waitMessage = "\"（考え中です…）\""; // LLMからの応答待ちメッセージ
    public string shakeMessage = "うぅ・・・振らないでください～！"; // シェイク時のメッセージ
    public float shakeForceThreshold = 0.15f; // シェイクの閾値
    public float chatTypingInterval = 0.025f; // チャットに出力される文字の時間間隔（TypewriterEffect利用時）
    public enum WindowStackOrders
    {
        Normal = 0, Topmost = 1, Bottommost = 2,
    }
}

