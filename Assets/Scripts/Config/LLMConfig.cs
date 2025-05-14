[System.Serializable]
public class LLMConfig
{
    // DifyのAPIを使う
    public bool useDify = false;
    public string difyApiUrl = "";
    // ローカルのLLMを使う場合はモデル名を指定する(ggufファイル)
    public string modelName = "gemma3.gguf";
    // DifyのAPIを使う場合はAPIキーを指定する
    public string difyApiKey = "";
    // 実応答LLMシステムプロンプトの記述されたファイル名
    public string agentSystemPromptFile = "";
    // 感情判定LLMシステムプロンプトの記述されたファイル名
    public string emotionSystemPromptFile = "";
    public string userName = "User";
    public string assistantName = "Assistant";
    public int maxContextLength = 8192;
    public string welcomeMessage = "何でも聞いてくださいね！";
    public string waitMessage = "\"（考え中です…）\"";
    public float temperature = 0.7f;
}