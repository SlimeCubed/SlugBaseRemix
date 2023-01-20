using System.IO;

namespace SlugBase
{
    /// <summary>
    /// Allows a <see cref="Feature{TOwner}"/> to save information across sessions.
    /// </summary>
    public interface ISavedFeature
    {
        /// <summary>
        /// Save a binary representation of this feature's data.
        /// </summary>
        /// <param name="writer"></param>
        public void Save(BinaryWriter writer);

        /// <summary>
        /// Load a binary representation of this feature's data.
        /// </summary>
        /// <param name="reader"></param>
        public void Load(BinaryReader reader);

        /// <summary>
        /// Indicates how this <see cref="ISavedFeature"/> should be saved and rolled back.
        /// </summary>
        public SaveType SaveMode { get; }

        /// <summary>
        /// Indicates how an <see cref="ISavedFeature"/> should be saved and rolled back.
        /// </summary>
        public enum SaveType
        {
            /// <summary>
            /// The feature is saved each time a cycle is survived and rolls back on death.
            /// </summary>
            Normal,

            /// <summary>
            /// The feature is saved each time the cycle ends.
            /// </summary>
            DeathPersist
        }
    }
}
