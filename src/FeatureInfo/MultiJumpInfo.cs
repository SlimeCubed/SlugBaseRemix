using System.Collections.Generic;
using UnityEngine;

namespace SlugBase.FeatureInfo
{
    public class MultiJumpInfo
    {
        public int MaxJumps { get; }
        public int Delay { get; }

        public int JumpsLeft { get; set; }
        public int DelayTimer { get; set; }

        public MultiJumpInfo(object json)
        {
            var obj = (Dictionary<string, object>)json;

            MaxJumps = obj.GetInt("jumps");
            Delay = Mathf.CeilToInt(obj.GetFloat("delay") * 40f);

            JumpsLeft = MaxJumps;
        }
    }
}
