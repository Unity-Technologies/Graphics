using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace UnityEditor.Rendering.Universal
{
    internal class CastingSourceDropDown
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

        class SelectionData
        {
            public SerializedObject         shadowCaster;
            public ShadowShape2DProvider    newShadowShapeProvider;
            public Component                newShadowShapeComponent;
            public int                      newCastingSource;

            public SelectionData(int inNewCastingSource, ShadowShape2DProvider inNewShapeProvider, Component inNewShadowShapeComponent, SerializedObject inShadowCaster)
            {
                shadowCaster = inShadowCaster;
                newShadowShapeProvider = inNewShapeProvider;
                newShadowShapeComponent = inNewShadowShapeComponent;
                newCastingSource = inNewCastingSource;
            }
        }

        struct ProviderComparer : IComparer<ShadowShapeProviderData>
        {
            public int Compare(ShadowShapeProviderData a, ShadowShapeProviderData b)
            {
                return b.provider.Priority() - a.provider.Priority();
            }
        }

        static public List<ShadowShapeProviderData> GetShadowShapeProviders(GameObject go)
        {
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

            var results = new List<ShadowShapeProviderData>();
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

            return results;
        }

        void OnMenuOptionSelected(object layerSelectionDataObject)
        {
            SelectionData selectionData = (SelectionData)layerSelectionDataObject;

            SerializedProperty shapeProvider = selectionData.shadowCaster.FindProperty("m_ShadowShape2DProvider");
            SerializedProperty shapeComponent = selectionData.shadowCaster.FindProperty("m_ShadowShape2DComponent");
            SerializedProperty castingSource = selectionData.shadowCaster.FindProperty("m_ShadowCastingSource");

            selectionData.shadowCaster.Update();
            castingSource.intValue  = selectionData.newCastingSource;
            shapeProvider.managedReferenceValue = selectionData.newShadowShapeProvider;
            shapeComponent.objectReferenceValue = selectionData.newShadowShapeComponent;
            selectionData.shadowCaster.ApplyModifiedProperties();
        }

        string GetCompactTypeName(Component component)
        {
            string type = component.GetType().ToString();
            int lastIndex = type.LastIndexOf('.');
            string compactTypeName = lastIndex < 0 ? type : type.Substring(lastIndex + 1);

            return ObjectNames.NicifyVariableName(compactTypeName);
        }

        public void OnCastingSource(SerializedObject serializedObject, UnityEngine.Object[] targets, GUIContent labelContent)
        {
            Rect totalPosition = EditorGUILayout.GetControlRect();
            Rect position = EditorGUI.PrefixLabel(totalPosition, labelContent);
            if (targets.Length <= 1)
            {
                ShadowCaster2D shadowCaster = targets[0] as ShadowCaster2D;

                // Check for the current value
                GUIContent selected = new GUIContent("None");
                if (shadowCaster.shadowCastingSource == ShadowCaster2D.ShadowCastingSources.ShapeEditor)
                    selected = new GUIContent("ShapeEditor");
                else if (shadowCaster.shadowCastingSource == ShadowCaster2D.ShadowCastingSources.ShapeProvider && shadowCaster.shadowShape2DProvider != null)
                {
                    if (shadowCaster.shadowShape2DComponent != null)
                        selected = new GUIContent(shadowCaster.shadowShape2DProvider.ProviderName(GetCompactTypeName(shadowCaster.shadowShape2DComponent)));
                    else
                        selected = new GUIContent("None");
                }

                // Draw the drop down menu
                if (EditorGUI.DropdownButton(position, selected, FocusType.Keyboard, EditorStyles.popup))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.allowDuplicateNames = true;

                    ProviderComparer providerComparer = new ProviderComparer();
                    List<ShadowShapeProviderData> castingSources = GetShadowShapeProviders(shadowCaster.gameObject);
                    castingSources.Sort(providerComparer);

                    for (int i = 0; i < castingSources.Count; i++)
                    {
                        string menuName = castingSources[i].provider.ProviderName(GetCompactTypeName(castingSources[i].component));
                        menu.AddItem(new GUIContent(menuName), false, OnMenuOptionSelected, new SelectionData((int)ShadowCaster2D.ShadowCastingSources.ShapeProvider, castingSources[i].provider, castingSources[i].component, serializedObject));
                    }


                    menu.AddItem(new GUIContent("Shape Editor"), false, OnMenuOptionSelected, new SelectionData((int)ShadowCaster2D.ShadowCastingSources.ShapeEditor, null, null, serializedObject));
                    menu.AddItem(new GUIContent("None"), false, OnMenuOptionSelected, new SelectionData((int)ShadowCaster2D.ShadowCastingSources.None, null, null, serializedObject));


                    menu.DropDown(position);
                }
            }
            else
            {
                EditorGUI.showMixedValue = true;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.DropdownButton(position, new GUIContent(""), FocusType.Keyboard, EditorStyles.popup);
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
