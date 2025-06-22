public class ChatWindowConfig
{
    public string windowBgRGBA = "#FFFFFF88";   // チャットウィンドウの背景色
    public string inputWindowBgRGBA = "#FFFFFFFF";  // チャット入力ウィンドウの背景色
    public string welcomeMessage = "何でも聞いてくださいね！"; // 起動完了時のメッセージ
    public string waitMessage = "\"（考え中です…）\""; // LLMからの応答待ちメッセージ
    public string shakeMessage = "うぅ・・・振らないでください～！"; // シェイク時のメッセージ
    public float botChatTypingInterval = 0.025f; // チャットに出力される文字の時間間隔(Bot)
    public float systemChatTypingInterval = 0.025f; // チャットに出力される文字の時間間隔（システム）
    public bool sendOnEnter = true;
}