using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.VFX;

using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using UnityEditor.Experimental.UIElements.GraphView;
using EditMode = UnityEditorInternal.EditMode;
using UnityObject = UnityEngine.Object;
using System.Reflection;
namespace UnityEditor.VFX
{
    public static class VisualEffectControl
    {
        public static void ControlStop(this VisualEffect effect)
        {
            effect.Reinit();
            effect.pause = true;
        }

        public static void ControlPlayPause(this VisualEffect effect)
        {
            effect.pause = !effect.pause;
        }

        public static void ControlStep(this VisualEffect effect)
        {
            effect.pause = true;
            effect.AdvanceOneFrame();
        }

        public static void ControlRestart(this VisualEffect effect)
        {
            effect.Reinit();
            effect.pause = false;
        }

        public const float minSlider = 1;
        public const float maxSlider = 4000;

        public const float playRateToValue = 100.0f;
        public const float valueToPlayRate = 1.0f / playRateToValue;


        public const float sliderPower = 10;

        public static readonly int[] setPlaybackValues = new int[] { 1, 10, 50, 100, 200, 500, 1000, 4000 };
    }

    static class VisualEffectEditorStyles
    {
        static GUIContent[] m_Icons;

        public enum Icon
        {
            Pause,
            Play,
            Restart,
            Step,
            Stop
        }

        public static readonly GUIStyle toggleStyle;

        static VisualEffectEditorStyles()
        {
            toggleStyle = new GUIStyle("ShurikenCheckMark");
            m_Icons = new GUIContent[1 + (int)Icon.Stop];
            for (int i = 0; i <= (int)Icon.Stop; ++i)
            {
                Icon icon = (Icon)i;
                string name = icon.ToString();

                //TODO replace with editor default resource call when going to trunk
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFXEditor/Editor/SceneWindow/Textures/" + name + ".png");
                if (texture == null)
                {
                    Debug.LogError("Can't find icon for " + name + " in VisualEffectEditorStyles");
                    continue;
                }
                m_Icons[i] = new GUIContent(texture);
            }
        }

        public static GUIContent GetIcon(Icon icon)
        {
            return m_Icons[(int)icon];
        }
    }

    static class VisualEffectUtility
    {
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
            else if (type == typeof(Texture2DArray))
            {
                return "m_NamedObject";
            }
            else if (type == typeof(Texture3D))
            {
                return "m_NamedObject";
            }
            else if (type == typeof(Cubemap))
            {
                return "m_NamedObject";
            }
            else if (type == typeof(CubemapArray))
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
                return "m_Uint";
            }
            else if (type == typeof(bool))
            {
                return "m_Bool";
            }
            else if (type == typeof(Matrix4x4))
            {
                return "m_Matrix4x4f";
            }
            //Debug.LogError("unknown vfx property type:"+type.UserFriendlyName());
            return null;
        }
    }
    public class VisualEffectEditor : Editor
    {
        public static bool CanSetOverride = false;

        SerializedProperty m_VisualEffectAsset;
        SerializedProperty m_ReseedOnPlay;
        SerializedProperty m_RandomSeed;
        SerializedProperty m_VFXPropertySheet;
        bool m_useNewSerializedField = false;

        private Contents m_Contents;
        private Styles m_Styles;
        private bool m_ShowDebugStats = false;

        protected void OnEnable()
        {
            m_RandomSeed = serializedObject.FindProperty("m_StartSeed");
            m_ReseedOnPlay = serializedObject.FindProperty("m_ResetSeedOnPlay");
            m_VisualEffectAsset = serializedObject.FindProperty("m_Asset");
            m_VFXPropertySheet = serializedObject.FindProperty("m_PropertySheet");

            m_Infos.Clear();
        }
        protected void OnDisable()
        {
            VisualEffect effect = ((VisualEffect)targets[0]);
            if (effect != null)
            {
                effect.pause = false;
                effect.playRate = 1.0f;
            }
        }

        struct Infos
        {
            public VFXPropertyIM propertyIM;
            public Type type;
        }

        Dictionary<VFXParameterNodeController, Infos> m_Infos = new Dictionary<VFXParameterNodeController, Infos>();

        struct FieldData
        {
            public System.Type type;
            public string exposedName;
            public string fieldName;
            public RangeAttribute rangeAttribute;
        }

        protected const float overrideWidth = 16;

        void DisplayProperty(VFXGraph.ParameterInfo parameter, SerializedProperty overrideProperty, SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();

            GUIContent nameContent = EditorGUIUtility.TextContent(parameter.name);

            if (!overrideProperty.hasMultipleDifferentValues)
            {
                bool result = EditorGUILayout.Toggle(overrideProperty.boolValue, VisualEffectEditorStyles.toggleStyle, GUILayout.Width(overrideWidth));

                if (overrideProperty.boolValue != result)
                {
                    overrideProperty.boolValue = result;
                }
            }
            else
            {
                //TODO what to do with multiple value
            }

            EditorGUI.BeginChangeCheck();
            if (parameter.min != Mathf.NegativeInfinity && parameter.max != Mathf.Infinity)
            {
                if (property.propertyType == SerializedPropertyType.Float)
                    EditorGUILayout.Slider(property, parameter.min, parameter.max, EditorGUIUtility.TextContent(parameter.name));
                else
                    EditorGUILayout.IntSlider(property, (int)parameter.min, (int)parameter.max, EditorGUIUtility.TextContent(parameter.name));
            }
            else if (property.propertyType == SerializedPropertyType.Color)
            {
                Vector4 vVal = property.vector4Value;
                Color c = new Color(vVal.x, vVal.y, vVal.z, vVal.w);
                c = EditorGUILayout.ColorField(nameContent, c, true, true, true);

                if (c.r != vVal.x || c.g != vVal.y || c.b != vVal.z || c.a != vVal.w)
                    property.vector4Value = new Vector4(c.r, c.g, c.b, c.a);
            }
            else if (property.propertyType == SerializedPropertyType.Vector4)
            {
                var oldVal = property.vector4Value;
                var newVal = EditorGUILayout.Vector4Field(nameContent, oldVal);
                if (oldVal.x != newVal.x || oldVal.y != newVal.y || oldVal.z != newVal.z || oldVal.w != newVal.w)
                {
                    property.vector4Value = newVal;
                }
            }
            else
            {
                EditorGUILayout.PropertyField(property, nameContent, true);
            }
            if (EditorGUI.EndChangeCheck())
            {
                overrideProperty.boolValue = true;
            }

            EditorGUILayout.EndHorizontal();
        }
        public void InitializeGUI()
        {
            if (m_Contents == null)
                m_Contents = new Contents();

            if (m_Styles == null)
                m_Styles = new Styles();
        }

        private void SceneViewGUICallback(UnityObject target, SceneView sceneView)
        {
            VisualEffect effect = ((VisualEffect)targets[0]);

            var buttonWidth = GUILayout.Width(50);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Stop), buttonWidth))
            {
                effect.ControlStop();
            }
            if (effect.pause)
            {
                if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Play), buttonWidth))
                {
                    effect.ControlPlayPause();
                }
            }
            else
            {
                if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Pause), buttonWidth))
                {
                    effect.ControlPlayPause();
                }
            }


            if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Step), buttonWidth))
            {
                effect.ControlStep();
            }
            if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Restart), buttonWidth))
            {
                effect.ControlRestart();
            }
            GUILayout.EndHorizontal();

            float playRate = effect.playRate * VisualEffectControl.playRateToValue;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Playback Rate", GUILayout.Width(84));
            playRate = EditorGUILayout.PowerSlider("", playRate, VisualEffectControl.minSlider, VisualEffectControl.maxSlider, VisualEffectControl.sliderPower, GUILayout.Width(138));
            effect.playRate = playRate * VisualEffectControl.valueToPlayRate;
            if (EditorGUILayout.DropdownButton(EditorGUIUtility.TextContent("Set"), FocusType.Passive, GUILayout.Width(36)))
            {
                GenericMenu menu = new GenericMenu();
                Rect buttonRect = GUILayoutUtility.topLevel.GetLast();
                foreach (var value in VisualEffectControl.setPlaybackValues)
                {
                    menu.AddItem(EditorGUIUtility.TextContent(string.Format("{0}%", value)), false, SetPlayRate, value);
                }
                menu.DropDown(buttonRect);
            }
            GUILayout.EndHorizontal();
        }

        void SetPlayRate(object value)
        {
            float rate = (float)((int)value)  * VisualEffectControl.valueToPlayRate;
            VisualEffect effect = ((VisualEffect)targets[0]);
            effect.playRate = rate;
        }

        protected virtual void OnSceneGUI()
        {
            SceneViewOverlay.Window(ParticleSystemInspector.playBackTitle, SceneViewGUICallback, (int)SceneViewOverlay.Ordering.ParticleEffect, SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);
        }

        private VisualEffectAsset m_asset;
        private VFXGraph m_graph;


        protected virtual void EmptyLineControl(string name, int depth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(overrideWidth + 4); // the 4 is so that Labels are aligned with elements having an override toggle.
            EditorGUILayout.LabelField(name);
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            InitializeGUI();

            var component = (VisualEffect)target;

            //Asset
            GUILayout.Label(m_Contents.HeaderMain, m_Styles.InspectorHeader);

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(m_VisualEffectAsset, m_Contents.AssetPath);

                GUI.enabled = component.visualEffectAsset != null; // Enabled state will be kept for all content until the end of the inspectorGUI.
                if (GUILayout.Button(m_Contents.OpenEditor, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
                {
                    VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

                    window.LoadAsset(component.visualEffectAsset);
                }
            }

            //Seed
            EditorGUI.BeginChangeCheck();
            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(m_ReseedOnPlay.boolValue))
                {
                    EditorGUILayout.PropertyField(m_RandomSeed, m_Contents.RandomSeed);
                    if (GUILayout.Button(m_Contents.SetRandomSeed, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
                    {
                        m_RandomSeed.intValue = UnityEngine.Random.Range(0, int.MaxValue);
                        component.startSeed = (uint)m_RandomSeed.intValue; // As accessors are bypassed with serialized properties...
                    }
                }
            }
            EditorGUILayout.PropertyField(m_ReseedOnPlay, m_Contents.ReseedOnPlay);
            bool reinit = EditorGUI.EndChangeCheck();

            //Field
            GUILayout.Label(m_Contents.HeaderParameters, m_Styles.InspectorHeader);

            if (m_graph == null || m_asset != component.visualEffectAsset)
            {
                m_asset = component.visualEffectAsset;
                if (m_asset != null)
                {
                    m_graph = m_asset.GetOrCreateGraph();
                }
            }

            if (m_graph != null)
            {

                if( m_graph.m_ParameterInfo == null)
                {
                    m_graph.BuildParameterInfo();
                }
                if( m_graph.m_ParameterInfo != null)
                {
                    List<int> stack = new List<int>();
                    int currentCount = m_graph.m_ParameterInfo.Length;
                    foreach(var parameter in m_graph.m_ParameterInfo)
                    {
                        --currentCount;
                        if( currentCount == 0 && stack.Count > 0 )
                        {
                            currentCount = stack.Last();
                            stack.RemoveAt(stack.Count-1);
                        }
                        if( parameter.descendantCount > 0)
                        {
                            stack.Add(currentCount);
                            currentCount = parameter.descendantCount;
                        }
                        
                        if(string.IsNullOrEmpty(parameter.sheetType))
                        {
                            if( parameter.name != null)
                            {
                                EmptyLineControl(parameter.name, stack.Count);
                            }
                        }
                        else if( parameter.sheetType != null)
                        {
                            var vfxField = m_VFXPropertySheet.FindPropertyRelative(parameter.sheetType + ".m_Array");
                            SerializedProperty property = null;
                            if (vfxField != null)
                            {
                                for (int i = 0; i < vfxField.arraySize; ++i)
                                {
                                    property = vfxField.GetArrayElementAtIndex(i);
                                    var nameProperty = property.FindPropertyRelative("m_Name").stringValue;
                                    if (nameProperty == parameter.path)
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

                                DisplayProperty(parameter, overrideProperty, property);

                                if (animated)
                                {
                                    GUI.color = previousColor;
                                }
                            }
                        }
                        EditorGUI.indentLevel = stack.Count;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            if (reinit)
            {
                component.Reinit();
            }

            GUI.enabled = true;
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
            public GUIContent ReseedOnPlay = new GUIContent("Reseed on play");
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

    [CustomEditor(typeof(VisualEffect))]
    public class AdvancedVisualEffectEditor : VisualEffectEditor
    {

        new void OnEnable()
        {
            base.OnEnable();
        }

        VFXParameter GetParameter(string name)
        {
            VisualEffect effect = (VisualEffect)target;

            if(effect.visualEffectAsset == null)
                return null;

            VFXGraph graph = effect.visualEffectAsset.graph as VFXGraph;
            if( graph == null)
                return null;

            var parameter = graph.children.OfType<VFXParameter>().FirstOrDefault(t=>t.exposedName == name && t.exposed == true);
            if( parameter == null)
                return null;

            return parameter;
        }

        VFXParameter m_GizmoedParameter;

        protected override void EmptyLineControl(string name, int depth)
        {
            if( depth != 1 )
            {
                base.EmptyLineControl(name,depth);
                return;
            }

            VFXParameter parameter = GetParameter(name);


            if(! VFXGizmoUtility.HasGizmo(parameter.type) )
            {
                base.EmptyLineControl(name,depth);
                return;
            }

            GUILayout.BeginHorizontal();
            //GUILayout.Space(overrideWidth + 4); // the 4 is so that Labels are aligned with elements having an override toggle.
            if(GUILayout.Button(EditorGUIUtility.IconContent("EditCollider"),GUILayout.Width(overrideWidth)))
            {
                m_GizmoedParameter = parameter;
            }
            EditorGUILayout.LabelField(name);
            GUILayout.EndHorizontal();
        }


        GizmoContext m_GizmoContext;

        new void OnSceneGUI()
        {
            base.OnSceneGUI();

            if(m_GizmoedParameter != null)
            {
                if( m_GizmoContext == null)
                {
                    m_GizmoContext = new GizmoContext(serializedObject,m_GizmoedParameter);
                }
                else
                {
                    m_GizmoContext.SetParameter(m_GizmoedParameter);
                }
                VFXGizmoUtility.Draw(m_GizmoContext,(VisualEffect)target);
            }

        }


        class GizmoContext : VFXGizmoUtility.Context
        {
            public GizmoContext(SerializedObject obj,VFXParameter parameter)
            {
                m_SerializedObject = obj;
                m_Parameter = parameter;
                m_VFXPropertySheet = m_SerializedObject.FindProperty("m_PropertySheet");
            }


            public override System.Type portType
            {
                get{return m_Parameter.type;} 
            }


            public List<object> m_Stack = new List<object>();

            public override object value
            {
                get{
                    m_Stack.Clear();
                    m_Stack.Add(System.Activator.CreateInstance(portType));
                    int stackSize = m_Stack.Count;

                    foreach(var cmd in m_ValueCmdList)
                    {
                        cmd(m_Stack);
                        stackSize = m_Stack.Count;
                    }


                    return m_Stack[0];
                }
            }

            SerializedObject m_SerializedObject;
            SerializedProperty m_VFXPropertySheet;

            public override VFXGizmo.IProperty<T> RegisterProperty<T>(string memberPath)
            {
                var cmdList = new List<Action<List<object>,object>>(); 
                bool succeeded = BuildPropertyValue<T>(cmdList,m_Parameter.type,m_Parameter.exposedName, memberPath.Split(separator[0]), 0);
                if( succeeded)
                {
                    return new Property<T>(m_SerializedObject,cmdList);
                }

                return VFXGizmoUtility.NullProperty<T>.defaultProperty;
            }

            bool BuildPropertyValue<T>(List<Action<List<object>,object>> cmdList, Type type,string propertyPath,string[] memberPath,int depth)
            {
                string field = VisualEffectUtility.GetTypeField(type);

                if( field != null)
                {
                    var vfxField = m_VFXPropertySheet.FindPropertyRelative(field + ".m_Array");
                    if( vfxField == null)
                        return false;

                    SerializedProperty property = null;
                    if (vfxField != null)
                    {
                        for (int i = 0; i < vfxField.arraySize; ++i)
                        {
                            property = vfxField.GetArrayElementAtIndex(i);
                            var nameProperty = property.FindPropertyRelative("m_Name").stringValue;
                            if (nameProperty == propertyPath)
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
                        
                        cmdList.Add((l,o)=>overrideProperty.boolValue = true);
                    }
                    else
                    {
                        return false;
                    }

                    if( depth < memberPath.Length)
                    {
                        cmdList.Add((l,o)=>l.Add(GetObjectValue(property)));
                        if( !BuildPropertySubValue<T>(cmdList,type,memberPath,depth))
                            return false;
                        cmdList.Add((l,o)=>SetObjectValue(property,l[l.Count-1]));
                        
                        return true;
                    }
                    else
                    {
                        var currentValue = GetObjectValue(property);
                        if( ! typeof(T).IsAssignableFrom(currentValue.GetType()))
                        {
                            return false;
                        }
                        cmdList.Add((l,o)=>SetObjectValue(property,o));
                        return true;
                    }
                }
                else if( depth < memberPath.Length)
                {
                    FieldInfo subField = type.GetField(memberPath[depth]);
                    if( subField == null)
                        return false;
                    return BuildPropertyValue<T>(cmdList,subField.FieldType,propertyPath + "_" + memberPath[depth],memberPath,depth+1);
                }
                Debug.LogError("Setting A value across multiple property is not yet supported");

                return false;
            }
            bool BuildPropertySubValue<T>(List<Action<List<object>,object>> cmdList, Type type,string[] memberPath,int depth)
            {
                FieldInfo subField = type.GetField(memberPath[depth]);
                if( subField == null)
                    return false;

                depth++;
                if( depth < memberPath.Length)
                {
                    cmdList.Add((l,o)=>l.Add(subField.GetValue(l[l.Count-1])));
                    BuildPropertySubValue<T>(cmdList,type,memberPath,depth);
                    cmdList.Add((l,o)=>subField.SetValue(l[l.Count-2],l[l.Count-1]));
                    cmdList.Add((l,o)=>l.RemoveAt(l.Count-1));
                }
                else
                {
                    if( subField.FieldType != typeof(T))
                        return false;
                    cmdList.Add((l,o)=>subField.SetValue(l[l.Count-1],o));
                }

                return true;
            }


            object GetObjectValue(SerializedProperty prop)
            {
                switch(prop.propertyType)
                {
                    case SerializedPropertyType.Float:
                        return prop.floatValue;
                    case SerializedPropertyType.Vector3:
                        return prop.vector3Value;
                    case SerializedPropertyType.Vector2:
                        return prop.vector2Value;
                    case SerializedPropertyType.Vector4:
                        return prop.vector4Value;
                    //case SerializedPropertyType.ObjectReference:
                    //    return prop.objectReferenceValue;
                    case SerializedPropertyType.Integer:
                        return prop.intValue;
                    case SerializedPropertyType.Boolean:
                        return prop.boolValue;
                    //case SerializedPropertyType.Gradient:
                    //    return prop.gradientValue;
                    //case SerializedPropertyType.AnimationCurve:
                    //    return prop.animationCurveValue;
                }
                return null;
            }
            void SetObjectValue(SerializedProperty prop, object value)
            {
                switch(prop.propertyType)
                {
                    case SerializedPropertyType.Float:
                        prop.floatValue = (float)value;
                        return;
                    case SerializedPropertyType.Vector3:
                        prop.vector3Value = (Vector3)value;
                        return;
                    case SerializedPropertyType.Vector2:
                        prop.vector2Value = (Vector2)value;
                        return;
                    case SerializedPropertyType.Vector4:
                        prop.vector4Value = (Vector4)value;
                        return;
                    //case SerializedPropertyType.ObjectReference:
                    //    prop.objectReferenceValue = (UnityEngine.Object)value;
                    //    return;
                    case SerializedPropertyType.Integer:
                        prop.intValue = (int)value;
                        return;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = (bool)value;
                        return;
                    //case SerializedPropertyType.Gradient:
                    //    prop.gradientValue = (Gradient)value;
                    //    return;
                    //case SerializedPropertyType.AnimationCurve:
                    //    prop.animationCurveValue = (AnimationCurve)value;
                    //    return;
                }
            }

            public void SetParameter(VFXParameter parameter)
            {
                if( parameter != m_Parameter)
                {
                    Unprepare();
                    m_Parameter = parameter;
                }
            }

            List<Action<List<object>>> m_ValueCmdList = new List<Action<List<object>>>();

            protected override void InternalPrepare()
            {
                m_ValueCmdList.Clear();

                BuildValue(m_ValueCmdList,portType,m_Parameter.exposedName);
            }

            void BuildValue(List<Action<List<object>>> cmdList, Type type,string propertyPath)
            {
                string field = VisualEffectUtility.GetTypeField(type);
                if (field != null)
                {
                    var vfxField = m_VFXPropertySheet.FindPropertyRelative(field + ".m_Array");
                    SerializedProperty property = null;
                    if (vfxField != null)
                    {
                        for (int i = 0; i < vfxField.arraySize; ++i)
                        {
                            property = vfxField.GetArrayElementAtIndex(i);
                            var nameProperty = property.FindPropertyRelative("m_Name").stringValue;
                            if (nameProperty == propertyPath)
                            {
                                break;
                            }
                            property = null;
                        }
                    }
                    if (property != null)
                    {
                        property = property.FindPropertyRelative("m_Value");
                        

                        //Debug.Log("PushProperty" + propertyPath + "("+property.propertyType.ToString()+")");
                        cmdList.Add(
                            o=>PushProperty(o,property)
                            );
                    }
                }
                else
                {
                    foreach(var fieldInfo in type.GetFields(BindingFlags.Public|BindingFlags.Instance))
                    {
                        if( fieldInfo.FieldType == typeof(CoordinateSpace))
                            continue;
                        //Debug.Log("Push "+type.UserFriendlyName()+"."+fieldInfo.Name+"("+fieldInfo.FieldType.UserFriendlyName());
                        cmdList.Add(o=>
                        {
                            Push(o,fieldInfo);
                        });
                        BuildValue(cmdList,fieldInfo.FieldType,propertyPath + "_" + fieldInfo.Name);
                        //Debug.Log("Pop "+type.UserFriendlyName()+"."+fieldInfo.Name+"("+fieldInfo.FieldType.UserFriendlyName());
                        cmdList.Add(o=>
                            Pop(o,fieldInfo)
                        );
                    }
                }
            }

            void PushProperty(List<object> stack, SerializedProperty property)
            {
                stack[stack.Count-1] = GetObjectValue(property);
            }

            void Push(List<object> stack,FieldInfo fieldInfo)
            {
                object prev = stack[stack.Count-1];
                stack.Add(fieldInfo.GetValue(prev));
            }

            void Pop(List<object> stack,FieldInfo fieldInfo)
            {
                fieldInfo.SetValue(stack[stack.Count-2],stack[stack.Count-1]);
                stack.RemoveAt(stack.Count-1);
            }


            class Property<T> : VFXGizmo.IProperty<T>
            {

                public Property(SerializedObject serilializedObject, List<Action<List<object>,object>> cmdlist)
                {
                    m_SerializedObject = serilializedObject;
                    m_CmdList = cmdlist;
                }

                public bool isEditable { get{return true;} }


                List<Action<List<object>,object>> m_CmdList;
                List<object> m_Stack = new List<object>();

                SerializedObject m_SerializedObject;

                public void SetValue(T value)
                {
                    m_Stack.Clear();
                    foreach( var cmd in m_CmdList)
                    {
                        cmd(m_Stack,value);
                    }
                    m_SerializedObject.ApplyModifiedProperties();
                }
            }

            VFXParameter m_Parameter;

        }

    }
}