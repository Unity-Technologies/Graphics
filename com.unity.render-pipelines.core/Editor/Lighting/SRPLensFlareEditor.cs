using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(SRPLensFlareData))]
    public class SRPLensFlareEditor : Editor
    {
        SerializedProperty m_Intensity;
        SerializedProperty m_AllowOffScreen;
        SerializedProperty m_OcclusionRadius;
        SerializedProperty m_SamplesCount;
        SerializedProperty m_ScaleCurve;
        SerializedProperty m_PositionCurve;
        SerializedProperty m_RadialAttenuationCurve;
        SerializedProperty m_DistanceAttenuationCurve;
        SerializedProperty m_Elements;

        public void OnEnable()
        {
            PropertyFetcher<SRPLensFlareData> entryPoint = new PropertyFetcher<SRPLensFlareData>(serializedObject);
            m_Intensity = entryPoint.Find(x => x.globalIntensity);
            m_OcclusionRadius = entryPoint.Find(x => x.occlusionRadius);
            m_SamplesCount = entryPoint.Find(x => x.samplesCount);
            m_AllowOffScreen = entryPoint.Find(x => x.allowOffScreen);
            m_ScaleCurve = entryPoint.Find(x => x.scaleCurve);
            m_PositionCurve = entryPoint.Find(x => x.positionCurve);
            m_RadialAttenuationCurve = entryPoint.Find(x => x.radialAttenuationCurve);
            m_DistanceAttenuationCurve = entryPoint.Find(x => x.distanceAttenuationCurve);
            m_Elements = entryPoint.Find(x => x.elements);
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Intensity);
            EditorGUILayout.PropertyField(m_AllowOffScreen);
            EditorGUILayout.PropertyField(m_OcclusionRadius);
            EditorGUILayout.PropertyField(m_SamplesCount);
            if (EditorGUI.EndChangeCheck())
            {
                m_Intensity.serializedObject.ApplyModifiedProperties();
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ScaleCurve);
            EditorGUILayout.PropertyField(m_PositionCurve);
            EditorGUILayout.PropertyField(m_RadialAttenuationCurve);
            EditorGUILayout.PropertyField(m_DistanceAttenuationCurve);
            if (EditorGUI.EndChangeCheck())
            {
                m_ScaleCurve.serializedObject.ApplyModifiedProperties();
                m_PositionCurve.serializedObject.ApplyModifiedProperties();
            }
            EditorGUI.BeginChangeCheck();
            SRPLensFlareData lensFlareDat = m_Elements.serializedObject.targetObject as SRPLensFlareData;
            int countBefore = lensFlareDat != null && lensFlareDat.elements != null ? lensFlareDat.elements.Length : 0;
            //EditorGUILayout.BeginHorizontal();
            //if (GUI.Button(EditorGUILayout.GetControlRect(), EditorGUIUtility.TrTextContent("Expand All")))
            //{
            //    //m_Elements.serializedObject.Update();
            //    for (int i = 0; i < m_Elements.arraySize; ++i)
            //    {
            //        m_Elements.GetArrayElementAtIndex(i).serializedObject.Update();
            //        SerializedProperty isFoldOpened = m_Elements.GetArrayElementAtIndex(i).FindPropertyRelative("isFoldOpened");
            //        isFoldOpened.boolValue = true;
            //        m_Elements.GetArrayElementAtIndex(i).serializedObject.ApplyModifiedProperties();
            //    }
            //    GUI.changed = true;
            //    //typeof(GUILayoutUtility).GetMethod("Layout", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            //    Repaint();
            //    //m_Elements.serializedObject.ApplyModifiedProperties();
            //    //GUILayoutUtility.Layout();
            //    //GUILayout.la
            //}
            //if (GUI.Button(EditorGUILayout.GetControlRect(), EditorGUIUtility.TrTextContent("Collapse All")))
            //{
            //    //m_Elements.serializedObject.Update();
            //    for (int i = 0; i < m_Elements.arraySize; ++i)
            //    {
            //        m_Elements.GetArrayElementAtIndex(i).serializedObject.Update();
            //        SerializedProperty isFoldOpened = m_Elements.GetArrayElementAtIndex(i).FindPropertyRelative("isFoldOpened");
            //        isFoldOpened.boolValue = false;
            //        m_Elements.GetArrayElementAtIndex(i).serializedObject.ApplyModifiedProperties();
            //    }
            //    GUI.changed = true;
            //    //typeof(GUILayoutUtility).GetMethod("Layout", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            //    Repaint();
            //    //m_Elements.serializedObject.ApplyModifiedProperties();
            //    //GUILayoutUtility.Layout();
            //}
            //EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(m_Elements);
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
    }
}
