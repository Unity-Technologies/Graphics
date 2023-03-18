#if USING_2DANIMATION
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ShadowShape2DProvider_SpriteSkin))]
    internal class ShadowShape2DProviderEditor_SpriteSkin : Editor
    {
        private static class Styles
        {
            public static GUIContent gpuSkinningError = EditorGUIUtility.TrTextContentWithIcon("Sprite skin shadows do not work with GPU skinning enabled.", MessageType.Error);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();

            if (ShadowShapeProvider2DUtility.IsUsingGpuDeformation())
                EditorGUILayout.HelpBox(Styles.gpuSkinningError);
        }
    }
}
#endif
