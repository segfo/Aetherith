[System.Serializable]
public class LLMConfig
{
    // DifyのAPIを使う
    public bool useDify = false;
    public string difyApiUrl = "";
    // ローカルのLLMを使う場合はモデル名を指定する(ggufファイル)
    public string modelName = "gemma3.gguf";
    public string llmEmotionModelName = "gemma3.gguf";
    // DifyのAPIを使う場合はAPIキーを指定する
    public string difyApiKey = "";
    // ローカルLLM（実応答）システムプロンプトの記述されたファイル名
    public string agentSystemPromptFile = "systemprompt.txt";
    // ローカルLLM（感情判定）システムプロンプトの記述されたファイル名
    public string emotionSystemPromptFile = "emotion_systemprompt.txt";
    public string userName = "User";
    public string assistantName = "Assistant";
    public int maxContextLength = 8192;
    public int llmEmotionMaxContextLength=8192;
    public string welcomeMessage = "何でも聞いてくださいね！";
    public string waitMessage = "\"（考え中です…）\"";
    public double temperature = 0.7f;
    public double emotionalTemperature = 0.0f;
}