using System;
using System.Collections.Generic;

namespace SlugBase.Features
{
    internal static class FeatureManager
    {
        private static readonly Dictionary<string, Feature> _features = new();

        public static void Register(Feature feature)
        {
            if (feature == null)
                throw new ArgumentNullException(nameof(Feature));

            _features.Add(feature.ID, feature);
        }

        public static bool TryGetFeature(string id, out Feature feature)
        {
            return _features.TryGetValue(id, out feature);
        }
    }
}
