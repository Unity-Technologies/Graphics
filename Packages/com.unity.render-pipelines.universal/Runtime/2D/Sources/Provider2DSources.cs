#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class Provider2DSources<T, U> : IProvider2DSources where T : Provider2D where U : Provider2DSource
    {

        //Editor 
        [SerializeField] int                    m_SelectedHashCode;
        [SerializeReference] SelectionSource    m_SelectedSource;
        [SerializeReference] IProvider2DCache   m_Provider2DCache;
        List<SelectionSource>                   m_Sources = new List<SelectionSource>();
        List<SelectionSource>                   m_AdditionalSources = new List<SelectionSource>();

        private static int GetSelectedSourceIndexFromId(Provider2DSources<T, U> provider2DSources)
        {
            int hashCode = provider2DSources.m_SelectedHashCode;
            for (int i = 0; i < provider2DSources.m_Sources.Count; i++)
            {
                int sourceHashCode = provider2DSources.m_Sources[i].GetHashCode();
                if (hashCode == sourceHashCode)
                {
                    return i;
                }
            }

            return -1;
        }


        static void DeconflictNames(List<SelectionSource> selectionSources)
        {
            Dictionary<string, List<SelectionSource>> selectionSourcesByName = new Dictionary<string, List<SelectionSource>>();

            // Add all the names to the dictionary
            foreach (var selection in selectionSources)
            {
                string name = selection.GetSourceName()?.text?.Trim(); ;

                List<SelectionSource> sourceList;
                if(selectionSourcesByName.ContainsKey(name))
                {
                    sourceList = selectionSourcesByName[name];
                }
                else
                {
                    sourceList = new List<SelectionSource>();
                    selectionSourcesByName.Add(name, sourceList);
                }

                sourceList.Add(selection);
            }


            // Look for any repeats
            foreach(var sourceList in selectionSourcesByName.Values)
            {
                // If there is more than one source with the same name renumber it
                if(sourceList.Count > 1)
                {
                    for(int i=0;i<sourceList.Count;i++)
                    {
                        if(i > 0)
                            sourceList[i].m_MenuName.text = sourceList[i].m_MenuName.text + " (" + i + ")";
                    }
                }
            }
        }


        static void AddAddtionalSources(Provider2DSources<T, U> provider2DSources)
        {
            List<SelectionSource> additionalSources = provider2DSources.m_AdditionalSources;
            if (additionalSources != null)
            {
                foreach (SelectionSource additionalSource in additionalSources)
                {
                    provider2DSources.m_Sources.Add(additionalSource);
                }
            }
        }

        // This must be called.
        public static int RefreshSources(IProvider2DSources sources, GameObject gameObj, int providerType)
        {
            Provider2DSources<T, U> provider2DSources = sources as Provider2DSources<T, U>;
            Debug.Assert(provider2DSources != null);

            provider2DSources.m_Sources.Clear();

            if (provider2DSources.m_Provider2DCache == null)
                provider2DSources.m_Provider2DCache = new Provider2DCache<T>();

            provider2DSources.m_Provider2DCache.UpdateCache(gameObj);
                
            // Combine additional sources with ones from the cache.
            foreach (var kvp in provider2DSources.m_Provider2DCache.Cache)
            {
                if (kvp.m_Key.m_Component != null)
                {
                    U source = (U)Activator.CreateInstance(typeof(U));
                    source.Initialize(kvp.m_Value.m_Provider, kvp.m_Key.m_Component, providerType);
                    provider2DSources.m_Sources.Add(source);
                }
            }

            AddAddtionalSources(provider2DSources);

            DeconflictNames(provider2DSources.m_Sources); // This has to be done because unfortunately the dropdown which supports GUIContent lacks the robustness to handle duplicate names.

            provider2DSources.m_Sources.Sort((a, b) => b.m_MenuPriority.CompareTo(a.m_MenuPriority));

            // Set the current source index
            return GetSelectedSourceIndexFromId(provider2DSources);
        }

        public static void UpdateSelectionFromIndex(IProvider2DSources sources, int index)
        {
            Provider2DSources<T, U> provider2DSources = sources as Provider2DSources<T, U>;
            Debug.Assert(provider2DSources != null);

            if (index >= 0)
            {
                provider2DSources.m_SelectedSource = provider2DSources.m_Sources[index];
                provider2DSources.m_SelectedHashCode = provider2DSources.m_Sources[index].GetHashCode();
            }
            else
            {
                provider2DSources.m_SelectedSource = null;
                provider2DSources.m_SelectedHashCode = 0;
            }
        }

        public GUIContent[] GetSourceNames()
        {
            GUIContent[] content = new GUIContent[m_Sources.Count];
            for(int i = 0; i < content.Length; i++)
            {
                content[i] = m_Sources[i].GetSourceName();
            }
            return content;
        }

        public List<SelectionSource> GetAdditionalSources()
        {
            return m_AdditionalSources;
        }

         public int selectedHashCode
        {
            get { return m_SelectedHashCode; }
            set { m_SelectedHashCode = value; }
        }

        static public void SetAdditionalSources(SerializedProperty property, List<SelectionSource> additionalSources)
        {
            Provider2DSources<T, U> sources = property.boxedValue as Provider2DSources<T, U>;
            if (sources.m_Sources != null)
            {
                sources.m_AdditionalSources.Clear();
                foreach (var additionalSource in additionalSources)
                    sources.m_AdditionalSources.Add(additionalSource);
            }

            property.boxedValue = sources;
            property.serializedObject.ApplyModifiedProperties();
        }

        static public void SetSourceType(SerializedProperty property)
        {
            property.serializedObject.Update();

            Provider2DSources<T, U> sources = property.boxedValue as Provider2DSources<T, U>;
            if (sources.m_SelectedSource != null)
                sources.m_SelectedSource.SetSourceType(property.serializedObject);

            property.serializedObject.ApplyModifiedProperties();
        }
        public static void DrawSelectedSourceUI(SerializedProperty property)
        {
            property.serializedObject.Update();

            Provider2DSources<T, U> sources = property.boxedValue as Provider2DSources<T, U>;
            if (sources.m_SelectedSource != null)
                sources.m_SelectedSource.DrawUI(property, property.serializedObject, property.serializedObject.targetObjects);

            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
