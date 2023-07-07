using UnityEngine;
using SlideShowID = Menu.SlideShow.SlideShowID;
using System;
using System.Linq;
using System.IO;
using static SlugBase.JsonUtils;

namespace SlugBase.Assets
{
    /// <summary>
    /// A scene added by SlugBase.
    /// </summary>
    public class CustomIntroOutroScene
    {
        /// <summary>
        /// Stores all registered <see cref="CustomIntroOutroScene"/>s.
        /// </summary>
        public static JsonRegistry<SlideShowID, CustomIntroOutroScene> Registry { get; } = new((key, json) => new(key, json));

        /// <summary>
        /// This scene's unique ID.
        /// </summary>
        public SlideShowID ID { get; }

        /// <summary>
        /// A path relative to StreamingAssets to load images from.
        /// </summary>
        public string SceneFolder { get; }

        /// <summary>
        /// The music to play during a custom intro or outro
        /// </summary>
        public MMusic Music { get; }

        /// <summary>
        /// An array of images in this scene.
        /// </summary>
        public Image[] Images { get; }

        private CustomIntroOutroScene(SlideShowID id, JsonObject json)
        {
            ID = id;

            Images = json.GetList("images")
                .Select(img => new Image(img.AsObject()))
                .ToArray();

            SceneFolder = json.TryGet("scene_folder")?.AsString().Replace('/', Path.DirectorySeparatorChar);
            // Don't know if I should force it to defalut to the normal intro theme or leave it empty so that it's an option for people to not have any music (But who would choose that? Someone probably)
            // In order to use a custom song, it must be in .ogg format, and placed in mods/MyMod/music/songs directory (Thank the Videocult overlords it's that simple)
            try {
                Music = new MMusic(json.GetObject("music"));
            }
            catch {
                Music = new MMusic("RW_Intro_Theme", 40f);
            }
        }

        /// <summary>
        /// An image from a <see cref="CustomIntroOutroScene"/>.
        /// </summary>
        public class Image
        {
            /// <summary>
            /// The file name of the image to load. This is combined with <see cref="SceneFolder"/>.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The pixel position of this image's bottom left corner in the scene. (683, 384) is the center of the screen
            /// </summary>
            public Vector2 Position { get; set; }

            /// <summary>
            /// The second that this image will start fading in
            /// </summary>
            public int StartAt { get; set; } = 0;

            /// <summary>
            /// The second that this image will finish fading in
            /// </summary>
            public int FadeInDoneAt { get; set; } = 0;

            /// <summary>
            /// The second that this image will finish fading out
            /// </summary>
            public int FadeOutStartAt { get; set; } = 0;

            /// <summary>
            /// If <c>true</c>, this image will display when in flat mode and will be hidden otherwise.
            /// </summary>
            public bool Flatmode { get; set; }

            /// <summary>
            /// Creates a new image.
            /// </summary>
            /// <param name="name">The file name.</param>
            /// <param name="position">The pixel position of the bottom left corner.</param>
            /// <exception cref="ArgumentNullException"></exception>
            public Image(string name, Vector2 position)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));

                Name = name.Replace('/', Path.DirectorySeparatorChar);
                Position = position;
            }

            /// <summary>
            /// Creates a new image from JSON.
            /// </summary>
            /// <param name="json">The JSON data to load from.</param>
            public Image(JsonObject json) : this(json.GetString("name"), ToVector2(json.Get("pos")))
            {
                StartAt = json.TryGet("displayat")?.AsInt() ?? 0;
                FadeInDoneAt = json.TryGet("fadeinfinish")?.AsInt() ?? 3;
                FadeOutStartAt = json.TryGet("fadeoutstart")?.AsInt() ?? 8;
                Flatmode = json.TryGet("flatmode")?.AsBool() ?? false;
            }
        }

        /// <summary>
        /// Data about a song from a <see cref="CustomIntroOutroScene"/>.
        /// </summary>
        public class MMusic{

            /// <summary>
            /// The file name of the sound to use. This is combined with <see cref="SceneFolder"/>.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The amount of time the sound will fade in for, until it is at full volume.
            /// </summary>
            public float FadeIn { get; set; }
            
            /// <summary>
            /// Creates new data about a song to play.
            /// </summary>
            /// <param name="name">The sound name.</param>
            public MMusic(string name)
            {
                Name = name;
            }

            /// <summary>
            /// Creates data about a song to play.
            /// </summary>
            /// <param name="name">The sound name.</param>
            /// <param name="fadeIn">The time for the music to fade in to full volume. </param>
            public MMusic(string name, float fadeIn) : this(name)
            {
                FadeIn = fadeIn;
            }

            /// <summary>
            /// Creates data about a song to play from a JSON
            /// </summary>
            /// <param name="json">The JSON data to load from. </param>
            public MMusic(JsonObject json) : this(json.GetString("name"))
            {
                float fadeIn;
                // This try catch feels really hacky, surely there's a better way? But GetFloat Can't return null so I can't use ?? ...
                try {
                    fadeIn = json.GetFloat("fadein");
                } catch {
                    fadeIn = 40f;
                }
                FadeIn = fadeIn;
            }
        }
    }
}
