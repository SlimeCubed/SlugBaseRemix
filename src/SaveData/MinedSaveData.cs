using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SlugBase.SaveData
{
    /// <summary>
    /// Holds all the slugcat associated data that is needed outside of loaded savestate
    /// </summary>
    internal class MinedSaveData
    {
        /// <summary>
        /// Registry of all additional data for each savegamedata
        /// </summary>
        internal static readonly ConditionalWeakTable<Menu.SlugcatSelectMenu.SaveGameData, MinedSaveData> Data = new();

        public Menu.MenuScene.SceneID SelectMenuScene;

        /// <summary>
        /// Mines select menu scene from saved data
        /// </summary>
        /// <param name="rainWorld"></param>
        /// <param name="slugcat"></param>
        public MinedSaveData(RainWorld rainWorld, SlugcatStats.Name slugcat)
        {
            string[] progLines = rainWorld.progression.GetProgLinesFromMemory();

            for (int i = 0; i < progLines.Length; i++)
            {
                string[] array = Regex.Split(progLines[i], "<progDivB>");

                if (array.Length == 2 
                    && array[0] == "SAVE STATE" 
                    && BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) == slugcat)
                {
                    if (Mine<string>(array[1], ">SELECTSCENE" + SlugBaseSaveData.KEY_SUFFIX_INTERNAL, "<") is string selectMenu)
                        SelectMenuScene = new Menu.MenuScene.SceneID(selectMenu);
                    
                    break;
                }
            }
        }

        /// <summary>
        /// Finds a base64 encoded json string inside saveline and tries to unpack it into an object
        /// </summary>
        /// <typeparam name="T">Type of object that is being looked for</typeparam>
        /// <param name="saveLine">Line to search object in</param>
        /// <param name="startMarker">Left delimiter of saved value</param>
        /// <param name="endMarker">Right delimiter of saved value</param>
        /// <param name="defaultValue">What to return if the process failed</param>
        /// <returns>instance of T saved inside string</returns>
        private static T Mine<T>(string saveLine, string startMarker, string endMarker, T defaultValue = default)
        {
            int start = saveLine.IndexOf(startMarker);

            if (start == -1) return defaultValue;

            start += startMarker.Length;
            int end = saveLine.IndexOf(endMarker, start);

            try
            {
                var text = Encoding.UTF8.GetString(Convert.FromBase64String(saveLine.Substring(start, end - start)));
                return JsonConvert.DeserializeObject<T>(text);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                SlugBasePlugin.Logger.LogError($"Failed to mine key \"{startMarker}\"");
            }

            return defaultValue;
        }
    }
}
