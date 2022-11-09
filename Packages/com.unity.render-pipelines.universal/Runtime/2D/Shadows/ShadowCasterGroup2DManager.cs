using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
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

        public static void CacheValues()
        {
            if (shadowCasterGroups != null)
            {
                for (int i = 0; i < shadowCasterGroups.Count; i++)
                {
                    if (shadowCasterGroups[i] != null)
                        shadowCasterGroups[i].CacheValues();
                }
            }
        }

        public static void AddShadowCasterGroupToList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
        {
            int positionToInsert = 0;
            for (positionToInsert = 0; positionToInsert < list.Count; positionToInsert++)
            {
                if (shadowCaster.m_Priority < list[positionToInsert].m_Priority)
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
                CompositeShadowCaster2D currentGroup;
                if (transformToCheck.TryGetComponent<CompositeShadowCaster2D>(out currentGroup))
                    retGroup = currentGroup;

                transformToCheck = transformToCheck.parent;
            }

            return retGroup;
        }


        public static int GetRendereringPriority(ShadowCaster2D shadowCaster)
        {
            int sortingOrder = 0;
            // This should take sorting groups into account, but doesn't as there isn't a way to do this at the moment.
            Renderer renderer;
            if (shadowCaster.TryGetComponent<Renderer>(out renderer))
                sortingOrder = renderer.sortingOrder;

            return sortingOrder;
        }

        public static bool AddToShadowCasterGroup(ShadowCaster2D shadowCaster, ref ShadowCasterGroup2D shadowCasterGroup, ref int priority)
        {
            ShadowCasterGroup2D newShadowCasterGroup = FindTopMostCompositeShadowCaster(shadowCaster) as ShadowCasterGroup2D;
            int newPriority = 0;
            if (newShadowCasterGroup == null)
            {
                newPriority = GetRendereringPriority(shadowCaster);
                shadowCaster.TryGetComponent<ShadowCasterGroup2D>(out newShadowCasterGroup);
            }

            if (newShadowCasterGroup != null && (shadowCasterGroup != newShadowCasterGroup || priority != newPriority))
            {
                newShadowCasterGroup.RegisterShadowCaster2D(shadowCaster);
                shadowCasterGroup = newShadowCasterGroup;
                priority = newPriority;
                return true;
            }

            return false;
        }

        public static void RemoveFromShadowCasterGroup(ShadowCaster2D shadowCaster, ShadowCasterGroup2D shadowCasterGroup)
        {
            if (shadowCasterGroup != null)
                shadowCasterGroup.UnregisterShadowCaster2D(shadowCaster);

            if (shadowCasterGroup == shadowCaster)
                RemoveGroup(shadowCasterGroup);
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
