using SlugBase.DataTypes;
using UnityEngine;
using static SlugBase.JsonUtils;

namespace SlugBase.Features
{
    /// <summary>
    /// Helper methods to construct <see cref="Feature{T}"/>s with simple parsing rules.
    /// </summary>
    public static class FeatureTypes
    {
        private static JsonAny AssertLength(JsonAny json, int minLength, int maxLength)
        {
            if (json.TryList() is JsonList list)
            {
                if (list.Count < minLength)
                    throw new JsonException($"List must contain at least {minLength} elements!", list);

                if (list.Count > maxLength)
                    throw new JsonException($"List may not contain more than {maxLength} elements!", list);
            }

            return json;
        }

        /// <summary>Create a player feature that takes one integer.</summary>
        public static PlayerFeature<int> PlayerInt(string id) => new(id, ToInt);

        /// <summary>Create a player feature that takes one integer.</summary>
        public static PlayerFeature<long> PlayerLong(string id) => new(id, ToLong);

        /// <summary>Create a player feature that takes one number.</summary>
        public static PlayerFeature<double> PlayerDouble(string id) => new(id, ToDouble);

        /// <summary>Create a player feature that takes one number.</summary>
        public static PlayerFeature<float> PlayerFloat(string id) => new(id, ToFloat);

        /// <summary>Create a player feature that takes one string.</summary>
        public static PlayerFeature<string> PlayerString(string id) => new(id, JsonUtils.ToString);

        /// <summary>Create a player feature that takes a slugcat name.</summary>
        public static PlayerFeature<SlugcatStats.Name> PlayerSlugcatName(string id) => new(id, ToSlugcatName);

        /// <summary>Create a player feature that takes an array of integers.</summary>
        public static PlayerFeature<int[]> PlayerInts(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToInts(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes an array of integers.</summary>
        public static PlayerFeature<long[]> PlayerLongs(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToLongs(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes an array of numbers.</summary>
        public static PlayerFeature<double[]> PlayerDoubles(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToDoubles(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes an array of numbers.</summary>
        public static PlayerFeature<float[]> PlayerFloats(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToFloats(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes an array of strings.</summary>
        public static PlayerFeature<string[]> PlayerStrings(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToStrings(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes an array of slugcat names.</summary>
        public static PlayerFeature<SlugcatStats.Name[]> PlayerSlugcatNames(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToSlugcatNames(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes a color.</summary>
        public static PlayerFeature<Color> PlayerColor(string id) => new(id, ToColor);

        /// <summary>Create a player feature that takes a palette-modified color.</summary>
        public static PlayerFeature<PlayerColor> PlayerCustomColor(string id) => new(id, ToPlayerColor);

        /// <summary>Create a player feature that takes one boolean.</summary>
        public static PlayerFeature<bool> PlayerBool(string id) => new(id, ToBool);

        /// <summary>Create a player feature that takes one enum value.</summary>
        public static PlayerFeature<T> PlayerEnum<T>(string id) where T : struct => new(id, ToEnum<T>);

        /// <summary>Create a player feature that takes one enum value.</summary>
        public static PlayerFeature<T> PlayerExtEnum<T>(string id) where T : ExtEnum<T> => new(id, ToExtEnum<T>);


        /// <summary>Create a game feature that takes one integer.</summary>
        public static GameFeature<int> GameInt(string id) => new(id, ToInt);
        
        /// <summary>Create a game feature that takes one integer.</summary>
        public static GameFeature<long> GameLong(string id) => new(id, ToLong);
        
        /// <summary>Create a game feature that takes one number.</summary>
        public static GameFeature<double> GameDouble(string id) => new(id, ToDouble);
        
        /// <summary>Create a game feature that takes one number.</summary>
        public static GameFeature<float> GameFloat(string id) => new(id, ToFloat);

        /// <summary>Create a game feature that takes one string.</summary>
        public static GameFeature<string> GameString(string id) => new(id, JsonUtils.ToString);

        /// <summary>Create a game feature that takes a slugcat name.</summary>
        public static GameFeature<SlugcatStats.Name> GameSlugcatName(string id) => new(id, ToSlugcatName);

        /// <summary>Create a game feature that takes an array of integers.</summary>
        public static GameFeature<int[]> GameInts(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToInts(AssertLength(json, minLength, maxLength)));
        
        /// <summary>Create a game feature that takes an array of integers.</summary>
        public static GameFeature<long[]> GameLongs(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToLongs(AssertLength(json, minLength, maxLength)));
        
        /// <summary>Create a game feature that takes an array of numbers.</summary>
        public static GameFeature<double[]> GameDoubles(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToDoubles(AssertLength(json, minLength, maxLength)));
        
        /// <summary>Create a game feature that takes an array of numbers.</summary>
        public static GameFeature<float[]> GameFloats(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToFloats(AssertLength(json, minLength, maxLength)));
        
        /// <summary>Create a game feature that takes an array of strings.</summary>
        public static GameFeature<string[]> GameStrings(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToStrings(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a game feature that takes an array of slugcat names.</summary>
        public static GameFeature<SlugcatStats.Name[]> GameSlugcatNames(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToSlugcatNames(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a game feature that takes a color.</summary>
        public static GameFeature<Color> GameColor(string id) => new(id, ToColor);

        /// <summary>Create a game feature that takes a palette-modified color.</summary>
        public static GameFeature<PlayerColor> GameCustomColor(string id) => new(id, ToPlayerColor);

        /// <summary>Create a game feature that takes one boolean.</summary>
        public static GameFeature<bool> GameBool(string id) => new(id, ToBool);

        /// <summary>Create a game feature that takes one enum value.</summary>
        public static GameFeature<T> GameEnum<T>(string id) where T : struct => new(id, ToEnum<T>);

        /// <summary>Create a game feature that takes one enum value.</summary>
        public static GameFeature<T> GameExtEnum<T>(string id) where T : ExtEnum<T> => new(id, ToExtEnum<T>);
    }
}
