using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{   
    [Serializable]
    class ShaderKeyword : ShaderInput
    {
        public ShaderKeyword()
        {
            displayName = "Keyword";
            m_Entries = new List<string>();
        }

        public override ConcreteSlotValueType concreteShaderValueType => ConcreteSlotValueType.Vector1;

        [SerializeField]
        private int m_Value;

        public int value
        {
            get => m_Value;
            set => m_Value = value;
        }

        [SerializeField]
        private List<string> m_Entries;

        public List<string> entries
        {
            get => m_Entries;
            set => m_Entries = value;
        }

        public override ShaderInput Copy()
        {
            return new ShaderKeyword()
            {
                displayName = displayName,
                value = value
            };
        }
    }
}
