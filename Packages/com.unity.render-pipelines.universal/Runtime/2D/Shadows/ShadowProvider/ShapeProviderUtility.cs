using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    internal class ShapeProviderUtility
    {
        static public void CallOnBeforeRender(ShadowShape2DProvider shapeProvider, Component component, ShadowMesh2D shadowMesh, Bounds bounds)
        {
            if (component != null)
            {
                if (shapeProvider != null && component.gameObject.activeInHierarchy)
                    shapeProvider.OnBeforeRender(component, bounds, shadowMesh);
            }
            else if (shadowMesh != null && shadowMesh.mesh != null)
            {
                shadowMesh.mesh.Clear();
            }
        }

        static public void PersistantDataCreated(ShadowShape2DProvider shapeProvider, Component component, ShadowMesh2D shadowMesh)
        {
            if (component != null)
            {
                if (shapeProvider != null)
                    shapeProvider.OnPersistantDataCreated(component, shadowMesh);
            }
        }

#if UNITY_EDITOR
        static public void TryGetDefaultShadowShapeProviderSource(GameObject go, out Component outSource, out ShadowShape2DProvider outProvider)
        {
            outSource = null;
            outProvider = null;

            // Create some providers to check against.
            var providerTypes = TypeCache.GetTypesDerivedFrom<ShadowShape2DProvider>();
            var providers = new List<ShadowShape2DProvider>(providerTypes.Count);
            foreach (Type providerType in providerTypes)
            {
                if (providerType.IsAbstract)
                    continue;

                providers.Add(Activator.CreateInstance(providerType) as ShadowShape2DProvider);
            }

            // Fetch the components to check.
            var components = go.GetComponents<Component>();

            var currentPriority = int.MinValue;
            foreach (var component in components)
            {
                // check each component to see if it is a valid provider
                foreach (var provider in providers)
                {
                    if (provider.IsShapeSource(component))
                    {
                        var menuPriority = provider.Priority();
                        if (menuPriority > currentPriority)
                        {
                            currentPriority = menuPriority;
                            outSource = component;
                            outProvider = provider;
                        }
                    }
                }
            }
        }

#endif
    }
}
