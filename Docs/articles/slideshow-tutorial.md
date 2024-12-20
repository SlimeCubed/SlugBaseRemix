# CustomSlideshow
A CustomSlideshow is Slugbase's method of allowing an easy way for mod creators to add slugcat-specific slideshows, such as intro, dream, or ending sequences.

CustomSlideshows are stored in the `slugbase/slideshows/` folder as unique .json files.
# Basic Usage
## "id"
`SlideShow.SlideShowID` (acts like a `string`)\
Ex. `"id": "Intro_MySlugcat"`

ID is a `SlideShow.SlideShowID`, which effectively is a string that no other scene should share. Instead of referencing a file name, all built-in SlugBase features reference whatever ID that is put here.

If no ID is defined, the scene will not be able to be referenced.
