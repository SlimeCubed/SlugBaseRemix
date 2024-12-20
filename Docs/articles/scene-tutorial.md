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

For example, with a specific scene there are 5 images stored in the folder `StreamingAssets/mods/myslugcat/scenes/sleep - myslugcat/`, each with a number name (such as `1.png`, `2.png`, etc). I could individually input each image with its filepath (as `scenes/sleep - myslugcat/1`, `scenes/sleep - myslugcat/1`, etc), or I could put `"scene_folder": "scenes/sleep - myslugcat"` at the start and then input the images as `1`, `2`, and so on instead.
## "idle_depths"
`float[]`\
Ex. `"idle_depths": [ 2.8 ]`

IdleDepths is an array of `float`s that determines what depths the camera may focus on. For death screens, the camera focus usually only has one focus, but for others, there is more room for variation.
# Select Screen Usage
This section is for parameters that are only effective on the select menus. All of these are optional, and none of these have any effect within slideshows or anywhere outside of the select screen.
## "slugcat_depth"
`float`\
Ex. `"slugcat_depth": 2.8`

SlugcatDepth is a `float` that determines the depth of the slugcat (or main object/creature of the scene). It's recommended to choose one of the numbers that `idle_depths` has, as otherwise the focus may only be on the object/creature you want focused on for a short period of time.
## "mark_pos"
`Vector2`\
Ex. `"mark_pos": [620,500]`

MarkPos is a `Vector2` (which is effectively an array of 2 integers) that determines where the Mark of Communication appears on the scene if the slugcat has acquired it on the current save file.
## "glow_pos"
`Vector2`\
Ex. `"glow_pos": [620,400]`

GlowPos is a `Vector2` (which is effectively an array of 2 integers) that determines where the glow from the Mark of Communication appears on the scene if the slugcat has acquired it on the current save file. Usually, it's best to set the second number to 100 below whatever your `mark_pos` is.
## "select_menu_pos`
`Vector2`\
Ex. `"select_menu_pos": [0, 0]`

SelectMenuOffset is a `Vector2` (which is effectively an array of 2 integers) that offsets all images in the scene by whatever numbers are within it.
# Images Within Slideshows
The "images" array is filled with `CustomScene.Image`s, which are just objects of which contain information about the layered images of the scene. Here are the constructors of each image:
## "name"
`string`\
Ex. `"name": "scenes/sleep - myslugcat/abc"` (When the image is stored at `StreamingAssets/mods/myslugcat/scenes/sleep - myslugcat/abc.png` and (insert link)[`scene_folder`] is not defined)

Name determines the filepath of the image shown. If (insert link)[`scene_folder`] is defined, SlugBase will look for the file at whatever folder (that is inside the mod folder) that it defines, instead of `StreamingAssets/mods/<mod_id>`.
## "pos"
`Vector2`

Position is a `Vector2` (which is effectively an array of 2 integers) that determines the pixel location of where the image is on-screen.
## "flatmode" (Defaults to `false`)
`bool`\
Ex. `"flatmode": true`

If `true`, this image will display when in flat mode and will be hidden otherwise.
## "depth"
`float`\
Ex. `"depth": 2.5`

Depth is a `float` determining the image's depth.
## "shader" (Defaults to `Menu.MenuDepthIllustration.MenuShader.Normal`)
`MenuDepthIllustration.MenuShader`\
Ex. `"shader": "Basic"`

Shader is how the image is rendered. By default, it uses the vanilla `Normal` shader.
# Example Scene
You can dig through (here)[https://github.com/SlimeCubed/SlugTemplate/tree/master/mod] for finding the example for how the scenes are constructed and referenced.
