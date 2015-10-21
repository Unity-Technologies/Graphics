using System;
using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public enum TextureType
    {
        White,
        Gray,
        Black,
        Bump
    }

    [Title("Input/Texture Node")]
    public class TextureNode : PropertyNode, IGeneratesBodyCode, IGeneratesVertexShaderBlock, IGeneratesVertexToFragmentBlock
    {
        protected const string kOutputSlotName = "Output";
        protected const string kUVSlotName = "UV";

        [SerializeField]
        public Texture2D m_DefaultTexture;

        [SerializeField]
        public TextureType m_TextureType;

        private List<string> m_TextureTypeNames;

        public override bool hasPreview { get { return false; } }

        public override void Init()
        {
            name = "Texture";
            base.Init();
            AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
            AddSlot(new Slot(SlotType.InputSlot, kUVSlotName));

            LoadTextureTypes();
        }

        private void LoadTextureTypes()
        {
            if (m_TextureTypeNames == null)
                m_TextureTypeNames = new List<string>(Enum.GetNames(typeof(TextureType)));
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot(kOutputSlotName);
            if (outputSlot == null)
                return;

            var uvSlot = FindInputSlot(kUVSlotName);
            if (uvSlot == null)
                return;

            var uvName = "IN.meshUV0";
            if (uvSlot.edges.Count > 0)
            {
                var fromNode = uvSlot.edges[0].fromSlot.node as BaseMaterialNode;
                uvName = fromNode.GetOutputVariableNameForSlot(uvSlot.edges[0].fromSlot, generationMode);
            }

            string body = "tex2D (" + GetPropertyName() + ", " + uvName + ".xy)";
            if (m_TextureType == TextureType.Bump)
                body = precision + "4(UnpackNormal(" + body + "), 0)";
            visitor.AddShaderChunk("float4 " + GetOutputVariableNameForSlot(outputSlot, generationMode) + " = " + body + ";", true);
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var uvSlot = FindInputSlot(kUVSlotName);
            if (uvSlot == null)
                return;

            if (uvSlot.edges.Count == 0)
                UVNode.StaticGenerateVertexToFragmentBlock(visitor, generationMode);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var uvSlot = FindInputSlot(kUVSlotName);
            if (uvSlot == null)
                return;

            if (uvSlot.edges.Count == 0)
                UVNode.GenerateVertexShaderBlock(visitor);
        }

        // Properties
        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (HasBoundProperty())
                return;

            visitor.AddShaderProperty(new TexturePropertyChunk(GetPropertyName(), GetPropertyName(), m_DefaultTexture, m_TextureType, true));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (HasBoundProperty())
                return;

            visitor.AddShaderChunk("sampler2D " + GetPropertyName() + ";", true);
        }

        // UI Shizz
        public override void NodeUI(GraphGUI host)
        {
            LoadTextureTypes();

            base.NodeUI();

            EditorGUI.BeginChangeCheck();
            m_DefaultTexture = EditorGUILayout.ObjectField(GUIContent.none, m_DefaultTexture, typeof(Texture2D), false) as Texture2D;
            m_TextureType = (TextureType)EditorGUILayout.Popup((int)m_TextureType, m_TextureTypeNames.ToArray(), EditorStyles.popup);
            if (EditorGUI.EndChangeCheck())
            {
                var boundProp = boundProperty as TextureProperty;
                if (boundProp != null)
                {
                    boundProp.defaultTexture = m_DefaultTexture;
                    boundProp.defaultTextureType = m_TextureType;
                }
                UpdatePreviewProperties();
                ForwardPreviewMaterialPropertyUpdate();
            }
        }

        public override void BindProperty(ShaderProperty property, bool rebuildShaders)
        {
            base.BindProperty(property, rebuildShaders);

            var texProp = property as TextureProperty;
            if (texProp)
            {
                m_DefaultTexture = texProp.defaultTexture;
                m_TextureType = texProp.defaultTextureType;
            }
            if (rebuildShaders)
                RegeneratePreviewShaders();
            else
            {
                UpdatePreviewProperties();
                ForwardPreviewMaterialPropertyUpdate();
            }
        }

        public override PreviewProperty GetPreviewProperty()
        {
            MaterialWindow.DebugMaterialGraph("Returning: " + GetPropertyName() + " " + m_DefaultTexture);
            return new PreviewProperty
                   {
                       m_Name = GetPropertyName(),
                       m_PropType = PropertyType.Texture2D,
                       m_Texture = m_DefaultTexture
                   };
        }

        public override PropertyType propertyType { get { return PropertyType.Texture2D; } }
    }
}
