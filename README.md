# DistanceFadeController 0.1.0 (β)

VRChatアバターのlilToon距離フェードを非破壊で一括設定するUnity Editor拡張です。
現在はβ版です。

- [導入・使い方](Packages/com.azipa.distance-fade-controller/README.md)
- [リリース手順](RELEASING.md)
- [検証結果と未検証項目](Packages/com.azipa.distance-fade-controller/Documentation~/Verification.md)

このリポジトリではパッケージ本体と配布処理を管理します。ローカルのアバター素材、Unityのキャッシュ、外部パッケージ、生成済みZIPはGit管理に含めません。

`v0.1.0` などのバージョンタグをGitHubへ送ると、GitHub ActionsがZIP・SHA256を作成しGitHub Releaseへ添付します。`0.x.x` とプレリリース識別子のあるバージョンは自動的に **Pre-release** として公開します。

VCC向けListingは `azipappa/azipa-tools-vpm` に登録しています。Release公開後、Listing側の更新でZIPを取り込みます。設定と公開手順は [リリース手順](RELEASING.md) を参照してください。

このプロジェクトは [MIT License](LICENSE) で公開しています。
