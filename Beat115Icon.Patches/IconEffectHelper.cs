using System.Reflection;
using Beat115Icon.Configuration;
using Beat115Icon.Services;
using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace Beat115Icon.Patches;

internal static class IconEffectHelper
{
	private static readonly FieldInfo FlyingDurationField = typeof(FlyingObjectEffect).GetField("_duration", BindingFlags.Instance | BindingFlags.NonPublic);

	public static void ApplyCustomDurationIfNeeded(FlyingObjectEffect effect, ref bool hasApplied)
	{
		if (hasApplied || effect == null || FlyingDurationField == null)
		{
			return;
		}
		hasApplied = true;
		PluginConfig config = PluginConfig.Instance;
		if (config != null && config.DisplayDuration > 0.01f)
		{
			FlyingDurationField.SetValue(effect, config.DisplayDuration);
		}
	}

	public static void AdvanceAnimation(ImageView iconImage, IconAsset asset, ref int frameIndex, ref float frameTimer, ref int lastAnimatedFrame)
	{
		if ((Object)(object)iconImage == null || asset == null || !asset.IsAnimated)
		{
			return;
		}
		int currentUnityFrame = Time.frameCount;
		if (lastAnimatedFrame == currentUnityFrame)
		{
			return;
		}
		lastAnimatedFrame = currentUnityFrame;

		Sprite[] frames = asset.Frames;
		float[] delays = asset.FrameDelaysSec;
		if (frames == null || frames.Length <= 1 || delays == null || delays.Length == 0)
		{
			return;
		}

		float dt = Mathf.Min(Time.deltaTime, 0.1f);
		if (dt <= 0f)
		{
			return;
		}

		frameTimer += dt;
		int prevIndex = frameIndex;
		int steps = 0;
		while (steps < 8)
		{
			float delay = (frameIndex >= 0 && frameIndex < delays.Length) ? delays[frameIndex] : 0.1f;
			if (delay < 0.02f)
			{
				delay = 0.1f;
			}
			if (frameTimer < delay)
			{
				break;
			}
			frameTimer -= delay;
			frameIndex = (frameIndex + 1) % frames.Length;
			steps++;
		}
		if (steps >= 8)
		{
			frameTimer = 0f;
		}

		if (frameIndex != prevIndex)
		{
			Sprite nextSprite = frames[frameIndex];
			if (nextSprite != null && ((Image)(object)iconImage).sprite != nextSprite)
			{
				((Image)(object)iconImage).sprite = nextSprite;
				((Graphic)(object)iconImage).SetAllDirty();
			}
		}
	}
}
