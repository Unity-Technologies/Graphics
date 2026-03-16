using System;


namespace UnityEngine.Rendering.Universal
{

    [Serializable]
    internal struct Provider2DRef
    {
        [SerializeReference] public Provider2D m_Provider;

        public Provider2DRef(Provider2D provider)
        {
            m_Provider = provider;
        }
    }
}
