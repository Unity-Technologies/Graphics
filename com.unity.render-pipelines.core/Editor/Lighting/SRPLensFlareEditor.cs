using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(SRPLensFlareData))]
    public class HDIESImporterEditor : Editor
    {
        SerializedProperty m_Intensity;
        SerializedProperty m_ScaleCurve;
        SerializedProperty m_Elements;

        public void OnEnable()
        {
            PropertyFetcher<SRPLensFlareData> entryPoint = new PropertyFetcher<SRPLensFlareData>(serializedObject);
            m_Intensity = entryPoint.Find(x => x.Intensity);
            m_ScaleCurve = entryPoint.Find(x => x.ScaleCurve);
            m_Elements = entryPoint.Find(x => x.Elements);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_Intensity);
            EditorGUILayout.PropertyField(m_ScaleCurve);
            EditorGUI.BeginChangeCheck();
            SRPLensFlareData lensFlareDat = m_Elements.serializedObject.targetObject as SRPLensFlareData;
            int countBefore = lensFlareDat != null && lensFlareDat.Elements != null ? lensFlareDat.Elements.Length : 0;
            EditorGUILayout.PropertyField(m_Elements);
            if (EditorGUI.EndChangeCheck())
            {
                m_Elements.serializedObject.ApplyModifiedProperties();
                int countAfter = lensFlareDat != null && lensFlareDat.Elements != null ? lensFlareDat.Elements.Length : 0;
                if (countAfter > countBefore)
                {
                    for (int i = countBefore; i < countAfter; ++i)
                    {
                        lensFlareDat.Elements[i] = new SRPLensFlareDataElement(); // Set Default values
                    }
                    m_Elements.serializedObject.Update();
                }
            }
        }
    }
}
