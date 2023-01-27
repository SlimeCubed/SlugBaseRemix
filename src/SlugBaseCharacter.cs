using System.Collections.Generic;
using System;
using SlugBase.Features;
using System.IO;

namespace SlugBase
{
    /// <summary>
    /// A character added by SlugBase.
    /// </summary>
    public class SlugBaseCharacter
    {
        private static readonly Dictionary<SlugcatStats.Name, SlugBaseCharacter> _characters = new();

        /// <summary>
        /// This character's unique name.
        /// </summary>
        public SlugcatStats.Name Name { get; }

        /// <summary>
        /// The displayed name of this character, such as "The Survivor", "The Monk", or "The Hunter".
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// A description of this character that appears on the select menu.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The path to this slugcat's JSON file, or <c>null</c> if it was not loaded via JSON.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Settings, abilities, or other <see cref="Feature"/>s of this character.
        /// </summary>
        public FeatureList Features { get; } = new();

        internal SlugBaseCharacter(SlugcatStats.Name name)
        {
            Name = name;
        }

        /// <summary>
        /// Create and register a new <see cref="SlugBaseCharacter"/> with the given name.
        /// </summary>
        /// <param name="name">The character's ID to be registered as its <see cref="SlugcatStats.Name"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is not unique.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        public static SlugBaseCharacter Register(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (SlugcatStats.Name.values.entries.Contains(name))
                throw new ArgumentException($"The slugcat ID \"{name}\" is already taken!");

            var id = new SlugcatStats.Name(name, true);
            try
            {
                var chara = new SlugBaseCharacter(id);
                Register(chara);

                return chara;
            }
            catch
            {
                id.Unregister();
                throw;
            }
        }

        /// <summary>
        /// Register a previously unregistered <see cref="SlugBaseCharacter"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="Name"/> is also unregistered.
        /// </remarks>
        /// <seealso cref="Unregister(SlugBaseCharacter)"/>
        /// <param name="character">The <see cref="SlugBaseCharacter"/> to register.</param>
        /// <exception cref="ArgumentException"><paramref name="character"/> is already registered.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="character"/> is <c>null</c>.</exception>
        public static void Register(SlugBaseCharacter character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            if (_characters.ContainsKey(character.Name))
                throw new ArgumentException($"The SlugBase character \"{character.Name}\" is already registered!");

            _characters.Add(character.Name, character);
        }

        /// <summary>
        /// Unregister a previously unregistered <see cref="SlugBaseCharacter"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="Name"/> is also registered.
        /// </remarks>
        /// <param name="character">The <see cref="SlugBaseCharacter"/> to unregister.</param>
        /// <exception cref="ArgumentException"><paramref name="character"/> is not registered.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="character"/> is <c>null</c>.</exception>
        public static void Unregister(SlugBaseCharacter character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            if (!_characters.Remove(character.Name))
                throw new ArgumentException($"The SlugBase character \"{character.Name}\" was not registered!");
        }

        /// <summary>
        /// Loads and registers a <see cref="SlugBaseCharacter"/> from a JSON file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A registered <see cref="SlugBaseCharacter"/> with <see cref="Path"/> set to <paramref name="path"/>.</returns>
        public static SlugBaseCharacter RegisterFromFile(string path)
        {
            JsonObject json = JsonAny.Parse(File.ReadAllText(path)).AsObject();

            string id = json.GetString("id");

            SlugBaseCharacter chara = Register(id);
            chara.LoadFrom(json);
            chara.Path = path;
            return chara;
        }

        /// <summary>
        /// Gets all registered <see cref="SlugBaseCharacter"/>s.
        /// </summary>
        public static IEnumerable<SlugBaseCharacter> Characters => _characters.Values;

        /// <summary>
        /// Gets a <see cref="SlugBaseCharacter"/> by <see cref="Name"/>.
        /// </summary>
        /// <param name="name">The <see cref="SlugcatStats.Name"/> to search for.</param>
        /// <returns>The <see cref="SlugBaseCharacter"/> with the given <paramref name="name"/>, or <c>null</c> if it was not found.</returns>
        public static SlugBaseCharacter Get(SlugcatStats.Name name)
        {
            return name != null && _characters.TryGetValue(name, out var chara) ? chara : null;
        }

        /// <summary>
        /// Gets a <see cref="SlugBaseCharacter"/> that matches <see cref="Player.SlugCatClass"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> to check the class of.</param>
        /// <returns>The <see cref="SlugBaseCharacter"/> of the same class as <paramref name="player"/>, or <c>null</c> if it was not found.</returns>
        public static SlugBaseCharacter Get(Player player) => Get(player.SlugCatClass);

        /// <summary>
        /// Gets a <see cref="SlugBaseCharacter"/> that matches <see cref="StoryGameSession.saveStateNumber"/>.
        /// </summary>
        /// <param name="game">The <see cref="RainWorldGame"/> to check the save state number of.</param>
        /// <returns>The <see cref="SlugBaseCharacter"/> that owns the <paramref name="game"/>'s save state, or <c>null</c> if it was not found.</returns>
        public static SlugBaseCharacter Get(RainWorldGame game) => Get(game.StoryCharacter);

        /// <summary>
        /// Gets a <see cref="SlugBaseCharacter"/> by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The <see cref="SlugcatStats.Name"/> to search for.</param>
        /// <param name="character">The <see cref="SlugBaseCharacter"/> with the given <paramref name="name"/>, or <c>null</c> if it was not found.</param>
        /// <returns><c>true</c> if the <see cref="SlugBaseCharacter"/> was found, <c>false</c> otherwise.</returns>
        public static bool TryGet(SlugcatStats.Name name, out SlugBaseCharacter character)
        {
            character = Get(name);
            return character != null;
        }

        /// <summary>
        /// Replace this slugcat's information and <see cref="Features"/> with those loaded from <paramref name="json"/>.
        /// </summary>
        /// <param name="json">The JSON element to parse.</param>
        public void LoadFrom(JsonObject json)
        {
            DisplayName = json.GetString("name");
            Description = json.GetString("description");

            Features.Clear();
            if (json.TryGet("features")?.AsObject() is JsonObject obj)
                Features.AddMany(obj);
        }

        /// <summary>
        /// Stores the <see cref="Feature"/>s of a <see cref="SlugBaseCharacter"/>.
        /// </summary>
        public class FeatureList
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
                        throw new JsonException($"Couldn't find feature: {pair.Key}!", json._path);
                }
            }

            internal void Clear()
            {
                _features.Clear();
            }
        }
    }
}
