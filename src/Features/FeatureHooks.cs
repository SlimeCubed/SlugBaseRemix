using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SlugBase.Features
{
    using static PlayerFeatures;
    using static GameFeatures;

    internal static class FeatureHooks
    {
        public static void Apply()
        {
            On.CreatureCommunities.LoadDefaultCommunityAlignments += CreatureCommunities_LoadDefaultCommunityAlignments;
            On.CreatureCommunities.LikeOfPlayer += CreatureCommunities_LikeOfPlayer;
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;
            On.DeathPersistentSaveData.CanUseUnlockedGates += DeathPersistentSaveData_CanUseUnlockedGates;
            On.RainCycle.ctor += RainCycle_ctor;
            On.SlugcatStats.AutoGrabBatflys += SlugcatStats_AutoGrabBatflys;
            On.SlugcatStats.ctor += SlugcatStats_ctor;
            On.SaveState.ctor += SaveState_ctor;
            On.WorldLoader.GeneratePopulation += WorldLoader_GeneratePopulation;
            On.OverseerAbstractAI.SetAsPlayerGuide += OverseerAbstractAI_SetAsPlayerGuide;
            On.WorldLoader.OverseerSpawnConditions += WorldLoader_OverseerSpawnConditions;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            IL.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DefaultSlugcatColor += PlayerGraphics_DefaultSlugcatColor;
            On.SaveState.setDenPosition += SaveState_setDenPosition;
        }

        // CommunityAlignments: Set initial reputation
        private static void CreatureCommunities_LoadDefaultCommunityAlignments(On.CreatureCommunities.orig_LoadDefaultCommunityAlignments orig, CreatureCommunities self, SlugcatStats.Name saveStateNumber)
        {
            orig(self, saveStateNumber);

            if(SlugBaseCharacter.TryGet(saveStateNumber, out var chara)
                && CommunityAlignments.TryGet(chara, out var reps))
            {
                foreach (var pair in reps)
                {
                    for (int region = 0; region < self.playerOpinions.GetLength(1); region++)
                    {
                        for (int player = 0; player < self.playerOpinions.GetLength(2); player++)
                        {
                            int community = (int)pair.Key;

                            if (community >= 0 && community < self.playerOpinions.GetLength(0))
                                self.playerOpinions[community, region, player] = Mathf.Lerp(self.playerOpinions[community, region, player], pair.Value.Target, pair.Value.Strength);
                        }
                    }
                }
            }
        }

        // CommunityAlignments: Lock reputation
        private static float CreatureCommunities_LikeOfPlayer(On.CreatureCommunities.orig_LikeOfPlayer orig, CreatureCommunities self, CreatureCommunities.CommunityID commID, int region, int playerNumber)
        {
            var players = self.session.Players;

            if(playerNumber >= 0 && playerNumber < players.Count
                && players[playerNumber].realizedObject is Player ply
                && CommunityAlignments.TryGet(ply, out var reps)
                && reps.TryGetValue(commID, out var repOverride)
                && repOverride.Locked)
            {
                return repOverride.Target;
            }

            return orig(self, commID, region, playerNumber);
        }

        // FoodMin, FoodMax: Change required and max food
        private static IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
        {
            IntVector2 meter = orig(slugcat);

            if(SlugBaseCharacter.TryGet(slugcat, out var chara))
            {
                if (FoodMin.TryGet(chara, out int min))
                    meter.y = min;

                if (FoodMax.TryGet(chara, out int max))
                    meter.x = max;
            }

            return meter;
        }

        // PermaUnlockGates: Unlock gates when opened
        private static bool DeathPersistentSaveData_CanUseUnlockedGates(On.DeathPersistentSaveData.orig_CanUseUnlockedGates orig, DeathPersistentSaveData self, SlugcatStats.Name slugcat)
        {
            if (SlugBaseCharacter.TryGet(slugcat, out var chara)
                && PermaUnlockGates.TryGet(chara, out bool val))
                return val;

            return orig(self, slugcat);
        }

        // CycleLengthMin, CycleLengthMax: Change cycle length
        private static void RainCycle_ctor(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
        {
            bool hasMin = CycleLengthMin.TryGet(world.game, out float minLen);
            bool hasMax = CycleLengthMin.TryGet(world.game, out float maxLen);
            if (hasMin || hasMax)
            {
                if (!hasMin) minLen = world.game.setupValues.cycleTimeMin / 60f;
                if (!hasMax) maxLen = world.game.setupValues.cycleTimeMax / 60f;

                minutes = Mathf.Lerp(minLen, maxLen, Random.value);
            }

            orig(self, world, minutes);
        }

        // AutoGrabFlies: Change grab behavior
        private static bool SlugcatStats_AutoGrabBatflys(On.SlugcatStats.orig_AutoGrabBatflys orig, SlugcatStats.Name slugcatNum)
        {
            if (SlugBaseCharacter.TryGet(slugcatNum, out var chara)
                && AutoGrabFlies.TryGet(chara, out bool val))
                return val;

            return orig(slugcatNum);
        }

        // WeightMul, ThrowSkill, WalkSpeedMul, ClimbSpeedMul, TunnelSpeedMul: Apply stats
        private static void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
        {
            orig(self, slugcat, malnourished);

            T ApplyStarve<T>(T[] values, T starveDefault)
            {
                if (malnourished)
                    return values.Length > 1 ? values[0] : starveDefault;
                else
                    return values[0];
            }

            if(SlugBaseCharacter.TryGet(slugcat, out var chara))
            {
                if (WeightMul.TryGet(chara, out var weight))
                    self.bodyWeightFac = ApplyStarve(weight, Mathf.Min(weight[0], 0.9f));

                if (TunnelSpeedMul.TryGet(chara, out var tunnelSpeed))
                    self.bodyWeightFac = ApplyStarve(tunnelSpeed, 0.86f);

                if (ClimbSpeedMul.TryGet(chara, out var climbSpeed))
                    self.bodyWeightFac = ApplyStarve(climbSpeed, 0.8f);

                if (WalkSpeedMul.TryGet(chara, out var walkSpeed))
                    self.runspeedFac = ApplyStarve(walkSpeed, 0.875f);

                if (CrouchStealth.TryGet(chara, out var crouchStealth))
                    self.visualStealthInSneakMode = ApplyStarve(crouchStealth, crouchStealth[0]);

                if (ThrowSkill.TryGet(chara, out var throwSkill))
                    self.bodyWeightFac = ApplyStarve(throwSkill, 0);

                if (LungsCapacityMul.TryGet(chara, out var lungCapacity))
                    self.lungsFac = ApplyStarve(lungCapacity, lungCapacity[0]);

                if (LoudnessMul.TryGet(chara, out var loudness))
                    self.loudnessFac = ApplyStarve(loudness, loudness[0]);
            }
        }

        // HasDreams: Add dreams
        // Karma, KarmaCap: Change initial values
        private static void SaveState_ctor(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig(self, saveStateNumber, progression);


            if (SlugBaseCharacter.TryGet(saveStateNumber, out var chara))
            {
                if (self.dreamsState == null
                    && HasDreams.TryGet(chara, out bool dreams)
                    && dreams)
                {
                    self.dreamsState = new DreamsState();
                }

                if (KarmaCap.TryGet(chara, out int initKarmaCap))
                    self.deathPersistentSaveData.karmaCap = initKarmaCap;

                if (Karma.TryGet(chara, out int initKarma))
                    self.deathPersistentSaveData.karma = initKarma;
            }
        }

        // GuideOverseer: Restrict hook to SetAsPlayerGuide to single call in GeneratePopulation
        private static bool _generatingPopulation;
        private static void WorldLoader_GeneratePopulation(On.WorldLoader.orig_GeneratePopulation orig, WorldLoader self, bool fresh)
        {
            try
            {
                _generatingPopulation = true;
                orig(self, fresh);
            }
            finally
            {
                _generatingPopulation = false;
            }
        }

        // GuideOverseer: Set color
        private static void OverseerAbstractAI_SetAsPlayerGuide(On.OverseerAbstractAI.orig_SetAsPlayerGuide orig, OverseerAbstractAI self, int ownerOverride)
        {
            if (_generatingPopulation && GuideOverseer.TryGet(self.world.game, out int guide))
                ownerOverride = guide;

            orig(self, ownerOverride);
        }

        // GuideOverseer: Remove when not present
        private static bool WorldLoader_OverseerSpawnConditions(On.WorldLoader.orig_OverseerSpawnConditions orig, WorldLoader self, SlugcatStats.Name character)
        {
            if (SlugBaseCharacter.TryGet(character, out var chara) && GuideOverseer.TryGet(chara, out _))
                return false;
            else
                return orig(self, character);
        }

        // EyeColor: Apply color override
        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (sLeaser.sprites.Length > 9 && sLeaser.sprites[9] != null
                && SlugBaseCharacter.TryGet(self.CharacterForColor, out var chara)
                && EyeColor.TryGet(chara, out var newPalColor))
            {
                sLeaser.sprites[9].color = newPalColor.GetColor(rCam.currentPalette);
            }
        }

        /// BodyColor: Apply color override
        private static void PlayerGraphics_ApplyPalette(ILContext il)
        {
            var c = new ILCursor(il);

            // Body color
            if(c.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(1),
                x => x.MatchCall<GraphicsModule>(nameof(GraphicsModule.HypothermiaColorBlend))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_1);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate<Func<PlayerGraphics, Color, RoomPalette, Color>>((self, color, palette) =>
                {
                    if(SlugBaseCharacter.TryGet(self.CharacterForColor, out var chara)
                        && BodyColor.TryGet(chara, out var newPalColor))
                    {
                        Color newColor = newPalColor.GetColor(palette);
                        Color starveColor = Color.Lerp(newColor, Color.gray, 0.4f);

                        if (BodyColorStarved.TryGet(chara, out var starvePalColor))
                            starveColor = starvePalColor.GetColor(palette);

                        float starveAmount = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
                        newColor = Color.Lerp(newColor, starveColor, starveAmount);

                        return newColor;
                    }
                    else
                    {
                        return color;
                    }
                });
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(PlayerGraphics_ApplyPalette)} failed!");
            }
        }

        // Color: Set color
        private static Color PlayerGraphics_DefaultSlugcatColor(On.PlayerGraphics.orig_DefaultSlugcatColor orig, SlugcatStats.Name i)
        {
            if (SlugBaseCharacter.TryGet(i, out var chara)
                && SlugcatColor.TryGet(chara, out var color))
            {
                return color;
            }
            else
            {
                return orig(i);
            }
        }

        // Den: Set initial den
        private static void SaveState_setDenPosition(On.SaveState.orig_setDenPosition orig, SaveState self)
        {
            orig(self);

            if(SlugBaseCharacter.TryGet(self.saveStateNumber, out var chara)
                && chara.Features.TryGet(Den, out string[] dens))
            {
                // Search through dens until a valid one is found
                foreach(var den in dens)
                {
                    if (WorldLoader.FindRoomFile(den, false, ".txt") != null)
                    {
                        self.denPosition = den;
                        break;
                    }
                }
            }
        }
    }
}
