# Built-In Features

- `boolean`: `true` or `false`
- `integer`: Number with no fractional component
- `float`: Number with some fractional component
- `color`: List of 3-4 color components, hex color string, or object with `"r"`, `"g"`, `"b"`, and optionally `"a"` properties
- `type[min..max]`: List of between `min` and `max` elements of `type`, or a single element of `type` not in a list

## Player Features
### "color"
`color`

Default color for body and UI elements.

### "auto_grab_batflies"
`boolean`

Grab batflies on collision.

### "weight"
`float[1..2]`

Player normal and starving body mass. If unspecified, the starving value is automatically calculated.

### "tunnel_speed"
`float[1..2]`

Player normal and starving speed multiplier in crawlspaces. If unspecified, the starving value is automatically calculated.

### "climb_speed"
`float[1..2]`

Player normal and starving speed multiplier on poles. If unspecified, the starving value is automatically calculated.

### "walk_speed"
`float[1..2]`

Player normal and starving speed multiplier when walking. If unspecified, the starving value is automatically calculated.

### "crouch_stealth"
`float[1..2]`

Player normal and starving multiplier for visual stealth when down on all fours. If unspecified, the starving value is automatically calculated.

### "throw_skill"
`integer[1..2]`

Player normal and starving body mass. If unspecified, the starving value is automatically calculated.

### "lung_capacity"
`float[1..2]`

Player normal and starving multiplier for lung capacity. If unspecified, the starving value is automatically calculated.

### "loudness"
`float[1..2]`

Player normal and starving multiplier for loudness in regards to drawing creature attention. If unspecified, the starving value is automatically calculated.

### "alignments"
```
{
  "<CreatureCommunities.CommunityID>": { "like": float, "strength": float = 1.0, "locked": boolean = false },
  ...
}
```

Default reputation values that creature communities have towards the player. When a save is started, each community's reputation is moved towards `"like"` by the fraction `"strength"`. If `"locked"`, then the reputation will always be `"like"`.

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

Configurable colors for this character. Colors are referenced by `"name"`, and will appear as such in the color customization menu. The color defaults to `"story"` in story mode, and is overridden by `"arena"` when in arena mode. If an insufficient number of colors are specified in `"arena"` or the property is omitted, then some players in arena mode will use `"story"` colors.

## Game Features
### "karma"
`integer`

Initial karma level from 0 to 9.

### "karma_cap"
`integer`

Initial karma cap from 0 to 9.

### "start_room"
`string[]`

Names of potential starting rooms. If the first room does not exist, then the second will be tried and so on.

### "guide_overseer"
`integer`

Color index of the player guide. If unspecified, then the guide overseer will not spawn.
- `1`: Yellow
- `2`: Green
- `3`: Red
- `4`: White
- `5`: Purple

Colors besides yellow may not work as expected if MSC is not enabled.

### "has_dreams"
`boolean`

Whether this character has a dream state.

### "cycle_length_min"
`float`

Minimum cycle length in minutes.

### "cycle_length_max"
`float`

Maximum cycle length in minutes.

### "perma_unlock_gates"
`boolean`

Whether this character unlocks gates permanently when passing through.

### "food_min"
`integer`

Food required to hibernate.

### "food_max"
`integer`

Max food stored at any time.

### "select_menu_scene"
`string`

Scene ID for character select menu.

### "select_menu_scene_ascended"
`string`

Scene ID for character select menu after ascension.

### "world_state"
`string`

A `SlugcatStats.Name` value to copy world state from. This includes spawns and room connections. Specifying an MSC slugcat will fail if MSC is not installed.