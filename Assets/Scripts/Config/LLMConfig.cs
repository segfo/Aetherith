[System.Serializable]
public class LLMConfig
{
    // DifyのAPIを使う
    public bool useDify = false;
    public DifyConfig dify = new DifyConfig();
    public LocalLLMConfig local = new LocalLLMConfig();
    public LLMConfig() { }
    public LLMConfig(string promptFileName,float temperature,string handoverConversationIdKeyName)
    {
        this.local.systemPromptFile = promptFileName;
        this.local.temperature = temperature;
        this.dify.handoverConversationIdKeyName = handoverConversationIdKeyName;
    }
}