﻿using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase.Assets;
using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using SlugBase.SaveData;

namespace SlugBase.Features
{
    using static PlayerFeatures;
    using static GameFeatures;

    internal static class FeatureHooks
    {
        public static void Apply()
        {
            On.ProcessManager.PreSwitchMainProcess += ProcessManager_PreSwitchMainProcess;
            IL.DreamsState.StaticEndOfCycleProgress += DreamsState_StaticEndOfCycleProgress;
            On.SlugcatStats.SlugcatTimelineOrder += SlugcatStats_SlugcatTimelineOrder;
            On.SlugcatStats.SlugcatCanMaul += SlugcatStats_SlugcatCanMaul;
            On.Player.CanMaulCreature += Player_CanMaulCreature;
            IL.Player.GrabUpdate += Player_GrabUpdate;
            On.Player.ctor += Player_ctor;
            IL.Menu.DreamScreen.Update += DreamScreen_Update;
            On.Menu.MenuScene.ctor += MenuScene_ctor;
            On.PlayerGraphics.DefaultBodyPartColorHex += PlayerGraphics_DefaultBodyPartColorHex;
            On.PlayerGraphics.ColoredBodyPartList += PlayerGraphics_ColoredBodyPartList;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            IL.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            
            On.Player.CanEatMeat += Player_CanEatMeat;
            IL.Player.EatMeatUpdate += Player_EatMeatUpdate;
            On.Player.ObjectEaten += Player_ObjectEaten;
            On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
            IL.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;
            On.CreatureCommunities.LoadDefaultCommunityAlignments += CreatureCommunities_LoadDefaultCommunityAlignments;
            On.CreatureCommunities.LikeOfPlayer += CreatureCommunities_LikeOfPlayer;
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;
            On.DeathPersistentSaveData.CanUseUnlockedGates += DeathPersistentSaveData_CanUseUnlockedGates;
            On.RainCycle.ctor += RainCycle_ctor;
            On.SlugcatStats.AutoGrabBatflys += SlugcatStats_AutoGrabBatflys;
            On.SlugcatStats.ctor += SlugcatStats_ctor;
            On.SlugcatStats.SlugcatStartingKarma += SlugcatStats_SlugcatStartingKarma;
            On.SaveState.ctor += SaveState_ctor;
            On.WorldLoader.GeneratePopulation += WorldLoader_GeneratePopulation;
            On.OverseerAbstractAI.SetAsPlayerGuide += OverseerAbstractAI_SetAsPlayerGuide;
            On.WorldLoader.OverseerSpawnConditions += WorldLoader_OverseerSpawnConditions;
            On.PlayerGraphics.DefaultSlugcatColor += PlayerGraphics_DefaultSlugcatColor;
            On.SaveState.GetStoryDenPosition += SaveState_GetStoryDenPosition;
            IL.Menu.IntroRoll.ctor += IntroRoll_ctor;

            SlugBaseCharacter.Refreshed += Refreshed;

            WorldHooks.Apply();
        }

        // Apply some changes immediately for fast iteration
        private static void Refreshed(object sender, SlugBaseCharacter.RefreshEventArgs args)
        {
            SlugBasePlugin.Logger.LogDebug($"Refreshed: {args.ID}");

            // Refresh graphics
            foreach (var rCam in args.Game.cameras)
            {
                foreach(var sLeaser in rCam.spriteLeasers)
                {
                    if (sLeaser.drawableObject is PlayerGraphics graphics
                        && graphics.player.SlugCatClass == args.ID)
                    {
                        graphics.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
                    }
                }
            }

            // Refresh arena mode stats
            if (ModManager.MSC && args.Game.IsArenaSession)
            {
                var stats = args.Game.GetArenaGameSession.characterStats_Mplayer;
                for(int i = 0; i < stats.Length; i++)
                {
                    if (stats[i].name == args.Character.Name)
                        stats[i] = new SlugcatStats(args.Character.Name, stats[i].malnourished);
                }
            }

            // Refresh coop stats
            if (ModManager.CoopAvailable && args.Game.IsStorySession)
            {
                var stats = args.Game.GetStorySession.characterStatsJollyplayer;
                for (int i = 0; i < stats.Length; i++)
                {
                    if (stats[i].name == args.Character.Name)
                        stats[i] = new SlugcatStats(args.Character.Name, stats[i].malnourished);
                }
            }

            // Refresh singleplayer stats
            if (args.Game.session.characterStats.name == args.Character.Name)
            {
                args.Game.session.characterStats = new SlugcatStats(args.Character.Name, args.Game.session.characterStats.malnourished);
            }
        }

        // WorldState: Reload custom timeline order on switch
        private static void ProcessManager_PreSwitchMainProcess(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            orig(self, ID);
            CustomTimeline.ReloadTimelineOrder();
        }

        // UseDefaultDreams: Remove default dreams
        private static void DreamsState_StaticEndOfCycleProgress(ILContext il)
        {
            var c = new ILCursor(il);

            if(c.TryGotoNext(MoveType.AfterLabel,
                x => x.MatchLdarg(0),
                x => x.MatchBrtrue(out _)))
            {
                var skipRet = c.DefineLabel();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((SaveState saveState) =>
                {
                    return saveState != null
                        && SlugBaseCharacter.TryGet(saveState.saveStateNumber, out var chara)
                        && UseDefaultDreams.TryGet(chara, out bool useDefaultDreams)
                        && !useDefaultDreams;
                });
                c.Emit(OpCodes.Brfalse, skipRet);
                c.Emit(OpCodes.Ret);
                c.MarkLabel(skipRet);
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(DreamsState_StaticEndOfCycleProgress)} failed!");
            }
        }

        // TimelineBefore, TimelineAfter: Apply timeline order
        private static LinkedList<SlugcatStats.Timeline> SlugcatStats_SlugcatTimelineOrder(On.SlugcatStats.orig_SlugcatTimelineOrder orig)
        {
            var order = orig();

            foreach (var timeline in CustomTimeline.OrderedTimelines)
            {
                if (!order.Contains(timeline.ID))
                {
                    if (timeline.InsertAfter?.FirstOrDefault(order.Contains) is SlugcatStats.Timeline afterTimeline)
                    {
                        order.AddAfter(order.Find(afterTimeline), timeline.ID);
                    }
                    else if (timeline.InsertBefore?.FirstOrDefault(order.Contains) is SlugcatStats.Timeline beforeTimeline)
                    {
                        order.AddBefore(order.Find(beforeTimeline), timeline.ID);
                    }
                }
            }

            return order;
        }

        // CanMaul: Allow mauling
        private static bool SlugcatStats_SlugcatCanMaul(On.SlugcatStats.orig_SlugcatCanMaul orig, SlugcatStats.Name name)
        {
            if (SlugBaseCharacter.TryGet(name, out var chara)
                && CanMaul.TryGet(chara, out bool canMaul))
            {
                return canMaul;
            }
            return orig(name);
        }

        // MaulBlacklist: Apply mauling blacklist
        public static bool Player_CanMaulCreature(On.Player.orig_CanMaulCreature orig, Player player, Creature crit)
        {
            if (MaulBlacklist.TryGet(player, out var blacklist))
            {
                var template = crit.Template;
                while(template != null)
                {
                    if (Array.IndexOf(blacklist, crit.Template.type) != -1)
                        return false;

                    template = template.ancestor;
                }
            }

            return orig(player, crit);
        }

        // MaulDamage: Change mauling damage, allow mauling without MSC enabled
        public static void Player_GrabUpdate(ILContext il)
        {
            // Change damage
            ILCursor c = new(il);
            if (c.TryGotoNext(x => x.MatchLdsfld<Creature.DamageType>(nameof(Creature.DamageType.Bite)))
                && c.TryGotoNext(x => x.MatchCallOrCallvirt<Creature>(nameof(Creature.Violence)))
                && c.TryGotoPrev(MoveType.After, x => x.MatchLdcR4(1f)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, Player, float>>((baseDamage, player) =>
                {
                    return MaulDamage.TryGet(player, out var newDamage) ? newDamage : baseDamage;
                });
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(Player_GrabUpdate)}, damage, failed!");
            }

            // Skip MSC check (Andrew approved)
            c.Index = 0;
            if (c.TryGotoNext(x => x.MatchCallOrCallvirt<SlugcatStats>(nameof(SlugcatStats.SlugcatCanMaul)))
                && c.TryGotoPrev(MoveType.After, x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(Player_GrabUpdate)}, remove MSC check, failed!");
            }
        }

        // BackSpear: Add back spear
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (BackSpear.TryGet(self, out var hasBackSpear) && hasBackSpear)
                self.spearOnBack ??= new Player.SpearOnBack(self);
        }

        // SleepScene: Fix dream scenes not changing off of slugcat
        private static void DreamScreen_Update(ILContext il)
        {
            var c = new ILCursor(il);

            var dreamsEqual = typeof(ExtEnum<MenuScene.SceneID>).GetMethod("op_Equality");

            if(c.TryGotoNext(
                    x => x.MatchLdcI4(35))
                && c.TryGotoPrev(
                    MoveType.After,
                    x => x.MatchLdsfld<MenuScene.SceneID>(nameof(MenuScene.SceneID.SleepScreen)),
                    x => x.MatchCallOrCallvirt(dreamsEqual)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((DreamScreen self) =>
                {
                    SlugcatStats.Name name;
                    if (self.manager.currentMainLoop is RainWorldGame game)
                        name = game.StoryCharacter;
                    else
                        name = self.manager.rainWorld.progression?.PlayingAsSlugcat;

                    return SlugBaseCharacter.TryGet(name, out var chara)
                        && SleepScene.TryGet(chara, out var sleepScene)
                        && sleepScene == self.scene.sceneID;
                });
                c.Emit(OpCodes.Or);
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(DreamScreen_Update)} failed!");
            }
        }

        // SleepScene, DeathScene, StarveScene: Replace scenes
        private static void MenuScene_ctor(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenuScene.SceneID sceneID)
        {
            SlugcatStats.Name name;
            if (menu.manager.currentMainLoop is RainWorldGame game)
                name = game.StoryCharacter;
            else
                name = menu.manager.rainWorld.progression?.PlayingAsSlugcat;

            if (name != null
                && SlugBaseCharacter.TryGet(name, out var chara)
                && (sceneID == MenuScene.SceneID.SleepScreen && SleepScene.TryGet(chara, out var newScene)
                    || sceneID == MenuScene.SceneID.NewDeath && DeathScene.TryGet(chara, out newScene)
                    || sceneID == MenuScene.SceneID.StarveScreen && StarveScene.TryGet(chara, out newScene))
                && newScene.Index != -1)
            {
                sceneID = newScene;
            }

            orig(self, menu, owner, sceneID);
        }

        // CustomColors: Set defaults for customization
        private static List<string> PlayerGraphics_DefaultBodyPartColorHex(On.PlayerGraphics.orig_DefaultBodyPartColorHex orig, SlugcatStats.Name slugcatID)
        {
            var list = orig(slugcatID);

            if (SlugBaseCharacter.TryGet(slugcatID, out var chara)
                && CustomColors.TryGet(chara, out var colorSlots))
            {
                list.Clear();
                list.AddRange(colorSlots.Select(slot => Custom.colorToHex(slot.Default)));
            }

            return list;
        }
        
        // CustomColors: Allow customization
        private static List<string> PlayerGraphics_ColoredBodyPartList(On.PlayerGraphics.orig_ColoredBodyPartList orig, SlugcatStats.Name slugcatID)
        {
            var list = orig(slugcatID);

            if(SlugBaseCharacter.TryGet(slugcatID, out var chara)
                && CustomColors.TryGet(chara, out var colorSlots))
            {
                list.Clear();
                list.AddRange(colorSlots.Select(slot => slot.Name));
            }

            return list;
        }

        // CustomColors: Apply color override
        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (sLeaser.sprites.Length > 9 && sLeaser.sprites[9] != null)
            {
                if (PlayerColor.Eyes.GetColor(self) is Color color)
                    sLeaser.sprites[9].color = color;
                else if (self.player.slugcatStats.name == SlugcatStats.Name.Night) // Fix Nightcat having black eyes outside arena
                    sLeaser.sprites[9].color = Color.Lerp(new Color(1f, 1f, 1f), rCam.currentPalette.blackColor, 0.3f);
            }
        }

        /// CustomColors: Apply body color override
        private static void PlayerGraphics_ApplyPalette(ILContext il)
        {
            var c = new ILCursor(il);

            // Body color
            if (c.TryGotoNext(MoveType.AfterLabel,
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(1),
                x => x.MatchCallOrCallvirt<GraphicsModule>(nameof(GraphicsModule.HypothermiaColorBlend))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_1);
                c.Emit(OpCodes.Ldarg_3);
                c.EmitDelegate<Func<PlayerGraphics, Color, RoomPalette, Color>>((self, color, palette) =>
                {
                    if (PlayerColor.Body.GetColor(self) is Color newColor)
                    {
                        Color starveColor = Color.Lerp(newColor, Color.gray, 0.4f);

                        float starveAmount = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
                        newColor = Color.Lerp(newColor, starveColor, starveAmount);

                        return newColor;
                    }
                    else
                    {
                        return color;
                    }
                });
                c.Emit(OpCodes.Stloc_1);
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(PlayerGraphics_ApplyPalette)} failed!");
            }
        }


        // Diet: Corpse edibility
        private static bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
        {
            if (SlugBaseCharacter.TryGet(self.SlugCatClass, out var chara)
                && Diet.TryGet(chara, out var diet))
            {
                return diet.GetMeatMultiplier(self, crit) > 0f
                    && (crit is not IPlayerEdible edible || !edible.Edible)
                    && crit.dead;
            }
            else
                return orig(self, crit);
        }

        // Diet: Multiplier for corpses
        private static void Player_EatMeatUpdate(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel foodAdded = null;

            // Match
            if (c.TryGotoNext(x => x.MatchLdarg(0),
                              x => x.MatchCallOrCallvirt<Player>(nameof(Player.AddQuarterFood)),
                              x => x.MatchBr(out foodAdded))
                && c.TryGotoNext(MoveType.AfterLabel,
                                 x => x.MatchLdarg(0),
                                 x => x.MatchLdcI4(1),
                                 x => x.MatchCallOrCallvirt<Player>(nameof(Player.AddFood))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<Player, int, bool>>((self, graspIndex) =>
                {
                    // Add rounded quarter pips from meat, intercepting vanilla AddFood
                    if (SlugBaseCharacter.TryGet(self.SlugCatClass, out var chara)
                        && Diet.TryGet(chara, out var diet)
                        && self.grasps[graspIndex].grabbed is Creature crit)
                    {
                        var mult = diet.GetMeatMultiplier(self, crit);

                        int quarterPips = Mathf.RoundToInt(mult * 4f);
                        for (; quarterPips >= 4; quarterPips -= 4)
                            self.AddFood(1);

                        for (; quarterPips >= 1; quarterPips -= 1)
                            self.AddQuarterFood();

                        return true;
                    }
                    else
                        return false;
                });
                c.Emit(OpCodes.Brtrue, foodAdded);
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(Player_EatMeatUpdate)} failed!");
            }
        }

        // Diet: Stun from negative nourishment
        private static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            if (SlugBaseCharacter.TryGet(self.SlugCatClass, out var chara)
                && Diet.TryGet(chara, out _)
                && SlugcatStats.NourishmentOfObjectEaten(self.SlugCatClass, edible) == -1)
            {
                (self.graphicsModule as PlayerGraphics)?.LookAtNothing();
                self.Stun(60);
            }
            else
            {
                orig(self, edible);
            }
        }

        // Diet: Multiplier for IPlayerEdibles
        private static int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
        {
            int n = orig(slugcatIndex, eatenobject);

            if (SlugBaseCharacter.TryGet(slugcatIndex, out var chara)
                && Diet.TryGet(chara, out var diet)
                && eatenobject is PhysicalObject obj)
            {
                float mul = diet.GetFoodMultiplier(obj);

                if (mul >= 0f)
                    n = Mathf.RoundToInt(n * mul);
                else
                    n = -1;
            }

            return n;
        }

        // SelectMenuScene, SelectMenuSceneAscended: Override scenes
        private static void SlugcatPage_AddImage(ILContext il)
        {
            var c = new ILCursor(il);

            if(c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(0)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate<Func<SlugcatSelectMenu.SlugcatPage, bool, MenuScene.SceneID, MenuScene.SceneID>>((self, ascended, sceneID) =>
                {
                    // Find scene ID
                    if(SlugBaseCharacter.TryGet(self.slugcatNumber, out var chara))
                    {
                        // Custom override
                        if (self.menu is SlugcatSelectMenu selectMenu
                            && selectMenu.saveGameData.TryGetValue(self.slugcatNumber, out var saveData)
                            && saveData != null
                            && MinedSaveData.Data.TryGetValue(saveData, out var minedData)
                            && minedData.SelectMenuScene?.Index > -1)
                        {
                            sceneID = minedData.SelectMenuScene;
                        }
                        
                        // Ascended
                        else if (ascended && SelectMenuSceneAscended.TryGet(chara, out var ascendedScene))
                        {
                            sceneID = ascendedScene;
                        }

                        // Normal
                        else if (SelectMenuScene.TryGet(chara, out var normalScene))
                        {
                            sceneID = normalScene;
                        }
                    }

                    // Override extra properties like mark position
                    if(CustomScene.Registry.TryGet(sceneID, out var customScene))
                    {
                        self.markOffset = customScene.MarkPos ?? self.markOffset;
                        self.glowOffset = customScene.GlowPos ?? self.glowOffset;
                        self.sceneOffset = customScene.SelectMenuOffset ?? self.sceneOffset;
                        self.slugcatDepth = customScene.SlugcatDepth ?? self.slugcatDepth;
                    }

                    return sceneID;
                });
                c.Emit(OpCodes.Stloc_0);
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(SlugcatPage_AddImage)} failed!");
            }
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
                            int community = (int)pair.Key - 1;

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
            var players = self.session?.Players;

            if(players != null
                && playerNumber >= 0 && playerNumber < players.Count
                && players[playerNumber]?.realizedObject is Player ply
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
            bool hasMax = CycleLengthMax.TryGet(world.game, out float maxLen);
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
                    return values.Length > 1 ? values[1] : starveDefault;
                else
                    return values[0];
            }

            if(SlugBaseCharacter.TryGet(slugcat, out var chara))
            {
                if (WeightMul.TryGet(chara, out var weight))
                    self.bodyWeightFac = ApplyStarve(weight, Mathf.Min(weight[0], 0.9f));

                if (TunnelSpeedMul.TryGet(chara, out var tunnelSpeed))
                    self.corridorClimbSpeedFac = ApplyStarve(tunnelSpeed, 0.86f);

                if (ClimbSpeedMul.TryGet(chara, out var climbSpeed))
                    self.poleClimbSpeedFac = ApplyStarve(climbSpeed, 0.8f);

                if (WalkSpeedMul.TryGet(chara, out var walkSpeed))
                    self.runspeedFac = ApplyStarve(walkSpeed, 0.875f);

                if (CrouchStealth.TryGet(chara, out var crouchStealth))
                    self.visualStealthInSneakMode = ApplyStarve(crouchStealth, crouchStealth[0]);

                if (ThrowSkill.TryGet(chara, out var throwSkill))
                    self.throwingSkill = ApplyStarve(throwSkill, 0);

                if (LungsCapacityMul.TryGet(chara, out var lungCapacity))
                    self.lungsFac = 1f / ApplyStarve(lungCapacity, lungCapacity[0]);

                if (LoudnessMul.TryGet(chara, out var loudness))
                    self.loudnessFac = ApplyStarve(loudness, loudness[0]);
            }
        }

        // KarmaCap: Fix cap resetting on echo encounter
        private static int SlugcatStats_SlugcatStartingKarma(On.SlugcatStats.orig_SlugcatStartingKarma orig, SlugcatStats.Name slugcatNum)
        {
            if (SlugBaseCharacter.TryGet(slugcatNum, out var chara)
                && KarmaCap.TryGet(chara, out int karmaCap))
            {
                return karmaCap;
            }
            else
            {
                return orig(slugcatNum);
            }
        }

        // HasDreams: Add dreams
        // Karma, KarmaCap: Change initial values
        // TheMark, TheGlow: Change starting state
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

                if (TheMark.TryGet(chara, out bool hasMark) && hasMark)
                    self.deathPersistentSaveData.theMark = true;

                if (TheGlow.TryGet(chara, out bool hasGlow) && hasGlow)
                    self.theGlow = true;
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
            if (SlugBaseCharacter.TryGet(self.game?.StoryCharacter ?? character, out var chara) && !GuideOverseer.TryGet(chara, out _))
                return false;
            else
                return orig(self, character);
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

        // StartRoom: Set initial den
        private static string SaveState_GetStoryDenPosition(On.SaveState.orig_GetStoryDenPosition orig, SlugcatStats.Name slugcat, out bool isVanilla)
        {
            if (SlugBaseCharacter.TryGet(slugcat, out var chara)
                && chara.Features.TryGet(StartRoom, out string[] dens))
            {
                // Search through dens until a valid one is found
                foreach (var den in dens)
                {
                    if (WorldLoader.FindRoomFile(den, false, ".txt") != null)
                    {
                        // Adapted from SaveState.TrySetVanillaDen
                        string root = Custom.RootFolderDirectory();
                        string regionName = "";
                        if (den.Contains("_"))
                        {
                            regionName = den.Split('_')[0];
                        }

                        isVanilla = File.Exists(Path.Combine(root, "World", regionName + "-Rooms", den + ".txt"))
                                 || File.Exists(Path.Combine(root, "World", "Gate Shelters", den + ".txt"));

                        return den;
                    }
                }
            }

            return orig(slugcat, out isVanilla);
        }

        // TitleCard: Add to pool
        private static void IntroRoll_ctor(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel introRollCAdded = null;

            // Move right before the DLC check for Watcher
            // Then, find where the Watcher intro roll branches to
            if (c.TryGotoNext(
                MoveType.AfterLabel,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.Watcher)),
                x => x.MatchBrfalse(out _))
                && c.Clone().TryGotoNext(x => x.MatchBr(out introRollCAdded)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((IntroRoll self) =>
                {
                    SlugcatStats.Name current = self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;
                    if (SlugBaseCharacter.TryGet(current, out var chara) && TitleCard.TryGet(chara, out string path))
                    {
                        self.illustrations[2] = new MenuIllustration(self, self.pages[0], "", path, new Vector2(0f, 0f), true, false);
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, introRollCAdded);
            }
        }
    }
}
