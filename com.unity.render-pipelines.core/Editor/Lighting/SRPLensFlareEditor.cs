using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// SRPLensFlareEditor shows how the SRP Lens Flare Asset is shown in the UI
    /// </summary>
    [CustomEditor(typeof(SRPLensFlareData))]
    public class SRPLensFlareEditor : Editor
    {
        SerializedProperty m_Elements;

        /// <summary>
        /// Prepare the code for the UI
        /// </summary>
        public void OnEnable()
        {
            PropertyFetcher<SRPLensFlareData> entryPoint = new PropertyFetcher<SRPLensFlareData>(serializedObject);
            m_Elements = entryPoint.Find(x => x.elements);
        }

        /// <summary>
        /// Implement this function to make a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            m_Elements.serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            SRPLensFlareData lensFlareDat = m_Elements.serializedObject.targetObject as SRPLensFlareData;
            int countBefore = lensFlareDat != null && lensFlareDat.elements != null ? lensFlareDat.elements.Length : 0;
            EditorGUILayout.PropertyField(m_Elements, Styles.elements);
            if (EditorGUI.EndChangeCheck())
            {
                m_Elements.serializedObject.ApplyModifiedProperties();
                int countAfter = lensFlareDat != null && lensFlareDat.elements != null ? lensFlareDat.elements.Length : 0;
                if (countAfter > countBefore)
                {
                    for (int i = countBefore; i < countAfter; ++i)
                    {
                        lensFlareDat.elements[i] = new SRPLensFlareDataElement(); // Set Default values
                    }
                    m_Elements.serializedObject.Update();
                }
            }
        }

        sealed class Styles
        {
            static public readonly GUIContent elements = EditorGUIUtility.TrTextContent("Elements", "List of elements in the Lens Flare.");
        }
    }
}
