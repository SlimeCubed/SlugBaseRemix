using System.Net.Sockets;

namespace SlugBase.DataTypes
{
    /// <summary>
    /// Represents the initial reputation of the player with a community.
    /// </summary>
    public struct RepOverride
    {
        /// <summary>
        /// The target like value of the player.
        /// </summary>
        public float Target;

        /// <summary>
        /// The amount to lerp reputation towards <see cref="Target"/> when loaded.
        /// </summary>
        public float Strength;
        
        /// <summary>
        /// If <c>true</c>, the like of the player will be locked to <see cref="Target"/> after it is set.
        /// </summary>
        public bool Locked;

        /// <summary>
        /// Creates a new <see cref="RepOverride"/>.
        /// </summary>
        /// <param name="target">The target like value of the player.</param>
        /// <param name="strength">The amount to lerp reputation towards <paramref name="target"/> when loaded.</param>
        /// <param name="locked">If <c>true</c>, the like of the player will be locked to <paramref name="target"/> after it is set.</param>
        public RepOverride(float target, float strength = 1f, bool locked = false)
        {
            Target = target;
            Strength = strength;
            Locked = locked;
        }

        /// <summary>
        /// Creates a new <see cref="RepOverride"/> from JSON.
        /// </summary>
        /// <param name="json">The JSON to load.</param>
        public RepOverride(JsonAny json)
        {
            if (json.TryFloat() != null)
            {
                Target = json.AsFloat();
                Strength = 1f;
                Locked = false;
            }
            else
            {
                var obj = json.AsObject();
                Target = obj.GetFloat("like");
                Strength = obj.TryGet("strength")?.AsFloat() ?? 1f;
                Locked = obj.TryGet("locked")?.AsBool() ?? false;
            }
        }
    }
}
