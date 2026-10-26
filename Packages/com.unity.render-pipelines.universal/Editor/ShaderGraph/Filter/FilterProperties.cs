using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // System-bound properties for a filter pass. _MainTex is fed by UIRFilterHelper.ApplyFilterPass at
    // draw time, never by the graph author (hidden = true). Reuses Canvas's own _MainTex rather than
    // declare a second copy that could silently drift.
    static class FilterProperties
    {
        public static readonly Texture2DShaderProperty MainTex = CanvasProperties.MainTex;
    }
}
