using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Beat115Icon.Configuration;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beat115Icon.UI;

internal class Beat115SettingsController : IDisposable, INotifyPropertyChanged
{
	private const string ResourceName = "Beat115Icon.UI.Views.SettingsView.bsml";

	private const string MenuTitle = "Beat115Icon";

	private static readonly PropertyInfo MainUIFontMaterialProp = typeof(BeatSaberUI).GetProperty("MainUIFontMaterial", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

	private static Beat115SettingsController _instance;

	public static Beat115SettingsController Instance => _instance ??= new Beat115SettingsController();

	[UIObject("beat115-settings-root")]
	private GameObject _settingsRoot;

	public event PropertyChangedEventHandler PropertyChanged;

	[UIValue("enabled")]
	public bool Enabled
	{
		get => PluginConfig.Instance?.Enabled ?? true;
		set
		{
			if (PluginConfig.Instance != null && PluginConfig.Instance.Enabled != value)
			{
				PluginConfig.Instance.Enabled = value;
				PluginConfig.Instance.Changed();
				NotifyPropertyChanged();
			}
		}
	}

	[UIValue("icon-scale")]
	public float IconScale
	{
		get => PluginConfig.Instance?.IconScale ?? 0.65f;
		set
		{
			if (PluginConfig.Instance != null && Math.Abs(PluginConfig.Instance.IconScale - value) > 0.001f)
			{
				PluginConfig.Instance.IconScale = value;
				PluginConfig.Instance.Changed();
				NotifyPropertyChanged();
			}
		}
	}

	[UIValue("vanilla-icon-scale")]
	public float VanillaIconScale
	{
		get => PluginConfig.Instance?.VanillaIconScale ?? 0.7f;
		set
		{
			if (PluginConfig.Instance != null && Math.Abs(PluginConfig.Instance.VanillaIconScale - value) > 0.001f)
			{
				PluginConfig.Instance.VanillaIconScale = value;
				PluginConfig.Instance.Changed();
				NotifyPropertyChanged();
			}
		}
	}

	[UIValue("offset-y")]
	public float OffsetY
	{
		get => PluginConfig.Instance?.OffsetY ?? 0.02f;
		set
		{
			if (PluginConfig.Instance != null && Math.Abs(PluginConfig.Instance.OffsetY - value) > 0.001f)
			{
				PluginConfig.Instance.OffsetY = value;
				PluginConfig.Instance.Changed();
				NotifyPropertyChanged();
			}
		}
	}

	[UIValue("vanilla-offset-y")]
	public float VanillaOffsetY
	{
		get => PluginConfig.Instance?.VanillaOffsetY ?? 0f;
		set
		{
			if (PluginConfig.Instance != null && Math.Abs(PluginConfig.Instance.VanillaOffsetY - value) > 0.001f)
			{
				PluginConfig.Instance.VanillaOffsetY = value;
				PluginConfig.Instance.Changed();
				NotifyPropertyChanged();
			}
		}
	}

	[UIValue("hide-number")]
	public bool HideNumberOnTargetScore
	{
		get => PluginConfig.Instance?.HideNumberOnTargetScore ?? true;
		set
		{
			if (PluginConfig.Instance != null && PluginConfig.Instance.HideNumberOnTargetScore != value)
			{
				PluginConfig.Instance.HideNumberOnTargetScore = value;
				PluginConfig.Instance.Changed();
				NotifyPropertyChanged();
			}
		}
	}

	[UIValue("override-underline")]
	public bool OverrideUnderlineColorOn115
	{
		get => PluginConfig.Instance?.OverrideUnderlineColorOn115 ?? true;
		set
		{
			if (PluginConfig.Instance != null && PluginConfig.Instance.OverrideUnderlineColorOn115 != value)
			{
				PluginConfig.Instance.OverrideUnderlineColorOn115 = value;
				PluginConfig.Instance.Changed();
				NotifyPropertyChanged();
			}
		}
	}

	private bool _subscribedToMainMenu;

	public void Initialize()
	{
		if (!_subscribedToMainMenu)
		{
			MainMenuAwaiter.MainMenuInitializing += RegisterMenus;
			_subscribedToMainMenu = true;
		}
		if (ZenjectSingleton<BSMLSettings>.Instance != null || ZenjectSingleton<GameplaySetup>.Instance != null)
		{
			RegisterMenus();
		}
	}

	private void RegisterMenus()
	{
		try
		{
			if (ZenjectSingleton<BSMLSettings>.Instance != null)
			{
				ZenjectSingleton<BSMLSettings>.Instance.RemoveSettingsMenu(this);
				ZenjectSingleton<BSMLSettings>.Instance.AddSettingsMenu(MenuTitle, ResourceName, this);
			}
		}
		catch (Exception ex)
		{
			Plugin.Log?.Error("Failed to register BSMLSettings menu: " + ex.Message);
		}
		try
		{
			if (ZenjectSingleton<GameplaySetup>.Instance != null)
			{
				ZenjectSingleton<GameplaySetup>.Instance.RemoveTab(MenuTitle);
				ZenjectSingleton<GameplaySetup>.Instance.AddTab(MenuTitle, ResourceName, this, MenuType.All);
			}
		}
		catch (Exception ex)
		{
			Plugin.Log?.Error("Failed to register GameplaySetup tab: " + ex.Message);
		}
	}

	[UIAction("#post-parse")]
	private void PostParse()
	{
		if (_settingsRoot == null)
		{
			return;
		}
		Button[] buttons = _settingsRoot.GetComponentsInChildren<Button>(includeInactive: true);
		for (int i = 0; i < buttons.Length; i++)
		{
			NormalizeButtonLayout(buttons[i]);
		}
	}

	[UIAction("reset-defaults")]
	public void ResetDefaults()
	{
		PluginConfig.Instance?.ResetDefaults();
		NotifyPropertyChanged(nameof(Enabled));
		NotifyPropertyChanged(nameof(IconScale));
		NotifyPropertyChanged(nameof(VanillaIconScale));
		NotifyPropertyChanged(nameof(OffsetY));
		NotifyPropertyChanged(nameof(VanillaOffsetY));
		NotifyPropertyChanged(nameof(HideNumberOnTargetScore));
		NotifyPropertyChanged(nameof(OverrideUnderlineColorOn115));
	}

	private static void NormalizeButtonLayout(Button btn)
	{
		if (btn == null)
		{
			return;
		}
		ContentSizeFitter fitter = btn.GetComponent<ContentSizeFitter>();
		if (fitter != null)
		{
			fitter.enabled = false;
		}
		TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
		string text = tmp != null ? tmp.text : "";
		btn.SetButtonText(text);
		LayoutElement le = btn.GetComponent<LayoutElement>();
		if (le != null)
		{
			if (le.preferredWidth > 0f)
			{
				le.minWidth = le.preferredWidth;
				le.flexibleWidth = 0f;
			}
			if (le.preferredHeight > 0f)
			{
				le.minHeight = le.preferredHeight;
				le.flexibleHeight = 0f;
			}
			if (btn.transform is RectTransform rt && le.preferredWidth > 0f && le.preferredHeight > 0f)
			{
				rt.sizeDelta = new Vector2(le.preferredWidth, le.preferredHeight);
			}
		}
		Transform content = btn.transform.Find("Content");
		if (content != null)
		{
			LayoutElement contentLe = content.GetComponent<LayoutElement>();
			if (contentLe != null)
			{
				UnityEngine.Object.DestroyImmediate(contentLe);
			}
			LayoutGroup lg = content.GetComponent<LayoutGroup>();
			if (lg != null)
			{
				lg.padding = new RectOffset(0, 0, 0, 0);
				if (lg is HorizontalOrVerticalLayoutGroup hvlg)
				{
					hvlg.spacing = 0f;
					hvlg.childControlWidth = true;
					hvlg.childControlHeight = true;
					hvlg.childForceExpandWidth = true;
					hvlg.childForceExpandHeight = true;
				}
				lg.enabled = false;
			}
			if (content is RectTransform crt)
			{
				crt.anchorMin = Vector2.zero;
				crt.anchorMax = Vector2.one;
				crt.pivot = new Vector2(0.5f, 0.5f);
				crt.anchoredPosition = Vector2.zero;
				crt.sizeDelta = Vector2.zero;
			}
		}
		if (tmp != null)
		{
			UnityEngine.Component[] comps = tmp.gameObject.GetComponents<UnityEngine.Component>();
			if (comps != null)
			{
				foreach (UnityEngine.Component c in comps)
				{
					if (c != null && c.GetType().Name.IndexOf("Localized", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						UnityEngine.Object.DestroyImmediate(c);
					}
				}
			}
			tmp.gameObject.SetActive(true);
			tmp.enabled = true;
			if (BeatSaberUI.MainTextFont != null)
			{
				tmp.font = BeatSaberUI.MainTextFont;
			}
			Material mat = MainUIFontMaterialProp?.GetValue(null) as Material;
			if (mat != null)
			{
				tmp.fontSharedMaterial = mat;
			}
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.enableWordWrapping = false;
			tmp.overflowMode = TextOverflowModes.Overflow;
			tmp.enableAutoSizing = false;
			tmp.richText = true;
			tmp.color = Color.white;
			tmp.alpha = 1f;
			tmp.margin = Vector4.zero;
			if (tmp.rectTransform != null)
			{
				tmp.rectTransform.anchorMin = Vector2.zero;
				tmp.rectTransform.anchorMax = Vector2.one;
				tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
				tmp.rectTransform.anchoredPosition = Vector2.zero;
				tmp.rectTransform.sizeDelta = Vector2.zero;
			}
		}
	}

	public void Dispose()
	{
		if (_subscribedToMainMenu)
		{
			MainMenuAwaiter.MainMenuInitializing -= RegisterMenus;
			_subscribedToMainMenu = false;
		}
		try
		{
			if (ZenjectSingleton<BSMLSettings>.Instance != null)
			{
				ZenjectSingleton<BSMLSettings>.Instance.RemoveSettingsMenu(this);
			}
		}
		catch
		{
		}
		try
		{
			if (ZenjectSingleton<GameplaySetup>.Instance != null)
			{
				ZenjectSingleton<GameplaySetup>.Instance.RemoveTab(MenuTitle);
			}
		}
		catch
		{
		}
	}

	protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
