using System.Collections.Generic;
using ObjType = AbstractPhysicalObject.AbstractObjectType;
using CritType = CreatureTemplate.Type;
using Name = SlugcatStats.Name;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;

namespace SlugBase.DataTypes
{
    /// <summary>
    /// Represents the nourishment and edibility of foods for a <see cref="SlugBaseCharacter"/>.
    /// </summary>
    public class Diet
    {
        /// <summary>
        /// The food value multiplier of dead creatures.
        /// <para>If the creature is consumed in its entirety, such as <see cref="CritType.Fly"/>, <see cref="Meat"/> is used instead.</para>
        /// </summary>
        public float Corpses { get; set; }

        /// <summary>
        /// The food value multiplier of small creatures or living objects like <see cref="ObjType.JellyFish"/>.
        /// </summary>
        public float Meat { get; set; }

        /// <summary>
        /// The food value multiplier of non-living foods.
        /// </summary>
        public float Plants { get; set; }

        /// <summary>
        /// The food value multipliers of individual object types.
        /// </summary>
        public Dictionary<ObjType, float> ObjectOverrides { get; } = new();

        /// <summary>
        /// The food value multipliers of individual creature types.
        /// </summary>
        public Dictionary<CritType, float> CreatureOverrides { get; } = new();

        /// <summary>
        /// Creates a new <see cref="Diet"/> from JSON.
        /// </summary>
        /// <param name="json">The JSON to load.</param>
        public Diet(JsonAny json)
        {
            Name baseName = null;
            float? corpses = null;
            float? meat = null;
            float? plants = null;

            if(json.TryString() is string str)
            {
                baseName = Utils.GetName(str);

                if (baseName != null)
                {
                    if (!SetBase(baseName))
                        throw new JsonException($"Couldn't find character \"{baseName.value}\" to inherit diet from!");
                }
            }
            else
            {
                var diet = json.AsObject();

                if (diet.TryGet("base")?.AsString() is string baseStr)
                    baseName = Utils.GetName(baseStr);

                if (baseName != null)
                {
                    if (!SetBase(baseName))
                        throw new JsonException($"Couldn't find character \"{baseName.value}\" to inherit diet from!");
                }

                corpses ??= diet.TryGet("corpses")?.AsFloat();
                meat ??= diet.TryGet("meat")?.AsFloat();
                plants ??= diet.TryGet("plants")?.AsFloat();

                if(diet.TryGet("overrides")?.AsObject() is JsonObject overrides)
                {
                    foreach(var pair in overrides)
                    {
                        var objType = new ObjType(pair.Key);
                        var critType = new CritType(pair.Key);

                        if((int)objType != -1)
                        {
                            ObjectOverrides[objType] = pair.Value.AsFloat();
                        }

                        if((int)critType != -1)
                        {
                            CreatureOverrides[critType] = pair.Value.AsFloat();
                        }
                    }
                }
            }

            // Throw if any fields are left unspecified
            if (baseName != null)
            {
                if (corpses == null) throw new JsonException("Missing \"corpses\" or \"base\" diet property!");
                if (meat == null) throw new JsonException("Missing \"meat\" or \"base\" diet property!");
                if (plants == null) throw new JsonException("Missing \"plants\" or \"base\" diet property!");
            }

            if (corpses.HasValue)
                Corpses = corpses.Value;

            if (meat.HasValue)
                Meat = meat.Value;

            if (plants.HasValue)
                Plants = plants.Value;
        }

        /// <summary>
        /// Gets the food value multiplier for an object that was consumed in its entirety.
        /// </summary>
        /// <param name="obj">The object that was eaten.</param>
        /// <returns>A multiplier for the nourishment of this object.</returns>
        public float GetFoodMultiplier(PhysicalObject obj)
        {
            if (obj is Creature crit && CreatureOverrides.TryGetValue(crit.Template.type, out var mul))
                return mul;
            
            if (ObjectOverrides.TryGetValue(obj.abstractPhysicalObject.type, out mul))
                return mul;
            
            return IsObjectMeat(obj.abstractPhysicalObject.type) ? Meat : Plants;
        }

        /// <summary>
        /// Gets the food value multiplier for a creature
        /// </summary>
        /// <param name="player">The player to pass to <see cref="Player.EatMeatOmnivoreGreenList(Creature)"/>.</param>
        /// <param name="crit">The creature that is being eaten.</param>
        /// <returns>A multiplier for the nourishment of this creature.</returns>
        public float GetMeatMultiplier(Player player, Creature crit)
        {
            if (CreatureOverrides.TryGetValue(crit.Template.type, out var mul))
                return mul;
            else
                return (player != null && player.EatMeatOmnivoreGreenList(crit)) ? Meat : Corpses;
        }

        private static bool IsObjectMeat(ObjType type)
        {
            return type == ObjType.Creature || type == ObjType.EggBugEgg || type == ObjType.JellyFish;
        }

        private bool SetBase(Name name)
        {
            if (name == null)
                return false;

            switch(name.value)
            {
                case nameof(Name.White):
                case nameof(Name.Yellow):
                case nameof(MSCName.Rivulet):
                    Corpses = -1f;
                    Meat = 1f;
                    Plants = 1f;
                    return true;

                case nameof(Name.Red):
                case nameof(MSCName.Artificer):
                    Corpses = 1f;
                    Meat = 1f;
                    Plants = 0.25f;
                    CreatureOverrides[CritType.Fly] = 0.25f;
                    return true;

                case nameof(MSCName.Gourmand):
                case nameof(MSCName.Sofanthiel):
                    Corpses = 0.5f;
                    Meat = 1f;
                    Plants = 1f;
                    return true;

                case nameof(MSCName.Spear):
                    Corpses = -1f;
                    Meat = -1f;
                    Plants = -1f;
                    return true;

                case nameof(MSCName.Saint):
                    Corpses = -1f;
                    Meat = -1f;
                    Plants = 1f;
                    return true;
            }

            return false;
        }
    }
}
