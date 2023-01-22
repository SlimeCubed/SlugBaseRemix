using System.Collections.Generic;
using System;

namespace SlugBase.Characters
{
    public class SlugBaseCharacter
    {
        public SlugcatStats.Name Name { get; }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }

        public string Error { get; set; }

        public FeatureList<PlayerState> PlayerFeatures { get; } = new();
        public FeatureList<RainWorldGame> GameFeatures { get; } = new();
        public FeatureList<PlayerProgression> GlobalFeatures { get; } = new();

        public SlugBaseCharacter(SlugcatStats.Name name, Dictionary<string, object> json) : this(name)
        {
            DisplayName = Get<object>(json, "name", "The Nameless").ToString();
            Description = Get<object>(json, "description", "A being with no description.").ToString();

            var empty = new Dictionary<string, object>();
            foreach(var pair in Get(json, "player", empty))
            {
                PlayerFeatures.Add(Features.PlayerFeatures[pair.Key], pair.Value);
            }

            foreach(var pair in Get(json, "game", empty))
            {
                GameFeatures.Add(Features.GameFeatures[pair.Key], pair.Value);
            }

            foreach(var pair in Get(json, "global", empty))
            {
                GlobalFeatures.Add(Features.GlobalFeatures[pair.Key], pair.Value);
            }
        }

        public SlugBaseCharacter(SlugcatStats.Name name)
        {
            Name = name;
        }

        public void Validate()
        {
            PlayerFeatures.Validate();
            GameFeatures.Validate();
            GlobalFeatures.Validate();
        }

        private T Get<T>(Dictionary<string, object> json, string key, T defaultValue)
        {
            if (json.TryGetValue(key, out object objValue) && objValue is T tValue)
                return tValue;
            else
                return defaultValue;
        }

        public class FeatureList<TOwner> where TOwner : class
        {
            private readonly List<KeyValuePair<IFeature<TOwner>, object>> _features = new();

            public void Add(IFeature<TOwner> feature, object json)
            {
                _features.Add(new(feature, json));
            }

            public void Attach(TOwner owner)
            {
                foreach (var pair in _features)
                    pair.Key.Attach(owner, pair.Value);
            }

            public void Validate()
            {
                foreach (var pair in _features)
                {
                    try
                    {
                        pair.Key.Attach(null, pair.Value);
                    }
                    catch(Exception e)
                    {
                        throw new FormatException($"Failed to validate {typeof(TOwner).Name} feature: {pair.Key}", e);
                    }
                }
            }
        }
    }
}
