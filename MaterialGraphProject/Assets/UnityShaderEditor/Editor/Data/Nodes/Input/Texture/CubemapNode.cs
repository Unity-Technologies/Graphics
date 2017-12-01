using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Input/Texture/Cubemap")]
    public class CubemapNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireViewDirection, IMayRequireNormal
    {
        public const int OutputSlotId = 0;
        public const int CubemapInputId = 1;
        public const int ViewDirInputId = 2;
        public const int NormalInputId = 3;
        public const int LODInputId = 4;

        const string kOutputSlotName = "Out";
        const string kCubemapInputName = "Cube";
        const string kViewDirInputName = "ViewDir";
        const string kNormalInputName = "Normal";
        const string kLODInputName = "LOD";

        public override bool hasPreview { get { return true; } }

        public CubemapNode()
        {
            name = "Cubemap";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new CubemapInputMaterialSlot(CubemapInputId, kCubemapInputName, kCubemapInputName));
            AddSlot(new ViewDirectionMaterialSlot(ViewDirInputId, kViewDirInputName, kViewDirInputName, CoordinateSpace.Object));
            AddSlot(new NormalMaterialSlot(NormalInputId, kNormalInputName, kNormalInputName, CoordinateSpace.Object));
            AddSlot(new Vector1MaterialSlot(LODInputId, kLODInputName, kLODInputName, SlotType.Input, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, CubemapInputId, ViewDirInputId, NormalInputId, LODInputId });
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string result = string.Format("{0}4 {1} = texCUBElod ({2}, {0}4(reflect(-{3}, {4}), {5}));"
                    , precision
                    , GetVariableNameForSlot(OutputSlotId)
                    , GetSlotValue(CubemapInputId, generationMode)
                    , GetSlotValue(ViewDirInputId, generationMode)
                    , GetSlotValue(NormalInputId, generationMode)
                    , GetSlotValue(LODInputId, generationMode));

            visitor.AddShaderChunk(result, true);
        }

        public NeededCoordinateSpace RequiresViewDirection()
        {
            var viewDirSlot = FindInputSlot<MaterialSlot>(ViewDirInputId);
            var edgesViewDir = owner.GetEdges(viewDirSlot.slotReference);
            if (!edgesViewDir.Any())
                return CoordinateSpace.Object.ToNeededCoordinateSpace();
            else
                return NeededCoordinateSpace.None;
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            var normalSlot = FindInputSlot<MaterialSlot>(NormalInputId);
            var edgesNormal = owner.GetEdges(normalSlot.slotReference);
            if (!edgesNormal.Any())
                return CoordinateSpace.Object.ToNeededCoordinateSpace();
            else
                return NeededCoordinateSpace.None;
        }
    }
}
