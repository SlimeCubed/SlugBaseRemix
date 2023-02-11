using SlugBase.Features;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static SlugBase.Features.PlayerFeatures;

namespace SlugBase.DataTypes
{
    /// <summary>
    /// Represents a color that may copy from <see cref="CustomColors"/>.
    /// </summary>
    public readonly struct PlayerColor
    {
        /// <summary>
        /// The body color of <see cref="SlugBaseCharacter"/>s.
        /// </summary>
        public static readonly PlayerColor Body = new("Body");

        /// <summary>
        /// The eye color of <see cref="SlugBaseCharacter"/>s.
        /// </summary>
        public static readonly PlayerColor Eyes = new("Eyes");

        private static readonly Regex _hex = new("^[0-9a-fA-F]+$");
        private readonly string _name;
        private readonly Color _color;

        /// <summary>
        /// Creates a new <see cref="PlayerColor"/> from JSON.
        /// </summary>
        /// <param name="json">The JSON to load.</param>
        public PlayerColor(JsonAny json)
        {
            if (json.AsString() is string str
                && !_hex.IsMatch(str))
            {
                _name = str;
                _color = default;
            }
            else
            {
                _name = null;
                _color = JsonUtils.ToColor(json);
            }
        }

        /// <summary>
        /// Creates a new <see cref="PlayerColor"/>.
        /// </summary>
        /// <param name="name">The name of the custom color to copy.</param>
        public PlayerColor(string name)
        {
            _name = name;
            _color = default;
        }

        /// <summary>
        /// Creates a new <see cref="PlayerColor"/>.
        /// </summary>
        /// <param name="color">The color to return.</param>
        public PlayerColor(Color color)
        {
            _name = null;
            _color = color;
        }

        /// <summary>
        /// Gets the value of this color for a player.
        /// </summary>
        /// <param name="playerGraphics">The player to get the color from.</param>
        /// <returns>The custom color, or <c>null</c> if it was not overridden via <see cref="CustomColors"/>.</returns>
        public Color? GetColor(PlayerGraphics playerGraphics)
        {
            if (_name == null)
            {
                return _color;
            }
            else if (CustomColors.TryGet(playerGraphics.player, out var colors))
            {
                for(int i = 0; i < colors.Length; i++)
                {
                    if (colors[i].Name == _name)
                    {
                        return colors[i].GetColor(playerGraphics);
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
        }
    }
}
