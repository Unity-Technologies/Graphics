using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    [Serializable]
    internal enum IncludeLocation
    {
        Pregraph,
        Graph,
        Postgraph
    }
}
