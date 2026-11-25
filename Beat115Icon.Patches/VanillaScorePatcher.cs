using System;
using System.Reflection;
using Beat115Icon.Configuration;
using Beat115Icon.Services;
using HMUI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beat115Icon.Patches;

[HarmonyPatch(typeof(FlyingScoreEffect))]
internal static class VanillaScorePatcher
{
	private static readonly FieldInfo _textField = typeof(FlyingScoreEffect).GetField("_text", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo _underlineField = typeof(FlyingScoreEffect).GetField("_maxCutDistanceScoreIndicator", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo _fadeCurveField = typeof(FlyingScoreEffect).GetField("_fadeAnimationCurve", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo _colorField = typeof(FlyingScoreEffect).GetField("_color", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo _colorAMultiplierField = typeof(FlyingScoreEffect).GetField("_colorAMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);

	[HarmonyPatch("InitAndPresent", new Type[]
	{
		typeof(IReadonlyCutScoreBuffer),
		typeof(float),
		typeof(Vector3),
		typeof(Color)
	})]
	[HarmonyPrefix]
	private static void InitAndPresentPrefix(FlyingScoreEffect __instance)
	{
		if (!(__instance == null))
		{
			PluginConfig instance = PluginConfig.Instance;
			if (instance == null || !instance.Enabled)
			{
				__instance.GetComponent<Beat115VanillaIconComponent>()?.ResetState();
			}
			else
			{
				GetOrCreateComponent(__instance).ResetState();
			}
		}
	}

	[HarmonyPatch("RefreshScore", new Type[]
	{
		typeof(int),
		typeof(int)
	})]
	[HarmonyPostfix]
	private static void RefreshScorePostfix(FlyingScoreEffect __instance, int score)
	{
		if (__instance == null)
		{
			return;
		}
		PluginConfig instance = PluginConfig.Instance;
		if (instance == null || !instance.Enabled)
		{
			__instance.GetComponent<Beat115VanillaIconComponent>()?.ResetState();
			return;
		}
		Beat115VanillaIconComponent orCreateComponent = GetOrCreateComponent(__instance);
		if (score >= instance.TargetScore)
		{
			orCreateComponent.IsTargetScore = true;
			if (orCreateComponent.NumberText != null)
			{
				if (instance.HideNumberOnTargetScore)
				{
					orCreateComponent.NumberText.text = string.Empty;
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
			Material noGlowMaterial = NalulunaScorePatcher.GetNoGlowMaterial();
			if (noGlowMaterial != null && ((Graphic)(object)orCreateComponent.IconImage).material != noGlowMaterial)
			{
				((Graphic)(object)orCreateComponent.IconImage).material = noGlowMaterial;
			}
			if (!orCreateComponent.HasPickedSpriteForThisCut || orCreateComponent.CurrentAsset == null || ((Image)(object)orCreateComponent.IconImage).sprite == null)
			{
				IconAsset randomAsset = IconCacheService.GetRandomAsset();
				orCreateComponent.CurrentAsset = randomAsset;
				orCreateComponent.CurrentFrameIndex = 0;
				orCreateComponent.FrameTimer = 0f;
				orCreateComponent.LastAnimatedFrame = Time.frameCount;
				Sprite firstSprite = randomAsset?.FirstSprite;
				if (firstSprite != null)
				{
					((Image)(object)orCreateComponent.IconImage).sprite = firstSprite;
					((Graphic)(object)orCreateComponent.IconImage).SetAllDirty();
				}
				orCreateComponent.HasPickedSpriteForThisCut = true;
			}
			if (orCreateComponent.CanvasObject != null)
			{
				if (orCreateComponent.CanvasObject.layer != __instance.gameObject.layer)
				{
					orCreateComponent.CanvasObject.layer = __instance.gameObject.layer;
				}
				ApplyUniformCanvasScale(orCreateComponent.CanvasObject.transform, __instance.transform, 0.4f);
			}
			((Component)(object)orCreateComponent.IconImage).gameObject.layer = __instance.gameObject.layer;
			((Graphic)(object)orCreateComponent.IconImage).rectTransform.sizeDelta = Vector2.one;
			((Image)(object)orCreateComponent.IconImage).preserveAspect = true;
			((Component)(object)orCreateComponent.IconImage).transform.localPosition = new Vector3(0f, instance.OffsetY, -0.02f);
			((Component)(object)orCreateComponent.IconImage).transform.localScale = Vector3.one * instance.IconScale;
			float alpha = ((orCreateComponent.NumberText != null) ? Mathf.Clamp01(orCreateComponent.NumberText.color.a) : 0f);
			((Graphic)(object)orCreateComponent.IconImage).color = instance.GetTintColor(alpha);
			((Component)(object)orCreateComponent.IconImage).gameObject.SetActive(value: true);
			((Graphic)(object)orCreateComponent.IconImage).SetAllDirty();
			return;
		}
		orCreateComponent.ResetState();
	}

	[HarmonyPatch("ManualUpdate", new Type[] { typeof(float) })]
	[HarmonyPostfix]
	private static void ManualUpdatePostfix(FlyingScoreEffect __instance, float t)
	{
		if (__instance == null)
		{
			return;
		}
		PluginConfig instance = PluginConfig.Instance;
		if (instance == null || !instance.Enabled)
		{
			return;
		}
		Beat115VanillaIconComponent component = __instance.GetComponent<Beat115VanillaIconComponent>();
		if (component == null || !component.IsTargetScore || (UnityEngine.Object)(object)component.IconImage == null || !((Component)(object)component.IconImage).gameObject.activeSelf)
		{
			return;
		}
		IconEffectHelper.ApplyCustomDurationIfNeeded(__instance, ref component.HasAppliedCustomDuration);
		IconEffectHelper.AdvanceAnimation(component.IconImage, component.CurrentAsset, ref component.CurrentFrameIndex, ref component.FrameTimer, ref component.LastAnimatedFrame);
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
		float alpha;
		if (component.NumberText != null)
		{
			alpha = Mathf.Clamp01(component.NumberText.color.a);
		}
		else
		{
			float num = component.FadeCurve?.Evaluate(t) ?? (1f - t);
			Color color = ((_colorField != null) ? ((Color)_colorField.GetValue(__instance)) : Color.white);
			float num2 = ((_colorAMultiplierField != null) ? ((float)_colorAMultiplierField.GetValue(__instance)) : 1f);
			alpha = Mathf.Clamp01(num * color.a * num2);
		}
		((Graphic)(object)component.IconImage).color = instance.GetTintColor(alpha);
		if (component.CanvasObject != null)
		{
			if (component.CanvasObject.layer != __instance.gameObject.layer)
			{
				component.CanvasObject.layer = __instance.gameObject.layer;
			}
			ApplyUniformCanvasScale(component.CanvasObject.transform, __instance.transform, 0.4f);
		}
		if (((Component)(object)component.IconImage).gameObject.layer != __instance.gameObject.layer)
		{
			((Component)(object)component.IconImage).gameObject.layer = __instance.gameObject.layer;
		}
		((Component)(object)component.IconImage).transform.localPosition = new Vector3(0f, instance.OffsetY, -0.02f);
		((Component)(object)component.IconImage).transform.localScale = Vector3.one * instance.IconScale;
		if (instance.OverrideUnderlineColorOn115 && component.UnderlineRenderer != null && component.UnderlineRenderer.enabled)
		{
			component.UnderlineRenderer.color = instance.GetUnderlineColor(alpha);
		}
	}

	private static Beat115VanillaIconComponent GetOrCreateComponent(FlyingScoreEffect effect)
	{
		Beat115VanillaIconComponent beat115VanillaIconComponent = effect.GetComponent<Beat115VanillaIconComponent>();
		if (beat115VanillaIconComponent != null && (UnityEngine.Object)(object)beat115VanillaIconComponent.IconImage != null && beat115VanillaIconComponent.CanvasObject != null)
		{
			return beat115VanillaIconComponent;
		}
		if (beat115VanillaIconComponent == null)
		{
			beat115VanillaIconComponent = effect.gameObject.AddComponent<Beat115VanillaIconComponent>();
		}
		beat115VanillaIconComponent.EffectInstance = effect;
		beat115VanillaIconComponent.NumberText = _textField?.GetValue(effect) as TextMeshPro;
		beat115VanillaIconComponent.UnderlineRenderer = _underlineField?.GetValue(effect) as SpriteRenderer;
		beat115VanillaIconComponent.FadeCurve = _fadeCurveField?.GetValue(effect) as AnimationCurve;
		Transform transform = effect.transform.Find("Beat115VanillaCanvas");
		GameObject gameObject;
		if (transform != null)
		{
			gameObject = transform.gameObject;
		}
		else
		{
			gameObject = new GameObject("Beat115VanillaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CurvedCanvasSettings));
			gameObject.layer = effect.gameObject.layer;
			gameObject.transform.SetParent(effect.transform, worldPositionStays: false);
		}
		Canvas canvas = gameObject.GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
		CurvedCanvasSettings curvedCanvasSettings = gameObject.GetComponent<CurvedCanvasSettings>() ?? gameObject.AddComponent<CurvedCanvasSettings>();
		curvedCanvasSettings.SetRadius(0f);
		RectTransform canvasRect = gameObject.GetComponent<RectTransform>();
		if (canvasRect != null)
		{
			canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
			canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
			canvasRect.pivot = new Vector2(0.5f, 0.5f);
			canvasRect.anchoredPosition = Vector2.zero;
			canvasRect.sizeDelta = Vector2.zero;
		}
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = Quaternion.identity;
		ApplyUniformCanvasScale(gameObject.transform, effect.transform, 0.4f);
		Transform transform2 = gameObject.transform.Find("Beat115VanillaIcon");
		GameObject gameObject2;
		if (transform2 != null)
		{
			gameObject2 = transform2.gameObject;
		}
		else
		{
			gameObject2 = new GameObject("Beat115VanillaIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(ImageView));
			gameObject2.layer = effect.gameObject.layer;
			gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		}
		ImageView imageView = gameObject2.GetComponent<ImageView>() ?? gameObject2.AddComponent<ImageView>();
		RectTransform iconRect = ((Graphic)(object)imageView).rectTransform;
		iconRect.anchorMin = new Vector2(0.5f, 0.5f);
		iconRect.anchorMax = new Vector2(0.5f, 0.5f);
		iconRect.pivot = new Vector2(0.5f, 0.5f);
		iconRect.anchoredPosition = Vector2.zero;
		iconRect.sizeDelta = Vector2.one;
		((Image)(object)imageView).type = Image.Type.Simple;
		((Image)(object)imageView).preserveAspect = true;
		((Image)(object)imageView).sprite = IconCacheService.GetRandomSprite();
		Material noGlowMaterial = NalulunaScorePatcher.GetNoGlowMaterial();
		if (noGlowMaterial != null)
		{
			((Graphic)(object)imageView).material = noGlowMaterial;
		}
		PluginConfig instance = PluginConfig.Instance;
		float num = instance?.IconScale ?? 0.65f;
		float y = instance?.OffsetY ?? 0.02f;
		gameObject2.transform.localPosition = new Vector3(0f, y, -0.02f);
		gameObject2.transform.localScale = Vector3.one * num;
		gameObject2.SetActive(value: false);
		beat115VanillaIconComponent.CanvasObject = gameObject;
		beat115VanillaIconComponent.IconImage = imageView;
		return beat115VanillaIconComponent;
	}

	private static void ApplyUniformCanvasScale(Transform canvasTransform, Transform parentTransform, float targetScale)
	{
		if (canvasTransform == null)
		{
			return;
		}
		if (parentTransform == null)
		{
			canvasTransform.localScale = Vector3.one * targetScale;
			return;
		}
		Vector3 lossy = parentTransform.lossyScale;
		float refScale = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
		float sx = Mathf.Abs(lossy.x) > 0.0001f ? (targetScale * refScale / Mathf.Abs(lossy.x)) : targetScale;
		float sy = targetScale;
		float sz = Mathf.Abs(lossy.z) > 0.0001f ? (targetScale * refScale / Mathf.Abs(lossy.z)) : targetScale;
		canvasTransform.localScale = new Vector3(sx, sy, sz);
	}
}
