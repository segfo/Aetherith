[System.Serializable]
public class LLMConfig
{
    // DifyのAPIを使う
    public bool useDify = false;
    public DifyConfig dify = new DifyConfig();
    // ローカルのLLMを使う場合はモデル名を指定する(ggufファイル)
    public string modelName = "gemma-3.gguf";
    // ローカルLLM（実応答）システムプロンプトの記述されたファイル名
    public string systemPromptFile = "systemprompt.txt";
    // ローカルLLM（感情判定）システムプロンプトの記述されたファイル名
    public string userName = "User";  // ユーザー名
    public string assistantName = "Assistant"; // アシスタント名
    public int maxContextLength = 8192; // 最大コンテキスト長（トークン数）
    public float temperature = 0.7f; // 温度パラメータ（0.0-1.0）
    public float topP = 0.95f; // トップPサンプリングの確率
    public int topK = 40; // トップKサンプリングのK値
    public int numGPULayers = 0; // GPUで処理するレイヤー数(0はCPUのみ)
    public LLMConfig() { }
    public LLMConfig(string promptFileName,float temperature,string handoverConversationIdKeyName)
    {
        this.systemPromptFile = promptFileName;
        this.temperature = temperature;
        this.dify.handoverConversationIdKeyName = handoverConversationIdKeyName;
    }
}