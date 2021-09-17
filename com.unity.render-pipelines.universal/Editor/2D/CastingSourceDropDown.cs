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
            public ShadowCaster2D shadowCaster;
            public Component shapeProvider;

            public SelectionData(ShadowCaster2D caster, Component provider)
            {
                shadowCaster = caster;
                shapeProvider = provider;
            }
        }

        void OnNoneSelected(object shadowCasterObject)
        {
            ShadowCaster2D shadowCaster = (ShadowCaster2D)shadowCasterObject;
            shadowCaster.shadowCastingSource = ShadowCaster2D.ShadowCastingSources.None;
            shadowCaster.shadowShape2DProvider = null;
            EditorUtility.SetDirty(shadowCaster);
        }

        void OnShapeEditorSelected(object shadowCasterObject)
        {
            ShadowCaster2D shadowCaster = (ShadowCaster2D)shadowCasterObject;
            shadowCaster.shadowCastingSource = ShadowCaster2D.ShadowCastingSources.ShapeEditor;
            shadowCaster.shadowShape2DProvider = null;
            EditorUtility.SetDirty(shadowCaster);
        }

        void OnShapeProviderSelected(object layerSelectionDataObject)
        {
            SelectionData selectionData = (SelectionData)layerSelectionDataObject;
            selectionData.shadowCaster.shadowCastingSource = ShadowCaster2D.ShadowCastingSources.ShapeProvider;
            selectionData.shadowCaster.shadowShape2DProvider = selectionData.shapeProvider;
            EditorUtility.SetDirty(selectionData.shadowCaster);
        }

        string GetCompactTypeName(Component component)
        {
            string type = component.GetType().ToString();
            int lastIndex = type.LastIndexOf('.');
            string compactTypeName = lastIndex < 0 ? type : type.Substring(lastIndex + 1);

            return compactTypeName;
        }

        public void OnCastingSource(SerializedObject serializedObject, Object[] targets, GUIContent labelContent, System.Action<SerializedObject> selectionChangedCallback)
        {
            Rect totalPosition = EditorGUILayout.GetControlRect();
            //GUIContent actualLabel = EditorGUI.BeginProperty(totalPosition, labelContent, m_ApplyToSortingLayers);
            Rect position = EditorGUI.PrefixLabel(totalPosition, labelContent);

            if (targets.Length <= 1)
            {
                ShadowCaster2D shadowCaster = targets[0] as ShadowCaster2D;

                // Check for the current value
                GUIContent selected = new GUIContent("None");
                if(shadowCaster.shadowCastingSource == ShadowCaster2D.ShadowCastingSources.ShapeEditor)
                    selected = new GUIContent("ShapeEditor");
                else if (shadowCaster.shadowCastingSource == ShadowCaster2D.ShadowCastingSources.ShapeProvider && shadowCaster.shadowShape2DProvider != null)
                    selected = new GUIContent(GetCompactTypeName(shadowCaster.shadowShape2DProvider));


                // Draw the drop down menu
                if (EditorGUI.DropdownButton(position, selected, FocusType.Keyboard, EditorStyles.popup))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.allowDuplicateNames = true;

                    menu.AddItem(new GUIContent("None"), false, OnNoneSelected, shadowCaster);
                    menu.AddItem(new GUIContent("Shape Editor"), false, OnShapeEditorSelected, shadowCaster);

                    List<Component> castingSources = ShadowUtility.GetShadowCastingSources(shadowCaster.gameObject);
                    for (int i = 0; i < castingSources.Count; i++)
                    {
                        menu.AddItem(new GUIContent(GetCompactTypeName(castingSources[i])), false, OnShapeProviderSelected, new SelectionData(shadowCaster, castingSources[i]));
                    }


                    menu.DropDown(position);
                }
            }
            else
            {
                EditorGUI.DropdownButton(position, new GUIContent(""), FocusType.Keyboard, EditorStyles.popup);
            }
            
            //EditorGUI.EndProperty();
        }
    }
}
