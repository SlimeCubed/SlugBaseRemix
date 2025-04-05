using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Timeline = SlugcatStats.Timeline;

namespace SlugBase.Features
{
    using static GameFeatures;

    internal static class WorldHooks
    {
        /// <summary>Path.DirectorySeparatorChar but shorter, because it takes too much space</summary>
        public static char Separator => Path.DirectorySeparatorChar;
        public static void Apply()
        {
            //map_XX hooks
            IL.HUD.Map.LoadConnectionPositions += InheritMapFile_Map;
            IL.HUD.Map.Update += InheritMapFile_Map;
            IL.World.LoadWorldForFastTravel_Timeline_List1_Int32Array_Int32Array_Int32Array += InheritMapFile_World;
            IL.World.LoadMapConfig_Timeline += InheritMapFile_World;

            //properties.txt hooks
            IL.World.LoadMapConfig_Timeline += PropertiesConfig;
            On.Region.ctor_string_int_int_Timeline += Region_ctor;

            //meta hooks
            On.SlugcatStats.SlugcatToTimeline += SlugcatStats_SlugcatToTimeline;
            On.SlugcatStats.SlugcatOptionalRegions += SlugcatStats_SlugcatOptionalRegions;
            On.SlugcatStats.SlugcatStoryRegions += SlugcatStats_SlugcatStoryRegions;
            On.Region.GetProperRegionAcronym_Timeline_string += Region_GetProperRegionAcronym;

            //worldloader
            IL.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues += WorldLoader_ctor_ILHook;
            On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues += WorldLoader_ctor;
            On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;

            //misc
            On.Region.GetRegionFullName += Region_GetRegionFullName;
            On.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame += RoomSettings_ctor;
            On.AbstractCreature.setCustomFlags += AbstractCreature_setCustomFlags;

            On.PlacedObject.FilterData.RefreshTimelineList += FilterData_RefreshTimelineList;

            //this property is only used to be passed into WorldLoader.ctor
            //new Hook( typeof(OverWorld).GetProperty(nameof(OverWorld.PlayerCharacterNumber)).GetGetMethod(),OverWorld_get_PlayerCharacterNumber );
            //On.Region.LoadAllRegions += Region_LoadAllRegions; //nah, we want the hook to be individualized
        }

        #region devtools
        private static void FilterData_RefreshTimelineList(On.PlacedObject.FilterData.orig_RefreshTimelineList orig, PlacedObject.FilterData self)
        {
            orig(self);

            // Except for weird edge cases regarding timelines with no slugcats, filters are saved as blacklists
            // If a region was made without a slugcat in mind, all filters will be active
            foreach (var customTimeline in CustomTimeline.Registry.Values)
            {
                // So, if a custom timeline or any of its parents are backlisted, blacklist it
                if (self.availableOnTimelines.Contains(customTimeline.ID)
                    && !customTimeline.Priorities.All(self.availableOnTimelines.Contains))
                {
                    self.availableOnTimelines.Remove(customTimeline.ID);
                }
            }
        }
        #endregion

        #region Map & Properties

        // Custom timelines: Fix map connections
        private static void InheritMapFile_Map(ILContext il)
        {
            var c = new ILCursor(il);

            try
            {
                // Sneak in right after resolving the slugcat-specific map file path
                c.GotoNext(x => x.MatchLdstr("map_"));
                c.GotoNext(MoveType.After, x => x.MatchCall<AssetManager>(nameof(AssetManager.ResolveFilePath)));

                // Modify the result on the stack
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((string origPath, HUD.Map map) =>
                {
                    var baseTimeline = SlugcatStats.SlugcatToTimeline(map.hud.rainWorld.progression.PlayingAsSlugcat);
                    if (CustomTimeline.Registry.TryGet(baseTimeline, out var customTimeline))
                    {
                        // Search through inherited timelines for map file variations
                        string prefix = "World" + Separator + map.RegionName + Separator + "map_" + map.UseMapName() + "-";
                        string suffix = ".png";
                        return FindWorldFileOverride(customTimeline, prefix, suffix) ?? origPath;
                    }
                    return origPath;
                });
            }
            catch (Exception e)
            {
                SlugBasePlugin.Logger.LogError("Failed to hook InheritMapFile_Map!");
                SlugBasePlugin.Logger.LogError(e);
            }
        }

        private static void InheritMapFile_World(ILContext il)
        {
            var c = new ILCursor(il);

            try
            {
                // Sneak in right after resolving the slugcat-specific map file path
                c.GotoNext(x => x.MatchLdstr("-"));
                c.GotoNext(MoveType.After, x => x.MatchCall<AssetManager>(nameof(AssetManager.ResolveFilePath)));

                // Modify the result on the stack
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((string origPath, World world, SlugcatStats.Timeline baseTimeline) =>
                {
                    if (CustomTimeline.Registry.TryGet(baseTimeline, out var customTimeline))
                    {
                        // Search through inherited timelines for map file variations
                        string prefix = "World" + Separator + world.name + Separator + "map_" + WorldLoader.MapNameManipulator(world.name, world.game) + "-";
                        string suffix = ".txt";
                        return FindWorldFileOverride(customTimeline, prefix, suffix) ?? origPath;
                    }
                    return origPath;
                });
            }
            catch (Exception e)
            {
                SlugBasePlugin.Logger.LogError("Failed to hook InheritMapFile_World!");
                SlugBasePlugin.Logger.LogError(e);
            }
        }

        // Apply timeline inheritance to region properties
        private static void PropertiesConfig(ILContext il)
        {
            var c = new ILCursor(il);

            try
            {
                // Region properties
                c.GotoNext(x => x.MatchLdstr("Properties-"));
                c.GotoNext(MoveType.After, x => x.MatchCall<AssetManager>(nameof(AssetManager.ResolveFilePath)));

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((string origPath, World world, Timeline baseTimeline) =>
                {
                    if (CustomTimeline.Registry.TryGet(baseTimeline, out var customTimeline))
                    {
                        string prefix = $"World{Separator}{world.name}{Separator}Properties-";
                        string suffix = ".txt";
                        return FindWorldFileOverride(customTimeline, prefix, suffix) ?? origPath;
                    }

                    return origPath;
                });

                // Broken shelters
                // Go to the condition after testing against "Broken Shelters"
                c.GotoNext(x => x.MatchLdstr("Broken Shelters"));
                c.GotoNext(x => x.MatchBrfalse(out _));
                c.GotoNext(x => x.MatchBrfalse(out _));

                // Break the shelter if any inherited timelines are broken
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldloc, 14);
                c.EmitDelegate((bool result, Timeline timeline, string[] parts) =>
                {
                    if (!result && CustomTimeline.Registry.TryGet(timeline, out var customTimeline))
                    {
                        var brokenTimeline = new Timeline(parts[1].Trim());
                        result = customTimeline.Priorities.Contains(brokenTimeline);
                    }

                    return result;
                });
            }
            catch (Exception e)
            {
                SlugBasePlugin.Logger.LogError("Failed to hook PropertiesConfig!");
                SlugBasePlugin.Logger.LogError(e);
            }
        }

        private static void Region_ctor(On.Region.orig_ctor_string_int_int_Timeline orig, Region self, string name, int firstRoomIndex, int regionNumber, SlugcatStats.Timeline timeline)
        {
            if (CustomTimeline.Registry.TryGet(timeline, out var customTimeline))
            {
                foreach (var inheritFrom in customTimeline.Priorities)
                {
                    string path = AssetManager.ResolveFilePath($"World{Separator}{name}{Separator}properties-{inheritFrom.value}.txt");
                    if (File.Exists(path)) { timeline = inheritFrom; break; }
                }
            }

            orig(self, name, firstRoomIndex, regionNumber, timeline);
        }

        private static string FindWorldFileOverride(CustomTimeline customTimeline, string prefix, string suffix)
        {
            foreach (var timeline in customTimeline.Priorities)
            {
                // If found, replace the original path
                string testPath = AssetManager.ResolveFilePath(prefix + timeline.value + suffix);
                if (File.Exists(testPath))
                    return testPath;
            }

            return null;
        }
        #endregion

        #region meta
        // Custom timelines: Set starting timeline
        private static Timeline SlugcatStats_SlugcatToTimeline(On.SlugcatStats.orig_SlugcatToTimeline orig, SlugcatStats.Name slugcat)
        {
            if (SlugBaseCharacter.TryGet(slugcat, out var chara) && WorldState.TryGet(chara, out var timelines))
                return Utils.FirstValidEnum(timelines);
            else
                return orig(slugcat);
        }

        // Custom timelines: Mark regions as optional for collections and safari
        private static List<string> SlugcatStats_SlugcatOptionalRegions(On.SlugcatStats.orig_SlugcatOptionalRegions orig, SlugcatStats.Name i)
        {
            if (!SlugBaseCharacter.TryGet(i, out _)) return orig(i);

            // Get all timelines this character might use
            var baseTimeline = SlugcatStats.SlugcatToTimeline(i);
            List<Timeline> timelines;
            if (CustomTimeline.Registry.TryGet(baseTimeline, out var customTimeline))
                timelines = customTimeline.Priorities;
            else
                timelines = new List<Timeline>() { baseTimeline };

            // Collect all slugcats that could result in these timelines
            var slugcats = new HashSet<SlugcatStats.Name>();
            foreach (var slugcatName in SlugcatStats.Name.values.entries)
            {
                var slugcat = new SlugcatStats.Name(slugcatName);
                if (timelines.Contains(SlugcatStats.SlugcatToTimeline(slugcat)))
                    slugcats.Add(slugcat);
            }

            //collect a list of all regions that have the slightest chance of being accessible
            var regions = new HashSet<string>();
            foreach (var slugcat in slugcats)
            {
                foreach (var region in orig(slugcat))
                    regions.Add(region);

                if (slugcat != i)
                {
                    foreach (var region in SlugcatStats.SlugcatStoryRegions(slugcat))
                        regions.Add(region);
                }
            }

            //remove the canon regions
            foreach (string region in SlugcatStats.SlugcatStoryRegions(i))
            {
                if (regions.Contains(region))
                    regions.Remove(region);
            }

            //order by FullRegionOrder
            List<string> allRegions = Region.GetFullRegionOrder();
            for (int j = allRegions.Count - 1; j >= 0; j--)
            {
                if (!regions.Contains(allRegions[j]))
                    allRegions.RemoveAt(j);
            }

            return allRegions;
        }

        // Custom timelines, StoryRegions: Mark regions as accessible for collections and safari
        private static List<string> SlugcatStats_SlugcatStoryRegions(On.SlugcatStats.orig_SlugcatStoryRegions orig, SlugcatStats.Name i)
        {
            if (!SlugBaseCharacter.TryGet(i, out var chara)) return orig(i);

            List<string> regions = null;

            // Inherit story regions from first non-SlugBase slugcat
            var baseTimeline = SlugcatStats.SlugcatToTimeline(i);
            var timelines = CustomTimeline.Registry.TryGet(baseTimeline, out var customTimeline)
                ? customTimeline.Priorities
                : new List<Timeline>() { baseTimeline };

            // Get the first non-SlugBase character that this inherits from
            SlugcatStats.Name vanillaSlugcat = timelines
                .Where(x => !CustomTimeline.Registry.TryGet(x, out _))
                .SelectMany(GetSlugcats)
                .FirstOrDefault();

            if (vanillaSlugcat != null)
            {
                regions = SlugcatStats.SlugcatStoryRegions(vanillaSlugcat);

                List<string> defaultRegions = orig(new SlugcatStats.Name(""));
                foreach (string region in orig(i))
                {
                    //add any extra regions from orig that wouldn't naturally be there
                    if (!defaultRegions.Contains(region) && !regions.Contains(region))
                    { regions.Add(region); }
                }
            }

            if(regions == null || regions.Count == 0)
            {
                regions = orig(i);
            }

            // Apply manual changes from StoryRegions
            if (StoryRegions.TryGet(chara, out var changes))
            {
                foreach (string str in changes)
                {
                    if (string.IsNullOrEmpty(str))
                    { /*something bad happened idk*/ }

                    else if (str.StartsWith("-"))
                    {
                        if (str.Length > 2 && regions.Contains(str.Substring(1)))
                        {
                            Debug.Log(string.Join(", ", regions));
                            regions.Remove(str.Substring(1));
                        }
                    }

                    else if (!regions.Contains(str))
                    { regions.Add(str); }
                }
            }

            return regions;
        }

        // Get all slugcats associated with a timeline
        private static IEnumerable<SlugcatStats.Name> GetSlugcats(Timeline timeline)
        {
            foreach (var name in SlugcatStats.Name.values.entries)
            {
                var slugcat = new SlugcatStats.Name(name);
                if (SlugcatStats.SlugcatToTimeline(slugcat) == timeline)
                    yield return slugcat;
            }
        }

        // Custom timelines: Swap regions based on character
        private static string Region_GetProperRegionAcronym(On.Region.orig_GetProperRegionAcronym_Timeline_string orig, Timeline timeline, string baseAcronym)
        {
            if (CustomTimeline.Registry.TryGet(timeline, out var customTimeline))
            {
                timeline = FindFirstEquivalences(customTimeline.Priorities, baseAcronym);
            }

            return orig(timeline, baseAcronym);
        }

        private static Timeline FindFirstEquivalences(IEnumerable<Timeline> names, string regionName)
        {
            string[] array = AssetManager.ListDirectory("World", true, false);
            foreach (var timeline in names)
            {
                //if it's not a slugbase timeline, it's probably real
                if (!CustomTimeline.Registry.TryGet(timeline, out _))
                {
                    return timeline;
                }

                for (int i = 0; i < array.Length; i++)
                {
                    string path = AssetManager.ResolveFilePath($"World{Separator}{Path.GetFileName(array[i])}{Separator}equivalences.txt");
                    if (!File.Exists(path)) continue;

                    string[] array2 = File.ReadAllText(path).Trim().Split(',');
                    for (int j = 0; j < array2.Length; j++)
                    {
                        string text2 = null;
                        string a = array2[j];
                        if (array2[j].Contains("-"))
                        {
                            a = array2[j].Split('-')[0];
                            text2 = array2[j].Split('-')[1];
                        }
                        if (a == regionName && text2 != null && timeline.value.ToLower() == text2.ToLower())
                        {
                            return timeline;
                        }
                    }
                }
            }
            return names.First();
        }

        #endregion

        #region worldloader

        private static ConditionalWeakTable<WorldLoader, StrongBox<Timeline>> _actualTimeline = new();

        public static StrongBox<Timeline> ActualTimeline(this WorldLoader p) => _actualTimeline.GetValue(p, _ => new(null));

        // Custom timelines: Change character filters
        private static void WorldLoader_ctor(On.WorldLoader.orig_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, Timeline timelinePosition, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            //store the Real slug so their name can be used later to get worldstate
            self.ActualTimeline().Value = timelinePosition;

            //guard clause
            if (!self.singleRoomWorld && CustomTimeline.Registry.TryGet(timelinePosition, out var customTimeline) && customTimeline.Base.Length > 0)
            {
                //open the world file and find the first slugcat mentioned
                string path = $"World{Separator}{worldName}{Separator}world_{worldName}.txt";
                string[] array = File.ReadAllLines(AssetManager.ResolveFilePath(path));
                timelinePosition = FirstTimelineMentionedInWorld(array.ToList(), customTimeline.Priorities);
            }

            orig(self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues);
        }

        // Custom timelines: Apply spawn file overrides
        private static void WorldLoader_ctor_ILHook(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After, x => x.MatchStfld<WorldLoader>(nameof(WorldLoader.simulateUpdateTicks))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<WorldLoader>>((self) =>
                {
                    if (self.singleRoomWorld || self.lines == null || self.lines.Count == 0) return;

                    int startIndex = self.lines.IndexOf("CREATURES");
                    int endIndex = self.lines.IndexOf("END CREATURES");
                    if (startIndex == -1 || endIndex == -1) return;

                    IEnumerable<Timeline> names;
                    if (CustomTimeline.Registry.TryGet(self.ActualTimeline().Value, out var customTimeline))
                        names = customTimeline.Priorities;
                    else
                        names = new Timeline[] { self.ActualTimeline().Value };

                    foreach (var vname in names)
                    {
                        string path = AssetManager.ResolveFilePath($"World{Separator}{self.worldName}{Separator}spawns_{self.worldName}-{vname.value}.txt");
                        if (File.Exists(path))
                        {
                            List<string> newSpawns = File.ReadAllLines(path).Where(x => !x.StartsWith("//") && !string.IsNullOrEmpty(x) && x.Contains(":")).ToList();

                            self.lines.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
                            self.lines.InsertRange(startIndex + 1, newSpawns);
                            break;
                        }
                    }
                });
            }
            else
            {
                SlugBasePlugin.Logger.LogError("Failed to IL hook WorldLoader.ctor!");
            }
        }

        private static Timeline FirstTimelineMentionedInWorld(List<string> lines, IEnumerable<Timeline> timelines)
        {
            int conditionalStart = lines.IndexOf("CONDITIONAL LINKS");
            int conditionalEnd = lines.IndexOf("END CONDITIONAL LINKS");

            foreach (var timeline in timelines)
            {
                //if it's not a slugbase character then it's assumed to have a valid world
                //jk, what if you want to load saint world but with Rivulet's UW
                //if (!SlugBaseCharacter.TryGet(name, out _) && name.index >= 0) return name;

                if (conditionalStart != -1 && conditionalEnd != -1)
                {
                    foreach (string line in lines.GetRange(conditionalStart, conditionalEnd - conditionalStart))
                    {
                        if (!string.IsNullOrEmpty(line) && line.Contains(" : ") && Regex.Split(line, " : ")[0] == timeline.value)
                        {
                            return timeline;
                        }
                    }
                }

                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line) && line[0] == '(' && line.IndexOf(")") > 0)
                    {
                        string text = line.Substring(1, line.IndexOf(")") - 1);
                        if (text.StartsWith("X-")) text = text.Substring(2);
                        string[] array = text.Split(',');
                        foreach (string str in array)
                        {
                            if (str == timeline.value) return timeline;
                        }
                    }
                }
            }

            return timelines.FirstOrDefault() ?? Timeline.White;
        }

        //set slug's name back to regular so it's passed into World.LoadMapConfig
        private static void WorldLoader_CreatingWorld(On.WorldLoader.orig_CreatingWorld orig, WorldLoader self)
        {
            Timeline currentTimeline = self.timelinePosition;
            self.timelinePosition = self.ActualTimeline().Value;

            orig(self);

            self.timelinePosition = currentTimeline;
        }
        #endregion

        #region misc
        // Custom timelines: Stop some creatures from freezing when using Saint's world state
        private static void AbstractCreature_setCustomFlags(On.AbstractCreature.orig_setCustomFlags orig, AbstractCreature self)
        {
            orig(self);

            if (ModManager.MSC
                && self.Room.world.game.IsStorySession
                && CustomTimeline.Registry.TryGet(self.Room.world.game.TimelinePoint, out var timeline)
                && timeline.InheritsFrom(Timeline.Saint))
            {
                if (self.creatureTemplate.BlizzardAdapted)
                {
                    self.HypothermiaImmune = true;
                }
                if (self.creatureTemplate.BlizzardWanderer)
                {
                    self.ignoreCycle = true;
                }
            }
        }

        // Custom timelines: Change conditional settings
        private static void RoomSettings_ctor(On.RoomSettings.orig_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame orig, RoomSettings self, Room room, string name, Region region, bool template, bool firstTemplate, Timeline timelinePoint, RainWorldGame game)
        {
            if (CustomTimeline.Registry.TryGet(timelinePoint, out var customTimeline))
            {
                foreach (var vname in customTimeline.Priorities)
                {
                    string path = WorldLoader.FindRoomFile(name, false, "_settings-" + vname.value + ".txt");
                    if (File.Exists(path)) { timelinePoint = vname; break; }
                }
            }

            orig(self, room, name, region, template, firstTemplate, timelinePoint, game);
        }

        // WorldState: Change regions on fast travel screen ||UNUSED||
        // private static Region[] Region_LoadAllRegions(On.Region.orig_LoadAllRegions orig, SlugcatStats.Name storyIndex)
        // {
        //     if (SlugBaseCharacter.TryGet(storyIndex, out var chara)
        //         && WorldState.TryGet(chara, out var copyWorld))
        //     {
        //         storyIndex = Utils.FirstValidEnum(copyWorld) ?? storyIndex;
        //     }
        // 
        //     return orig(storyIndex);
        // }

        // Custom timelines: Change region names
        private static string Region_GetRegionFullName(On.Region.orig_GetRegionFullName orig, string regionAcro, SlugcatStats.Name slugcatIndex)
        {
            var timeline = SlugcatStats.SlugcatToTimeline(slugcatIndex);
            if (CustomTimeline.Registry.TryGet(timeline, out var customTimeline))
            {
                foreach (var vname in customTimeline.Priorities)
                {
                    string path = AssetManager.ResolveFilePath($"World{Path.DirectorySeparatorChar}{regionAcro}{Path.DirectorySeparatorChar}DisplayName-{vname.value}.txt");
                    if (File.Exists(path)) { slugcatIndex = new SlugcatStats.Name(vname.value); break; }
                }
            }

            return orig(regionAcro, slugcatIndex);
        }
        #endregion misc
    }
}
