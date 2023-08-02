using System;
using System.Collections.Generic;
using SlugBase.SaveData;
using UnityEngine;
using SlugBase.Assets;

namespace SlugBase
{
    /// <summary>
    /// Use the functions contained in this class to help do stuff with slugbase!
    /// </summary>
    public static class Helpers
    {
        internal static List<Color> StringListToColorList(this List<string> stringList)
        {
            if (stringList == null) { return null; }
            List<Color> colorList = new List<Color>();
            foreach (string color in stringList) {
                ColorUtility.TryParseHtmlString("#" + color, out Color loadedColor);
                colorList.Add(loadedColor);
            }
            return colorList;
        }
        internal static List<string> ColorListToStringList(this List<Color> colorList)
        {
            if (colorList == null) { return null; }
            List<string> stringList = new List<string>();
            foreach (Color color in colorList) {
                stringList.Add(ColorUtility.ToHtmlStringRGB(color));
            }
            return stringList;
        }

        /// <summary>
        /// Gets the color of slot <paramref name="colorNum"/> for a given player, for use instead of <see cref="DataTypes.ColorSlot.GetColor(PlayerGraphics)"/> if a slugcat has more than 3 custom colors for compatiblility with Jolly-Coop.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that this extension method is acting on. </param>
        /// <param name="colorNum">The index of the color to use, defined in the character's slugbase JSON file, indicies going from top to the bottom</param>
        /// <returns>The color of this body part after modifications are applied.</returns>
        public static Color GetPlayerAndJollyColor(this Player player, int colorNum) {
        // Basically, this is just a different way to use ColorSlot.GetColor(PlayerGraphics), but also does some extra stuff for considering the extended Jolly Coop menu. It is also a bit simpler to use for a beginner since it gets the DataTypes.ColorSlot[] for them, so I feel it is worth keeping separate from ColorSlot.GetColor()
            int playerInt = player.playerState.playerNumber;
            // Populate the BodyColors[int] list for the player who called for the function, if it is empty. Will be the case if the players skip touching the Jolly Color Menu and the slugcat has more than 3 custom colors.
            if (ModManager.CoopAvailable && (player.graphicsModule as PlayerGraphics).useJollyColor && AssetHooks.BodyColors[playerInt].Count == 0 && SlugBaseCharacter.TryGet(player.slugcatStats.name, out var chara1) && Features.PlayerFeatures.CustomColors.TryGet(chara1, out DataTypes.ColorSlot[] colors) && colors.Length > 3) {
                // Get the saved custom colors and turn them into a List<Color>
                player.abstractCreature.Room.realizedRoom.game.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().TryGet($"Custom_Colors_{playerInt}", out List<string> stringLoadedColors);
                List<Color> loadedColorsList = stringLoadedColors.StringListToColorList();
                // If it is not null, add it to the custom colors for jolly.
                if (loadedColorsList != null) {
                    AssetHooks.BodyColors[playerInt].AddRange(loadedColorsList);
                }
                // 
                else {
                    for (int i = 3; i < colors.Length; i++) {
                        AssetHooks.BodyColors[playerInt].Add(colors[i].Default);
                    }
                    // Save them so that it is slightly faster to run this method on future calls(?).
                    player.abstractCreature.Room.world.game.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set<List<string>>($"Custom_Colors_{playerInt}", AssetHooks.BodyColors[playerInt].ColorListToStringList());
                }
            }
            
            if (ModManager.CoopAvailable && (player.graphicsModule as PlayerGraphics).useJollyColor && colorNum >= 3 && AssetHooks.BodyColors[playerInt].Count > colorNum-3) {
                return AssetHooks.BodyColors[playerInt][colorNum-3];
            }
            else if (SlugBaseCharacter.TryGet(player.slugcatStats.name, out var chara) && Features.PlayerFeatures.CustomColors.TryGet(chara, out DataTypes.ColorSlot[] colors1)) {
                return colors1[colorNum].GetColor(player.graphicsModule as PlayerGraphics);
            }
            Debug.LogWarning("Could not find valid condition for selecting a color");
            return Color.red;
        }

        /// <summary>
        /// Set the dream scene that will display when the player hibernates next.
        /// </summary>
        /// <param name="name">The id of the scene to display.</param>
        public static void QueueDream(string name)
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame rainGame && CustomScene.Registry.TryGet(new(name), out var customScene))
            {
                rainGame.GetStorySession.saveState.dreamsState.InitiateEventDream(new (name));
            }
            else if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame)
            {
                Debug.LogError("Slugbase dream set fail, curentMainLoop is not a RainWorldGame!");
            }
            else if (!CustomScene.Registry.TryGet(new(name), out var scene))
            {
                Debug.LogError("Slugbase dream set fail, could not find matching scene");
            }
        }
        
        /// <summary>
        /// Must match the ID to the id in a slideshow's json file, and provide the ProcessManager, in order to play an outro slideshow
        /// </summary>
        /// <param name="ID">The ID of the slideshow to play, should be declared as a new Menu.SlideShow.SlideShowID(string, false) with the string matching the id of a slugbase slideshow .json file.</param>
        /// <param name="manager">The ProcessManager, needed to change the active process.</param>
        /// <param name="fadeOutSeconds">The time taken to fade to black.</param>
        /// <param name="newMenuSelectScene">The new scene to display on the charact select screen.</param>
        public static void NewOutro(ProcessManager manager, string ID, string newMenuSelectScene = null, float fadeOutSeconds = 0.45f)
        {
            manager.nextSlideshow = new (ID);
            if (newMenuSelectScene != null && manager.currentMainLoop is RainWorldGame rainGame)
            {
                manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set<string>($"menu_select_scene_alt_{rainGame.StoryCharacter.value}", newMenuSelectScene);
                manager.rainWorld.progression.SaveToDisk(true, true, true);
            }
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow, fadeOutSeconds);
        }
    }
}