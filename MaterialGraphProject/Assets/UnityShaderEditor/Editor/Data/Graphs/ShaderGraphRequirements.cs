using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
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

        public static ShaderGraphRequirements none
        {
            get
            {
                return new ShaderGraphRequirements
                {
                    requiresMeshUVs = new List<UVChannel>()
                };
            }
        }

        public bool NeedsTangentSpace()
        {
            var compoundSpaces = requiresBitangent | requiresNormal | requiresPosition
                | requiresTangent | requiresViewDir | requiresPosition
                | requiresNormal;

            return (compoundSpaces & NeededCoordinateSpace.Tangent) > 0;
        }

        public ShaderGraphRequirements Union(ShaderGraphRequirements other)
        {
            var newReqs = new ShaderGraphRequirements();
            newReqs.requiresNormal = other.requiresNormal | requiresNormal;
            newReqs.requiresTangent = other.requiresTangent | requiresTangent;
            newReqs.requiresBitangent = other.requiresBitangent | requiresBitangent;
            newReqs.requiresViewDir = other.requiresViewDir | requiresViewDir;
            newReqs.requiresPosition = other.requiresPosition | requiresPosition;
            newReqs.requiresScreenPosition = other.requiresScreenPosition | requiresScreenPosition;
            newReqs.requiresVertexColor = other.requiresVertexColor | requiresVertexColor;

            newReqs.requiresMeshUVs = new List<UVChannel>();
            if (requiresMeshUVs != null)
                newReqs.requiresMeshUVs.AddRange(requiresMeshUVs);
            if (other.requiresMeshUVs != null)
                newReqs.requiresMeshUVs.AddRange(other.requiresMeshUVs);
            return newReqs;
        }
    }
}
