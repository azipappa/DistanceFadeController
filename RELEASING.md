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

`OWNER/REPOSITORY` を作成したリポジトリに置き換えてください。リモート設定・pushはまだ実行していません。

## 自動処理

`.github/workflows/release.yml` が `v*` タグで起動します。

1. タグのバージョンとパッケージの `package.json` の `version` が一致するか確認。
2. 必要ファイルとUnity `.meta` のGUID・Prefab参照を確認。
3. パッケージの中身をZIPのルートに配置して、ZIP・SHA256・リリースノートを生成。
4. GitHub Releaseを作成し、ZIPとSHA256を添付。

`0.1.0` のような `0.x.x` はβ版として **Pre-release** に設定します。`1.0.0-beta.1` のようなプレリリース識別子も同様です。`1.0.0` 以降でプレリリース識別子のないものは通常リリースになります。
標準の `GITHUB_TOKEN` を使用するため、追加のトークンやSecretsは不要です。リポジトリでGitHub Actionsとworkflowの書き込み権限が許可されている必要があります。

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

GitHub ReleaseのZIP公開と、VCCが読み込むVPMリポジトリは別の設定です。
VPMリポジトリの各バージョンエントリーに `package.json` の内容とGitHub ReleaseのZIP URL・SHA256を登録し、リポジトリJSONをホストする必要があります。
今回のworkflowはVPMリポジトリJSONやGitHub Pagesを更新しません。
