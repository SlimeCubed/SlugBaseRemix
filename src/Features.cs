namespace SlugBase
{
    /// <summary>
    /// Helpers to get and manipulate <see cref="Feature{TOwner}"/> instances attached to the player or world.
    /// </summary>
    public static class Features
    {
        /// <summary>
        /// Features of a SlugBase character instance.
        /// </summary>
        public static FeatureRegistry<AbstractCreature> PlayerFeatures { get; } = new();

        /// <summary>
        /// Features of a SlugBase character's game.
        /// </summary>
        public static FeatureRegistry<RainWorldGame> GameFeatures { get; } = new();

        /// <summary>
        /// Fetch a feature of the given player.
        /// </summary>
        /// <typeparam name="T">The subclass of <see cref="Feature{TOwner}"/> to fetch. <c>null</c> is returned if the feature is not assignable to <typeparamref name="T"/>.</typeparam>
        /// <param name="player">The player to get the feature of.</param>
        /// <param name="id">The ID of the feature.</param>
        /// <param name="feature">A feature of <paramref name="player"/>, or <c>null</c> if none was found.</param>
        /// <returns><c>true</c> if the feature was present, <c>false</c> otherwise.</returns>
        public static bool TryGetFeature<T>(this Player player, string id, out T feature) where T : Feature<AbstractCreature>
        {
            PlayerFeatures.TryGet(player.abstractCreature, id, out var baseFeature);
            feature = baseFeature as T;
            return feature != null;
        }

        /// <summary>
        /// Fetch a feature of the given game.
        /// </summary>
        /// <typeparam name="T">The subclass of <see cref="Feature{TOwner}"/> to fetch. <c>null</c> is returned if the feature is not assignable to <typeparamref name="T"/>.</typeparam>
        /// <param name="game">The game to get the feature of.</param>
        /// <param name="id">The ID of the feature.</param>
        /// <param name="feature">A feature of <paramref name="game"/>, or <c>null</c> if none was found.</param>
        /// <returns><c>true</c> if the feature was present, <c>false</c> otherwise.</returns>
        public static bool TryGetFeature<T>(this RainWorldGame game, string id, out T feature) where T : Feature<RainWorldGame>
        {
            GameFeatures.TryGet(game, id, out var baseFeature);
            feature = baseFeature as T;
            return feature != null;
        }
    }
}
