# Beat115Icon

Beat Saber 向け 115点カスタムアイコン表示MODです。  
パーフェクトカット（115点）を達成した際に、「115」という数字の代わりにカスタムPNGアイコンをランダム表示します。

**バニラ（Beat Saber標準）および NalulunaCutScore の両方に完全対応**しています。

---

## 🌺 主な特徴

1. **バニラ＆NalulunaCutScore ハイブリッド両対応**
   - **バニラ環境**: NalulunaCutScoreが導入されていなくても、Beat Saber標準のスコアポップアップ（`FlyingScoreEffect`）で115点が出た瞬間にアイコンを表示。
   - **NalulunaCutScore併用時**: NalulunaCutScoreのスコア表示も自動検出し、同様にアイコン化。
2. **数字を消してクリアな表示**
   - 115点達成時、数字テキストを完全に非表示にし、アイコンのみを綺麗に表示します。
3. **ゼロ遅延のランダム画像抽選**
   - `UserData/Beat115Icon/` フォルダ内のすべてのPNG画像をゲーム起動時にメモリへ先読みキャッシュ。ノーツカット時のカクつき（フレームドロップ）が一切ありません。
4. **好きな画像を追加可能**
   - お好みの透過PNG（256×256推奨）を `UserData/Beat115Icon/` に入れるだけで、自動的に抽選プールに追加されます。

---

## 🔧 前提MOD
- [BSIPA](https://github.com/beat-saber-modding-group/BeatSaber-IPA-Reloaded) (4.3.0+)
- ※NalulunaCutScoreは任意（無くてもバニラ単体で動作します）

---

## 📦 インストール方法
1. 最新リリースの `Beat115Icon_v1.0.0.zip` をダウンロードします。
2. 解凍した `Plugins` および `UserData` フォルダを、Beat Saberのインストール先フォルダへ上書きコピーしてください。

---

## ⚙️ 設定（UserData/Beat115Icon.json）
- `Enabled`: MODの有効/無効 (既定: `true`)
- `TargetScore`: 対象スコア (既定: `115`)
- `VanillaIconScale`: バニラ表示時のアイコンサイズ (既定: `0.70`)
- `IconScale`: NalulunaCutScore表示時のアイコンサイズ (既定: `0.65`)
- `HideNumberOnTargetScore`: 115の数字を消すかどうか (既定: `true`)
- `OverrideUnderlineColorOn115`: 115点達成時の下線色を変更するか (既定: `true`)
