using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{

    /// <summary>
    /// Class <c>ShadowShape2DProvider_ProperyDrawer</c> is the default property drawer for all shadow providers and will render the providers serialized properties
    /// </summary>
    [CustomPropertyDrawer(typeof(ShadowShape2DProvider), true)]
    public class ShadowShape2DProvider_ProperyDrawer : PropertyDrawer
    {
        delegate void ProcessChild(SerializedProperty child);

        void ProcessChildren(SerializedProperty parentProperty, ProcessChild onProcessChild)
        {
            var enumerator = parentProperty.GetEnumerator();
            while (enumerator.MoveNext())
            {
                SerializedProperty child = enumerator.Current as SerializedProperty;
                if (child != null)
                {
                    onProcessChild(child);
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
            ProcessChildren(property, (SerializedProperty child) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, child);
                rect.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
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
            ProcessChildren(property, (SerializedProperty child) =>
            {
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
            });

            return height;
        }
    }
}
