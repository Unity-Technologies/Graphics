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
        static public void CallOnBeforeRender(ShadowShape2DProvider shapeProvider, Component component, ShadowMesh2D shadowMesh, Bounds bounds, Camera camera)
        {
            if (component != null)
            {
                if (shapeProvider != null && component.gameObject.activeInHierarchy)
                {
                    // Both overloads are called, the older three-argument one first.
                    //
                    // When only the Camera overload was called, a provider written against the older
                    // signature silently never ran: it pushed no shape, which on the Legacy geometry
                    // generation goes unnoticed because the geometry serialized with the caster
                    // already matches. Switch the project to Enhanced and that caster draws Legacy
                    // vertices through the enhanced shader -- the one state ShadowMesh2D calls out as
                    // producing a wrong shadow rather than a missing one.
                    //
                    // The older overload is deliberately not [Obsolete]: it is still called, so
                    // warning about it would ask every provider using it to make a change it does not
                    // have to. A provider that overrides both gets both, old first -- the accepted
                    // cost of keeping existing providers working, and reachable only by a provider
                    // that implements the same callback twice, since both base bodies are empty.
                    shapeProvider.OnBeforeRender(component, bounds, shadowMesh);
                    shapeProvider.OnBeforeRender(camera, component, bounds, shadowMesh);
                }
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
                    shapeProvider.OnInitialized(component, shadowMesh);
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

            var currentMenuPriority = int.MinValue;
            foreach (var component in components)
            {
                // check each component to see if it is a valid provider
                foreach (var provider in providers)
                {
                    if (provider.IsRequiredComponentData(component))
                    {
                        var menuPriority = provider.MenuPriority();
                        if (menuPriority > currentMenuPriority)
                        {
                            currentMenuPriority = menuPriority;
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
