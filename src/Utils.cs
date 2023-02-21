using Name = SlugcatStats.Name;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;

namespace SlugBase
{
    internal static class Utils
    {
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
    }
}
