using System;
using System.Collections;
using System.Collections.Generic;

namespace SlugBase
{
    /// <summary>
    /// A JSON object, list, number, or string.
    /// </summary>
    public readonly struct JsonAny
    {
        internal readonly object _object;
        internal readonly JsonPathNode _path;

        internal JsonAny(object obj, JsonPathNode path)
        {
            _object = obj;
            _path = path;
        }

        /// <summary>Cast to <see cref="JsonObject"/>.</summary>
        /// <exception cref="JsonException">This isn't a JSON object.</exception>
        public JsonObject AsObject()
        {
            if (_object is Dictionary<string, object> dict) return new JsonObject(dict, _path);
            else throw new JsonException($"Json {Type} cannot be converted to Object!", this);
        }

        /// <summary>Cast to <see cref="JsonList"/>.</summary>
        /// <exception cref="JsonException">This isn't a JSON list.</exception>
        public JsonList AsList()
        {
            if (_object is List<object> list) return new JsonList(list, _path);
            else throw new JsonException($"Json {Type} cannot be converted to List!", this);
        }

        /// <summary>Cast to <see cref="double"/>.</summary>
        /// <exception cref="JsonException">This isn't a number.</exception>
        public double AsDouble()
        {
            if (_object is double d) return d;
            else throw new JsonException($"Json {Type} cannot be converted to Double!", this);
        }

        /// <summary>Cast to <see cref="long"/>.</summary>
        /// <exception cref="JsonException">This isn't a number.</exception>
        public long AsLong() => (long)AsDouble();

        /// <summary>Cast to <see cref="string"/>.</summary>
        /// <exception cref="JsonException">This isn't a string.</exception>
        public string AsString()
        {
            if (_object is string str) return str;
            else throw new JsonException($"Json {Type} cannot be converted to String!", this);
        }

        /// <summary>Cast to <see cref="int"/>.</summary>
        /// <exception cref="JsonException">This isn't a number.</exception>
        public int AsInt() => (int)AsDouble();

        /// <summary>Cast to <see cref="float"/>.</summary>
        /// <exception cref="JsonException">This isn't a number.</exception>
        public float AsFloat() => (float)AsDouble();

        /// <summary>Cast to <see cref="bool"/>.</summary>
        /// <exception cref="JsonException">This isn't a boolean.</exception>
        public bool AsBool()
        {
            if (_object is bool b) return b;
            else throw new JsonException($"Json {Type} cannot be converted to Bool!", this);
        }


        /// <summary>Try casting to <see cref="JsonObject"/>, returning <c>null</c> on failure.</summary>
        public JsonObject? TryObject() => Type == Element.Object ? AsObject() : null;

        /// <summary>Try casting to <see cref="JsonList"/>, returning <c>null</c> on failure.</summary>
        public JsonList? TryList() => Type == Element.List ? AsList() : null;

        /// <summary>Try casting to <see cref="long"/>, returning <c>null</c> on failure.</summary>
        public long? TryLong() => Type == Element.Number ? AsLong() : null;

        /// <summary>Try casting to <see cref="int"/>, returning <c>null</c> on failure.</summary>
        public int? TryInt() => Type == Element.Number ? AsInt() : null;

        /// <summary>Try casting to <see cref="double"/>, returning <c>null</c> on failure.</summary>
        public double? TryDouble() => Type == Element.Number ? AsDouble() : null;

        /// <summary>Try casting to <see cref="float"/>, returning <c>null</c> on failure.</summary>
        public float? TryFloat() => Type == Element.Number ? AsFloat() : null;

        /// <summary>Try casting to <see cref="string"/>, returning <c>null</c> on failure.</summary>
        public string TryString() => Type == Element.String ? AsString() : null;

        /// <summary>Try casting to <see cref="bool"/>, returning <c>null</c> on failure.</summary>
        public bool? TryBool() => Type == Element.Bool ? AsBool() : null;

        /// <summary>Test if this value is <c>null</c>.</summary>
        public bool IsNull() => Type == Element.Null;


        /// <summary>Cast to <see cref="JsonObject"/>.</summary>
        /// <exception cref="JsonException"><paramref name="json"/> isn't a JSON object.</exception>
        public static JsonObject AsObject(JsonAny json) => json.AsObject();

        /// <summary>Cast to <see cref="JsonList"/>.</summary>
        /// <exception cref="JsonException"><paramref name="json"/> isn't a JSON list.</exception>
        public static JsonList AsList(JsonAny json) => json.AsList();

        /// <summary>Cast to <see cref="double"/>.</summary>
        /// <exception cref="JsonException"><paramref name="json"/> isn't a number.</exception>
        public static double AsDouble(JsonAny json) => json.AsDouble();

        /// <summary>Cast to <see cref="float"/>.</summary>
        /// <exception cref="JsonException"><paramref name="json"/> isn't a number.</exception>
        public static float AsFloat(JsonAny json) => json.AsFloat();

        /// <summary>Cast to <see cref="long"/>.</summary>
        /// <exception cref="JsonException"><paramref name="json"/> isn't a number.</exception>
        public static long AsLong(JsonAny json) => json.AsLong();

        /// <summary>Cast to <see cref="int"/>.</summary>
        /// <exception cref="JsonException"><paramref name="json"/> isn't a number.</exception>
        public static int AsInt(JsonAny json) => json.AsInt();

        /// <summary>Cast to <see cref="string"/>.</summary>
        /// <exception cref="JsonException"><paramref name="json"/> isn't a string.</exception>
        public static string AsString(JsonAny json) => json.AsString();

        /// <summary>Cast to <see cref="bool"/>.</summary>
        /// <exception cref="JsonException"><paramref name="json"/> isn't a boolean.</exception>
        public static bool AsBool(JsonAny json) => json.AsBool();

        /// <summary>
        /// Parse JSON text as a <see cref="JsonAny"/>.
        /// </summary>
        /// <param name="data">The JSON text.</param>
        /// <returns>A <see cref="JsonAny"/> representing the root element.</returns>
        /// <exception cref="JsonParseException">The root element could not be parsed.</exception>
        public static JsonAny Parse(string data) => new(JsonParser.Parse(data), new JsonPathNode("root", null));

        /// <summary>
        /// The type of this element.
        /// </summary>
        public Element Type => _object switch
        {
            Dictionary<string, object> => Element.Object,
            List<object> => Element.List,
            double => Element.Number,
            string => Element.String,
            bool => Element.Bool,
            null => Element.Null,
            _ => throw new InvalidOperationException("Invalid value in JsonAny!")
        };

        /// <summary>
        /// Represents the type of a JSON element.
        /// </summary>
        public enum Element
        {
            /// <summary>
            /// Values associated with string keys.
            /// </summary>
            Object,
            /// <summary>
            /// Values associated with integer keys.
            /// </summary>
            List,
            /// <summary>
            /// A number.
            /// </summary>
            Number,
            /// <summary>
            /// A string.
            /// </summary>
            String,
            /// <summary>
            /// A boolean.
            /// </summary>
            Bool,
            /// <summary>
            /// A <c>null</c> value.
            /// </summary>
            Null
        }
    }

    /// <summary>
    /// A JSON object.
    /// </summary>
    public readonly struct JsonObject : IEnumerable<KeyValuePair<string, JsonAny>>
    {
        internal readonly Dictionary<string, object> _dict;
        internal readonly JsonPathNode _path;

        internal JsonObject(Dictionary<string, object> dict, JsonPathNode path)
        {
            _dict = dict;
            _path = path;
        }

        /// <summary>
        /// Get an element from this object.
        /// </summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <returns>The found value.</returns>
        /// <exception cref="JsonException">The specified property doesn't exist.</exception>
        public JsonAny Get(string key)
        {
            if (!_dict.TryGetValue(key, out object value))
                throw new JsonException($"Missing \"{key}\" property!", this);

            return new JsonAny(value, new("." + key, _path));
        }

        /// <summary>
        /// Get an element from this object.
        /// </summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <returns>The found value, or <c>null</c> if the property doesn't exist.</returns>
        public JsonAny? TryGet(string key)
        {
            if (_dict.TryGetValue(key, out object value))
                return new JsonAny(value, new("." + key, _path));
            else
                return null;
        }

        /// <summary>Get a <see cref="JsonObject"/> from this object.</summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <exception cref="JsonException">The specified property doesn't exist.</exception>
        public JsonObject GetObject(string key) => Get(key).AsObject();

        /// <summary>Get a <see cref="JsonList"/> from this object.</summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <exception cref="JsonException">The specified property doesn't exist or was the wrong type.</exception>
        public JsonList GetList(string key) => Get(key).AsList();

        /// <summary>Get a <see cref="double"/> from this object.</summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <exception cref="JsonException">The specified property doesn't exist or was the wrong type.</exception>
        public double GetDouble(string key) => Get(key).AsDouble();

        /// <summary>Get a <see cref="float"/> from this object.</summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <exception cref="JsonException">The specified property doesn't exist or was the wrong type.</exception>
        public float GetFloat(string key) => Get(key).AsFloat();

        /// <summary>Get a <see cref="long"/> from this object.</summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <exception cref="JsonException">The specified property doesn't exist or was the wrong type.</exception>
        public long GetLong(string key) => Get(key).AsLong();

        /// <summary>Get an <see cref="int"/> from this object.</summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <exception cref="JsonException">The specified property doesn't exist or was the wrong type.</exception>
        public int GetInt(string key) => Get(key).AsInt();

        /// <summary>Get a <see cref="string"/> from this object.</summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <exception cref="JsonException">The specified property doesn't exist or was the wrong type.</exception>
        public string GetString(string key) => Get(key).AsString();

        /// <summary>Get a <see cref="bool"/> from this object.</summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <exception cref="JsonException">The specified property doesn't exist or was the wrong type.</exception>
        public bool GetBool(string key) => Get(key).AsBool();

        /// <summary>
        /// Get an element from this object.
        /// </summary>
        /// <param name="key">The JSON property to search for.</param>
        /// <returns>The found value.</returns>
        /// <exception cref="JsonException">The specified property doesn't exist.</exception>
        public JsonAny this[string key] => Get(key);

        /// <summary>
        /// Get <paramref name="obj"/> as a <see cref="JsonAny"/>.
        /// </summary>
        public static implicit operator JsonAny(JsonObject obj) => new JsonAny(obj._dict, obj._path);

        /// <summary>
        /// Get an enumerator for all properties and values of this object.
        /// </summary>
        public IEnumerator<KeyValuePair<string, JsonAny>> GetEnumerator()
        {
            foreach (var key in _dict.Keys)
                yield return new(key, Get(key));
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A JSON list.
    /// </summary>
    public readonly struct JsonList : IReadOnlyList<JsonAny>
    {
        internal readonly List<object> _list;
        internal readonly JsonPathNode _path;

        internal JsonList(List<object> list, JsonPathNode path)
        {
            _list = list;
            _path = path;
        }

        /// <summary>
        /// Get an element from this list.
        /// </summary>
        /// <param name="key">The index.</param>
        /// <returns>The found value.</returns>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range.</exception>
        public JsonAny Get(int key)
        {
            if (key < 0)
                throw new JsonException($"Index {key} may not be negative!", this);
            else if (key >= _list.Count)
                throw new JsonException($"Index {key} is out of range for a list of size {_list.Count}!", this);

            return new JsonAny(_list[key], new($"[{key}]", _path));
        }

        /// <summary>
        /// Get an element from this list.
        /// </summary>
        /// <param name="key">The index.</param>
        /// <returns>The found value, or <c>null</c> if <paramref name="key"/> is out of range.</returns>
        public JsonAny? TryGet(int key)
        {
            if (key >= 0 && key < _list.Count)
                return new JsonAny(_list[key], new($"[{key}]", _path));
            else
                return null;

        }

        /// <summary>Get a <see cref="JsonObject"/> from this list.</summary>
        /// <param name="key">The index.</param>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range or is not the right type.</exception>
        public JsonObject GetObject(int key) => Get(key).AsObject();

        /// <summary>Get a <see cref="JsonList"/> from this list.</summary>
        /// <param name="key">The index.</param>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range or is not the right type.</exception>
        public JsonList GetList(int key) => Get(key).AsList();

        /// <summary>Get a <see cref="double"/> from this list.</summary>
        /// <param name="key">The index.</param>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range or is not the right type.</exception>
        public double GetDouble(int key) => Get(key).AsDouble();
        
        /// <summary>Get a <see cref="float"/> from this list.</summary>
        /// <param name="key">The index.</param>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range or is not the right type.</exception>
        public float GetFloat(int key) => Get(key).AsFloat();
        
        /// <summary>Get a <see cref="long"/> from this list.</summary>
        /// <param name="key">The index.</param>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range or is not the right type.</exception>
        public long GetLong(int key) => Get(key).AsLong();
        
        /// <summary>Get an <see cref="int"/> from this list.</summary>
        /// <param name="key">The index.</param>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range or is not the right type.</exception>
        public int GetInt(int key) => Get(key).AsInt();
        
        /// <summary>Get a <see cref="string"/> from this list.</summary>
        /// <param name="key">The index.</param>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range or is not the right type.</exception>
        public string GetString(int key) => Get(key).AsString();

        /// <summary>Get a <see cref="bool"/> from this list.</summary>
        /// <param name="key">The index.</param>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range or is not the right type.</exception>
        public bool GetBool(int key) => Get(key).AsBool();

        /// <summary>
        /// Get the number of elements in this list.
        /// </summary>
        public int Count => _list.Count;

        /// <summary>
        /// Get an element from this list.
        /// </summary>
        /// <param name="key">The index.</param>
        /// <returns>The found value.</returns>
        /// <exception cref="JsonException"><paramref name="key"/> is out of range.</exception>
        public JsonAny this[int key] => Get(key);

        /// <summary>
        /// Get <paramref name="list"/> as a <see cref="JsonAny"/>.
        /// </summary>
        public static implicit operator JsonAny(JsonList list) => new JsonAny(list._list, list._path);

        /// <summary>
        /// Gets an enumerator for all elements of this list.
        /// </summary>
        public IEnumerator<JsonAny> GetEnumerator()
        {
            for(int i = 0; i < _list.Count; i++)
                yield return Get(i);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class JsonPathNode
    {
        private readonly string _text;
        private readonly JsonPathNode _prev;

        public JsonPathNode(string text, JsonPathNode prev)
        {
            _text = text;
            _prev = prev;
        }

        public override string ToString()
        {
            var parts = new List<string>();
            var node = this;

            while (node != null && parts.Count < 100)
            {
                parts.Add(node._text);
                node = node._prev;
            }

            parts.Reverse();
            return string.Concat(parts);
        }
    }

    /// <summary>
    /// Represents errors that occur when accessing JSON data.
    /// </summary>
    public class JsonException : Exception
    {
        /// <summary>
        /// The path to the element that failed to parse, starting from the root object.
        /// </summary>
        public string JsonPath { get; set; }
        
        /// <summary>Initializes a new instance of the <see cref="JsonException"/> class with a path to the invalid element.</summary>
        public JsonException(JsonAny json) => JsonPath = json._path?.ToString();

        /// <summary>Initializes a new instance of the <see cref="JsonException"/> class with a specified error message
        /// and a path to the invalid element.</summary>
        public JsonException(string message, JsonAny json) : base(message) => JsonPath = json._path?.ToString();

        /// <summary>Initializes a new instance of the <see cref="JsonException"/> class with a specified error message,
        /// a reference to the inner exception that is the cause of this exception, and a path to the invalid element.</summary>
        public JsonException(string message, Exception inner, JsonAny json) : base(message, inner) => JsonPath = json._path?.ToString();

        internal JsonException(JsonPathNode path) => JsonPath = path?.ToString();

        internal JsonException(string message, JsonPathNode path) : base(message) => JsonPath = path?.ToString();

        internal JsonException(string message, Exception inner, JsonPathNode path) : base(message, inner) => JsonPath = path?.ToString();
    }
}
