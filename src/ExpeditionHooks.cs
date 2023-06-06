using Expedition;
using UnityEngine;
using MoreSlugcats;
using System.Collections.Generic;
using System;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.IO;

namespace SlugBase
{
    internal class ExpeditionHooks
    {
        public static int maxSlugPages = 0;
        public static int currentSlugPage = 0;

        public static void Apply()
        {
            On.Expedition.ExpeditionData.GetPlayableCharacters += ExpeditionData_GetPlayableCharacters;
            On.Expedition.ExpeditionProgression.CheckUnlocked += ExpeditionProgression_CheckUnlocked;
            IL.Menu.CharacterSelectPage.ctor += CharacterSelectPage_ctorIL;
            On.Menu.CharacterSelectPage.ctor += CharacterSelectPage_ctor;
            On.Menu.CharacterSelectPage.Singal += CharacterSelectPage_Singal;
            On.Menu.CharacterSelectPage.UpdateSelectedSlugcat += CharacterSelectPage_UpdateSelectedSlugcat;
            On.Menu.CharacterSelectPage.GetSlugcatPortrait += CharacterSelectPage_GetSlugcatPortrait;
        }

        // Adding custom slugs to the expedition playable characters
        private static List<SlugcatStats.Name> ExpeditionData_GetPlayableCharacters(On.Expedition.ExpeditionData.orig_GetPlayableCharacters orig)
        {
            maxSlugPages = 0; // Gotta set this back to 0, cause if you for SOME UNGODLY REASON disable then re-enable MSC it creates additional empty pages
            currentSlugPage = 0;
            List<SlugcatStats.Name> list = orig.Invoke();

            list.AddRange(SlugBaseCharacter.Registry.Keys);

            for (int i = 0; i < list.Count; i++)
            {
                if (i % 8 == 0) maxSlugPages++;
            }

            return list;
        }

        private static bool ExpeditionProgression_CheckUnlocked(On.Expedition.ExpeditionProgression.orig_CheckUnlocked orig, ProcessManager manager, SlugcatStats.Name slugcat)
        {
            if (SlugBaseCharacter.TryGet(slugcat, out var chara)) return true;

            return orig.Invoke(manager, slugcat);
        }

        // Adding custom slugs to the expedition select page
        private static void CharacterSelectPage_ctorIL(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchStloc(4)
                ))
            {
                c.Index += 1;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_3);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Action<CharacterSelectPage, int, Menu.Menu>>((self, i, menu) =>
                {
                    if (i > 7)
                    {
                        self.slugcatButtons[i] = new SelectOneButton(menu, self, "", "SLUG-" + i.ToString(), new Vector2(415f + 110f * (float)(i - 3), 410f), new Vector2(94f, 94f), self.slugcatButtons, i);
                        self.subObjects.Add(self.slugcatButtons[i]);
                    }
                });
            }
            else SlugBasePlugin.Logger.LogError($"IL hook {nameof(CharacterSelectPage_ctorIL)} failed!");
        }

        // Adding control arrows and rearranging stuff based on the "pages"
        private static void CharacterSelectPage_ctor(On.Menu.CharacterSelectPage.orig_ctor orig, CharacterSelectPage self, Menu.Menu menu, MenuObject owner, Vector2 pos)
        {
            orig.Invoke(self, menu, owner, pos);

            if (ExpeditionGame.playableCharacters.Count > 8)
            {
                currentSlugPage = 0;
                ConstructPage(self, menu);

                SymbolButton slugLeft = new SymbolButton(menu, self, "Big_Menu_Arrow", "CUSTOMSLUGPAGE_LEFT", new Vector2(330f, 432.5f));
                slugLeft.symbolSprite.rotation = 270f;
                slugLeft.size = new Vector2(45f, 45f);
                slugLeft.roundedRect.size = slugLeft.size;
                self.subObjects.Add(slugLeft);

                SymbolButton slugRight = new SymbolButton(menu, self, "Big_Menu_Arrow", "CUSTOMSLUGPAGE_RIGHT", new Vector2(990f, 432.5f));
                slugRight.symbolSprite.rotation = 90f;
                slugRight.size = new Vector2(45f, 45f);
                slugRight.roundedRect.size = slugRight.size;
                self.subObjects.Add(slugRight);

                menu.selectedObject = self.slugcatButtons[1];
                // Preventing errors for when expedition saved a selected slugcat far in the list and then the mod got disabled
                if ((menu as ExpeditionMenu).currentSelection >= ExpeditionGame.playableCharacters.Count ||
                    (menu as ExpeditionMenu).currentSelection < 0)
                {
                    (menu as ExpeditionMenu).currentSelection = 1;
                }
            }

            //foreach (var scug in ExpeditionGame.unlockedExpeditionSlugcats)
            //{
            //    SlugBasePlugin.Logger.LogMessage(scug);
            //}
        }

        // Rearranging slugcat select buttons based on the current custom slug page
        private static void ConstructPage(CharacterSelectPage self, Menu.Menu menu)
        {
            for (int i = 0; i < ExpeditionGame.playableCharacters.Count; i++)
            {
                self.slugcatButtons[i].RemoveSprites();
                self.RemoveSubObject(self.slugcatButtons[i]);
                self.slugcatButtons[i].pos = new Vector2(10000, 10000); // Throw that off screeeen somewhere (so unwanted portraits dont appear on screen)
            }

            for (int i = currentSlugPage * 8; i < Mathf.Min((currentSlugPage + 1) * 8, ExpeditionGame.playableCharacters.Count); i++)
            {
                // Copied from the original method
                bool greyedOut = !ExpeditionGame.unlockedExpeditionSlugcats.Contains(ExpeditionGame.playableCharacters[i]);
                int positionI = i - (8 * currentSlugPage);
                if (i < 3 + (8 * currentSlugPage))
                {
                    self.slugcatButtons[i] = new SelectOneButton(menu, self, "", "SLUG-" + i.ToString(), new Vector2(525f + 110f * (float)positionI, 525f), new Vector2(94f, 94f), self.slugcatButtons, i);
                    self.subObjects.Add(self.slugcatButtons[i]);
                }
                else if (i >= 3 + (8 * currentSlugPage) && i <= 7 + (8 * currentSlugPage))
                {
                    self.slugcatButtons[i] = new SelectOneButton(menu, self, "", "SLUG-" + i.ToString(), new Vector2(415f + 110f * (float)(positionI - 3), 410f), new Vector2(94f, 94f), self.slugcatButtons, i);
                    self.subObjects.Add(self.slugcatButtons[i]);
                }
                self.slugcatButtons[i].buttonBehav.greyedOut = greyedOut;
            }

            self.ReloadSlugcatPortraits();
        }

        //Controls for the slugcat page switch buttons
        private static void CharacterSelectPage_Singal(On.Menu.CharacterSelectPage.orig_Singal orig, CharacterSelectPage self, MenuObject sender, string message)
        {
            orig.Invoke(self, sender, message);

            if (message == "CUSTOMSLUGPAGE_LEFT")
            {
                currentSlugPage--;
                if (currentSlugPage < 0) currentSlugPage = maxSlugPages - 1;
                ConstructPage(self, self.menu);
                self.menu.PlaySound(SoundID.MENU_Checkbox_Check);
            }

            if (message == "CUSTOMSLUGPAGE_RIGHT")
            {
                currentSlugPage++;
                if (currentSlugPage > maxSlugPages - 1) currentSlugPage = 0;
                ConstructPage(self, self.menu);
                self.menu.PlaySound(SoundID.MENU_Checkbox_Check);
            }
        }

        // Possible random background scenes to appear for custom slugs
        // (I put them in an array like this cause some select scenes dont work, and some wouldnt fit)
        private static MenuScene.SceneID[] randomScenes =
        {
            MenuScene.SceneID.MainMenu_Downpour,
            MenuScene.SceneID.Intro_1_Tree,
            MenuScene.SceneID.Intro_2_Branch,
            MenuScene.SceneID.Intro_3_In_Tree,
            MenuScene.SceneID.Intro_4_Walking,
            MenuScene.SceneID.Intro_5_Hunting,
            MenuScene.SceneID.Intro_8_Climbing,
            MenuScene.SceneID.Intro_9_Rainy_Climb,
            MenuScene.SceneID.Intro_10_Fall,
            MenuScene.SceneID.Intro_10_5_Separation,
            MenuScene.SceneID.Intro_11_Drowning,
            MenuScene.SceneID.Landscape_CC,
            MenuScene.SceneID.Landscape_DS,
            MenuScene.SceneID.Landscape_GW,
            MenuScene.SceneID.Landscape_HI,
            MenuScene.SceneID.Landscape_LF,
            MenuScene.SceneID.Landscape_SB,
            MenuScene.SceneID.Landscape_SH,
            MenuScene.SceneID.Landscape_SI,
            MenuScene.SceneID.Landscape_SL,
            MenuScene.SceneID.Landscape_SU,
            MenuScene.SceneID.Dream_Moon_Friend,
            MenuScene.SceneID.Dream_Pebbles,
            MoreSlugcatsEnums.MenuSceneID.Landscape_MS,
            MoreSlugcatsEnums.MenuSceneID.Landscape_LC,
            MoreSlugcatsEnums.MenuSceneID.Landscape_OE,
            MoreSlugcatsEnums.MenuSceneID.Landscape_HR,
            MoreSlugcatsEnums.MenuSceneID.Landscape_UG,
            MoreSlugcatsEnums.MenuSceneID.Landscape_VS,
            MoreSlugcatsEnums.MenuSceneID.Landscape_CL,
        };

        // Assigning the name, description, and a random background scene
        private static void CharacterSelectPage_UpdateSelectedSlugcat(On.Menu.CharacterSelectPage.orig_UpdateSelectedSlugcat orig, CharacterSelectPage self, int num)
        {
            if (num < 0 || num >= ExpeditionGame.playableCharacters.Count) num = 1; // This might not be necessary, but better be safe
            orig.Invoke(self, num);

            if (num > (ModManager.MSC ? 7 : 2))
            {
                SlugcatStats.Name name = ExpeditionGame.playableCharacters[num];
                if (SlugBaseCharacter.TryGet(name, out var chara))
                {
                    self.slugcatName.text = chara.DisplayName.ToUpper();
                    self.slugcatDescription.text = chara.Description;
                    self.slugcatScene = randomScenes[UnityEngine.Random.Range(0, randomScenes.Length - (ModManager.MSC ? 0 : 7))];
                    //SlugBasePlugin.Logger.LogMessage(self.slugcatScene);
                }
            }
        }

        // Getting the slugbase slugcat portrait
        private static MenuIllustration CharacterSelectPage_GetSlugcatPortrait(On.Menu.CharacterSelectPage.orig_GetSlugcatPortrait orig, CharacterSelectPage self, SlugcatStats.Name slugcat, Vector2 pos)
        {
            MenuIllustration image = orig.Invoke(self, slugcat, pos);

            if (SlugBaseCharacter.TryGet(slugcat, out var chara))
            {
                string folderName = "illustrations";
                string fileName = "multiplayerportrait30"; // Nightcat deadeth icon if some slug doesnt have a portrait
                // Checking for all portraits starting from the 5th one
                for (int i = -1; i < 4; i++)
                {
                    string charaString = string.Concat(
                         "multiplayerportrait",
                         (i == -1 ? 4 : i).ToString(),
                         "1-",
                         chara.Name
                        );
                    if (File.Exists(AssetManager.ResolveFilePath(
                        folderName +
                        Path.DirectorySeparatorChar.ToString() +
                        charaString +
                         ".png"
                        )))
                    {
                        fileName = charaString;
                        break;
                    }
                }
                image = new(self.menu, self, folderName, fileName, pos, true, true);
            }

            return image;
        }
    }
}
