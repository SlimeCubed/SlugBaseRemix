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
        private readonly Dictionary<string, Func<TOwner, FeatureSettings, Feature<TOwner>>> _factories = new();
        private readonly ConditionalWeakTable<TOwner, Dictionary<string, Feature<TOwner>>> _attachedFeatures = new();

        /// <summary>
        /// Register a new type of feature.
        /// </summary>
        /// <param name="id">The feature's ID. This must be unique for this registry.</param>
        /// <param name="factory">A delegate that constructs a new instance of the feature.</param>
        /// <exception cref="ArgumentNullException"><paramref name="id"/> or <paramref name="factory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="id"/> is empty.</exception>
        public void Register(string id, Func<TOwner, FeatureSettings, Feature<TOwner>> factory)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (id == "") throw new ArgumentException("ID may not be empty!", nameof(id));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _factories.Add(id, factory);
        }

        /// <summary>
        /// Create an instance of a registered feature.
        /// </summary>
        /// <param name="owner">The object the feature is attached to.</param>
        /// <param name="id">The feature's ID.</param>
        /// <param name="settings">Settings passed to the feature upon creation.</param>
        /// <returns>The newly-created feature.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="owner"/>, <paramref name="id"/>, or <paramref name="settings"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">No features matching <paramref name="id"/> could be found.</exception>
        /// <exception cref="ArgumentException"><paramref name="owner"/> already has a feature with the same ID as <paramref name="id"/>.</exception>
        public Feature<TOwner> Attach(TOwner owner, string id, FeatureSettings settings)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if(!_attachedFeatures.TryGetValue(owner, out var features))
            {
                features = new Dictionary<string, Feature<TOwner>>();
                _attachedFeatures.Add(owner, features);
            }

            if (!_factories.TryGetValue(id, out var factory))
                throw new KeyNotFoundException($"Feature \"{id}\" not found for {typeof(TOwner).Name}!");

            Feature<TOwner> newFeature = factory(owner, settings);
            features.Add(id, newFeature);
            return newFeature;
        }

        /// <summary>
        /// Fetch a feature with the given id.
        /// </summary>
        /// <param name="owner">The object the feature is associated with.</param>
        /// <param name="id">The ID of the feature.</param>
        /// <param name="feature">A feature of <paramref name="owner"/>, or <c>null</c> if none was found.</param>
        /// <returns><c>true</c> if the feature was present, <c>false</c> otherwise.</returns>
        public bool TryGet(TOwner owner, string id, out Feature<TOwner> feature)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            if (_attachedFeatures.TryGetValue(owner, out var features) && features.TryGetValue(id, out feature))
            {
                return true;
            }
            else
            {
                feature = default;
                return false;
            }
        }

        public IEnumerable<T> GetAll<T>(TOwner owner)
        {
            if(_attachedFeatures.TryGetValue(owner, out var values))
            {
                foreach(var pair in values)
                {
                    if (pair.Value is T valueT)
                        yield return valueT;
                }
            }
        }

        public void ForEach<T>(TOwner owner, Action<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (T t in GetAll<T>(owner))
                action(t);
        }
    }
}
