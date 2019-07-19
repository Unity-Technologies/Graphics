using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{

    public class LightReactor2D : MonoBehaviour, IShadowCasterGroup2D
    {
        //[SerializeField]
        public int m_ShadowGroup = 0;
        List<ShadowCaster2D> m_ShadowCasters;

        int m_PreviousShadowGroup = 0;

        public List<ShadowCaster2D> GetShadowCasters() { return m_ShadowCasters; }

        public int GetShadowGroup() { return m_ShadowGroup; }

        public void RegisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters == null)
                m_ShadowCasters = new List<ShadowCaster2D>();

            m_ShadowCasters.Add(shadowCaster2D);
            //LightUtility.AddShadowCasterGroupToList(shadowCaster2D, m_ShadowCasters);
        }

        public void UnregisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if(m_ShadowCasters != null)
                m_ShadowCasters.Remove(shadowCaster2D);
            //LightUtility.RemoveShadowCasterGroupFromList(shadowCaster2D, m_ShadowCasters);
        }

        private void OnEnable()
        {
            ShadowCasterGroup2DManager.AddGroup(this);
        }

        private void OnDisable()
        {
            ShadowCasterGroup2DManager.RemoveGroup(this);
        }

        public void Update()
        {
            if (LightUtility.CheckForChange(m_ShadowGroup, ref m_PreviousShadowGroup))
            {
                // 
                //m_ShadowCasters
            }
        }
    }
}
