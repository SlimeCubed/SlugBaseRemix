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
        internal static List<LoadError> loadErrors = new();

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

                FeatureHooks.Apply();
                CoreHooks.Apply();
            };

            On.RainWorld.PostModsInit += (orig, self) =>
            {
                orig(self);

                ScanCharacters();
            };
        }

        public static void ScanCharacters()
        {
            loadErrors.Clear();
            var files = AssetManager.ListDirectory("slugbase", includeAll: true);

            foreach (var file in files.Where(file => file.EndsWith(".json")))
            {
                try
                {
                    var chara = SlugBaseCharacter.Characters.FirstOrDefault(chara => chara.Path.Equals(file, StringComparison.InvariantCultureIgnoreCase));
                    if (chara == null)
                    {
                        chara = SlugBaseCharacter.RegisterFromFile(file);
                        Logger.LogMessage($"Loaded SlugBase character \"{chara.Name}\" from {Path.GetFileName(file)}");
                    }
                }
                catch (Exception e)
                {
                    if (e is JsonException jsonE)
                        Logger.LogError($"Failed to parse SlugBase character from {Path.GetFileName(file)}: {jsonE.Message}\nField: {jsonE.JsonPath ?? "unknown"}");
                    else
                        Logger.LogError($"Failed to load SlugBase character from {Path.GetFileName(file)}: {e.Message}");
                    Debug.LogException(e);

                    loadErrors.Add(new LoadError(file, e));
                }
            }
        }

        internal class LoadError
        {
            public readonly string Path;
            public readonly Exception Exception;

            public LoadError(string path, Exception exception)
            {
                Path = path;
                Exception = exception;
            }
        }
    }
}
