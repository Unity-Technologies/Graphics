using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    internal class DefaultVFXTargetData : TargetImplementationData
    {
        [SerializeField]
        bool m_Lit;

        [SerializeField]
        bool m_AlphaTest;

        public bool lit
        {
            get => m_Lit;
            set => m_Lit = value;
        }

        public bool alphaTest
        {
            get => m_AlphaTest;
            set => m_AlphaTest = value;
        }
    }
}
