using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugBase
{
    /// <summary>
    /// Represents an ability, setting, or other feature of an instance of <typeparamref name="TOwner"/>.
    /// </summary>
    /// <typeparam name="TOwner">The type to associate the feature with.</typeparam>
    public interface IFeature<TOwner> : IFeature where TOwner : class
    {
        /// <summary>
        /// Add an instance of this feature's data to an object.
        /// If <paramref name="owner"/> is null, this has no effect except validating that <paramref name="json"/> is properly formatted.
        /// </summary>
        /// <param name="owner">The object that owns the feature, or <c>null</c> to validate <paramref name="json"/>.</param>
        /// <param name="json">The parsed JSON data to pass to the feature's factory.</param>
        void Attach(TOwner owner, object json);
    }

    /// <summary>
    /// Represents an ability, setting, or other feature of an object.
    /// </summary>
    public interface IFeature
    {
        /// <summary>
        /// The unique ID of this feature.
        /// </summary>
        string ID { get; }
    }
}
