# Built-In Features

- `boolean`: `true` or `false`
- `integer`: Number with no fractional component
- `float`: Number with some fractional component
- `color`: List of 3-4 color components, hex color string, or object with `"r"`, `"g"`, `"b"`, and optionally `"a"` properties
- `type[min..max]`: List of between `min` and `max` elements of `type`, or a single element of `type` not in a list

Values specified in angle brackets refer to `ExtEnum`s in the game's code. Replace these with the values from the associated enum. For example, `<SlugcatStats.Name>` may be replaced with `White`. Default values for the enums used below are as follows:

# [SlugcatStats.Name](#tab/slugcatname)

**Vanilla:** `White`, `Yellow`, `Red`

**More Slugcats:** `Rivulet`, `Artificer`, `Saint`, `Spear`, `Gourmand`, `Slugpup`, `Inv`

# [CreatureCommunities.CommunityID](#tab/communityid)

**Vanilla:** `None`, `All`, `Scavengers`, `Lizards`, `Cicadas`, `GarbageWorms`, `Deer`, `Jetfish`

# [CreatureTemplate.Type](#tab/creaturetype)

**Vanilla:** `StandardGroundCreature`, `Slugcat`, `LizardTemplate`, `PinkLizard`, `GreenLizard`, `BlueLizard`, `YellowLizard`, `WhiteLizard`, `RedLizard`, `BlackLizard`, `Salamander`, `CyanLizard`, `Fly`, `Leech`, `SeaLeech`, `Snail`, `Vulture`, `GarbageWorm`, `LanternMouse`, `CicadaA`, `CicadaB`, `Spider`, `JetFish`, `BigEel`, `Deer`, `TubeWorm`, `DaddyLongLegs`, `BrotherLongLegs`, `TentaclePlant`, `PoleMimic`, `MirosBird`, `TempleGuard`, `Centipede`, `RedCentipede`, `Centiwing`, `SmallCentipede`, `Scavenger`, `Overseer`, `VultureGrub`, `EggBug`, `BigSpider`, `SpitterSpider`, `SmallNeedleWorm`, `BigNeedleWorm`, `DropBug`, `KingVulture`, `Hazer`

**More Slugcats:** `MirosVulture`, `SpitLizard`, `EelLizard`, `MotherSpider`, `TerrorLongLegs`, `AquaCenti`, `HunterDaddy`, `FireBug`, `StowawayBug`, `ScavengerElite`, `Inspector`, `Yeek`, `BigJelly`, `SlugNPC`, `JungleLeech`, `ZoopLizard`, `ScavengerKing`, `TrainLizard`

# [AbstractPhysicalObject.Type](#tab/objecttype)

**Vanilla:** `Creature`, `Rock`, `Spear`, `FlareBomb`, `VultureMask`, `PuffBall`, `DangleFruit`, `Oracle`, `PebblesPearl`, `SLOracleSwarmer`, `SSOracleSwarmer`, `DataPearl`, `SeedCob`, `WaterNut`, `JellyFish`, `Lantern`, `KarmaFlower`, `Mushroom`, `VoidSpawn`, `FirecrackerPlant`, `SlimeMold`, `FlyLure`, `ScavengerBomb`, `SporePlant`, `AttachedBee`, `EggBugEgg`, `NeedleEgg`, `DartMaggot`, `BubbleGrass`, `NSHSwarmer`, `OverseerCarcass`, `CollisionField`, `BlinkingFlower`,

**More Slugcats:** `JokeRifle`, `Bullet`, `SingularityBomb`, `Spearmasterpearl`, `FireEgg`, `EnergyCell`, `Germinator`, `Seed`, `GooieDuck`, `LillyPuck`, `GlowWeed`, `MoonCloak`, `HalcyonPearl`, `DandelionPeach`, `HRGuard`

---

Some values will not be present if More Slugcats is not installed. Other mods may add values to these. Features that take enums will often ignore invalid values.

## Player Features
### "color"
`color`\
Ex: `"color": "6B12FF"`

Default color for body and UI elements.

### "auto_grab_batflies"
`boolean`\
Ex: `"auto_grab_batflies": false`

Grab batflies on collision.

### "weight"
`float[1..2]`\
Ex: `"weight": 1.2`, `"weight": [0.9, 0.7]`

Player normal and starving body mass. If unspecified, the starving value is automatically calculated.

### "tunnel_speed"
`float[1..2]`\
Ex: `"tunnel_speed": 1.2`, `"tunnel_speed": [0.9, 0.7]`

Player normal and starving speed multiplier in crawlspaces. If unspecified, the starving value is automatically calculated.

### "climb_speed"
`float[1..2]`\
Ex: `"climb_speed": 1.2`, `"climb_speed": [0.9, 0.7]`

Player normal and starving speed multiplier on poles. If unspecified, the starving value is automatically calculated.

### "walk_speed"
`float[1..2]`\
Ex: `"walk_speed": 1.2`, `"walk_speed": [0.9, 0.7]`

Player normal and starving speed multiplier when walking. If unspecified, the starving value is automatically calculated.

### "crouch_stealth"
`float[1..2]`\
Ex: `"crouch_stealth": 1.2`, `"crouch_stealth": [0.9, 0.7]`

Player normal and starving multiplier for visual stealth when down on all fours. If unspecified, the starving value matches the normal value.

### "throw_skill"
`integer[1..2]`\
Ex: `"throw_skill": 2`, `"throw_skill": [2, 1]`

Player normal and starving spear throwing damage and distance. If unspecified, the starving value is 0.
- `0`: 0.6 to 0.9 damage, matching Monk
- `1`: 1.0 damage, matching Survivor
- `2`: 1.25 damage, matching Hunter

### "lung_capacity"
`float[1..2]`\
Ex: `"lung_capacity": 1.2`, `"lung_capacity": [0.9, 0.7]`

Player normal and starving multiplier for lung capacity. If unspecified, the starving value matches the normal value.

### "loudness"
`float[1..2]`\
Ex: `"loudness": 1.2`, `"loudness": [0.9, 0.7]`

Player normal and starving multiplier for loudness in regards to drawing creature attention. If unspecified, the starving value matches the normal value.

### "alignments"
```
{
  "<CreatureCommunities.CommunityID>": { "like": float, "strength": float = 1.0, "locked": boolean = false },
  ...
}
```
Ex:
```
"alignments": {
  "Lizards": { "like": 1.0, "strength": 0.2 },
  "Scavengers": { "like": -0.5, "locked": true }
}
```

Default reputation values that creature communities have towards the player. When a save is started, each community's reputation is moved towards `"like"` by the fraction `"strength"`. If `"locked"`, then the reputation will always be `"like"`.

`"like"` should range between -1 and 1. `"strength"` should range between 0 and 1.

### "diet"
```
{
  "base": string,
  "corpses": float,
  "meat": float,
  "plants": float,
  "overrides": {
    "<CreatureTemplate.Type>": float,
    "<AbstractPhysicalObject.Type>": float,
    ...
  }
}
```
Ex:
```
"diet": {
  "base": "White",
  "corpses": 0.5,
  "plants": 0.5,
  "overrides": {
    "Scavenger": 1.25,
    "DangleFruit": 0.5
  }
}
```

Edibility and nourishment of foods. Values are rounded to the nearest fourth. 0 indicates inedibility and -1 indicates that eating stuns the player. `"corpses"` multiplies food from eating large, non-centipede bodies. `"meat"` multiplies food from eating small creatures, centipedes, and meat-like objects (i.e., eggbug eggs and jellyfish). `"plants"` multiplies food from eating non-meat objects. `"overrides"` sets multipliers for individual creatures and objects.

If `"base"` is specified, it must refer to a `SlugcatStats.Name` that is from the base game or More Slugcats. When present, other diet properties are optional and default to the values for this character. Otherwise, these properties are required.

### "custom_colors"
```
[
  { "name": "Body", "story": color, "arena": color[] },
  { "name": "Eyes", "story": color, "arena": color[] },
  { "name": string, "story": color, "arena": color[] },
  ...
]
```
Ex:
```
"custom_colors": [
  { "name": "Body", "story": "10200C" },
  { "name": "Eyes", "story": "FFFFFF", "arena": [ "FFFFFF", "FF8080", "80FF80", "8080FF" ] },
  { "name": "Gills", "story": "454560" }
]
```

Configurable colors for this character. Colors are referenced by `"name"`, and will appear as such in the color customization menu. The color defaults to `"story"` in story mode, and is overridden by `"arena"` when in arena mode. If an insufficient number of colors are specified in `"arena"` or the property is omitted, then some players in arena mode will use `"story"` colors.

### "back_spear"
`boolean`\
Ex: `"back_spear": true`

Ability to store a spear on the player's back.

### "can_maul"
`boolean`\
Ex: `"can_maul": true`

Ability to deal damage to stunned creatures by holding grab.

### "maul_blacklist"
`string[]`\
Ex: `"maul_blacklist": [ "Scavenger", "PinkLizard" ]`

Creature types that may not be mauled by this character. These refer to `CreatureTemplate.Type` values.

### "maul_damage"
`float`\
Ex: `"maul_damage": 1.5`

Damage dealt when mauling. This defaults to 1.

## Game Features
### "karma"
`integer`\
Ex: `"karma": 3`

Initial karma level from 0 to 9.

### "karma_cap"
`integer`\
Ex: `"karma_cap": 7`

Initial karma cap from 0 to 9.

### "the_mark"
`boolean`\
Ex: `"the_mark": true`

Whether this character starts with the mark of communication.

### "the_glow"
`boolean`\
Ex: `"the_glow": true`

Whether this character starts with the neuron glow.

### "start_room"
`string[]`\
Ex: `"start_room": "LF_H01"`, `"start_room": [ "DS_RIVSTART", "LF_H01" ]`

Names of potential starting rooms. If the first room does not exist, then the second will be tried and so on.

### "guide_overseer"
`integer`\
Ex: `"guide_overseer": 1`

Color index of the player guide. If unspecified, then the guide overseer will not spawn.
- `1`: Yellow
- `2`: Green
- `3`: Red
- `4`: White
- `5`: Purple

Colors besides yellow may not work as expected if MSC is not enabled.

### "has_dreams"
`boolean`\
Ex: `"has_dreams": true`

Whether this character has a dream state.

### "use_default_dreams"
`boolean`\
Ex: `"use_default_dreams": false`

Whether this character uses Survivor's dreams. This defaults to `true`.

### "cycle_length_min"
`float`\
Ex: `"cycle_length_min": 5.5`

Minimum cycle length in minutes.

### "cycle_length_max"
`float`\
Ex: `"cycle_length_max": 8.25`

Maximum cycle length in minutes.

### "perma_unlock_gates"
`boolean`\
Ex: `"perma_unlock_gates": true`

Whether this character unlocks gates permanently when passing through.

### "food_min"
`integer`\
Ex: `"food_min": 7`

Food required to hibernate.

### "food_max"
`integer`\
Ex: `"food_max": 10`

Max food stored at any time.

### "select_menu_scene"
`string`\
Ex: `"select_menu_scene": "MySlugcatSelect"`

Scene ID for character select menu.

### "select_menu_scene_ascended"
`string`\
Ex: `"select_menu_scene_ascended": "MySlugcatAscended"`

Scene ID for character select menu after ascension.

### "sleep_scene"
`string`\
Ex: `"sleep_scene": "MySlugcatSleep"`

Scene ID for hibernation.

### "starve_scene"
`string`\
Ex: `"starve_scene": "MySlugcatStarve"`

Scene ID for death screen after starvation.

### "death_scene"
`string`\
Ex: `"death_scene": "MySlugcatDeath"`

Scene ID for non-starvation deaths.

### "intro_slideshow"
`string`\
Ex: `"intro_slideshow": "Scholar_Intro"`

Slideshow ID to use for the intro slideshow when starting a campaign for the first time.

### "outro_slideshow"
`string`\
Ex: `"outro_slideshow": "Scholar_Outro"`

Slideshow ID to use for the void sea slideshow.

### "world_state"
`string[]`\
Ex: `"world_state": "Red"`, `"world_state": [ "Spear", "Red" ]`

`SlugcatStats.Name` values to copy world state from. This includes spawns and room connections. If the first name does not exist, then the second will be tried and so on.

See [World State Tutorial](world-state-tutorial.md) for more information.

### "timeline_before"
`string[]`\
Ex: `"timeline_before": "Red"`, `"timeline_after": [ "Artificer", "Red" ]`

`SlugcatStats.Name` values to position this character before in the timeline. If the first name does not exist or is not on the timeline, then the second will be tried and so on. This takes precedence over `"timeline_after"`.

### "timeline_after"
`string[]`\
Ex: `"timeline_after": "Yellow"`, `"timeline_after": [ "Rivulet", "Yellow" ]`

`SlugcatStats.Name` values to position this character after in the timeline. If the first name does not exist or is not on the timeline, then the second will be tried and so on. `"timeline_before"` takes precedence over this.

### "title_card"
`string`\
Ex: `"title_card": "Title_Card_MySlugcat"`

Image to use instead of `illustrations/intro_roll_c.png` on the title screen, relative to the `illustrations` folder. The card will be picked randomly if multiple modded characters are installed. The image should be 1366 by 768 pixels to fill the screen.

### "expedition_enabled"
`boolean`\
Ex: `"expedition_enabled": false`

Whether to allow this character in Expedition mode. This defaults to `true`.

### "expedition_description"
`string`\
Ex: `"expedition_description": "First line of description.<LINE>Second line of description."`

Description on Expedition mode's character select screen. If unspecified, this character's story mode description will be used.