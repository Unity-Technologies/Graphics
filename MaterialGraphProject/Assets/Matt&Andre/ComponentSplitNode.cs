using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Channels/Component Split")]
    public class ComponentSplitNode : PropertyNode, IGeneratesBodyCode
    {
        protected const string kInputSlotName = "Input";
        protected const string kOutputSlotRName = "R";
        protected const string kOutputSlotGName = "G";
        protected const string kOutputSlotBName = "B";
        protected const string kOutputSlotAName = "A";
        protected const string kOutputSlotRGBASlotName = "Output";

        public const int InputSlotId = 0;
        public const int OutputSlotRId = 1;
        public const int OutputSlotGId = 2;
        public const int OutputSlotBId = 3;
        public const int OutputSlotAId = 4;
        public const int OutputSlotRGBAId = 5;
        
        public ComponentSplitNode()
        {
            name = "ComponentSplit";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotRGBAId, kOutputSlotRGBASlotName, kOutputSlotRGBASlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { InputSlotId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, OutputSlotRGBAId }; }
        }

        [SerializeField]
        private Vector4 m_Value;

        public Vector4 value
        {
            get { return m_Value; }
            set
            {
                if (m_Value == value)
                    return;

                m_Value = value;

                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        public override PropertyType propertyType { get { return PropertyType.Vector4; } }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == ExposedState.Exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, PropertyChunk.HideState.Visible));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
                visitor.AddShaderChunk(precision + "4 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
              //  return;
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            visitor.AddShaderChunk(precision + "4 " + propertyName + " = " + inputValue + ";", false);
            //visitor.AddShaderChunk(precision + "4 " + propertyName + " = " + precision + "4 (" + m_Value.x + ", " + m_Value.y + ", " + m_Value.z + ", " + m_Value.w + ");", true);
        }

        protected virtual MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual string GetInputSlotName() { return "Input"; }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Vector4,
                m_Vector4 = m_Value
            };
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            string slotOutput;
            switch (slotId)
            {
                case OutputSlotRId:
                    slotOutput = ".r";
                    break;
                case OutputSlotGId:
                    slotOutput = ".g";
                    break;
                case OutputSlotBId:
                    slotOutput = ".b";
                    break;
                case OutputSlotAId:
                    slotOutput = ".a";
                    break;
                default:
                    slotOutput = "";
                    break;
            }
            return propertyName + slotOutput;
            //return GetVariableNameForNode() + slotOutput;
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(GetPreviewProperty());
        }

        /* TEXTURE
        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var uvSlot = FindInputSlot<MaterialSlot>(UvSlotId);
            if (uvSlot == null)
                return;

            var uvName = string.Format("{0}.xy", UVChannel.uv0.GetUVName());
            var edges = owner.GetEdges(uvSlot.slotReference).ToList();

            if (edges.Count > 0)
            {
                var edge = edges[0];
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                uvName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.Vector2, true);
            }

            string body = "tex2D (" + propertyName + ", " + uvName + ")";
            if (m_TextureType == TextureType.Bump)
                body = precision + "4(UnpackNormal(" + body + "), 0)";
            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForNode() + " = " + body + ";", true);
        }

        // Properties
        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(
                new TexturePropertyChunk(
                    propertyName,
                    description,
                    defaultTexture, m_TextureType,
                    PropertyChunk.HideState.Visible,
                    exposedState == ExposedState.Exposed ?
                        TexturePropertyChunk.ModifiableState.Modifiable
                        : TexturePropertyChunk.ModifiableState.NonModifiable));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("sampler2D " + propertyName + ";", true);
        }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Texture2D,
                m_Texture = defaultTexture
            };
        }

        public override PropertyType propertyType { get { return PropertyType.Texture2D; } }

        public bool RequiresMeshUV(UVChannel channel)
        {
            if (channel != UVChannel.uv0)
            {
                return false;
            }

            var uvSlot = FindInputSlot<MaterialSlot>(UvSlotId);
            if (uvSlot == null)
                return true;

            var edges = owner.GetEdges(uvSlot.slotReference).ToList();
            return edges.Count == 0;
        }*/
    }
}
