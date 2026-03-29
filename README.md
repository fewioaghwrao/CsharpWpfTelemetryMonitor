# センサー監視シミュレーション

C# (.NET 8 WPF) を用いて開発したデスクトップアプリケーションです。  
仮想センサーからのデータストリームをリアルタイム可視化し、しきい値判定によるアラート通知・ログ記録・履歴エクスポートに対応しています。

---

## 概要

- 仮想センサー（サイン波・ランダムウォーク）のストリーム生成
- 折れ線グラフでのリアルタイム可視化（OxyPlot）
- しきい値超過時にトースト通知および Serilog によるログ出力  
  （表示 ⇒ 計測ログ）
- `appsettings.json` のホットリロード対応  
  （稼働中に設定変更を即反映）
- 履歴データを CSV にエクスポート可能  
  （ファイル ⇒ エクスポート(CSV)）
- .NET標準ライブラリとNuGetだけで動作

![センサー監視アプリのスクリーンショット](docs/images/C1.png)

---

## 使用技術

- .NET 8 / WPF
- CommunityToolkit.MVVM（MVVM アーキテクチャ）
- OxyPlot（リアルタイムグラフ描画）
- Serilog（構造化ログ出力）
- Microsoft.Data.Sqlite（軽量データ保持、外部インストール不要）
- JSON 設定ファイル（ホットリロードによる柔軟な運用）

---

## 推奨動作環境

### OS

- Windows 10 / Windows 11  
  （.NET 8 および WPF の公式サポート対象）

### .NET ランタイム

- .NET 8 Desktop Runtime  
  （`net8.0-windows` でビルド）

### ハードウェア

- CPU: Intel Core i3 以上（推奨 i5 / i7 以上）
- メモリ: 4GB 以上（推奨 8GB 以上）
- ストレージ: SSD 推奨、空き容量 500MB 以上

### 画面解像度

- 推奨: 1280 × 800 以上  
  （UI が快適に表示されるため）

### その他

- 音声機能利用時: Windows 標準の音声合成（SpeechService）対応環境
- インターネット接続: GitHub からの取得や更新時のみ必要

---

## ビルド手順（開発者向け）

### 前提条件

- Windows 10 / 11
- Visual Studio 2022 以降  
  （.NET デスクトップ開発ワークロード）
- .NET 8 SDK

### 手順

1. リポジトリをクローン
```bash
   git clone https://github.com/fewioaghwrao/CsharpWpfTelemetryMonitor.git
```

2. Visual Studio で `TelemetryMonitor.sln` を開く
3. NuGet パッケージを自動復元
4. デバッグまたはリリースビルドを実行
5. `bin/Release/net8.0-windows/` に実行ファイルが生成されます

---

## 実行手順（利用者向け）

1. 配布パッケージ `SensorMonitor_v1.0.zip` を展開
2. `SensorMonitor.exe` を起動
3. 必要に応じて `appsettings.json` を編集  
   （しきい値・サンプリング周期など）

---

## 設定ファイル例
```json
{
  "AppSettings": {
    "SampleIntervalMs": 1000,
    "DatabasePath": "telemetry.db",
    "MaxInMemory": 2000,
    "AlertThreshold": 50.0
  }
}
```

---

## 関連リンク

- [GitHub リポジトリ](https://github.com/fewioaghwrao/CsharpWpfTelemetryMonitor)
- [詳細ドキュメント](#)
- [C#ポートフォリオトップ](#)

---

## ライセンス

MIT License の下で公開しています。  
詳細は `LICENSE` ファイルをご確認ください。