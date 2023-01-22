using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using SlugBase.Characters;

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
                Features.Init();

                orig(self);
            };

            On.RainWorld.OnModsInit += (orig, self) =>
            {
                orig(self);

                if (_initialized) return;
                _initialized = true;

                FeatureHooks.Apply();

                On.PlayerState.ctor += PlayerState_ctor;
            };

            On.RainWorld.PostModsInit += (orig, self) =>
            {
                orig(self);

                CharacterManager.Scan();
            };
        }

        private void PlayerState_ctor(On.PlayerState.orig_ctor orig, PlayerState self, AbstractCreature crit, int playerNumber, SlugcatStats.Name slugcatCharacter, bool isGhost)
        {
            orig(self, crit, playerNumber, slugcatCharacter, isGhost);

            // Temporary!
            CharacterManager.characters[new SlugcatStats.Name("SlugBaseExample")].PlayerFeatures.Attach(self);
        }
    }
}
