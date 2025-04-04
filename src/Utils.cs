using System.Collections.Generic;
using System.Linq;
using Name = SlugcatStats.Name;
using UnityEngine;

namespace SlugBase
{
    internal static class Utils
    {
        /// <summary>
        /// Gets the first non-null, registered enum in <paramref name="values"/>.
        /// </summary>
        /// <param name="values">The collection to search through.</param>
        /// <returns>A registered enum, or <c>null</c> if no enums in the list are registered.</returns>
        public static T FirstValidEnum<T>(IEnumerable<T> values)
            where T : ExtEnum<T>
        {
            return values.FirstOrDefault(val => val != null && val.Index != -1);
        }
        /// <summary>
        /// Gets all non-null, registered enums in <paramref name="values"/>.
        /// </summary>
        /// <param name="values">The collection to search through.</param>
        public static List<T> AllValidEnums<T>(IEnumerable<T> values)
            where T : ExtEnum<T>
        {
            return values.Where(val => val != null && val.Index != -1).ToList();
        }

        /// <summary>
        /// Gets the <see cref="Name"/> corresponding to <paramref name="text"/>, converting canon character names to code names.
        /// </summary>
        /// <param name="text">The name of the character to convert to an ID.</param>
        public static Name GetName(string text)
        {
            // Return an exact match, if found
            if (ExtEnumBase.TryParse(typeof(Name), text, true, out var res))
                return (Name)res;

            return text.ToLowerInvariant() switch
            {
                // Convert canon names to code names
                "survivor" => Name.White,
                "hunter" => Name.Red,
                "monk" => Name.Yellow,
                "spearmaster" => new Name("Spear"),
                "inv" => new Name("Inv"),

                // If nothing matched, return the input text as an enum
                _ => new Name(text)
            };
        }

        /// <summary>
        /// Changes the case of <paramref name="name"/> to match the corresponding enum in <typeparamref name="T"/>.
        /// </summary>
        /// <param name="name">The string to capitalize.</param>
        /// <returns>The value of an enum in <typeparamref name="T"/>, or <paramref name="name"/> if no matching entry exists.</returns>
        public static string MatchCaseInsensitiveEnum<T>(string name)
            where T : ExtEnum<T>
        {
            return ExtEnum<T>.values.entries.FirstOrDefault(value => value.Equals(name, System.StringComparison.InvariantCultureIgnoreCase)) ?? name;
        }

        /// <summary>
        /// Converts a list of hexadecimal strings to colors.
        /// </summary>
        public static List<Color> StringsToColors(IEnumerable<string> stringList)
        {
            if (stringList == null) return null;
            var colorList = new List<Color>();
            foreach (string color in stringList)
            {
                ColorUtility.TryParseHtmlString("#" + color, out Color loadedColor);
                colorList.Add(loadedColor);
            }
            return colorList;
        }

        /// <summary>
        /// Converts a list of colors to hexadecimal strings.
        /// </summary>
        public static List<string> ColorsToStrings(IEnumerable<Color> colorList)
        {
            if (colorList == null) return null;
            var stringList = new List<string>();
            foreach (Color color in colorList)
            {
                stringList.Add(ColorUtility.ToHtmlStringRGB(color));
            }
            return stringList;
        }
    }
}
