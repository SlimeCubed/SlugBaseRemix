global using System;
global using System.Collections.Generic;
global using UnityEngine;
using BepInEx;
using System.Runtime.CompilerServices;

namespace SlugBase;

class DoubleJump
{
    public DoubleJump(Dictionary<string, object> json)
    {
        Delay = json.TryGetValue("delay", out object delayO) && delayO is int delay ? delay : 0;
        Max = json.TryGetValue("max", out object maxO) && maxO is int max ? max : 0;
    }

    public readonly int Delay;
    public readonly int Max;
    public int Cooldown;
    public int Remaining;
}

[BepInPlugin("slime-cubed.slugbase", "SlugBase", "2.0.0")]
class SlugBasePlugin : BaseUnityPlugin
{
    public static PlayerIntFeature KarmaMax = new("slugbase/karma_max");
    public static PlayerIntFeature KarmaStart = new("slugbase/karma_start");
    public static PlayerCompositeFeature<DoubleJump> DoubleJumpDelay = new("slugbase/double_jump", (p, json) => new(json));

    public void OnEnable()
    {
        On.Player.Update += Player_Update;
    }

    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (DoubleJumpDelay.TryGet(self, out var doubleJump)) {
            TryDoubleJump(self, doubleJump);
        }
    }

    private static void TryDoubleJump(Player self, DoubleJump doubleJump)
    {
        if (self.bodyChunks[1].contactPoint.y == 0) {
            doubleJump.Remaining = doubleJump.Max;
        }

        doubleJump.Cooldown -= 1;

        bool jump = doubleJump.Remaining > 0 && self.input[0].jmp && !self.input[1].jmp;
        if (jump && doubleJump.Cooldown < 0) {
            doubleJump.Remaining -= 1;
            doubleJump.Cooldown = doubleJump.Delay;
            self.bodyChunks[0].vel.y += 10;
            self.bodyChunks[1].vel.y += 10;
            self.animation = Player.AnimationIndex.Flip;
        }
    }
}

class PlayerCharacter
{
    public static readonly PlayerCharacter Vanilla = new();

    public PlayerCharacter()
    {
        // TODO: Create a constructor that loads from json
        // TODO: Store a list of player characters somewhere, and when game loads, use PlayerCharacterCache.For(player).Character = characters[TheCharacterTheyChose];
    }

    public readonly Dictionary<string, int> Ints = new();
    public readonly Dictionary<string, Dictionary<string, object>> Objects = new();
}


class PlayerCharacterCache
{
    private static readonly ConditionalWeakTable<PlayerState, PlayerCharacterCache> features = new();
    public static PlayerCharacterCache For(Player player) => features.GetValue(player.playerState, p => new(p));

    PlayerCharacterCache(PlayerState _) { }

    public PlayerCharacter Character = PlayerCharacter.Vanilla;
}
