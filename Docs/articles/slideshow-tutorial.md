# CustomSlideshow
A CustomSlideshow is Slugbase's method of allowing an easy way for mod creators to add slugcat-specific slideshows, such as intro, dream, or ending sequences.

CustomSlideshows are stored in the `slugbase/slideshows/` folder as unique .json files.
# Basic Usage
## "id"
`SlideShow.SlideShowID` (acts like a `string`)\
Ex. `"id": "Intro_MySlugcat"`

ID is a `SlideShow.SlideShowID`, which effectively is a string that no other scene should share. Instead of referencing a file name, all built-in SlugBase features reference whatever ID that is put here.

If no ID is defined, the scene will not be able to be referenced.
## "slideshow_folder" (Optional)
`string`\
Ex. `"slideshow_folder": "scenes/intro - myslugcat"`

SlideshowFolder is a `string`, acting as a directory from the root of your mod (usually a folder in `StreamingAssets/mods/`) of which all of the images within this scene will use instead of the root directory. Do not add a `/` to the end, as it may cause errors!

(An example for how it works can be found here(insert link).)
## "music"
`CustomSlideshow.SlideshowMusic`\
Ex.
```
"music": {
    "name": "RW_Outro_Theme_B",
    "fade_in": 5
}
```
Music is a `CustomSlideshow.SlideshowMusic`, which is an object with two inputs: `"name"` and `"fade_in"`. `"name"` is a string representing the file name of the sound to use from the `StreamingAssets/music/songs` folder, and `"fade_in"` is a float representing the amount of seconds to fade the music in.
## "next_process"
`ProcessManager.ProcessID`\
Ex. `"next_process": "Credits"`

Process is a `ProcessManager.ProcessID`, which acts like a string. All of the available-to-reference processes are listed [here](https://rainworldmodding.miraheze.org/wiki/Slideshows_and_Scenes#Next_process).
# Using Scenes Within Slideshows
To use scenes within slideshows, first you must start with a `"scenes": []`, and fill the array with the json from each scene. However, these do not act like normal scenes in some ways. Mainly in that some of the focusing-related parameters of a scene are obsolete, and are replaced with the ability to control the focus and camera movement. Here are the added parameters of any given slideshow scene:
## "fade_in_start"
`float`\
Ex. `"fade_in_start": 1.1`

StartAt is a `float` that determines the time of which the scene will begin to fade in. The time is recorded in seconds from the point that the cutscene started playing, so keep in mind that all of the time-related parameters should be chronological (so that two scenes aren't playing at the same time).
## "fade_in_end"
`float`\
Ex. `"fade_in_end": 3.5`

FadeInDoneAt is a `float` that determines the time of which the scene will finish fading in. (This is not the length of how long the fading in is! You can calculate how long a given scene takes to fade in by doing `fade_in_end`-`fade_in_start`.)
## "fade_out_start"
`float`\
Ex. `"fade_out_start": 9.2`

FadeOutStartAt is a `float` that determines the time of which the scene will begin to fade out. (There are no parameters for how long the fading out is.)
## "camera_path"
`Vector3[]`\
Ex.
```
"camera_path": {
    [0, 100, 0.5],
    [10, 200, 0.7],
    [-50, 150, 0.4]
}
```

CameraMovement is an array with `Vector3`s in it. The first/second numbers represent the x/y (in pixel position) of the camera, and the third number represents the depth of focus.
