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
        protected const string kOutputSlotRGBAName = "RGBA";
        protected const string kOutputSlotRName = "R";
        protected const string kOutputSlotGName = "G";
        protected const string kOutputSlotBName = "B";
        protected const string kOutputSlotAName = "A";
        protected const string kUVSlotName = "UV";

        [SerializeField]
        public Texture2D m_DefaultTexture;

        [SerializeField]
        public TextureType m_TextureType;

        private List<string> m_TextureTypeNames;

        public override bool hasPreview { get { return false; } }

        public override void OnCreate()
        {
            name = "Texture";
            base.OnCreate();
            LoadTextureTypes();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotRGBAName), SlotValueType.Vector4));
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotRName), SlotValueType.Vector1));
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotGName), SlotValueType.Vector1));
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotBName), SlotValueType.Vector1));
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotAName), SlotValueType.Vector1));
            
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.InputSlot, kUVSlotName), SlotValueType.Vector2));

            RemoveSlotsNameNotMatching(validSlots);
        }

        protected string[] validSlots
        {
            get { return new[] {kOutputSlotRGBAName, kOutputSlotRName, kOutputSlotGName, kOutputSlotBName, kOutputSlotAName, kUVSlotName}; }
        }

        private void LoadTextureTypes()
        {
            if (m_TextureTypeNames == null)
                m_TextureTypeNames = new List<string>(Enum.GetNames(typeof(TextureType)));
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
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
            visitor.AddShaderChunk("float4 " + GetOutputVariableNameForNode() + " = " + body + ";", true);
        }

        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            string slotOutput;
            switch (s.name)
            {
                case kOutputSlotRName:
                    slotOutput = ".r";
                    break;
                case kOutputSlotGName:
                    slotOutput = ".g";
                    break;
                case kOutputSlotBName:
                    slotOutput = ".b";
                    break;
                case kOutputSlotAName:
                    slotOutput = ".a";
                    break;
                default:
                    slotOutput = "";
                    break;
            }
            return GetOutputVariableNameForNode() + slotOutput;
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
        {}

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            if (HasBoundProperty())
                return;

            visitor.AddShaderChunk("sampler2D " + GetPropertyName() + ";", true);
        }

        public override float GetNodeUIHeight(float width)
        {
            return EditorGUIUtility.singleLineHeight + width - 20;
        }

        public override bool NodeUI(Rect drawArea)
        {
            LoadTextureTypes();

            base.NodeUI(drawArea);

            EditorGUI.BeginChangeCheck();
            m_DefaultTexture = EditorGUI.ObjectField(new Rect(drawArea.x, drawArea.y, drawArea.width, drawArea.width), GUIContent.none, m_DefaultTexture, typeof (Texture2D), false) as Texture2D;
            var texureChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            m_TextureType = (TextureType) EditorGUI.Popup(new Rect(drawArea.x, drawArea.y + drawArea.width, drawArea.width, EditorGUIUtility.singleLineHeight), (int) m_TextureType, m_TextureTypeNames.ToArray(), EditorStyles.popup);
            var typeChanged = EditorGUI.EndChangeCheck();

            if (typeChanged)
            {
                RegeneratePreviewShaders();
                return true;
            }

            if (texureChanged)
            {
                var boundProp = boundProperty as TextureProperty;
                if (boundProp != null)
                {
                    boundProp.defaultTexture = m_DefaultTexture;
                    boundProp.defaultTextureType = m_TextureType;
                }
                UpdatePreviewProperties();
                ForwardPreviewMaterialPropertyUpdate();
                return true;
            }
            return false;
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
