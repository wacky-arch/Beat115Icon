using System;
using UnityEngine;

namespace Beat115Icon.Configuration;

internal class PluginConfig
{
	public static PluginConfig Instance { get; set; }

	public virtual bool Enabled { get; set; } = true;


	public virtual int TargetScore { get; set; } = 115;


	public virtual float IconScale { get; set; } = 0.65f;


	public virtual float OffsetY { get; set; } = 0.02f;


	public virtual float VanillaIconScale { get; set; } = 0.7f;


	public virtual float VanillaOffsetY { get; set; }

	public virtual bool HideNumberOnTargetScore { get; set; } = true;


	public virtual float TintR { get; set; } = 1f;


	public virtual float TintG { get; set; } = 1f;


	public virtual float TintB { get; set; } = 1f;


	public virtual bool OverrideUnderlineColorOn115 { get; set; } = true;


	public virtual float UnderlineRedR { get; set; } = 1f;


	public virtual float UnderlineRedG { get; set; } = 0.2f;


	public virtual float UnderlineRedB { get; set; } = 0.3f;


	public virtual string CustomDirectoryPath { get; set; } = "";


	public event Action ConfigChangedEvent;

	public virtual void Changed()
	{
		this.ConfigChangedEvent?.Invoke();
	}

	public void ResetDefaults()
	{
		Enabled = true;
		IconScale = 0.65f;
		OffsetY = 0.02f;
		VanillaIconScale = 0.7f;
		VanillaOffsetY = 0f;
		HideNumberOnTargetScore = true;
		OverrideUnderlineColorOn115 = true;
		Changed();
	}

	public Color GetTintColor(float alpha)
	{
		return new Color(TintR, TintG, TintB, alpha);
	}

	public Color GetUnderlineColor(float alpha)
	{
		return new Color(UnderlineRedR, UnderlineRedG, UnderlineRedB, alpha);
	}
}
