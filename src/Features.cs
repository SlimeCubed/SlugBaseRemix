using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SlugBase;

public sealed class PlayerIntFeature
{
    private readonly string id;

    public PlayerIntFeature(string id)
    {
        this.id = id;
    }

    public bool TryGet(Player player, out int data) => PlayerCharacterCache.For(player).Character.Ints.TryGetValue(id, out data);
}

public sealed class PlayerCompositeFeature
{
    private readonly string id;

    public PlayerCompositeFeature(string id)
    {
        this.id = id;
    }

    public bool TryGet(Player player, out object data) => PlayerCharacterCache.For(player).Character.Objects.TryGetValue(id, out data);
}

public sealed class PlayerIntFeature<TData> where TData : class
{
    private readonly string id;
    private readonly Func<Player, int, TData> factory;

    private static readonly ConditionalWeakTable<PlayerState, TData> customField = new();

    public PlayerIntFeature(string id, Func<Player, int, TData> factory)
    {
        this.id = id;
        this.factory = factory;
    }

    public bool TryGet(Player player, [MaybeNullWhen(false)] out TData data)
    {
        if (PlayerCharacterCache.For(player).Character.Ints.TryGetValue(id, out var d)) {
            data = customField.GetValue(player.playerState, _ => factory(player, d));
            return true;
        }
        data = default;
        return false;
    }
}

public sealed class PlayerCompositeFeature<TData> where TData : class
{
    private readonly string id;
    private readonly Func<Player, Dictionary<string, object>, TData> factory;

    private static readonly ConditionalWeakTable<PlayerState, TData> customField = new();

    public PlayerCompositeFeature(string id, Func<Player, Dictionary<string, object>, TData> factory)
    {
        this.id = id;
        this.factory = factory;
    }

    public bool TryGet(Player player, [MaybeNullWhen(false)] out TData data)
    {
        if (PlayerCharacterCache.For(player).Character.Objects.TryGetValue(id, out var d)) {
            data = customField.GetValue(player.playerState, _ => factory(player, d));
            return true;
        }
        data = default;
        return false;
    }
}

// TODO: PlayerBoolFeature, PlayerFloatFeature, PlayerStringFeature, PlayerListFeature, PlayerCompositeFeature
