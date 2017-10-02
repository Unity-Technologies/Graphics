using System;
using System.Linq;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class AbstractLightweightPBRMasterNode : AbstractLightweightMasterNode
    {
        public const string AlbedoSlotName = "Albedo";
        public const string NormalSlotName = "Normal";
        public const string EmissionSlotName = "Emission";
        public const string SmoothnessSlotName = "Smoothness";
        public const string OcclusionSlotName = "Occlusion";
        public const string AlphaSlotName = "Alpha";
        public const string VertexOffsetName = "VertexPosition";

        public const int AlbedoSlotId = 0;
        public const int NormalSlotId = 1;
        public const int EmissionSlotId = 3;
        public const int SmoothnessSlotId = 4;
        public const int OcclusionSlotId = 5;
        public const int AlphaSlotId = 6;

        protected override void GetLightweightDefinesAndRemap(ShaderGenerator defines, ShaderGenerator surfaceOutputRemap, MasterRemapGraph remapper)
        {
            base.GetLightweightDefinesAndRemap(defines, surfaceOutputRemap, remapper);

            defines.AddShaderChunk("#define _GLOSSYREFLECTIONS_ON", true);
            defines.AddShaderChunk("#define _SPECULARHIGHLIGHTS_ON", true);

            if(IsNormalMapConnected())
                defines.AddShaderChunk("#define _NORMALMAP 1", true);
        }

        protected override int GetInterpolatorStartIndex()
        {
            return 2;
        }

        public override ShaderGraphRequirements GetNodeSpecificRequirements()
        {
            var reqs = ShaderGraphRequirements.none;
            reqs.requiresNormal |= NeededCoordinateSpace.World;
            reqs.requiresTangent |= NeededCoordinateSpace.World;
            reqs.requiresBitangent |= NeededCoordinateSpace.World;
            reqs.requiresPosition |= NeededCoordinateSpace.World;
            reqs.requiresViewDir |= NeededCoordinateSpace.World;
            return base.GetNodeSpecificRequirements().Union(reqs);
        }

        bool IsNormalMapConnected()
        {
            var nm = FindSlot<MaterialSlot>(NormalSlotId);
            return owner.GetEdges(nm.slotReference).Any();
        }
    }
}
