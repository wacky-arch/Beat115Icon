using System;
using System.Reflection;
using Beat115Icon.Configuration;
using Beat115Icon.Services;
using HarmonyLib;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beat115Icon.Patches
{
    internal class Beat115VanillaIconComponent : MonoBehaviour
    {
        public ImageView IconImage;
        public GameObject CanvasObject;
        public TextMeshPro NumberText;
        public SpriteRenderer UnderlineRenderer;
        public AnimationCurve FadeCurve;
        public FlyingScoreEffect EffectInstance;
        public bool IsTargetScore;
        public bool HasPickedSpriteForThisCut;

        public void ResetState()
        {
            IsTargetScore = false;
            HasPickedSpriteForThisCut = false;
            if (IconImage != null)
            {
                IconImage.gameObject.SetActive(false);
            }
            if (NumberText != null)
            {
                NumberText.enabled = true;
                if (NumberText.renderer != null)
                {
                    NumberText.renderer.enabled = true;
                }
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
        [HarmonyPrefix]
        private static void InitAndPresentPrefix(FlyingScoreEffect __instance)
        {
            if (__instance == null)
            {
                return;
            }

            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled)
            {
                __instance.GetComponent<Beat115VanillaIconComponent>()?.ResetState();
                return;
            }

            Beat115VanillaIconComponent comp = GetOrCreateComponent(__instance);
            comp.ResetState();
        }

        [HarmonyPatch("RefreshScore", new Type[] { typeof(int), typeof(int) })]
        [HarmonyPostfix]
        private static void RefreshScorePostfix(FlyingScoreEffect __instance, int score)
        {
            if (__instance == null)
            {
                return;
            }

            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled)
            {
                __instance.GetComponent<Beat115VanillaIconComponent>()?.ResetState();
                return;
            }

            Beat115VanillaIconComponent comp = GetOrCreateComponent(__instance);
            bool isTargetScore = score >= config.TargetScore;

            if (isTargetScore)
            {
                comp.IsTargetScore = true;

                if (comp.NumberText != null)
                {
                    if (config.HideNumberOnTargetScore)
                    {
                        comp.NumberText.text = string.Empty;
                        comp.NumberText.enabled = false;
                        if (comp.NumberText.renderer != null)
                        {
                            comp.NumberText.renderer.enabled = false;
                        }
                    }
                    else
                    {
                        comp.NumberText.enabled = true;
                        if (comp.NumberText.renderer != null)
                        {
                            comp.NumberText.renderer.enabled = true;
                        }
                    }
                }

                if (comp.IconImage != null)
                {
                    Material mat = NalulunaScorePatcher.GetNoGlowMaterial();
                    if (mat != null && comp.IconImage.material != mat)
                    {
                        comp.IconImage.material = mat;
                    }

                    if (!comp.HasPickedSpriteForThisCut || comp.IconImage.sprite == null)
                    {
                        Sprite picked = IconCacheService.GetRandomSprite();
                        if (picked != null)
                        {
                            comp.IconImage.sprite = picked;
                        }
                        comp.HasPickedSpriteForThisCut = true;
                    }

                    if (comp.CanvasObject != null && comp.CanvasObject.layer != __instance.gameObject.layer)
                    {
                        comp.CanvasObject.layer = __instance.gameObject.layer;
                    }
                    comp.IconImage.gameObject.layer = __instance.gameObject.layer;
                    comp.IconImage.transform.localPosition = new Vector3(0f, config.VanillaOffsetY, -0.02f);
                    comp.IconImage.transform.localScale = Vector3.one * config.VanillaIconScale;

                    // 有効化直前に現在アルファを適用して初フレームの白フラッシュを防ぐ
                    float alpha = comp.NumberText != null ? Mathf.Clamp01(comp.NumberText.color.a) : 0f;
                    comp.IconImage.color = config.GetTintColor(alpha);
                    comp.IconImage.gameObject.SetActive(true);
                }
            }
            else
            {
                comp.IsTargetScore = false;
                if (comp.IconImage != null)
                {
                    comp.IconImage.gameObject.SetActive(false);
                }
                if (comp.NumberText != null)
                {
                    comp.NumberText.enabled = true;
                    if (comp.NumberText.renderer != null)
                    {
                        comp.NumberText.renderer.enabled = true;
                    }
                }
            }
        }

        [HarmonyPatch("ManualUpdate", new Type[] { typeof(float) })]
        [HarmonyPostfix]
        private static void ManualUpdatePostfix(FlyingScoreEffect __instance, float t)
        {
            if (__instance == null)
            {
                return;
            }

            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled)
            {
                return;
            }

            Beat115VanillaIconComponent comp = __instance.GetComponent<Beat115VanillaIconComponent>();
            if (comp == null || !comp.IsTargetScore || comp.IconImage == null || !comp.IconImage.gameObject.activeSelf)
            {
                return;
            }

            // 数字の再表示を抑止
            if (config.HideNumberOnTargetScore && comp.NumberText != null)
            {
                if (comp.NumberText.enabled)
                {
                    comp.NumberText.enabled = false;
                }
                if (comp.NumberText.renderer != null && comp.NumberText.renderer.enabled)
                {
                    comp.NumberText.renderer.enabled = false;
                }
            }

            // フェード透明度のアニメーション同期
            float finalAlpha;
            if (comp.NumberText != null)
            {
                finalAlpha = Mathf.Clamp01(comp.NumberText.color.a);
            }
            else
            {
                AnimationCurve curve = comp.FadeCurve;
                float alphaProgress = curve != null ? curve.Evaluate(t) : (1.0f - t);
                Color baseColor = _colorField != null ? (Color)_colorField.GetValue(__instance) : Color.white;
                float aMult = _colorAMultiplierField != null ? (float)_colorAMultiplierField.GetValue(__instance) : 1.0f;
                finalAlpha = Mathf.Clamp01(alphaProgress * baseColor.a * aMult);
            }

            comp.IconImage.color = config.GetTintColor(finalAlpha);

            if (comp.CanvasObject != null && comp.CanvasObject.layer != __instance.gameObject.layer)
            {
                comp.CanvasObject.layer = __instance.gameObject.layer;
            }
            if (comp.IconImage.gameObject.layer != __instance.gameObject.layer)
            {
                comp.IconImage.gameObject.layer = __instance.gameObject.layer;
            }

            // 下線色のオーバーライド
            if (config.OverrideUnderlineColorOn115 && comp.UnderlineRenderer != null && comp.UnderlineRenderer.enabled)
            {
                comp.UnderlineRenderer.color = config.GetUnderlineColor(finalAlpha);
            }
        }

        private static Beat115VanillaIconComponent GetOrCreateComponent(FlyingScoreEffect effect)
        {
            Beat115VanillaIconComponent comp = effect.GetComponent<Beat115VanillaIconComponent>();
            if (comp != null && comp.IconImage != null && comp.CanvasObject != null)
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

            Transform canvasChild = effect.transform.Find("Beat115VanillaCanvas");
            GameObject canvasObj;
            if (canvasChild != null)
            {
                canvasObj = canvasChild.gameObject;
            }
            else
            {
                canvasObj = new GameObject("Beat115VanillaCanvas");
                canvasObj.layer = effect.gameObject.layer;
                canvasObj.transform.SetParent(effect.transform, worldPositionStays: false);
            }

            Canvas canvas = canvasObj.GetComponent<Canvas>() ?? canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = Vector2.zero;
            }

            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = Vector3.one * 0.4f;

            Transform iconChild = canvasObj.transform.Find("Beat115VanillaIcon");
            GameObject iconObj;
            if (iconChild != null)
            {
                iconObj = iconChild.gameObject;
            }
            else
            {
                iconObj = new GameObject("Beat115VanillaIcon");
                iconObj.layer = effect.gameObject.layer;
                iconObj.transform.SetParent(canvasObj.transform, worldPositionStays: false);
            }

            ImageView imageView = iconObj.GetComponent<ImageView>() ?? iconObj.AddComponent<ImageView>();
            imageView.rectTransform.sizeDelta = Vector2.one;
            imageView.type = Image.Type.Simple;
            imageView.sprite = IconCacheService.GetRandomSprite();

            Material mat = NalulunaScorePatcher.GetNoGlowMaterial();
            if (mat != null)
            {
                imageView.material = mat;
            }

            var config = PluginConfig.Instance;
            float scale = config != null ? config.VanillaIconScale : 0.70f;
            float offsetY = config != null ? config.VanillaOffsetY : 0.0f;

            iconObj.transform.localPosition = new Vector3(0f, offsetY, -0.02f);
            iconObj.transform.localScale = Vector3.one * scale;
            iconObj.SetActive(false);

            comp.CanvasObject = canvasObj;
            comp.IconImage = imageView;

            return comp;
        }
    }
}
