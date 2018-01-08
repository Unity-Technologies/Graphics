using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum MetalMaterial
    {
        Iron,
        Silver,
        Aluminium,
        Gold,
        Copper,
        Chromium,
        Nickel,
        Titanium,
        Cobalt,
        Platinum
    };

    [Title("Input", "PBR", "Metal Reflectance")]
    public class MetalReflectanceNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public MetalReflectanceNode()
        {
            name = "Metal Reflectance";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private MetalMaterial m_Material = MetalMaterial.Iron;

        [EnumControl("Material")]
        public MetalMaterial material
        {
            get { return m_Material; }
            set
            {
                if (m_Material == value)
                    return;

                m_Material = value;
                Dirty(ModificationScope.Graph);
            }
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            switch (m_Material)
            {
                case MetalMaterial.Silver:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.972, 0.960, 0.915);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case MetalMaterial.Aluminium:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.913, 0.921, 0.925);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case MetalMaterial.Gold:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(1.000, 0.766, 0.336);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case MetalMaterial.Copper:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.955, 0.637, 0.538);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case MetalMaterial.Chromium:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.550, 0.556, 0.554);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case MetalMaterial.Nickel:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.660, 0.609, 0.526);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case MetalMaterial.Titanium:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.542, 0.497, 0.449);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case MetalMaterial.Cobalt:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.662, 0.655, 0.634);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                case MetalMaterial.Platinum:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.672, 0.637, 0.585);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
                default:
                    visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3(0.560, 0.570, 0.580);", precision, GetVariableNameForSlot(kOutputSlotId)), true);
                    break;
            }
        }
    }
}
