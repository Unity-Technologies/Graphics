using UnityEngine;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    namespace Attributes
    {
        // 0 is reserved!

        [GenerateHLSL]
        public enum DebugViewVarying
        {
            Texcoord0 = 1,
            Texcoord1,
            Texcoord2,
            Texcoord3,
            VertexTangentWS,
            VertexBitangentWS,
            VertexNormalWS,
            VertexColor,
            VertexColorAlpha,
            // caution if you add something here, it must start below
        };

        // Number must be contiguous
        [GenerateHLSL]
        public enum DebugViewGbuffer
        {
            Depth = DebugViewVarying.VertexColorAlpha + 1,
            BakeDiffuseLighting,
        }
    }
} // namespace UnityEngine.Experimental.ScriptableRenderLoop
