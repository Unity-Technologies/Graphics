using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
    {
        [Title("Input/Texture/Texture Sampler")]
        public class TextureSamplerNode : PropertyNode, IGeneratesBodyCode, IMayRequireMeshUV
        {
            private const string kTextureAssetName = "Texture Asset";
            private const string kUVSlotName = "UV";
            private const string kSamplerName = "Sampler";
            protected const string kOutputSlotRGBAName = "RGBA";

            public const int TextureAssetSlotId = 0;
            public const int UVSlotId = 1;
            public const int SamplerSlotId = 2;
            public const int OutputSlotRGBAId = 3;

            [SerializeField]
            private string m_SerializedTexture;

            [SerializeField]
            private TextureType m_TextureType;

            [Serializable]
            private class TextureHelper
            {
                public Texture2D texture;
            }

            public override bool hasPreview { get { return true; } }

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
            }

            public TextureSamplerNode()
            {
                name = "TextureSamplerNode";
                UpdateNodeAfterDeserialization();
            }

        public sealed override void UpdateNodeAfterDeserialization()
            {
            AddSlot(new MaterialSlot(TextureAssetSlotId, kTextureAssetName, kTextureAssetName, SlotType.Input, SlotValueType.Texture2D, Vector4.zero, false));
            AddSlot(new MaterialSlot(UVSlotId, kUVSlotName, kUVSlotName, SlotType.Input, SlotValueType.Vector2, Vector4.zero, false));
            AddSlot(new MaterialSlot(SamplerSlotId, kSamplerName, kSamplerName, SlotType.Input, SlotValueType.SamplerState, Vector4.zero, false));
            AddSlot(new MaterialSlot(OutputSlotRGBAId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            RemoveSlotsNameNotMatching(validSlots);
            }

            protected int[] validSlots
            {
                get { return new[] { OutputSlotRGBAId, SamplerSlotId, UVSlotId, TextureAssetSlotId }; }
            }

            // Node generations
            public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
            {

                //Texture input slot
                var textureSlot = FindInputSlot<MaterialSlot>(TextureAssetSlotId);
                var textureAssetName = "";
                var edgesTexture = owner.GetEdges(textureSlot.slotReference).ToList();

                if (edgesTexture.Count > 0)
                {
                    var edge = edgesTexture[0];
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                    textureAssetName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.Texture2D, true);
                }

                //UV input slot
                var uvSlot = FindInputSlot<MaterialSlot>(UVSlotId);
                var uvName = string.Format("{0}.xy", UVChannel.uv0.GetUVName());
                if (uvSlot == null)
                return;

                var edgesUV = owner.GetEdges(uvSlot.slotReference).ToList();

                if (edgesUV.Count > 0)
                {
                    var edge = edgesUV[0];
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                    uvName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.Vector2, true);
                }


                //Sampler input slot
                var samplerSlot = FindInputSlot<MaterialSlot>(SamplerSlotId);
                var samplerName = "my_linear_repeat_sampler";

                if (samplerSlot == null)
                    return;

                var edgesSampler = owner.GetEdges(samplerSlot.slotReference).ToList();

                if (edgesSampler.Count > 0)
                {
                    var edge = edgesSampler[0];
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                    samplerName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.SamplerState, true);
                }

            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForNode() + "_Uniform = " + precision + "4(0,0,0,0);", true);
            visitor.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", true);
            string body = textureAssetName + ".Sample(" + samplerName + ", " + uvName + ");";
                if (m_TextureType == TextureType.Bump)
                    body = precision + "4(UnpackNormal(" + body + "), 0)";
            
                 visitor.AddShaderChunk(GetVariableNameForNode() + "_Uniform" + " = " + body, true);

            visitor.AddShaderChunk("#endif", true);
        }
       
            // Properties
            public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
            {   /*
                visitor.AddShaderProperty(
                    new TexturePropertyChunk(
                        propertyName,
                        description,
                        defaultTexture, m_TextureType,
                        PropertyChunk.HideState.Visible,
                        exposedState == ExposedState.Exposed ?
                            TexturePropertyChunk.ModifiableState.Modifiable
                            : TexturePropertyChunk.ModifiableState.NonModifiable));
                */
            }

            public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
            {

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerSlotId);
            var samplerName = "my_linear_repeat_sampler";

            if (samplerSlot == null)
                return;

            var edgesSampler = owner.GetEdges(samplerSlot.slotReference).ToList();

            if (edgesSampler.Count > 0)
            {
                var edge = edgesSampler[0];
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                samplerName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.SamplerState, true);
            }
            
            visitor.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", false);
            visitor.AddShaderChunk(samplerSlot.valueType + " " + samplerName + ";", true);
            visitor.AddShaderChunk("#endif", false);
        }
        
        
            public override PreviewProperty GetPreviewProperty()
            {
                return new PreviewProperty
                {
                    m_Name = propertyName,
                    m_PropType = PropertyType.Texture,
                    m_Texture = defaultTexture
                };
            }
        
            public override PropertyType propertyType { get { return PropertyType.Texture; } }

            public bool RequiresMeshUV(UVChannel channel)
            {
                if (channel != UVChannel.uv0)
                {
                    return false;
                }

                var uvSlot = FindInputSlot<MaterialSlot>(UVSlotId);
                if (uvSlot == null)
                    return true;

                var edges = owner.GetEdges(uvSlot.slotReference).ToList();
                return edges.Count == 0;
            }

        //prevent validation errors when a sampler2D input is missing
        //use on any input requiring a TextureAssetNode
        public override void ValidateNode()
        {
            base.ValidateNode();
            var slot = FindInputSlot<MaterialSlot>(TextureAssetSlotId);
            if (slot == null)
                return;

            var edges = owner.GetEdges(slot.slotReference).ToList();
            hasError |= edges.Count == 0;
        }

    }
    }
