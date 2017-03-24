using System;
using System.Linq;
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
            case VFXValueType.kInt:
                if (m_Component.HasInt(m_Name))
                    m_Slot.Set<int>(m_Component.GetInt(m_Name));
                break;
            case VFXValueType.kUint:
                if (m_Component.HasUInt(m_Name))
                    m_Slot.Set<UInt32>(m_Component.GetUInt(m_Name));
                break;
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

        if (!VFXComponentEditor.CanSetOverride) // Hack
            return;

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
    public static bool CanSetOverride = false;

    SerializedProperty m_VFXAsset;
    SerializedProperty m_RandomSeed;
    SerializedProperty m_VFXPropertySheet;
    bool m_useNewSerializedField = false;

    private Contents m_Contents;
    private Styles m_Styles;

    private class ExposedData
    {
        public VFXOutputSlot slot;
        public List<SlotValueBinder> valueBinders = new List<SlotValueBinder>();
        public VFXUIWidget widget = null;
    }

    private List<ExposedData> m_ExposedData = new List<ExposedData>();

    // Debug Stats : To be refactored into Debug Views
    private bool m_ShowDebugStats = false;
    private enum DebugStatMode
    {
        Efficiency = 0
    }
    private DebugStatMode m_DebugStatMode = DebugStatMode.Efficiency;
    private VFXSystemComponentStat[] m_DebugStatCurrentData;
    private List<uint>[] m_DebugStatAliveHistory;
    private uint[] m_DebugStatMaxCapacity;
    private double m_DebugStatLastUpdateTime;

    private static Color[] s_DebugStatCurveColors;
    private const uint DEBUGSTAT_CURVE_COLOR_COUNT = 25;
    private const float DEBUGSTAT_UPDATE_INTERVAL = 0.033f;
    private const uint DEBUGSTAT_NUM_SAMPLES = 100;

    void OnEnable()
    {
        m_RandomSeed = serializedObject.FindProperty("m_Seed");
        m_VFXAsset = serializedObject.FindProperty("m_Asset");
        m_VFXPropertySheet = serializedObject.FindProperty("m_PropertySheet");

        InitSlots();
    }

    void OnDisable()
    {
        foreach (var exposed in m_ExposedData)
            exposed.slot.RemoveAllObservers();
    }

    private void InitSlots()
    {
        foreach (var exposed in m_ExposedData)
            exposed.slot.RemoveAllObservers();

        m_ExposedData.Clear();

        if (m_VFXAsset == null)
            return;

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
        UpdateDebugData();

        var component = (VFXComponent)target;
        var stats = VFXComponent.GetSystemComponentsStatsFilter(component);

        //Basic display BBox
        var debugColors = new Color[] { Color.magenta, Color.green, Color.cyan, Color.yellow };
        for (int iStat = 0; iStat < stats.Length; ++iStat)
        {
            var stat = stats[iStat];

            var transform = component.GetComponent<Transform>();
            var bckpMatrix = Handles.matrix;
            Handles.matrix = transform.localToWorldMatrix;
            Handles.color = debugColors[iStat % debugColors.Length];
            Handles.DrawWireCube(stat.localBounds.center, stat.localBounds.extents * 2);
            Handles.matrix = bckpMatrix;
        }

        GameObject sceneCamObj = GameObject.Find("SceneCamera");
        if (sceneCamObj != null)
        {
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            Handles.BeginGUI();
            Camera cam = sceneCamObj.GetComponent<Camera>();
            Rect windowRect = new Rect(cam.pixelWidth / 2 - 140, cam.pixelHeight - 64 , 324, 68);
            Rect debugWindowRect = new Rect(10, 28, 480, 300 + m_DebugStatCurrentData.Length * 16);
            GUI.Window(666, windowRect, DrawPlayControlsWindow, "VFX Playback Control");

            if(m_ShowDebugStats)
                GUI.Window(667, debugWindowRect, DrawDebugControlsWindow, "VFX Debug Information");

            Handles.EndGUI();
            GL.sRGBWrite = false;
        }


        CanSetOverride = true;
        foreach (var exposed in m_ExposedData)
            if (exposed.widget != null)
                exposed.widget.OnSceneGUI(SceneView.currentDrawingSceneView);
        CanSetOverride = false;
    }

    public void DrawPlayControlsWindow(int windowID)
    {
        var component = (VFXComponent)target;

        m_ShowDebugStats = GUI.Toggle(new Rect(260,0,64,16),m_ShowDebugStats, m_Contents.infoButton, EditorStyles.miniButton);

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

        // Handle click in window to avoid unselecting asset
        if (Event.current.type == EventType.mouseDown)
            Event.current.Use();
    }

    public override void OnInspectorGUI()
    {
        InitializeGUI();
        
        var component = (VFXComponent)target;

        //Asset
        GUILayout.Label(m_Contents.HeaderMain, m_Styles.InspectorHeader);

        using (new GUILayout.HorizontalScope())
        {

            EditorGUILayout.PropertyField(m_VFXAsset, m_Contents.AssetPath);
            if(GUILayout.Button(m_Contents.OpenEditor, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
            {
                VFXEditor.ShowWindow();
            }
        }

        //Seed
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

        //Fields
        GUILayout.Label(m_Contents.HeaderParameters, m_Styles.InspectorHeader);
        m_useNewSerializedField = EditorGUILayout.ToggleLeft("Enable new inspector (WIP)", m_useNewSerializedField);

        if (m_useNewSerializedField)
        {
            EditorGUI.BeginChangeCheck();
            var fields = new string[] { "m_Float", "m_Vector2f", "m_Vector3f", "m_Vector4f", "m_Texture" };
            foreach (var field in fields)
            {
                var vfxField = m_VFXPropertySheet.FindPropertyRelative(field + ".m_Array");
                if (vfxField != null)
                {
                    for (int i = 0; i < vfxField.arraySize; ++i)
                    {
                        var property = vfxField.GetArrayElementAtIndex(i);
                        var nameProperty = property.FindPropertyRelative("m_Name").stringValue;
                        var overriddenProperty = property.FindPropertyRelative("m_Overridden");
                        var valueProperty = property.FindPropertyRelative("m_Value");
                        Color previousColor = GUI.color;
                        var animated = AnimationMode.IsPropertyAnimated(target, valueProperty.propertyPath);
                        if (animated)
                        {
                            GUI.color = AnimationMode.animatedPropertyColor;
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            overriddenProperty.boolValue = EditorGUILayout.ToggleLeft(new GUIContent(nameProperty), overriddenProperty.boolValue);
                            EditorGUI.BeginDisabledGroup(!overriddenProperty.boolValue);
                            EditorGUILayout.PropertyField(valueProperty, new GUIContent(""));
                            EditorGUI.EndDisabledGroup();
                        }
                        if (animated)
                        {
                            GUI.color = previousColor;
                        }
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            serializedObject.Update();
        }
        else
        {
            //Parameters
            bool valueDirty = false;
            foreach (var exposed in m_ExposedData)
                foreach (var valueBinder in exposed.valueBinders)
                    valueDirty |= valueBinder.Update();

            foreach (var exposed in m_ExposedData)
            {
                using (new GUILayout.HorizontalScope())
                {
                    CanSetOverride = true;
                    bool showWidget = GUILayout.Toggle(exposed.widget != null, m_Contents.ToggleWidget, m_Styles.ToggleGizmo, GUILayout.Width(10));
                    if (showWidget && exposed.widget == null)
                        exposed.widget = exposed.slot.Semantics.CreateUIWidget(exposed.slot, component.transform);
                    else if (!showWidget && exposed.widget != null)
                        exposed.widget = null;

                    using (new GUILayout.VerticalScope())
                    {
                        int l = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;
                        exposed.slot.Semantics.OnInspectorGUI(exposed.slot);
                        EditorGUI.indentLevel = l;
                    }
                    CanSetOverride = false;

                    if (GUILayout.Button(m_Contents.ResetOverrides, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
                    {
                        foreach (var valueBinder in exposed.valueBinders)
                            component.ResetOverride(valueBinder.Name);
                    }
                }
            }

            if (valueDirty && !Application.isPlaying)
            {
                // TODO Do that better ?
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
                //serializedObject.SetIsDifferentCacheDirty();
                serializedObject.Update();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                InitSlots();
            }
            serializedObject.Update();
        }
    }

    private void SetPlayRate(object rate)
    {
        var component = (VFXComponent)target;
        component.playRate = (float)rate;
    }


#region Debug Window

    public void InitDebug(int count, VFXSystemComponentStat[] stats)
    {
        m_DebugStatCurrentData = stats;
        m_DebugStatAliveHistory = new List<uint>[count];
        m_DebugStatMaxCapacity = new uint[count];

        if(s_DebugStatCurveColors == null)
        {
            s_DebugStatCurveColors = new Color[DEBUGSTAT_CURVE_COLOR_COUNT];
            for(int i = 0; i < DEBUGSTAT_CURVE_COLOR_COUNT; i++)
                s_DebugStatCurveColors[i] = Color.HSVToRGB( (0.71405f+i*0.37135766f) % 1.0f , 0.5f , 1 );
        }

        for(int i = 0; i < count; i++)
        {
            m_DebugStatAliveHistory[i] = new List<uint>();
            m_DebugStatMaxCapacity[i] = 0;
        }
    }

    private void UpdateDebugData()
    {
        double time = Time.time; //EditorApplication.timeSinceStartup;

        var stats = VFXComponent.GetSystemComponentsStatsFilter((VFXComponent)target);

        // First frame, or system count changed
        if (m_DebugStatCurrentData == null || m_DebugStatCurrentData.Length != stats.Length)
        {
            InitDebug(stats.Length, stats);
        }


        if(time - m_DebugStatLastUpdateTime > DEBUGSTAT_UPDATE_INTERVAL)
        {

            for(int i = 0; i < m_DebugStatMaxCapacity.Length; i++)
            {
                if (m_DebugStatAliveHistory[i].Count > DEBUGSTAT_NUM_SAMPLES)
                {
                    m_DebugStatAliveHistory[i].RemoveAt(0);
                }
                m_DebugStatAliveHistory[i].Add(m_DebugStatCurrentData[i].alive);

                m_DebugStatMaxCapacity[i] = Math.Max(m_DebugStatCurrentData[i].capacity, m_DebugStatMaxCapacity[i]);
            }

            m_DebugStatCurrentData = stats;
            m_DebugStatLastUpdateTime = time;
        }
        else
        {
            for(int i = 0; i < m_DebugStatMaxCapacity.Length; i++)
            {
                m_DebugStatCurrentData[i].alive = Math.Max(stats[i].alive, m_DebugStatCurrentData[i].alive);
                m_DebugStatCurrentData[i].spawnRequest += stats[i].spawnRequest;
            }
        }

    }


    public void DrawDebugControlsWindow(int windowID)
    {
        using (new GUILayout.VerticalScope())
        {
            if (m_DebugStatCurrentData.Length > 0)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Debug Mode", GUILayout.Width(120));
                    m_DebugStatMode = (DebugStatMode)EditorGUILayout.EnumPopup(m_DebugStatMode);
                    GUILayout.Space(32);
                }


                GUILayout.Space(16);

                Rect curveRect;

                using (new GUILayout.HorizontalScope(GUILayout.Height(200)))
                {
                    curveRect = GUILayoutUtility.GetRect(400, 200);
                    using (new GUILayout.VerticalScope(GUILayout.Width(32)))
                    {
                        GUILayout.Label("100%");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("75%");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("50%");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("25%");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("0%");
                    }
                }

                Handles.DrawSolidRectangleWithOutline(curveRect,new Color(0.1f,0.1f,0.1f,1.0f), new Color(0.2f,0.2f,0.2f,1.0f));
                Handles.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
                Handles.DrawLine(new Vector3(curveRect.xMin, curveRect.yMin + 0.25f * curveRect.height),new Vector3(curveRect.xMax, curveRect.yMin + 0.25f * curveRect.height));
                Handles.DrawLine(new Vector3(curveRect.xMin, curveRect.yMin + 0.5f * curveRect.height),new Vector3(curveRect.xMax, curveRect.yMin + 0.5f * curveRect.height));
                Handles.DrawLine(new Vector3(curveRect.xMin, curveRect.yMin + 0.75f * curveRect.height),new Vector3(curveRect.xMax, curveRect.yMin + 0.75f * curveRect.height));
                Handles.color = Color.white;

                for(int i = 0; i < m_DebugStatMaxCapacity.Length; i++)
                {
                    Vector3 offset = new Vector3(40, 40);
                    Vector3[] points = new Vector3[m_DebugStatAliveHistory[i].Count];
                    for (int j = 0 ; j < m_DebugStatAliveHistory[i].Count; j++)
                    {
                        points[j] = new Vector3(curveRect.position.x + curveRect.width*(float)j/DEBUGSTAT_NUM_SAMPLES, curveRect.position.y + curveRect.height * (1-(float)m_DebugStatAliveHistory[i][j]/m_DebugStatMaxCapacity[i]));
                    }
                    Handles.matrix = GUI.matrix;
                    Handles.color = s_DebugStatCurveColors[i % DEBUGSTAT_CURVE_COLOR_COUNT];
                    Handles.DrawAAPolyLine(4,points);
                    Handles.color = Color.white;
                }

                GUILayout.Space(16);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("#", m_Styles.entryHeader, GUILayout.Width(32));
                    GUILayout.Label("Alive", m_Styles.entryHeader, GUILayout.Width(80));
                    GUILayout.Label("Allocated", m_Styles.entryHeader, GUILayout.Width(80));
                    GUILayout.Label("Spawned", m_Styles.entryHeader, GUILayout.Width(80));
                    GUILayout.Label("Overflow", m_Styles.entryHeader, GUILayout.Width(80));
                    GUILayout.Box(GUIContent.none, m_Styles.entryHeader, GUILayout.ExpandWidth(true));
                    GUILayout.Label("Efficiency", m_Styles.entryHeader, GUILayout.Width(80));
                }


                var allsum = m_DebugStatCurrentData.Aggregate((a, b) => new VFXSystemComponentStat { alive = a.alive + b.alive, capacity = a.capacity + b.capacity });
                var allpercentage = (float)allsum.alive / (float)allsum.capacity;

                using (new GUILayout.HorizontalScope())
                {
                    GUI.contentColor = Color.white;
                    GUILayout.Label("ALL", m_Styles.entryEven, GUILayout.Width(32));
                    GUILayout.Label(allsum.alive.ToString(), m_Styles.entryEven, GUILayout.Width(80));
                    GUILayout.Label(allsum.capacity.ToString(), m_Styles.entryEven, GUILayout.Width(80));
                    GUILayout.Box(GUIContent.none, m_Styles.entryEven, GUILayout.ExpandWidth(true));

                    float percentage = (float)allsum.alive / allsum.capacity;
                    GUI.contentColor = percentage < 0.33f ? Color.red : percentage < 0.66f ? Color.yellow : Color.green;
                    GUILayout.Label((int)(percentage*100)+"%", m_Styles.entryEven, GUILayout.Width(80));
                }

                for(int i = 0; i < m_DebugStatMaxCapacity.Length; i++)
                {
                    GUIStyle s = (i % 2 == 0) ? m_Styles.entryOdd : m_Styles.entryEven ;

                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.contentColor = s_DebugStatCurveColors[i % DEBUGSTAT_CURVE_COLOR_COUNT];
                        GUILayout.Label("#"+(i+1), s, GUILayout.Width(32));
                        GUILayout.Label(m_DebugStatCurrentData[i].alive.ToString(), s, GUILayout.Width(80));
                        GUILayout.Label(m_DebugStatCurrentData[i].capacity.ToString(), s, GUILayout.Width(80));
                        GUILayout.Label(m_DebugStatCurrentData[i].spawnRequest.ToString(), s, GUILayout.Width(80));

                        int overflow = Math.Max((int)m_DebugStatCurrentData[i].alive + (int) m_DebugStatCurrentData[i].spawnRequest - (int) m_DebugStatCurrentData[i].capacity,0);
                        GUILayout.Label(overflow.ToString(), s, GUILayout.Width(80));

                        GUILayout.Box(GUIContent.none, s, GUILayout.ExpandWidth(true));

                        float percentage = (float)m_DebugStatCurrentData[i].alive / m_DebugStatCurrentData[i].capacity;
                        GUI.contentColor = percentage < 0.33f ? Color.red : percentage < 0.66f ? Color.yellow : Color.green;
                        GUILayout.Label((int)(percentage*100)+"%", s, GUILayout.Width(80));
                    }
                }

            }
        }

        // Handle click in window to avoid unselecting asset
        if (Event.current.type == EventType.mouseDown)
            Event.current.Use();
    }

#endregion

    private class Styles
    {
        public GUIStyle InspectorHeader;
        public GUIStyle ToggleGizmo;

        public GUILayoutOption MiniButtonWidth = GUILayout.Width(48);
        public GUILayoutOption PlayControlsHeight = GUILayout.Height(24);

        public GUIStyle entryEven;
        public GUIStyle entryOdd;
        public GUIStyle entryHeader;

        public Styles()
        {
            InspectorHeader = new GUIStyle("ShurikenModuleTitle");
            InspectorHeader.fontSize = 12;
            InspectorHeader.fontStyle = FontStyle.Bold;
            InspectorHeader.contentOffset = new Vector2(2,-2);
            InspectorHeader.border = new RectOffset(4, 4, 4, 4);
            InspectorHeader.overflow = new RectOffset(4, 4, 4, 4);
            InspectorHeader.margin = new RectOffset(4, 4, 16, 8);

            Texture2D showIcon = EditorGUIUtility.Load("VisibilityIcon.png") as Texture2D;
            Texture2D hideIcon = EditorGUIUtility.Load("VisibilityIconDisabled.png") as Texture2D;

            ToggleGizmo = new GUIStyle();
            ToggleGizmo.margin = new RectOffset(0, 0, 4, 0);
            ToggleGizmo.active.background = hideIcon;
            ToggleGizmo.onActive.background = showIcon;
            ToggleGizmo.normal.background = hideIcon;
            ToggleGizmo.onNormal.background = showIcon;
            ToggleGizmo.focused.background = hideIcon;
            ToggleGizmo.onFocused.background = showIcon;
            ToggleGizmo.hover.background = hideIcon;
            ToggleGizmo.onHover.background = showIcon;

            entryEven = new GUIStyle("OL EntryBackEven");
            entryEven.margin = new RectOffset();
            entryEven.contentOffset = new Vector2();
            entryEven.padding = new RectOffset(8,0,0,0);
            entryOdd = new GUIStyle("OL EntryBackOdd");
            entryOdd.margin = new RectOffset();
            entryOdd.contentOffset = new Vector2();
            entryOdd.padding = new RectOffset(8,0,0,0);
            entryHeader = new GUIStyle(entryOdd);
            entryHeader.fontStyle = FontStyle.Bold;

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

        public GUIContent infoButton = new GUIContent("Debug",EditorGUIUtility.IconContent("console.infoicon").image);
    }
}
