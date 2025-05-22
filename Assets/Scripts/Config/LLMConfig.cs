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
    // ローカルLLM（実応答）システムプロンプトの記述されたファイル名
    public string systemPromptFile = "systemprompt.txt";
    // ローカルLLM（感情判定）システムプロンプトの記述されたファイル名
    public string userName = "User";
    public string assistantName = "Assistant";
    public int maxContextLength = 8192;
    public float temperature = 0.7f;
    public float topP = 0.95f;
    public int topK = 40;
    public LLMConfig() { }
    public LLMConfig(string promptFileName,float temperature)
    {
        this.systemPromptFile = promptFileName;
        this.temperature = temperature;
    }
}