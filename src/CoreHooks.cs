using Menu;
using RWCustom;
using SlugBase.Interface;
using System.Linq;
using UnityEngine;
using System;

namespace SlugBase
{
    internal static class CoreHooks
    {
        public static void Apply()
        {
            On.SlugcatStats.getSlugcatName += SlugcatStats_getSlugcatName;
            On.InGameTranslator.LoadFonts += InGameTranslator_LoadFonts;
            On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += SlugcatPageNewGame_ctor;
            On.Menu.SlugcatSelectMenu.SetSlugcatColorOrder += SlugcatSelectMenu_SetSlugcatColorOrder;
        }

        // Update slugcat name on other menus
        private static string SlugcatStats_getSlugcatName(On.SlugcatStats.orig_getSlugcatName orig, SlugcatStats.Name i)
        {
            if (SlugBaseCharacter.TryGet(i, out var chara)
                && chara.DisplayName is string displayName)
            {
                return displayName.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase) ? displayName.Substring(4) : displayName;
            }
            else
            {
                return orig(i);
            }
        }

        private static void InGameTranslator_LoadFonts(On.InGameTranslator.orig_LoadFonts orig, InGameTranslator.LanguageID lang, Menu.Menu menu)
        {
            orig(lang, menu);

            ErrorList.Instance.MarkDirty();
        }

        // Update slugcat name and description
        private static void SlugcatPageNewGame_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.orig_ctor orig, SlugcatSelectMenu.SlugcatPageNewGame self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
        {
            orig(self, menu, owner, pageIndex, slugcatNumber);

            if (SlugBaseCharacter.TryGet(slugcatNumber, out var chara))
            {
                string name = chara.DisplayName ?? "Missing Name";
                string desc = chara.Description ?? "Missing Description";

                name = self.menu.Translate(name.ToUpper());
                desc = Custom.ReplaceLineDelimeters(self.menu.Translate(desc));
                int descLines = desc.Count((char f) => f == '\n');
                float offset = descLines > 1 ? 30f : 0f;

                self.difficultyLabel.text = name;
                self.difficultyLabel.pos = new Vector2(-1000f, self.imagePos.y - 249f + offset);

                self.infoLabel.text = desc;
                self.infoLabel.pos = new Vector2(-1000f, self.imagePos.y - 249f - 60f + offset / 2f);
            }
        }

        // Add SlugBaseCharacters to the main menu
        private static void SlugcatSelectMenu_SetSlugcatColorOrder(On.Menu.SlugcatSelectMenu.orig_SetSlugcatColorOrder orig, SlugcatSelectMenu self)
        {
            orig(self);

            foreach (var id in SlugBaseCharacter.Registry.Keys)
            {
                if(!SlugcatStats.HiddenOrUnplayableSlugcat(id))
                    self.slugcatColorOrder.Add(id);
            }

            for (int i = 0; i < self.slugcatColorOrder.Count; i++)
            {
                if (self.slugcatColorOrder[i] == self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat)
                {
                    self.slugcatPageIndex = i;
                    return;
                }
                if (self.slugcatColorOrder[i] == SlugcatStats.Name.White)
                {
                    self.slugcatPageIndex = i;
                }
            }
        }
    }
}
