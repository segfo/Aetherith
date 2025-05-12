[System.Serializable]
public class LLMConfig
{
    // Dify��API���g��
    public bool useDify = false;
    public string difyApiUrl = "";
    // ���[�J����LLM���g���ꍇ�̓��f�������w�肷��(gguf�t�@�C��)
    public string modelName = "gpt-3.5-turbo.gguf";
    // Dify��API���g���ꍇ��API�L�[���w�肷��
    public string difyApiKey = "";
    // ������LLM�V�X�e���v�����v�g�̋L�q���ꂽ�t�@�C����
    public string agentSystemPromptFile = "";
    // �����LLM�V�X�e���v�����v�g�̋L�q���ꂽ�t�@�C����
    public string emotionSystemPromptFile = "";
    public string userName = "User";
    public string assistantName = "Assistant";
    public int maxContextLength = 8192;
    public string welcomeMessage = "���ł������Ă��������ˁI";
    public string waitMessage = "\"�i�l�����ł��c�j\"";
    public float temperature = 0.7f;
}