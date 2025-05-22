# Aetherith
Avatar Engine THE Responsive Interface Tunable Helperの略
好きなアバターを設定して好きなLLM（GGUF量子化）を好きなプロンプトで動かせます。
たまに動くよ。好きなアニメーション入れてもいいかもね。（将来の話）

# ビルド方法（Unityでの開き方）
1. UnityHubをインストールします
2. UnityHubで`Unity Editor 6000.0.47f1`をインストールします
3. GitHubからプロジェクトをクローンします
`git clone https://github.com/segfo/Aetherith`
4. シーンファイルをダブルクリックします
`./Aetherith/Assets/Scenes/SampleScene.unity`
5. ロードされてウィンドウが開いたらいったん閉じます
6. もう一度シーンファイルをダブルクリックします
`./Aetherith/Assets/Scenes/SampleScene.unity`
7. 多分いい感じにGameObjectとかが配置されるはずなのでVRMとLLMを以下のディレクトリ直下に配置します
VRM `Assets/StreamingAssets/VRM/`
LLM `Assets/StreamingAssets/LLM/`
5. UnityEditorのメニューバー > File > Build And Run
でビルドできます。（初回ビルドはEditor上で実行してから、ログを見て実行時エラーが無いことを確認するとよいでしょう。）

# 実装済みの機能
- VRM 1.0のみに対応しています。0.x系は使えません。挙動を安定させるために切り捨ててます。
- ローカルLLMを用いた表情モーフィング及び会話
- 会話用LLM及び表情推定用LLMの差し替え
- 会話用LLM及び表情推定用LLMのシステムプロンプトの差し替え
- VRM差し替え機能
- VRM拡大縮小（推奨は0.8～1.8倍くらい）
- マスコットを動かしたとき、ゆれ物が動く機能
  
# 初回起動時に作成されるファイル
- appconfig.json
- systemprompt.txt: 会話用LLMのシステムプロンプトです。好きにしてください。
- emotional_systemprompt.txt: 表情推定用LLM用のシステムプロンプトです。これにはちょっと縛りがあるので例（後述）を参考にしてうまいことやってください。
  
# 設定ファイル(appconfig.json)
## LLMの設定
- useDify: DifyのAPIを使うときの設定です。まだ実装できてませんが実装する予定です。ていうか一番欲しい機能なので。
- difyApiKey: DifyのAPIキーです。上記同様まだ実装していません。
- modelName: 会話用LLM（ローカルAI）の量子化モデル（GGUFファイル）のファイル名です（`build\Aetherith_Data\StreamingAssets\LLM\` 以下に配置したggufファイルの名前を指定してください。デフォルトは`Gemma3.gguf`です）
- llmEmotionModelName: 表情推定用LLM（ローカルAI）の量子化モデルのファイル名です。ディレクトリはmodelNameと同じ場所から読み込まれます。
- agentSystemPromptFile: 会話用LLM（ローカルAI）のシステムプロンプトです。
- emotionSystemPromptFile: 表情推定用LLM（ローカルAI）のシステムプロンプトです。好きに書いていいわけではなく縛りがあります。（後述）
- userName: ユーザ名です。（多分ここで指定するよりもシステムプロンプトで指定した方がいいかも）
- assistantName: AIアシスタント名です。（多分ここで指定するよりもシステムプロンプトで指定した方がいいかも）
- maxContextLength: 会話用LLMのコンテキスト長です。モデルに合わせて増減させてください。
- llmEmotionMaxContextLength: 表情推定用LLMのコンテキスト長です。モデルに合わせて増減させてください。
- welcomeMessage: 起動時、チャットを何もしていないときに表示されるメッセージです。
- waitMessage: LLMが応答を返すまでの時間に表示するメッセージです。
- temperature: 会話用LLMがどの程度システムプロンプトに従うかの重みです。（`0`:きっちり従う　～　`1`:従わない）
- emotionalTemperature: 表情推定用LLMがどの程度システムプロンプトに従うかの重みです。（きっちり従ってほしいので、`0.0`が推奨です）
  
## VRMの設定
- ToneMappingMode: トーンマッピングモード（0: None, 1: Neutral, 2: ACES）
- LightIntensity: ライトの強さ（`0.0`～`1.0`）
- LightColorRGBA: ライトの色(#RRGGBBAA の書式で記載します。例：`#FFF0E0FF`)
- ShadowStrength: モデルに対する影の入りの強さ（`0.0`～`1.0`）
- Scale: VRMの大きさ倍率（身長160cmのモデルでおおよそ`0.5`～`1.8`くらいまで。170cmだと1.7倍までかも。）
- VrmDisplayOffsetX: VRMの表示位置のオフセット（デフォルト値: `-1.5`　変えない方がよいと思います。VRMが見切れるかも）
- VrmDisplayOffsetX: VRMの表示位置のオフセット（デフォルト値: `-0.15`　変えない方がよいと思います。VRMが見切れるかも）
- BackgroundWindowTransparent: 今は使っていません。将来的に使うと思います。（主にVrmDisplayOffset等をいじるときに必要なので）
- FileName: VRMのファイル名です。（`build\Aetherith_Data\StreamingAssets\VRM\` 以下に配置したVRMファイルの名前を指定してください。デフォルトは`Default.vrm`です）
  
### SpringBoneの設定
- springBone > ExternalForceMultiplier: 揺れの倍率。`0.01`が推奨値です。`0.1`だとめっちゃ揺れるのでお勧めしません。
- springBone > MovementThreshold: 揺れの最大値(`0`～`1`)。上限を1とかにすると早く動かしたときめっちゃ揺れます。
  
# 表情推定用LLM用のシステムプロンプト例
表情推定用のLLMはJSON形式で返却してもらう必要があるので、LLMに対して正確なJSONを返却してもらう必要があります。
以下のような制約を付けたプロンプトを書く形です。
ただし、LLMのモデルによっては正確なJSONを返さないこともあるので以下の例を参考にチューニングしてください。
Gemma-3 12億パラメータ 4bit量子化モデル（gemma-3-4b-it-q4_0.gguf）に対するプロンプトです。
```
# あなたの役割
あなたは感情推定AIです。
渡される発言は**ユーザの発言**です。
**ユーザの発言**を受けた時の**AI側の感情を推定して**ください。

# 感情推定を行うAIのシステムプロンプト
\`\`\`
あなたは優秀なアシスタントAIです
\`\`\`
# 注意事項
- AI側の感情を推定してください。
- 表情に反映されます、できるだけシンプルな表情を選んでください。

# 感情表現について
- JSON Schema形式で応答してください。
- 応答フォーマットの <<Value>> で示される値を感情に合わせて変更してください。
- 応答は応答例に従ってください

## 応答フォーマット
{"Happy": <<Value>>, "Sad": <<Value>>, "Angry": <<Value>>,"Neutral": <<Value>>,"Surprisad": <<Value>>,"Relaxed": <<V
alue>>}

## 応答例
{"Happy": 0.6, "Sad": 0, "Angry": 0,"Neutral": 0,"Surprisad": 0,"Relaxed": 0.4}

## <<Value>>の範囲
最小：0
最大：1
```
プログラム側では、このプロンプトと発言内容から出力される  
`{"Happy": 0.6, "Sad": 0, "Angry": 0,"Neutral": 0,"Surprisad": 0,"Relaxed": 0.4}`  
というJSONを解釈して表情を動的に変更しています。  
