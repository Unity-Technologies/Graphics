using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;

using UnityEditor.VFX;
using UnityEditor.VFX.UI;


static class VFXComponentUtility
{
    static public object GetValue(this VFXComponent comp, Type type, string name)
    {
        if (type == typeof(Vector2))
        {
            return comp.GetVector2(name);
        }
        else if (type == typeof(Vector3))
        {
            return comp.GetVector3(name);
        }
        else if (type == typeof(Vector4))
        {
            return comp.GetVector4(name);
        }
        else if (type == typeof(Color))
        {
            Color c;
            Vector4 vec = comp.GetVector4(name);
            c.r = vec.x;
            c.g = vec.y;
            c.b = vec.z;
            c.a = vec.w;

            return c;
        }
        else if (type == typeof(AnimationCurve))
        {
            return comp.GetAnimationCurve(name);
        }
        else if (type == typeof(Gradient))
        {
            return comp.GetGradient(name);
        }
        else if (type == typeof(Texture2D))
        {
            return comp.GetTexture2D(name);
        }
        else if (type == typeof(Texture3D))
        {
            return comp.GetTexture3D(name);
        }
        else if (type == typeof(float))
        {
            return comp.GetFloat(name);
        }
        else if (type == typeof(int))
        {
            return comp.GetInt(name);
        }
        else if (type == typeof(uint))
        {
            return comp.GetUInt(name);
        }
        return null;
    }

    static public void SetValue(this VFXComponent comp, Type type, string name, object value)
    {
        if (type == typeof(Vector2))
        {
            comp.SetVector2(name, (Vector2)value);
        }
        else if (type == typeof(Vector3))
        {
            comp.SetVector3(name, (Vector3)value);
        }
        else if (type == typeof(Vector4))
        {
            comp.SetVector4(name, (Vector4)value);
        }
        else if (type == typeof(Color))
        {
            Color c = (Color)value;

            Vector4 vec;
            vec.x = c.r;
            vec.y = c.g;
            vec.z = c.b;
            vec.w = c.a;

            comp.SetVector4(name, vec);
        }
        else if (type == typeof(AnimationCurve))
        {
            comp.SetAnimationCurve(name, (AnimationCurve)value);
        }
        else if (type == typeof(Gradient))
        {
            comp.SetGradient(name, (Gradient)value);
        }
        else if (type == typeof(Texture2D))
        {
            comp.SetTexture2D(name, (Texture2D)value);
        }
        else if (type == typeof(Texture3D))
        {
            comp.SetTexture3D(name, (Texture3D)value);
        }
        else if (type == typeof(float))
        {
            comp.SetFloat(name, (float)value);
        }
        else if (type == typeof(int))
        {
            comp.SetInt(name, (int)value);
        }
        else if (type == typeof(uint))
        {
            comp.SetUInt(name, (uint)value);
        }
    }

    public static string GetTypeField(Type type)
    {
        if (type == typeof(Vector2))
        {
            return "m_Vector2f";
        }
        else if (type == typeof(Vector3))
        {
            return "m_Vector3f";
        }
        else if (type == typeof(Vector4))
        {
            return "m_Vector4f";
        }
        else if (type == typeof(Color))
        {
            return "m_Vector4f";
        }
        else if (type == typeof(AnimationCurve))
        {
            return "m_AnimationCurve";
        }
        else if (type == typeof(Gradient))
        {
            return "m_Gradient";
        }
        else if (type == typeof(Texture2D))
        {
            return "m_NamedObject";
        }
        else if (type == typeof(Texture3D))
        {
            return "m_NamedObject";
        }
        else if (type == typeof(Mesh))
        {
            return "m_NamedObject";
        }
        else if (type == typeof(float))
        {
            return "m_Float";
        }
        else if (type == typeof(int))
        {
            return "m_Int";
        }
        else if (type == typeof(uint))
        {
            return "m_UInt";
        }
        //Debug.LogError("unknown vfx property type:"+type.UserFriendlyName());
        return null;
    }
}

//using UnityEngine.Experimental.VFX;
/*
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
*/
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
    /*
    private class ExposedData
    {
        public VFXOutputSlot slot;
        public List<SlotValueBinder> valueBinders = new List<SlotValueBinder>();
        public VFXUIWidget widget = null;
    }

    private List<ExposedData> m_ExposedData = new List<ExposedData>();*/
    //private List<VFXOutputSlot> m_Slots = new List<VFXOutputSlot>();
    //private List<SlotValueBinder> m_ValueBinders = new List<SlotValueBinder>();

    //private VFXComponentDebugPanel m_DebugPanel;
    private bool m_ShowDebugStats = false;

    void OnEnable()
    {
        m_RandomSeed = serializedObject.FindProperty("m_StartSeed");
        m_VFXAsset = serializedObject.FindProperty("m_Asset");
        m_VFXPropertySheet = serializedObject.FindProperty("m_PropertySheet");

        InitSlots();

        m_Infos.Clear();
    }

    void OnDisable()
    {
        /*m_DebugPanel = null;
        foreach (var exposed in m_ExposedData)
            exposed.slot.RemoveAllObservers();*/
    }

    struct Infos
    {
        public VFXPropertyIM propertyIM;
        public Type type;
    }

    Dictionary<VFXParameterPresenter, Infos> m_Infos = new Dictionary<VFXParameterPresenter, Infos>();

    void OnParamGUI(VFXParameterPresenter parameter)
    {
        VFXComponent comp = (VFXComponent)target;

        string fieldName = VFXComponentUtility.GetTypeField(parameter.anchorType);


        var vfxField = m_VFXPropertySheet.FindPropertyRelative(fieldName + ".m_Array");
        SerializedProperty property = null;
        if (vfxField != null)
        {
            for (int i = 0; i < vfxField.arraySize; ++i)
            {
                property = vfxField.GetArrayElementAtIndex(i);
                var nameProperty = property.FindPropertyRelative("m_Name").stringValue;
                if (nameProperty == parameter.exposedName)
                {
                    break;
                }
                property = null;
            }
        }
        if (property != null)
        {
            SerializedProperty overrideProperty = property.FindPropertyRelative("m_Overridden");
            property = property.FindPropertyRelative("m_Value");
            string firstpropName = property.name;

            Color previousColor = GUI.color;
            var animated = AnimationMode.IsPropertyAnimated(target, property.propertyPath);
            if (animated)
            {
                GUI.color = AnimationMode.animatedPropertyColor;
            }


            EditorGUIUtility.SetBoldDefaultFont(overrideProperty.boolValue);

            EditorGUI.BeginChangeCheck();
            if (parameter.anchorType == typeof(Color))
            {
                Vector4 vVal = property.vector4Value;
                Color c = new Color(vVal.x, vVal.y, vVal.z, vVal.w);
                c = EditorGUILayout.ColorField(parameter.exposedName, c);

                if (c.r != vVal.x || c.g != vVal.y || c.b != vVal.z || c.a != vVal.w)
                    property.vector4Value = new Vector4(c.r, c.g, c.b, c.a);
            }
            else
                EditorGUILayout.PropertyField(property, new GUIContent(parameter.exposedName), true);

            if (EditorGUI.EndChangeCheck())
            {
                overrideProperty.boolValue = true;
            }

            if (animated)
            {
                GUI.color = previousColor;
            }
        }
    }

    private void InitSlots()
    {
        /*foreach (var exposed in m_ExposedData)
            exposed.slot.RemoveAllObservers();

        m_ExposedData.Clear();
        */
        if (m_VFXAsset == null)
            return;

        VFXAsset asset = m_VFXAsset.objectReferenceValue as VFXAsset;
        if (asset == null)
            return;

        VFXViewWindow.viewPresenter.SetVFXAsset(asset, false);


        /*
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
        }*/
    }

    /*
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
    */
    public void InitializeGUI()
    {
        if (m_Contents == null)
            m_Contents = new Contents();

        if (m_Styles == null)
            m_Styles = new Styles();
    }

    /*
    public void OnSceneGUI()
    {
        InitializeGUI();

        if(m_ShowDebugStats)
        {
            m_DebugPanel.UpdateDebugData();
            m_DebugPanel.OnSceneGUI();
        }

        GameObject sceneCamObj = GameObject.Find("SceneCamera");
        if (sceneCamObj != null)
        {
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            Handles.BeginGUI();
            Camera cam = sceneCamObj.GetComponent<Camera>();
            Rect windowRect = new Rect(cam.pixelWidth / 2 - 140, cam.pixelHeight - 64 , 324, 68);
            GUI.Window(666, windowRect, DrawPlayControlsWindow, "VFX Playback Control");

            if(m_ShowDebugStats)
                m_DebugPanel.OnWindowGUI();

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
    }*/

    public override void OnInspectorGUI()
    {
        InitializeGUI();

        var component = (VFXComponent)target;

        //Asset
        GUILayout.Label(m_Contents.HeaderMain, m_Styles.InspectorHeader);

        using (new GUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(m_VFXAsset, m_Contents.AssetPath);
            if (GUILayout.Button(m_Contents.OpenEditor, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
            {
                //VFXEditor.ShowWindow();
            }
        }

        //Seed
        using (new GUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(m_RandomSeed, m_Contents.RandomSeed);
            if (GUILayout.Button(m_Contents.SetRandomSeed, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
            {
                m_RandomSeed.intValue = UnityEngine.Random.Range(0, int.MaxValue);
                component.startSeed = (uint)m_RandomSeed.intValue; // As accessors are bypassed with serialized properties...
                component.Reinit();
            }
        }

        //Field
        GUILayout.Label(m_Contents.HeaderParameters, m_Styles.InspectorHeader);

        var newList = VFXViewWindow.viewPresenter.allChildren.OfType<VFXParameterPresenter>().Where(t => t.exposed).OrderBy(t => t.order).ToArray();

        foreach (var parameter in newList)
        {
            OnParamGUI(parameter);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void SetPlayRate(object rate)
    {
        var component = (VFXComponent)target;
        component.playRate = (float)rate;
    }

    private class Styles
    {
        public GUIStyle InspectorHeader;
        public GUIStyle ToggleGizmo;

        public GUILayoutOption MiniButtonWidth = GUILayout.Width(48);
        public GUILayoutOption PlayControlsHeight = GUILayout.Height(24);

        public Styles()
        {
            InspectorHeader = new GUIStyle("ShurikenModuleTitle");
            InspectorHeader.fontSize = 12;
            InspectorHeader.fontStyle = FontStyle.Bold;
            InspectorHeader.contentOffset = new Vector2(2, -2);
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

        public GUIContent infoButton = new GUIContent("Debug", EditorGUIUtility.IconContent("console.infoicon").image);
    }
}
