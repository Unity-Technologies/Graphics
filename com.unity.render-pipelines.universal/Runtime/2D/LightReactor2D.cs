using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    public class LightReactor2D : ShadowCaster2D, IShadowCasterGroup2D
    {
        [SerializeField] int m_ShadowGroup = 0;
        [SerializeField] bool m_SelfShadows = false;
        [SerializeField] bool m_CastsShadows = true;
        [SerializeField] bool m_ReceivesShadows = true;
        List<ShadowCaster2D> m_ShadowCasters;
        Renderer m_Renderer;

        public bool selfShadows => m_SelfShadows;
        public bool castsShadows => m_CastsShadows;
        public bool receivesShadows => m_ReceivesShadows;

        int m_PreviousShadowGroup = 0;
        bool m_PreviousCastsShadows = true;
        bool m_PreviousReceivesShadows = true;

        public List<ShadowCaster2D> GetShadowCasters() { return m_ShadowCasters; }

        public int GetShadowGroup() { return m_ShadowGroup; }

        public Renderer GetRenderer() { return m_Renderer; }

        public void RegisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters == null)
                m_ShadowCasters = new List<ShadowCaster2D>();

            m_ShadowCasters.Add(shadowCaster2D);
        }

        public void UnregisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if(m_ShadowCasters != null)
                m_ShadowCasters.Remove(shadowCaster2D);
        }

        private void OnStart()
        {
            m_Renderer = GetComponent<Renderer>();
        }

        new private void OnEnable()
        {
            base.OnEnable();
            ShadowCasterGroup2DManager.AddGroup(this);
        }

        new private void OnDisable()
        {
            base.OnDisable();
            ShadowCasterGroup2DManager.RemoveGroup(this);
        }


        new public void Update()
        {
            base.Update();

            if (LightUtility.CheckForChange(m_ShadowGroup, ref m_PreviousShadowGroup))
            {
                ShadowCasterGroup2DManager.RemoveGroup(this);
                ShadowCasterGroup2DManager.AddGroup(this);
            }

            if(LightUtility.CheckForChange(m_CastsShadows, ref m_PreviousCastsShadows))
            {
                if(m_CastsShadows)
                    ShadowCasterGroup2DManager.AddGroup(this);
                else
                    ShadowCasterGroup2DManager.RemoveGroup(this);
            }

            if(LightUtility.CheckForChange(m_ReceivesShadows, ref m_PreviousReceivesShadows))
            {
                
            }
        }
    }
}
