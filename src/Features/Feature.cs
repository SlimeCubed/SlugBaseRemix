using System;

namespace SlugBase.Features
{
    /// <summary>
    /// A constant setting of a <see cref="SlugBaseCharacter"/>'s player.
    /// </summary>
    /// <typeparam name="T">The type that stores this setting's information</typeparam>
    public class PlayerFeature<T> : Feature<T>
    {
        /// <summary>
        /// Creates a new <see cref="PlayerFeature{T}"/> with the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The JSON key.</param>
        /// <param name="factory">A delegate that parses <see cref="JsonAny"/> into <typeparamref name="T"/>. An exception should be thrown on failure.</param>
        public PlayerFeature(string id, Func<JsonAny, T> factory) : base(id, factory) {}

        /// <summary>
        /// Gets the <typeparamref name="T"/> instance assocated with <paramref name="player"/>.
        /// </summary>
        /// <param name="player">A <see cref="Player"/> instance that may be a <see cref="SlugBaseCharacter"/> with this <see cref="Feature"/>.</param>
        /// <param name="value">The stored setting, or <typeparamref name="T"/>'s default value if the feature wasn't found.</param>
        /// <returns><c>true</c> if the <paramref name="player"/>'s <see cref="SlugBaseCharacter"/> had this feature, <c>false</c> otherwise.</returns>
        public bool TryGet(Player player, out T value) => TryGet(SlugBaseCharacter.Get(player), out value);
    }

    /// <summary>
    /// A constant setting of a <see cref="SlugBaseCharacter"/>'s save slot.
    /// </summary>
    /// <typeparam name="T">The type that stores this setting's information.</typeparam>
    public class GameFeature<T> : Feature<T>
    {
        /// <summary>
        /// Creates a new <see cref="GameFeature{T}"/> with the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The JSON key.</param>
        /// <param name="factory">A delegate that parses <see cref="JsonAny"/> into <typeparamref name="T"/>. An exception should be thrown on failure.</param>
        public GameFeature(string id, Func<JsonAny, T> factory) : base(id, factory) { }

        /// <summary>
        /// Gets the <typeparamref name="T"/> instance assocated with <paramref name="game"/>.
        /// </summary>
        /// <param name="game">A <see cref="RainWorldGame"/> instance that may belong to a <see cref="SlugBaseCharacter"/> with this <see cref="Feature"/>.</param>
        /// <param name="value">The stored setting, or <typeparamref name="T"/>'s default value if the feature wasn't found.</param>
        /// <returns><c>true</c> if the <paramref name="game"/>'s <see cref="SlugBaseCharacter"/> had this feature, <c>false</c> otherwise.</returns>
        public bool TryGet(RainWorldGame game, out T value) => TryGet(SlugBaseCharacter.Get(game), out value);
    }

    /// <summary>
    /// A strongly-typed constant setting of a <see cref="SlugBaseCharacter"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="PlayerFeature{T}"/> and <see cref="GameFeature{T}"/> should be used when possible.
    /// Otherwise, consider making a child class with a <c>TryGet</c> method that finds the most
    /// appropriate <see cref="SlugBaseCharacter"/> to pass to <see cref="TryGet(SlugBaseCharacter, out T)"/>.
    /// </remarks>
    /// <typeparam name="T">The type that stores this setting's information.</typeparam>
    public class Feature<T> : Feature
    {
        private readonly Func<JsonAny, T> _factory;

        /// <summary>
        /// Creates a new <see cref="Feature{T}"/> with the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The JSON key.</param>
        /// <param name="factory">A delegate that parses <see cref="JsonAny"/> into <typeparamref name="T"/>. An exception should be thrown on failure.</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A <see cref="Feature"/> with the given <paramref name="id"/> already exists.</exception>
        public Feature(string id, Func<JsonAny, T> factory) : base(id)
        {
            _factory = factory;
        }

        /// <summary>
        /// Gets the <typeparamref name="T"/> instance assocated with <paramref name="character"/>.
        /// </summary>
        /// <param name="character">The <see cref="SlugBaseCharacter"/> that may have this <see cref="Feature"/>.</param>
        /// <param name="value">The stored setting, or <typeparamref name="T"/>'s default value if the feature wasn't found.</param>
        /// <returns><c>true</c> if <paramref name="character"/> had this feature, <c>false</c> otherwise.</returns>
        public bool TryGet(SlugBaseCharacter character, out T value)
        {
            value = default;
            return character != null && character.Features.TryGet(this, out value);
        }

        internal override object Create(JsonAny json) => _factory(json);
    }

    /// <summary>
    /// Represents a constant setting of a <see cref="SlugBaseCharacter"/>.
    /// </summary>
    public abstract class Feature
    {
        /// <summary>
        /// This <see cref="Feature"/>'s JSON key.
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// Creates a new <see cref="Feature"/> with the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The JSON key.</param>
        /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A <see cref="Feature"/> with the given <paramref name="id"/> already exists.</exception>
        public Feature(string id)
        {
            ID = id;

            if (id == null) throw new ArgumentNullException();

            FeatureManager.Register(this);
        }

        internal abstract object Create(JsonAny json);
    }
}
