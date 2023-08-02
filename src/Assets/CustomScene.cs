using UnityEngine;
using Menu;
using SceneID = Menu.MenuScene.SceneID;
using System;
using System.Linq;
using System.IO;
using static SlugBase.JsonUtils;

namespace SlugBase.Assets
{
    /// <summary>
    /// A scene added by SlugBase.
    /// </summary>
    public class CustomScene
    {
        /// <summary>
        /// Stores all registered <see cref="CustomScene"/>s.
        /// </summary>
        public static JsonRegistry<SceneID, CustomScene> Registry { get; } = new((key, json) => new(key, json));

        /// <summary>
        /// This scene's unique ID.
        /// </summary>
        public SceneID ID { get; }

        /// <summary>
        /// An array of images in this scene.
        /// </summary>
        public Image[] Images { get; }

        /// <summary>
        /// An array of depths that the camera may focus on.
        /// </summary>
        public float[] IdleDepths { get; }

        /// <summary>
        /// A path relative to StreamingAssets to load images from.
        /// </summary>
        public string SceneFolder { get; }

        /// <summary>
        /// The position of the glow's center.
        /// <para>Only effective when used on the slugcat select screen.</para>
        /// </summary>
        public Vector2? GlowPos { get; }

        /// <summary>
        /// The position of the mark's center.
        /// <para>Only effective when used on the slugcat select screen.</para>
        /// </summary>
        public Vector2? MarkPos { get; }

        /// <summary>
        /// The pixel offset for this scene in the select menu.
        /// <para>Only effective when used on the slugcat select screen.</para>
        /// </summary>
        public Vector2? SelectMenuOffset { get; }

        /// <summary>
        /// The depth of the slugcat image in this scene.
        /// <para>Only effective when used on the slugcat select screen.</para>
        /// </summary>
        public float? SlugcatDepth { get; }

        /// <summary>
        /// If a scene is used as a Dream, should it replace any current dream
        /// </summary>
        public bool OverrideDream { get; }

        internal CustomScene(SceneID id, JsonObject json)
        {
            ID = id;

            Images = json.GetList("images")
                .Select(img => new Image(img.AsObject()))
                .ToArray();

            if (this is not SlugBase.Assets.CustomSlideshow.CustomSlideshowScene)
                IdleDepths = json.GetList("idle_depths")
                    .Select(depth => depth.AsFloat())
                    .ToArray();

            SceneFolder = json.TryGet("scene_folder")?.AsString().Replace('/', Path.DirectorySeparatorChar);

            if(json.TryGet("glow_pos") is JsonAny glowPos)
                GlowPos = ToVector2(glowPos);

            if(json.TryGet("mark_pos") is JsonAny markPos)
                MarkPos = ToVector2(markPos);

            if (json.TryGet("select_menu_pos") is JsonAny selectMenuPos)
                SelectMenuOffset = ToVector2(selectMenuPos);

            SlugcatDepth = json.TryGet("slugcat_depth")?.AsFloat();

            OverrideDream = json.TryGet("dream_override")?.AsBool() ?? true;
        }

        /// <summary>
        /// An image from a <see cref="CustomScene"/>.
        /// </summary>
        public class Image
        {
            /// <summary>
            /// The file name of the image to load. This is combined with <see cref="SceneFolder"/>.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The pixel position of this image's bottom left corner in the scene.
            /// </summary>
            public Vector2 Position { get; set; }

            /// <summary>
            /// The depth of this image in the scene.
            /// </summary>
            public float Depth { get; set; } = -1f;

            /// <summary>
            /// The shader to use when rendering. Defaults to <see cref="MenuDepthIllustration.MenuShader.Normal"/>.
            /// </summary>
            public MenuDepthIllustration.MenuShader Shader { get; set; }

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
                Depth = json.TryGet("depth")?.AsFloat() ?? -1f;
                Shader = json.TryGet("shader")?.AsString() is string shader ? new(shader) : MenuDepthIllustration.MenuShader.Normal;
                Flatmode = json.TryGet("flatmode")?.AsBool() ?? false;
            }
        }
    }
}
