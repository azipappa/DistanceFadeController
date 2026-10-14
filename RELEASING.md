# リリース手順

## 最初のアップロード

GitHubに空のリポジトリを作成してください（GitHub側でREADMEなどを追加せずに作成するとスムーズです）。
ローカルの初期コミットを送った後、バージョンタグを送ります。

```powershell
git remote add origin https://github.com/OWNER/REPOSITORY.git
git push -u origin main
git tag v0.1.0
git push origin v0.1.0
```

`OWNER/REPOSITORY` を作成したリポジトリに置き換えてください。この作業環境ではoriginは `https://github.com/azipappa/DistanceFadeController.git` に設定済みです。

## 自動処理

`.github/workflows/release.yml` が `v*` タグで起動します。

1. タグのバージョンとパッケージの `package.json` の `version` が一致するか確認。
2. 必要ファイルとUnity `.meta` のGUID・Prefab参照を確認。
3. パッケージの中身をZIPのルートに配置して、ZIP・SHA256・リリースノートを生成。
4. GitHub Releaseを作成し、ZIPとSHA256を添付。

`0.1.0` のような `0.x.x` はβ版として **Pre-release** に設定します。`1.0.0-beta.1` のようなプレリリース識別子も同様です。`1.0.0` 以降でプレリリース識別子のないものは通常リリースになります。
標準の `GITHUB_TOKEN` を使用するため、追加のトークンやSecretsは不要です。リポジトリでGitHub Actionsとworkflowの書き込み権限が許可されている必要があります。

`Settings > Secrets and variables > Actions` には **Secrets・Variablesとも追加不要**です。`GITHUB_TOKEN` を手動登録する必要もありません。workflow内に `permissions: contents: write` を指定済みです。

`main` のpush・Pull Requestではパッケージ作成を確認します。Unity Editorテストは自動実行しません。公開前に対象Unity環境で検証してください。

## 次のバージョン

1. `Packages/com.azipa.distance-fade-controller/package.json` の `version` を変更。
2. パッケージのCHANGELOGにそのバージョンの項目を追加し、READMEの表記も更新。
3. 変更をコミットし、mainをpush。
4. 同じバージョンのタグ（例：`v0.1.1`）を作成してpush。

既存のリリースタグは使い回さず、新しいバージョンで公開してください。

## ローカルでZIPを確認

Python 3.9以上で実行できます。

```powershell
py -3 scripts/build_release.py --tag v0.1.0
```

生成先はGit管理から除外した `Releases` フォルダーです。

## VPM / VCC配布

VPM Listingは `azipappa/azipa-tools-vpm` の `source.json` に登録しています。Listing側はZIP内の `package.json` を読み、ダウンロードURL・SHA256を付けてGitHub Pagesへ公開します。Releaseに別ファイルとしてpackage.jsonを添付する必要はありません。

1. このリポジトリの変更をcommit・pushし、`v0.1.0`タグをpush。
2. GitHub ReleaseへのZIP添付を確認。
3. `azipa-tools-vpm` の今回の変更をcommit・push。
4. Listingの `Build Repo Listing` を手動実行するか、毎時の自動更新を待つ。

Listing側も追加のSecrets・Variablesは不要です。`Settings > Pages > Build and deployment > Source` を **GitHub Actions** に設定してください。
Release未公開のリポジトリは取り込み前に一時的に除外され、公開後の更新で取り込まれます。登録元のsource.jsonからは削除しません。

VCCの `Settings > Packages > Add Repository` に次のURLを登録します。

```text
https://azipappa.github.io/azipa-tools-vpm/index.json
```

同一バージョンのZIPは公開後に差し替えず、修正時は新しいバージョンを公開してください。
