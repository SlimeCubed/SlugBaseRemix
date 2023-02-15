# Writing Custom Features!
## Basics
Before writing any custom features, you'll need to make a BepInEx plugin that references SlugBase. You can download those binaries from the [Releases](https://github.com/SlimeCubed/SlugBaseRemix/releases/latest) page on GitHub.

At its core, defining a feature just requires you to instantiate [`Feature<T>`](../api/SlugBase.Features.Feature-1.yml). The constructor takes a string ID (the same one that you must specify in your character's JSON) and a delegate that converts a [`JsonAny`](../api/SlugBase.JsonAny.yml) into `T`. Accessing your feature requires a [`SlugBaseCharacter`](../api/SlugBase.SlugBaseCharacter.yml) instance, which you can find via [`SlugBaseCharacter.TryGet`](../api/SlugBase.SlugBaseCharacter.yml#SlugBase_SlugBaseCharacter_TryGet_).

In most cases, neither parsing your own data nor getting the [`SlugBaseCharacter`](../api/SlugBase.SlugBaseCharacter.yml) will be necessary.

## Simple Features
For most features, [`FeatureTypes`](../api/SlugBase.Features.FeatureTypes.yml) will provide a suitable factory method that does the parsing for you.
```cs
using static SlugBase.Features.FeatureTypes;

[BepInPlugin("mycoolplugin", "My Cool Plugin", "1.2.3")]
class MyCoolPlugin : BaseUnityPlugin
{
    static readonly PlayerFeature<float> SuperJump = PlayerFloat("super_jump");
}
```
Then, you implement it with hooks:
```cs
void Awake()
{
    On.Player.Jump += Player_Jump;
}

void Player_Jump(On.Player.orig_Jump orig, Player self)
{
    orig(self);
    if(SuperJump.TryGet(self, out var superJump))
    {
        self.jumpBoost *= 1f + superJump;
    }
}
```
SlugBase provides two types of features: [`PlayerFeature<T>`](../api/SlugBase.Features.PlayerFeature-1.yml) and [`GameFeature<T>`](../api/SlugBase.Features.GameFeature-1.yml). These allow you to get the feature's data by `Player` or `RainWorldGame` instance. If you've hooked a method that doesn't have access to either of those, getting by [`SlugBaseCharacter`](../api/SlugBase.SlugBaseCharacter.yml) still works.

> [!WARNING]
> If you construct your features in a static constructor or static field initializer, you must make sure that it is called during mod initialization! Consider placing feature fields directly in your `BaseUnityPlugin`.

## Advanced Features
If [`FeatureTypes`](../api/SlugBase.Features.FeatureTypes.yml) doesn't have the data type that you need, you'll need to pass in your own factory delegate:
```cs
static readonly GameFeature<Dictionary<CreatureTemplate.Type, float>> CreatureHealth = new("creature_health", json =>
{
    var result = new Dictionary<CreatureTemplate.Type, float>();
    
    foreach (var pair in json.AsObject())
        result.Add(new(pair.Key), pair.Value.AsFloat());

    return result;
});
```
After that it acts the same as any other feature. If the JSON data is invalid, consider throwing a [`JsonException`](../api/SlugBase.JsonException.yml), passing in the offending JSON element. This will give the modder a descriptive error with a path to that element.

## Reloading Features
You can change a feature's value at any time by modifying the character's JSON file. Your features might not work properly with this right off the bat. If a feature you've implemented needs to update the game state upon reload, consider subscribing to [`SlugBaseCharacter.Refreshed`](../api/SlugBase.SlugBaseCharacter.yml#SlugBase_SlugBaseCharacter_Refreshed). This event is only raised while in-game.