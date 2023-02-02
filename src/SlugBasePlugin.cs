using BepInEx;
using BepInEx.Logging;
using SlugBase.Features;
using System.IO;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using System.Collections.Generic;
using SlugBase.Assets;

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

        private bool _initialized = false;

        public SlugBasePlugin()
        {
            Logger = base.Logger;
        }

        public void Awake()
        {
            On.RainWorld.PreModsInit += (orig, self) =>
            {
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
            SlugBaseCharacter.Registry.ScanDirectory("slugbase");
            CustomScene.Registry.ScanDirectory("slugbase/scenes");
        }
    }
}
