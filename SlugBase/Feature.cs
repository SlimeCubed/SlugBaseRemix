using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugBase
{
    /// <summary>
    /// Represents a ability, property, setting, or some other feature of a <typeparamref name="TOwner"/> instance added by SlugBase.
    /// </summary>
    /// <typeparam name="TOwner"></typeparam>
    public abstract class Feature<TOwner>
    {
        /// <summary>
        /// The <typeparamref name="TOwner"/> that this feature is associated with.
        /// </summary>
        public TOwner Owner { get; }

        /// <summary>
        /// Creates a new <see cref="Feature{TOwner}"/> with the given owner.
        /// </summary>
        /// <param name="owner">The <typeparamref name="TOwner"/> that this feature is associated with.</param>
        /// <param name="settings">Settings for this feature.</param>
        public Feature(TOwner owner, FeatureSettings settings)
        {
            Owner = owner;
        }
    }
}
