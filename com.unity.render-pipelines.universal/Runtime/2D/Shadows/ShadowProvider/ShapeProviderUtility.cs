using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    internal class ShapeProviderUtility
    {
        static public void CallOnBeforeRender(ShadowShape2DProvider shapeProvider, Component component, ShadowMesh2D shadowMesh, Bounds bounds)
        {
            if (component != null)
            {
                if (shapeProvider != null)
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
                if(shapeProvider != null)
                    shapeProvider.OnPersistantDataCreated(component, shadowMesh);
            }
        }


        static public void TryGetDefaultShadowShapeProviderSource(GameObject go, out Component source, out ShadowShape2DProvider provider)
        {
            source = null;
            provider = null;

            TypeCache.TypeCollection providerTypes = TypeCache.GetTypesDerivedFrom<ShadowShape2DProvider>();
            Component[] components = go.GetComponents<Component>();

            int currentPriority = int.MinValue;
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                foreach (System.Type providerType in providerTypes)
                {
                    ShadowShape2DProvider currentProvider = (ShadowShape2DProvider)ScriptableObject.CreateInstance(providerType);
                    if(currentProvider.CanProvideShape(component))
                    {
                        int menuPriority = currentProvider.MenuPriority();
                        if(menuPriority > currentPriority)
                        {
                            currentPriority = menuPriority;
                            source = component;
                            provider = currentProvider;
                        }
                    }
                }
            }
        }

    }
}
