using System.Collections.Generic;
using System;
using SlugBase.Features;
using System.Runtime.CompilerServices;

namespace SlugBase.Characters
{
    public class SlugBaseCharacter
    {
        public SlugcatStats.Name Name { get; }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }

        public FeatureList Features { get; } = new();

        public SlugBaseCharacter(SlugcatStats.Name name, JsonObject json) : this(name)
        {
            DisplayName = json.GetString("name");
            Description = json.GetString("description");

            if (json.TryGet("features")?.AsObject() is JsonObject obj)
                Features.Add(obj);
        }

        public SlugBaseCharacter(SlugcatStats.Name name)
        {
            Name = name;
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

            public bool Contains(Feature feature)
            {
                return _features.ContainsKey(feature);
            }

            internal void Add(JsonObject json)
            {
                foreach(var pair in json)
                {
                    if(FeatureManager.TryGetFeature(pair.Key, out Feature feature))
                        _features.Add(feature, feature.Create(pair.Value));
                    else
                        throw new JsonException($"Couldn't find feature: {pair.Key}!", json._path);
                }
            }
        }
    }
}
