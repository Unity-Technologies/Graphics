using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Input/Vector 4 Node")]
    class Vector4Node : PropertyNode, IGeneratesBodyCode
    {
        protected class NodeSpecificData : PropertyNode.NodeSpecificData
        {
            [SerializeField]
            public Vector4 m_Value;
        }

        protected void ApplyNodeSpecificData(NodeSpecificData data)
        {
            base.ApplyNodeSpecificData(data);
            m_NodeSpecificData.m_Value = data.m_Value;
        }

        protected override void DelegateOnBeforeSerialize()
        {
            m_JSONNodeSpecificData = JsonUtility.ToJson(m_NodeSpecificData);
        }

        protected override void DelegateOnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_JSONNodeSpecificData))
                return;

            var data = JsonUtility.FromJson<NodeSpecificData>(m_JSONNodeSpecificData);
            ApplyNodeSpecificData(data);
            InternalValidate();
        }

        private void InternalValidate() 
        {
            AddSlot(new Slot(guid, kOutputSlotName, kOutputSlotName, Slot.SlotType.Output, SlotValueType.Vector4, Vector4.zero));
        }

        private const string kOutputSlotName = "Value";
        
        private NodeSpecificData m_NodeSpecificData = new NodeSpecificData();

        public Vector4Node(BaseMaterialGraph owner) : base(owner)
        {
            name = "V4Node";
            InternalValidate();
        }
        
        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_NodeSpecificData.m_Value, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType valueType)
        {
            if (exposed || generationMode.IsPreview())
                visitor.AddShaderChunk("float4 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposed || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "4 " +  propertyName + " = " + precision + "4 (" + m_NodeSpecificData.m_Value.x + ", " + m_NodeSpecificData.m_Value.y + ", " + m_NodeSpecificData.m_Value.z + ", " + m_NodeSpecificData.m_Value.w + ");", true);
        }

        public override GUIModificationType NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);

            EditorGUI.BeginChangeCheck();
            m_NodeSpecificData.m_Value = EditorGUI.Vector4Field(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), "Value", m_NodeSpecificData.m_Value);
            if (EditorGUI.EndChangeCheck())
                return GUIModificationType.Repaint;
            return GUIModificationType.None;
        }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
                   {
                       m_Name = propertyName,
                       m_PropType = PropertyType.Vector4,
                       m_Vector4 = m_NodeSpecificData.m_Value
                   };
        }
    }
}
