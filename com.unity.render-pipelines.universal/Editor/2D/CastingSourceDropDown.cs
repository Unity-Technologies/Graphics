using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class CastingSourceDropDown
    {
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

        void OnMenuOptionSelected(object layerSelectionDataObject)
        {
            SelectionData selectionData = (SelectionData)layerSelectionDataObject;

            SerializedProperty shapeProvider = selectionData.shadowCaster.FindProperty("m_ShadowShape2DProvider");
            SerializedProperty shapeComponent = selectionData.shadowCaster.FindProperty("m_ShadowShape2DComponent");
            SerializedProperty castingSource = selectionData.shadowCaster.FindProperty("m_ShadowCastingSource");

            selectionData.shadowCaster.Update();
            castingSource.intValue  = selectionData.newCastingSource;
            shapeProvider.objectReferenceValue = (Object)selectionData.newShadowShapeProvider;
            shapeComponent.objectReferenceValue = (Object)selectionData.newShadowShapeComponent;
            selectionData.shadowCaster.ApplyModifiedProperties();
        }

        string GetCompactTypeName(Component component)
        {
            string type = component.GetType().ToString();
            int lastIndex = type.LastIndexOf('.');
            string compactTypeName = lastIndex < 0 ? type : type.Substring(lastIndex + 1);

            return ObjectNames.NicifyVariableName(compactTypeName);
        }

        public void OnCastingSource(SerializedObject serializedObject, Object[] targets, GUIContent labelContent)
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
                    selected = new GUIContent(GetCompactTypeName(shadowCaster.shadowShape2DComponent));

                // Draw the drop down menu
                if (EditorGUI.DropdownButton(position, selected, FocusType.Keyboard, EditorStyles.popup))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.allowDuplicateNames = true;

                    menu.AddItem(new GUIContent("None"), false, OnMenuOptionSelected, new SelectionData((int)ShadowCaster2D.ShadowCastingSources.None, null, null, serializedObject));
                    menu.AddItem(new GUIContent("Shape Editor"), false, OnMenuOptionSelected, new SelectionData((int)ShadowCaster2D.ShadowCastingSources.ShapeEditor, null, null, serializedObject));

                    var castingSources = ShapeProviderEditorUtility.GetShadowShapeProviders(shadowCaster.gameObject);
                    for (int i = 0; i < castingSources.Count; i++)
                    {
                        menu.AddItem(new GUIContent(GetCompactTypeName(castingSources[i].component)), false, OnMenuOptionSelected, new SelectionData((int)ShadowCaster2D.ShadowCastingSources.ShapeProvider, castingSources[i].provider, castingSources[i].component, serializedObject));
                    }


                    menu.DropDown(position);
                }
            }
            else
            {
                EditorGUI.DropdownButton(position, new GUIContent(""), FocusType.Keyboard, EditorStyles.popup);
            }
        }
    }
}
