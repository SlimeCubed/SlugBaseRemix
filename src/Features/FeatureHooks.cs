namespace SlugBase.Features
{
    using static PlayerFeatures;
    using static GameFeatures;

    internal static class FeatureHooks
    {
        public static void Apply()
        {
            On.Player.MovementUpdate += Player_MovementUpdate;
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig(self, eu);

            if(MultiJumps.TryGet(self, out int maxJumps) && MultiJumpData.TryGet(self, out var jumpData))
            {
                if (self.bodyChunks[1].ContactPoint.y == -1)
                {
                    jumpData.JumpsLeft = maxJumps;
                }
                else if (self.input[0].jmp && !self.input[1].jmp && jumpData.JumpsLeft > 0)
                {
                    jumpData.JumpsLeft--;

                    self.bodyChunks[0].vel.y = 4f;
                    self.bodyChunks[1].vel.y = 4f;
                    self.jumpBoost = 7f;

                    self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk, false, 1f, 1f);
                }
            }
        }
    }
}
