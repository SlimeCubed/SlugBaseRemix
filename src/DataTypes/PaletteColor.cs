using UnityEngine;

namespace SlugBase.DataTypes
{
    /// <summary>
    /// Represents a color that may change based on the current palette.
    /// </summary>
    public struct PaletteColor
    {
        /// <summary>The base color. Alpha will be taken from this.</summary>
        public Color MainColor;

        /// <summary>The multiplier for the base color.</summary>
        public float MainAmount;

        /// <summary>The multiplier for <see cref="RoomPalette.blackColor"/>.</summary>
        public float BlackAmount;
        
        /// <summary>The multiplier for <see cref="RoomPalette.fogColor"/>.</summary>
        public float FogAmount;

        /// <summary>The multiplier for <see cref="RoomPalette.skyColor"/>.</summary>
        public float SkyAmount;

        /// <summary>
        /// Creates a new <see cref="PaletteColor"/>.
        /// </summary>
        /// <param name="mainColor">The base color. Alpha will be taken from this.</param>
        /// <param name="mainAmount">The multiplier for the base color.</param>
        /// <param name="blackAmount">The multiplier for <see cref="RoomPalette.blackColor"/>.</param>
        /// <param name="fogAmount">The multiplier for <see cref="RoomPalette.fogColor"/>.</param>
        /// <param name="skyAmount">The multiplier for <see cref="RoomPalette.skyColor"/>.</param>
        public PaletteColor(Color mainColor, float mainAmount, float blackAmount, float fogAmount, float skyAmount)
        {
            MainColor = mainColor;
            MainAmount = mainAmount;
            BlackAmount = blackAmount;
            FogAmount = fogAmount;
            SkyAmount = skyAmount;
        }

        /// <summary>
        /// Get the color for a given palette.
        /// </summary>
        /// <param name="palette">The palette to sample from.</param>
        public Color GetColor(RoomPalette palette)
        {
            Color c = MainColor * MainAmount
                + palette.blackColor * BlackAmount
                + palette.fogColor * FogAmount
                + palette.skyColor * SkyAmount;

            c.a = MainColor.a;
            return c;
        }
    }
}
