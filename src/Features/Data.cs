using SlugBase.Characters;
using System;
using System.Runtime.CompilerServices;

namespace SlugBase.Features
{
    public class PlayerData<TValue> : Data<PlayerState, TValue>
        where TValue : new()
    {
        public PlayerData(Feature requiredFeature) : base(requiredFeature) {}

        private SlugBaseCharacter GetChara(PlayerState state) => CharacterManager.Get((state.creature.realizedObject as Player)?.SlugCatClass);

        public Box<TValue> Get(PlayerState state) => Get(GetChara(state), state);
        public Box<TValue> Get(Player player) => Get(player.playerState);

        public bool TryGet(PlayerState state, out TValue value) => Box<TValue>.TryUnbox(Get(state), out value);
        public bool TryGet(Player player, out TValue value) => Box<TValue>.TryUnbox(Get(player), out value);
    }

    public class GameData<TValue> : Data<RainWorldGame, TValue>
        where TValue : new()
    {
        public GameData(Feature requiredFeature) : base(requiredFeature) {}

        public Box<TValue> Get(RainWorldGame game) => Get(CharacterManager.Get(game), game);

        public bool TryGet(RainWorldGame game, out TValue value) => Box<TValue>.TryUnbox(Get(game), out value);
    }

    public class Data<THolder, TValue> : Data
        where THolder : class
        where TValue : new()
    {
        private readonly ConditionalWeakTable<THolder, Box<TValue>> _values = new();

        public Data(Feature requiredFeature) : base(requiredFeature) {}

        public Box<TValue> Get(SlugBaseCharacter character, THolder key)
        {
            if (RequiredFeature != null && !character.Features.Contains(RequiredFeature))
                return null;

            if (!_values.TryGetValue(key, out var box))
                _values.Add(key, box = new(new TValue()));

            return box;
        }
    }

    public abstract class Data
    {
        public Feature RequiredFeature { get; }

        public Data(Feature requiredFeature)
        {
            RequiredFeature = requiredFeature;
        }

        public class Box<TValue>
        {
            public TValue Value;

            internal Box(TValue value)
            {
                Value = value;
            }

            public static bool TryUnbox(Box<TValue> box, out TValue value)
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
}
