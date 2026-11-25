# Beat115Icon

Beat Saber（`1.40.8` / `1.44.1`）向け 115点カスタムアイコン表示MODです。  
パーフェクトカット（115点）を達成した際に、「115」という数字の代わりにカスタム **PNG / GIFアニメーション** アイコンをランダム表示します。  
**バニラ（Beat Saber標準）および NalulunaCutScore の両方に完全対応**しています。

A custom score 115 icon replacement mod for Beat Saber (`1.40.8` / `1.44.1`).  
Replaces the standard "115" score text with custom **PNG or animated GIF** icons upon scoring a perfect cut (115).  
**Fully compatible with both Vanilla (standard Beat Saber) and NalulunaCutScore.**

![Beat115Icon Demo](docs/demo.gif)

▶ [音声付き動画 / Full Video with Audio (MP4)](docs/demo.mp4)

---

> [!WARNING]
> ### 免責事項 / Disclaimer
> 
> 本MODは wacky が個人的に楽しむために作成・カスタマイズしているものです。  
> そのため、他のPC環境や将来のBeat Saberバージョンでの動作は保証できません。  
> 導入およびご利用はすべて自己責任にてお願いいたします。
> 
> This mod is created and maintained by wacky purely for personal enjoyment.  
> As such, proper operation in other environments or future Beat Saber versions is not guaranteed.  
> Please use this mod entirely at your own risk.

---

## 🌺 主な特徴 / Features

1. **バニラ＆NalulunaCutScore ハイブリッド両対応 / Vanilla & NalulunaCutScore Dual Support**
   - **バニラ環境**: NalulunaCutScoreが導入されていなくても、Beat Saber標準のスコアポップアップ（`FlyingScoreEffect`）で115点が出た瞬間にアイコンを表示。フェード曲線にも完全追従。
   - **NalulunaCutScore併用時**: NalulunaCutScoreのスコア表示も自動検出し、同様にアイコン化。
2. **数字を消してクリアな表示 / Clean Display without Overlapping Numbers**
   - 115点達成時、数字テキストを完全に非表示にし、アイコンのみを綺麗に描画します。
3. **ゼロ遅延のランダム画像抽選（PNG / GIFアニメ対応） / Zero-lag Random Sprite Selection (PNG & Animated GIF)**
   - `UserData/Beat115Icon/` フォルダ内のすべての **PNG / GIFアニメーション** 画像をゲーム起動時にメモリへ先読みキャッシュ。ノーツカット時のカクつき（フレームドロップ）が一切ありません。
4. **好きな画像・GIFを追加可能 / Customizable PNG & GIF Images**
   - お好みの透過PNG（256×256推奨）や透過GIFアニメを `UserData/Beat115Icon/` に入れるだけで、自動的に抽選プールに追加されます。
   - サンプルとしてハイビスカスアイコン3種（赤・ピンク・黄）を同梱しています。
5. **ゲーム内設定UI（v1.1.0で共通設定に統合） / In-Game Settings UI (Unified Settings)**
   - **MOD SETTINGS > Beat115Icon** および **選曲画面左パネル（GameplaySetup）> Beat115Icon** の両方から、NalulunaCutScore / バニラ共通の1セット設定として、有効化・アイコンサイズ・上下オフセット・115表示時間（`0.00〜3.00秒`）・数字非表示・下線色変更をリアルタイムに調整・再読込・リセットできます。

---

## 🔧 前提MOD / Prerequisites
- [BSIPA](https://github.com/beat-saber-modding-group/BeatSaber-IPA-Reloaded) (4.3.0+)
- [BeatSaberMarkupLanguage (BSML)](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) (1.12.0+)
- ※NalulunaCutScoreは任意（無くてもバニラ単体で動作します / Optional）

---

## 📦 インストール方法 / Installation
1. [Releases](../../releases) からお使いの Beat Saber バージョン（`bs1.40.8` または `bs1.44.1`）向けの ZIP ファイルをダウンロードし、Beat Saber のインストールフォルダにそのまま解凍してください（`Plugins/Beat115Icon.dll` とサンプル画像の入った `UserData/Beat115Icon/` が配置されます）。
2. お好みの透過PNG / GIF画像を `UserData/Beat115Icon/` フォルダへ追加・入れ替えしてお楽しみください。
