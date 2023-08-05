using System;
using System.IO;
using System.Linq;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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
            On.Menu.SlideShowMenuScene.ctor += SlideShowMenuScene_ctor;

            CustomDreams.Apply();
        }

        // Use paths as atlas names instead of just the file name
        private static void MenuIllustration_LoadFile_string(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<AssetManager>(nameof(AssetManager.ResolveFilePath))))
            {
                c.EmitDelegate<Func<string, string>>(path =>
                {
                    if (path.StartsWith(SLUGBASE_FOLDER + Path.DirectorySeparatorChar))
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

        // Override building custom scenes, including slideshow scenes
        private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);

            CustomScene customScene;

            // Slideshow scenes
            if (self.menu is SlideShow slideShow && CustomSlideshow.Registry.TryGet(slideShow.slideShowID, out var customSlideshow))
            {
                // Add images
                customScene = customSlideshow.GetScene(self.sceneID);

                if (customScene != null)
                {
                    self.sceneFolder = customScene.SceneFolder ?? customSlideshow.SlideshowFolder ?? "";
                    AddImages(self, customScene);
                }
                else
                {
                    SlugBasePlugin.Logger.LogError($"Could not find slideshow scene with ID {self.sceneID}!");
                }
            }

            // Normal scenes
            else if (CustomScene.Registry.TryGet(self.sceneID, out customScene))
            {
                // Add images
                self.sceneFolder = customScene.SceneFolder ?? "";
                AddImages(self, customScene);
            }

            static void AddImages(MenuScene self, CustomScene scene)
            {
                bool flatmode = self.flatMode && scene.Images.Any(img => img.Flatmode);

                foreach (var img in scene.Images)
                {
                    if (img.Flatmode != flatmode)
                        continue;

                    if (img.Depth != -1f)
                        self.AddIllustration(new MenuDepthIllustration(self.menu, self, SLUGBASE_FOLDER, Path.Combine(self.sceneFolder, img.Name), img.Position, img.Depth, img.Shader));
                    else
                        self.AddIllustration(new MenuIllustration(self.menu, self, SLUGBASE_FOLDER, Path.Combine(self.sceneFolder, img.Name), img.Position, false, false));
                }

                if (self is InteractiveMenuScene interactiveScene)
                {
                    interactiveScene.idleDepths.AddRange(scene.IdleDepths);
                }
            }
        }

        // Go to intro slideshow when starting the game
        public static void SlugcatSelectMenu_StartGame(ILContext il)
        {
            ILCursor c = new(il);

            // Check for slugcats with custom slideshows
            ILLabel postNameCheck = null;
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<SlugcatStats.Name>(nameof(SlugcatStats.Name.White)),
                x => x.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Inequality")),
                x => x.MatchBrfalse(out postNameCheck)))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((SlugcatStats.Name storyGameCharacter) =>
                {
                    return SlugBaseCharacter.TryGet(storyGameCharacter, out var chara)
                        && IntroScene.TryGet(chara, out _);
                });
                c.Emit(OpCodes.Brtrue, postNameCheck);
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(SlugcatSelectMenu_StartGame)}, match name, failed!");
                return;
            }

            // Set slideshow ID
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<SlideShowID>(nameof(SlideShowID.WhiteIntro)),
                x => x.MatchStfld<ProcessManager>(nameof(ProcessManager.nextSlideshow))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter) =>
                {
                    if (SlugBaseCharacter.TryGet(storyGameCharacter, out var chara) && IntroScene.TryGet(chara, out var newSlideShowID))
                    {
                        self.manager.nextSlideshow = newSlideShowID;
                    }
                });
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(SlugcatSelectMenu_StartGame)}, set next slideshow, failed!");
                return;
            }
        }

        // Go to outro slideshow when ascending
        public static void RainWorldGame_ExitToVoidSeaSlideShow(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(x => x.MatchCallOrCallvirt<ProcessManager>(nameof(ProcessManager.RequestMainProcessSwitch)))
                && c.TryGotoPrev(x => x.MatchLdarg(0)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame self) =>
                {
                    if (SlugBaseCharacter.TryGet(self.StoryCharacter, out var chara) && OutroScene.TryGet(chara, out var OutroSlideShow))
                    {
                        self.manager.nextSlideshow = OutroSlideShow;
                    }
                });
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(RainWorldGame_ExitToVoidSeaSlideShow)} failed!");
            }
        }

        // Build custom slideshows
        public static void SlideShow_ctor(ILContext il)
        {
            ILCursor cursor = new(il);

            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchStfld<SlideShow>(nameof(SlideShow.playList))))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate((SlideShow self, ProcessManager manager, SlideShowID slideShowID) =>
                {
                    if (CustomSlideshow.Registry.TryGet(slideShowID, out var customSlideshow))
                    {
                        try
                        {
                            if (manager.musicPlayer != null && customSlideshow.Music != null)
                            {
                                self.waitForMusic = customSlideshow.Music.Name;
                                self.stall = false;
                                manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, customSlideshow.Music.FadeIn);
                            }
                        }
                        catch (Exception e)
                        {
                            SlugBasePlugin.Logger.LogError($"Failed to load slideshow music!\n{e}");
                        }

                        foreach (var scene in customSlideshow.Scenes)
                        {
                            self.playList.Add(new Scene(scene.ID, scene.StartAt, scene.FadeInDoneAt, scene.FadeOutStartAt));
                        }
                        self.processAfterSlideShow = customSlideshow.Process;
                    }
                });
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(SlideShow_ctor)} failed!");
            }
        }

        // Determine slideshow camera path
        public static void SlideShowMenuScene_ctor(On.Menu.SlideShowMenuScene.orig_ctor orig, SlideShowMenuScene self, Menu.Menu menu, MenuObject owner, SceneID sceneID)
        {
            orig(self, menu, owner, sceneID);
            if (!self.flatMode && self.menu is SlideShow slideShow && CustomSlideshow.Registry.TryGet(slideShow.slideShowID, out var customSlideshow))
            {
                var scene = customSlideshow.GetScene(sceneID);

                if (scene != null)
                {
                    self.cameraMovementPoints.AddRange(scene.CameraMovement);
                }
                else
                {
                    SlugBasePlugin.Logger.LogError($"Could not find slideshow scene with ID {sceneID}!");
                }
            }
        }
    }
}