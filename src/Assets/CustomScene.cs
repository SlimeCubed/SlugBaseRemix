using System.Collections.Generic;
using UnityEngine;
using Menu;
using SceneID = Menu.MenuScene.SceneID;
using System;
using System.Linq;
using SlugBase.Features;
using System.IO;

namespace SlugBase.Assets
{
    public class CustomScene
    {
        public static JsonRegistry<SceneID, CustomScene> Registry { get; } = new((key, json) => new(key, json));

        public SceneID ID { get; }
        public Image[] Images { get; }
        public float[] IdleDepths { get; }
        public string SceneFolder { get; }

        public Vector2? GlowPos { get; }
        public Vector2? MarkPos { get; }
        public Vector2? SelectMenuOffset { get; }
        public float? SlugcatDepth { get; }

        private CustomScene(SceneID id, JsonObject json)
        {
            ID = id;

            Images = json.GetList("images")
                .Select(img => new Image(img.AsObject()))
                .ToArray();

            IdleDepths = json.GetList("idle_depths")
                .Select(depth => depth.AsFloat())
                .ToArray();

            SceneFolder = json.TryGet("scene_folder")?.AsString().Replace('/', Path.DirectorySeparatorChar);

            if(json.TryGet("glow_pos") is JsonAny glowPos)
                GlowPos = FeatureTypes.ToVector2(glowPos);

            if(json.TryGet("mark_pos") is JsonAny markPos)
                MarkPos = FeatureTypes.ToVector2(markPos);

            if (json.TryGet("select_menu_pos") is JsonAny selectMenuPos)
                SelectMenuOffset = FeatureTypes.ToVector2(selectMenuPos);

            SlugcatDepth = json.TryGet("slugcat_depth")?.AsFloat();
        }

        public class Image
        {
            public string Name { get; set; }
            public Vector2 Position { get; set; }
            public float Depth { get; set; } = -1f;
            public MenuDepthIllustration.MenuShader Shader { get; set; }
            public bool Flatmode { get; set; }

            public Image(string name, Vector2 position)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));

                Name = name.Replace('/', Path.DirectorySeparatorChar);
                Position = position;
            }

            public Image(JsonObject json) : this(json.GetString("name"), FeatureTypes.ToVector2(json.Get("pos")))
            {
                Depth = json.TryGet("depth")?.AsFloat() ?? -1f;
                Shader = json.TryGet("shader")?.AsString() is string shader ? new(shader) : MenuDepthIllustration.MenuShader.Normal;
                Flatmode = json.TryGet("flatmode")?.AsBool() ?? false;
            }
        }
    }
}
