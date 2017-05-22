using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Depth Texture")]
    public class DepthTextureNode : PropertyNode, IGeneratesBodyCode
    {
        protected const string kUVSlotName = "UV";
        protected const string kOutputSlotName = "Output";

        public const int UvSlotId = 0;
        public const int OutputSlotId = 1;

        public override bool hasPreview { get { return true; } }

        /*[SerializeField]
        private string m_SerializedTexture;

        [SerializeField]
        private TextureType m_TextureType;

        [Serializable]
        private class TextureHelper
        {
            public Texture2D texture;
        }

#if UNITY_EDITOR
        public Texture2D defaultTexture
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedTexture))
                    return null;

                var tex = new TextureHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, tex);
                return tex.texture;
            }
            set
            {
                if (defaultTexture == value)
                    return;

                var tex = new TextureHelper();
                tex.texture = value;
                m_SerializedTexture = EditorJsonUtility.ToJson(tex, true);

                if (onModified != null)
                {
                    onModified(this, ModificationScope.Node);
                }
            }
        }
#else
        public Texture2D defaultTexture {get; set; }
#endif

        public TextureType textureType
        {
            get { return m_TextureType; }
            set
            {
                if (m_TextureType == value)
                    return;


                m_TextureType = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }*/

        public Texture2D defaultTexture { get; set; }

        public DepthTextureNode()
        {
            name = "DepthTexture";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(UvSlotId, kUVSlotName, kUVSlotName, SlotType.Input, SlotValueType.Vector2, Vector4.zero, false));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { OutputSlotId, UvSlotId }; }
        }

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

            string body = "tex2D (_CameraDepthTexture, " + uvName + ")";
            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForNode() + " = " + body + ";", true);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(GetPreviewProperty());
        }

        // Properties
        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            /*visitor.AddShaderProperty(
                new TexturePropertyChunk(
                    propertyName,
                    description,
                    defaultTexture, m_TextureType,
                    PropertyChunk.HideState.Visible,
                    exposedState == ExposedState.Exposed ?
                        TexturePropertyChunk.ModifiableState.Modifiable
                        : TexturePropertyChunk.ModifiableState.NonModifiable));*/
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("sampler2D _CameraDepthTexture;", true);
            //visitor.AddShaderChunk("sampler2D " + propertyName + ";", true);
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

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
            //return propertyName;
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
        }
    }
}
