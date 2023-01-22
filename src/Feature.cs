using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SlugBase
{
    /// <summary>
    /// Represents an ability, setting, or other feature or an instance of <typeparamref name="TOwner"/>.
    /// </summary>
    /// <typeparam name="TOwner">The type to associate the feature with.</typeparam>
    /// <typeparam name="TData">The type of data to attach to each <typeparamref name="TOwner"/>.</typeparam>
    public class Feature<TOwner, TData> : IFeature<TOwner> where TOwner : class
    {
        private readonly Func<TOwner, object, TData> _factory;
        private readonly ConditionalWeakTable<TOwner, object> _attached = new(); // TValue is object so that value types may be used for TData

        /// <inheritdoc/>
        public string ID { get; }

        /// <summary>
        /// Create, but do not register, a new feature type.
        /// </summary>
        /// <param name="id">This feature's unique ID.</param>
        /// <param name="factory">A factory that creates this feature's data.</param>
        public Feature(string id, Func<TOwner, object, TData> factory)
        {
            ID = id;
            _factory = factory;
        }

        /// <summary>
        /// Create and register a new feature type.
        /// </summary>
        /// <param name="registry">The registry to add this feature to.</param>
        /// <param name="id">This feature's unique ID.</param>
        /// <param name="factory">A factory that creates this feature's data.</param>
        public Feature(FeatureRegistry<TOwner> registry, string id, Func<TOwner, object, TData> factory) : this(id, factory)
        {
            registry.Add(this);
        }

        /// <inheritdoc/>
        public void Attach(TOwner owner, object json)
        {
            var data = _factory(owner, json);
            if(owner != null)
                _attached.Add(owner, data);
        }

        /// <summary>
        /// Gets this feature's data from a <typeparamref name="TOwner"/> instance, if it is attached.
        /// </summary>
        /// <param name="owner">The feature's owner.</param>
        /// <param name="data">The data attached to <paramref name="owner"/>.</param>
        /// <returns><c>true</c> if the owner had any data attached, <c>false</c> otherwise.</returns>
        public bool TryGet(TOwner owner, out TData data)
        {
            if(_attached.TryGetValue(owner, out object dataObj))
            {
                data = (TData)dataObj;
                return true;
            }
            else
            {
                data = default;
                return false;
            }
        }
    }
}
