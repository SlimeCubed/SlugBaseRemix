using System;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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

            // Clean the next Dream
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            On.Player.Die += Player_Die;
        }

        #region Clear the upcoming dream if the player exits the campaign or dies
        private static void Player_Die(On.Player.orig_Die orig, Player self)
        {
            orig(self);
            CustomScene.QueueDream("");
        }
        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);
            CustomScene.QueueDream("");
        }
        #endregion
        
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
                if (SlugBaseCharacter.TryGet(self.StoryCharacter, out var chara) && OutroScenes.TryGet(chara, out var newOutroSSIDs))
                {
                    self.manager.nextSlideshow = newOutroSSIDs[0];
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
                SlugcatStats.Name name = null;
                if (manager.oldProcess is Menu.SlugcatSelectMenu slugcatmenu) { name = slugcatmenu.slugcatPages[slugcatmenu.slugcatPageIndex].slugcatNumber; }
                if (manager.oldProcess is RainWorldGame rainGame) { name = rainGame.StoryCharacter; }
                if (SlugBaseCharacter.TryGet(name, out var chara)) {
                    if (IntroScene.TryGet(chara, out var newIntroSlideShowID) && slideShowID == newIntroSlideShowID && CustomSlideshow.Registry.TryGet(newIntroSlideShowID, out var customIntroSlideshow))
                    {
                        BuildSlideShowScenes(self, customIntroSlideshow, manager, chara.Name.value, true);
                    }
                    if (OutroScenes.TryGet(chara, out var newOutroSlideShowIDList))
                    {
                        foreach (var newOutroSlideShowID in newOutroSlideShowIDList)
                        {
                            if (slideShowID == newOutroSlideShowID && slideShowID == newOutroSlideShowID && CustomSlideshow.Registry.TryGet(newOutroSlideShowID, out var customOutroSlideshow))
                            {
                                BuildSlideShowScenes(self, customOutroSlideshow, manager, chara.Name.value, false);
                            }
                        }
                    }
                }
            });
        }
        // This one is for just adding the dynamic motion to the images... so much for so little...
        public static void SlideShowMenuScene_ctor(On.Menu.SlideShowMenuScene.orig_ctor orig, SlideShowMenuScene self, Menu.Menu menu, MenuObject owner, SceneID sceneID)
        {
            orig(self, menu, owner, sceneID);

            SlugcatStats.Name name = null;
            if (self.menu.manager.oldProcess is Menu.SlugcatSelectMenu slugcatmenu) {
                name = slugcatmenu.slugcatPages[slugcatmenu.slugcatPageIndex].slugcatNumber;
            }
            if (self.menu.manager.oldProcess is RainWorldGame rainGame)
            {
                name = rainGame.StoryCharacter;
            }
            if (SlugBaseCharacter.TryGet(name, out var chara))
            {
                if (IntroScene.TryGet(chara, out var newIntroSlideShowID) && self.menu is SlideShow slideShow && slideShow.slideShowID == newIntroSlideShowID && CustomSlideshow.Registry.TryGet(newIntroSlideShowID, out var customIntroSlideshow))
                {
                    AddSlideShowMovement(self, sceneID, customIntroSlideshow, chara.Name.value);
                }
                if (OutroScenes.TryGet(chara, out var newOutroSlideShowIDList))
                {
                    foreach (var newOutroSlideShowID in newOutroSlideShowIDList)
                    {
                        if (CustomSlideshow.Registry.TryGet(newOutroSlideShowID, out var customOutroSlideshow) && self.menu is SlideShow outroShow && outroShow.slideShowID == newOutroSlideShowID)
                        {
                            AddSlideShowMovement(self, sceneID, customOutroSlideshow, chara.Name.value);
                        }
                    }
                }
            }
        }
        public static void MenuScene_BuildScene_IntroOutroSlideshow(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);

            SlugcatStats.Name name = null;
            if (self.menu.manager.oldProcess is Menu.SlugcatSelectMenu slugcatmenu) {
                name = slugcatmenu.slugcatPages[slugcatmenu.slugcatPageIndex].slugcatNumber;
            }
            if (self.menu.manager.oldProcess is RainWorldGame rainGame)
            {
                name = rainGame.StoryCharacter;
            }
            if (SlugBaseCharacter.TryGet(name, out var chara)) {
                if (IntroScene.TryGet(chara, out var newIntroSlideShowID) && self.menu is SlideShow slideShow && slideShow.slideShowID == newIntroSlideShowID && CustomSlideshow.Registry.TryGet(newIntroSlideShowID, out var customIntroSlideshow))
                {
                    self.sceneFolder = customIntroSlideshow.SlideshowFolder;
                    AddSlideShowImages(self, customIntroSlideshow, chara.Name.value);
                }
                if (OutroScenes.TryGet(chara, out var newOutroSlideShowIDList))
                {
                    foreach (var newOutroSlideShowID in newOutroSlideShowIDList)
                    {
                        if (CustomSlideshow.Registry.TryGet(newOutroSlideShowID, out var customOutroSlideshow) && self.menu is SlideShow outroShow && outroShow.slideShowID == newOutroSlideShowID)
                        {
                            self.sceneFolder = customOutroSlideshow.SlideshowFolder;
                            AddSlideShowImages(self, customOutroSlideshow, chara.Name.value);
                        }
                    }
                }
            }
        }
        #endregion
        
        // These hooks are for switching to a Dream if CustomScene.nextDreamID is not equal to ""
        #region Dream Hooks
        private static void RainWorldGame_Win(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel label = il.DefineLabel();

            // Just insert at the top of the method, calling near the bottom was presenting some troubles and not switching processes 'correctly' (It went to DreamID.Empty no matter what)
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate((RainWorldGame self) => {
                if (SlugBaseCharacter.TryGet(self.StoryCharacter, out var chara) && HasDreams.TryGet(chara, out bool dreams) && CustomScene.nextDreamID != "")
                {
                    self.GetStorySession.saveState.dreamsState.upcomingDream = new DreamID(CustomScene.nextDreamID, false);
                    self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Dream);
                    return true;
                }
                if (CustomScene.nextDreamID != "") Debug.LogError($"No match found for DreamID: {CustomScene.nextDreamID}");
                return false;
            });
            // If the above it true, don't bother with the rest of the method, just in case it messes something up. (Have not actually tested with a normal dream yet!)
            cursor.Emit(OpCodes.Brfalse_S, label);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(label);
        }
        private static SceneID SceneID_SceneFromDream(On.Menu.DreamScreen.orig_SceneFromDream orig, Menu.DreamScreen self, DreamsState.DreamID dreamID)
        {
            SceneID origSceneID = orig(self, dreamID);
            if (self.manager.oldProcess is RainWorldGame rainGame && SlugBaseCharacter.TryGet(rainGame.StoryCharacter, out var chara) && HasDreams.TryGet(chara, out bool dreams) && dreamID.value == CustomScene.nextDreamID)
            {
                CustomScene.QueueDream("");
                return new SceneID(dreamID.value, false);
            }
            if (CustomScene.nextDreamID != "") Debug.LogError($"dreamID ({dreamID}) did not match {CustomScene.nextDreamID}");  // This should realistically never trigger (probably)
            return origSceneID;
        }
        #endregion
        
        #region Please save yourself from the nesting hell
        private static void BuildSlideShowScenes(SlideShow self, CustomSlideshow customSlideshow, ProcessManager manager, string charaName, bool intro)
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

            // Would maybe be less confusing if some maths were done here for the timing so that the user can just put times relative to the previous images' times, but idk
            foreach (var scene in customSlideshow.Scenes) {
                self.playList.Add(new Scene(new SceneID( $"Slugbase_{charaName}_{customSlideshow.ID}_{scene.Name}_intro", false), self.ConvertTime(0, scene.StartAt, 0), self.ConvertTime(0, scene.FadeInDoneAt, 0), self.ConvertTime(0, scene.FadeOutStartAt, 0)));
            }
            if (intro) { self.processAfterSlideShow = ProcessManager.ProcessID.Game; }
            else if (customSlideshow.Credits) { self.processAfterSlideShow = ProcessManager.ProcessID.Credits; }
            else { self.processAfterSlideShow = ProcessManager.ProcessID.Statistics; }
        }
        private static void AddSlideShowMovement(SlideShowMenuScene self, SceneID sceneID, CustomSlideshow customSlideshow, string charaName)
        {
            foreach (var scene in customSlideshow.Scenes)
            {
                if (new SceneID($"Slugbase_{charaName}_{customSlideshow.ID}_{scene.Name}_intro", false) == sceneID && !self.flatMode)
                {
                    foreach (var move in scene.Movement) {
                        self.cameraMovementPoints.Add(new Vector3(-move.x, -move.y, 0f));
                    }
                }
            }
        }
        private static void AddSlideShowImages(MenuScene self, CustomSlideshow customSlideshow, string charaName)
        {
            foreach (var scene in customSlideshow.Scenes)
            {
                if (new SceneID($"Slugbase_{charaName}_{customSlideshow.ID}_{scene.Name}_intro", false) == self.sceneID)
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
            }
        }
        #endregion
    }
}
