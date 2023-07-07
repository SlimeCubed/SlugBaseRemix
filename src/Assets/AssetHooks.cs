using System;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
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
            IL.Menu.MenuIllustration.LoadFile_string += MenuIllustration_LoadFile_string;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
            
            IL.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;
            IL.Menu.SlideShow.ctor += SlideShow_ctor;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene_Intro;
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
        public static void SlugcatSelectMenu_StartGame(ILContext il)
        {
            ILCursor cursor = new(il);
            ILLabel label = il.DefineLabel();

            cursor.GotoNext(MoveType.Before, i => i.MatchLdarg(1), i => i.MatchLdsfld<SlugcatStats.Name>("White"));

            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate((SlugcatStats.Name storyGameCharacter) => {
                if (SlugBaseCharacter.TryGet(storyGameCharacter, out var chara) && IntroScene.TryGet(chara, out var newScene)) {
                    Debug.Log("Did thing 1 part 1");
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
                if (SlugBaseCharacter.TryGet(storyGameCharacter, out var chara) && IntroScene.TryGet(chara, out var newScene)) {
                    // Reason why the slideshows can't use MenuScene I believe (Maybe I could just change it to make it work, but it works now fine and that would still then use a foreach loop here anyway)
                    Debug.Log("Did thing 1 part 2");
                    self.manager.nextSlideshow = newScene;
                }
            });
        }
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
                // Unsure if looping is better, or trying to grab the SlugcatStats.Name from manager.sceneSlot or RWCustom.Custom.rainWorld.lastActiveSaveSlot
                // I feel looping since it won't ever be accidentally null or the wrong value, and prevent the scene from playing at all
                foreach(var chara in SlugBaseCharacter.Registry.Values) {
                    if (IntroScene.TryGet(chara, out var newScene) && slideShowID == newScene && CustomIntroOutroScene.Registry.TryGet(newScene, out var customIntroOutroScene)) {
                        Debug.Log($"Slugbase: (Cutscenes) Playing slideshow {newScene}");

                        try {
                            if (manager.musicPlayer != null && customIntroOutroScene.Music.Name != "")
                            {
                                self.waitForMusic = customIntroOutroScene.Music.Name;
                                self.stall = false;
                                manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, customIntroOutroScene.Music.FadeIn);
                            }
                        }
                        catch (Exception err) {
                            SlugBasePlugin.Logger.LogError($"Slugbase music loading error\n{err}");
                        }

                        // Would maybe be less confusing if I did some maths here for the timeing so that the user can just put times relative to the previous image's times, but idk
                        foreach (var image in customIntroOutroScene.Images) {
                            Debug.Log($"Slugbase added new scene {image.Name}");
                            self.playList.Add(new Scene(new SceneID(image.Name, false), self.ConvertTime(0, image.StartAt, 0), self.ConvertTime(0, image.FadeInDoneAt, 0), self.ConvertTime(0, image.FadeOutStartAt, 0)));
                        }
                        self.processAfterSlideShow = ProcessManager.ProcessID.Game;
                        break;
                    }
                }
            });
        }
        public static void MenuScene_BuildScene_Intro(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);

            // I do not know of a way to get the SlugcatStats.Name reliably in this method, so a loop through all in the Registry to match them to a value in the IntroScene field is necessary
            foreach(var chara in SlugBaseCharacter.Registry.Values) {
                if (IntroScene.TryGet(chara, out var newScene) && CustomIntroOutroScene.Registry.TryGet(newScene, out var customIntroOutroScene)) {
                    foreach (var image in customIntroOutroScene.Images) {
                        Debug.Log($"Slugbase: {customIntroOutroScene.Images}");
                        if (new SceneID(image.Name, false) == self.sceneID)
                        {
                            Debug.Log("Now replacing scene");
                            self.sceneFolder = customIntroOutroScene.SceneFolder;
                            //Debug.Log($"Slugbase Log Path: {self.sceneFolder}");
                            self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, image.Name, image.Position, false, true));
                        }
                    }
                }
            }
        }
    }
}
