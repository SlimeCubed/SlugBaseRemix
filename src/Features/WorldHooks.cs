using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SlugBase.Features
{
    using static GameFeatures;
    using static System.Net.Mime.MediaTypeNames;

    internal static class WorldHooks
    {
        public static bool DebugMode = true;
        public static void Apply()
        {
            //map_XX hooks
            IL.HUD.Map.LoadConnectionPositions += ApplyWorldState;
            IL.HUD.Map.Update += ApplyWorldState;
            IL.World.LoadWorldForFastTravel += World_LoadMapConfig;
            IL.World.LoadMapConfig += World_LoadMapConfig;

            //properties.txt hooks
            IL.World.LoadMapConfig += PropertiesConfig;
            On.Region.ctor += Region_ctor;

            //meta hooks
            On.SlugcatStats.getSlugcatOptionalRegions += SlugcatStats_getSlugcatOptionalRegions;
            On.SlugcatStats.getSlugcatStoryRegions += SlugcatStats_getSlugcatStoryRegions;
            On.Region.GetProperRegionAcronym += Region_GetProperRegionAcronym;

            //worldloader
            On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues += WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues;
            On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;

            //misc
            On.Region.GetRegionFullName += Region_GetRegionFullName;
            On.RoomSettings.ctor += RoomSettings_ctor;
            On.AbstractCreature.setCustomFlags += AbstractCreature_setCustomFlags;

            //this is breaking my brain
            //IL.PlacedObject.FilterData.ToString += FilterData_ToString;
            //On.PlacedObject.FilterData.FromString += FilterData_FromString;

            //this property is only used to be passed into WorldLoader.ctor
            //new Hook( typeof(OverWorld).GetProperty(nameof(OverWorld.PlayerCharacterNumber)).GetGetMethod(),OverWorld_get_PlayerCharacterNumber );
            //On.Region.LoadAllRegions += Region_LoadAllRegions; //nah, we want the hook to be individualized
        }

        private static ConditionalWeakTable<PlacedObject.FilterData, List<SlugcatStats.Name>> Mentioned = new();

        public static List<SlugcatStats.Name> PlayerMentioned(this PlacedObject.FilterData p) => Mentioned.GetValue(p, _ => new());

        private static void FilterData_FromString(On.PlacedObject.FilterData.orig_FromString orig, PlacedObject.FilterData self, string s)
        {
            orig(self, s);
            self.PlayerMentioned().Clear();
            string[] array = Regex.Split(s, "~")[4].Split('|');
            if (!array.Contains("{SlugbaseVersion}")) return;

            for (int i = 0; i < ExtEnum<SlugcatStats.Name>.values.Count; i++)
            {
                string entry = ExtEnum<SlugcatStats.Name>.values.GetEntry(i);
                SlugcatStats.Name name = new(entry, false);
                if (SlugcatStats.HiddenOrUnplayableSlugcat(name)) continue;
                foreach (string str in array)
                {
                    string character = str;
                    if (str.StartsWith("{include}"))
                    {
                        character = character.Substring("{include}".Length);
                    }
                    if (character == entry)
                    {
                        self.PlayerMentioned().Add(name);
                        break;
                    }
                }
            }
        }

        private static void FilterData_ToString(ILContext il)
        {
            var c = new ILCursor(il);

            int loc = 5;
            int loc2 = 4;
            while (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(out loc),
                x => x.MatchCall<SlugcatStats>("HiddenOrUnplayableSlugcat"),
                x => x.MatchBrtrue(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PlacedObject.FilterData>("availableToPlayers"),
                x => x.MatchLdloc(out _),
                x => x.MatchCallvirt<List<SlugcatStats.Name>>("Contains"),
                x => x.MatchBrtrue(out _),
                x => x.MatchLdloc(out _),
                x => x.MatchLdloc(out loc2),
                x => x.MatchCallvirt<List<string>>("Add")
                ))
            {
                c.Emit(OpCodes.Ldloca_S, loc);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloca_S);
                c.EmitDelegate<Action<PlacedObject.FilterData, SlugcatStats.Name, List<string>>>((self, name, list) =>
                {
                    list.Add("{SlugbaseVersion}");
                    if (!SlugcatStats.HiddenOrUnplayableSlugcat(name) && self.availableToPlayers.Contains(name))
                    {
                        list.Add("{include}"+name.value);
                    }
                });
            }
        }

        #region Map & Properties
        // WorldState: Fix map connections
        private static void ApplyWorldState(ILContext il)
        {
            var c = new ILCursor(il);

            while (c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<PlayerProgression>("get_PlayingAsSlugcat")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<SlugcatStats.Name, HUD.Map, SlugcatStats.Name>>((name, self) =>
                {
                    if (SlugBaseCharacter.TryGet(name, out var chara)
                        && WorldState.TryGet(chara, out var copyWorld))
                    {
                        foreach (var vname in Utils.AllValidEnums(copyWorld).ToArray())
                        {
                            string path = AssetManager.ResolveFilePath($"World{Path.DirectorySeparatorChar}{self.RegionName}{Path.DirectorySeparatorChar}map_{self.RegionName}-{vname.value}.txt");
                            if (File.Exists(path)) { name = vname; Debug.Log($"map slugbase found alternate world_state map for slug [{vname.value}]"); break; }
                        }
                    }

                    return name;
                });
            }
        }
        private static void World_LoadMapConfig(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<World>("name"),
                x => x.MatchStelemRef(),
                x => x.MatchDup(),
                x => x.MatchLdcI4(6),
                x => x.MatchLdstr("-"),
                x => x.MatchStelemRef(),
                x => x.MatchDup(),
                x => x.MatchLdcI4(7),
                x => x.MatchLdarg(1)))
                //x => x.MatchLdfld<ExtEnumBase>("value")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<World>(OpCodes.Ldfld, "name");
                c.EmitDelegate<Func<SlugcatStats.Name, string, SlugcatStats.Name>>((name, regionName) =>
                {
                    //cursed hook
                    if (SlugBaseCharacter.TryGet(name, out var chara)
                        && WorldState.TryGet(chara, out var copyWorld))
                    {
                        foreach (var vname in Utils.AllValidEnums(copyWorld).ToArray())
                        {
                            string path = AssetManager.ResolveFilePath($"World{Path.DirectorySeparatorChar}{regionName}{Path.DirectorySeparatorChar}map_{regionName}-{vname.value}.txt");
                            if (File.Exists(path))
                            { return vname; }
                        }
                    }

                    return name;
                });
            }
            else
            {
                Debug.Log("Failed to hook World_LoadMapConfig!!");
            }
        }


        private static void PropertiesConfig(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                x => x.MatchStelemRef(),
                x => x.MatchDup(),
                x => x.MatchLdcI4(4),
                x => x.MatchLdstr("Properties- "), //one space typo in vanilla code lol
                x => x.MatchStelemRef(),
                x => x.MatchDup(),
                x => x.MatchLdcI4(5),
                x => x.MatchLdarg(1)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<World>(OpCodes.Ldfld, "name");
                c.EmitDelegate<Func<SlugcatStats.Name, string, SlugcatStats.Name>>((name, regionName) =>
                {
                    //cursed hook
                    if (SlugBaseCharacter.TryGet(name, out var chara) && WorldState.TryGet(chara, out var copyWorld))
                    {
                        foreach (var vname in Utils.AllValidEnums(copyWorld).ToArray())
                        {
                            string path = AssetManager.ResolveFilePath($"World{Path.DirectorySeparatorChar}{regionName}{Path.DirectorySeparatorChar}Properties- {vname.value}.txt");
                            if (File.Exists(path))
                            { name = vname; break; }
                        }
                    }

                    return name;
                });
            }
            else
            {
                Debug.Log("Failed to IL hook LoadMap properties 1!!");
            }

            int loc = 14;
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out loc),
                x => x.MatchLdcI4(0),
                x => x.MatchLdelemRef(),
                x => x.MatchLdstr("Broken Shelters"),
                x => x.MatchCall(typeof(string),"op_Equality"),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(1)))
            {
                c.Emit(OpCodes.Ldloc, loc);
                c.EmitDelegate<Func<SlugcatStats.Name, string[], SlugcatStats.Name>>((name, lines) =>
                {
                    //cursed hook
                    if (SlugBaseCharacter.TryGet(name, out var chara) && WorldState.TryGet(chara, out var copyWorld))
                    {
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("Broken Shelters"))
                            {
                                foreach (var vname in Utils.AllValidEnums(copyWorld).ToArray())
                                {
                                    string[] array = Regex.Split(RWCustom.Custom.ValidateSpacedDelimiter(line, ":"), ": ");
                                    if (array.Length > 1 && array[1] == vname.value)
                                    {
                                        return vname;
                                    }
                                }
                            }
                        }
                    }

                    return name;
                });
            }
            else
            {
                Debug.Log("Failed to IL hook LoadMap properties 2!!");
            }
        }

        private static void Region_ctor(On.Region.orig_ctor orig, Region self, string name, int firstRoomIndex, int regionNumber, SlugcatStats.Name storyIndex)
        {
            if (SlugBaseCharacter.TryGet(storyIndex, out var chara)
                && WorldState.TryGet(chara, out var copyWorld))
            {
                foreach (var vname in Utils.AllValidEnums(copyWorld).ToArray())
                {
                    string path = AssetManager.ResolveFilePath($"World{Path.DirectorySeparatorChar}{name}{Path.DirectorySeparatorChar}properties-{vname.value}.txt");
                    if (File.Exists(path)) { storyIndex = vname; break; }
                }
            }

            orig(self, name, firstRoomIndex, regionNumber, storyIndex);
        }
        #endregion

        #region meta
        // WorldState: Mark regions as optional for collections and safari
        private static string[] SlugcatStats_getSlugcatOptionalRegions(On.SlugcatStats.orig_getSlugcatOptionalRegions orig, SlugcatStats.Name i)
        {
            if (!(SlugBaseCharacter.TryGet(i, out var chara) && WorldState.TryGet(chara, out var copyWorld))) return orig(i);

            List<string> allRegions = Region.GetFullRegionOrder();
            List<string> regions = new();

            //collect a list of all regions that have the slightest chance of being accessible
            foreach (SlugcatStats.Name name in Utils.AllValidEnums(copyWorld))
            {
                if (name != i)
                {
                    regions.AddRange(orig(name).Where(x => !regions.Contains(x)));
                    regions.AddRange(SlugcatStats.getSlugcatStoryRegions(name).Where(x => !regions.Contains(x)));
                }
                else
                {
                    regions.AddRange(orig(i).Where(x => !regions.Contains(x)));
                }
            }

            //remove the canon regions
            foreach (string region in SlugcatStats.getSlugcatStoryRegions(i))
            {
                if (regions.Contains(region))
                { regions.Remove(region); }
            }

            //order by FullRegionOrder
            for (int j = allRegions.Count; j > 0; j++)
            {
                if (!regions.Contains(allRegions[j]))
                { allRegions.RemoveAt(j); }
            }

            return allRegions.ToArray();
        }

        // WorldState: Mark regions as accessible for collections and safari
        private static string[] SlugcatStats_getSlugcatStoryRegions(On.SlugcatStats.orig_getSlugcatStoryRegions orig, SlugcatStats.Name i)
        {
            if (!(SlugBaseCharacter.TryGet(i, out var chara) && WorldState.TryGet(chara, out var copyWorld))) return orig(i);
            SlugcatStats.Name[] names = Utils.AllValidEnums(copyWorld).ToArray();

            List<string> regions = new();

            //find first slug that isn't me
            int j = 0;
            for (; j < names.Length; j++)
            {
                if (names[j] != i)
                { break; }
            }

            if (j != names.Length)
            {
                if (SlugBaseCharacter.TryGet(names[j], out SlugBaseCharacter chara2)
                    && WorldState.TryGet(chara2, out SlugcatStats.Name[] copyWorld2))
                {
                    //this is very hacky I know, but it's easier than what I would do otherwise
                    //temporarily pretend the modded slug's worldstate is the current slug's
                    chara2.Features.Set(StoryRegions, new JsonAny(names.Skip(Mathf.Max(0, j - 1)), null));
                    regions = SlugcatStats.getSlugcatStoryRegions(names[j]).ToList();
                    chara2.Features.Set(StoryRegions, new JsonAny(copyWorld2, null));
                }
                else
                {
                    regions = SlugcatStats.getSlugcatStoryRegions(names[j]).ToList();
                }
            }

            List<string> defaultRegions = orig(new SlugcatStats.Name("")).ToList();
            foreach (string region in orig(i).ToList())
            {
                //add any extra regions from orig that wouldn't naturally be there
                if (!defaultRegions.Contains(region) && !regions.Contains(region))
                { regions.Add(region); }
            }

            if (StoryRegions.TryGet(chara, out var changes))
            {
                foreach (string str in changes)
                {
                    if (string.IsNullOrEmpty(str))
                    { /*something bad happened idk*/ }

                    else if (str.StartsWith("-") && str.Length > 2 && regions.Contains(str.Substring(1)))
                    { regions.Remove(str.Substring(1)); }

                    else if (!regions.Contains(str))
                    { regions.Add(str); }
                }
            }

            return regions.ToArray();
        }

        // WorldState: Swap regions based on character
        private static string Region_GetProperRegionAcronym(On.Region.orig_GetProperRegionAcronym orig, SlugcatStats.Name character, string baseAcronym)
        {
            if (SlugBaseCharacter.TryGet(character, out var chara) && WorldState.TryGet(chara, out var copyWorld) && Utils.AllValidEnums(copyWorld).Count != 0)
            {
                character = FindFirstEquivalences(Utils.AllValidEnums(copyWorld).ToArray(), baseAcronym);
            }

            return orig(character, baseAcronym);
        }

        private static SlugcatStats.Name FindFirstEquivalences(SlugcatStats.Name[] names, string regionName)
        {
            string[] array = AssetManager.ListDirectory("World", true, false);
            foreach (SlugcatStats.Name name in names)
            {
                //if it's not a slugbase character, it's probably real
                if (!SlugBaseCharacter.TryGet(name, out _))
                {
                    return name;
                }
                for (int i = 0; i < array.Length; i++)
                {
                    string path = AssetManager.ResolveFilePath($"World{Path.DirectorySeparatorChar}{Path.GetFileName(array[i])}{Path.DirectorySeparatorChar}equivalences.txt");
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
                        if (a == regionName && text2 != null && name.value.ToLower() == text2.ToLower())
                        {
                            return name;
                        }
                    }
                }
            }
            return names[0];
        }

        #endregion

        #region worldloader

        private static ConditionalWeakTable<WorldLoader, StrongBox<SlugcatStats.Name>> _ActualPlayer = new();

        public static StrongBox<SlugcatStats.Name> ActualPlayer(this WorldLoader p) => _ActualPlayer.GetValue(p, _ => new(null));
        // WorldState: Change character filters
        private static void WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues
            (On.WorldLoader.orig_ctor_RainWorldGame_Name_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game,
            SlugcatStats.Name playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            //guard clause
            if (self.singleRoomWorld || !SlugBaseCharacter.TryGet(playerCharacter, out var chara)
                || !WorldState.TryGet(chara, out var copyWorld) || copyWorld.Length == 0)
            {
                orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
                return;
            }

            //story the Real slug so their name can be used later to get worldstate
            self.ActualPlayer().Value = playerCharacter;

            //open the world file and find the first slugcat mentioned
            string path = $"World{Path.DirectorySeparatorChar}{worldName}{Path.DirectorySeparatorChar}world_{worldName}.txt";
            string[] array = File.ReadAllLines(AssetManager.ResolveFilePath(path));
            playerCharacter = FirstSlugcatMentionedInWorld(array.ToList(), Utils.AllValidEnums(copyWorld).ToArray());

            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);

            //find spawns file, if exists
            foreach (var vname in Utils.AllValidEnums(copyWorld).ToArray())
            {
                path = AssetManager.ResolveFilePath($"World{Path.DirectorySeparatorChar}{worldName}{Path.DirectorySeparatorChar}spawns_{worldName}-{vname.value}.txt");
                if (File.Exists(path))
                {
                    List<string> newSpawns = File.ReadAllLines(path).Where(x => !x.StartsWith("//")).ToList();

                    int startIndex = self.lines.IndexOf("CREATURES");
                    int endIndex = self.lines.IndexOf("END CREATURES");
                    if (startIndex == -1 || endIndex == -1) return;

                    self.lines.RemoveRange(startIndex + 1, endIndex - startIndex - 2);
                    self.lines.InsertRange(startIndex + 1, newSpawns);
                    break;
                }
            }
        }

        private static SlugcatStats.Name FirstSlugcatMentionedInWorld(List<string> lines, SlugcatStats.Name[] names)
        {
            int conditionalStart = lines.IndexOf("CONDITIONAL LINKS");
            int conditionalEnd = lines.IndexOf("END CONDITIONAL LINKS");

            foreach (SlugcatStats.Name name in names)
            {
                //if it's not a slugbase character then it's assumed to have a valid world
                if (!SlugBaseCharacter.TryGet(name, out _) && name.index >= 0) return name;

                if (conditionalStart != -1 && conditionalEnd != -1)
                {
                    foreach (string line in lines.GetRange(conditionalStart, conditionalEnd - conditionalStart))
                    {
                        if (!string.IsNullOrEmpty(line) && line.Contains(" : ") && Regex.Split(line, " : ")[0] == name.value)
                        {
                            return name;
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
                            if (str == name.value) return name;
                        }
                    }
                }
            }

            return names[0] ?? SlugcatStats.Name.White;
        }

        //set slug's name back to regular so it's passed into World.LoadMapConfig
        private static void WorldLoader_CreatingWorld(On.WorldLoader.orig_CreatingWorld orig, WorldLoader self)
        {
            SlugcatStats.Name currentName = self.playerCharacter;
            self.playerCharacter = self.ActualPlayer().Value;

            Debug.Log($"playerchar is [{currentName?.value}], actualplayer is [{self.playerCharacter?.value}]");

            orig(self);

            self.playerCharacter = currentName;
        }
        #endregion

        #region misc
        // WorldState: Stop some creatures from freezing when using Saint's world state
        private static void AbstractCreature_setCustomFlags(On.AbstractCreature.orig_setCustomFlags orig, AbstractCreature self)
        {
            orig(self);

            if (ModManager.MSC
                && self.Room.world.game.IsStorySession
                && SlugBaseCharacter.TryGet(self.Room.world.game.StoryCharacter, out var chara)
                && WorldState.TryGet(chara, out var copyWorld)
                && Utils.FirstValidEnum(copyWorld) == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
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

        // WorldState: Change region world files ||UNUSED||
        private static SlugcatStats.Name OverWorld_get_PlayerCharacterNumber(Func<OverWorld, SlugcatStats.Name> orig, OverWorld self)
        {
            var playerChar = orig(self);

            if (SlugBaseCharacter.TryGet(playerChar, out var chara)
                && WorldState.TryGet(chara, out var copyWorld))
            {
                playerChar = Utils.FirstValidEnum(copyWorld) ?? playerChar;
            }

            return playerChar;
        }

        // WorldState: Change conditional settings
        private static void RoomSettings_ctor(On.RoomSettings.orig_ctor orig, RoomSettings self, string name, Region region, bool template, bool firstTemplate, SlugcatStats.Name playerChar)
        {
            if (SlugBaseCharacter.TryGet(playerChar, out var chara)
                && WorldState.TryGet(chara, out var copyWorld))
            {
                foreach (var vname in Utils.AllValidEnums(copyWorld).ToArray())
                {
                    string path = WorldLoader.FindRoomFile(name, false, "_settings-" + vname.value + ".txt");
                    if (File.Exists(path)) { playerChar = vname; break; }
                }
            }

            orig(self, name, region, template, firstTemplate, playerChar);
        }

        // WorldState: Change regions on fast travel screen ||UNUSED||
        private static Region[] Region_LoadAllRegions(On.Region.orig_LoadAllRegions orig, SlugcatStats.Name storyIndex)
        {
            if (SlugBaseCharacter.TryGet(storyIndex, out var chara)
                && WorldState.TryGet(chara, out var copyWorld))
            {
                storyIndex = Utils.FirstValidEnum(copyWorld) ?? storyIndex;
            }

            return orig(storyIndex);
        }

        // WorldState: Change region names
        private static string Region_GetRegionFullName(On.Region.orig_GetRegionFullName orig, string regionAcro, SlugcatStats.Name slugcatIndex)
        {
            if (SlugBaseCharacter.TryGet(slugcatIndex, out var chara)
                && WorldState.TryGet(chara, out var copyWorld))
            {
                foreach (var vname in Utils.AllValidEnums(copyWorld).ToArray())
                {
                    string path = AssetManager.ResolveFilePath($"World{Path.DirectorySeparatorChar}{regionAcro}{Path.DirectorySeparatorChar}DisplayName-{vname.value}.txt");
                    if (File.Exists(path)) { slugcatIndex = vname; break; }
                }
            }

            return orig(regionAcro, slugcatIndex);
        }
        #endregion misc
    }
}
