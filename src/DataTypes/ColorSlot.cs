using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SlugBase.Features.PlayerFeatures;

namespace SlugBase.DataTypes
{
    /// <summary>
    /// Represents a color that may be configured by the user.
    /// </summary>
    public class ColorSlot
    {
        /// <summary>
        /// The index of this color slot in <see cref="PlayerGraphics.ColoredBodyPartList(SlugcatStats.Name)"/>.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// This color's name for use with <see cref="PlayerColor"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The default color.
        /// </summary>
        public Color Default { get; set; }

        /// <summary>
        /// The preset colors to use in multiplayer.
        /// </summary>
        public Color[] Variants { get; set; }

        /// <summary>
        /// Create an empty <see cref="ColorSlot"/>.
        /// </summary>
        /// <param name="index">The index for use with <see cref="PlayerGraphics.ColoredBodyPartList(SlugcatStats.Name)"/>.</param>
        /// <param name="name">The name of the body part this colors.</param>
        public ColorSlot(int index, string name)
        {
            Index = index;
            Name = name;
            Variants = Array.Empty<Color>();
        }

        /// <summary>
        /// Create a <see cref="ColorSlot"/> from JSON.
        /// </summary>
        /// <param name="index">The index for use with <see cref="PlayerGraphics.ColoredBodyPartList(SlugcatStats.Name)"/>.</param>
        /// <param name="json">The JSON to load.</param>
        public ColorSlot(int index, JsonAny json) : this(index, json.AsObject().GetString("name"))
        {
            var obj = json.AsObject();

            Default = JsonUtils.ToColor(obj.Get("story"));

            if(obj.TryGet("arena")?.AsList() is JsonList list)
            {
                Variants = list.Select(JsonUtils.ToColor).ToArray();
            }
        }

        /// <summary>
        /// Gets a color variant from <see cref="Variants"/> by index.
        /// </summary>
        /// <param name="slugcatCharacter">The index.</param>
        /// <returns>The color for <paramref name="slugcatCharacter"/> from <see cref="Variants"/>, or <see cref="Default"/> if the index was out of range.</returns>
        public Color GetColor(int slugcatCharacter)
        {
            if (slugcatCharacter < 0 || slugcatCharacter >= Variants.Length)
                return Default;

            return Variants[slugcatCharacter];
        }

        /// <summary>
        /// Gets the color of this slot for a given player.
        /// </summary>
        /// <param name="graphics">The player graphics to get the color from.</param>
        /// <returns>The color of this body part after modifications are applied.</returns>
        public Color GetColor(PlayerGraphics graphics)
        {
            int plyNum = graphics.player.playerState.playerNumber;

            if (ModManager.CoopAvailable && graphics.useJollyColor
                && plyNum >= 0
                && plyNum < PlayerGraphics.jollyColors.Length
                && Index >= 0
                && Index < PlayerGraphics.jollyColors[plyNum].Length)
            {
                return PlayerGraphics.JollyColor(plyNum, Index);
            }
            else if (PlayerGraphics.CustomColorsEnabled()
                && Index >= 0
                && Index < PlayerGraphics.customColors.Count)
            {
                return PlayerGraphics.CustomColorSafety(Index);
            }
            else if (graphics.CharacterForColor != graphics.player.SlugCatClass)
            {
                return GetColor(graphics.CharacterForColor.Index);
            }
            else
            {
                return Default;
            }
        }
    }
}
