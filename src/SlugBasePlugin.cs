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

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace SlugBase
{
    [BepInPlugin("slime-cubed.slugbase", "SlugBase", "2.0.0")]
    internal class SlugBasePlugin : BaseUnityPlugin
    {
        new internal static ManualLogSource Logger;
        internal static Config RemixConfig;

        private bool _initialized = false;

        public SlugBasePlugin()
        {
            Logger = base.Logger;
        }

        public void Awake()
        {
            On.RainWorld.PreModsInit += (orig, self) =>
            {
                // Ensure that all features are registered before use
                RuntimeHelpers.RunClassConstructor(typeof(PlayerFeatures).TypeHandle);
                RuntimeHelpers.RunClassConstructor(typeof(GameFeatures).TypeHandle);

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

                    CoreHooks.Apply();
                    AssetHooks.Apply();
                    FeatureHooks.Apply();
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            };

            On.RainWorld.PostModsInit += (orig, self) =>
            {
                orig(self);

                ScanFiles();
            };
        }

        public static void ScanFiles()
        {
            SlugBaseCharacter.Registry.WatchForChanges = true;
            CustomScene.Registry.WatchForChanges = true;

            SlugBaseCharacter.Registry.ScanDirectory("slugbase");
            CustomScene.Registry.ScanDirectory("slugbase/scenes");
        }

        public void Update()
        {
            SlugBaseCharacter.Registry.ReloadChangedFiles();
            CustomScene.Registry.ReloadChangedFiles();
        }
    }
}
