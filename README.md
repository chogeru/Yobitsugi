# ヨビツギ / Yobitsugi

夕暮れで時間の止まった無人の町を舞台にした、**リミナル探索ホラー**。
**2D（ノベル）パートと 3D（探索）パートを行き来する**構成で、ノベルの選択がフラグとして残り、探索で集めた手がかりが物語を進めます。

- ティザーサイト: https://yobitsugi-abu.abubu.chatgpt.site/
- 制作: ABUBU / 現在は企画・プロトタイプ段階

---

## 動作環境

| 項目 | 内容 |
|---|---|
| Unity | 6000.3.12f1 (Unity 6) |
| レンダーパイプライン | URP 17.3 |
| 入力 | Input System 1.19 |

### 依存アセット

UPM パッケージ（`Packages/manifest.json` で自動解決）:
`com.unity.inputsystem` / `com.unity.render-pipelines.universal` / `com.unity.ugui` / `com.cysharp.unitask` / `com.unity.ai.navigation`

Asset Store 製の有料アセット（`Assets/Plugins`, `Assets/Beautify`, `Assets/vHierarchy`）:

| アセット | 用途 | 無い場合 |
|---|---|---|
| DOTween Pro | 全アニメーション（暗転・文字送り・立ち絵） | **必須**。無いとコンパイル不可 |
| Odin Inspector | インスペクターの整理と説明表示 | 無くても動作（`ODIN_INSPECTOR` で分岐済み） |
| Beautify (URP) | ポストエフェクト | 無くても動作（Renderer Feature を外す） |
| vHierarchy | ヒエラルキー整理の開発補助 | 無くても動作 |

> ⚠️ これらは再配布不可のライセンスです。リポジトリを公開する場合は取り扱いにご注意ください。

必要な Scripting Define: `UNITASK_DOTWEEN_SUPPORT`（設定済み）

---

## はじめかた

1. `YobitsugiGame` を Unity 6000.3.12f1 で開く
2. `Assets/Scenes/Yobitsugi_Base.unity` を開いて再生

ゲーム開始時はノベルパート（イントロ）が流れ、終了すると自動で 3D 探索に切り替わります。

### 操作

| 場面 | 操作 |
|---|---|
| ノベル | クリック / 決定ボタン（Enter・ゲームパッド）で送り。文字送り中の入力で全文表示 |
| 探索 | WASD 移動、マウス視点、`E` 調べる、`Shift` ダッシュ、`C` しゃがみ |
| 共通 | `Esc`（またはキャンセルボタン）でシステムメニュー。右上の `≡` でも開閉 |

セーブデータは `Application.persistentDataPath/save_slot_{0..2}.json` に保存されます。

---

## プロジェクト構成

```
Assets/
├─ Art/                      作業用の元アセット（ビルドには含めない素材置き場）
│  ├─ Characters/<キャラ>/<表情>.png   立ち絵の元画像
│  ├─ Fonts/                 日本語フォント（TMP 移行用・未配置）
│  └─ KeyVisual/             キャラが描き込まれたキービジュアル（背景には使わない）
├─ Editor/                   エディタ拡張（後述のツール群）
├─ Prefabs/                  Player / VN Canvas / System Menu Canvas
├─ Resources/                実行時に ID で検索するアセット
│  ├─ VNScenes/              シナリオ本体（VN Scene アセット）
│  ├─ Characters/            キャラクター定義
│  ├─ Backgrounds/           背景画像
│  └─ Voices/                ボイス
├─ Scenario/                 シナリオ CSV（執筆用の中間データ）
├─ Scenes/Yobitsugi_Base.unity
└─ Scripts/
   ├─ Core/                  モード切替・セーブ・フラグ・イベント
   ├─ VisualNovel/           ノベル進行・立ち絵・シナリオデータ
   ├─ UI/                    HUD・システムメニュー・暗転
   ├─ Sequences/             イベント（演出手順）
   ├─ Player/                一人称移動と調べる操作
   ├─ Interactables/         手がかり・扉・脱出地点
   └─ Audio/                 BGM・環境音・SE・ボイス
```

シーン内は `[Systems] / [UI] / [Level] / [Actors] / [Events]` に分けて配置しています。

---

## 設計

### 2D ⇄ 3D の切り替え

`GameModeManager` が唯一の切り替え地点です。必ず暗転（`ScreenFader`）を挟み、暗転中にモードを入れ替えるため、切り替わる瞬間に反対側の画面が見えることはありません。

- ノベルへ: プレイヤー操作停止 → カーソル表示 → ノベル UI 表示
- 探索へ: ノベル UI 非表示 → プレイヤー操作再開 → カーソルロック

HUD の表示切り替えは `GameModeManager` が直接持たず、`GameEvents.OnModeChanged` を購読する `ModeVisibility` が行います。

### MVP（View と Presenter の分離）

UI のロジックは MonoBehaviour に依存しません。シーンが無くてもテストできます。

| View（MonoBehaviour） | Presenter（素の C#） |
|---|---|
| `VNUI` / `VNPortraitView` | `VNPresenter` |
| `SystemMenuView` | `SystemMenuPresenter` |

View は「描画」と「押された事の通知」だけを担い、進行判断は Presenter 側にあります。

### イベントによる疎結合

`GameEvents` が唯一の連絡窓口です。オーディオ・HUD・バックログはこれを購読するだけなので、ゲーム進行側はそれらの存在を知りません。

発行されるイベント: モード切替 / ノベルシーン開始・終了 / 台詞表示 / ボイス要求 / 手がかり取得 / セーブ・ロード完了

### セーブ

`ISaveParticipant` を実装したコンポーネントを `SaveCoordinator` が自動収集します。**保存項目を増やす時もコーディネータ側の変更は不要**です。

現在の参加者: `GameManager`（手がかり）/ `StoryFlags`（フラグ）/ `GameModeManager`（位置・ノベル進行）

書き込みは一時ファイル経由（書き込み中にクラッシュしても既存セーブは壊れません）、`version` によるスキーマ管理付き。

### シーケンス（イベント演出）

`GameSequence` に `SequenceStep` をインスペクタで並べて演出を組みます（ノベル再生 / 待機 / 暗転 / ワープ / 表示切替）。新しい挙動はステップを 1 つ書き足すだけで、実行側の変更は不要です。

---

## シナリオの作りかた

シナリオは **`Assets/Resources/VNScenes` の VN Scene アセットが唯一の正データ**です（コード内に台詞はありません）。

### 1 行に設定できるもの

| 項目 | 説明 |
|---|---|
| 話者 | キャラクター定義を指定（名前と名前色が自動反映）。ナレーションは空欄 |
| 本文 | 表示テキスト |
| 背景 | 設定した行で背景が切り替わる（クロスフェード） |
| ボイス | 再生する音声。オート送り時はボイス終了まで待ちます |
| 立ち絵 | 登場 / 退場 / 表情変更 / 移動 / 感情表現 |
| フラグ | この行で立てるフラグ、表示条件フラグ、除外フラグ |
| 選択肢 | 遷移先の行、立てるフラグ、表示条件フラグ |

立ち絵は左右に配置され、話者が明るく、それ以外は暗くなります。

### CSV での執筆

`Yobitsugi/Scenario/Export Selected VN Scene to CSV` で書き出し、表計算ソフトで執筆してから `Import CSV into Selected VN Scene` で取り込めます。カンマや改行を含む台詞も扱えます。

### 立ち絵の追加

`Assets/Art/Characters/<キャラ名>/<表情>.png` に画像を置いて `Yobitsugi/Build Character Definitions from Art` を実行すると、Sprite 設定の修正込みでキャラクター定義が生成・更新されます（表示名や名前色などの手調整は保持されます）。

---

## エディタツール（`Yobitsugi` メニュー）

| メニュー | 内容 |
|---|---|
| Build Base Scene | ベースシーンを再生成（確認ダイアログあり） |
| Validate VN Scenes | 空の行・範囲外ジャンプ・ID 重複などを検査 |
| Validate Scene References | シーン内の未設定参照・スクリプト欠損を検査 |
| Build Character Definitions from Art | 立ち絵フォルダからキャラクター定義を生成 |
| Scenario/Export・Import | シナリオ CSV の書き出し・取り込み |

**Build Base Scene はシーンを作り直します。** 手作業の変更を残したい場合は、対象を `Assets/Prefabs` のプレハブ側で編集してください（プレハブが存在すればビルダーはそれを配置します）。シナリオアセットも上書きされません。

---

## 今後の予定

- TextMeshPro への移行（日本語フォント Noto Sans JP の配置待ち。現状はレガシー UI Text）
- ノベルパートのテスト整備（`VNPresenter` の分岐・フラグ・オート/スキップ）
- アセンブリ定義（asmdef）による依存方向の強制
- HIDE（足音から隠れる）システム — 保留中
