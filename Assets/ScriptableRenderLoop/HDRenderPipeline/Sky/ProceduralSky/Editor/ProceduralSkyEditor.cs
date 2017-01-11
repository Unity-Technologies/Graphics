using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    //[CanEditMultipleObjects]
    //[CustomEditor(typeof(ProceduralSkyParameters))]
    //public class ProceduralSkyParametersEditor
    //    : Editor
    //{
    //    private class Styles
    //    {
    //        public readonly GUIContent skyHDRI = new GUIContent("HDRI");
    //        public readonly GUIContent skyResolution = new GUIContent("Resolution");
    //        public readonly GUIContent skyExposure = new GUIContent("Exposure");
    //        public readonly GUIContent skyRotation = new GUIContent("Rotation");
    //        public readonly GUIContent skyMultiplier = new GUIContent("Multiplier");
    //    }

    //    private static Styles s_Styles = null;
    //    private static Styles styles
    //    {
    //        get
    //        {
    //            if (s_Styles == null)
    //                s_Styles = new Styles();
    //            return s_Styles;
    //        }
    //    }

    //    private SerializedProperty m_SkyHDRI;
    //    private SerializedProperty m_SkyResolution;
    //    private SerializedProperty m_SkyExposure;
    //    private SerializedProperty m_SkyMultiplier;
    //    private SerializedProperty m_SkyRotation;

    //    void OnEnable()
    //    {
    //        m_SkyHDRI = serializedObject.FindProperty("skyHDRI");
    //        m_SkyResolution = serializedObject.FindProperty("resolution");
    //        m_SkyExposure = serializedObject.FindProperty("exposure");
    //        m_SkyMultiplier = serializedObject.FindProperty("multiplier");
    //        m_SkyRotation = serializedObject.FindProperty("rotation");
    //    }

    //    public override void OnInspectorGUI()
    //    {
    //        serializedObject.Update();

    //        EditorGUILayout.PropertyField(m_SkyHDRI, styles.skyHDRI);
    //        EditorGUILayout.PropertyField(m_SkyResolution, styles.skyResolution);
    //        EditorGUILayout.PropertyField(m_SkyExposure, styles.skyExposure);
    //        EditorGUILayout.PropertyField(m_SkyMultiplier, styles.skyMultiplier);
    //        EditorGUILayout.PropertyField(m_SkyRotation, styles.skyRotation);

    //        serializedObject.ApplyModifiedProperties();
    //    }
    //}
}
