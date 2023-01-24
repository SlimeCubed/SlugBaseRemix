using BepInEx;
using BepInEx.Logging;
using SlugBase.Characters;
using SlugBase.Features;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;


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
            var c = GetInitialClass(crit, self);
            CharacterManager.characters[c] = CharacterManager.characters[new SlugcatStats.Name("SlugBaseExample")];
        }

        // This is so dumb
        private SlugcatStats.Name GetInitialClass(AbstractCreature player, PlayerState state)
        {
            if (ModManager.MSC && player.creatureTemplate.TopAncestor().type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC)
            {
                return MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup;
            }
            if (ModManager.CoopAvailable && player.Room.world.game.IsStorySession)
            {
                return player.world.game.rainWorld.options.jollyPlayerOptionsArray[state.playerNumber].playerClass ?? player.world.game.GetStorySession.saveState.saveStateNumber;
            }
            else
            {
                if (!ModManager.MSC || player.Room.world.game.IsStorySession)
                {
                    return state.slugcatCharacter;
                }

                if (ModManager.MSC && !player.world.game.IsStorySession)
                {
                    return (player.world.game.session as ArenaGameSession).characterStats_Mplayer[state.playerNumber].name;
                }
                if (ModManager.CoopAvailable && player.world.game.IsStorySession)
                {
                    return (player.world.game.session as StoryGameSession).characterStatsJollyplayer[state.playerNumber].name;
                }
                return player.world.game.session.characterStats.name;
            }
        }
    }
}
