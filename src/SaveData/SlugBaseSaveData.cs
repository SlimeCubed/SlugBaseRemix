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
    public readonly struct SlugBaseSaveData
    {
        internal const string SAVE_DATA_PREFIX = "_SlugBaseSaveData_";
        internal static readonly ConditionalWeakTable<object, Dictionary<string, object>> SavedObjects = new();

        private readonly object _owner;

        internal SlugBaseSaveData(object owner)
        {
            _owner = owner;
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
            if (SavedObjects.TryGetValue(_owner, out var objects) && objects.TryGetValue(key, out var obj))
            {
                value = (T)obj;
                return true;
            }

            if (LoadStringFromUnrecognizedStrings(key, out var stringValue))
            {
                value = JsonConvert.DeserializeObject<T>(stringValue);
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
        public void Set(string key, object value)
        {
            if (SavedObjects.TryGetValue(_owner, out var objects))
            {
                objects[key] = value;
            }
        }

        private bool LoadStringFromUnrecognizedStrings(string key, out string value)
        {
            var prefix = key + SAVE_DATA_PREFIX;

            List<string> strings;
            switch (_owner)
            {
                case DeathPersistentSaveData dpData:
                    strings = dpData.unrecognizedSaveStrings;
                    break;
                case MiscWorldSaveData mwData:
                    strings = mwData.unrecognizedSaveStrings;
                    break;
                case PlayerProgression.MiscProgressionData mpData:
                    strings = mpData.unrecognizedSaveStrings;
                    break;
                default:
                    value = default;
                    return false;
            }

            foreach (var s in strings)
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