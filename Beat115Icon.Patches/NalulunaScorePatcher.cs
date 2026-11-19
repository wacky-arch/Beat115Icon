using System;
using System.Linq;
using System.Reflection;
using Beat115Icon.Configuration;
using Beat115Icon.Services;
using HMUI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beat115Icon.Patches;

internal static class NalulunaScorePatcher
{
	private static bool _isPatched;

	private static FieldInfo _textField;

	private static PropertyInfo _uiNoGlowMaterialProp;

	private static Material _cachedNoGlowMaterial;

	public static void ApplyPatch(Harmony harmony)
	{
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Expected O, but got Unknown
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Expected O, but got Unknown
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Expected O, but got Unknown
		if (_isPatched)
		{
			return;
		}
		try
		{
			Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => string.Equals(a.GetName().Name, "NalulunaCutScore", StringComparison.OrdinalIgnoreCase));
			if (assembly == null)
			{
				Plugin.Log?.Info("NalulunaCutScore not detected. Running in Vanilla mode only.");
				return;
			}
			Type type = assembly.GetTypes().FirstOrDefault((Type t) => typeof(FlyingObjectEffect).IsAssignableFrom(t) && typeof(ICutScoreBufferDidChangeReceiver).IsAssignableFrom(t));
			if (type == null)
			{
				Plugin.Log?.Warn("Could not locate CutScore FlyingObjectEffect in NalulunaCutScore.");
				return;
			}
			BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			_textField = type.GetFields(bindingAttr).FirstOrDefault((FieldInfo f) => f.FieldType == typeof(TextMeshPro));
			MethodInfo methodInfo = type.GetMethods(bindingAttr).FirstOrDefault(delegate(MethodInfo m)
			{
				ParameterInfo[] parameters3 = m.GetParameters();
				return parameters3.Length == 3 && parameters3[0].ParameterType == typeof(IReadonlyCutScoreBuffer);
			});
			MethodInfo methodInfo2 = type.GetMethods(bindingAttr).FirstOrDefault(delegate(MethodInfo m)
			{
				ParameterInfo[] parameters2 = m.GetParameters();
				return parameters2.Length == 1 && parameters2[0].ParameterType == typeof(IReadonlyCutScoreBuffer);
			});
			MethodInfo methodInfo3 = type.GetMethods(bindingAttr).FirstOrDefault(delegate(MethodInfo m)
			{
				ParameterInfo[] parameters = m.GetParameters();
				return m.IsVirtual && parameters.Length == 1 && parameters[0].ParameterType == typeof(float);
			});
			if (methodInfo2 == null || methodInfo3 == null)
			{
				Plugin.Log?.Error("Could not locate target methods in NalulunaCutScore.");
				return;
			}
			_uiNoGlowMaterialProp = (AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => string.Equals(a.GetName().Name, "NalulunaUtils", StringComparison.OrdinalIgnoreCase))?.GetType("NalulunaUtils.BeatSaber"))?.GetProperty("uiNoGlowMaterial", BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (methodInfo != null)
			{
				MethodInfo method = typeof(NalulunaScorePatcher).GetMethod("InitPrefix", BindingFlags.Static | BindingFlags.NonPublic);
				if (method != null)
				{
					harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				}
			}
			else
			{
				Plugin.Log?.Warn("Could not locate Init method in NalulunaCutScore; proceeding with UpdateScore/ManualUpdate patches.");
			}
			MethodInfo method2 = typeof(NalulunaScorePatcher).GetMethod("UpdateScorePostfix", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo method3 = typeof(NalulunaScorePatcher).GetMethod("ManualUpdatePostfix", BindingFlags.Static | BindingFlags.NonPublic);
			harmony.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			harmony.Patch((MethodBase)methodInfo3, (HarmonyMethod)null, new HarmonyMethod(method3), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			_isPatched = true;
			Plugin.Log?.Info("NalulunaCutScore compatibility patch successfully applied! (" + type.Name + ")");
		}
		catch (Exception ex)
		{
			Plugin.Log?.Warn("NalulunaCutScore patch could not be applied: " + ex.Message);
		}
	}

	private static void InitPrefix(FlyingObjectEffect __instance)
	{
		PluginConfig instance = PluginConfig.Instance;
		if (instance == null || !instance.Enabled || __instance == null)
		{
			return;
		}
		Beat115NalulunaIconComponent orCreateComponent = GetOrCreateComponent(__instance);
		orCreateComponent.HasPickedSpriteForThisCut = false;
		orCreateComponent.IsShowingIcon = false;
		if (orCreateComponent.NumberText != null)
		{
			orCreateComponent.NumberText.enabled = true;
			if (orCreateComponent.NumberText.renderer != null)
			{
				orCreateComponent.NumberText.renderer.enabled = true;
			}
		}
		if ((UnityEngine.Object)(object)orCreateComponent.IconImage != null)
		{
			((Component)(object)orCreateComponent.IconImage).gameObject.SetActive(value: false);
		}
	}

	private static void UpdateScorePostfix(FlyingObjectEffect __instance, IReadonlyCutScoreBuffer __0)
	{
		if (__instance == null)
		{
			return;
		}
		PluginConfig instance = PluginConfig.Instance;
		Beat115NalulunaIconComponent orCreateComponent = GetOrCreateComponent(__instance);
		if (orCreateComponent == null)
		{
			return;
		}
		if (instance == null || !instance.Enabled || __0 == null)
		{
			orCreateComponent.IsShowingIcon = false;
			if (orCreateComponent.NumberText != null)
			{
				orCreateComponent.NumberText.enabled = true;
				if (orCreateComponent.NumberText.renderer != null)
				{
					orCreateComponent.NumberText.renderer.enabled = true;
				}
			}
			if ((UnityEngine.Object)(object)orCreateComponent.IconImage != null)
			{
				((Component)(object)orCreateComponent.IconImage).gameObject.SetActive(value: false);
			}
			return;
		}
		int num = __0.beforeCutScore + __0.afterCutScore + __0.centerDistanceCutScore;
		if (__0.cutScore >= instance.TargetScore || num >= instance.TargetScore)
		{
			orCreateComponent.IsShowingIcon = true;
			if (orCreateComponent.NumberText != null)
			{
				if (instance.HideNumberOnTargetScore)
				{
					orCreateComponent.NumberText.SetText(string.Empty);
					orCreateComponent.NumberText.enabled = false;
					if (orCreateComponent.NumberText.renderer != null)
					{
						orCreateComponent.NumberText.renderer.enabled = false;
					}
				}
				else
				{
					orCreateComponent.NumberText.enabled = true;
					if (orCreateComponent.NumberText.renderer != null)
					{
						orCreateComponent.NumberText.renderer.enabled = true;
					}
				}
			}
			if (!((UnityEngine.Object)(object)orCreateComponent.IconImage != null))
			{
				return;
			}
			Material noGlowMaterial = GetNoGlowMaterial();
			if (noGlowMaterial != null && ((Graphic)(object)orCreateComponent.IconImage).material != noGlowMaterial)
			{
				((Graphic)(object)orCreateComponent.IconImage).material = noGlowMaterial;
			}
			if (!orCreateComponent.HasPickedSpriteForThisCut || ((Image)(object)orCreateComponent.IconImage).sprite == null)
			{
				Sprite randomSprite = IconCacheService.GetRandomSprite();
				if (randomSprite != null)
				{
					((Image)(object)orCreateComponent.IconImage).sprite = randomSprite;
				}
				orCreateComponent.HasPickedSpriteForThisCut = true;
			}
			((Component)(object)orCreateComponent.IconImage).gameObject.layer = __instance.gameObject.layer;
			((Component)(object)orCreateComponent.IconImage).transform.localPosition = new Vector3(0f, instance.OffsetY, -0.02f);
			((Component)(object)orCreateComponent.IconImage).transform.localScale = Vector3.one * instance.IconScale;
			float alpha = ((orCreateComponent.NumberText != null) ? orCreateComponent.NumberText.color.a : 1f);
			((Graphic)(object)orCreateComponent.IconImage).color = instance.GetTintColor(alpha);
			((Component)(object)orCreateComponent.IconImage).gameObject.SetActive(value: true);
			return;
		}
		orCreateComponent.IsShowingIcon = false;
		if (orCreateComponent.NumberText != null)
		{
			orCreateComponent.NumberText.enabled = true;
			if (orCreateComponent.NumberText.renderer != null)
			{
				orCreateComponent.NumberText.renderer.enabled = true;
			}
		}
		if ((UnityEngine.Object)(object)orCreateComponent.IconImage != null)
		{
			((Component)(object)orCreateComponent.IconImage).gameObject.SetActive(value: false);
		}
	}

	private static void ManualUpdatePostfix(FlyingObjectEffect __instance)
	{
		if (__instance == null)
		{
			return;
		}
		Beat115NalulunaIconComponent component = __instance.GetComponent<Beat115NalulunaIconComponent>();
		if (component == null || !component.IsShowingIcon || (UnityEngine.Object)(object)component.IconImage == null)
		{
			return;
		}
		PluginConfig instance = PluginConfig.Instance;
		if (instance == null || !instance.Enabled)
		{
			return;
		}
		if (instance.HideNumberOnTargetScore && component.NumberText != null)
		{
			if (component.NumberText.enabled)
			{
				component.NumberText.enabled = false;
			}
			if (component.NumberText.renderer != null && component.NumberText.renderer.enabled)
			{
				component.NumberText.renderer.enabled = false;
			}
		}
		float num = ((component.NumberText != null) ? component.NumberText.color.a : 1f);
		((Graphic)(object)component.IconImage).color = instance.GetTintColor(num);
		if (((Component)(object)component.IconImage).gameObject.layer != __instance.gameObject.layer)
		{
			((Component)(object)component.IconImage).gameObject.layer = __instance.gameObject.layer;
		}
		if (instance.OverrideUnderlineColorOn115 && (UnityEngine.Object)(object)component.UnderlineImage != null && ((Component)(object)component.UnderlineImage).gameObject.activeSelf)
		{
			((Graphic)(object)component.UnderlineImage).color = instance.GetUnderlineColor(Mathf.Sqrt(Mathf.Clamp01(num)));
		}
	}

	private static Beat115NalulunaIconComponent GetOrCreateComponent(FlyingObjectEffect effect)
	{
		Beat115NalulunaIconComponent beat115NalulunaIconComponent = effect.GetComponent<Beat115NalulunaIconComponent>();
		if (beat115NalulunaIconComponent != null && (UnityEngine.Object)(object)beat115NalulunaIconComponent.IconImage != null)
		{
			return beat115NalulunaIconComponent;
		}
		if (beat115NalulunaIconComponent == null)
		{
			beat115NalulunaIconComponent = effect.gameObject.AddComponent<Beat115NalulunaIconComponent>();
		}
		beat115NalulunaIconComponent.NumberText = (_textField?.GetValue(effect) as TextMeshPro) ?? effect.transform.Find("Number")?.GetComponent<TextMeshPro>();
		beat115NalulunaIconComponent.UnderlineImage = effect.transform.Find("CenterDistanceMaxScoreIndicator")?.GetComponent<ImageView>();
		Transform transform = effect.transform.Find("Beat115NalulunaIcon");
		GameObject gameObject = ((transform != null) ? transform.gameObject : new GameObject("Beat115NalulunaIcon"));
		gameObject.layer = effect.gameObject.layer;
		gameObject.transform.SetParent(effect.transform, worldPositionStays: false);
		ImageView imageView = gameObject.GetComponent<ImageView>() ?? gameObject.AddComponent<ImageView>();
		RectTransform rectTransform = ((Graphic)(object)imageView).rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.sizeDelta = Vector2.one;
		((Image)(object)imageView).type = Image.Type.Simple;
		((Image)(object)imageView).preserveAspect = true;
		((Image)(object)imageView).sprite = IconCacheService.GetRandomSprite();
		Material noGlowMaterial = GetNoGlowMaterial();
		if (noGlowMaterial != null)
		{
			((Graphic)(object)imageView).material = noGlowMaterial;
		}
		PluginConfig instance = PluginConfig.Instance;
		float num = instance?.IconScale ?? 0.65f;
		float y = instance?.OffsetY ?? 0.02f;
		gameObject.transform.localPosition = new Vector3(0f, y, -0.02f);
		gameObject.transform.localScale = Vector3.one * num;
		gameObject.SetActive(value: false);
		beat115NalulunaIconComponent.IconImage = imageView;
		return beat115NalulunaIconComponent;
	}

	internal static Material GetNoGlowMaterial()
	{
		if (_cachedNoGlowMaterial != null)
		{
			return _cachedNoGlowMaterial;
		}
		if (_uiNoGlowMaterialProp != null)
		{
			_cachedNoGlowMaterial = _uiNoGlowMaterialProp.GetValue(null) as Material;
			if (_cachedNoGlowMaterial != null)
			{
				return _cachedNoGlowMaterial;
			}
		}
		_cachedNoGlowMaterial = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault((Material m) => m != null && m.name == "UINoGlow");
		return _cachedNoGlowMaterial;
	}
}
