[System.Serializable]
public class AppConfig
{
    public LLMConfig characterLlm = new LLMConfig();
    public LLMConfig emotionLlm = new LLMConfig("emotion_systemprompt.txt",0.0f);
    public VRMConfig vrm = new VRMConfig();
    public string chatWindowBgRGBA = "#FFFFFF88";   // チャットウィンドウの背景色
    public string chatInputWindowBgRGBA = "#FFFFFFFF";  // チャット入力ウィンドウの背景色
    public string welcomeMessage = "何でも聞いてくださいね！"; // 起動完了時のメッセージ
    public string waitMessage = "\"（考え中です…）\""; // LLMからの応答待ちメッセージ
    public string shakeMessage = "うぅ・・・振らないでください～！"; // シェイク時のメッセージ
    public float shakeForceThreshold = 0.15f; // シェイクの閾値
    public float chatTypingInterval = 0.025f; // チャットに出力される文字の時間間隔（TypewriterEffect利用時）
}
