using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class SlotValueBinder : VFXPropertySlotObserver
{
    public SlotValueBinder(string name, VFXComponent component, VFXPropertySlot slot)
    {
        m_Name = name;
        m_Component = component;
        m_Slot = slot;

        Update(); // Update before adding the observer in order not to receive a spurious event
        m_Slot.AddObserver(this);
    }

    public bool Update()
    {
        switch (m_Slot.ValueType)
        {
            case VFXValueType.kFloat:
                if (m_Component.HasFloat(m_Name))
                    m_Slot.Set<float>(m_Component.GetFloat(m_Name));
                break;
            case VFXValueType.kFloat2:
                if (m_Component.HasVector2(m_Name))
                    m_Slot.Set<Vector2>(m_Component.GetVector2(m_Name));
                break;
            case VFXValueType.kFloat3:
                if (m_Component.HasVector3(m_Name))
                    m_Slot.Set<Vector3>(m_Component.GetVector3(m_Name));
                break;
            case VFXValueType.kFloat4:
                if (m_Component.HasVector4(m_Name))
                    m_Slot.Set<Vector4>(m_Component.GetVector4(m_Name));
                break;
            case VFXValueType.kTexture2D:
                if (m_Component.HasTexture2D(m_Name))
                    m_Slot.Set<Texture2D>(m_Component.GetTexture2D(m_Name));
                break;
            case VFXValueType.kTexture3D:
                if (m_Component.HasTexture3D(m_Name))
                    m_Slot.Set<Texture3D>(m_Component.GetTexture3D(m_Name));
                break;
        }

        bool dirty = m_Dirty;
        m_Dirty = false;
        return dirty;
    }

    public void OnSlotEvent(VFXPropertySlot.Event type, VFXPropertySlot slot)
    {
        if (m_Slot != slot || type != VFXPropertySlot.Event.kValueUpdated)
            throw new Exception("Something wrong went on !"); // This should never happen

        switch (slot.ValueType)
        {
            case VFXValueType.kFloat:
                if (m_Component.HasFloat(m_Name))
                    m_Component.SetFloat(m_Name, m_Slot.Get<float>());
                break;
            case VFXValueType.kFloat2:
                if (m_Component.HasVector2(m_Name))
                    m_Component.SetVector2(m_Name, m_Slot.Get<Vector2>());
                break;
            case VFXValueType.kFloat3:
                if (m_Component.HasVector3(m_Name))
                    m_Component.SetVector3(m_Name, m_Slot.Get<Vector3>());
                break;
            case VFXValueType.kFloat4:
                if (m_Component.HasVector4(m_Name))
                    m_Component.SetVector4(m_Name, m_Slot.Get<Vector4>());
                break;
            case VFXValueType.kTexture2D:
                if (m_Component.HasTexture2D(m_Name))
                    m_Component.SetTexture2D(m_Name, m_Slot.Get<Texture2D>());
                break;
            case VFXValueType.kTexture3D:
                if (m_Component.HasTexture3D(m_Name))
                    m_Component.SetTexture3D(m_Name, m_Slot.Get<Texture3D>());
                break;
        }

        m_Dirty = true;
    }

    public string Name { get { return m_Name; } }

    private string m_Name;
    private VFXComponent m_Component;
    private VFXPropertySlot m_Slot;
    private bool m_Dirty = false;
}

[CustomEditor(typeof(VFXComponent))]
public class VFXComponentEditor : Editor
{
    SerializedProperty m_VFXAsset;
    SerializedProperty m_RandomSeed;

    private Contents m_Contents;
    private Styles m_Styles;

    private class ExposedData
    {
        public VFXOutputSlot slot;
        public List<SlotValueBinder> valueBinders = new List<SlotValueBinder>();
        public VFXUIWidget widget = null;
    }

    private List<ExposedData> m_ExposedData = new List<ExposedData>();
    //private List<VFXOutputSlot> m_Slots = new List<VFXOutputSlot>();
    //private List<SlotValueBinder> m_ValueBinders = new List<SlotValueBinder>(); 

    void OnEnable()
    {
        m_RandomSeed = serializedObject.FindProperty("m_Seed");
        m_VFXAsset = serializedObject.FindProperty("m_Asset");

        InitSlots();
    }

    void OnDisable()
    {
        foreach (var exposed in m_ExposedData)
            exposed.slot.RemoveAllObservers();
    }

    private void InitSlots()
    {
        if (m_VFXAsset == null)
        {
            m_ExposedData.Clear();
            return;
        }

        VFXAsset asset = m_VFXAsset.objectReferenceValue as VFXAsset;
        if (asset == null)
            return;

        int nbDescs = asset.GetNbEditorExposedDesc();
        for (int i = 0; i < nbDescs; ++i)
        {
            string semanticType = asset.GetEditorExposedDescSemanticType(i);
            string exposedName = asset.GetEditorExposedDescName(i);
            bool worldSpace = asset.GetEditorExposedDescWorldSpace(i);

            var dataBlock = VFXEditor.BlockLibrary.GetDataBlock(semanticType);
            if (dataBlock != null)
            {
                var property = new VFXProperty(dataBlock.Semantics, exposedName);
                var slot = new VFXOutputSlot(property);
                slot.WorldSpace = worldSpace;

                var exposedData = new ExposedData();
                exposedData.slot = slot;
                m_ExposedData.Add(exposedData);

                CreateValueBinders(exposedData,slot);
            }
        }
    }

    private void CreateValueBinders(ExposedData data,VFXPropertySlot slot,string parentName = "")
    {
        string name = VFXPropertySlot.AggregateName(parentName, slot.Name);
        if (slot.GetNbChildren() > 0)
            for (int i = 0; i < slot.GetNbChildren(); ++i)
            {
                var child = slot.GetChild(i);
                CreateValueBinders(data,child, name);
            }
        else
        {
            data.valueBinders.Add(new SlotValueBinder(name, (VFXComponent)target, slot));
        }
    }

    public void InitializeGUI()
    {
        if (m_Contents == null)
           m_Contents = new Contents();

        if (m_Styles == null)
           m_Styles = new Styles();
    }

    public void OnSceneGUI()
    {
        InitializeGUI();

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

        foreach (var exposed in m_ExposedData)
            if (exposed.widget != null)
                exposed.widget.OnSceneGUI(SceneView.currentDrawingSceneView);
    }

    public void DrawPlayControlsWindow(int windowID)
    {
        var component = (VFXComponent)target;

        // PLAY CONTROLS
        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarRestart), EditorStyles.miniButtonLeft, m_Styles.PlayControlsHeight))
            {
                component.pause = false;
                component.Reinit();
            }

            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarPlay), EditorStyles.miniButtonMid, m_Styles.PlayControlsHeight))
                component.pause = false;

            component.pause = GUILayout.Toggle(component.pause, new GUIContent(VFXEditor.styles.ToolbarPause), EditorStyles.miniButtonMid, m_Styles.PlayControlsHeight);


            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarStop), EditorStyles.miniButtonMid, m_Styles.PlayControlsHeight))
            {
                component.pause = true;
                component.Reinit();
            }

            if (GUILayout.Button(new GUIContent(VFXEditor.styles.ToolbarFrameAdvance), EditorStyles.miniButtonRight, m_Styles.PlayControlsHeight))
            {
                component.pause = true;
                component.AdvanceOneFrame();
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label(m_Contents.PlayRate, GUILayout.Width(54));
            // Play Rate
            float r = component.playRate;
            float nr = Mathf.Pow(GUILayout.HorizontalSlider(Mathf.Sqrt(component.playRate), 0.0f, Mathf.Sqrt(8.0f)), 2.0f);
            GUILayout.Label(Mathf.Round(nr * 100) + "%", GUILayout.Width(36));
            if (r != nr)
                SetPlayRate(nr);

            if (GUILayout.Button(m_Contents.SetPlayRate, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
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
        InitializeGUI();
        
        var component = (VFXComponent)target;

        // ASSET CONTROL

        GUILayout.Label(m_Contents.HeaderMain, m_Styles.InspectorHeader);

        using (new GUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(m_VFXAsset, m_Contents.AssetPath);
            if(GUILayout.Button(m_Contents.OpenEditor, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
            {
                VFXEditor.ShowWindow();
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(m_RandomSeed, m_Contents.RandomSeed);
            if(GUILayout.Button(m_Contents.SetRandomSeed, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
            {
                m_RandomSeed.intValue = UnityEngine.Random.Range(0, int.MaxValue);
                component.seed = (uint)m_RandomSeed.intValue; // As accessors are bypassed with serialized properties...
                component.Reinit();
            }
        }

        // Update parameters
        bool valueDirty = false;
        foreach (var exposed in m_ExposedData)
            foreach (var valueBinder in exposed.valueBinders)
                valueDirty |= valueBinder.Update();

        GUILayout.Label(m_Contents.HeaderParameters, m_Styles.InspectorHeader);
        foreach (var exposed in m_ExposedData)
        {
            using (new GUILayout.HorizontalScope())
            {
                bool showWidget = GUILayout.Toggle(exposed.widget != null, m_Contents.ToggleWidget);
                if (showWidget && exposed.widget == null)
                    exposed.widget = exposed.slot.Semantics.CreateUIWidget(exposed.slot, component.transform);
                else if (!showWidget && exposed.widget != null)
                    exposed.widget = null;

                if (GUILayout.Button(m_Contents.ResetOverrides, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
                {
                    foreach (var valueBinder in exposed.valueBinders)
                        component.ResetOverride(valueBinder.Name);
                }            

                exposed.slot.Semantics.OnInspectorGUI(exposed.slot);
            }
        }

        if (valueDirty)
        {
            // TODO Do that better ?
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            serializedObject.SetIsDifferentCacheDirty();
            serializedObject.Update();
        }

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
        public GUIContent ResetOverrides = new GUIContent("Reset");

        public GUIContent ButtonRestart = new GUIContent();
        public GUIContent ButtonPlay = new GUIContent();
        public GUIContent ButtonPause = new GUIContent();
        public GUIContent ButtonStop = new GUIContent();
        public GUIContent ButtonFrameAdvance = new GUIContent();

        public GUIContent ToggleWidget = new GUIContent();
    }
}
