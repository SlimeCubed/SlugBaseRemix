using System;
using System.Collections;
using System.Collections.Generic;

namespace SlugBase
{
    public struct JsonAny
    {
        internal readonly object _object;
        internal readonly JsonPathNode _path;

        internal JsonAny(object obj, JsonPathNode path)
        {
            _object = obj;
            _path = path;

            if (Type == Element.Invalid)
                throw new JsonException("Failed to parse text as JSON!", _path);
        }

        public JsonObject AsObject()
        {
            if (_object is Dictionary<string, object> dict) return new JsonObject(dict, _path);
            else throw new JsonException($"Json {Type} cannot be converted to Object!", _path);
        }

        public JsonList AsList()
        {
            if (_object is List<object> list) return new JsonList(list, _path);
            else throw new JsonException($"Json {Type} cannot be converted to List!", _path);
        }

        public double AsDouble()
        {
            if (_object is double d) return d;
            else if (_object is long l) return (double)l;
            else throw new JsonException($"Json {Type} cannot be converted to Float!", _path);
        }

        public long AsLong()
        {
            if (_object is double d) return (long)d;
            else if (_object is long l) return l;
            else throw new JsonException($"Json {Type} cannot be converted to Integer!", _path);
        }

        public string AsString()
        {
            if (_object is string str) return str;
            else throw new JsonException($"Json {Type} cannot be converted to String!", _path);
        }

        public int AsInt() => (int)AsLong();
        public float AsFloat() => (float)AsDouble();


        public JsonObject? TryObject() => Type == Element.Object ? AsObject() : null;
        public JsonList? TryList() => Type == Element.List ? AsList() : null;
        public long? TryLong() => Type == Element.Integer ? AsLong() : null;
        public int? TryInt() => Type == Element.Integer ? AsInt() : null;
        public double? TryDouble() => Type == Element.Float ? AsDouble() : null;
        public float? TryFloat() => Type == Element.Float ? AsFloat() : null;
        public string TryString() => Type == Element.String ? AsString() : null;

        public static JsonObject AsObject(JsonAny json) => json.AsObject();
        public static JsonList AsList(JsonAny json) => json.AsList();
        public static double AsDouble(JsonAny json) => json.AsDouble();
        public static float AsFloat(JsonAny json) => json.AsFloat();
        public static long AsLong(JsonAny json) => json.AsLong();
        public static int AsInt(JsonAny json) => json.AsInt();
        public static string AsString(JsonAny json) => json.AsString();

        public static JsonAny Parse(string data) => new JsonAny(Json.Deserialize(data), new JsonPathNode("root", null));

        private Element Type => _object switch
        {
            Dictionary<string, object> => Element.Object,
            List<object> => Element.List,
            long => Element.Integer,
            double => Element.Float,
            string => Element.String,
            _ => Element.Invalid
        };

        enum Element
        {
            Object,
            List,
            Integer,
            Float,
            String,
            Invalid
        }
    }

    public struct JsonObject : IEnumerable<KeyValuePair<string, JsonAny>>
    {
        internal readonly Dictionary<string, object> _dict;
        internal readonly JsonPathNode _path;

        internal JsonObject(Dictionary<string, object> dict, JsonPathNode path)
        {
            _dict = dict;
            _path = path;
        }

        public JsonAny Get(string key)
        {
            if (!_dict.TryGetValue(key, out object value))
                throw new JsonException($"Missing \"{key}\" property!", _path);

            return new JsonAny(value, new("." + key, _path));
        }

        public JsonAny? TryGet(string key)
        {
            if (_dict.TryGetValue(key, out object value))
                return new JsonAny(value, new("." + key, _path));
            else
                return null;
        }

        public JsonObject GetObject(string key) => Get(key).AsObject();
        public JsonList GetList(string key) => Get(key).AsList();
        public double GetDouble(string key) => Get(key).AsDouble();
        public float GetFloat(string key) => Get(key).AsFloat();
        public long GetLong(string key) => Get(key).AsLong();
        public int GetInt(string key) => Get(key).AsInt();
        public string GetString(string key) => Get(key).AsString();

        public JsonAny this[string key] => Get(key);

        public IEnumerator<KeyValuePair<string, JsonAny>> GetEnumerator()
        {
            foreach (var key in _dict.Keys)
                yield return new(key, Get(key));
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct JsonList : IReadOnlyList<JsonAny>
    {
        internal readonly List<object> _list;
        internal readonly JsonPathNode _path;

        internal JsonList(List<object> list, JsonPathNode path)
        {
            _list = list;
            _path = path;
        }

        public JsonAny Get(int key)
        {
            if (key < 0)
                throw new JsonException($"Index {key} may not be negative!", _path);
            else if (key >= _list.Count)
                throw new JsonException($"Index {key} is out of range for a list of size {_list.Count}!", _path);

            return new JsonAny(_list[key], new($"[{key}]", _path));
        }

        public JsonAny? TryGet(int key)
        {
            if (key >= 0 && key < _list.Count)
                return new JsonAny(_list[key], new($"[{key}]", _path));
            else
                return null;

        }

        public JsonObject GetObject(int key) => Get(key).AsObject();
        public JsonList GetList(int key) => Get(key).AsList();
        public double GetDouble(int key) => Get(key).AsDouble();
        public float GetFloat(int key) => Get(key).AsFloat();
        public long GetLong(int key) => Get(key).AsLong();
        public int GetInt(int key) => Get(key).AsInt();
        public string GetString(int key) => Get(key).AsString();

        public int Count => _list.Count;
        public JsonAny this[int key] => Get(key);

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

    public class JsonException : Exception
    {
        public string JsonPath { get; set; }

        public JsonException() { }
        public JsonException(string message) : base(message) { }
        public JsonException(string message, Exception inner) : base(message, inner) { }

        internal JsonException(JsonPathNode path) => JsonPath = path.ToString();
        internal JsonException(string message, JsonPathNode path) : base(message) => JsonPath = path.ToString();
        internal JsonException(string message, Exception inner, JsonPathNode path) : base(message, inner) => JsonPath = path.ToString();
    }
}
