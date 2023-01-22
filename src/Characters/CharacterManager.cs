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
            var files = AssetManager.ListDirectory("slugbase", includeAll: true);

            foreach(var file in files.Where(file => file.EndsWith(".json")))
            {
                try
                {
                    var chara = Load(file);
                    characters.Add(chara.Name, chara);
                }
                catch(Exception e)
                {
                    SlugBasePlugin.Logger.LogError($"Failed to load SlugBase character: {Path.GetFileName(file)}");
                    Debug.LogException(e);

                    errors.Add(new LoadError()
                    {
                        Exception = e,
                        FilePath = file
                    });
                }
            }
        }

        private static SlugBaseCharacter Load(string jsonPath)
        {
            string id;

            Dictionary<string, object> json = File.ReadAllText(jsonPath).dictionaryFromJson();
            if (json.TryGetValue("id", out object jsonIdObj) && jsonIdObj is string jsonId)
                id = jsonId;
            else
                throw new FormatException("Missing \"id\" property!");

            if (SlugcatStats.Name.values.entries.Contains(id))
                throw new FormatException($"The slugcat ID {id} is taken!");

            var chara = new SlugBaseCharacter(new SlugcatStats.Name(id, true), json);

            chara.Validate();

            return chara;
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
