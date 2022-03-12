using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class ShapeProviderEditorUtility
    {
        public struct ShadowShapeProviderData
        {
            public Component component;
            public ShadowShape2DProvider provider;

            public ShadowShapeProviderData(Component inComponent, ShadowShape2DProvider inProvider)
            {
                component = inComponent;
                provider = inProvider;
            }
        }

        static public List<ShadowShapeProviderData> GetShadowShapeProviders(GameObject go)
        {
            TypeCache.TypeCollection providerTypes = TypeCache.GetTypesDerivedFrom<ShadowShape2DProvider>();

            Component[] components = go.GetComponents<Component>();
            List<ShadowShapeProviderData> retList = new List<ShadowShapeProviderData>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                // check each component to see if it is a valid provider
                foreach (System.Type providerType in providerTypes)
                {
                    if (!providerType.IsAbstract)
                    {
                        ShadowShape2DProvider provider = (ShadowShape2DProvider)ScriptableObject.CreateInstance(providerType);
                        if (provider.CanProvideShape(component))
                        {
                            retList.Add(new ShadowShapeProviderData(component, provider));
                        }
                    }
                }
            }

            return retList;
        }

    }
}
