using System;
using System.Reflection;
using Beat115Icon.Configuration;
using Beat115Icon.Services;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Beat115Icon.Patches
{
    internal class Beat115VanillaIconComponent : MonoBehaviour
    {
        public SpriteRenderer IconRenderer;
        public TextMeshPro NumberText;
        public SpriteRenderer UnderlineRenderer;
        public AnimationCurve FadeCurve;
        public FlyingScoreEffect EffectInstance;
        public bool IsTargetScore;

        public void ResetState()
        {
            IsTargetScore = false;
            if (IconRenderer != null)
            {
                IconRenderer.enabled = false;
            }
            if (NumberText != null)
            {
                NumberText.enabled = true;
            }
        }
    }

    [HarmonyPatch(typeof(FlyingScoreEffect))]
    internal static class VanillaScorePatcher
    {
        private static readonly FieldInfo _textField = typeof(FlyingScoreEffect).GetField("_text", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _underlineField = typeof(FlyingScoreEffect).GetField("_maxCutDistanceScoreIndicator", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _fadeCurveField = typeof(FlyingScoreEffect).GetField("_fadeAnimationCurve", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _colorField = typeof(FlyingScoreEffect).GetField("_color", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _colorAMultiplierField = typeof(FlyingScoreEffect).GetField("_colorAMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(nameof(FlyingScoreEffect.InitAndPresent), new Type[] { typeof(IReadonlyCutScoreBuffer), typeof(float), typeof(Vector3), typeof(Color) })]
        [HarmonyPostfix]
        private static void InitAndPresentPostfix(FlyingScoreEffect __instance, IReadonlyCutScoreBuffer cutScoreBuffer, Color color)
        {
            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled)
            {
                return;
            }

            Beat115VanillaIconComponent comp = GetOrCreateComponent(__instance);
            comp.ResetState();

            if (comp.IconRenderer != null)
            {
                comp.IconRenderer.sprite = IconCacheService.GetRandomSprite();
            }

            if (cutScoreBuffer != null && cutScoreBuffer.cutScore == config.TargetScore)
            {
                ApplyTargetScoreDisplay(comp, __instance);
            }
        }

        [HarmonyPatch(nameof(FlyingScoreEffect.HandleCutScoreBufferDidChange))]
        [HarmonyPostfix]
        private static void HandleDidChangePostfix(FlyingScoreEffect __instance, CutScoreBuffer cutScoreBuffer)
        {
            CheckScore(__instance, cutScoreBuffer);
        }

        [HarmonyPatch(nameof(FlyingScoreEffect.HandleCutScoreBufferDidFinish))]
        [HarmonyPostfix]
        private static void HandleDidFinishPostfix(FlyingScoreEffect __instance, CutScoreBuffer cutScoreBuffer)
        {
            CheckScore(__instance, cutScoreBuffer);
        }

        [HarmonyPatch("ManualUpdate")]
        [HarmonyPostfix]
        private static void ManualUpdatePostfix(FlyingScoreEffect __instance, float t)
        {
            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled)
            {
                return;
            }

            Beat115VanillaIconComponent comp = __instance.GetComponent<Beat115VanillaIconComponent>();
            if (comp == null || !comp.IsTargetScore || comp.IconRenderer == null || !comp.IconRenderer.enabled)
            {
                return;
            }

            // 数字の再表示を抑止
            if (config.HideNumberOnTargetScore && comp.NumberText != null && comp.NumberText.enabled)
            {
                comp.NumberText.enabled = false;
            }

            // フェード透明度のアニメーション同期
            AnimationCurve curve = comp.FadeCurve;
            float alphaProgress = curve != null ? curve.Evaluate(t) : (1.0f - t);

            Color baseColor = _colorField != null ? (Color)_colorField.GetValue(__instance) : Color.white;
            float aMult = _colorAMultiplierField != null ? (float)_colorAMultiplierField.GetValue(__instance) : 1.0f;

            float finalAlpha = Mathf.Clamp01(alphaProgress * baseColor.a * aMult);
            comp.IconRenderer.color = config.GetTintColor(finalAlpha);

            // 下線色のオーバーライド
            if (config.OverrideUnderlineColorOn115 && comp.UnderlineRenderer != null && comp.UnderlineRenderer.enabled)
            {
                comp.UnderlineRenderer.color = config.GetUnderlineColor(finalAlpha);
            }
        }

        private static void CheckScore(FlyingScoreEffect effect, IReadonlyCutScoreBuffer cutScoreBuffer)
        {
            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled || effect == null)
            {
                return;
            }

            Beat115VanillaIconComponent comp = GetOrCreateComponent(effect);

            if (cutScoreBuffer != null && cutScoreBuffer.cutScore == config.TargetScore)
            {
                ApplyTargetScoreDisplay(comp, effect);
            }
            else if (comp.IsTargetScore)
            {
                // スコア確定等で115点未満になった場合は元に戻す
                comp.IsTargetScore = false;
                if (comp.IconRenderer != null)
                {
                    comp.IconRenderer.enabled = false;
                }
                if (comp.NumberText != null)
                {
                    comp.NumberText.enabled = true;
                }
            }
        }

        private static void ApplyTargetScoreDisplay(Beat115VanillaIconComponent comp, FlyingScoreEffect effect)
        {
            var config = PluginConfig.Instance;
            comp.IsTargetScore = true;

            if (comp.IconRenderer != null)
            {
                if (comp.IconRenderer.sprite == null)
                {
                    comp.IconRenderer.sprite = IconCacheService.GetRandomSprite();
                }
                comp.IconRenderer.enabled = true;
            }

            if (config.HideNumberOnTargetScore && comp.NumberText != null)
            {
                comp.NumberText.enabled = false;
            }
        }

        private static Beat115VanillaIconComponent GetOrCreateComponent(FlyingScoreEffect effect)
        {
            Beat115VanillaIconComponent comp = effect.GetComponent<Beat115VanillaIconComponent>();
            if (comp != null && comp.IconRenderer != null)
            {
                return comp;
            }

            if (comp == null)
            {
                comp = effect.gameObject.AddComponent<Beat115VanillaIconComponent>();
            }

            comp.EffectInstance = effect;
            comp.NumberText = _textField?.GetValue(effect) as TextMeshPro;
            comp.UnderlineRenderer = _underlineField?.GetValue(effect) as SpriteRenderer;
            comp.FadeCurve = _fadeCurveField?.GetValue(effect) as AnimationCurve;

            Transform child = effect.transform.Find("Beat115IconSprite");
            GameObject iconObj;
            if (child != null)
            {
                iconObj = child.gameObject;
            }
            else
            {
                iconObj = new GameObject("Beat115IconSprite");
                iconObj.layer = effect.gameObject.layer;
                iconObj.transform.SetParent(effect.transform, worldPositionStays: false);
            }

            SpriteRenderer sr = iconObj.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = iconObj.AddComponent<SpriteRenderer>();
            }

            var config = PluginConfig.Instance;
            float scale = config != null ? config.VanillaIconScale : 0.70f;
            float offsetY = config != null ? config.VanillaOffsetY : 0.0f;

            iconObj.transform.localPosition = new Vector3(0f, offsetY, -0.01f);
            iconObj.transform.localScale = Vector3.one * scale;

            sr.enabled = false;
            comp.IconRenderer = sr;

            return comp;
        }
    }
}
