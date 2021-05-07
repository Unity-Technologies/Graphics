using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// LensFlareDataSRP shows how the SRP Lens Flare Asset is shown in the UI
    /// </summary>
    [CustomEditorForRenderPipeline(typeof(LensFlareDataSRP), typeof(UnityEngine.Rendering.RenderPipelineAsset))]
    internal class LensFlareEditorSRP : Editor
    {
        SerializedProperty m_Elements;

        void OnEnable()
        {
            PropertyFetcher<LensFlareDataSRP> entryPoint = new PropertyFetcher<LensFlareDataSRP>(serializedObject);
            m_Elements = entryPoint.Find(x => x.elements);
        }

        /// <summary>
        /// Implement this function to make a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            m_Elements.serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            LensFlareDataSRP lensFlareData = m_Elements.serializedObject.targetObject as LensFlareDataSRP;
            int countBefore = lensFlareData != null && lensFlareData.elements != null ? lensFlareData.elements.Length : 0;
            EditorGUILayout.PropertyField(m_Elements, Styles.elements);
            if (EditorGUI.EndChangeCheck())
            {
                m_Elements.serializedObject.ApplyModifiedProperties();
                int countAfter = lensFlareData != null && lensFlareData.elements != null ? lensFlareData.elements.Length : 0;
                if (countAfter > countBefore)
                {
                    for (int i = countBefore; i < countAfter; ++i)
                    {
                        lensFlareData.elements[i] = new LensFlareDataElementSRP(); // Set Default values
                    }
                    m_Elements.serializedObject.Update();
                }
            }
        }

        static class Styles
        {
            static public readonly GUIContent elements = EditorGUIUtility.TrTextContent("Elements", "List of elements in the Lens Flare.");
        }
    }
}
