using IL.Menu.Remix;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlugBase
{
    /// <summary>
    /// Converts to and from JSON values.
    /// </summary>
    public static class JsonConverter
    {
        /// <summary>Create a read-only <see cref="JsonObject"/> from a copy of <paramref name="obj"/>.</summary>
        /// <remarks>If <c>null</c> values are valid, use <see cref="ToJsonAny(object)"/> instead.</remarks>
        /// <exception cref="ArgumentException">An object in <paramref name="obj"/> could not be converted to JSON.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> was <c>null</c>.</exception>
        public static JsonObject ToJson(Dictionary<string, object> obj) => obj != null ? new(DeepClone(obj), null) : throw new ArgumentNullException(nameof(obj));

        /// <summary>Create a read-only <see cref="JsonList"/> from a copy of <paramref name="list"/>.</summary>
        /// <remarks>If <c>null</c> values are valid, use <see cref="ToJsonAny(object)"/> instead.</remarks>
        /// <exception cref="ArgumentException">An object in <paramref name="list"/> could not be converted to JSON.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> was <c>null</c>.</exception>
        public static JsonList ToJson(List<object> list) => list != null ? new(DeepClone(list), null) : throw new ArgumentNullException(nameof(list));

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="string"/>.</summary>
        public static JsonAny ToJson(string value) => new(value, null);

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="bool"/>.</summary>
        public static JsonAny ToJson(bool value) => new(value, null);

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="double"/>.</summary>
        public static JsonAny ToJson(double value) => new(value, null);

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="float"/>.</summary>
        public static JsonAny ToJson(float value) => new((double)value, null);

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="int"/>.</summary>
        public static JsonAny ToJson(int value) => new((double)value, null);

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="uint"/>.</summary>
        public static JsonAny ToJson(uint value) => new((double)value, null);

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="short"/>.</summary>
        public static JsonAny ToJson(short value) => new((double)value, null);

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="ushort"/>.</summary>
        public static JsonAny ToJson(ushort value) => new((double)value, null);

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="long"/>. <paramref name="value"/> is converted to a <see cref="double"/>, so very large numbers may lose precision.</summary>
        public static JsonAny ToJson(long value) => new((double)value, null);

        /// <summary>Create a <see cref="JsonAny"/> from a single <see cref="ulong"/>. <paramref name="value"/> is converted to a <see cref="double"/>, so very large numbers may lose precision.</summary>
        public static JsonAny ToJson(ulong value) => new((double)value, null);

        /// <summary>Create a read-only <see cref="JsonAny"/> from a copy of <paramref name="value"/>.</summary>
        /// <exception cref="ArgumentException">An object in the list or dictionary could not be converted to JSON.</exception>
        public static JsonAny ToJsonAny(object value) => new(DeepClone(value), null);

        /// <summary>
        /// Create a mutable copy of a <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="value">The <see cref="JsonObject"/> to copy.</param>
        /// <returns>A copy of <paramref name="value"/> as a <see cref="Dictionary{String, Object}"/>.</returns>
        public static Dictionary<string, object> ToDictionary(JsonObject value) => DeepClone(value._dict);

        /// <summary>
        /// Create a mutable copy of a <see cref="JsonList"/>.
        /// </summary>
        /// <param name="value">The <see cref="JsonList"/> to copy.</param>
        /// <returns>A copy of <paramref name="value"/> as a <see cref="List{Object}"/>.</returns>
        public static List<object> ToList(JsonList value) => DeepClone(value._list);

        /// <summary>
        /// Create a mutable copy of a <see cref="JsonAny"/>.
        /// </summary>
        /// <param name="value">The <see cref="JsonAny"/> to copy.</param>
        /// <returns>A copy of <paramref name="value"/> as a <see cref="List{Object}"/>, <see cref="Dictionary{String, Object}"/>, <see cref="string"/>, <see cref="bool"/>, <see cref="double"/>, or <c>null</c>.</returns>
        public static object ToObject(JsonAny value) => DeepClone(value._object);

        private static Dictionary<string, object> DeepClone(Dictionary<string, object> src)
        {
            var dst = new Dictionary<string, object>(src);

            foreach(var pair in src)
                dst[pair.Key] = DeepClone(pair.Value);

            return dst;
        }

        private static List<object> DeepClone(List<object> src)
        {
            var dst = new List<object>(src);

            for(int i = 0; i < src.Count; i++)
                dst[i] = DeepClone(src[i]);

            return dst;
        }

        private static object DeepClone(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("JSON values may not be null!");

            return obj switch
            {
                string i => i,
                bool i => i,
                double i => i,
                float i => (double)i,
                int i => (double)i,
                uint i => (double)i,
                short i => (double)i,
                ushort i => (double)i,
                long i => (double)i,
                ulong i => (double)i,
                Dictionary<string, object> dict => DeepClone(dict),
                List<object> list => DeepClone(list),
                null => null,
                _ => throw new ArgumentException($"Type could not be converted to JSON: {obj.GetType().Name}"),
            };
        }
    }
}
