using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SlugBase
{
    /// <summary>
    /// Stores registered features and attaches features to instances of <typeparamref name="TOwner"/>.
    /// </summary>
    /// <typeparam name="TOwner">The type to attach features to.</typeparam>
    public class FeatureRegistry<TOwner> where TOwner : class
    {
        private readonly Dictionary<string, IFeature<TOwner>> _features = new();

        public void Add(IFeature<TOwner> feature)
        {
            _features.Add(feature.ID, feature);
        }

        public bool TryGet(string id, out IFeature<TOwner> feature)
        {
            return _features.TryGetValue(id, out feature);
        }

        public IFeature<TOwner> this[string id]
        {
            get => _features[id];
        }
    }
}
