using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;


[CustomEditor(typeof(VFXComponent))]
public class VFXComponentEditor : Editor
{
    SerializedProperty m_VFXAsset;
    SerializedProperty m_RandomSeed;

    static Contents s_Contents;
    static Styles s_Styles;

    void OnEnable()
    {
        if (s_Contents == null)
           s_Contents = new Contents();

        if (s_Styles == null)
           s_Styles = new Styles();

        m_RandomSeed = serializedObject.FindProperty("m_Seed");
        m_VFXAsset = serializedObject.FindProperty("m_Asset");
    }

    public void OnSceneGUI()
    {
        GameObject sceneCamObj = GameObject.Find( "SceneCamera");
        if ( sceneCamObj != null )
        {
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            Handles.BeginGUI();
            Camera cam = sceneCamObj.GetComponent<Camera>();
            Rect windowRect = new Rect(cam.pixelWidth / 2 - 140, cam.pixelHeight - 80 , 280, 64);
            GUI.Window(666, windowRect, DrawPlayControlsWindow, "VFX Playback Control");
        
            Handles.EndGUI();
            GL.sRGBWrite = false;
        }
    }

    public void DrawPlayControlsWindow(int windowID)
    {
        var component = (VFXComponent)target;

        // PLAY CONTROLS
        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarRestart), EditorStyles.miniButtonLeft, s_Styles.PlayControlsHeight))
            {
                component.pause = false;
                component.Reinit();
            }

            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarPlay), EditorStyles.miniButtonMid, s_Styles.PlayControlsHeight))
                component.pause = false;

            component.pause = GUILayout.Toggle(component.pause, new GUIContent(VFXEditor.styles.ToolbarPause), EditorStyles.miniButtonMid, s_Styles.PlayControlsHeight);


            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarStop), EditorStyles.miniButtonMid, s_Styles.PlayControlsHeight))
            {
                component.pause = true;
                component.Reinit();
            }

            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarFrameAdvance), EditorStyles.miniButtonRight, s_Styles.PlayControlsHeight))
            {
                component.pause = true;
                component.AdvanceOneFrame();
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label(s_Contents.PlayRate, GUILayout.Width(54));
            // Play Rate
            float r = component.playRate;
            float nr = Mathf.Pow(GUILayout.HorizontalSlider(Mathf.Sqrt(component.playRate), 0.0f, Mathf.Sqrt(8.0f)), 2.0f);
            GUILayout.Label(Mathf.Round(nr * 100) + "%", GUILayout.Width(36));
            if (r != nr)
                SetPlayRate(nr);

            if (GUILayout.Button(s_Contents.SetPlayRate, EditorStyles.miniButton, s_Styles.MiniButtonWidth))
            {
                GenericMenu toolsMenu = new GenericMenu();
                float rate = component.playRate;
                toolsMenu.AddItem(new GUIContent("800%"), rate == 8.0f, SetPlayRate, 8.0f);
                toolsMenu.AddItem(new GUIContent("200%"), rate == 2.0f, SetPlayRate, 2.0f);
                toolsMenu.AddItem(new GUIContent("100% (RealTime)"), rate == 1.0f, SetPlayRate, 1.0f);
                toolsMenu.AddItem(new GUIContent("50%"), rate == 0.5f, SetPlayRate, 0.5f);
                toolsMenu.AddItem(new GUIContent("25%"), rate == 0.25f, SetPlayRate, 0.25f);
                toolsMenu.AddItem(new GUIContent("10%"), rate == 0.1f, SetPlayRate, 0.1f);
                toolsMenu.AddItem(new GUIContent("1%"), rate == 0.01f, SetPlayRate, 0.01f);
                toolsMenu.ShowAsContext();
            }
        }
    }

    public override void OnInspectorGUI()
    {
        var component = (VFXComponent)target;

        // ASSET CONTROL

        GUILayout.Label(s_Contents.HeaderMain, s_Styles.InspectorHeader);

        using (new GUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(m_VFXAsset, s_Contents.AssetPath);
            if(GUILayout.Button(s_Contents.OpenEditor, EditorStyles.miniButton, s_Styles.MiniButtonWidth))
            {
                VFXEditor.ShowWindow();
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(m_RandomSeed, s_Contents.RandomSeed);
            if(GUILayout.Button(s_Contents.SetRandomSeed, EditorStyles.miniButton, s_Styles.MiniButtonWidth))
            {
                m_RandomSeed.intValue = Random.Range(0, int.MaxValue);
            }
        }

        // TODO : PARAMETERS

        GUILayout.Label(s_Contents.HeaderParameters, s_Styles.InspectorHeader);
        GUILayout.Label("Still need to be done :)");
        serializedObject.ApplyModifiedProperties();
        serializedObject.Update();
    }

    private void SetPlayRate(object rate)
    {
        var component = (VFXComponent)target;
        component.playRate = (float)rate;
    }

    private class Styles
    {
        public GUIStyle InspectorHeader;

        public GUILayoutOption MiniButtonWidth = GUILayout.Width(48);
        public GUILayoutOption PlayControlsHeight = GUILayout.Height(24);

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
        public GUIContent HeaderMain = new GUIContent("VFX Asset");
        public GUIContent HeaderPlayControls = new GUIContent("Play Controls");
        public GUIContent HeaderParameters = new GUIContent("Parameters");

        public GUIContent AssetPath = new GUIContent("Asset Template");
        public GUIContent RandomSeed = new GUIContent("Random Seed");
        public GUIContent OpenEditor = new GUIContent("Edit");
        public GUIContent SetRandomSeed = new GUIContent("Reseed");
        public GUIContent SetPlayRate = new GUIContent("Set");
        public GUIContent PlayRate = new GUIContent("PlayRate");


        public GUIContent ButtonRestart = new GUIContent();
        public GUIContent ButtonPlay = new GUIContent();
        public GUIContent ButtonPause = new GUIContent();
        public GUIContent ButtonStop = new GUIContent();
        public GUIContent ButtonFrameAdvance = new GUIContent();


    }
}
