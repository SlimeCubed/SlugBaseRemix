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
        internal const string SAVE_DATA_PREFIX = "_SlugBaseSaveData_";
        internal static readonly ConditionalWeakTable<MiscWorldSaveData, SlugBaseSaveData> WorldData = new();
        internal static readonly ConditionalWeakTable<PlayerProgression.MiscProgressionData, SlugBaseSaveData> ProgressionData = new();
        internal static readonly ConditionalWeakTable<DeathPersistentSaveData, SlugBaseSaveData> DeathPersistentData = new();

        private readonly Dictionary<string, object> _data;
        private readonly List<string> _unrecognizedSaveStrings;

        internal SlugBaseSaveData(List<string> unrecognizedSaveStrings)
        {
            _data = new Dictionary<string, object>();
            _unrecognizedSaveStrings = unrecognizedSaveStrings;
        }

        /// <summary>
        /// Gets a value from the save data.
        /// </summary>
        /// <param name="key">The key for retrieving the value.</param>
        /// <param name="value">The stored value.</param>
        /// <typeparam name="T">The value's type.</typeparam>
        /// <returns><c>true</c> if a stored value was found, <c>false</c> otherwise.</returns>
        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var obj) && obj is T castObj)
            {
                value = castObj;
                return true;
            }

            if (LoadStringFromUnrecognizedStrings(key, out var stringValue))
            {
                value = JsonConvert.DeserializeObject<T>(stringValue);
                _data[key] = value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Sets a <see cref="object"/> value on the save data.
        /// </summary>
        /// <param name="key">The key for string the value.</param>
        /// <param name="value">The value to be stored.</param>
        /// <typeparam name="T">The value's type.</typeparam>
        public void Set<T>(string key, T value)
        {
            _data[key] = value;
        }

        internal void SaveToStrings(List<string> strings)
        {
            foreach(var pair in _data)
            {
                SavePairToStrings(strings, pair.Key, JsonConvert.SerializeObject(pair.Value));
            }
        }

        private static void SavePairToStrings(List<string> strings, string key, string value)
        {
            var prefix = key + SAVE_DATA_PREFIX;
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

        internal bool LoadStringFromUnrecognizedStrings(string key, out string value)
        {
            var prefix = key + SAVE_DATA_PREFIX;

            foreach (var s in _unrecognizedSaveStrings)
            {
                if (s.StartsWith(prefix))
                {
                    value = Encoding.UTF8.GetString(Convert.FromBase64String(s.Substring(prefix.Length)));
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}