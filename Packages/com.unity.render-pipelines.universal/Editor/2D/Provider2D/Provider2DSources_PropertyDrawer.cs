using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal abstract class Provider2DSources_PropertyDrawer<T, U> : PropertyDrawer where T : Provider2D where U : Provider2DSource
    {
        public abstract int GetProviderType();

        public int DrawDropdown(Rect popupRect, GUIContent label, int selectedIndex, GUIContent[] menuOptions)
        {
            if (menuOptions == null || menuOptions.Length == 0)
                menuOptions = new GUIContent[1] { new GUIContent("null") };

            return EditorGUI.Popup(popupRect, label, selectedIndex, menuOptions);   // Will not deal well with duplicates.
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;

            property.serializedObject.Update();
            

            Component component = property.serializedObject.targetObject as Component;
            IProvider2DSources providerSources = property.boxedValue as IProvider2DSources;
            if (providerSources != null)
            {
                bool forceUpdate = false;
                int prevSourceIndex = Provider2DSources<T, U>.RefreshSources(providerSources, component.gameObject, GetProviderType());

                // If we didn't have a previously selected source
                if(prevSourceIndex < 0)
                {
                    forceUpdate = true;
                    prevSourceIndex = 0;  // This needs to be 0 so its it correctly shown in the dropdown.
                }

                int newSourceIndex = DrawDropdown(position, label, prevSourceIndex, providerSources.GetSourceNames());

                if ((prevSourceIndex != newSourceIndex) || forceUpdate)
                {
                    Provider2DSources<T, U>.UpdateSelectionFromIndex(providerSources, newSourceIndex);
                    property.boxedValue = providerSources;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
