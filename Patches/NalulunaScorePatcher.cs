using System;
using System.IO;
using System.Linq;
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
    internal class Beat115NalulunaIconComponent : MonoBehaviour
    {
        public ImageView IconImage;
        public TextMeshPro NumberText;
        public ImageView UnderlineImage;
        public bool IsShowingIcon;
        public bool HasPickedSpriteForThisCut;
    }

    internal static class NalulunaScorePatcher
    {
        private static bool _isPatched;
        private static FieldInfo _textField;
        private static PropertyInfo _uiNoGlowMaterialProp;
        private static Material _cachedNoGlowMaterial;

        public static void ApplyPatch(Harmony harmony)
        {
            if (_isPatched)
            {
                return;
            }

            try
            {
                Assembly ncsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "NalulunaCutScore", StringComparison.OrdinalIgnoreCase));

                if (ncsAssembly == null)
                {
                    Plugin.Log?.Info("NalulunaCutScore not detected. Running in Vanilla mode only.");
                    return;
                }

                Type effectType = ncsAssembly.GetTypes().FirstOrDefault(t =>
                    typeof(FlyingObjectEffect).IsAssignableFrom(t) &&
                    typeof(ICutScoreBufferDidChangeReceiver).IsAssignableFrom(t));

                if (effectType == null)
                {
                    Plugin.Log?.Warn("Could not locate CutScore FlyingObjectEffect in NalulunaCutScore.");
                    return;
                }

                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

                _textField = effectType.GetFields(flags)
                    .FirstOrDefault(f => f.FieldType == typeof(TextMeshPro));

                MethodInfo initMethod = effectType.GetMethods(flags)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] ps = m.GetParameters();
                        return ps.Length == 3 && ps[0].ParameterType == typeof(IReadonlyCutScoreBuffer);
                    });

                MethodInfo updateScoreMethod = effectType.GetMethods(flags)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] ps = m.GetParameters();
                        return ps.Length == 1 && ps[0].ParameterType == typeof(IReadonlyCutScoreBuffer);
                    });

                MethodInfo manualUpdateMethod = effectType.GetMethods(flags)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] ps = m.GetParameters();
                        return m.IsVirtual && ps.Length == 1 && ps[0].ParameterType == typeof(float);
                    });

                if (updateScoreMethod == null || manualUpdateMethod == null)
                {
                    Plugin.Log?.Error("Could not locate target methods in NalulunaCutScore.");
                    return;
                }

                Assembly utilsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "NalulunaUtils", StringComparison.OrdinalIgnoreCase));
                Type bsType = utilsAssembly?.GetType("NalulunaUtils.BeatSaber");
                _uiNoGlowMaterialProp = bsType?.GetProperty("uiNoGlowMaterial", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase);

                if (initMethod != null)
                {
                    MethodInfo initPrefix = typeof(NalulunaScorePatcher).GetMethod(nameof(InitPrefix), BindingFlags.NonPublic | BindingFlags.Static);
                    if (initPrefix != null)
                    {
                        harmony.Patch(initMethod, prefix: new HarmonyMethod(initPrefix));
                    }
                }
                else
                {
                    Plugin.Log?.Warn("Could not locate Init method in NalulunaCutScore; proceeding with UpdateScore/ManualUpdate patches.");
                }

                MethodInfo updateScorePostfix = typeof(NalulunaScorePatcher).GetMethod(nameof(UpdateScorePostfix), BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo manualUpdatePostfix = typeof(NalulunaScorePatcher).GetMethod(nameof(ManualUpdatePostfix), BindingFlags.NonPublic | BindingFlags.Static);

                harmony.Patch(updateScoreMethod, postfix: new HarmonyMethod(updateScorePostfix));
                harmony.Patch(manualUpdateMethod, postfix: new HarmonyMethod(manualUpdatePostfix));

                _isPatched = true;
                Plugin.Log?.Info($"NalulunaCutScore compatibility patch successfully applied! ({effectType.Name})");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"NalulunaCutScore patch could not be applied: {ex.Message}");
            }
        }

        private static void InitPrefix(FlyingObjectEffect __instance)
        {
            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled || __instance == null) return;

            Beat115NalulunaIconComponent comp = GetOrCreateComponent(__instance);
            comp.HasPickedSpriteForThisCut = false;
            comp.IsShowingIcon = false;

            if (comp.NumberText != null)
            {
                comp.NumberText.enabled = true;
                if (comp.NumberText.renderer != null)
                {
                    comp.NumberText.renderer.enabled = true;
                }
            }
            if (comp.IconImage != null)
            {
                comp.IconImage.gameObject.SetActive(false);
            }
        }

        private static void UpdateScorePostfix(FlyingObjectEffect __instance, IReadonlyCutScoreBuffer __0)
        {
            if (__instance == null) return;

            var config = PluginConfig.Instance;
            Beat115NalulunaIconComponent comp = GetOrCreateComponent(__instance);
            if (comp == null) return;

            if (config == null || !config.Enabled || __0 == null)
            {
                comp.IsShowingIcon = false;
                if (comp.NumberText != null)
                {
                    comp.NumberText.enabled = true;
                    if (comp.NumberText.renderer != null)
                    {
                        comp.NumberText.renderer.enabled = true;
                    }
                }
                if (comp.IconImage != null)
                {
                    comp.IconImage.gameObject.SetActive(false);
                }
                return;
            }

            int totalScore = __0.beforeCutScore + __0.afterCutScore + __0.centerDistanceCutScore;
            bool isTargetScore = __0.cutScore >= config.TargetScore || totalScore >= config.TargetScore;

            if (isTargetScore)
            {
                comp.IsShowingIcon = true;

                if (comp.NumberText != null)
                {
                    if (config.HideNumberOnTargetScore)
                    {
                        comp.NumberText.SetText(string.Empty);
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
                    Material mat = GetNoGlowMaterial();
                    if (mat != null && comp.IconImage.material != mat)
                    {
                        comp.IconImage.material = mat;
                    }

                    if (!comp.HasPickedSpriteForThisCut || comp.IconImage.sprite == null)
                    {
                        Sprite pickedSprite = IconCacheService.GetRandomSprite();
                        if (pickedSprite != null)
                        {
                            comp.IconImage.sprite = pickedSprite;
                        }
                        comp.HasPickedSpriteForThisCut = true;
                    }

                    comp.IconImage.gameObject.layer = __instance.gameObject.layer;
                    comp.IconImage.transform.localPosition = new Vector3(0f, config.OffsetY, -0.02f);
                    comp.IconImage.transform.localScale = Vector3.one * config.IconScale;

                    float alpha = comp.NumberText != null ? comp.NumberText.color.a : 1f;
                    comp.IconImage.color = config.GetTintColor(alpha);
                    comp.IconImage.gameObject.SetActive(true);
                }
            }
            else
            {
                comp.IsShowingIcon = false;
                if (comp.NumberText != null)
                {
                    comp.NumberText.enabled = true;
                    if (comp.NumberText.renderer != null)
                    {
                        comp.NumberText.renderer.enabled = true;
                    }
                }
                if (comp.IconImage != null)
                {
                    comp.IconImage.gameObject.SetActive(false);
                }
            }
        }

        private static void ManualUpdatePostfix(FlyingObjectEffect __instance)
        {
            if (__instance == null) return;

            Beat115NalulunaIconComponent comp = __instance.GetComponent<Beat115NalulunaIconComponent>();
            if (comp == null || !comp.IsShowingIcon || comp.IconImage == null) return;

            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled) return;

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

            float alpha = comp.NumberText != null ? comp.NumberText.color.a : 1f;
            comp.IconImage.color = config.GetTintColor(alpha);

            if (comp.IconImage.gameObject.layer != __instance.gameObject.layer)
            {
                comp.IconImage.gameObject.layer = __instance.gameObject.layer;
            }

            if (config.OverrideUnderlineColorOn115 && comp.UnderlineImage != null && comp.UnderlineImage.gameObject.activeSelf)
            {
                comp.UnderlineImage.color = config.GetUnderlineColor(Mathf.Sqrt(Mathf.Clamp01(alpha)));
            }
        }

        private static Beat115NalulunaIconComponent GetOrCreateComponent(FlyingObjectEffect effect)
        {
            Beat115NalulunaIconComponent comp = effect.GetComponent<Beat115NalulunaIconComponent>();
            if (comp != null && comp.IconImage != null) return comp;

            if (comp == null) comp = effect.gameObject.AddComponent<Beat115NalulunaIconComponent>();

            comp.NumberText = (_textField?.GetValue(effect) as TextMeshPro)
                              ?? effect.transform.Find("Number")?.GetComponent<TextMeshPro>();

            comp.UnderlineImage = effect.transform.Find("CenterDistanceMaxScoreIndicator")?.GetComponent<ImageView>();

            Transform existing = effect.transform.Find("Beat115NalulunaIcon");
            GameObject iconObj = existing != null ? existing.gameObject : new GameObject("Beat115NalulunaIcon");

            iconObj.layer = effect.gameObject.layer;
            iconObj.transform.SetParent(effect.transform, worldPositionStays: false);

            ImageView imageView = iconObj.GetComponent<ImageView>() ?? iconObj.AddComponent<ImageView>();
            imageView.rectTransform.sizeDelta = Vector2.one;
            imageView.type = Image.Type.Simple;
            imageView.sprite = IconCacheService.GetRandomSprite();

            Material mat = GetNoGlowMaterial();
            if (mat != null) imageView.material = mat;

            var config = PluginConfig.Instance;
            float scale = config != null ? config.IconScale : 0.65f;
            float offsetY = config != null ? config.OffsetY : 0.02f;

            iconObj.transform.localPosition = new Vector3(0f, offsetY, -0.02f);
            iconObj.transform.localScale = Vector3.one * scale;
            iconObj.SetActive(false);

            comp.IconImage = imageView;
            return comp;
        }

        internal static Material GetNoGlowMaterial()
        {
            if (_cachedNoGlowMaterial != null) return _cachedNoGlowMaterial;

            if (_uiNoGlowMaterialProp != null)
            {
                _cachedNoGlowMaterial = _uiNoGlowMaterialProp.GetValue(null) as Material;
                if (_cachedNoGlowMaterial != null) return _cachedNoGlowMaterial;
            }

            _cachedNoGlowMaterial = Resources.FindObjectsOfTypeAll<Material>()
                .FirstOrDefault(m => m != null && m.name == "UINoGlow");

            return _cachedNoGlowMaterial;
        }
    }
}
