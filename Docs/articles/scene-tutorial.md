# CustomScene
A CustomScene is Slugbase's method of allowing an easy way for mod creators to add slugcat-specific scenes, such as unique select screens, sleep screens, or even intro sequences.

CustomScenes are stored either in the `slugbase/scenes/` folder as unique .json files, or inside of (insert link)[CustomSlideshow]s.
# Basic Usage
## "id"
`MenuScene.SceneID` (acts like a `string`)\
Ex. `"id": "Sleep_MySlugcat"`

ID is a `SceneID`, which effectively is a string that no other scene should share. Instead of referencing a file name, all built-in SlugBase features reference whatever ID that is put here.

If no ID is defined, the scene will not be able to be referenced, unless it is a scene that is inside of a (insert link)[CustomSlideshow].
## "scene_folder" (Optional)
`string`\
Ex. `"scene_folder": "scenes/sleep - myslugcat"`

SceneFolder is a `string`, acting as a directory from the root of your mod (usually a folder in `StreamingAssets/mods/`) of which all of the images within this scene will use instead of the root directory. Do not add a `/` to the end, as it may cause errors!

For example, with a specific scene there are 5 images stored in the folder `StreamingAssets/mods/scenes/sleep - myslugcat/`, each with a number name (such as `1.png`, `2.png`, etc). I could individually input each image with its filepath (as `scenes/sleep - myslugcat/1`, `scenes/sleep - myslugcat/1`, etc), or I could put `"scene_folder": "scenes/sleep - myslugcat"` at the start and then input the images as `1`, `2`, and so on instead.
