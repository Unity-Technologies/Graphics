using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering
{
    class SerializedUIBase
    {
        protected AnimBool[] m_AnimBools = null;

        public SerializedUIBase(int animBoolCount)
        {
            m_AnimBools = new AnimBool[animBoolCount];
            for (var i = 0; i < m_AnimBools.Length; ++i)
                m_AnimBools[i] = new AnimBool();
        }

        public virtual void Reset(UnityAction repaint)
        {
            for (var i = 0; i < m_AnimBools.Length; ++i)
            {
                m_AnimBools[i].valueChanged.RemoveAllListeners();
                m_AnimBools[i].valueChanged.AddListener(repaint);
            }

            Update();
        }

        public virtual void Update()
        {
        }
    }
}
