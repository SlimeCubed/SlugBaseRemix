using System;
using System.IO;
using System.Linq;
using System.Xml.Schema;
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
                if (SlugBaseCharacter.TryGet(self.StoryCharacter, out var chara) && OutroScene.TryGet(chara, out var OutroSlideShow))
                {
                    self.manager.nextSlideshow = OutroSlideShow;
                    self.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set<string>("menu_select_scene_alt", null);
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
    }
}
