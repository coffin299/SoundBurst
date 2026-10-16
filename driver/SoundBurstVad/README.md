# SoundBurst Virtual Audio Driver (SoundBurstVad)

SysVAD 派生の最小仮想オーディオケーブルです。

- **SoundBurst Speakers** … アプリがミックス結果を再生する出力
- **SoundBurst Microphone** … Discord 等がマイクとして選ぶ入力（Speakers のループバック）

## 要件

- Visual Studio 2022 + **Windows Driver Kit (WDK)**（Windows 11 SDK と揃える）
- 管理者権限
- 当面は **テスト署名 + Test Mode**（EV / Hardware Dashboard 署名なし）

## ビルド概要

1. [Windows-driver-samples](https://github.com/microsoft/Windows-driver-samples) の `audio/sysvad` をベースにする  
2. 本ディレクトリの `branding.patch.md` と INF を適用し、エンドポイント名を SoundBurst にする  
3. Visual Studio で x64 / Release をビルド  
4. `../../scripts/create-test-cert.ps1` で証明書作成  
5. `../../scripts/test-sign-driver.ps1` で `.sys` / `.cat` をテスト署名  
6. `../../scripts/enable-testsigning.ps1` → 再起動  
7. `../../scripts/install-driver.ps1` でインストール

クイックスタート（サンプル取得）:

```powershell
..\..\scripts\fetch-sysvad.ps1
..\..\scripts\apply-soundburst-branding.ps1
# その後 WDK で driver\SoundBurstVad\sysvad\*.sln をビルド
```

## 注意

- 完全無署名のカーネルドライバは現代の Windows ではロードされません
- Test Mode ではデスクトップ右下に "Test Mode" 透かしが出ます
- Secure Boot 有効時は `bcdedit` が拒否されることがあるため、一時的に Secure Boot を無効化してください
- カーネルドライバの不具合は BSOD の原因になり得ます。まずはテスト機で検証してください
