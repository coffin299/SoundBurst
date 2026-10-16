# SoundBurst

Windows 専用の OSS サウンドボード／仮想ミキサーです。

物理マイクと登録した SE をミックスし、**SoundBurst Speakers → SoundBurst Microphone** の仮想経路経由で Discord / Zoom / OBS などへ流します。モニター（ヘッドホン）は別経路です。

> **現状の配布形態（署名なし OSS 期間）**  
> - アプリ: 署名なし（SmartScreen 警告あり得る）+ [Velopack](https://docs.velopack.io/) で GitHub Releases から自動更新  
> - 仮想ドライバ: **完全無署名は不可**。自己テスト証明書で署名し、利用者が **Test Mode** を有効化する必要があります  
> - ターゲット: 開発者／パワーユーザー（一般向けワンクリックは EV 署名後）

ライセンス: [Apache-2.0](LICENSE)

## アーキテクチャ

```
[物理マイク] --WASAPI--> \
                           +--> Mix (48 kHz float) --WASAPI--> [SoundBurst Speakers]
[SE クリップ] ----------> /                              |
                                                         v (ドライバ loopback)
                                              [SoundBurst Microphone] --> Discord 等
同じ Mix --WASAPI--> [ヘッドホン]（モニター）
```

## 要件

- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（アプリ開発）
- 仮想ドライバビルド時: Visual Studio 2022 + **WDK**

## アプリのビルド

```powershell
dotnet build SoundBurst.sln -c Release
dotnet run --project src/SoundBurst.App
```

## 仮想ドライバ（Test Mode）

詳細は [driver/SoundBurstVad/README.md](driver/SoundBurstVad/README.md)。

```powershell
# 管理者 PowerShell
.\scripts\fetch-sysvad.ps1
.\scripts\apply-soundburst-branding.ps1
# Visual Studio + WDK で x64 ビルド → driver\SoundBurstVad\out\x64 へ配置

.\scripts\create-test-cert.ps1
.\scripts\enable-testsigning.ps1   # 再起動
.\scripts\test-sign-driver.ps1 -DriverDir .\driver\SoundBurstVad\out\x64
.\scripts\install-driver.ps1     -DriverDir .\driver\SoundBurstVad\out\x64
```

ドライバが無くても、アプリは任意の再生デバイスへミックス出力できます（仮想マイクとしては使えません）。

## Discord 設定

1. SoundBurst でミックス開始  
2. Discord → 音声・ビデオ → 入力デバイス = **SoundBurst Microphone**  
3. ノイズ抑制をオフ（SE が消えにくい）

## 自動更新（Velopack）

アプリは起動時に GitHub Releases（`https://github.com/coffin299/SoundBurst`）を確認できます。

ローカルでパッケージ作成:

```powershell
.\scripts\publish-velopack.ps1 -Version 0.1.0
# アップロード例:
# .\scripts\publish-velopack.ps1 -Version 0.1.0 -Repo coffin299/SoundBurst -Upload
```

タグ `v0.1.0` を push すると `.github/workflows/release.yml` が Velopack 成果物を Release に添付します。

**ドライバ更新はアプリ更新と分離**しています（再起動・管理者権限が必要なため）。

## リポジトリ構成

```
src/SoundBurst.App/      WPF UI + Velopack
src/SoundBurst.Audio/    NAudio エンジン・ホットキー
src/SoundBurst.Core/     設定・モデル
driver/SoundBurstVad/    SysVAD 派生ドライバ手順・INF・ノート
scripts/                 ビルド / テスト署名 / インストール / 公開
```

## UI について

計画当初は WinUI 3 でしたが、本環境の Appx/PRI ビルドツール不足のため **WPF** で同等機能を実装しています。オーディオエンジンとドライバ設計は計画どおりです。

## トラブルシュート

| 症状 | 対処 |
|------|------|
| 仮想デバイスが出ない | Test Mode・証明書・`install-driver.ps1` を確認 |
| `bcdedit` が失敗 | Secure Boot を一時無効化 |
| SE が Discord で消える | Discord ノイズ抑制オフ |
| 更新が「開発実行中」 | Velopack インストーラ経由で入れたビルドが必要 |
