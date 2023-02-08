using SlugBase.DataTypes;
using System;
using System.Linq;
using UnityEngine;

namespace SlugBase
{
    /// <summary>
    /// Converts <see cref="JsonAny"/> to other types.
    /// </summary>
    public static class JsonUtils
    {
        private static float ToColorElement(JsonAny json)
        {
            switch (json.Type)
            {
                case JsonAny.Element.Integer:
                    long longVal = json.AsLong();
                    if (longVal < 0 || longVal > 255)
                        throw new JsonException("Integer color element was out of range!", json);
                    return longVal / 255f;

                case JsonAny.Element.Float:
                    float floatVal = json.AsFloat();
                    if (floatVal < 0f || floatVal > 1f)
                        throw new JsonException("Float color element was out of range!", json);
                    return floatVal;

                default:
                    throw new JsonException("Color element wasn't a float or integer!", json);
            }
        }

        ///<summary>Convert to <see cref="long"/>.</summary>
        public static long ToLong(JsonAny json) => json.AsLong();

        ///<summary>Convert to <see cref="int"/>.</summary>
        public static int ToInt(JsonAny json) => json.AsInt();

        ///<summary>Convert to <see cref="double"/>.</summary>
        public static double ToDouble(JsonAny json) => json.AsDouble();

        ///<summary>Convert to <see cref="float"/>.</summary>
        public static float ToFloat(JsonAny json) => json.AsFloat();

        ///<summary>Convert to <see cref="string"/>.</summary>
        public static string ToString(JsonAny json) => json.AsString();

        ///<summary>Convert to <see cref="bool"/>.</summary>
        public static bool ToBool(JsonAny json) => json.AsBool();

        /// <summary>
        /// Convert to <see cref="Color"/>.
        /// </summary>
        /// <remarks>
        /// This may be a hex string or equivalent integer; list of components; or object with "r", "g", "b", and possibly "a" properties.
        /// </remarks>
        public static Color ToColor(JsonAny json)
        {
            switch (json.Type)
            {
                case JsonAny.Element.String:
                    try
                    {
                        return RXUtils.GetColorFromHex(json.AsString());
                    }
                    catch (Exception e)
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
                    for (int i = 0; i < list.Count; i++)
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

        ///<summary>Convert to <see cref="PlayerColor"/> via <see cref="PlayerColor(JsonAny)"/>.</summary>
        public static PlayerColor ToPlayerColor(JsonAny json) => new(json);

        ///<summary>Convert list to <see cref="long"/>[].</summary>
        public static long[] ToLongs(JsonAny json) => json.TryLong() == null ? json.AsList().Select(ToLong).ToArray() : new[] { json.AsLong() };

        ///<summary>Convert list to <see cref="int"/>[].</summary>
        public static int[] ToInts(JsonAny json) => json.TryInt() == null ? json.AsList().Select(ToInt).ToArray() : new[] { json.AsInt() };

        ///<summary>Convert list to <see cref="double"/>[].</summary>
        public static double[] ToDoubles(JsonAny json) => json.TryDouble() == null ? json.AsList().Select(ToDouble).ToArray() : new[] { json.AsDouble() };

        ///<summary>Convert list to <see cref="float"/>[].</summary>
        public static float[] ToFloats(JsonAny json) => json.TryFloat() == null ? json.AsList().Select(ToFloat).ToArray() : new[] { json.AsFloat() };

        ///<summary>Convert list to <see cref="string"/>[].</summary>
        public static string[] ToStrings(JsonAny json) => json.TryString() == null ? json.AsList().Select(ToString).ToArray() : new[] { json.AsString() };

        ///<summary>Convert to <see cref="Enum"/> value.</summary>
        public static T ToEnum<T>(JsonAny json) where T : struct
        {
            if (Enum.TryParse(json.AsString(), true, out T value))
                return value;
            else
                throw new JsonException($"\"{json.AsString()}\" was not a value of \"{typeof(T).Name}\"!", json);
        }

        /// <summary>
        /// Convert to <see cref="ExtEnum{T}"/> value.
        /// </summary>
        /// <remarks>
        /// <see cref="ExtEnumBase.Index"/> will be -1 for values that could not be parsed.
        /// </remarks>
        public static T ToExtEnum<T>(JsonAny json) where T : ExtEnum<T>
        {
            //// Fails if the enum hasn't loaded yet
            //if (ExtEnumBase.TryParse(typeof(T), json.AsString(), true, out var value))
            //    return (T)value;
            //else
            //    throw new JsonException($"\"{json.AsString()}\" was not a value of \"{typeof(T).Name}\"!", json);

            return (T)Activator.CreateInstance(typeof(T), json.AsString(), false);
        }

        /// <summary>
        /// Convert to <see cref="Vector2"/>
        /// </summary>
        /// <remarks>
        /// This may be a list of components or an object with "x" and "y" properties.
        /// </remarks>
        public static Vector2 ToVector2(JsonAny json)
        {
            switch (json.Type)
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
    }
}
