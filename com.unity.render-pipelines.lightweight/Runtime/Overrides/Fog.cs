using System;

namespace UnityEngine.Rendering.LWRP
{
    public enum FogType
    {
        Linear,
        Exp2,
    }

    [Serializable, VolumeComponentMenu("Environment/Fog")]
    public sealed class Fog : VolumeComponent
    {
        [Tooltip("The type of fog to use.")] 
        public FogTypeParameter type = new FogTypeParameter(FogType.Linear);
        
        public ColorParameter fogColor = new ColorParameter(Color.white, true, false, true);
        
        public FloatParameter density = new FloatParameter(0.025f);
        
    }

    [Serializable]
    public sealed class FogTypeParameter : VolumeParameter<FogType> { public FogTypeParameter(FogType value, bool overrideState = false) : base(value, overrideState) { } }
}
