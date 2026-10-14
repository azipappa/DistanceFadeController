# DistanceFadeController 0.1.0 (β)

VRChat PCアバターのlilToon距離フェードを、ビルド時の複製に一括設定するUnity Editor拡張です。
元のMaterial・Prefab・AnimationClip・AnimatorControllerは変更しません。
現在はβ版として配布します。

## 導入

- Unity **2022.3.22f1**、VRChat Avatars SDK 3.x、NDMF **1.11.0以上**、lilToonを使用します。
- このフォルダーをプロジェクトの `Packages/com.azipa.distance-fade-controller` に配置してください。
- VPM配布には、このフォルダーの中身をルートにしたZIPをリリースへ公開し、そのURL・SHA256をVPMリポジトリのバージョンエントリーへ登録します。VCCでそのリポジトリを追加すると依存解決と更新が可能です。配布URLは未設定です。
- Modular Avatarは任意です。アセンブリの参照・必須依存には含めません。

## 使い方

1. Hierarchyで `VRCAvatarDescriptor` のあるアバタールートを右クリックします。
2. `DistanceFadeController > Add Controller` を選びます。
3. 追加されたPrefabのInspectorで距離・カラー・対象範囲を設定します。
4. VRChat SDKからビルドします。NDMFが複製したマテリアルとアニメーションに設定を反映します。

追加操作はUndoに対応し、重複追加時には既存Controllerを選択します。Prefab Asset、Prefab Mode、複数選択では追加できません。
`Select Controller` で既存の設定へ移動できます。削除すれば次回ビルドから処理されません。

## 設定

上書きチェックを外したプロパティは元の値を維持します。Controllerの上書き全体を無効にするとマテリアルは変更されません。
「上書きする項目」の「裏面を影にする」は距離設定と独立して切り替えられます。OFFでは元マテリアルの `_DistanceFade.w` を維持し、ONでは距離フェード欄のチェック状態を適用します。初期値は上書きOFFです。
フェード有効を外した場合は、パラメータ上書きが有効なときに `_DistanceFade.z = 0` を設定します。

公式lilToon **2.3.4** のInspector、シェーダーを確認した対応:

| プロパティ | 設定 |
| --- | --- |
| `_DistanceFade.x` | 開始距離（m） |
| `_DistanceFade.y` | 終了距離（m） |
| `_DistanceFade.z` | 強度（0で無効） |
| `_DistanceFade.w` | 裏面を強制的に影にする |
| `_DistanceFadeMode` | 0: Vertex、1: Object Position |
| `_DistanceFadeColor` | HDRカラー、アルファ |
| `_DistanceFadeRimColor` | HDRリムカラー、アルファ |
| `_DistanceFadeRimFresnelPower` | リムのフレネル強度 |

開始距離 > 終了距離は正規の使い方です。初期値は0.1m → 0.01m、強度1です。同一距離はゼロ除算になるので拒否します。
Multi版では公式の `SetupMultiMaterial(Material)` でキーワードを更新します。

Inspectorは公式の順序（色・開始距離・終了距離・強度・裏面を影にする・モード・リムライト）に合わせています。強度0で無効化し、裏面はチェックボックスで設定します。リムライトの色と不透明度は別表示で、細さは公式と同じPower 3のスライダーです。右端のチェックで項目ごとの上書きを切り替えられます。

Allはアバター全体、Excludeは指定した対象を除外、Include Onlyは指定した対象のみ適用します。
GameObject指定は子階層を含みます。Renderer・Materialの個別指定にも対応します。複数指定は和集合です。
非アクティブ衣装と無効Rendererも収集します。Inspectorの集計は現在のScene割当のユニークマテリアル数です。

## Material Swapとビルド順序

NDMF TransformingでMAの通常処理と後段Transformingの後に実行します。NDMF AnimatorServicesの公開APIを使用し、Animator、VRChatのPlayable Layers、BlendTree、AnimatorOverrideControllerを仮想化した結果のマテリアル曲線を処理します。
対象パス・Rendererを解決し、曲線ごとにInclude/Excludeを適用するため、同一マテリアルを使う除外オブジェクトへ誤適用しません。
生成マテリアルはNDMF AssetSaverへ保存し、クリップとコントローラーの複製・保存はNDMFが管理します。本ツール独自の恒久アセットは作成しません。

未解決パス、非標準Material参照、変更できないマーカークリップ、Legacy Animationは警告します。
距離フェード値を直接アニメーションするFloat Curveは保持します。そのアニメーションがビルド設定を再上書きする場合があります。
後続の最適化ツールがマテリアルを置換する場合は、そのツールとの個別検証が必要です。
ライブプレビューは実装していません。NDMF Manual Bakeで複製結果を確認できます。
有効なControllerを複数置くとビルドを中止します（優先順位制御は将来対応）。

## テスト

Unity Test RunnerでパッケージのEditorテストを実行します。プロジェクトの `Packages/manifest.json` の `testables` にこのパッケージ名を追加してください。
テスト結果と実環境での未検証項目は [Verification.md](Documentation~/Verification.md) を参照してください。

## English

Right-click a VRChat avatar root → DistanceFadeController → Add Controller. Edit the connected prefab. Per-property overrides and All/Exclude/Include Only filters apply to build copies, including inactive renderers and material swap curves. Original assets remain untouched. Delete the controller to stop future overrides. NDMF is required; Modular Avatar is optional. Inspector labels follow the OS language, with bilingual tooltips. See [Usage](Documentation~/Usage.md).

## 参考資料

- [NDMF公開APIとビルドフェーズ](https://ndmf.nadena.dev/)
- [lilToon公式ソース](https://github.com/lilxyzw/lilToon/tree/master/Assets/lilToon)
- [Unity 2022.3 MenuItem](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MenuItem.html)

MIT License.
