using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SlugBase.Features
{
    /// <summary>
    /// Stores per-player variables.
    /// </summary>
    /// <typeparam name="TValue">The type of data stored.</typeparam>
    public class PlayerData<TValue> : Data<PlayerState, TValue>
        where TValue : new()
    {
        /// <summary>
        /// Creates a new per-player variable that depends on <paramref name="requiredFeature"/>.
        /// </summary>
        /// <param name="requiredFeature">The required <see cref="Feature"/>, or <c>null</c> if data access should not be locked behind a feature.</param>
        public PlayerData(Feature requiredFeature) : base(requiredFeature) {}

        /// <summary>
        /// Gets the <typeparamref name="TValue"/> instance assocated with <paramref name="state"/>, constructing it if it does not exist.
        /// <para>If the game's <see cref="SlugBaseCharacter"/> does not have <see cref="Data.RequiredFeature"/>, then <c>null</c> is returned.</para>
        /// </summary>
        /// <param name="state">The player state the variable is associated with.</param>
        public StrongBox<TValue> Get(PlayerState state) => Get(state.creature.realizedObject as Player);

        /// <summary>
        /// Gets the <typeparamref name="TValue"/> instance assocated with <paramref name="player"/>, constructing it if it does not exist.
        /// <para>If the game's <see cref="SlugBaseCharacter"/> does not have <see cref="Data.RequiredFeature"/>, then <c>null</c> is returned.</para>
        /// </summary>
        /// <param name="player">The player the variable is associated with.</param>
        public StrongBox<TValue> Get(Player player) => Get(SlugBaseCharacter.Get(player.SlugCatClass), player.playerState);

        /// <summary>
        /// Gets the <typeparamref name="TValue"/> instance assocated with <paramref name="state"/>, constructing it if it does not exist.
        /// </summary>
        /// <param name="state">The player state the variable is associated with.</param>
        /// <param name="value">The stored value, or <typeparamref name="TValue"/>'s default value if the required feature wasn't found.</param>
        /// <returns><c>true</c> if the player's <see cref="SlugBaseCharacter"/> had <see cref="Data.RequiredFeature"/>, <c>false</c> otherwise.</returns>
        public bool TryGet(PlayerState state, out TValue value) => TryUnbox(Get(state), out value);

        /// <summary>
        /// Gets the <typeparamref name="TValue"/> instance assocated with <paramref name="player"/>, constructing it if it does not exist.
        /// </summary>
        /// <param name="player">The player the variable is associated with.</param>
        /// <param name="value">The stored value, or <typeparamref name="TValue"/>'s default value if the required feature wasn't found.</param>
        /// <returns><c>true</c> if the player's <see cref="SlugBaseCharacter"/> had <see cref="Data.RequiredFeature"/>, <c>false</c> otherwise.</returns>
        public bool TryGet(Player player, out TValue value) => TryUnbox(Get(player), out value);
    }

    /// <summary>
    /// Stores per-game variables.
    /// </summary>
    /// <typeparam name="TValue">The type of data stored.</typeparam>
    public class GameData<TValue> : Data<RainWorldGame, TValue>
        where TValue : new()
    {
        /// <summary>
        /// Create a new per-game variable that depends on <paramref name="requiredFeature"/>.
        /// </summary>
        /// <param name="requiredFeature">The required <see cref="Feature"/>, or <c>null</c> if data access should not be locked behind a feature.</param>
        public GameData(Feature requiredFeature) : base(requiredFeature) {}

        /// <summary>
        /// Gets the <typeparamref name="TValue"/> instance assocated with <paramref name="game"/>, constructing it if it does not exist.
        /// <para>If the game's <see cref="SlugBaseCharacter"/> does not have <see cref="Data.RequiredFeature"/>, then <c>null</c> is returned.</para>
        /// </summary>
        /// <param name="game">The current game.</param>
        public StrongBox<TValue> Get(RainWorldGame game) => Get(SlugBaseCharacter.Get(game.StoryCharacter), game);

        /// <summary>
        /// Gets the <typeparamref name="TValue"/> instance assocated with <paramref name="game"/>, constructing it if it does not exist.
        /// </summary>
        /// <param name="game">The current game.</param>
        /// <param name="value">The stored value, or <typeparamref name="TValue"/>'s default value if the required feature wasn't found.</param>
        /// <returns><c>true</c> if <paramref name="game"/> had <see cref="Data.RequiredFeature"/>, <c>false</c> otherwise.</returns>
        public bool TryGet(RainWorldGame game, out TValue value) => TryUnbox(Get(game), out value);
    }

    /// <summary>
    /// Stores <typeparamref name="TValue"/>s associated with <typeparamref name="THolder"/>s.
    /// </summary>
    /// <remarks>
    /// <see cref="PlayerData{TValue}"/> and <see cref="GameData{TValue}"/> should be used when possible.
    /// Otherwise, consider making a child class with <c>Get</c> and <c>TryGet</c> methods that find the most
    /// appropriate <see cref="SlugBaseCharacter"/> to pass to <see cref="Get(SlugBaseCharacter, THolder)"/>.
    /// </remarks>
    /// <typeparam name="THolder">The key type that values are associated with.</typeparam>
    /// <typeparam name="TValue">The type of data stored.</typeparam>
    public class Data<THolder, TValue> : Data
        where THolder : class
        where TValue : new()
    {
        private readonly ConditionalWeakTable<THolder, StrongBox<TValue>> _values = new();

        /// <summary>
        /// Creates a new instance of <see cref="Data{THolder, TValue}"/> that depends on <paramref name="requiredFeature"/>.
        /// </summary>
        /// <param name="requiredFeature">The required <see cref="Feature"/>, or <c>null</c> if data access should not be locked behind a feature.</param>
        public Data(Feature requiredFeature) : base(requiredFeature) {}

        /// <summary>
        /// Gets the <typeparamref name="TValue"/> instance assocated with <paramref name="key"/>, constructing it if it does not exist.
        /// <para>If <paramref name="character"/> does not have <see cref="Data.RequiredFeature"/>, then <c>null</c> is returned.</para>
        /// </summary>
        /// <param name="character">The <see cref="SlugBaseCharacter"/> that may own <see cref="Data.RequiredFeature"/>.</param>
        /// <param name="key">The key the data is attached to.</param>
        public StrongBox<TValue> Get(SlugBaseCharacter character, THolder key)
        {
            if (RequiredFeature != null && (character == null || !character.Features.Contains(RequiredFeature)))
                return null;

            if (!_values.TryGetValue(key, out var box))
                _values.Add(key, box = new(new TValue()));

            return box;
        }
    }

    /// <summary>
    /// Represents variable information of a <see cref="SlugBaseCharacter"/> that depends on a <see cref="Feature"/>.
    /// </summary>
    public abstract class Data
    {
        /// <summary>
        /// The feature this data depends upon.
        /// </summary>
        public Feature RequiredFeature { get; }

        /// <summary>
        /// Create a new <see cref="Data"/> instance that requires a given feature.
        /// </summary>
        /// <param name="requiredFeature">The feature that this requires, or null to not require a feature.</param>
        public Data(Feature requiredFeature)
        {
            RequiredFeature = requiredFeature;
        }

        /// <summary>
        /// Gets the value of a <see cref="StrongBox{T}"/>.
        /// </summary>
        /// <typeparam name="T">The stored value's type.</typeparam>
        /// <param name="box">The <see cref="StrongBox{T}"/> holding the value or <c>null</c>.</param>
        /// <param name="value">The stored value, or <c>default</c> if <paramref name="box"/> is <c>null</c>.</param>
        /// <returns><c>false</c> if <paramref name="box"/> was <c>null</c>, <c>true</c> otherwise.</returns>
        public static bool TryUnbox<T>(StrongBox<T> box, out T value)
        {
            if(box != null)
            {
                value = box.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
