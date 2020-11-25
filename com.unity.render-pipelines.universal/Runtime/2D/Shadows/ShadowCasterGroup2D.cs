using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ShadowCasterGroup2D : MonoBehaviour
    {
        [SerializeField] internal int m_ShadowGroup = 0;
        List<ShadowCaster2D> m_ShadowCasters;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<ShadowCaster2D> GetShadowCasters() { return m_ShadowCasters; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetShadowGroup() { return m_ShadowGroup; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shadowCaster2D"></param>
        public void RegisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters == null)
                m_ShadowCasters = new List<ShadowCaster2D>();

            m_ShadowCasters.Add(shadowCaster2D);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shadowCaster2D"></param>
        public void UnregisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters != null)
                m_ShadowCasters.Remove(shadowCaster2D);
        }
    }
}
