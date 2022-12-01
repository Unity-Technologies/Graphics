using System;
using System.Collections.Generic;
using UnityEngine;
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
            var results = new List<ShadowShapeProviderData>();

            // Create some providers to check against.
            var providerTypes = TypeCache.GetTypesDerivedFrom<ShadowShape2DProvider>();
            var providers = new List<ShadowShape2DProvider>(providerTypes.Count);

            if (providerTypes.Count > 0)
            {
                foreach (Type providerType in providerTypes)
                {
                    if (providerType.IsAbstract)
                        continue;

                    providers.Add(Activator.CreateInstance(providerType) as ShadowShape2DProvider);
                }

                // Fetch the components to check.
                var components = go.GetComponents<Component>();

                foreach (var component in components)
                {
                    // check each component to see if it is a valid provider
                    foreach (var provider in providers)
                    {
                        if (provider.IsShapeSource(component))
                        {
                            results.Add(new ShadowShapeProviderData(component, Activator.CreateInstance(provider.GetType()) as ShadowShape2DProvider));
                        }
                    }
                }
            }

            return results;
        }
    }
}
