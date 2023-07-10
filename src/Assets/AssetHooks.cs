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
            
            // Slideshow hooks
            IL.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;
            IL.RainWorldGame.ExitToVoidSeaSlideShow += RainWorldGame_ExitToVoidSeaSlideShow;
            IL.Menu.SlideShow.ctor += SlideShow_ctor;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene_IntroOutroSlideshow;

            // Dream hooks
            IL.RainWorldGame.Win += RainWorldGame_Win;
            On.Menu.DreamScreen.SceneFromDream += SceneID_SceneFromDream;
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
        
        // These hooks assign the SlideShowID to the ProcessManager.nextSlideshow
        #region Assign the next slideshowID
        public static void SlugcatSelectMenu_StartGame(ILContext il)
        {
            ILCursor cursor = new(il);
            ILLabel label = il.DefineLabel();

            cursor.GotoNext(MoveType.Before, i => i.MatchLdarg(1), i => i.MatchLdsfld<SlugcatStats.Name>("White"));

            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate((SlugcatStats.Name storyGameCharacter) => {
                Debug.Log("Slugbase start game slideshow hook");
                if (SlugBaseCharacter.TryGet(storyGameCharacter, out var chara) && IntroScene.TryGet(chara, out var newScene)) {
                    Debug.Log("Slugbase Did thing 1 part 1");
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
                    // Reason why the slideshows can't use MenuScene I believe (Maybe I could just change it to make it work, but it works now fine and that would still then use a foreach loop here anyway)
                    Debug.Log("Slugbase Did thing 1 part 2");
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
                if (manager.oldProcess is Menu.SlugcatSelectMenu slugcatmenu)
                {
                    name = slugcatmenu.slugcatPages[slugcatmenu.slugcatPageIndex].slugcatNumber;
                }
                if (manager.oldProcess is RainWorldGame rainGame)
                {
                    name = rainGame.StoryCharacter;
                }
                if (SlugBaseCharacter.TryGet(name, out var chara)) {
                    if (IntroScene.TryGet(chara, out var newIntroSlideShowID) && slideShowID == newIntroSlideShowID && CustomSlideshow.Registry.TryGet(newIntroSlideShowID, out var customIntroSlideshow))
                    {
                        Debug.Log($"Slugbase: (Cutscenes) Playing slideshow {newIntroSlideShowID}\nIs Intro");
                        try {
                            if (manager.musicPlayer != null && customIntroSlideshow.Music.Name != "")
                            {
                                self.waitForMusic = customIntroSlideshow.Music.Name;
                                self.stall = false;
                                manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, customIntroSlideshow.Music.FadeIn);
                            }
                        }
                        catch (Exception err) {
                            SlugBasePlugin.Logger.LogError($"Slugbase music loading error\n{err}");
                        }

                        // Would maybe be less confusing if some maths were done here for the timing so that the user can just put times relative to the previous images' times, but idk
                        foreach (var scene in customIntroSlideshow.Scenes) {
                            Debug.Log($"Slugbase added new scene Slugbase_{chara.Name.value}_{customIntroSlideshow.ID}_{scene.Name}_intro");
                            // Append stuff to the name so that each one is unique, even if it's not in the json
                            self.playList.Add(new Scene(new SceneID( $"Slugbase_{chara.Name.value}_{customIntroSlideshow.ID}_{scene.Name}_intro", false), self.ConvertTime(0, scene.StartAt, 0), self.ConvertTime(0, scene.FadeInDoneAt, 0), self.ConvertTime(0, scene.FadeOutStartAt, 0)));
                        }
                        self.processAfterSlideShow = ProcessManager.ProcessID.Game;
                    }
                    if (OutroScenes.TryGet(chara, out var newOutroSlideShowIDList))
                    {
                        foreach (var newOutroSlideShowID in newOutroSlideShowIDList)
                        {
                            if (slideShowID == newOutroSlideShowID && slideShowID == newOutroSlideShowID && CustomSlideshow.Registry.TryGet(newOutroSlideShowID, out var customOutroSlideshow))
                            {
                                Debug.Log($"Slugbase: (Cutscenes) Playing slideshow {newIntroSlideShowID}\nIs Outro");
                                try {
                                    if (manager.musicPlayer != null && customOutroSlideshow.Music.Name != "")
                                    {
                                        self.waitForMusic = customOutroSlideshow.Music.Name;
                                        self.stall = false;
                                        manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, customOutroSlideshow.Music.FadeIn);
                                    }
                                }
                                catch (Exception err) {
                                    SlugBasePlugin.Logger.LogError($"Slugbase music loading error\n{err}");
                                }

                                // Would maybe be less confusing if I did some maths here for the timeing so that the user can just put times relative to the previous image's times, but idk
                                foreach (var scene in customOutroSlideshow.Scenes) {
                                    Debug.Log($"Slugbase added new scene Slugbase_{chara.Name.value}_{customOutroSlideshow.ID}_{scene.Name}");
                                    // Append stuff to the name so that each one is unique, even if it's not in the json
                                    self.playList.Add(new Scene(new SceneID( $"Slugbase_{chara.Name.value}_{customOutroSlideshow.ID}_{scene.Name}", false), self.ConvertTime(0, scene.StartAt, 0), self.ConvertTime(0, scene.FadeInDoneAt, 0), self.ConvertTime(0, scene.FadeOutStartAt, 0)));
                                }
                                if (customOutroSlideshow.Credits)
                                {
                                    self.processAfterSlideShow = ProcessManager.ProcessID.Credits;
                                }
                                else
                                {
                                    self.processAfterSlideShow = ProcessManager.ProcessID.Statistics;
                                }
                            }
                        }
                    }
                }
            });
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
                if (IntroScene.TryGet(chara, out var newIntroSlideShowID) && (self.menu is SlideShow slideShow) && slideShow.slideShowID == newIntroSlideShowID && CustomSlideshow.Registry.TryGet(newIntroSlideShowID, out var customIntroSlideshow))
                {
                    foreach (var scene in customIntroSlideshow.Scenes)
                    {
                        Debug.Log("Slugbase checking if sceneID matches");
                        if (new SceneID($"Slugbase_{chara.Name.value}_{customIntroSlideshow.ID}_{scene.Name}_intro", false) == self.sceneID)
                        {
                            Debug.Log($"Slugbase now Playing: Slugbase_{chara.Name.value}_{customIntroSlideshow.ID}_{scene.Name}_intro");
                            foreach (var image in scene.Images) {
                                self.sceneFolder = customIntroSlideshow.SceneFolder;
                                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, image.Name, image.Position, false, true));
                                Debug.Log($"Slugbase added image {image.Name} to scene {scene.Name}");
                            }
                        }
                    }
                }
                if (OutroScenes.TryGet(chara, out var newOutroSlideShowIDList))
                {
                    foreach (var newOutroSlideShowID in newOutroSlideShowIDList)
                    {
                        if (CustomSlideshow.Registry.TryGet(newOutroSlideShowID, out var customOutroSlideshow) && (self.menu is SlideShow outroShow) && outroShow.slideShowID == newOutroSlideShowID)
                        {
                            foreach (var scene in customOutroSlideshow.Scenes)
                            {
                                Debug.Log("Slugbase checking if sceneID matches");
                                if (new SceneID($"Slugbase_{chara.Name.value}_{customOutroSlideshow.ID}_{scene.Name}", false) == self.sceneID)
                                {
                                    Debug.Log($"Slugbase now Playing: Slugbase_{chara.Name.value}_{customOutroSlideshow.ID}_{scene.Name}");
                                    foreach (var image in scene.Images)
                                    {
                                        self.sceneFolder = customOutroSlideshow.SceneFolder;
                                        self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, image.Name, image.Position, false, true));
                                        Debug.Log($"Slugbase added image {image.Name} to scene ");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion
        
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
                    Debug.Log($"Slugbase force dream {CustomScene.nextDreamID}");
                    self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Dream);
                    return true;
                }
                Debug.Log($"Slugbase no match for DreamID {CustomScene.nextDreamID}.");
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
                Debug.Log($"Slugbase new scene from dream {dreamID.value}");
                CustomScene.QueueDream("");
                return new SceneID(dreamID.value, false);
            }
            Debug.Log("Slugbase returning normal SceneID");
            if (CustomScene.nextDreamID != "") Debug.LogError($"dreamID ({dreamID}) did not match {CustomScene.nextDreamID}");  // This should realistically never trigger (probably)
            return origSceneID;
        }
        #endregion
    }
}
