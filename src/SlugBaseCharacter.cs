using System.Collections.Generic;
using System;
using SlugBase.Features;
using System.IO;

namespace SlugBase
{
    public class SlugBaseCharacter
    {
        private static readonly Dictionary<SlugcatStats.Name, SlugBaseCharacter> _characters = new();

        public SlugcatStats.Name Name { get; }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }

        public FeatureList Features { get; } = new();

        internal SlugBaseCharacter(SlugcatStats.Name name)
        {
            Name = name;
        }

        public static SlugBaseCharacter Register(string name)
        {
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

        public static void Register(SlugBaseCharacter chara)
        {
            if (_characters.ContainsKey(chara.Name))
                throw new ArgumentException($"The SlugBase character \"{chara.Name}\" is already registered!");

            _characters.Add(chara.Name, chara);
        }

        public static void Unregister(SlugBaseCharacter chara)
        {
            if (!_characters.Remove(chara.Name))
                throw new ArgumentException($"The SlugBase character \"{chara.Name}\" was not registered!");
        }

        public static SlugBaseCharacter RegisterFromFile(string path)
        {
            JsonObject json = JsonAny.Parse(File.ReadAllText(path)).AsObject();

            string id = json.GetString("id");

            SlugBaseCharacter chara = Register(id);
            chara.LoadFrom(json);
            chara.Path = path;
            return chara;
        }

        public static IEnumerable<SlugBaseCharacter> Characters => _characters.Values;

        public static SlugBaseCharacter Get(SlugcatStats.Name name)
        {
            return name != null && _characters.TryGetValue(name, out var chara) ? chara : null;
        }

        public static SlugBaseCharacter Get(Player player) => Get(player.SlugCatClass);
        public static SlugBaseCharacter Get(RainWorldGame game) => Get(game.StoryCharacter);

        public static bool TryGet(SlugcatStats.Name name, out SlugBaseCharacter chara)
        {
            chara = Get(name);
            return chara != null;
        }

        public void LoadFrom(JsonObject json)
        {
            DisplayName = json.GetString("name");
            Description = json.GetString("description");

            if (json.TryGet("features")?.AsObject() is JsonObject obj)
                Features.AddMany(obj);
        }

        public class FeatureList
        {
            private readonly Dictionary<Feature, object> _features = new();

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

            public bool Contains(Feature feature) => _features.ContainsKey(feature);
        }
    }
}
