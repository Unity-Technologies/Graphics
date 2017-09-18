using System.Collections.Generic;

namespace UnityEngine.MaterialGraph
{
    public struct ShaderGraphRequirements
    {
        public NeededCoordinateSpace requiresNormal;
        public NeededCoordinateSpace requiresBitangent;
        public NeededCoordinateSpace requiresTangent;
        public NeededCoordinateSpace requiresViewDir;
        public NeededCoordinateSpace requiresPosition;
        public bool requiresScreenPosition;
        public bool requiresVertexColor;
        public List<UVChannel> requiresMeshUVs;

        public bool NeedsTangentSpace()
        {
            var compoundSpaces = requiresBitangent | requiresNormal | requiresPosition
                                 | requiresTangent | requiresViewDir | requiresPosition
                                 | requiresNormal;

            return (compoundSpaces & NeededCoordinateSpace.Tangent) > 0;
        }
    }
}
