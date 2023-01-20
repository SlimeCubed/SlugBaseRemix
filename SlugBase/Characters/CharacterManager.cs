using System.Collections.Generic;

namespace SlugBase.Characters
{
    internal static class CharacterManager
    {
        public static readonly List<SlugBaseCharacter> characters = new();

        public static void Scan()
        {
            var folders = AssetManager.ListDirectory("slugbase", true);

            // TODO: Load slugcat JSON files
        }
    }
}
