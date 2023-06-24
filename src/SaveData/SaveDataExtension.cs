namespace SlugBase.SaveData
{
    /// <summary>
    /// Extensions to generate the <see cref="SlugBaseSaveData"/> helper from the game's save data.
    /// </summary>
    public static class SaveDataExtension
    {
        /// <summary>
        /// Gets a <see cref="SlugBaseSaveData"/> from the game's <see cref="DeathPersistentSaveData"/>.
        /// </summary>
        /// <param name="data">The <see cref="DeathPersistentSaveData"/> instance.</param>
        /// <returns>A <see cref="SlugBaseSaveData"/> bound to the <see cref="DeathPersistentSaveData"/>.</returns>
        public static SlugBaseSaveData GetSlugBaseData(this DeathPersistentSaveData data)
        {
            return SlugBaseSaveData.DeathPersistentData.GetValue(data, dpsd => new(dpsd.unrecognizedSaveStrings));
        }

        /// <summary>
        /// Gets a <see cref="SlugBaseSaveData"/> from the game's <see cref="MiscWorldSaveData"/>.
        /// </summary>
        /// <param name="data">The <see cref="MiscWorldSaveData"/> instance.</param>
        /// <returns>A <see cref="SlugBaseSaveData"/> bound to the <see cref="MiscWorldSaveData"/> instance.</returns>
        public static SlugBaseSaveData GetSlugBaseData(this MiscWorldSaveData data)
        {
            return SlugBaseSaveData.WorldData.GetValue(data, mwsd => new(mwsd.unrecognizedSaveStrings));
        }

        /// <summary>
        /// Gets a <see cref="SlugBaseSaveData"/> from the game's <see cref="PlayerProgression.MiscProgressionData"/>.
        /// </summary>
        /// <param name="data">The <see cref="PlayerProgression.MiscProgressionData"/> instance.</param>
        /// <returns>A <see cref="SlugBaseSaveData"/> bound to the <see cref="PlayerProgression.MiscProgressionData"/> instance.</returns>
        public static SlugBaseSaveData GetSlugBaseData(this PlayerProgression.MiscProgressionData data)
        {
            return SlugBaseSaveData.ProgressionData.GetValue(data, mpd => new(mpd.unrecognizedSaveStrings));
        }
    }
}