using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class ShadowCasterGroup2DManager : MonoBehaviour
    {

        static List<IShadowCasterGroup2D> m_ShadowCasterGroups = null;

        public static List<IShadowCasterGroup2D> shadowCasterGroups { get { return m_ShadowCasterGroups; } }

        public static void AddGroup(IShadowCasterGroup2D group)
        {
            if (group == null)
                return;

            if (m_ShadowCasterGroups == null)
                m_ShadowCasterGroups = new List<IShadowCasterGroup2D>();

            LightUtility.AddShadowCasterGroupToList(group, m_ShadowCasterGroups);
        }
        public static void RemoveGroup(IShadowCasterGroup2D group)
        {
            if (group != null && m_ShadowCasterGroups != null)
                LightUtility.RemoveShadowCasterFromList(group, m_ShadowCasterGroups);
        }


    }
}
