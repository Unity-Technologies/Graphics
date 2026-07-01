using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
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


        public void UpdateCommonContent(ref GUIContent[] content, ref List<GUIContent> commonContent)
        {
            List<GUIContent> returnContent = new List<GUIContent>();
            if (commonContent == null)
            {
                for(int i=0;i < content.Length;i++)
                    returnContent.Add(content[i]);
            }
            else
            {
                for (int i = 0; i < commonContent.Count; i++)
                {
                    GUIContent curContent = commonContent[i];
                    for (int j=0;j < content.Length; j++)
                    {
                        if (curContent.text.GetHashCode() == content[j].text.GetHashCode())
                        {
                            returnContent.Add(curContent);
                        }
                    }
                }
            }
            commonContent = returnContent;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;

            property.serializedObject.Update();


            // I need 2 lists. First a list of content for each ShadowCaster2D. Second a list of common menu items 
            List<GUIContent[]> guiContent = new List<GUIContent[]>();
            List<GUIContent> commonContent = null;
            GUIContent selectedContent = null;
            bool showMixedContent = false;

            UnityEngine.Object[] targets = property.serializedObject.targetObjects;
            for(int i=0;i<targets.Length;i++)
            {
                Component targetComponent = targets[i] as Component;
                SerializedObject serializedTarget = new SerializedObject(targetComponent);
                SerializedProperty targetProperty = serializedTarget.FindProperty("m_SelectionSources");
                Provider2DSources<T, U> targetSources = targetProperty.boxedValue as Provider2DSources<T, U>;

                // Here we need to build a list
                int targetSelIndex = Provider2DSources<T, U>.RefreshSources(targetSources, targetComponent.gameObject, GetProviderType());
                GUIContent[] content = targetSources.GetSourceNames();

                // Update all the content which is saved for later
                if (selectedContent == null)
                {
                    selectedContent = content[targetSelIndex < 0 ? 0 : targetSelIndex];
                }
                else
                {
                    if (content[targetSelIndex < 0 ? 0 : targetSelIndex].text.GetHashCode() != selectedContent.text.GetHashCode())
                        showMixedContent = true;
                    
                }
                guiContent.Add(content);
                UpdateCommonContent(ref content, ref commonContent);
            }

            if (!property.serializedObject.isEditingMultipleObjects)
                showMixedContent = false;


            // Find the selected content index in commonContent
            int selectedIndex = -1;
            for(int i = 0; i < commonContent.Count; i++)
            {
                if (commonContent[i].text.GetHashCode() == selectedContent.text.GetHashCode())
                {
                    selectedIndex = i;
                }
            }


            EditorGUI.showMixedValue = showMixedContent;

            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = DrawDropdown(position, label, selectedIndex, commonContent.ToArray());
            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                GUIContent newSelectedContent = commonContent[newSelectedIndex];
                for (int i = 0; i < targets.Length; i++)
                {
                    // Now we need to find the selected item index for each of the providers
                    Component targetComponent = targets[i] as Component;
                    SerializedObject serializedTarget = new SerializedObject(targetComponent);
                    SerializedProperty targetProperty = serializedTarget.FindProperty("m_SelectionSources");
                    Provider2DSources<T, U> targetSources = targetProperty.boxedValue as Provider2DSources<T, U>;

                    GUIContent[] savedGuiContent = guiContent[i];
                    for (int j=0; j < savedGuiContent.Length; j++)
                    {
                        GUIContent content = savedGuiContent[j];
                        if(content.text.GetHashCode() == newSelectedContent.text.GetHashCode())
                        {
                            Provider2DSources<T, U>.UpdateSelectionFromIndex(targetSources, j);
                            // Persist the selection change by updating both the provider selection and m_LightType
                            targetProperty.boxedValue = targetSources;
                            Provider2DSources<T, U>.SetSourceType(targetProperty);
                        }
                    }
                }

                // Force all editor views to repaint so the Light Type dropdown updates.
                // (Trunk uses InspectorWindow.RepaintAllInspectors() but that type is not
                // accessible from the URP editor assembly on 6000.5/staging.)
                InternalEditorUtility.RepaintAllViews();
            }

            EditorGUI.EndProperty();
        }
    }
}
