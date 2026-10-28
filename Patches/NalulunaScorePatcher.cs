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

                Type matSourceType = ncsAssembly.GetTypes().FirstOrDefault(t => t.GetProperty("UINoGlowMaterial", BindingFlags.Public | BindingFlags.Static) != null);
                if (matSourceType != null)
                {
                    _uiNoGlowMaterialProp = matSourceType.GetProperty("UINoGlowMaterial", BindingFlags.Public | BindingFlags.Static);
                }

                MethodInfo initMethod = effectType.GetMethods(flags)
                    .FirstOrDefault(m => m.Name.Contains("InitAndPresent") && m.GetParameters().Length >= 2);

                MethodInfo didChangeMethod = effectType.GetMethod("HandleCutScoreBufferDidChange", BindingFlags.Public | BindingFlags.Instance);
                MethodInfo didFinishMethod = effectType.GetMethod("HandleCutScoreBufferDidFinish", BindingFlags.Public | BindingFlags.Instance);
                MethodInfo manualUpdateMethod = effectType.GetMethod("ManualUpdate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                MethodInfo initPostfix = typeof(NalulunaScorePatcher).GetMethod(nameof(InitPostfix), BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo didChangePostfix = typeof(NalulunaScorePatcher).GetMethod(nameof(DidChangePostfix), BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo didFinishPostfix = typeof(NalulunaScorePatcher).GetMethod(nameof(DidFinishPostfix), BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo manualUpdatePostfix = typeof(NalulunaScorePatcher).GetMethod(nameof(ManualUpdatePostfix), BindingFlags.NonPublic | BindingFlags.Static);

                if (initMethod != null && initPostfix != null)
                {
                    harmony.Patch(initMethod, postfix: new HarmonyMethod(initPostfix));
                }
                if (didChangeMethod != null && didChangePostfix != null)
                {
                    harmony.Patch(didChangeMethod, postfix: new HarmonyMethod(didChangePostfix));
                }
                if (didFinishMethod != null && didFinishPostfix != null)
                {
                    harmony.Patch(didFinishMethod, postfix: new HarmonyMethod(didFinishPostfix));
                }
                if (manualUpdateMethod != null && manualUpdatePostfix != null)
                {
                    harmony.Patch(manualUpdateMethod, postfix: new HarmonyMethod(manualUpdatePostfix));
                }

                _isPatched = true;
                Plugin.Log?.Info("NalulunaCutScore compatibility patch successfully applied!");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"NalulunaCutScore patch could not be applied: {ex.Message}");
            }
        }

        private static void InitPostfix(FlyingObjectEffect __instance)
        {
            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled) return;

            Beat115NalulunaIconComponent comp = GetOrCreateComponent(__instance);
            comp.IsShowingIcon = false;
            if (comp.IconImage != null)
            {
                comp.IconImage.sprite = IconCacheService.GetRandomSprite();
                comp.IconImage.gameObject.SetActive(false);
            }
            if (comp.NumberText != null)
            {
                comp.NumberText.enabled = true;
            }
        }

        private static void DidChangePostfix(FlyingObjectEffect __instance, CutScoreBuffer cutScoreBuffer)
        {
            CheckScore(__instance, cutScoreBuffer);
        }

        private static void DidFinishPostfix(FlyingObjectEffect __instance, CutScoreBuffer cutScoreBuffer)
        {
            CheckScore(__instance, cutScoreBuffer);
        }

        private static void ManualUpdatePostfix(FlyingObjectEffect __instance, float t)
        {
            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled) return;

            Beat115NalulunaIconComponent comp = __instance.GetComponent<Beat115NalulunaIconComponent>();
            if (comp == null || !comp.IsShowingIcon || comp.IconImage == null) return;

            if (config.HideNumberOnTargetScore && comp.NumberText != null)
            {
                comp.NumberText.enabled = false;
                MeshRenderer mr = comp.NumberText.GetComponent<MeshRenderer>();
                if (mr != null && mr.enabled) mr.enabled = false;
            }

            float alpha = Mathf.Clamp01(1f - t);
            comp.IconImage.color = config.GetTintColor(alpha);

            if (comp.IconImage.gameObject.layer != __instance.gameObject.layer)
            {
                comp.IconImage.gameObject.layer = __instance.gameObject.layer;
            }

            if (config.OverrideUnderlineColorOn115 && comp.UnderlineImage != null && comp.UnderlineImage.gameObject.activeSelf)
            {
                comp.UnderlineImage.color = config.GetUnderlineColor(Mathf.Sqrt(alpha));
            }
        }

        private static void CheckScore(FlyingObjectEffect effect, IReadonlyCutScoreBuffer cutScoreBuffer)
        {
            var config = PluginConfig.Instance;
            if (config == null || !config.Enabled || effect == null) return;

            Beat115NalulunaIconComponent comp = GetOrCreateComponent(effect);

            if (cutScoreBuffer != null && cutScoreBuffer.cutScore == config.TargetScore)
            {
                comp.IsShowingIcon = true;
                if (comp.IconImage != null)
                {
                    if (comp.IconImage.sprite == null) comp.IconImage.sprite = IconCacheService.GetRandomSprite();
                    comp.IconImage.gameObject.SetActive(true);
                }
                if (config.HideNumberOnTargetScore && comp.NumberText != null)
                {
                    comp.NumberText.enabled = false;
                    MeshRenderer mr = comp.NumberText.GetComponent<MeshRenderer>();
                    if (mr != null && mr.enabled) mr.enabled = false;
                }
            }
            else if (comp.IsShowingIcon)
            {
                comp.IsShowingIcon = false;
                if (comp.IconImage != null) comp.IconImage.gameObject.SetActive(false);
                if (comp.NumberText != null) comp.NumberText.enabled = true;
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

            Material mat = GetNoGlowMaterial(effect);
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

        private static Material GetNoGlowMaterial(FlyingObjectEffect effect)
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
