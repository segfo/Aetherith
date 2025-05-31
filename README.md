# Aetherith
Avatar Engine THE Responsive Interface Tunable Helperの略。  
好きなアバターを設定して好きなLLM（GGUF量子化）を好きなプロンプトで動かせます。  
たまに動くよ。好きなアニメーション入れてもいいかもね。（将来の話）  

## 使っているところ(gifアニメ)
止まってたらスーパーリロードしてみてください
![Animation](README_resources/Play.gif)

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
- VRM拡大縮小
- マスコットを動かしたとき、ゆれ物が動く機能
- 設定ファイルの動的適用
- 背景透過・不透過設定（UI調整用機能）
  
# お前を消す方法
- 方法1：マスコットをクリックしてから`ALT+F4`
- 方法2：チャット窓に`/exit`して送信
- 方法3(ネタ)：「お前を消す方法」って入力したらなんかインタラクトできるようにしたい（未実装）

# 初回起動時に作成されるファイル
- appconfig.json
- systemprompt.txt: 会話用LLMのシステムプロンプトです。好きにしてください。
- emotional_systemprompt.txt: 表情推定用LLM用のシステムプロンプトです。これにはちょっと縛りがあるので例（後述）を参考にしてうまいことやってください。
  
# 設定ファイル(appconfig.json)
## LLMの設定

### **アプリ全体の設定**

| フィールド名            | 説明           |
| -----------------------| ----------------- |
| `characterLlm`         | キャラクターの会話AI設定     |
| `emotionLlm`           | 感情分析用AI設定         |
| `vrm`                  | キャラクターの見た目・動き設定   |
| `BackgroundWindowTransparent` | ウィンドウの背景を透明にするかどうかを設定します。（trueで透明、falseで不透明）チャットUIやキャラクターの配置調整に使用します |
| `chatWindowBgRGBA`     | チャットの背景色          |
| `chatInputWindowBgRGBA`| 入力ボックスの背景色        |
| `welcomeMessage`       | 起動時のメッセージ         |
| `waitMessage`          | 考え中のメッセージ         |
| `shakeMessage`         | マスコットを振ったときの反応    |
| `shakeForceThreshold`  | 振られたと判定する強さ       |
| `chatTypingInterval`   | 文字の表示スピード（タイピング風） |

### **AI関連設定（`characterLlm`と`emotionLlm`）**
設定ファイル動的ロード機能の対象外です（適用にはアプリの再起動が必要です）
| フィールド名             | 説明            |
| ------------------ |  ------------------ |
| `useDify`          | Difyを使うかどうか   |
| `difyApiUrl`       | AIサービスの接続先         |
| `modelName`        | ローカルで使うAIモデルのファイル名（gguf） |
| `difyApiKey`       | Difyの認証キー        |
| `systemPromptFile` | システムプロンプトのファイル名 |
| `userName`         | ユーザーの名前  |
| `assistantName`    | アシスタントの名前     |
| `maxContextLength` | 最大コンテキスト長     |
| `temperature`      | AIの自由さ・創造性のレベル     |
| `topP`             | トップPフィルタリング / 応答の多様性制御（確率フィルター）  |
| `topK`             | トップKフィルタリング / 応答候補の数の制限          |
| `numGPULayers`     | GPUにオフロードするレイヤー数。GPUがあれば使ってください。スペックに見合わない数字だと動かなくなるので1から始めたほうがよいです。 |

### **キャラクターの見た目設定（`vrm`）**

| フィールド名                   | 説明  |
| ----------------------------- | ------------ |
| `ToneMappingMode`             | トーンマッピングモード    |
| `LightIntensity`              | ライトの強さ   |
| `LightColorRGBA`              | ライトの色 （肌色調整など）   |
| `ShadowStrength`              | キャラクターの影の濃さ     |
| `Scale`                       | VRMの表示スケール      |
| `VrmDisplayOffsetX/Y`         | 画面内でのキャラの位置調整<br>X軸: +値=右方向移動、-値=左方向移動<br>Y軸: +値=上方向移動、-値下方向移動   |
| `ChatInputOffsetX/Y` | 画面内でのチャットUIの位置調整<br>X軸: +値=右方向移動、-値=左方向移動<br>Y軸: +値=上方向移動、-値下方向移動|
| `FileName`                    | 使用するVRMファイル名 （設定ファイル動的ロード機能の対象外です。適用にはアプリの再起動が必要です）  |
| `springBone` | スプリングボーンの設定 | 物理動作に関する設定 |
| `blinkExclusionExpressionTreshold`  | 瞬きを抑制（瞬き禁止の場合は許可）する表情と、瞬きを抑制する表情の重み閾値を設定します（`{"Surprised":0.4,...}`であれば、「驚き」の表情の重みが0.4よりも大きい時、瞬きが抑制されます。`1.0`にすると常に瞬きします） |
| `blinkDisable` | デフォルトの瞬きを禁止するかどうか。糸目キャラに有効です。blinkExclusionExpressionTresholdの意味が反転します(`{"Surprised":0.4,...}`であれば、「驚き」の表情の重みが0.4よりも大きい時、瞬きします。`1.0`にすると一切瞬きしません) |

### **物理動作 (`SpringBone`)**

| フィールド名                | 説明          |
| -------------------------  | ---------------- |
| `ExternalForceMultiplier`  | 揺れる部分（髪や服）の反応の速さ |
| `MaximumMovementForce`     | 揺れる大きさの上限        |

### **表情設定 (`BlinkExclusionExpressionTreshold`)**

| フィールド名            | 説明               |
| ---------------------- | --------------------- |
| `Happy, Sad, Angry...` | 目が細まるような表情（笑顔・悲しみ）などのときに、瞬きを抑えるかの設定。</br>`blinkDisable`をtrueにしたうえで、目を見開く驚きなどの表情に重み(0.9以下)を設定すると瞬きするようになります。（細目キャラの過剰な瞬きを抑制するための機能です） |

# 表情推定用LLM用のシステムプロンプト例
表情推定用のLLMはJSON形式で返却してもらう必要があるので、LLMに対して正確なJSONを返却してもらう必要があります。
以下のような制約を付けたプロンプトを書く形です。
ただし、LLMのモデルによっては正確なJSONを返さないこともあるので以下の例を参考にチューニングしてください。
Gemma-3 120億パラメータ 4bit量子化モデル（gemma-3-4b-it-q4_0.gguf）に対するプロンプトです。
```
# あなたの役割
あなたは感情推定AIです。
渡される発言は**ユーザの発言**です。
**ユーザの発言**を受けた時の**AI側の感情を推定して**ください。

# 感情推定を行うAIのシステムプロンプト
```
あなたは優秀なアシスタントAIです
```
# 注意事項
- AI側の感情を推定してください。
- 表情に反映されます、できるだけシンプルな表情を選んでください。

# 感情表現について
- JSON Schema形式で応答してください。
- 応答フォーマットの <<Value>> で示される値を感情に合わせて変更してください。
- 応答は応答例に従ってください

## 応答フォーマット
{"Happy": <<Value>>, "Sad": <<Value>>, "Angry": <<Value>>,"Neutral": <<Value>>,"Surprised": <<Value>>,"Relaxed": <<V
alue>>}

## 応答例
{"Happy": 0.6, "Sad": 0, "Angry": 0,"Neutral": 0,"Surprised": 0,"Relaxed": 0.4}

## <<Value>>の範囲
最小：0
最大：1
```
プログラム側では、このプロンプトと発言内容から出力される  
`{"Happy": 0.6, "Sad": 0, "Angry": 0,"Neutral": 0,"Surprised": 0,"Relaxed": 0.4}`  
というJSONを解釈して表情を動的に変更しています。  
