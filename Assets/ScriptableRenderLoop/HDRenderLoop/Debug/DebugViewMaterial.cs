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
        };

        // Number must be contiguous
        [GenerateHLSL]
        public enum DebugViewGbuffer
        {
            Depth = DebugViewVarying.VertexColor + 1,
            BakeDiffuseLighting,
        }
    }
} // namespace UnityEngine.Experimental.ScriptableRenderLoop
