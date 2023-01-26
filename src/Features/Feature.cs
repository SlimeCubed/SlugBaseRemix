using System;

namespace SlugBase.Features
{
    public class PlayerFeature<T> : Feature<T>
    {
        public PlayerFeature(string id, Func<JsonAny, T> factory) : base(id, factory) {}

        public bool TryGet(Player player, out T value) => TryGet(SlugBaseCharacter.Get(player), out value);
    }

    public class GameFeature<T> : Feature<T>
    {
        public GameFeature(string id, Func<JsonAny, T> factory) : base(id, factory) { }

        public bool TryGet(RainWorldGame game, out T value) => TryGet(SlugBaseCharacter.Get(game), out value);
    }

    public class Feature<T> : Feature
    {
        private readonly Func<JsonAny, T> _factory;

        public Feature(string id, Func<JsonAny, T> factory) : base(id)
        {
            _factory = factory;
        }

        public bool TryGet(SlugBaseCharacter chara, out T value)
        {
            return chara.Features.TryGet(this, out value);
        }

        internal override object Create(JsonAny json) => _factory(json);
    }

    public abstract class Feature
    {
        public string ID { get; }

        public Feature(string id)
        {
            ID = id;

            if (id == null) throw new ArgumentNullException();

            FeatureManager.Register(this);
        }

        internal abstract object Create(JsonAny json);
    }
}
