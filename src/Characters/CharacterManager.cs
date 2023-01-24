using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SlugBase.Characters
{
    internal static class CharacterManager
    {
        public static readonly Dictionary<SlugcatStats.Name, SlugBaseCharacter> characters = new();
        public static readonly List<LoadError> errors = new();

        public static void Scan()
        {
            characters.Clear();

            var files = AssetManager.ListDirectory("slugbase", includeAll: true);

            foreach(var file in files.Where(file => file.EndsWith(".json")))
            {
                try
                {
                    var chara = Load(file);
                    characters.Add(chara.Name, chara);
                    SlugBasePlugin.Logger.LogMessage($"Loaded SlugBase character: {Path.GetFileName(file)}");
                }
                catch(JsonException e)
                {
                    SlugBasePlugin.Logger.LogError($"Failed to parse SlugBase character from {Path.GetFileName(file)}: {e.Message}\nField: {e.JsonPath ?? "unknown"}");
                    Debug.LogException(e);

                    errors.Add(new LoadError()
                    {
                        Exception = e,
                        FilePath = file
                    });
                }
                catch(Exception e)
                {
                    SlugBasePlugin.Logger.LogError($"Failed to load SlugBase character from {Path.GetFileName(file)}: {e.Message}");
                    Debug.LogException(e);

                    errors.Add(new LoadError()
                    {
                        Exception = e,
                        FilePath = file
                    });
                }
            }
        }

        public static SlugBaseCharacter Get(SlugcatStats.Name name)
        {
            if (name == null || (int)name == -1) return null;

            return characters.TryGetValue(name, out var chara) ? chara : null;
        }

        public static SlugBaseCharacter Get(Player player) => Get(player?.SlugCatClass);
        public static SlugBaseCharacter Get(RainWorldGame game) => Get(game?.StoryCharacter);

        private static SlugBaseCharacter Load(string jsonPath)
        {
            var json = JsonAny.Parse(File.ReadAllText(jsonPath)).AsObject();

            string id = json.GetString("id");

            Debug.Log(string.Join(", ", SlugcatStats.Name.values.entries));
            if (SlugcatStats.Name.values.entries.Contains(id))
                throw new FormatException($"The slugcat ID {id} is taken!");

            return new SlugBaseCharacter(new SlugcatStats.Name(id, true), json)
            {
                Path = jsonPath
            };
        }

        private static void Unload(SlugBaseCharacter chara)
        {
            //TODO: Implement unloading
        }

        public class LoadError
        {
            public Exception Exception;
            public string FilePath;
        }
    }
}
