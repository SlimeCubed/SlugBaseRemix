using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SlugBase.SaveData
{
    internal class MinedSaveData
    {
        internal static readonly ConditionalWeakTable<Menu.SlugcatSelectMenu.SaveGameData, MinedSaveData> Data = new();

        public Menu.MenuScene.SceneID SelectMenuScene;

        public MinedSaveData(RainWorld rainWorld, SlugcatStats.Name slugcat)
        {
            var progLines = rainWorld.progression.GetProgLinesFromMemory();

            for (int i = 0; i < progLines.Length; i++)
            {
                string[] array = Regex.Split(progLines[i], "<progDivB>");
                if (array.Length == 2 && array[0] == "SAVE STATE" && BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) == slugcat)
                {
                    var targets = new List<SaveStateMiner.Target>
                    {
                        new (">SELECTSCENE", SlugBaseSaveData.KEY_SUFFIX_INTERNAL, "<dpA>", 200)
                    };

                    foreach (var result in SaveStateMiner.Mine(rainWorld, array[1], targets))
                    {
                        switch(result.name)
                        {
                            case ">SELECTSCENE": SelectMenuScene = new(Encoding.UTF8.GetString(Convert.FromBase64String(result.data))); break;
                        }
                    }
                }
            }
        }
    }
}
