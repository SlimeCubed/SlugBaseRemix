using UnityEngine;
using SlideShowID = Menu.SlideShow.SlideShowID;
using System;
using System.Linq;
using System.IO;
using static SlugBase.JsonUtils;
using System.Collections.Generic;
using Menu;

namespace SlugBase.Assets
{
    /// <summary>
    /// An intro cutscene added by SlugBase.
    /// </summary>
    public class CustomSlideshow
    {
        /// <summary>
        /// Match the ID to the id in a slideshow's json file, and provide the ProcessManager.
        /// </summary>
        /// <param name="ID">The ID of the slideshow to play, should be declared as a new Menu.SlideShow.SlideShowID(string, false) with the string matching the id of a slugbase slideshow .json file.</param>
        /// <param name="manager">The ProcessManager, needed to change the active process.</param>
        public static void NewOutro(string ID, ProcessManager manager)
        {
            manager.nextSlideshow = new Menu.SlideShow.SlideShowID(ID, false);
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
        }

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
        public string SceneFolder { get; }

        /// <summary>
        /// The music to play during a custom intro or outro
        /// </summary>
        public MMusic Music { get; }

        /// <summary>
        /// An array of images in this scene.
        /// </summary>
        public Scene[] Scenes { get; }

        /// <summary>
        /// If the game goes to the credits after playing the slideshow or the statistics screen
        /// </summary>
        public bool Credits { get; }

        private CustomSlideshow(SlideShowID id, JsonObject json)
        {
            ID = id;

            Scenes = json.GetList("scenes")
                .Select(img => new Scene(img.AsObject()))
                .ToArray();

            SceneFolder = json.TryGet("scene_folder")?.AsString().Replace('/', Path.DirectorySeparatorChar);
            // Don't know if I should force it to defalut to the normal intro theme or leave it empty so that it's an option for people to not have any music (But who would choose that? Someone probably)
            // In order to use a custom song, it must be in .ogg format, and placed in mods/MyMod/music/songs directory (Thank the Videocult overlords it's that simple)
            if (json.TryGet("music") is JsonAny music) {
                Music = new MMusic(music.AsObject());
            }
            else {
                Music = new MMusic("RW_Intro_Theme", 40f);
            }

            Credits = json.TryGet("credits")?.AsBool() ?? true;
        }

        /// <summary>
        /// An image from a <see cref="CustomSlideshow"/>.
        /// </summary>
        public class Image
        {
            /// <summary>
            /// The file name of the image to load. This is combined with <see cref="SceneFolder"/>.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The pixel position of this image's bottom left corner in the scene. (683, 384) is the center of the screen for flatmode, (0,0) for depth mode
            /// </summary>
            public Vector2 Position { get; set; }

            /// <summary>
            /// The depth of this image in the scene.
            /// </summary>
            public float Depth { get; set; } = 1f;

            /// <summary>
            /// The shader to use when rendering. Defaults to <see cref="MenuDepthIllustration.MenuShader.Normal"/>.
            /// </summary>
            public MenuDepthIllustration.MenuShader Shader { get; set; }


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
                Depth = json.TryGet("depth")?.AsFloat() ?? 1f;
                Shader = json.TryGet("shader")?.AsString() is string shader ? new(shader) : MenuDepthIllustration.MenuShader.Basic;
            }
        }

        /// <summary>
        /// A scene from a <see cref="CustomSlideshow"/> that holds data about when to appear and what images to use for what amount of time
        /// </summary>
        public class Scene
        {
            /// <summary>
            /// The unique name of the scene to load.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The list of images the scene has
            /// </summary>
            public List<Image> Images {get; set; }

            /// <summary>
            /// The second that this scene will start fading in
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
            /// The positions that the images will try to go to, if they are not in flatMode (Determined by the game)
            /// </summary>
            public Vector2[] Movement { get; set; }
            
            /// <summary>
            /// Creates a new image.
            /// </summary>
            /// <param name="name">The file name.</param>
            /// <param name="images">The list of images the scene gets.</param>
            /// <exception cref="ArgumentNullException"></exception>
            public Scene (string name, List<Image> images)
            {
                Name = name;
                Images = images;
            }

            /// <summary>
            /// Creates a new scene from JSON.
            /// </summary>
            /// <param name="json">The JSON data to load from.</param>
            public Scene (JsonObject json) : this(json.GetString("name"), json.GetList("images")
                                                                                                    .Select(img => new Image(img.AsObject()))
                                                                                                    .ToList())
            {
                StartAt = json.TryGet("displayat")?.AsInt() ?? 0;
                FadeInDoneAt = json.TryGet("fadeinfinish")?.AsInt() ?? 3;
                FadeOutStartAt = json.TryGet("fadeoutstart")?.AsInt() ?? 8;
                // Find out better way to do this
                try {
                    Movement = json.GetList("movepositions").Select(vec => ToVector2(vec)).ToArray();
                }
                catch {
                    Movement = new Vector2[1]{new(0,0)};
                }
            }
        }

        /// <summary>
        /// Data about a song from a <see cref="CustomSlideshow"/>.
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
            /// <param name="fadeIn">The time for the music to fade in to full volume.</param>
            public MMusic(string name, float fadeIn) : this(name)
            {
                FadeIn = fadeIn;
            }

            /// <summary>
            /// Creates data about a song to play from a JSON
            /// </summary>
            /// <param name="json">The JSON data to load from.</param>
            public MMusic(JsonObject json) : this(json.GetString("name"))
            {
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
