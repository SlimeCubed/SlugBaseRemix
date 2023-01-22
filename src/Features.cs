namespace SlugBase
{
    using FeatureInfo;

    /// <summary>
    /// Helpers to get and manipulate <see cref="Feature{TOwner, TData}"/> instances attached to the player or world.
    /// </summary>
    public static class Features
    {
        internal static void Init() { }

        // Registries //

        /// <summary>Features of a SlugBase player instance.</summary>
        public static FeatureRegistry<PlayerState> PlayerFeatures { get; } = new();
        
        /// <summary>Features of a SlugBase character's game.</summary>
        public static FeatureRegistry<RainWorldGame> GameFeatures { get; } = new();

        /// <summary>Features of a SlugBase character's game.</summary>
        public static FeatureRegistry<PlayerProgression> GlobalFeatures { get; } = new();


        // Player features //
        public static readonly Feature<PlayerState, ExplosionInfo> ExplodeOnDeath = new(PlayerFeatures, "explode_on_death", (game, json) => new(json));
        public static readonly Feature<PlayerState, MultiJumpInfo> MultiJump = new(PlayerFeatures, "multi_jump", (game, json) => new(json));

        // Game features //
        public static readonly Feature<RainWorldGame, int> Karma = new(GameFeatures, "karma", (game, json) => (int)(long)json);
        public static readonly Feature<RainWorldGame, int> KarmaCap = new(GameFeatures, "karma_cap", (game, json) => (int)(long)json);

        // Global features //
    }
}
