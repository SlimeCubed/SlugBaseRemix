using System.Collections.Generic;
using System;
using System.Text;
using Newtonsoft.Json;

namespace SlugBase.SaveData
{
    internal static class SaveDataHooks
    {
        public static void Apply()
        {
            On.DeathPersistentSaveData.ctor += DeathPersistentSaveData_ctor;
            On.MiscWorldSaveData.ctor += MiscWorldSaveData_ctor;
            On.PlayerProgression.MiscProgressionData.ctor += MiscProgressionData_ctor;
            
            On.DeathPersistentSaveData.ToString += DeathPersistentSaveData_ToString;
            On.MiscWorldSaveData.ToString += MiscWorldSaveData_ToString;
            On.PlayerProgression.MiscProgressionData.ToString += MiscProgressionData_ToString;
        }

        private static string MiscProgressionData_ToString(On.PlayerProgression.MiscProgressionData.orig_ToString orig, PlayerProgression.MiscProgressionData self)
        {
            SaveObjectsToUnrecognizedStrings(self, self.unrecognizedSaveStrings);
            return orig(self);
        }

        private static string MiscWorldSaveData_ToString(On.MiscWorldSaveData.orig_ToString orig, MiscWorldSaveData self)
        {
            SaveObjectsToUnrecognizedStrings(self, self.unrecognizedSaveStrings);
            return orig(self);
        }

        private static string DeathPersistentSaveData_ToString(On.DeathPersistentSaveData.orig_ToString orig, DeathPersistentSaveData self)
        {
            SaveObjectsToUnrecognizedStrings(self, self.unrecognizedSaveStrings);
            return orig(self);
        }

        private static void MiscProgressionData_ctor(On.PlayerProgression.MiscProgressionData.orig_ctor orig, PlayerProgression.MiscProgressionData self, PlayerProgression owner)
        {
            orig(self, owner);
            SlugBaseSaveData.SavedObjects.Add(self, new());
        }

        private static void MiscWorldSaveData_ctor(On.MiscWorldSaveData.orig_ctor orig, MiscWorldSaveData self, SlugcatStats.Name savestatenumber)
        {
            orig(self, savestatenumber);
            SlugBaseSaveData.SavedObjects.Add(self, new());
        }

        private static void DeathPersistentSaveData_ctor(On.DeathPersistentSaveData.orig_ctor orig, DeathPersistentSaveData self, SlugcatStats.Name slugcat)
        {
            orig(self, slugcat);
            SlugBaseSaveData.SavedObjects.Add(self, new());
        }

        private static void SaveObjectsToUnrecognizedStrings(object owner, List<string> strings)
        {
            if (!SlugBaseSaveData.SavedObjects.TryGetValue(owner, out var objects))
            {
                return;
            }

            foreach (var kvp in objects)
            {
                SaveStringToUnrecognizedStrings(strings, kvp.Key, JsonConvert.SerializeObject(kvp.Value));
            }
        }
        
        private static void SaveStringToUnrecognizedStrings(List<string> strings, string key, string value)
        {
            var prefix = key + SlugBaseSaveData.SAVE_DATA_PREFIX;
            var dataToStore = prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

            for (var i = 0; i < strings.Count; i++)
            {
                if (strings[i].StartsWith(prefix))
                {
                    strings[i] = dataToStore;
                    return;
                }
            }

            strings.Add(dataToStore);
        }
    }
}