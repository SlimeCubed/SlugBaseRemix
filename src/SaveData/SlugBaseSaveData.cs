using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace SlugBase.SaveData
{
    /// <summary>
    /// A helper for interacting with the game's save data.
    /// </summary>
    public class SlugBaseSaveData
    {
        internal const string KEY_SUFFIX = "_SlugBaseSaveData_";
        internal const string KEY_SUFFIX_INTERNAL = "_SlugBaseInternal_";
        internal static readonly ConditionalWeakTable<MiscWorldSaveData, SlugBaseSaveData> WorldData = new();
        internal static readonly ConditionalWeakTable<PlayerProgression.MiscProgressionData, SlugBaseSaveData> ProgressionData = new();
        internal static readonly ConditionalWeakTable<DeathPersistentSaveData, SlugBaseSaveData> DeathPersistentData = new();

        private readonly Dictionary<string, object> _data;
        private readonly Dictionary<string, object> _internalData;
        private readonly List<string> _unrecognizedSaveStrings;

        internal SlugBaseSaveData(List<string> unrecognizedSaveStrings)
        {
            _data = new Dictionary<string, object>();
            _internalData = new Dictionary<string, object>();
            _unrecognizedSaveStrings = unrecognizedSaveStrings;
        }

        /// <summary>
        /// Gets a value from the save data.
        /// </summary>
        /// <param name="key">The key for retrieving the value.</param>
        /// <param name="value">The stored value.</param>
        /// <typeparam name="T">The value's type.</typeparam>
        /// <returns><c>true</c> if a stored value was found, <c>false</c> otherwise.</returns>
        public bool TryGet<T>(string key, out T value) => TryGet(key + KEY_SUFFIX, _data, out value);

        internal bool TryGetInternal<T>(string key, out T value) => TryGet(key + KEY_SUFFIX_INTERNAL, _internalData, out value);

        private bool TryGet<T>(string key, Dictionary<string, object> pairs, out T value)
        {
            if (pairs.TryGetValue(key, out var obj) && obj is T castObj)
            {
                value = castObj;
                return true;
            }

            if (LoadStringFromUnrecognizedStrings(key, out var stringValue))
            {
                value = JsonConvert.DeserializeObject<T>(stringValue);
                pairs[key] = value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Sets or adds a value to the save data.
        /// </summary>
        /// <param name="key">The key for storing the value.</param>
        /// <param name="value">The value to be stored.</param>
        /// <typeparam name="T">The value's type.</typeparam>
        public void Set<T>(string key, T value)
        {
            _data[key + KEY_SUFFIX] = value;
        }

        internal void SetInternal<T>(string key, T value)
        {
            _internalData[key + KEY_SUFFIX_INTERNAL] = value;
        }

        /// <summary>
        /// Removes a value from the save data.
        /// </summary>
        /// <param name="key">The key for removing the value.</param>
        /// <returns><see langword="true"/> if the key was found and removed, <see langword="false"/> otherwise.</returns>
        public bool Remove(string key)
        {
            return _data.Remove(key);
        }

        internal bool RemoveInternal(string key)
        {
            return _internalData.Remove(key);
        }

        internal void SaveToStrings(List<string> strings)
        {
            foreach(var pair in _data)
            {
                SavePairToStrings(strings, pair.Key, JsonConvert.SerializeObject(pair.Value));
            }
        }

        internal bool LoadStringFromUnrecognizedStrings(string key, out string value)
        {
            foreach (var s in _unrecognizedSaveStrings)
            {
                if (s.StartsWith(key))
                {
                    value = Encoding.UTF8.GetString(Convert.FromBase64String(s.Substring(key.Length)));
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static void SavePairToStrings(List<string> strings, string key, string value)
        {
            var prefix = key + KEY_SUFFIX;
            var dataToStore = prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

            for (var i = 0; i < strings.Count; i++)
            {
                if (strings[i].StartsWith(prefix))
                {
                    strings[i] = dataToStore;
                    return;
                }
            }

            strings.Add(dataToStore);
        }
    }
}