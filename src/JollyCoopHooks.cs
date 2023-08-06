using JollyCoop.JollyMenu;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SlugBase.DataTypes;
using System.Runtime.CompilerServices;
using ColorSlider = JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider;
using static SlugBase.Features.PlayerFeatures;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SlugBase
{
    internal static class JollyCoopHooks
    {
        private static readonly ConditionalWeakTable<ColorChangeDialog, List<ColorSlider>> _extraSliders = new();
        private static readonly ConditionalWeakTable<JollyPlayerOptions, List<Color>> _extraColors = new();

        public static void Apply()
        {
            // Jolly Color Menu Hooks
            On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += ColorChangeDialog_ValueOfSlider;
            On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += ColorChangeDialog_SliderSetValue;
            On.PlayerGraphics.LoadJollyColorsFromOptions += PlayerGraphics_LoadJollyColorsFromOptions;
            On.Options.ApplyOption += Options_ApplyOption;
            On.Options.ToString += Options_ToString;
            On.JollyCoop.JollyMenu.ColorChangeDialog.ctor += On_ColorChangeDialog_ctor;
            On.JollyCoop.JollyMenu.ColorChangeDialog.ActualSavingColor += ColorChangeDialog_ActualSavingColor;
            On.JollyCoop.JollyMenu.ColorChangeDialog.Singal += ColorChangeDialog_Singal;
            On.JollyCoop.JollyMenu.ColorChangeDialog.AddSlider += ColorChangeDialog_AddSlider;
            IL.JollyCoop.JollyMenu.ColorChangeDialog.ctor += IL_ColorChangeDialog_ctor;
        }

        // Properly write values to custom color sliders
        private static void ColorChangeDialog_SliderSetValue(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_SliderSetValue orig, ColorChangeDialog self, Slider slider, float f)
        {
            if (slider.ID.value.Contains("JOLLY")
                && _extraSliders.TryGetValue(self, out var extraSliders))
            {
                string[] args = slider.ID.value.Split('_');
                if (int.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out int bodyPart) && bodyPart - 3 >= 0 && bodyPart - 3 < extraSliders.Count)
                {
                    var colorPicker = extraSliders[bodyPart - 3];
                    string hslPart = args[2];

                    switch(hslPart)
                    {
                        case "HUE": colorPicker.hslColor.hue = f; break;
                        case "SAT": colorPicker.hslColor.saturation = f; break;
                        case "LIT": colorPicker.hslColor.lightness = f; break;
                    }

                    colorPicker.HSL2RGB();
                    self.selectedObject = slider;
                    return;
                }
            }

            orig(self, slider, f);
        }

        // Properly read values from custom color sliders
        private static float ColorChangeDialog_ValueOfSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ValueOfSlider orig, ColorChangeDialog self, Slider slider)
        {
            if (slider.ID.value.Contains("JOLLY")
                && _extraSliders.TryGetValue(self, out var extraSliders))
            {
                string[] args = slider.ID.value.Split('_');
                if (int.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out int bodyPart) && bodyPart - 3 >= 0 && bodyPart - 3 < extraSliders.Count)
                {
                    var colorPicker = extraSliders[bodyPart - 3];
                    string hslPart = args[2];

                    switch (hslPart)
                    {
                        case "HUE": return colorPicker.hslColor.hue;
                        case "SAT": return colorPicker.hslColor.saturation;
                        case "LIT": return colorPicker.hslColor.lightness;
                    }
                }
            }

            return orig(self, slider);
        }

        private static void PlayerGraphics_LoadJollyColorsFromOptions(On.PlayerGraphics.orig_LoadJollyColorsFromOptions orig, int playerNumber)
        {
            orig(playerNumber);

            var playerOptions = RWCustom.Custom.rainWorld.options.jollyPlayerOptionsArray[playerNumber];
            var extraColors = GetExtraColors(playerOptions);

            if(extraColors.Count > 0 && PlayerGraphics.jollyColors[playerNumber].Length < extraColors.Count + 3)
            {
                Array.Resize(ref PlayerGraphics.jollyColors[playerNumber], extraColors.Count + 3);
            }

            for(int i = 0; i < extraColors.Count; i++)
            {
                PlayerGraphics.jollyColors[playerNumber][i + 3] = extraColors[i];
            }
        }

        // Load custom colors
        private static bool Options_ApplyOption(On.Options.orig_ApplyOption orig, Options self, string[] splt2)
        {
            try
            {
                if (splt2[0] == "SlugBaseCustomJollyColors" && ModManager.JollyCoop)
                {
                    var playerColors = Regex.Split(splt2[1], "<optC>");
                    for (int i = 0; i < self.jollyPlayerOptionsArray.Length && i < playerColors.Length; i++)
                    {
                        if (playerColors[i] == "") continue;

                        var colors = Utils.StringsToColors(playerColors[i].Split(','));

                        _extraColors.Remove(self.jollyPlayerOptionsArray[i]);
                        _extraColors.Add(self.jollyPlayerOptionsArray[i], colors);
                    }
                    return true;
                }
            }
            catch(Exception e)
            {
                SlugBasePlugin.Logger.LogError("Failed to load SlugBase's Jolly Coop colors!\n" + e);
            }

            return orig(self, splt2);
        }

        // Save custom colors
        private static string Options_ToString(On.Options.orig_ToString orig, Options self)
        {
            string newSaveData = "";

            if (ModManager.JollyCoop)
            {
                newSaveData = "SlugBaseCustomJollyColors<optB>";
                for (int i = 0; i < self.jollyPlayerOptionsArray.Length; i++)
                {
                    var colors = GetExtraColors(self.jollyPlayerOptionsArray[i]);
                    newSaveData += string.Join(",", Utils.ColorsToStrings(colors)) + "<optC>";
                }
                newSaveData += "<optA>";
            }

            return orig(self) + newSaveData;
        }

        // Add new sliders to Jolly's color picker popup
        private static void On_ColorChangeDialog_ctor(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ctor orig, ColorChangeDialog self, JollySetupDialog jollyDialog, SlugcatStats.Name playerName, int playerNumber, ProcessManager manager, List<string> names)
        {
            orig(self, jollyDialog, playerName, playerNumber, manager, names);

            if (SlugBaseCharacter.TryGet(playerName, out var chara) && CustomColors.TryGet(chara, out ColorSlot[] allColors) && allColors.Length > 3)
            {
                var extraColorSlots = allColors.Skip(3).ToArray();
                var extraColors = GetExtraColors(self.JollyOptions);

                var extraSliders = new List<ColorSlider>();
                _extraSliders.Add(self, extraSliders);

                // Initialize colors
                while(extraColors.Count < extraColorSlots.Length)
                {
                    extraColors.Add(extraColorSlots[extraColors.Count].Default);
                }

                // Add color pickers for all of the new colors
                for (int i = 0; i < extraColorSlots.Length; i++)
                {
                    ColorSlider slider = null;
                    self.AddSlider(
                        ref slider,
                        labelString: jollyDialog.Translate(extraColorSlots[i].Name),
                        position: new Vector2(135f + 140f * (i % 3), 90f - 100f * Mathf.Max((i - 2) % 3, 0f)),
                        playerNumber: self.playerNumber,
                        bodyPart: i + 3);

                    // Save new slider for easy access elswhere, though the same could be accomplished using the menu's page[0].subObjects List I guess.
                    extraSliders.Add(slider);

                    // Add controller support
                    slider.litSlider.nextSelectable[3] = self.okButton;
                    self.okButton.nextSelectable[i % 3] = slider.litSlider;

                    // Get the ColorSlider of which values we need to change
                    ColorSlider previousColorPicker = i == 0 ? self.body : i == 1 ? self.face : i == 2 ? self.unique : extraSliders[i - 3];
                    slider.hueSlider.nextSelectable[1] = previousColorPicker.litSlider;
                    previousColorPicker.litSlider.nextSelectable[3] = slider.hueSlider;

                    extraSliders[i].color = extraColors[i];
                    extraSliders[i].RGB2HSL();
                }

                SlugBasePlugin.Logger.LogDebug($"{extraSliders.Count} sliders, {extraColors.Count} colors, {extraColorSlots.Length} color slots, {allColors.Length} all colors");
            }
        }

        // Read added colors back from sliders
        private static void ColorChangeDialog_ActualSavingColor(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ActualSavingColor orig, ColorChangeDialog self)
        {
            orig(self);

            if (_extraSliders.TryGetValue(self, out var extraSliders))
            {
                var extraColors = GetExtraColors(self.JollyOptions);

                for (int i = 0; i < extraSliders.Count && i < extraColors.Count; i++)
                {
                    extraColors[i] = extraSliders[i].color;
                }
            }
        }

        // Reset colors
        private static void ColorChangeDialog_Singal(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_Singal orig, ColorChangeDialog self, MenuObject sender, string message)
        {
            orig(self, sender, message);
            if (message.StartsWith("RESET_COLOR_DIALOG_")
                && SlugBaseCharacter.TryGet(self.playerClass, out var chara) && CustomColors.TryGet(chara, out ColorSlot[] allColors) && allColors.Length > 3
                && _extraSliders.TryGetValue(self, out var extraSliders))
            {
                var extraColorSlots = allColors.Skip(3).ToArray();
                var extraColors = GetExtraColors(self.JollyOptions);

                for (int i = 0; i < extraSliders.Count; i++)
                {
                    // Ideally this never happens, but with hooks you never know
                    if (i >= extraColors.Count)
                        extraColors.Add(Color.white);

                    // Reset to defaults for the custom slugcat
                    extraColors[i] = i < extraColorSlots.Length ? extraColorSlots[i].Default : Color.white;

                    // Load the previous colors into the new sliders.
                    extraSliders[i].color = extraColors[i];
                    extraSliders[i].RGB2HSL();
                }
            }
        }

        // Change position of original sliders
        private static void ColorChangeDialog_AddSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_AddSlider orig, ColorChangeDialog self, ref ColorSlider slider, string labelString, Vector2 position, int playerNumber, int bodyPart)
        {
            // Use size of custom colors in json to determine height
            if (SlugBaseCharacter.TryGet(self.playerClass, out var chara) && CustomColors.TryGet(chara, out ColorSlot[] colors) && colors.Length > 3)
            {
                // Based on how many colors there are, adjust the y position.
                position.y += 50f;
                if (colors.Length > 6)
                {
                    position.y += 50f;
                }
            }

            orig(self, ref slider, labelString, position, playerNumber, bodyPart);
        }

        // Resize the background box and change position of the reset button
        private static void IL_ColorChangeDialog_ctor(ILContext il)
        {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.Before, i => i.MatchNewobj<Vector2>()))
            {
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate((float height, SlugcatStats.Name name) =>
                {
                    if (SlugBaseCharacter.TryGet(name, out var chara) && CustomColors.TryGet(chara, out ColorSlot[] colors) && colors.Length > 3)
                    {
                        height += 200f;
                        if (colors.Length > 6)
                        {
                            height += 200f;
                        }
                    }
                    return height;
                });

                cursor.Index++;
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(IL_ColorChangeDialog_ctor)}, resize box, failed!");
                return;
            }

            if (cursor.TryGotoNext(MoveType.Before, i => i.MatchNewobj<Vector2>()))
            {
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate((float resetY, SlugcatStats.Name playerClass) =>
                {
                    if (SlugBaseCharacter.TryGet(playerClass, out var chara) && CustomColors.TryGet(chara, out ColorSlot[] colors) && colors.Length > 3)
                    {
                        resetY += 100f;
                        if (colors.Length > 6)
                        {
                            resetY += 100f;
                        }
                    }
                    return resetY;
                });
            }
            else
            {
                SlugBasePlugin.Logger.LogError($"IL hook {nameof(IL_ColorChangeDialog_ctor)}, move reset button, failed!");
                return;
            }
        }

        private static List<Color> GetExtraColors(JollyPlayerOptions playerOptions)
        {
            if (!_extraColors.TryGetValue(playerOptions, out var colors))
                _extraColors.Add(playerOptions, colors = new());

            return colors;
        }
    }
}
