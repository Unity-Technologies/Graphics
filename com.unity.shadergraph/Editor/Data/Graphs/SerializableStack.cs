using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SerializableStack : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_SerializedStack;

        [SerializeField]
        string m_Guid;

        [NonSerialized]
        VTStack m_Stack;

        [Serializable]
        class StackHelper
        {
#pragma warning disable 649
            public VTStack stack;
#pragma warning restore 649
        }

        public VTStack stack
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedStack))
                {
                    var textureHelper = new StackHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedStack, textureHelper);
                    m_SerializedStack = null;
                    m_Guid = null;
                    m_Stack = textureHelper.stack;
                }
                else if (!string.IsNullOrEmpty(m_Guid) && m_Stack == null)
                {
                    m_Stack = AssetDatabase.LoadAssetAtPath<VTStack>(AssetDatabase.GUIDToAssetPath(m_Guid));
                    m_Guid = null;
                }

                return m_Stack;
            }
            set
            {
                m_Stack = value;
                m_Guid = null;
                m_SerializedStack = null;
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializedStack = EditorJsonUtility.ToJson(new StackHelper { stack = stack }, false);
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
