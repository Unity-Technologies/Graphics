using System;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public abstract class SkySettings : ScriptableObject
    {
        [Range(0,360)]
        public float                    rotation = 0.0f;
        public float                    exposure = 0.0f;
        public float                    multiplier = 1.0f;
        public SkyResolution            resolution = SkyResolution.SkyResolution256;
        public EnvironementUpdateMode   updateMode = EnvironementUpdateMode.OnChanged;
        public float                    updatePeriod = 0.0f;
        public Cubemap                  lightingOverride = null;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = hash * 23 + rotation.GetHashCode();
                hash = hash * 23 + exposure.GetHashCode();
                hash = hash * 23 + multiplier.GetHashCode();
                hash = hash * 23 + resolution.GetHashCode();
                hash = hash * 23 + updateMode.GetHashCode();
                hash = hash * 23 + updatePeriod.GetHashCode();
                hash = lightingOverride != null ? hash * 23 + rotation.GetHashCode() : hash;
                return hash;
            }
        }

        public abstract SkyRenderer GetRenderer();
    }
}
