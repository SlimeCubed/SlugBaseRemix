﻿using UnityEngine;
using SlugBase.DataTypes;
using System.Collections.Generic;
using Menu;
using System.Linq;

namespace SlugBase.Features
{
    using static FeatureTypes;

    /// <summary>
    /// Built-in <see cref="Feature"/>s describing the player.
    /// </summary>
    public static class PlayerFeatures
    {
        /// <summary>"color": Player body and UI color.</summary>
        public static readonly PlayerFeature<Color> SlugcatColor = PlayerColor("color");

        /// <summary>"auto_grab_batflies": Grab batflies on collision.</summary>
        public static readonly PlayerFeature<bool> AutoGrabFlies = PlayerBool("auto_grab_batflies");

        /// <summary>"weight": Weight multiplier.</summary>
        public static readonly PlayerFeature<float[]> WeightMul = PlayerFloats("weight", 1, 2);

        /// <summary>"tunnel_speed": Move speed in tunnels.</summary>
        public static readonly PlayerFeature<float[]> TunnelSpeedMul = PlayerFloats("tunnel_speed", 1, 2);

        /// <summary>"climb_speed": Move speed on poles.</summary>
        public static readonly PlayerFeature<float[]> ClimbSpeedMul = PlayerFloats("climb_speed", 1, 2);

        /// <summary>"walk_speed": Standing move speed.</summary>
        public static readonly PlayerFeature<float[]> WalkSpeedMul = PlayerFloats("walk_speed", 1, 2);

        /// <summary>"crouch_stealth": Visual stealth while crouched.</summary>
        public static readonly PlayerFeature<float[]> CrouchStealth = PlayerFloats("crouch_stealth", 1, 2);

        /// <summary>"throw_skill": Spear damage and speed.</summary>
        public static readonly PlayerFeature<int[]> ThrowSkill = PlayerInts("throw_skill", 1, 2);

        /// <summary>"lung_capacity": Time underwater before drowning.</summary>
        public static readonly PlayerFeature<float[]> LungsCapacityMul = PlayerFloats("lung_capacity", 1, 2);

        /// <summary>"loudness": Sound alert multiplier.</summary>
        public static readonly PlayerFeature<float[]> LoudnessMul = PlayerFloats("loudness", 1, 2);

        /// <summary>"back_spear": Store a spear on back.</summary>
        public static readonly PlayerFeature<bool> BackSpear = PlayerBool("back_spear");
        
        /// <summary>"alignments": Initial community reputation.</summary>
        public static readonly PlayerFeature<Dictionary<CreatureCommunities.CommunityID, RepOverride>> CommunityAlignments = new("alignments", json =>
        {
            var obj = json.AsObject();
            var reps = new Dictionary<CreatureCommunities.CommunityID, RepOverride>();
            foreach (var pair in obj)
            {
                var community = new CreatureCommunities.CommunityID(Utils.MatchCaseInsensitiveEnum<CreatureCommunities.CommunityID>(pair.Key));
                reps[community] = new(pair.Value);
            }
            return reps;
        });

        /// <summary>"diet": Edibility and nourishment of foods.</summary>
        public static readonly PlayerFeature<Diet> Diet = new("diet", json => new Diet(json));

        /// <summary>"custom_colors": Configurable player colors.</summary>
        public static readonly PlayerFeature<ColorSlot[]> CustomColors = new("custom_colors", json =>
        {
            var list = json.AsList();
            var colors = new ColorSlot[list.Count];

            for (int i = 0; i < colors.Length; i++)
                colors[i] = new ColorSlot(i, list[i]);

            if (colors.Length < 1 || colors[0].Name != "Body") throw new JsonException("Expected \"Body\" as first custom color!", list);
            if (colors.Length < 2 || colors[1].Name != "Eyes") throw new JsonException("Expected \"Eyes\" as second custom color!", list);

            return colors;
        });

        /// <summary>"can_maul": Ability to maul creatures.</summary>
        public static readonly PlayerFeature<bool> CanMaul = PlayerBool("can_maul");

        /// <summary>"maul_damage": Damage of maul attack.</summary>
        public static readonly PlayerFeature<float> MaulDamage = PlayerFloat("maul_damage");

        /// <summary>"maul_blacklist": Creatures that cannot be mauled.</summary>
        public static readonly PlayerFeature<CreatureTemplate.Type[]> MaulBlacklist = new("maul_blacklist", json =>
        {
            var list = json.AsList();
            return list.Select(JsonUtils.ToExtEnum<CreatureTemplate.Type>).ToArray();
        });
    }

    /// <summary>
    /// Built-in <see cref="Feature"/>s describing the game.
    /// </summary>
    public static class GameFeatures
    {
        /// <summary>"karma": Initial karma.</summary>
        public static readonly GameFeature<int> Karma = GameInt("karma");

        /// <summary>"karma_cap": Initial karma cap.</summary>
        public static readonly GameFeature<int> KarmaCap = GameInt("karma_cap");

        /// <summary>"the_mark": Start with mark of communication.</summary>
        public static readonly GameFeature<bool> TheMark = GameBool("the_mark");

        /// <summary>"the_glow": Start glowing.</summary>
        public static readonly GameFeature<bool> TheGlow = GameBool("the_glow");

        /// <summary>"start_room": Initial room, plus backups from highest to lowest priority.</summary>
        public static readonly GameFeature<string[]> StartRoom = GameStrings("start_room", 1);

        /// <summary>"guide_overseer": Player guide overseer color index.</summary>
        public static readonly GameFeature<int> GuideOverseer = GameInt("guide_overseer");

        /// <summary>"has_dreams": Whether or not to track dream state.</summary>
        public static readonly GameFeature<bool> HasDreams = GameBool("has_dreams");

        /// <summary>"use_default_dreams": Whether or not to show Survivor's dreams.</summary>
        public static readonly GameFeature<bool> UseDefaultDreams = GameBool("use_default_dreams");

        /// <summary>"cycle_length_min": Minimum cycle length in minutes.</summary>
        public static readonly GameFeature<float> CycleLengthMin = GameFloat("cycle_length_min");

        /// <summary>"cycle_length_max": Maximum cycle length in minutes.</summary>
        public static readonly GameFeature<float> CycleLengthMax = GameFloat("cycle_length_max");

        /// <summary>"perma_unlock_gates": Permanently unlock gates once used.</summary>
        public static readonly GameFeature<bool> PermaUnlockGates = GameBool("perma_unlock_gates");

        /// <summary>"food_min": Food needed to sleep.</summary>
        public static readonly GameFeature<int> FoodMin = GameInt("food_min");

        /// <summary>"food_max": Maximum food stored during a cycle.</summary>
        public static readonly GameFeature<int> FoodMax = GameInt("food_max");

        /// <summary>"select_menu_scene": The scene for this slugcat on the select menu.</summary>
        public static readonly GameFeature<MenuScene.SceneID> SelectMenuScene = GameExtEnum<MenuScene.SceneID>("select_menu_scene");

        /// <summary>"select_menu_scene_ascended": The scene for this slugcat on the select menu when ascended.</summary>
        public static readonly GameFeature<MenuScene.SceneID> SelectMenuSceneAscended = GameExtEnum<MenuScene.SceneID>("select_menu_scene_ascended");

        /// <summary>"sleep_scene": The scene for this slugcat when hibernating.</summary>
        public static readonly GameFeature<MenuScene.SceneID> SleepScene = GameExtEnum<MenuScene.SceneID>("sleep_scene");

        /// <summary>"starve_scene": The scene for this slugcat when losing from starvation.</summary>
        public static readonly GameFeature<MenuScene.SceneID> StarveScene = GameExtEnum<MenuScene.SceneID>("starve_scene");

        /// <summary>"death_scene": The scene for this slugcat when losing from a non-starvation death.</summary>
        public static readonly GameFeature<MenuScene.SceneID> DeathScene = GameExtEnum<MenuScene.SceneID>("death_scene");

        /// <summary>"intro_scene": Add an intro scene in the style of Survivor or Gourmand.</summary>
        public static readonly GameFeature<SlideShow.SlideShowID> IntroScene = GameExtEnum<SlideShow.SlideShowID>("intro_slideshow");

        /// <summary>"outro_scene": Add an outro scene for a slugbase slugcat. </summary>
        public static readonly GameFeature<SlideShow.SlideShowID> OutroScene = GameExtEnum<SlideShow.SlideShowID>("outro_slideshow");
        // This ideally should be able to support an arbitrary amount of outros for alternate endings. Personally I'd also be fine with only allowing two, one being `outro_scene_alt`, seems like a reasonable compromise

        /// <summary>"world_state": The timeline to use for creature spawns and room connections.</summary>
        public static readonly GameFeature<SlugcatStats.Timeline[]> WorldState = GameExtEnums<SlugcatStats.Timeline>("world_state");

        /// <summary>"story_regions": The new or removed story regions from an inherited world_state.</summary>
        public static readonly GameFeature<string[]> StoryRegions = GameStrings("story_regions");

        /// <summary>"timeline_before": The next timeline after this character's.</summary>
        public static readonly GameFeature<SlugcatStats.Name[]> TimelineBefore = GameSlugcatNames("timeline_before", 1);

        /// <summary>"timeline_after": The previous timeline before this character's.</summary>
        public static readonly GameFeature<SlugcatStats.Name[]> TimelineAfter = GameSlugcatNames("timeline_after", 1);

        /// <summary>"title_card": Add a intro titlecard to be randomly selected. Must be 1366x768 pixels big to display correctly.</summary>
        public static readonly GameFeature<string> TitleCard = GameString("title_card");

        /// <summary>"expedition_enabled": Enable Expedition mode for this character.</summary>
        public static readonly GameFeature<bool> ExpeditionEnabled = GameBool("expedition_enabled");

        /// <summary>"expedition_description": Character description in Expedition mode.</summary>
        public static readonly GameFeature<string> ExpeditionDescription = GameString("expedition_description");
    }
}