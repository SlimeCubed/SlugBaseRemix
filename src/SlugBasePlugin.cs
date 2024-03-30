using BepInEx;
using BepInEx.Logging;
using SlugBase.Features;
using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using SlugBase.Assets;
using SlugBase.Interface;
using SlugBase.SaveData;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace SlugBase
{
    [BepInPlugin("slime-cubed.slugbase", "SlugBase", Version)]
    internal class SlugBasePlugin : BaseUnityPlugin
    {
        new internal static ManualLogSource Logger;
        internal static Config RemixConfig;
        internal static Color SlugBaseBlue = new(19f / 255f, 63f / 255f, 231f / 255f);
        internal static Color SlugBaseGray = new(146f / 255f, 150f / 255f, 164f / 255f);
        internal const string Version = "2.7.7";

        private bool _initialized = false;

        public SlugBasePlugin()
        {
            Logger = base.Logger;
        }

        public void Awake()
        {
            On.RainWorld.PreModsInit += (orig, self) =>
            {
                try
                {
                    // Ensure that all features are registered before use
                    RuntimeHelpers.RunClassConstructor(typeof(PlayerFeatures).TypeHandle);
                    RuntimeHelpers.RunClassConstructor(typeof(GameFeatures).TypeHandle);
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }

                orig(self);
            };

            On.RainWorld.OnModsInit += (orig, self) =>
            {
                orig(self);

                if (_initialized) return;
                _initialized = true;

                try
                {
                    RemixConfig = new Config();
                    // MachineConnector.SetRegisteredOI("slime-cubed.slugbase", RemixConfig);

                    Futile.atlasManager.LoadAtlas("atlases/slugbase");

                    ErrorList.Instance = ErrorList.Attach();
                    FeatureManager.LogErrors();

                    CoreHooks.Apply();
                    AssetHooks.Apply();
                    FeatureHooks.Apply();
                    ExpeditionHooks.Apply();
                    JollyCoopHooks.Apply();
                    SaveDataHooks.Apply();

                    SlugBaseCharacter.Registry.WatchForChanges = true;
                    CustomScene.Registry.WatchForChanges = true;
                    CustomSlideshow.Registry.WatchForChanges = true;
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            };

            On.RainWorld.PostModsInit += (orig, self) =>
            {
                orig(self);

                try
                {
                    ErrorList.Instance.ClearFileErrors();
                    ScanFiles();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };
        }

        public static void ScanFiles()
        {
            SlugBaseCharacter.Registry.ScanDirectory("slugbase");
            CustomScene.Registry.ScanDirectory("slugbase/scenes");
            CustomSlideshow.Registry.ScanDirectory("slugbase/slideshows");
        }

        public void Update()
        {
            SlugBaseCharacter.Registry.ReloadChangedFiles();
            CustomScene.Registry.ReloadChangedFiles();
            CustomSlideshow.Registry.ReloadChangedFiles();
        }
    }
}
