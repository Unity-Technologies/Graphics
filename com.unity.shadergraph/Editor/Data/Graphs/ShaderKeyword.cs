using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{   
    enum KeywordType { Boolean, Enum }

    [Serializable]
    class ShaderKeyword : ShaderInput
    {
#region Name
        [SerializeField]
        private KeywordType m_KeywordType;
        public KeywordType keywordType => m_KeywordType;
        public override ConcreteSlotValueType concreteShaderValueType => keywordType.ToConcreteSlotValueType();
#endregion

#region Data
        [SerializeField]
        private bool m_BoolValue;

        public bool boolValue
        {
            get => m_BoolValue;
            set => m_BoolValue = value;
        }

        [SerializeField]
        private int m_IndexValue;

        public int indexValue
        {
            get => m_IndexValue;
            set => m_IndexValue = value;
        }
#endregion

#region Utility
        public override AbstractMaterialNode ToConcreteNode()
        {
            switch(keywordType)
            {
                case KeywordType.Boolean:
                    return new BooleanNode() { value = new ToggleData(boolValue) };
                case KeywordType.Enum:
                    var node = new Vector1Node();
                    node.FindInputSlot<Vector1MaterialSlot>(Vector1Node.InputSlotXId).value = indexValue;
                    return node;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
#endregion
    }
}
