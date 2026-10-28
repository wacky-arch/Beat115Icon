using System;
using System.Reflection;
using Beat115Icon.Configuration;
using Beat115Icon.Patches;
using Beat115Icon.Services;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPALogger = IPA.Logging.Logger;

namespace Beat115Icon
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        public const string HarmonyId = "com.wacky.beat115icon";

        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        private Harmony _harmony;

        [Init]
        public void Init(IPALogger logger, Config conf)
        {
            Instance = this;
            Log = logger;
            PluginConfig.Instance = conf.Generated<PluginConfig>();
        }

        [OnEnable]
        public void OnEnable()
        {
            try
            {
                IconCacheService.Initialize(PluginConfig.Instance?.CustomDirectoryPath);

                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                NalulunaScorePatcher.ApplyPatch(_harmony);

                Log?.Info("Beat115Icon successfully enabled! (Vanilla & NalulunaCutScore ready)");
            }
            catch (Exception ex)
            {
                Log?.Error($"Error enabling Beat115Icon: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [OnDisable]
        public void OnDisable()
        {
            try
            {
                _harmony?.UnpatchSelf();
                Log?.Info("Beat115Icon disabled.");
            }
            catch (Exception ex)
            {
                Log?.Error($"Error disabling Beat115Icon: {ex.Message}");
            }
        }
    }
}
