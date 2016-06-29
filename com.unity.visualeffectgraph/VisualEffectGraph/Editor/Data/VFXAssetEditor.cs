using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;


[CustomEditor(typeof(VFXAsset))]
public class VFXAssetEditor : Editor
{
    SerializedProperty m_Bounds;

    public VFXAsset safeTarget { get { return target as VFXAsset; } }

    private Contents m_Contents;
    private Styles m_Styles;

    public void InitializeGUI()
    {
        if (m_Contents == null)
           m_Contents = new Contents();

        if (m_Styles == null)
           m_Styles = new Styles();
    }

    public override void OnInspectorGUI()
    {
        InitializeGUI();
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        GUILayout.Label(m_Contents.AssetHeader, m_Styles.InspectorHeader);
        if(GUILayout.Button(m_Contents.OpenEditor, GUILayout.Height(20)))
        {
            EditorWindow.GetWindow<VFXEditor>();
        }
        EditorGUILayout.HelpBox("VFX Editor Assets have to be edited using the VFX Editor Window, please click Open Editor to open the editor", MessageType.Info);

        GUILayout.Space(20);

        GUILayout.Label(m_Contents.AssetParametersHeader, m_Styles.InspectorHeader);

        safeTarget.bounds = EditorGUILayout.BoundsField(safeTarget.bounds);

        if(EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(safeTarget);
            serializedObject.ApplyModifiedProperties();
        }


    }

    private class Styles
    {
        public GUIStyle InspectorHeader;

        public Styles()
        {
            InspectorHeader = new GUIStyle("ShurikenModuleTitle");
            InspectorHeader.fontSize = 12;
            InspectorHeader.fontStyle = FontStyle.Bold;
            InspectorHeader.contentOffset = new Vector2(2,-2);
            InspectorHeader.border = new RectOffset(4, 4, 4, 4);
            InspectorHeader.overflow = new RectOffset(4, 4, 4, 4);
            InspectorHeader.margin = new RectOffset(4, 4, 16, 8);
        }
    }

    private class Contents
    {
        public GUIContent AssetHeader = new GUIContent("VFX Asset");
        public GUIContent OpenEditor = new GUIContent("Open VFX Editor");

        public GUIContent AssetParametersHeader = new GUIContent("Asset Parameters");
        public GUIContent BoundingBox = new GUIContent("Bounding Box");

    }

}
