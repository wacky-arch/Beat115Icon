using System;
using System.Reflection;
using Beat115Icon.Configuration;
using Beat115Icon.Patches;
using Beat115Icon.Services;
using Beat115Icon.UI;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Logging;

namespace Beat115Icon;

[Plugin(RuntimeOptions.DynamicInit)]
public class Plugin
{
	public const string HarmonyId = "com.wacky.beat115icon";

	private Harmony _harmony;

	internal static Plugin Instance { get; private set; }

	internal static Logger Log { get; private set; }

	[Init]
	public void Init(Logger logger, Config conf)
	{
		Instance = this;
		Log = logger;
		PluginConfig.Instance = conf.Generated<PluginConfig>();
	}

	[OnEnable]
	public void OnEnable()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		try
		{
			IconCacheService.Initialize(PluginConfig.Instance?.CustomDirectoryPath);
			_harmony = new Harmony("com.wacky.beat115icon");
			_harmony.PatchAll(Assembly.GetExecutingAssembly());
			NalulunaScorePatcher.ApplyPatch(_harmony);
			Beat115SettingsController.Instance.Initialize();
			Log?.Info("Beat115Icon successfully enabled! (Vanilla & NalulunaCutScore ready)");
		}
		catch (Exception ex)
		{
			Log?.Error("Error enabling Beat115Icon: " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	[OnDisable]
	public void OnDisable()
	{
		try
		{
			Beat115SettingsController.Instance.Dispose();
			Harmony harmony = _harmony;
			if (harmony != null)
			{
				harmony.UnpatchSelf();
			}
			Log?.Info("Beat115Icon disabled.");
		}
		catch (Exception ex)
		{
			Log?.Error("Error disabling Beat115Icon: " + ex.Message);
		}
	}
}
