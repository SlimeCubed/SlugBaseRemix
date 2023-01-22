using IL.MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugBase
{
    public static class Utils
    {
        public static bool TryGet<TData>(this Feature<PlayerState, TData> feature, Player player, out TData data)
        {
            return feature.TryGet(player.playerState, out data);
        }

        public static float GetFloat(this Dictionary<string, object> json, string key)
        {
            return json[key] switch
            {
                float f => f,
                double d => (float)d,
                int i => i,
                long l => l,
                _ => throw new ArgumentException($"Couldn't convert \"{key}\" from \"{json[key].GetType().Name}\" to float!")
            };
        }

        public static int GetInt(this Dictionary<string, object> json, string key)
        {
            return json[key] switch
            {
                float f => (int)f,
                double d => (int)d,
                int i => i,
                long l => (int)l,
                _ => throw new ArgumentException($"Couldn't convert \"{key}\" from \"{json[key].GetType().Name}\" to int!")
            };
        }
    }
}
