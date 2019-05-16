namespace UnityEngine.Experimental.Rendering
{
    public class CrossSectionDefinitions
    {
        // todo: need to manage HLSL dependencies accross shadergraph, HDRP and others
        // [GenerateHLSL(PackingRules.Exact)]
        public enum CutMainStyle
        {
            Transparent = 0,
            Hollow,
            HollowCustom,
            Filled,
        }
        public class ShaderPropertyIds
        {
            public static readonly int ClipPlanePosition = Shader.PropertyToID("_UnityAEC_ClipPlanePosition");
            public static readonly int ClipPlaneNormal = Shader.PropertyToID("_UnityAEC_ClipPlaneNormal");
            public static readonly int ClipPlaneTangent = Shader.PropertyToID("_UnityAEC_ClipPlaneTangent");
            public static readonly int ClipPlaneBitangent = Shader.PropertyToID("_UnityAEC_ClipPlaneBitangent");
            public static readonly int CutMainStyle = Shader.PropertyToID("_UnityAEC_CutMainStyle");
        }
    }
}
