# YMM-REC-Plugin

YukkuriMovieMaker 4 用録音プラグインです。  

# 録音プラグインサポート終了のお知らせ

録音プラグインはYMM4バージョン4.53以降のバージョンで本体に付属されるようになりました


## 主な機能

- キャラクターボイスの録音とタイムラインへの自動反映
- マイク録音（WAV / 48kHz / 16bit / Mono）
- 録音停止後にタイムラインへ自動反映
- 再生成ボタン（再録音）
- 再生ボタン（録音音声の確認）
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
3. ツールから「録音プラグイン」を起動

## 使い方（キャラクター音声）

1. キャラクターボイス設定で VoiceItem を選択
2. 普段通りセリフを入力
3. 録音プラグイン→収録UIを開く
4. 使用中のセリフを確認して、録音開始終了
5. 自動でタイムラインへ反映
6. 続けて録音する場合はそのまま次を録音

## 使い方（ツール録音）

1. 録音プラグインを起動し、録音開始終了
2. 停止後、自動的にタイムラインに追加されます

## サードパーティライセンス

- NAudio
- License: Microsoft Public License (MS-PL)
- Copyright (c) NAudio contributors
- License text: https://github.com/naudio/NAudio/blob/master/license.txt

## ライセンス

MIT License
