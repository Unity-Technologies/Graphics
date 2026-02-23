using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{

    /// <summary>
    /// Class <c>Provider2D_ProperyDrawer</c> is the default property drawer for all shadow providers and will render the providers serialized properties
    /// </summary>
    /// 
    [CustomPropertyDrawer(typeof(Provider2D), true)]
    public class Provider2D_ProperyDrawer : PropertyDrawer
    {
        delegate void ProcessChild(SerializedProperty child);

        bool IsChildVisible(Type parentType, SerializedProperty child)
        {
            // Check to see if the child is public and not hidden in the inspector
            FieldInfo fieldInfoPublic = parentType.GetField(child.name, BindingFlags.Public | BindingFlags.Instance);
            bool publicAndVisible = fieldInfoPublic != null && ((fieldInfoPublic.GetCustomAttribute<HideInInspector>() == null) || (fieldInfoPublic.GetCustomAttribute<NonSerializedAttribute>() == null));

            bool privateButVisible = false;
            if (fieldInfoPublic == null)
            {
                // Check to see if the child is a private m
                FieldInfo fieldInfoNonPublic = parentType.GetField(child.name, BindingFlags.NonPublic | BindingFlags.Instance);
                privateButVisible = fieldInfoNonPublic != null && (fieldInfoNonPublic.GetCustomAttribute<SerializeField>() != null);
            }

            return publicAndVisible || privateButVisible;
        }

        void ProcessProviderChildren(SerializedProperty parentProperty, ProcessChild onProcessChild)
        {
            if (parentProperty == null)
                return;

            // 1. Get the type and the starting depth
            Type parentType = parentProperty.boxedValue?.GetType();
            SerializedProperty iterator = parentProperty.Copy();
            int rootDepth = iterator.depth;

            // 2. The loop condition: 
            // - NextVisible(true) moves to the next property (descending into children)
            // - iterator.depth > rootDepth ensures we haven't climbed back out to a sibling
            while (iterator.NextVisible(true) && iterator.depth > rootDepth)
            {
                if (IsChildVisible(parentType, iterator))
                {
                    onProcessChild(iterator);
                }
            }
        }

        /// <summary>
        /// Gets the name to be listed in the <c>ShadowCaster2D</c> Casting Option drop down.
        /// </summary>
        /// <param name="rect"> Rectangle on the screen to use for the property GUI. </param>
        /// <param name="property"> The SerializedProperty to make the custom GUI for. </param>
        /// <param name="label"> The label of this property. </param>
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            ProcessProviderChildren(property, (SerializedProperty child) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, child, true);
                float height = EditorGUI.GetPropertyHeight(child, includeChildren: true);
                rect.y += EditorGUIUtility.standardVerticalSpacing + height;
            });
        }

        /// <summary>
        /// Gets the name to be listed in the <c>ShadowCaster2D</c> Casting Option drop down.
        /// </summary>
        /// <param name="property"> The SerializedProperty to make the custom GUI for. </param>
        /// <param name="label"> The label of this property. </param>
        /// <returns> The float height in pixels. </returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;
            ProcessProviderChildren(property, (SerializedProperty child) =>
            {
                float propHeight = EditorGUI.GetPropertyHeight(child, includeChildren: true);
                height += EditorGUIUtility.standardVerticalSpacing + propHeight;
            });

            return height;
        }
    }
}
