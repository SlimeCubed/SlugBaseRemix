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
        public bool TryGet<T>(string key, out T value) => TryGetRaw(key + KEY_SUFFIX, out value);

        internal bool TryGetInternal<T>(string key, out T value) => TryGetRaw(key + KEY_SUFFIX_INTERNAL, out value);

        private bool TryGetRaw<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var obj) && obj is T castObj)
            {
                value = castObj;
                return true;
            }

            if (LoadStringFromUnrecognizedStrings(key, out var stringValue))
            {
                try
                {
                    value = JsonConvert.DeserializeObject<T>(stringValue);
                }
                catch(Exception e)
                {
                    SlugBasePlugin.Logger.LogError($"Failed to convert key \"{key}\" to {typeof(T).Name}: \"{stringValue}\"");
                    UnityEngine.Debug.LogException(e);
                    value = default;
                    return false;
                }
                _data[key] = value;
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
            _data[key + KEY_SUFFIX_INTERNAL] = value;
        }

        /// <summary>
        /// Removes a value from the save data.
        /// </summary>
        /// <param name="key">The key for removing the value.</param>
        /// <returns><see langword="true"/> if the key was found and removed, <see langword="false"/> otherwise.</returns>
        public bool Remove(string key)
        {
            return _data.Remove(key + KEY_SUFFIX);
        }

        internal bool RemoveInternal(string key)
        {
            return _data.Remove(key + KEY_SUFFIX_INTERNAL);
        }

        internal void SaveToStrings(List<string> strings)
        {
            foreach(var pair in _data)
            {
                try
                {
                    SavePairToStrings(strings, pair.Key, JsonConvert.SerializeObject(pair.Value));
                }
                catch(Exception e)
                {
                    SlugBasePlugin.Logger.LogError($"Failed to serialize key \"{pair.Key}\" of type {pair.Value?.GetType().Name ?? "null"}!");
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        internal bool LoadStringFromUnrecognizedStrings(string key, out string value)
        {
            for(int i = 0; i < _unrecognizedSaveStrings.Count; i++)
            {
                var s = _unrecognizedSaveStrings[i];
                if (s.StartsWith(key))
                {
                    try
                    {
                        value = Encoding.UTF8.GetString(Convert.FromBase64String(s.Substring(key.Length)));
                    }
                    catch(Exception e)
                    {
                        SlugBasePlugin.Logger.LogError($"Badly formatted save data for key \"{key}\": \"{s.Substring(key.Length)}\"");
                        UnityEngine.Debug.LogException(e);
                        _unrecognizedSaveStrings.RemoveAt(i);
                        break;
                    }
                    _unrecognizedSaveStrings.RemoveAt(i);
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static void SavePairToStrings(List<string> strings, string key, string value)
        {
            var dataToStore = key + Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

            for (var i = 0; i < strings.Count; i++)
            {
                if (strings[i].StartsWith(key))
                {
                    strings[i] = dataToStore;
                    return;
                }
            }

            strings.Add(dataToStore);
        }
    }
}