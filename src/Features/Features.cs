using System.Collections.Generic;

namespace SlugBase.Features
{
    public static class PlayerFeatures
    {
        public static readonly PlayerFeature<int> MultiJumps = new("multi_jumps", JsonAny.AsInt);
        public static readonly PlayerData<JumpData> MultiJumpData = new(MultiJumps);

        public class JumpData
        {
            public int JumpsLeft;
        }
    }

    public static class GameFeatures
    {
        public static readonly GameFeature<int> Karma = new("karma", JsonAny.AsInt);
        public static readonly GameFeature<int> KarmaCap = new("karma_cap", JsonAny.AsInt);
    }
}
