using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.Rendering.Universal
{
    [MovedFrom("UnityEngine.Experimental.Rendering.Universal")]
    public abstract class ShadowCasterGroup2D : MonoBehaviour
    {
        [SerializeField] internal int m_ShadowGroup = 0;
        List<ShadowCaster2D> m_ShadowCasters;

        internal virtual void CacheValues()
        {
            for (int i = 0; i < m_ShadowCasters.Count; i++)
                m_ShadowCasters[i].CacheValues();
        }

        public List<ShadowCaster2D> GetShadowCasters() { return m_ShadowCasters; }

        public int GetShadowGroup() { return m_ShadowGroup; }

        public void RegisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters == null)
                m_ShadowCasters = new List<ShadowCaster2D>();

            m_ShadowCasters.Add(shadowCaster2D);
        }

        public void UnregisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters != null)
                m_ShadowCasters.Remove(shadowCaster2D);
        }
    }
}
