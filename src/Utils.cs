using System.Collections.Generic;
using System.Linq;
using Name = SlugcatStats.Name;
using UnityEngine;

namespace SlugBase
{
    internal static class Utils
    {

        /// <summary>
        /// Returns first non-null and non-negative index extenum in a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static T FirstValidEnum<T>(IEnumerable<T> values)
            where T : ExtEnum<T>
        {
            return values.FirstOrDefault(val => val != null && val.Index != -1);
        }
        /// <summary>
        /// Filters out all non-null and non-negative extenums in a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static List<T> AllValidEnums<T>(IEnumerable<T> values)
            where T : ExtEnum<T>
        {
            return values.Where(val => val != null && val.Index != -1).ToList();
        }

        /// <summary>
        /// Tries to parse text as name
        /// </summary>
        /// <param name="text">ID of name</param>
        /// <returns></returns>
        #warning unclean method
        public static Name GetName(string text)
        {
            if (!ExtEnumBase.TryParse(typeof(Name), text, true, out var res))
                return (Name)res;

            return text.ToLowerInvariant() switch
            {
                "survivor" => Name.White,
                "hunter" => Name.Red,
                "monk" => Name.Yellow,
                "spearmaster" => new Name("Spear"),
                "inv" => new Name("Inv"),
                _ => new Name(text)
            };
        }

        /// <summary>
        /// Find in ExtEnum matching ID
        /// </summary>
        /// <typeparam name="T">Extenum type</typeparam>
        /// <param name="name">ID of lookup</param>
        /// <returns>Matching ID if found, same string if not</returns>
        #warning weird return logic
        //it will always return same thing specified in name, be it a successful lookup or not
        public static string MatchCaseInsensitiveEnum<T>(string name)
            where T : ExtEnum<T>
        {
            return ExtEnum<T>.values.entries.FirstOrDefault(value => value.Equals(name, System.StringComparison.InvariantCultureIgnoreCase)) ?? name;
        }

        /// <summary>
        /// Parses an IEnumerable of strings into colors
        /// </summary>
        /// <param name="stringList"></param>
        /// <returns>List of same size as input list. Invalid strings have UB in parsting</returns>
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
        /// Parses an IEnumerable of colors into a list of strings.
        /// </summary>
        /// <param name="colorList"></param>
        /// <returns>List of colors same size as input</returns>
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
