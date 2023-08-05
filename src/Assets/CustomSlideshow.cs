using UnityEngine;
using SlideShowID = Menu.SlideShow.SlideShowID;
using System.Linq;
using System.IO;
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

        internal CustomSlideshowScene GetScene(SceneID id)
        {
            return Scenes.FirstOrDefault(scene => scene.ID == id);
        }

        /// <summary>
        /// A scene from a <see cref="CustomSlideshow"/> that holds data about when to appear and what images to use for what amount of time.
        /// </summary>
        public class CustomSlideshowScene : CustomScene
        {
            /// <summary>
            /// The time that this scene will start fading in, in seconds.
            /// </summary>
            public float StartAt { get; set; }

            /// <summary>
            /// The time that this image will finish fading in, in seconds.
            /// </summary>
            public float FadeInDoneAt { get; set; }

            /// <summary>
            /// The time that this image will start fading out at, in seconds.
            /// </summary>
            public float FadeOutStartAt { get; set; }

            /// <summary>
            /// The positions that the camera will focus on when playing this scene.
            /// </summary>
            /// <remarks>
            /// X and Y represent the pixel position of the camera, while Z represents the focus depth.
            /// </remarks>
            public Vector3[] CameraMovement { get; set; }

            /// <summary>
            /// Creates a new Scene from JSON.
            /// </summary>
            /// <param name="json">The JSON data to load from.</param>
            public CustomSlideshowScene(JsonObject json) : base(new SceneID(json.GetString("name"), false), json)
            {
                StartAt = json.GetFloat("fade_in_start");
                FadeInDoneAt = json.TryGet("fade_in_end")?.AsFloat() ?? (StartAt + 1f);
                FadeOutStartAt = json.GetFloat("fade_out_start");

                if(json.TryGet("camera_path")?.AsList() is JsonList movementList)
                {
                    CameraMovement = new Vector3[movementList.Count];
                    for(int i = 0; i < CameraMovement.Length; i++)
                    {
                        var vecList = movementList.GetList(i);
                        CameraMovement[i] = new Vector3(
                            vecList.GetFloat(0),
                            vecList.GetFloat(1),
                            vecList.TryGet(2)?.AsFloat() ?? -1f
                        );
                    }
                }
                else
                {
                    CameraMovement = new Vector3[] { new(0f, 0f, -1f) };
                }
            }
        }

        /// <summary>
        /// Data about a song from a <see cref="CustomSlideshow"/>.
        /// </summary>
        public class SlideshowMusic
        {
            /// <summary>
            /// The file name of the sound to use. This comes from the `StreamingAssets/music/songs` folder.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The duration of the song's fade in, in seconds.
            /// </summary>
            public float FadeIn { get; set; }

            /// <summary>
            /// Load information about a slideshow song from JSON data.
            /// </summary>
            /// <param name="json">The JSON data to load from.</param>
            public SlideshowMusic(JsonObject json)
            {
                Name = json.GetString("name");
                FadeIn = json.TryGet("fadein")?.AsFloat() ?? 40f;
            }
        }
    }
}
