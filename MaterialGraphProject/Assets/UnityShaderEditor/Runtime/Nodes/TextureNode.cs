using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
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

        public override bool hasPreview { get { return true; } }

        public TextureNode(IGraph owner) : base(owner)
        {
            name = "Texture";
            LoadTextureTypes();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            LoadTextureTypes();

            AddSlot(new MaterialSlot(kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, 0, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotRName, kOutputSlotRName, SlotType.Output, 1, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotGName, kOutputSlotGName, SlotType.Output, 2, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotBName, kOutputSlotBName, SlotType.Output, 3, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotAName, kOutputSlotAName, SlotType.Output, 4, SlotValueType.Vector1, Vector4.zero));

            AddSlot(new MaterialSlot(kUVSlotName, kUVSlotName, SlotType.Input, 0, SlotValueType.Vector2, Vector4.zero));
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

            var uvName = "IN.meshUV0.xy";

            var edges = owner.GetEdges(GetSlotReference(uvSlot.name)).ToList();

            if (edges.Count > 0)
            {
                var edge = edges[0];
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                var slot = fromNode.FindMaterialOutputSlot(edge.outputSlot.slotName);
                uvName = ShaderGenerator.AdaptNodeOutput(fromNode, slot, generationMode, ConcreteSlotValueType.Vector2, true);

            }

            string body = "tex2D (" + propertyName + ", " + uvName + ")";
            if (m_TextureType == TextureType.Bump)
                body = precision + "4(UnpackNormal(" + body + "), 0)";
            visitor.AddShaderChunk("float4 " + GetVariableNameForNode() + " = " + body + ";", true);
        }

        public override string GetOutputVariableNameForSlot(MaterialSlot s)
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
            return GetVariableNameForNode() + slotOutput;
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var uvSlot = FindInputSlot(kUVSlotName);
            if (uvSlot == null)
                return;

            var edges = owner.GetEdges(GetSlotReference(uvSlot.name));
            if (edges.Any())
                UVNode.StaticGenerateVertexToFragmentBlock(visitor, generationMode);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var uvSlot = FindInputSlot(kUVSlotName);
            if (uvSlot == null)
                return;

            var edges = owner.GetEdges(GetSlotReference(uvSlot.name));
            if (edges.Any())
                UVNode.GenerateVertexShaderBlock(visitor);
        }

        // Properties
        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(new TexturePropertyChunk(propertyName, description, m_DefaultTexture, m_TextureType, false, exposed));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            visitor.AddShaderChunk("sampler2D " + propertyName + ";", true);
        }

        /*
        public override float GetNodeUIHeight(float width)
        {
            return EditorGUIUtility.singleLineHeight * 2;
        }

        public override GUIModificationType NodeUI(Rect drawArea)
        {
            LoadTextureTypes();

            base.NodeUI(drawArea);

            EditorGUI.BeginChangeCheck();
            m_DefaultTexture = EditorGUI.MiniThumbnailObjectField( new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), new GUIContent("Texture"), m_DefaultTexture, typeof(Texture2D), null) as Texture2D;
            var texureChanged = EditorGUI.EndChangeCheck();

            drawArea.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginChangeCheck();
            m_TextureType = (TextureType) EditorGUI.Popup(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), (int) m_TextureType, m_TextureTypeNames.ToArray(), EditorStyles.popup);
            var typeChanged = EditorGUI.EndChangeCheck();

            if (typeChanged)
            {
                pixelGraph.RevalidateGraph();
                return GUIModificationType.Repaint;
            }

            return texureChanged ? GUIModificationType.Repaint : GUIModificationType.None;
        }

    
        public override bool DrawSlotDefaultInput(Rect rect, Slot inputSlot)
        {
            var uvSlot = FindInputSlot(kUVSlotName);
            if (uvSlot != inputSlot)
                return base.DrawSlotDefaultInput(rect, inputSlot);


            var rectXmax = rect.xMax;
            rect.x = rectXmax - 70;
            rect.width = 70;

            EditorGUI.DrawRect(rect, new Color(0.0f, 0.0f, 0.0f, 0.7f));
            GUI.Label(rect, "From Mesh");

            return false;
        }
        */

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Texture2D,
                m_Texture = m_DefaultTexture
            };
        }
        
        public override PropertyType propertyType { get { return PropertyType.Texture2D; } }
    }
}
