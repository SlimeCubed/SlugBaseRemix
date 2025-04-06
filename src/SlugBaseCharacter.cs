﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SlugBase.Features;

namespace SlugBase
{
    /// <summary>
    /// A character added by SlugBase.
    /// </summary>
    public class SlugBaseCharacter
    {
        /// <summary>
        /// Stores all registered <see cref="SlugBaseCharacter"/>s.
        /// </summary>
        public static JsonRegistry<SlugcatStats.Name, SlugBaseCharacter> Registry { get; } = new((key, json) => new(key, json));

        /// <summary>
        /// Occurs when any <see cref="SlugBaseCharacter"/>'s JSON file is modified, after all features have been loaded.
        /// </summary>
        /// <remarks>
        /// This event is only raised when in-game.
        /// </remarks>
        public static event EventHandler<RefreshEventArgs> Refreshed;

        /// <summary>
        /// Gets a <see cref="SlugBaseCharacter"/> by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The <see cref="SlugcatStats.Name"/> to search for.</param>
        /// <param name="character">The <see cref="SlugBaseCharacter"/> with the given <paramref name="name"/>, or <c>null</c> if it was not found.</param>
        /// <returns><c>true</c> if the <see cref="SlugBaseCharacter"/> was found, <c>false</c> otherwise.</returns>
        public static bool TryGet(SlugcatStats.Name name, out SlugBaseCharacter character) => Registry.TryGet(name, out character);

        /// <summary>
        /// Gets a <see cref="SlugBaseCharacter"/> by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The <see cref="SlugcatStats.Name"/> to search for.</param>
        /// <returns>The <see cref="SlugBaseCharacter"/>, or <c>null</c> if it was not found.</returns>
        public static SlugBaseCharacter Get(SlugcatStats.Name name) => Registry.GetOrDefault(name);

        /// <summary>
        /// Creates a new, blank <see cref="SlugBaseCharacter"/>.
        /// </summary>
        /// <remarks>
        /// Use <see cref="DisplayName"/>, <see cref="Description"/>, and <see cref="Features"/> to customize this character.
        /// </remarks>
        /// <param name="id">The new character's unique ID.</param>
        /// <returns>A new <see cref="SlugBaseCharacter"/> with a default name, default description, and no features.</returns>
        public static SlugBaseCharacter Create(string id)
        {
            var data = new Dictionary<string, object>()
            {
                { "id", id },
                { "name", "No Name" },
                { "description", "No description." },
                { "background", -1 }
            };

            return Registry.Add(JsonConverter.ToJson(data)).Value;
        }

        static SlugBaseCharacter()
        {
            Registry.EntryReloaded += (_, args) =>
            {
                if(UnityEngine.Object.FindObjectOfType<RainWorld>()?.processManager.currentMainLoop is RainWorldGame game)
                {
                    Refreshed?.Invoke(_, new RefreshEventArgs(game, args.Key, args.Value));
                }
                args.Value.UpdateLegacyTimeline();
            };
        }

        /// <summary>
        /// This character's unique name.
        /// </summary>
        public SlugcatStats.Name Name { get; }

        /// <summary>
        /// The displayed name of this character, such as "The Survivor", "The Monk", or "The Hunter".
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The ID of this character's background in the Expedition select screen. Is an integer between -1 and 29, where -1 chooses a random one.
        /// </summary>
        public int Background { get; set; }

        /// <summary>
        /// A description of this character that appears on the select menu.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Settings, abilities, or other <see cref="Feature"/>s of this character.
        /// </summary>
        public FeatureList Features { get; } = new();

        private SlugBaseCharacter(SlugcatStats.Name name, JsonObject json)
        {
            Name = name;

            DisplayName = json.GetString("name");
            Description = json.GetString("description");
            if (json.TryGet("background")) {
                Background = json.Get("background");
            } else {
                Background = -1;
            }

            Features.Clear();
            if (json.TryGet("features")?.AsObject() is JsonObject obj)
                Features.AddMany(obj);
        }

        /// <summary>
        /// If necessary, registers or updates a character-specific timeline that uses the pre-Watcher world state system.
        /// </summary>
        internal void UpdateLegacyTimeline()
        {
            // Create a new timeline if the first entry matches the character's name and isn't registered
            // Update only if it's a SlugBase timeline not added from a file
            if (GameFeatures.WorldState.TryGet(this, out var worldStates)
                && worldStates.Length > 0
                && worldStates[0].value == Name.value
                && (worldStates[0].Index == -1
                    || CustomTimeline.Registry.TryGet(worldStates[0], out _) && !CustomTimeline.Registry.TryGetPath(worldStates[0], out _)))
            {
                try
                {
                    var id = worldStates[0];
                    var before = GameFeatures.TimelineBefore.TryGet(this, out var beforeEnums)
                        ? new List<object>(beforeEnums.Select(x => x.value))
                        : new List<object>();

                    var after = GameFeatures.TimelineBefore.TryGet(this, out var afterEnums)
                        ? new List<object>(beforeEnums.Select(x => x.value))
                        : new List<object>();

                    var dict = new Dictionary<string, object>()
                    {
                        { "id", id.value },
                        { "base", new List<object>(worldStates.Skip(1).Select(x => x.value)) }
                    };
                    if (before.Count > 0)
                        dict.Add("before", before);
                    if (after.Count > 0)
                        dict.Add("after", after);

                    var json = JsonConverter.ToJson(dict);

                    if (CustomTimeline.Registry.TryGet(id, out _))
                        CustomTimeline.Registry.Remove(id);

                    CustomTimeline.Registry.Add(json);
                }
                catch (Exception e)
                {
                    Registry.TryGetPath(Name, out string path);
                    Interface.ErrorList.Instance.AddError(Interface.ErrorList.ErrorIcon.Timeline, "Failed to create legacy timeline: " + e.Message, path, null);
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Stores the <see cref="Feature"/>s of a <see cref="SlugBaseCharacter"/>.
        /// </summary>
        public class FeatureList : IEnumerable<Feature>
        {
            private readonly Dictionary<Feature, object> _features = new();

            /// <summary>
            /// Get the value of a <see cref="Feature{T}"/>.
            /// </summary>
            /// <typeparam name="T">The <see cref="Feature{T}"/>'s data type.</typeparam>
            /// <param name="feature">The feature to get data from.</param>
            /// <param name="value">The feature's data, or <typeparamref name="T"/>'s default value if it was not found.</param>
            /// <returns><c>true</c> if the feature was found, <c>false</c> otherwise.</returns>
            public bool TryGet<T>(Feature<T> feature, out T value)
            {
                if (_features.TryGetValue(feature, out object outObj))
                {
                    value = (T)outObj;
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            /// <summary>
            /// Add a <see cref="Feature"/> or replace an existing <see cref="Feature"/>'s value.
            /// </summary>
            /// <remarks>
            /// Some features are only read occasionally, such as when starting a game or entering a room.
            /// Instead of modifying features during gameplay, consider defining a custom <see cref="Feature{T}"/> and corresponding <see cref="Data{THolder, TValue}"/>.
            /// </remarks>
            /// <param name="feature">The feature to add or replace.</param>
            /// <param name="value">The feature's new value.</param>
            /// <exception cref="JsonException"><paramref name="value"/> was not a valid value for <paramref name="feature"/>.</exception>
            public void Set(Feature feature, JsonAny value)
            {
                _features[feature] = feature.Create(value);
            }

            /// <summary>
            /// Remove a <see cref="Feature"/>.
            /// </summary>
            /// <remarks>
            /// Some features are only read occasionally, such as when starting a game or entering a room.
            /// Instead of modifying features during gameplay, consider defining a custom <see cref="Feature{T}"/> and corresponding <see cref="Data{THolder, TValue}"/>.
            /// </remarks>
            /// <param name="feature">The feature to remove.</param>
            /// <returns><c>true</c> if the feature was present, <c>false</c> otherwise.</returns>
            public bool Remove(Feature feature)
            {
                return _features.Remove(feature);
            }

            /// <summary>
            /// Check this list for a <see cref="Feature"/>.
            /// </summary>
            /// <param name="feature">The <see cref="Feature"/> to check for.</param>
            /// <returns><c>true</c> if <paramref name="feature"/> was found, <c>false</c> otherwise.</returns>
            public bool Contains(Feature feature) => _features.ContainsKey(feature);

            internal void AddMany(JsonObject json)
            {
                foreach (var pair in json)
                {
                    if (FeatureManager.TryGetFeature(pair.Key, out Feature feature))
                        _features.Add(feature, feature.Create(pair.Value));
                    else
                        throw new JsonException($"Couldn't find feature: {pair.Key}!", json);
                }
            }

            internal void Clear()
            {
                _features.Clear();
            }

            /// <summary>
            /// Returns an enumerator that iterates through all features in this collection.
            /// </summary>
            public IEnumerator<Feature> GetEnumerator() => _features.Keys.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }


        /// <summary>
        /// Provides data for the <see cref="Refreshed"/> event.
        /// </summary>
        public class RefreshEventArgs : EventArgs
        {
            /// <summary>
            /// The current <see cref="RainWorldGame"/>.
            /// </summary>
            public RainWorldGame Game { get; }

            /// <summary>
            /// The ID of the reloaded <see cref="SlugBaseCharacter"/>.
            /// </summary>
            public SlugcatStats.Name ID { get; }

            /// <summary>
            /// The reloaded <see cref="SlugBaseCharacter"/>.
            /// </summary>
            public SlugBaseCharacter Character { get; }

            internal RefreshEventArgs(RainWorldGame game, SlugcatStats.Name id, SlugBaseCharacter character)
            {
                Game = game;
                ID = id;
                Character = character;
            }
        }
    }
}
