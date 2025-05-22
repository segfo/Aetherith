[System.Serializable]
public class AppConfig
{
    public LLMConfig characterLlm = new LLMConfig();
    public LLMConfig emotionLlm = new LLMConfig();
    public VRMConfig vrm = new VRMConfig();
    public string welcomeMessage = "何でも聞いてくださいね！";
    public string waitMessage = "\"（考え中です…）\"";
}
