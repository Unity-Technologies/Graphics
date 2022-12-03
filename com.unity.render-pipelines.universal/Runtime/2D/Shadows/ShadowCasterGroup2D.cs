using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class for 2D shadow caster groups.
    /// </summary>
    [MovedFrom(false, "UnityEngine.Experimental.Rendering.Universal", "com.unity.render-pipelines.universal")]
    public abstract class ShadowCasterGroup2D : MonoBehaviour
    {
        [SerializeField] internal int  m_ShadowGroup = 0;
        [SerializeField] internal int  m_Priority = 0;
        List<ShadowCaster2D> m_ShadowCasters;

        internal virtual void CacheValues()
        {
            if (m_ShadowCasters != null)
            {
                for (int i = 0; i < m_ShadowCasters.Count; i++)
                    m_ShadowCasters[i].CacheValues();
            }
        }

        /// <summary>
        /// Returns a list of registered 2D shadow casters.
        /// </summary>
        /// <returns>A list of 2D shadow casters that have been registered..</returns>
        public List<ShadowCaster2D> GetShadowCasters()
        {
            return m_ShadowCasters;
        }

        /// <summary>
        /// Returns the shadow group.
        /// </summary>
        /// <returns>The shadow group used.</returns>
        public int GetShadowGroup()
        {
            return m_ShadowGroup;
        }

        /// <summary>
        /// Registers a 2D shadow caster.
        /// </summary>
        /// <param name="shadowCaster2D">The 2D shadow to register.</param>
        public void RegisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters == null)
                m_ShadowCasters = new List<ShadowCaster2D>();

            int insertAtIndex = 0;
            for (insertAtIndex = 0; insertAtIndex < m_ShadowCasters.Count; insertAtIndex++)
            {
                if (shadowCaster2D.m_Priority >= m_ShadowCasters[insertAtIndex].m_Priority)
                    break;
            }

            m_ShadowCasters.Insert(insertAtIndex, shadowCaster2D);
        }

        /// <summary>
        /// Unregisters a 2D shadow caster.
        /// </summary>
        /// <param name="shadowCaster2D">The 2D shadow to unregister.</param>
        public void UnregisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters != null)
                m_ShadowCasters.Remove(shadowCaster2D);
        }
    }
}
