
using System;


namespace UnityEngine.Rendering.Universal
{


    [Serializable]
    internal struct Provider2DKVPair
    {
        public Provider2DInfo m_Key;
        public Provider2DRef m_Value;

        public Provider2DKVPair(Provider2DInfo key, Provider2DRef value)
        {
            m_Key = key;
            m_Value = value;
        }
    }
}
