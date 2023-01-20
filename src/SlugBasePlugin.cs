using System;
using BepInEx;

namespace SlugBase
{
    [BepInPlugin("slime-cubed.slugbase", "SlugBase", "2.0.0")]
    internal class SlugBasePlugin : BaseUnityPlugin
    {
        private bool initialized = false;

        public void Awake()
        {
            On.RainWorld.OnModsInit += (orig, self) =>
            {
                orig(self);

                if (initialized) return;
                initialized = true;

                // Features.PlayerFeatures.Register("explode_on_death", (p, s) => new ExplodeOnDeath(p, s));
            };
        }
    }
}
