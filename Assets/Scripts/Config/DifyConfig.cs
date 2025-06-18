// Dify固有の設定項目
using UnityEngine;

[System.Serializable]
public class DifyConfig {
    public string apiUrl = string.Empty;
    // DifyのAPIを使う場合はAPIキーを指定する
    public string apiKey = string.Empty;
    public bool conversationHandover = true; // Difyでの会話の引継ぎ
    public string handoverConversationIdKeyName = "HandoverConversationIdKeyCharacter";
    public static void InitConversationHandover(LLMConfig conf)
    {
        // 会話引継ぎモードがオフの時は、新しい会話を始める。
        if (!conf.Dify.conversationHandover)
        {
            PlayerPrefs.SetString(conf.Dify.handoverConversationIdKeyName, string.Empty);
            PlayerPrefs.Save(); // 明示的に保存する
        }
    }
}