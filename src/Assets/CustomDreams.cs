using System;
using SceneID = Menu.MenuScene.SceneID;
using DreamID = DreamsState.DreamID;
using System.Collections.Generic;

namespace SlugBase.Assets
{
    /// <summary>
    /// Helpers for adding custom dreams.
    /// </summary>
    public static class CustomDreams
    {
        private static readonly Dictionary<DreamID, SceneID> _dreamScenes = new();

        /// <summary>
        /// Registers a new dream and its associated scene.
        /// </summary>
        /// <param name="dream">The dream ID to add.</param>
        /// <param name="scene">The scene ID to display when the dream occurs.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dream"/> is <see langword="null"/>.</exception>
        public static void SetDreamScene(DreamID dream, SceneID scene)
        {
            if (dream == null) throw new ArgumentNullException(nameof(dream));

            if (scene == null)
                _dreamScenes.Remove(dream);
            else
                _dreamScenes[dream] = scene;
        }

        /// <summary>
        /// Set the dream scene that will display when the player hibernates next.
        /// </summary>
        /// <param name="storySession">The current story game session.</param>
        /// <param name="dreamID">The id of the dream to queue.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dreamID"/> or <paramref name="storySession"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="dreamID"/> wasn't registered with <see cref="SetDreamScene(DreamID, SceneID)"/>.</exception>
        public static void QueueDream(StoryGameSession storySession, DreamID dreamID)
        {
            if (!_dreamScenes.ContainsKey(dreamID)) throw new ArgumentException("dreamID must be registered with RegisterDream before use!", nameof(dreamID));
            if (storySession == null) throw new ArgumentNullException(nameof(storySession));
            if (dreamID == null) throw new ArgumentNullException(nameof(dreamID));

            var dreamState = storySession.saveState?.dreamsState;
            if (dreamState == null) throw new ArgumentException("The current save state doesn't have a dream state!", nameof(storySession));

            dreamState.InitiateEventDream(dreamID);
        }

        internal static void Apply()
        {
            On.DreamsState.InitiateEventDream += DreamsState_InitiateEventDream;
            On.Menu.DreamScreen.SceneFromDream += DreamScreen_SceneFromDream;
        }

        // Stop non-SlugBase scenes from overriding dreams with the DreamOverride set
        private static void DreamsState_InitiateEventDream(On.DreamsState.orig_InitiateEventDream orig, DreamsState self, DreamID evDreamID)
        {
            bool curIsOverride = self.eventDream != null
                && _dreamScenes.TryGetValue(self.eventDream, out var sceneID)
                && CustomScene.Registry.TryGet(sceneID, out var customScene)
                && customScene.OverrideDream;

            bool newIsOverride = evDreamID != null
                && _dreamScenes.TryGetValue(evDreamID, out sceneID)
                && CustomScene.Registry.TryGet(sceneID, out customScene)
                && customScene.OverrideDream;

            if (!curIsOverride || newIsOverride)
            {
                orig(self, evDreamID);
            }
        }

        // Return registered scenes from SceneFromDream
        private static SceneID DreamScreen_SceneFromDream(On.Menu.DreamScreen.orig_SceneFromDream orig, Menu.DreamScreen self, DreamID dreamID)
        {
            return _dreamScenes.TryGetValue(dreamID, out var sceneID) ? sceneID : orig(self, dreamID);
        }
    }
}
