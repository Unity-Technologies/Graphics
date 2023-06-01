using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDSixWayData : HDTargetData
    {
        [SerializeField]
        bool m_ReceiveShadows = true;

        public bool receiveShadows
        {
            get => m_ReceiveShadows;
            set => m_ReceiveShadows = value;
        }

        [SerializeField]
        bool m_UseColorAbsorption = true;

        public bool useColorAbsorption
        {
            get => m_UseColorAbsorption;
            set => m_UseColorAbsorption = value;
        }
    }
}
