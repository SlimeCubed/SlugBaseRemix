using System.Collections.Generic;
using System.Linq;
using Name = SlugcatStats.Name;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using UnityEngine;

namespace SlugBase
{
    internal static class Utils
    {
        public static T FirstValidEnum<T>(IEnumerable<T> values)
            where T : ExtEnum<T>
        {
            return values.FirstOrDefault(val => val != null && val.Index != -1);
        }
        public static List<T> AllValidEnums<T>(IEnumerable<T> values)
            where T : ExtEnum<T>
        {
            return values.Where(val => val != null && val.Index != -1).ToList();
        }

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

        public static string MatchCaseInsensitiveEnum<T>(string name)
            where T : ExtEnum<T>
        {
            return ExtEnum<T>.values.entries.FirstOrDefault(value => value.Equals(name, System.StringComparison.InvariantCultureIgnoreCase)) ?? name;
        }

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
