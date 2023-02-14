using Menu;
using RWCustom;
using System.Linq;
using UnityEngine;

namespace SlugBase
{
    internal static class CoreHooks
    {
        public static void Apply()
        {
            On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += SlugcatPageNewGame_ctor;
            On.Menu.SlugcatSelectMenu.SetSlugcatColorOrder += SlugcatSelectMenu_SetSlugcatColorOrder;
        }

        // Update slugcat name and description
        private static void SlugcatPageNewGame_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.orig_ctor orig, SlugcatSelectMenu.SlugcatPageNewGame self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
        {
            orig(self, menu, owner, pageIndex, slugcatNumber);

            if (SlugBaseCharacter.TryGet(slugcatNumber, out var chara))
            {
                string name = chara.DisplayName ?? "Missing Name";
                string desc = chara.Description ?? "Missing Description";

                desc = Custom.ReplaceLineDelimeters(desc);
                int descLines = desc.Count((char f) => f == '\n');
                float offset = descLines > 1 ? 30f : 0f;

                self.difficultyLabel.text = name.ToUpper();
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
