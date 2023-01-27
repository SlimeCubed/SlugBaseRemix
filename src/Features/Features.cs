using System.Collections.Generic;

namespace SlugBase.Features
{
    /// <summary>
    /// Built-in <see cref="Feature"/>s describing the player.
    /// </summary>
    public static class PlayerFeatures
    {
        /// <summary>"multi_jumps": Allows a character to jump multiple times.</summary>
        public static readonly PlayerFeature<int> MultiJumps = new("multi_jumps", JsonAny.AsInt);

        /// <summary>Data used by <see cref="MultiJumps"/>.</summary>
        public static readonly PlayerData<JumpData> MultiJumpData = new(MultiJumps);
        
        /// <summary>
        /// Data type of <see cref="MultiJumpData"/>.
        /// </summary>
        public class JumpData
        {
            /// <summary>
            /// The number of midair jumps remaining.
            /// </summary>
            public int JumpsLeft;
        }
    }

    /// <summary>
    /// Built-in <see cref="Feature"/>s describing the game.
    /// </summary>
    public static class GameFeatures
    {
        /// <summary>"karma": Sets the initial karma.</summary>
        public static readonly GameFeature<int> Karma = new("karma", JsonAny.AsInt);

        /// <summary>"karma": Sets the initial karma cap.</summary>
        public static readonly GameFeature<int> KarmaCap = new("karma_cap", JsonAny.AsInt);
    }
}
