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
            int countBefore = lensFlareDat != null && lensFlareDat.Elements != null ? lensFlareDat.Elements.Count : 0;
            EditorGUILayout.PropertyField(m_Elements);
            if (EditorGUI.EndChangeCheck())
            {
                m_Elements.serializedObject.ApplyModifiedProperties();
                int countAfter = lensFlareDat != null && lensFlareDat.Elements != null ? lensFlareDat.Elements.Count : 0;
                Debug.Log($"8888_Before: {countBefore}, After: {countAfter}");
                if (countAfter > countBefore)
                {
                    Debug.Log($"Before: {countBefore}, After: {countAfter}");
                    for (int i = countBefore; i < countAfter; ++i)
                    {
                        SRPLensFlareDataElement element = lensFlareDat.Elements[i];
                        element.Intensity = 1.0f;
                        element.LensFlareTexture = null;
                        element.SizeX = 0.1f;
                        element.SizeY = 0.1f;
                        element.Rotation = 0.0f;
                        element.Tint = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                        element.Speed = 1.0f;
                    }
                    m_Elements.serializedObject.Update();
                }
            }
        }
    }
}
