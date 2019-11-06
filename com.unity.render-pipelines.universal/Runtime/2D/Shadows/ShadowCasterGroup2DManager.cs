using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class ShadowCasterGroup2DManager
    {
        static List<ShadowCasterGroup2D> s_ShadowCasterGroups = null;

        public static List<ShadowCasterGroup2D> shadowCasterGroups { get { return s_ShadowCasterGroups; } }

        public static void AddGroup(ShadowCasterGroup2D group)
        {
            if (group == null)
                return;

            if (s_ShadowCasterGroups == null)
                s_ShadowCasterGroups = new List<ShadowCasterGroup2D>();

            LightUtility.AddShadowCasterGroupToList(group, s_ShadowCasterGroups);
        }
        public static void RemoveGroup(ShadowCasterGroup2D group)
        {
            if (group != null && s_ShadowCasterGroups != null)
                LightUtility.RemoveShadowCasterGroupFromList(group, s_ShadowCasterGroups);
        }
    }
}
