using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class Decal
    {
        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 10000)]
        public struct DecalSurfaceData
        {
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector4 baseColor;
            [SurfaceDataAttributes("Normal", true)]
            public Vector4 normalWS;
        };

        [GenerateHLSL(PackingRules.Exact)]
        public enum DBufferMaterial
        {
            // Note: This count doesn't include the velocity buffer. On shader and csharp side the velocity buffer will be added by the framework
            Count = 2
        };
    }
}
