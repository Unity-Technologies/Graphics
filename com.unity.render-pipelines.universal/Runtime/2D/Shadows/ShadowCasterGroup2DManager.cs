using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.Universal
{
#if UNITY_EDITOR
    [InitializeOnLoadAttribute]
#endif
    internal class ShadowCasterGroup2DManager
    {
        static List<ShadowCasterGroup2D> s_ShadowCasterGroups = null;

        public static List<ShadowCasterGroup2D> shadowCasterGroups { get { return s_ShadowCasterGroups; } }


#if UNITY_EDITOR
        static ShadowCasterGroup2DManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (s_ShadowCasterGroups != null && (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode))
                s_ShadowCasterGroups.Clear();
        }

#endif

        public static void AddShadowCasterGroupToList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
        {
            int positionToInsert = 0;
            for (positionToInsert = 0; positionToInsert < list.Count; positionToInsert++)
            {
                if (shadowCaster.GetShadowGroup() == list[positionToInsert].GetShadowGroup())
                    break;
            }

            list.Insert(positionToInsert, shadowCaster);
        }

        public static void RemoveShadowCasterGroupFromList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
        {
            list.Remove(shadowCaster);
        }

        static CompositeShadowCaster2D FindTopMostCompositeShadowCaster(ShadowCaster2D shadowCaster)
        {
            CompositeShadowCaster2D retGroup = null;

            Transform transformToCheck = shadowCaster.transform.parent;
            while (transformToCheck != null)
            {
                CompositeShadowCaster2D currentGroup = transformToCheck.GetComponent<CompositeShadowCaster2D>();
                if (currentGroup != null)
                    retGroup = currentGroup;

                transformToCheck = transformToCheck.parent;
            }

            return retGroup;
        }

        public static bool AddToShadowCasterGroup(ShadowCaster2D shadowCaster, ref ShadowCasterGroup2D shadowCasterGroup)
        {
            ShadowCasterGroup2D newShadowCasterGroup = FindTopMostCompositeShadowCaster(shadowCaster) as ShadowCasterGroup2D;

            if (newShadowCasterGroup == null)
                newShadowCasterGroup = shadowCaster.GetComponent<ShadowCaster2D>();

            if (newShadowCasterGroup != null && shadowCasterGroup != newShadowCasterGroup)
            {
                newShadowCasterGroup.RegisterShadowCaster2D(shadowCaster);
                shadowCasterGroup = newShadowCasterGroup;
                return true;
            }

            return false;
        }

        public static void RemoveFromShadowCasterGroup(ShadowCaster2D shadowCaster, ShadowCasterGroup2D shadowCasterGroup)
        {
            if (shadowCasterGroup != null)
                shadowCasterGroup.UnregisterShadowCaster2D(shadowCaster);
        }

        public static void AddGroup(ShadowCasterGroup2D group)
        {
            if (group == null)
                return;

            if (s_ShadowCasterGroups == null)
                s_ShadowCasterGroups = new List<ShadowCasterGroup2D>();

            AddShadowCasterGroupToList(group, s_ShadowCasterGroups);
        }
        public static void RemoveGroup(ShadowCasterGroup2D group)
        {
            if (group != null && s_ShadowCasterGroups != null)
                RemoveShadowCasterGroupFromList(group, s_ShadowCasterGroups);
        }
    }
}
