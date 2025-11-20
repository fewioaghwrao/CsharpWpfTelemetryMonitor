# センサー監視シミュレーション（C# / WPF / .NET 8）

このリポジトリは、WPF (.NET 8) を用いた **センサー監視シミュレーションアプリ** のソースコードです。  
リアルタイムグラフ表示、しきい値アラート、ログ出力、CSV エクスポートなどを備えています。

## 🔗 詳細ドキュメント
より詳細な README（スクリーンショット・仕様書・使用技術・セットアップ手順など）は  
以下のポートフォリオページにまとめています：

👉 (https://fewioaghwrao.github.io/my-portfoliohogwhigrox/CCharp/docs/sensor-readme.html)

## 🛠 主な機能
- 仮想センサー（サイン波・ランダムウォーク）生成
- リアルタイムチャート描画（OxyPlot）
- しきい値アラート（トースト通知 + Serilog）
- 設定ファイル（appsettings.json）ホットリロード
- 計測履歴の CSV エクスポート
- ローカル DB（SQLite）対応

## 📦 技術スタック
- .NET 8 / WPF
- CommunityToolkit.MVVM
- OxyPlot
- Serilog
- Microsoft.Data.Sqlite

## 🚀 ビルド／起動方法
Visual Studio 2022 で `.sln` を開いてビルドしてください。

