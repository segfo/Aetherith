using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum LLMProvider
{
    Local,Dify
}

public class LLMConfig
{
    // どのLLMを使うか
    public LLMProvider useLLM = LLMProvider.Local;
    public DifyConfig Dify = new DifyConfig();
    public LocalLLMConfig Local = new LocalLLMConfig();
    public LLMConfig() { }
    public LLMConfig(string promptFileName,float temperature,string handoverConversationIdKeyName)
    {
        this.Local.systemPromptFile = promptFileName;
        this.Local.temperature = temperature;
        this.Dify.handoverConversationIdKeyName = handoverConversationIdKeyName;
    }
}