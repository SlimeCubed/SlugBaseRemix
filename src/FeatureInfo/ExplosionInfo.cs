using System.Collections.Generic;

namespace SlugBase.FeatureInfo
{
    public class ExplosionInfo
    {
        public float Radius { get; }
        public float Damage { get; }

        public ExplosionInfo(object json)
        {
            var obj = (Dictionary<string, object>)json;

            Radius = obj.GetFloat("radius");
            Damage = obj.GetFloat("damage");
        }
    }
}
