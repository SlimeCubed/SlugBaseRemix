using UnityEngine;
using SlideShowID = Menu.SlideShow.SlideShowID;
using System.Linq;
using System.IO;
using static SlugBase.JsonUtils;
using static Menu.MenuScene;
using System;
using SlugBase.SaveData;

namespace SlugBase.Assets
{
    /// <summary>
    /// An intro cutscene added by SlugBase.
    /// </summary>
    public class CustomSlideshow
    {
        /// <summary>
        /// Stores all registered <see cref="CustomSlideshow"/>s.
        /// </summary>
        public static JsonRegistry<SlideShowID, CustomSlideshow> Registry { get; } = new((key, json) => new(key, json));

        /// <summary>
        /// This scene's unique ID.
        /// </summary>
        public SlideShowID ID { get; }

        /// <summary>
        /// A path relative to StreamingAssets to load images from.
        /// </summary>
        public string SlideshowFolder { get; }

        /// <summary>
        /// The music to play during a custom intro or outro
        /// </summary>
        public SlideshowMusic Music { get; }

        /// <summary>
        /// An array of images and other data in this scene.
        /// </summary>
        public CustomSlideshowScene[] Scenes { get; }

        /// <summary>
        /// The process to go to after playing the slideshow
        /// </summary>
        public ProcessManager.ProcessID Process { get; }

        private CustomSlideshow(SlideShowID id, JsonObject json)
        {
            ID = id;

            SlideshowFolder = json.TryGet("slideshow_folder")?.AsString().Replace('/', Path.DirectorySeparatorChar);

            // In order to use a custom song, it must be in .ogg format, and placed in mods/MyMod/music/songs directory (Thank the Videocult overlords it's that simple)
            if (json.TryGet("music") is JsonAny music) { Music = new SlideshowMusic(music.AsObject()); }

            Scenes = json.GetList("scenes")
                .Select(img => new CustomSlideshowScene(img.AsObject()))
                .ToArray();

            Process = new ProcessManager.ProcessID(json.GetString("next_process"));
        }

        /// <summary>
        /// A scene from a <see cref="CustomSlideshow"/> that holds data about when to appear and what images to use for what amount of time
        /// </summary>
        public class CustomSlideshowScene : CustomScene
        {

            /// <summary>
            /// The second that this scene will start fading in, in seconds
            /// </summary>
            public float StartAt { get; set; }

            /// <summary>
            /// The second that this image will finish fading in, in seconds
            /// </summary>
            public float FadeInDoneAt { get; set; }

            /// <summary>
            /// The second that this image will start fading out at, in seconds
            /// </summary>
            public float FadeOutStartAt { get; set; }

            /// <summary>
            /// The positions that the images will try to go to, if they are not in flatMode (Determined by the game)
            /// </summary>
            public Vector2[] Movement { get; set; }

            /// <summary>
            /// Creates a new Scene from JSON.
            /// </summary>
            /// <param name="json">The JSON data to load from.</param>
            public CustomSlideshowScene (JsonObject json) : base(new Menu.MenuScene.SceneID(json.GetString("name"), false), json)
            {
                StartAt = json.TryGet("fade_in")?.AsFloat() ?? 0;
                FadeInDoneAt = json.TryGet("fade_in_finish")?.AsFloat() ?? 3;
                FadeOutStartAt = json.TryGet("fade_out_start")?.AsFloat() ?? 8;
                Movement = json.TryGet("movements")?.AsList().Select(vec => ToVector2(vec)).ToArray() ?? new Vector2[1]{new(0,0)};
            }
        }

        /// <summary>
        /// Data about a song from a <see cref="CustomSlideshow"/>.
        /// </summary>
        public class SlideshowMusic{

            /// <summary>
            /// The file name of the sound to use. This comes from the 'StreamingAssets/music/songs' folder.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The amount of time the sound will fade in for, until it is at full volume.
            /// </summary>
            public float FadeIn { get; set; }

            /// <summary>
            /// Creates data about a song to play from a JSON
            /// </summary>
            /// <param name="json">The JSON data to load from.</param>
            public SlideshowMusic(JsonObject json)
            {
                Name = json.GetString("name");
                if (json.TryGet("fadein") is JsonAny fadeIn)
                {
                    FadeIn = fadeIn.AsFloat();
                }
                else
                {
                    FadeIn = 40f;
                }
            }
        }
    }
}
