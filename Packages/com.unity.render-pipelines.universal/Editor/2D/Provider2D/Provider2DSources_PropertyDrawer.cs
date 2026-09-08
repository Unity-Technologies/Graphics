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

        /// <summary>
        /// Refreshes the sources of every selected object and returns, per object, the entries it can offer.
        /// </summary>
        /// <param name="targets">The selected objects.</param>
        /// <param name="guiContent">Filled with one entry list per object, in target order.</param>
        /// <param name="selectedContent">The entry the first object that offers any is currently on, or null.</param>
        /// <param name="showMixedContent">True when the objects are not all on the same entry.</param>
        /// <returns>The entries every object can offer.</returns>
        internal List<GUIContent> CollectTargetSources(UnityEngine.Object[] targets, List<GUIContent[]> guiContent, out GUIContent selectedContent, out bool showMixedContent)
        {
            List<GUIContent> commonContent = null;
            selectedContent = null;
            showMixedContent = false;

            for (int i = 0; i < targets.Length; i++)
            {
                Component targetComponent = targets[i] as Component;
                SerializedObject serializedTarget = new SerializedObject(targetComponent);
                SerializedProperty targetProperty = serializedTarget.FindProperty("m_SelectionSources");
                Provider2DSources<T, U> targetSources = targetProperty?.boxedValue as Provider2DSources<T, U>;

                // Here we need to build a list
                GUIContent[] content = new GUIContent[0];
                int targetSelIndex = -1;
                if (targetSources != null)
                {
                    targetSelIndex = Provider2DSources<T, U>.RefreshSources(targetSources, targetComponent.gameObject, GetProviderType());
                    content = targetSources.GetSourceNames();
                }

                // Update all the content which is saved for later. A target can offer nothing at all --
                // a caster on a GameObject that carries no component any provider reads -- and then it
                // has no entry of its own to compare.
                if (content.Length > 0)
                {
                    GUIContent targetSelContent = content[targetSelIndex < 0 ? 0 : targetSelIndex];
                    if (selectedContent == null)
                        selectedContent = targetSelContent;
                    else if (targetSelContent.text.GetHashCode() != selectedContent.text.GetHashCode())
                        showMixedContent = true;
                }
                guiContent.Add(content);
                UpdateCommonContent(ref content, ref commonContent);
            }

            // No target contributed any content, so there is nothing to intersect.
            return commonContent ?? new List<GUIContent>();
        }

        /// <summary>
        /// Applies the entry the user picked to every selected object.
        /// </summary>
        /// <remarks>
        /// Each object is matched against its own entry list and written through its own SerializedObject.
        /// Writing through the shared one instead reaches only the first object for [SerializeReference]
        /// fields, and worse, copies that object's source Component onto all the others.
        /// </remarks>
        /// <param name="targets">The selected objects.</param>
        /// <param name="guiContent">The per-object entry lists that <see cref="CollectTargetSources"/> returned.</param>
        /// <param name="newSelectedContent">The entry the user picked.</param>
        internal void ApplySelection(UnityEngine.Object[] targets, List<GUIContent[]> guiContent, GUIContent newSelectedContent)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                // Now we need to find the selected item index for each of the providers
                Component targetComponent = targets[i] as Component;
                SerializedObject serializedTarget = new SerializedObject(targetComponent);
                SerializedProperty targetProperty = serializedTarget.FindProperty("m_SelectionSources");
                Provider2DSources<T, U> targetSources = targetProperty?.boxedValue as Provider2DSources<T, U>;
                if (targetSources == null)
                    continue;

                GUIContent[] savedGuiContent = guiContent[i];
                for (int j = 0; j < savedGuiContent.Length; j++)
                {
                    GUIContent content = savedGuiContent[j];
                    if (content.text.GetHashCode() == newSelectedContent.text.GetHashCode())
                    {
                        Provider2DSources<T, U>.UpdateSelectionFromIndex(targetSources, j);
                        // Persist the selection change by updating both the provider selection and m_LightType
                        targetProperty.boxedValue = targetSources;
                        Provider2DSources<T, U>.SetSourceType(targetProperty);
                        break;
                    }
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;

            property.serializedObject.Update();

            // I need 2 lists. First a list of content for each ShadowCaster2D. Second a list of common menu items
            List<GUIContent[]> guiContent = new List<GUIContent[]>();
            GUIContent selectedContent;
            bool showMixedContent;

            UnityEngine.Object[] targets = property.serializedObject.targetObjects;
            List<GUIContent> commonContent = CollectTargetSources(targets, guiContent, out selectedContent, out showMixedContent);

            if (!property.serializedObject.isEditingMultipleObjects)
                showMixedContent = false;

            // Find the selected content index in commonContent
            int selectedIndex = -1;
            if (selectedContent != null)
            {
                for (int i = 0; i < commonContent.Count; i++)
                {
                    if (commonContent[i].text.GetHashCode() == selectedContent.text.GetHashCode())
                    {
                        selectedIndex = i;
                    }
                }
            }

            EditorGUI.showMixedValue = showMixedContent;

            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = DrawDropdown(position, label, selectedIndex, commonContent.ToArray());
            EditorGUI.showMixedValue = false;

            // DrawDropdown substitutes a placeholder entry when the selected objects have nothing in
            // common, so the index the popup reports can fall outside commonContent.
            if(EditorGUI.EndChangeCheck() && newSelectedIndex >= 0 && newSelectedIndex < commonContent.Count)
            {
                ApplySelection(targets, guiContent, commonContent[newSelectedIndex]);

                // Force all Inspector windows to repaint so the Light Type dropdown updates
                InspectorWindow.RepaintAllInspectors();
            }

            EditorGUI.EndProperty();
        }
    }
}
