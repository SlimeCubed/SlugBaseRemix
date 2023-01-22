using Noise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SlugBase
{
    using static Features;

    internal static class FeatureHooks
    {
        public static void Apply()
        {
            On.Player.Die += Player_Die;
            On.Player.MovementUpdate += Player_MovementUpdate;
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig(self, eu);

            if(MultiJump.TryGet(self, out var jumpData))
            {
                jumpData.DelayTimer--;

                if (self.bodyChunks[1].ContactPoint.y == -1)
                {
                    jumpData.JumpsLeft = jumpData.MaxJumps;
                    jumpData.DelayTimer = jumpData.Delay;
                }
                else if (self.input[0].jmp && !self.input[1].jmp && jumpData.DelayTimer <= 0 && jumpData.JumpsLeft > 0)
                {
                    jumpData.JumpsLeft--;
                    jumpData.DelayTimer = jumpData.Delay;

                    self.bodyChunks[0].vel.y = 4f;
                    self.bodyChunks[1].vel.y = 4f;
                    self.jumpBoost = 7f;

                    self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk, false, 1f, 1f);
                }
            }
        }

        private static void Player_Die(On.Player.orig_Die orig, Player self)
        {
            bool dead = self.dead;

            orig(self);

            if(!dead && ExplodeOnDeath.TryGet(self, out var explosionData))
            {
                var room = self.room;
                Vector2 pos = self.mainBodyChunk.pos;

                room.PlaySound(SoundID.Bomb_Explode, pos);
                room.InGameNoise(new InGameNoise(pos, 9000f, self, 1f));

                float radius = explosionData.Radius;
                float damage = explosionData.Damage;
                room.AddObject(new SootMark(room, pos, 80f, true));
                room.AddObject(new Explosion(room, self, pos, 7, radius, 6.2f, damage, 280f, 0.25f, self, 0.7f, 160f, 1f));
                room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, new Color(1f, 0.4f, 0.3f)));
                room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, new Color(1f, 0.4f, 0.3f)));
                room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));
            }
        }
    }
}
