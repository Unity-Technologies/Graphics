#if USING_2DANIMATION
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(ShadowShape2DProvider_SpriteSkin))]
    internal class ShadowShape2DProvider_SpriteSkin_PropertyDrawer : ShadowShape2DProvider_ProperyDrawer
    {
        public static string k_GPUSkinningError = "Shadow Caster 2D is not compatible with GPU skinning.";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShadowShape2DProvider_SpriteSkin provider = property.managedReferenceValue as ShadowShape2DProvider_SpriteSkin;
            base.OnGUI(position, property, label);

            if (PlayerSettings.gpuSkinning)
                EditorGUILayout.HelpBox(k_GPUSkinningError, MessageType.Error);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }

    }
}
#endif
