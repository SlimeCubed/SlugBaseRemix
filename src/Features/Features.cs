using UnityEngine;
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

        /// <summary>"cycle_length_min": Minimum cycle length in minutes.</summary>
        public static readonly GameFeature<float> CycleLengthMin = GameFloat("cycle_length_min");

        /// <summary>"cycle_length_max": Maximum cycle length in minutes.</summary>
        public static readonly GameFeature<float> CycleLengthMax = GameFloat("cycle_length_max");

        /// <summary>"perma_unlock_gates": Maximum cycle length in minutes.</summary>
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

        /// <summary>"world_state": The character to use for creature spawns and room connections.</summary>
        public static readonly GameFeature<SlugcatStats.Name[]> WorldState = new("world_state", json =>
        {
            if (json.TryList() is JsonList list)
            {
                return list.Select(value => Utils.GetName(value.AsString())).ToArray();
            }
            else
            {
                return new SlugcatStats.Name[] {
                    Utils.GetName(json.AsString())
                };
            }
        });
    }
}