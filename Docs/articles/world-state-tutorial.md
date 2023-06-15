# World State

A "World State" is SlugBase' way of categorizing all world-related slugcat specific changes. A SlugBase slugcat can inherit a world state from another slugcat to use its same room connections, spawns, accessible regions, and so on. The world will appear as if it was the campaign of the other slugcat, except for non-world changes, such as iterator states, echoes, campaign cutscenes, ect.

# Basic Usage

### "world_state"
`string[]`\
Ex: `"world_state": "Red"`, `"world_state": [ "Spear", "Red" ]`

World State is an array of `SlugcatStats.Name`. If one slugcat is specified, that world state will be used. 
In the example of `"world_state": "Red"`, the world will contain the same spawns, rooms, and regions as the slugcat with the id of "Red" (aka Hunter.)

If the world state is set to a non-present ID, it will be ignored and the next slugcat in the array is inherited from instead.
In the example of `"world_state": [ "Spear", "Red" ]`, Spearmaster's world state will be used if MSC is enabled, otherwise Hunter's will be used instead.

If no world state is defined, or no slugcats in the world state are valid, the slugcat's own world state will be used. By default, this world state contains no slugcat-specific changes, which is usually closest to Survivor, but with significantly less spawns. And any deviations from this state must be manually defined. It is **strongly recommended** to inherit from a vanilla or MSC slugcat, for the sake of other mods which won't be able to define the world state for every modded slugcat (such as custom regions, or spawn mods.)

# Custom World State
The proper way to have a slugcat specific world state is to use the slugcat's own ID in the world state listed before a base game slugcat. 
```json
"world_state": [ "MySlugcat", "Artificer" ]
```
Now, any time the world state is defined for "MySlugcat", it will *override* Artificer's, but it will still use the Artificer world state when none is defined for "MySlugcat."

Any number of slugcats can be defined in world state, and their priority will be in order of left to right. 
```json
"world_state": [ "MySlugcat", "YourSlugcat", "Saint", "Rivulet", "Monk" ]
```
For example, something like the above may be desirable to mostly inherit from Saint's world state, but to make "MySlugcat" specifically be able to access Exterior instead of Silent Construct, and inherit Rivulet's version of Exterior.

The exact logic of how each file\action is evaluated to pick the most appropriate world state varies depending on the file\action, so the rest of this guide will go over each individually.

# Overrides

## Room Settings, Map Files, Region Properties, and Region Display Names
Slug-specific versions of these are defined by appending -slugname to their filenames.
The one that's chosen will be the first slugcat who has a unique file.

```json
"world_state": [ "MySlugcat", "YourSlugcat", "Artificer" ]
```
Since "MySlugcat" is an existing slugcat ID and is first in the `"world_state"` list, it will be prioritized:
```txt
XX_A01_settings-MySlugcat.txt <- this one is chosen
XX_A01_settings-Artificer.txt
XX_A01_settings.txt
```

If a file for the first slugcat isn't there, the next on the list is used instead:
```txt
Map_XX-Artificer.txt <- this one is chosen
Map_XX.txt
```

If none are present, then the default is used:
```txt
Properties-Mother.txt
Properties-Spear.txt
Properties.txt <- this one is chosen
```

## Object Slugcat Filters (filters, tokens, triggers)
The first slugcat name is always used.

## World Files
As with other items, the first valid slugcat in `"world_state"` is used when reading world files. Slugcats *must be mentioned anywhere in the file* to be considered valid.

Most of the time this will just work, but if you want a world file to have no slugcat-specific spawns or conditional links, you must mention the slugcat by name. This can be as simple as adding a conditional link that does nothing.
```txt
MySlugcat : DISCONNECTED <- this is good enough to use
```

It's recommended to only use the actual world file for room connections, as there's a new way to define creatures for a specific slug...

## Spawns Override
A new file can be added to a region's folder called `Spawns_XX-slugname.txt` that fully replaces the spawns section for a region. If multiple slugs in the worldstate have spawns defined, the same logic as all other -slugname files is used.
Example:
`MyMod\World\SU\Spawns_SU-MySlugcat.txt`
```txt
OFFSCREEN : 0-MirosVulture-12
SU_B08 : 3-Red
LINEAGE : SU_A06 : 4 : Red-1, NONE-0.001, Red-0
//this spawns nothing but a couple terrifying red lizards
//and a dozen miros vultures in the region
//comments are ignored, just as if they were in the world file
```

## Story Regions
A slugcat's story regions are the ones they're required to visit for the Wanderer achievement. They're also the ones that will show up by default on the collectables tracker.

Story regions are initially inherited from the first vanilla or MSC slugcat, and can then be modified using a new feature called `"story_regions"`.
```json
"story_regions": ["XX", "SL", "-LM"]
```
Each acronym listed will be added to the story regions. If an acronym has "-" before it, that region will be removed instead. In this case, our cat "MySlugcat" inherits from Artificer who replaces LM with SL. These story regions undo that replacement, as well as adding the new "XX" region.

Story regions can also be defined using CRS' `MetaProperties.txt` file for each region.

## Optional Regions
Optional regions are all the non-story regions that the game thinks a slugcat can visit. These regions' collectable trackers will appear once they're visited, and will be selectable in Safari as the slug if the slug has visited them.

Since in ordinary gameplay there's never any harm in including an optional region, SlugBase will add any story or optional region  for all slugs in the worldstate to the optional regions.

## Region Equivalences
Equivalent regions are what makes LM load instead of SL when playing as Spearmaster or Artificer. For custom regions or custom slugs, there's a base game system for equivalences that can be used.

If MSC's Equivalences weren't hardcoded, there'd be a file in `world\LM\` called `Equivalences.txt` that would contain the text `SL-Artificer,SL-Spear`. This means LM is equivalent to SL for Artificer and Spear(master).

SlugBase will use the first equivalence it finds, if any.
In this example, LM would be used instead of SL because Artificer is the first equivalence.
This can be undone by making `world\SL\Equivalences.txt` and writing `SL-Wanderer`, making SL equivalent to SL for our "Wanderer" cat.

## Broken Shelters
Broken shelters will use the first slugcat who has broken shelters defined. This can be overridden in a similar fashion to the others by throwing `Broken Shelters: Wanderer: ` into the `properties.txt`.

## Slug-Specific Room Objects
Some room objects like tokens, filters, and triggers are slugcat specific. For now, these will just use the custom slugcat's name.


