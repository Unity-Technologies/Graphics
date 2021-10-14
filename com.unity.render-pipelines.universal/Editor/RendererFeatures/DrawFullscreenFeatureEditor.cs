using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(DrawFullscreenPass))]
    internal class DrawFullscreenPassEditor : Editor
    {
        #region Serialized Properties
        private SerializedProperty m_InjectionPoint;
        private SerializedProperty m_RequiresNormalTexture;
        private SerializedProperty m_RequiresMotionVectorTexture;
        private SerializedProperty m_BlitMaterial;
        private SerializedProperty m_BlitMaterialPassIndex;
        private SerializedProperty m_Source;
        private SerializedProperty m_Destination;
        #endregion

        private bool m_IsInitialized = false;

        private struct Styles
        {
            public static GUIContent InjectionPoint = EditorGUIUtility.TrTextContent("Injection Point", "Specify when the pass will be executed during the rendering of a frame.");
            public static GUIContent RequiresNormalTexture = EditorGUIUtility.TrTextContent("Requires Normal texture", "Specifies whether the shader needs the world space normal texture. Keeping this option disabled will avoid the normal texture to be computed.");
            public static GUIContent RequiresMotionVectorTexture = EditorGUIUtility.TrTextContent("Requires Motion Texture", "Specifies whether the shader needs the motion texture. Keeping this option disabled will avoid the motion texture to be computed.");
            public static GUIContent BlitMaterial = new GUIContent("Material", "The material used to perform the fullscreen draw.");
            public static GUIContent BlitMaterialPassIndex = EditorGUIUtility.TrTextContent("Pass Name", "Name of the material pass to use for the fullscreen draw.");
            public static GUIContent Source = EditorGUIUtility.TrTextContent("Source", "Specifies the source texture for the blit. You can sample it from");
            public static GUIContent Destination = EditorGUIUtility.TrTextContent("Destination", ".");
        }

        private void Init()
        {
            SerializedProperty settings = serializedObject.FindProperty("m_Settings");
            m_InjectionPoint = settings.FindPropertyRelative("injectionPoint");
            m_RequiresNormalTexture = settings.FindPropertyRelative("requiresNormalTexture");
            m_RequiresMotionVectorTexture = settings.FindPropertyRelative("requiresMotionVectorTexture");
            m_BlitMaterial = settings.FindPropertyRelative("blitMaterial");
            m_BlitMaterialPassIndex = settings.FindPropertyRelative("blitMaterialPassIndex");
            m_Source = settings.FindPropertyRelative("source");
            m_Destination = settings.FindPropertyRelative("destination");
            m_IsInitialized = true;
        }

        GUIContent[] GetMaterialPassNames(Material mat)
        {
            GUIContent[] passNames = new GUIContent[mat.passCount];

            for (int i = 0; i < mat.passCount; i++)
            {
                string passName = mat.GetPassName(i);
                passNames[i] = new GUIContent(string.IsNullOrEmpty(passName) ? i.ToString() : passName);
            }

            return passNames;
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
                Init();

            EditorGUILayout.PropertyField(m_InjectionPoint, Styles.InjectionPoint);
            EditorGUILayout.PropertyField(m_RequiresNormalTexture, Styles.RequiresNormalTexture);
            EditorGUILayout.PropertyField(m_RequiresMotionVectorTexture, Styles.RequiresMotionVectorTexture);
            EditorGUILayout.PropertyField(m_BlitMaterial, Styles.BlitMaterial);
            if (m_BlitMaterial.objectReferenceValue is Material material && material?.passCount > 0)
            {
                EditorGUI.indentLevel++;

                var rect = EditorGUILayout.GetControlRect(true);
                EditorGUI.BeginProperty(rect, Styles.BlitMaterialPassIndex, m_BlitMaterialPassIndex);

                EditorGUI.BeginChangeCheck();
                int index = m_BlitMaterialPassIndex.intValue;

                if (index >= material.passCount)
                    index = material.passCount - 1;

                index = EditorGUI.IntPopup(rect, Styles.BlitMaterialPassIndex, index, GetMaterialPassNames(material), Enumerable.Range(0, material.passCount).ToArray());
                if (EditorGUI.EndChangeCheck())
                    m_BlitMaterialPassIndex.intValue = index;

                EditorGUI.EndProperty();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(m_Source, Styles.Source);
            EditorGUILayout.PropertyField(m_Destination, Styles.Destination);
        }
    }
}
