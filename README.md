# YMM-REC-Plugin

YukkuriMovieMaker 4 用録音プラグインです。  

## 主な機能

- マイク録音（WAV / 48kHz / 16bit / Mono）
- 録音停止後にタイムラインへ自動反映
- 再生成ボタン（再録音）
- 再生ボタン（録音音声の確認）
- 連続録音フロー
- 録音停止後、次のセリフを自動準備
- 録音ウィンドウを閉じずに続けて録音可能

## 保存先

録音ファイルは以下に保存されます。

`<YMM4フォルダ>\user\plugin\YMM-REC-Plugin\Records`

例:
`C:\Users\<User>\Desktop\YukkuriMovieMaker_v4_Lite\user\plugin\YMM-REC-Plugin\Records`

## インストール方法

1. リリースから最新の `.ymme` をダウンロード  
2. ダウンロードした `.ymme` を起動してインストール  
3. ツールから「素材同梱プラグイン」を起動

## 使い方（基本）

1. キャラクターボイス設定で VoiceItem を選択
2. 普段通りセリフを入力
3. 録音ウィンドウを開く
4. `録音開始` → `録音停止`
5. 自動でタイムラインへ反映
6. 続けて録音する場合はそのまま次を録音

## 使い方（ツール録音）

1. 最初のセリフを選択して録音
2. 停止後、ウィンドウ内のテキストが次セリフに切り替わる
3. そのまま `録音開始` を押して次を録音

補足:
- UI上の選択表示が動かないケースでも、内部ターゲット追跡で次セリフへ反映する設計です。
- セリフ不一致時の上書き事故を避けるためのガードを入れています。

## サードパーティライセンス

- NAudio
- License: Microsoft Public License (MS-PL)
- Copyright (c) NAudio contributors
- License text: https://github.com/naudio/NAudio/blob/master/license.txt

## ライセンス

MIT License
