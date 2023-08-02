using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JollyCoop.JollyMenu;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SlugBase.SaveData;
using UnityEngine;
using static DreamsState;
using static Menu.MenuScene;
using static Menu.SlideShow;
using static SlugBase.Features.GameFeatures;

namespace SlugBase.Assets
{
    internal static class AssetHooks
    {
        private const string SLUGBASE_FOLDER = "SlugBase Assets";
        private static List<List<ColorChangeDialog.ColorSlider>> NewSliders = new List<List<ColorChangeDialog.ColorSlider>>{new(), new(), new(), new()};
        internal static List<List<Color>> BodyColors = new List<List<Color>>{new(), new(), new(), new()};

        public static void Apply()
        {
            // Static menu hooks building (select screen, dreams)
            IL.Menu.MenuIllustration.LoadFile_string += MenuIllustration_LoadFile_string;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
            
            // Slideshow hooks (Intro and Outro are covered)
            IL.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;
            IL.RainWorldGame.ExitToVoidSeaSlideShow += RainWorldGame_ExitToVoidSeaSlideShow;
            IL.Menu.SlideShow.ctor += SlideShow_ctor;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene_IntroOutroSlideshow;
            On.Menu.SlideShowMenuScene.ctor += SlideShowMenuScene_ctor;

            // Dream hooks
            IL.RainWorldGame.Win += RainWorldGame_Win;
            On.Menu.DreamScreen.SceneFromDream += SceneID_SceneFromDream;
            On.DreamsState.InitiateEventDream += DreamsState_InitiateEventDream;

            // Jolly Color Menu Hooks
            On.JollyCoop.JollyMenu.ColorChangeDialog.ctor += On_ColorChangeDialog_ctor;
            On.JollyCoop.JollyMenu.ColorChangeDialog.ActualSavingColor += ColorChangeDialog_ActualSavingColor;
            On.JollyCoop.JollyMenu.ColorChangeDialog.Singal += ColorChangeDialog_Singal;
            IL.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += ColorChangeDialog_ValueOfSlider;
            IL.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += ColorChangeDialog_SliderSetValue;
            On.JollyCoop.JollyMenu.ColorChangeDialog.AddSlider += ColorChangeDialog_AddSlider;
            IL.JollyCoop.JollyMenu.ColorChangeDialog.ctor += IL_ColorChangeDialog_ctor;
        }
        
        // Use paths as atlas names instead of just the file name
        private static void MenuIllustration_LoadFile_string(ILContext il)
        {
            var c = new ILCursor(il);

            if(c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<AssetManager>(nameof(AssetManager.ResolveFilePath))))
            {
                c.EmitDelegate<Func<string, string>>(path =>
                {
                    if(path.StartsWith(SLUGBASE_FOLDER + Path.DirectorySeparatorChar))
                        return path.Substring(SLUGBASE_FOLDER.Length + 1);
                    else
                        return path;
                });
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(MenuIllustration_LoadFile_string)} failed!");
            }
        }

        // Override building custom scenes
        private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);

            if (CustomScene.Registry.TryGet(self.sceneID, out CustomScene customScene))
            {
                // Add images
                bool flatmode = self.flatMode && customScene.Images.Any(img => img.Flatmode);
                self.sceneFolder = customScene.SceneFolder ?? "";

                foreach (var img in customScene.Images)
                {
                    if (img.Flatmode != flatmode)
                        continue;

                    if (img.Depth != -1f)
                        self.AddIllustration(new MenuDepthIllustration(self.menu, self, SLUGBASE_FOLDER, Path.Combine(self.sceneFolder, img.Name), img.Position, img.Depth, img.Shader));
                    else
                        self.AddIllustration(new MenuIllustration(self.menu, self, SLUGBASE_FOLDER, Path.Combine(self.sceneFolder, img.Name), img.Position, false, false));
                }

                if(self is InteractiveMenuScene interactiveScene)
                {
                    interactiveScene.idleDepths.AddRange(customScene.IdleDepths);
                }
            }
        }
        
        #region Slideshow Stuff
            // These hooks assign the SlideShowID to the ProcessManager.nextSlideshow
            #region Assign the next slideshowID
            public static void SlugcatSelectMenu_StartGame(ILContext il)
            {
                ILCursor cursor = new(il);
                ILLabel label = il.DefineLabel();

                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdarg(1), i => i.MatchLdsfld<SlugcatStats.Name>("White")))
                {
                    return;
                };

                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate((SlugcatStats.Name storyGameCharacter) => {
                    if (SlugBaseCharacter.TryGet(storyGameCharacter, out var chara) && IntroScene.TryGet(chara, out var newScene)) {
                        return true;
                    }
                    return false;
                });
                cursor.Emit(OpCodes.Brtrue_S, label);

                if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdsfld<SlideShowID>("WhiteIntro"), i => i.MatchStfld<ProcessManager>("nextSlideshow"))) {
                    return;
                }

                cursor.MarkLabel(label);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate((SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter) =>
                {
                    if (SlugBaseCharacter.TryGet(storyGameCharacter, out var chara) && IntroScene.TryGet(chara, out var newSlideShowID)) {
                        self.manager.nextSlideshow = newSlideShowID;
                        self.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set<string>($"menu_select_scene_alt_{storyGameCharacter.value}", null);
                    }
                });
            }
            public static void RainWorldGame_ExitToVoidSeaSlideShow(ILContext il)
            {
                ILCursor c = new(il);
                
                c.GotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<ProcessManager>("RequestMainProcessSwitch"));
                c.GotoPrev(MoveType.Before, i => i.MatchLdarg(0));

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame self) =>
                {
                    if (SlugBaseCharacter.TryGet(self.StoryCharacter, out var chara) && OutroScene.TryGet(chara, out var OutroSlideShow))
                    {
                        self.manager.nextSlideshow = OutroSlideShow;
                        self.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set<string>($"menu_select_scene_alt_{chara.Name.value}", null);
                    }
                });
            }
            #endregion

            // These hooks assign the scenes, images, ect, actually build stuff for the slideshows specifically
            #region Build the Slideshow
            public static void SlideShow_ctor(ILContext il)
            {
                ILCursor cursor = new(il);

                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdsfld<SlideShowID>("WhiteIntro"))) {
                    return;
                }
                
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate((SlideShow self, ProcessManager manager, SlideShowID slideShowID) =>
                {
                    if (CustomSlideshow.Registry.TryGet(slideShowID, out var customSlideshow))
                    {
                        try {
                            if (manager.musicPlayer != null && customSlideshow.Music != null)
                            {
                                self.waitForMusic = customSlideshow.Music.Name;
                                self.stall = false;
                                manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, customSlideshow.Music.FadeIn);
                            }
                        }
                        catch (Exception err) {
                            SlugBasePlugin.Logger.LogError($"Slugbase music loading error\n{err}");
                        }

                        foreach (var scene in customSlideshow.Scenes) {
                            self.playList.Add(new Scene(scene.ID, scene.StartAt, scene.FadeInDoneAt, scene.FadeOutStartAt));
                        }
                        self.processAfterSlideShow = customSlideshow.Process;
                    }
                });
            }
            public static void SlideShowMenuScene_ctor(On.Menu.SlideShowMenuScene.orig_ctor orig, SlideShowMenuScene self, Menu.Menu menu, MenuObject owner, SceneID sceneID)
            {
                orig(self, menu, owner, sceneID);
                if (!self.flatMode && self.menu is SlideShow slideShow && CustomSlideshow.Registry.TryGet(slideShow.slideShowID, out var customSlideshow))
                {
                    var scene = Array.Find(customSlideshow.Scenes, scene => scene.ID == sceneID);
                    if (scene != null)
                    {
                        foreach (var move in scene.Movement) {
                            // Unsure what exactly the z value does here
                            self.cameraMovementPoints.Add(new (-move.x, -move.y, -1f));
                        }
                    }
                    else { Debug.LogError($"Slugbase could not find matching CustomSlideshowScene with matching ID {sceneID}"); }
                }
            }
            public static void MenuScene_BuildScene_IntroOutroSlideshow(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
            {
                orig(self);
                if (self.menu is SlideShow slideShow && CustomSlideshow.Registry.TryGet(slideShow.slideShowID, out var customSlideshow))
                {
                    self.sceneFolder = customSlideshow.SlideshowFolder;
                    var scene = Array.Find(customSlideshow.Scenes, scene => scene.ID == self.sceneID);
                    if (scene != null)
                    {
                        foreach (var image in scene.Images) {
                            if (self.flatMode)
                            {
                                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, image.Name, image.Position+new Vector2(683,384), false, true));
                            }
                            else
                            {
                                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, image.Name, image.Position, image.Depth, image.Shader));
                            }
                        }
                    }
                    else { Debug.LogError($"Slugbase could not find matching CustomSlideshowScene with matching ID {self.sceneID}"); }
                }
            }
            #endregion

            #region Dream Hooks
            private static void RainWorldGame_Win(ILContext il)
            {
                ILCursor cursor = new ILCursor(il);
                ILLabel label = il.DefineLabel();

                if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdsfld<ModManager>("MSC")))
                {
                    SlugBasePlugin.Logger.LogError("Slugbase cursor could not move 1");
                    return;
                }
                if (!cursor.TryGotoNext(moveType: MoveType.Before, i => i.MatchLdsfld<ModManager>("MSC")))
                {
                    SlugBasePlugin.Logger.LogError("Slugbase cursor could not move 2");
                    return;
                }

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate((RainWorldGame self) => {
                    if (self.GetStorySession.saveState.dreamsState?.eventDream != null
                        && SlugBaseCharacter.TryGet(self.StoryCharacter, out var chara)
                        && HasDreams.TryGet(chara, out bool dreams)
                        && CustomScene.Registry.TryGet(self.GetStorySession.saveState.dreamsState.eventDream.DreamIDToSceneID(), out var dream))
                    {
                        //self.GetStorySession.saveState.dreamsState.upcomingDream = self.GetStorySession.saveState.dreamsState.eventDream;
                        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Dream);
                        return true;
                    }
                    return false;
                });
                cursor.Emit(OpCodes.Brfalse_S, label);
                cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(label);
            }

            // Without this hook, the eventDream dreamID could be overritten when visiting Moon or Pebbles for the first time. But it should still respect other custom dreams if they are not found in the CustomScene.Registry
            private static void DreamsState_InitiateEventDream(On.DreamsState.orig_InitiateEventDream orig, DreamsState self, DreamID evDreamID)
            {
                orig(self, evDreamID);
                // Comparing against evDreamID is the same as saving it's value and comparing against that, so just... don't do that and use evDreamID lol
                if (self.eventDream != evDreamID && CustomScene.Registry.TryGet(evDreamID.DreamIDToSceneID(), out var dreamScene) && dreamScene.OverrideDream)
                {
                    self.eventDream = evDreamID;
                }
            }
            private static SceneID SceneID_SceneFromDream(On.Menu.DreamScreen.orig_SceneFromDream orig, DreamScreen self, DreamID dreamID)
            {
                // Could use IL, but running orig first and returning later it's value later is easier (and technically better for compatability?)
                SceneID origSceneID = orig(self, dreamID);
                if (self.manager.oldProcess is RainWorldGame rainGame && SlugBaseCharacter.TryGet(rainGame.StoryCharacter, out var chara) && HasDreams.TryGet(chara, out bool dreams) && CustomScene.Registry.TryGet(dreamID.DreamIDToSceneID(), out var dream))
                {
                    return dreamID.DreamIDToSceneID();
                }
                return origSceneID;
            }
            #endregion

            /// <summary>
            /// Turns a DreamID into a SceneID with the same string value
            /// </summary>
            private static SceneID DreamIDToSceneID(this DreamID dreamID)
            {
                return new (dreamID.value);
            }
        #endregion
        
        #region Jolly Menu Color Stuff

            #region Creation of more sliders and save their data
            private static void On_ColorChangeDialog_ctor(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ctor orig, ColorChangeDialog self, JollySetupDialog jollyDialog, SlugcatStats.Name playerName, int playerNumber, ProcessManager manager, List<string> names)
            {
                orig(self, jollyDialog, playerName, playerNumber, manager, names);
                if (SlugBaseCharacter.TryGet(playerName, out var chara) && Features.PlayerFeatures.CustomColors.TryGet(chara, out DataTypes.ColorSlot[] colors) && colors.Length > 3) {
                    // Clear the list, becasue at this point it only holds old sliders that were already supposed to be destroyed.
                    NewSliders[playerNumber].Clear();
                    self.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().TryGet($"Custom_Colors_{playerNumber}", out List<string> stringLoadedColors);
                    List<Color> loadedColorsList = stringLoadedColors.StringListToColorList();
                    // Loop through all the colors the base game can not handle itself.
                    for (int i = 3; i < colors.Length; i++) {
                        ColorChangeDialog.ColorSlider sliderRef = null;
                        self.AddSlider(ref sliderRef, jollyDialog.Translate(colors[i].Name), new Vector2(135 + 140*(i%3), 90 - 100*Mathf.Max((i-5)%3, 0f)), self.playerNumber, i);
                        // Save new slider for easy access elswhere, though the same could be accomplished using the menu's page[0].subObjects List I guess.
                        NewSliders[playerNumber].Add(sliderRef);

                        // Add controller support
                        NewSliders[playerNumber][i-3].litSlider.nextSelectable[3] = self.okButton;
                        self.okButton.nextSelectable[i%3] = NewSliders[playerNumber][i-3].litSlider;
                        // Get the ColorSlider of which values we need to change
                        ColorChangeDialog.ColorSlider previousColorPicker = i-3==0? self.body : i-3==1? self.face : i-3==2? self.unique : NewSliders[playerNumber][i-6];
                        NewSliders[playerNumber][i-3].hueSlider.nextSelectable[1] = previousColorPicker.litSlider;
                        previousColorPicker.litSlider.nextSelectable[3] = NewSliders[playerNumber][i-3].hueSlider;

                        // If the color list is less than the custom slugcat's color amount (because first time generating or a different slugcat is selected), add the extra colors.
                        if (BodyColors[playerNumber].Count < colors.Length-3) {
                            if (loadedColorsList != null && i < loadedColorsList.Count) {
                                BodyColors[playerNumber].Add(loadedColorsList[i-3]);
                            }
                            else {
                                BodyColors[playerNumber].Add(colors[BodyColors[playerNumber].Count+3].Default);
                            }
                        }
                        NewSliders[playerNumber][i-3].color = BodyColors[playerNumber][i-3];
                        NewSliders[playerNumber][i-3].RGB2HSL();
                    }
                    // Clear out extra colors, just in case, so that they aren't accessed by accident.
                    if (BodyColors[playerNumber].Count > NewSliders[playerNumber].Count) {
                        BodyColors[playerNumber].RemoveRange(NewSliders[playerNumber].Count, BodyColors[playerNumber].Count-NewSliders[playerNumber].Count);
                    }
                }
            }
            private static void ColorChangeDialog_ActualSavingColor(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ActualSavingColor orig, ColorChangeDialog self)
            {
                orig(self);
                if (SlugBaseCharacter.TryGet(self.playerClass, out var chara) && Features.PlayerFeatures.CustomColors.TryGet(chara, out DataTypes.ColorSlot[] colors) && colors.Length > 3) {
                    for (int i = 3; i < colors.Length; i++) {
                        // Assign the colors of the sliders to the bodycolors
                        NewSliders[self.playerNumber][i-3].HSL2RGB();
                        BodyColors[self.playerNumber][i-3] = NewSliders[self.playerNumber][i-3].color;
                    }
                    // Save this to match normal Jolly Color behavior
                    self.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set<List<string>>($"Custom_Colors_{self.playerNumber}", BodyColors[self.playerNumber].ColorListToStringList());
                }
            }
            private static void ColorChangeDialog_Singal(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_Singal orig, ColorChangeDialog self, MenuObject sender, string message)
            {
                orig(self, sender, message);
                if (SlugBaseCharacter.TryGet(self.playerClass, out var chara) && Features.PlayerFeatures.CustomColors.TryGet(chara, out DataTypes.ColorSlot[] colors) && colors.Length > 3) {
                    for (int i = 0; i < colors.Length-3; i++) {
                        if (message.StartsWith("RESET_COLOR_DIALOG_")) {
                            // Reset to defaults for the custom slugcat
                            BodyColors[self.playerNumber][i] = colors[i+3].Default;
                        }
                        // Load the previous colors into the new sliders.
                        NewSliders[self.playerNumber][i].color = BodyColors[self.playerNumber][i];
                        NewSliders[self.playerNumber][i].RGB2HSL();
                    }
                }
            }
            #endregion

            #region Assign values to the correct sliders
            private static void ColorChangeDialog_ValueOfSlider(ILContext il)
            {
                var cursor = new ILCursor(il);
                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdloc(2))) {
                    return;
                }
                if (!cursor.TryGotoPrev(MoveType.After, i => i.MatchLdloc(out _))) {
                    return;
                }
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate((ColorChangeDialog.ColorSlider colorSlider, ColorChangeDialog self, int num) => {
                    if (num >= 3) {
                        colorSlider = NewSliders[self.playerNumber][num-3];
                    }
                    return colorSlider;
                });
            }
            private static void ColorChangeDialog_SliderSetValue(ILContext il)
            {
                var cursor = new ILCursor(il);
                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdloc(3))) {
                    return;
                }
                if (!cursor.TryGotoPrev(MoveType.After, i => i.MatchLdloc(out _))) {
                    return;
                }
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate((ColorChangeDialog.ColorSlider colorSlider, ColorChangeDialog self, int num) => {
                    if (num >= 3) {
                        colorSlider = NewSliders[self.playerNumber][num-3];
                    }
                    return colorSlider;
                });
            }
            #endregion

            #region Adjust UI Positions
            // Change position of original sliders
            private static void ColorChangeDialog_AddSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_AddSlider orig, ColorChangeDialog self, ref ColorChangeDialog.ColorSlider slider, string labelString, Vector2 position, int playerNumber, int bodyPart)
            {
                // Use size of custom colors in json to determine height
                if (SlugBaseCharacter.TryGet(self.playerClass, out var chara) && Features.PlayerFeatures.CustomColors.TryGet(chara, out DataTypes.ColorSlot[] colors) && colors.Length > 3) {
                    // Based on how many colors there are, adjust the y position.
                    position.y += 50;
                    if (colors.Length > 6) {
                        position.y += 50;
                    }
                }
                orig(self, ref slider, labelString, position, playerNumber, bodyPart);
            }
            // Resize the background box and change position of the reset button
            private static void IL_ColorChangeDialog_ctor(ILContext il)
            {
                var cursor = new ILCursor(il);

                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchNewobj<Vector2>())) {
                    return;
                }
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate((float f, SlugcatStats.Name name) => {
                    if (SlugBaseCharacter.TryGet(name, out var chara) && Features.PlayerFeatures.CustomColors.TryGet(chara, out DataTypes.ColorSlot[] colors) && colors.Length > 3) {
                        f += 200;
                        if (colors.Length > 6) {
                            f += 200;
                        }
                    }
                    return f;
                });

                cursor.Index++;

                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchNewobj<Vector2>())) {
                    return;
                }
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate((float f, SlugcatStats.Name playerClass) => {
                    if (SlugBaseCharacter.TryGet(playerClass, out var chara) && Features.PlayerFeatures.CustomColors.TryGet(chara, out DataTypes.ColorSlot[] colors) && colors.Length > 3) {
                        f += 100f;
                        if (colors.Length > 6) {
                            f += 100f;
                        }
                    }
                    return f;
                });
            }
            #endregion

        #endregion
    }
}