using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SlugBase.Features
{
    /// <summary>
    /// Helper methods to construct <see cref="Feature{T}"/>s with simple parsing rules.
    /// </summary>
    public static class FeatureTypes
    {
        internal static float ToColorElement(JsonAny json)
        {
            switch(json.Type)
            {
                case JsonAny.Element.Integer:
                    long longVal = json.AsLong();
                    if (longVal < 0 || longVal > 255)
                        throw new JsonException("Integer color element was out of range!", json);
                    return longVal / 255f;

                case JsonAny.Element.Float:
                    float floatVal = json.AsFloat();
                    if(floatVal < 0f || floatVal > 1f)
                        throw new JsonException("Float color element was out of range!", json);
                    return floatVal;

                default:
                    throw new JsonException("Color element wasn't a float or integer!", json);
            }
        }

        internal static long ToLong(JsonAny json) => json.AsLong();
        internal static int ToInt(JsonAny json) => json.AsInt();
        internal static double ToDouble(JsonAny json) => json.AsDouble();
        internal static float ToFloat(JsonAny json) => json.AsFloat();
        internal static string ToString(JsonAny json) => json.AsString();
        internal static bool ToBool(JsonAny json) => json.AsBool();

        internal static Color ToColor(JsonAny json)
        {
            switch(json.Type)
            {
                case JsonAny.Element.String:
                    try
                    {
                        return RXUtils.GetColorFromHex(json.AsString());
                    }
                    catch(Exception e)
                    {
                        throw new JsonException("Could not convert hex string to color!", e, json);
                    }

                case JsonAny.Element.Float:
                    throw new JsonException("Could not convert float to color!", json);

                case JsonAny.Element.Integer:
                    {
                        long longVal = json.AsLong();
                        if (longVal < uint.MinValue || longVal > uint.MaxValue)
                            throw new JsonException("Integer color was out of range!", json);
                        return RXUtils.GetColorFromHex((uint)longVal);
                    }

                case JsonAny.Element.List:
                    var list = json.AsList();
                    if (list.Count < 3 || list.Count > 4)
                        throw new JsonException("Color list must have 3 or 4 elements!", json);

                    Color col = new(0f, 0f, 0f, 1f);
                    for(int i = 0; i < list.Count; i++)
                    {
                        col[i] = ToColorElement(list[i]);
                    }
                    return col;

                case JsonAny.Element.Object:
                    var obj = json.AsObject();
                    return new Color(
                        ToColorElement(obj.Get("r")),
                        ToColorElement(obj.Get("g")),
                        ToColorElement(obj.Get("b")),
                        obj.TryGet("a") is JsonAny a ? ToColorElement(a) : 1f
                    );

                default:
                    throw new JsonException("Invalid color!", json);
            }
        }

        internal static PaletteColor ToPaletteColor(JsonAny json)
        {
            if (json.TryObject() is JsonObject obj
                && obj.TryGet("r") == null
                && obj.TryGet("g") == null
                && obj.TryGet("b") == null)
            {
                PaletteColor color = new();
                float? alpha = null;
                foreach (var pair in obj)
                {
                    switch (pair.Key)
                    {
                        case "black": color.BlackAmount = pair.Value.AsFloat(); break;
                        case "fog": color.FogAmount = pair.Value.AsFloat(); break;
                        case "sky": color.SkyAmount = pair.Value.AsFloat(); break;
                        case "a":
                        case "alpha": alpha = pair.Value.AsFloat(); break;
                        default:
                            try
                            {
                                Color main = RXUtils.GetColorFromHex(pair.Key);
                                float weight = pair.Value.AsFloat();

                                color.MainColor += main;
                                color.MainAmount += weight;
                            }
                            catch (Exception e)
                            {
                                throw new JsonException($"Failed to convert \"{pair.Key}\" to color!", e, json);
                            }
                            break;
                    }
                }

                if (alpha.HasValue)
                    color.MainColor.a = alpha.Value;
                else
                    color.MainColor.a = 1f;

                return color;
            }
            else
            {
                Color main = ToColor(json);
                return new PaletteColor(main, 1f, 0f, 0f, 0f);
            }
        }

        internal static long[] ToLongs(JsonAny json) => json.TryLong() == null ? json.AsList().Select(ToLong).ToArray() : new[] { json.AsLong() };
        internal static int[] ToInts(JsonAny json) => json.TryInt() == null ? json.AsList().Select(ToInt).ToArray() : new[] { json.AsInt() };
        internal static double[] ToDoubles(JsonAny json) => json.TryDouble() == null ? json.AsList().Select(ToDouble).ToArray() : new[] { json.AsDouble() };
        internal static float[] ToFloats(JsonAny json) => json.TryFloat() == null ? json.AsList().Select(ToFloat).ToArray() : new[] { json.AsFloat() };
        internal static string[] ToStrings(JsonAny json) => json.TryString() == null ? json.AsList().Select(ToString).ToArray() : new[] { json.AsString() };
        internal static T ToEnum<T>(JsonAny json) where T : struct
        {
            if (Enum.TryParse(json.AsString(), true, out T value))
                return value;
            else
                throw new JsonException($"\"{json.AsString()}\" was not a value of \"{typeof(T).Name}\"!", json);
        }
        internal static T ToExtEnum<T>(JsonAny json) where T : ExtEnum<T>
        {
            //// Fails if the enum hasn't loaded yet
            //if (ExtEnumBase.TryParse(typeof(T), json.AsString(), true, out var value))
            //    return (T)value;
            //else
            //    throw new JsonException($"\"{json.AsString()}\" was not a value of \"{typeof(T).Name}\"!", json);

            return (T)Activator.CreateInstance(typeof(T), json.AsString(), false);
        }
        internal static Vector2 ToVector2(JsonAny json)
        {
            switch(json.Type)
            {
                case JsonAny.Element.List:
                    var list = json.AsList();
                    if (list.Count != 2)
                        throw new JsonException("2D vector must contain 2 values!", json);

                    return new Vector2(list.GetFloat(0), list.GetFloat(1));

                case JsonAny.Element.Object:
                    var obj = json.AsObject();
                    return new Vector2(obj.GetFloat("x"), obj.GetFloat("y"));

                default:
                    throw new JsonException("Invalid 2D vector!", json);
            }
        }

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
        public static PlayerFeature<int> PlyInt(string id) => new(id, ToInt);

        /// <summary>Create a player feature that takes one integer.</summary>
        public static PlayerFeature<long> PlyLong(string id) => new(id, ToLong);

        /// <summary>Create a player feature that takes one number.</summary>
        public static PlayerFeature<double> PlyDouble(string id) => new(id, ToDouble);

        /// <summary>Create a player feature that takes one number.</summary>
        public static PlayerFeature<float> PlyFloat(string id) => new(id, ToFloat);

        /// <summary>Create a player feature that takes one string.</summary>
        public static PlayerFeature<string> PlyString(string id) => new(id, ToString);

        /// <summary>Create a player feature that takes an array of integers.</summary>
        public static PlayerFeature<int[]> PlyInts(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToInts(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes an array of integers.</summary>
        public static PlayerFeature<long[]> PlyLongs(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToLongs(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes an array of numbers.</summary>
        public static PlayerFeature<double[]> PlyDoubles(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToDoubles(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes an array of numbers.</summary>
        public static PlayerFeature<float[]> PlyFloats(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToFloats(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes an array of strings.</summary>
        public static PlayerFeature<string[]> PlyStrings(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToStrings(AssertLength(json, minLength, maxLength)));

        /// <summary>Create a player feature that takes a color.</summary>
        public static PlayerFeature<Color> PlyColor(string id) => new(id, ToColor);

        /// <summary>Create a player feature that takes a palette-modified color.</summary>
        public static PlayerFeature<PaletteColor> PlyPaletteColor(string id) => new(id, ToPaletteColor);

        /// <summary>Create a player feature that takes one boolean.</summary>
        public static PlayerFeature<bool> PlyBool(string id) => new(id, ToBool);

        /// <summary>Create a player feature that takes one enum value.</summary>
        public static PlayerFeature<T> PlyEnum<T>(string id) where T : struct => new(id, ToEnum<T>);

        /// <summary>Create a player feature that takes one enum value.</summary>
        public static PlayerFeature<T> PlyExtEnum<T>(string id) where T : ExtEnum<T> => new(id, ToExtEnum<T>);


        /// <summary>Create a game feature that takes one integer.</summary>
        public static GameFeature<int> GameInt(string id) => new(id, ToInt);
        
        /// <summary>Create a game feature that takes one integer.</summary>
        public static GameFeature<long> GameLong(string id) => new(id, ToLong);
        
        /// <summary>Create a game feature that takes one number.</summary>
        public static GameFeature<double> GameDouble(string id) => new(id, ToDouble);
        
        /// <summary>Create a game feature that takes one number.</summary>
        public static GameFeature<float> GameFloat(string id) => new(id, ToFloat);
        
        /// <summary>Create a game feature that takes one string.</summary>
        public static GameFeature<string> GameString(string id) => new(id, ToString);
        
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

        /// <summary>Create a game feature that takes a color.</summary>
        public static GameFeature<Color> GameColor(string id) => new(id, ToColor);

        /// <summary>Create a game feature that takes a palette-modified color.</summary>
        public static GameFeature<PaletteColor> GamePaletteColor(string id) => new(id, ToPaletteColor);

        /// <summary>Create a game feature that takes one boolean.</summary>
        public static GameFeature<bool> GameBool(string id) => new(id, ToBool);

        /// <summary>Create a game feature that takes one enum value.</summary>
        public static GameFeature<T> GameEnum<T>(string id) where T : struct => new(id, ToEnum<T>);

        /// <summary>Create a game feature that takes one enum value.</summary>
        public static GameFeature<T> GameExtEnum<T>(string id) where T : ExtEnum<T> => new(id, ToExtEnum<T>);
    }
}
