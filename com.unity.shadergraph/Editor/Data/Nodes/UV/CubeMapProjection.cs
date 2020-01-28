using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using System;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.CubeMapProjection")]
    [Title("Input", "Texture", "CubeMapProjection")]
    class CubeMapProjectionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition
    {
        public const int    OutputSlotId            = 0;
        public const int    CubemapInputId          = 1;
        public const int    WorldPosInputId         = 2;
        public const int    OriginInputId           = 3;
        public const int    ProjDirInputId          = 4;
        public const int    ProjIntensityInputId    = 5;
        public const int    SamplerInputId          = 6;
        public const int    LODInputId              = 7;
               const string kOutputSlotName         = "Out";
               const string kCubemapInputName       = "Cubemap";
               const string kWorldPosInputName      = "WorldPosition";
               const string kOriginInputName        = "Origin";
               const string kProjDirInputName       = "Projection Direction";
               const string kProjIntensityInputName = "Projection Intensity";
               const string kSamplerInputName       = "Sampler";
               const string kLODInputName           = "LOD";

        public CubeMapProjectionNode()
        {
            name = "CubeMap Projection";
            UpdateNodeAfterDeserialization();
        }

        string GetFunctionName()
        {
            return $"Unity_GetCubeMapProjection";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new CubemapInputMaterialSlot(CubemapInputId, kCubemapInputName, kCubemapInputName));
            AddSlot(new PositionMaterialSlot(WorldPosInputId, kWorldPosInputName, kWorldPosInputName, CoordinateSpace.AbsoluteWorld));
            AddSlot(new Vector3MaterialSlot(OriginInputId, kOriginInputName, kOriginInputName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(ProjDirInputId, kProjDirInputName, kProjDirInputName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(ProjIntensityInputId, kProjIntensityInputName, kProjIntensityInputName, SlotType.Input, Vector3.zero));
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(LODInputId, kLODInputName, kLODInputName, SlotType.Input, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, CubemapInputId, WorldPosInputId, OriginInputId, ProjDirInputId, ProjIntensityInputId, SamplerInputId, LODInputId });
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.AbsoluteWorld;
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("$precision3 Unity_GetCubeMapProjection($precision3 worldPos, $precision3 origin, $precision3 projDir, $precision projIntensity)");
                    using (s.BlockScope())
                    {
                        s.AppendLines(@"
$precision3 delta = worldPos - origin;
delta -= projIntensity*projDir;

return delta;");
                    }
                });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            //Sampler input slot

            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInputId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);
            var id = GetSlotValue(CubemapInputId, generationMode);
            string result = string.Format("" +
                "$precision4 {0} = SAMPLE_TEXTURECUBE_LOD({1}, {2}, Unity_GetCubeMapProjection({3}, {4}, {5}, {6}), {7});"
                    , GetVariableNameForSlot(OutputSlotId)
                    , id
                    , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id
                    , GetSlotValue(WorldPosInputId, generationMode)
                    , GetSlotValue(OriginInputId, generationMode)
                    , GetSlotValue(ProjDirInputId, generationMode)
                    , GetSlotValue(ProjIntensityInputId, generationMode)
                    , GetSlotValue(LODInputId, generationMode));

            sb.AppendLine(result);
        }
    }
}
