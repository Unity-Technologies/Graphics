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
            m_Entries = new List<KeyValuePair<string, string>>();

            // TODO: Remove when BlackboardFieldKeywordView has content
            m_Entries.Add(new KeyValuePair<string, string>("A", "_A"));
            m_Entries.Add(new KeyValuePair<string, string>("B", "_B"));
            m_Entries.Add(new KeyValuePair<string, string>("C", "_C"));
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
        private List<KeyValuePair<string, string>> m_Entries;

        public List<KeyValuePair<string, string>> entries
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
