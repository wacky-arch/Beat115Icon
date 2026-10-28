using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace Beat115Icon.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        public virtual bool Enabled { get; set; } = true;

        public virtual int TargetScore { get; set; } = 115;

        // NalulunaCutScore用 スケールとオフセット
        public virtual float IconScale { get; set; } = 0.65f;
        public virtual float OffsetY { get; set; } = 0.02f;

        // バニラ FlyingScoreEffect用 スケールとオフセット
        public virtual float VanillaIconScale { get; set; } = 0.70f;
        public virtual float VanillaOffsetY { get; set; } = 0.0f;

        // 115の数字を完全に隠すかどうか
        public virtual bool HideNumberOnTargetScore { get; set; } = true;

        // アイコンの色調補正 (1.0, 1.0, 1.0 で無補正の原色)
        public virtual float TintR { get; set; } = 1.0f;
        public virtual float TintG { get; set; } = 1.0f;
        public virtual float TintB { get; set; } = 1.0f;

        // 115点達成時の下線色（バニラ/Naluluna両方）
        public virtual bool OverrideUnderlineColorOn115 { get; set; } = true;
        public virtual float UnderlineRedR { get; set; } = 1.0f;
        public virtual float UnderlineRedG { get; set; } = 0.20f;
        public virtual float UnderlineRedB { get; set; } = 0.30f;

        public virtual string CustomDirectoryPath { get; set; } = "";

        public Color GetTintColor(float alpha) => new Color(TintR, TintG, TintB, alpha);

        public Color GetUnderlineColor(float alpha) => new Color(UnderlineRedR, UnderlineRedG, UnderlineRedB, alpha);
    }
}
