using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;

using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using UnityEditor.Experimental.GraphView;
using EditMode = UnityEditorInternal.EditMode;
using UnityObject = UnityEngine.Object;
using System.Reflection;
namespace UnityEditor.VFX
{
    static class VisualEffectSerializationUtility
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
            else if (type == typeof(Texture))
            {
                return "m_NamedObject";
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

    [CustomEditor(typeof(VisualEffect))]
    [CanEditMultipleObjects]
    class AdvancedVisualEffectEditor : VisualEffectEditor, IToolModeOwner
    {
        new void OnEnable()
        {
            base.OnEnable();
            EditMode.editModeStarted += OnEditModeStart;
            EditMode.editModeEnded += OnEditModeEnd;

            // Force rebuilding the parameterinfos
            VisualEffect effect = ((VisualEffect)targets[0]);

            var asset = effect.visualEffectAsset;
            if (asset != null && asset.GetResource() != null)
            {
                var graph = asset.GetResource().GetOrCreateGraph();

                if (graph)
                {
                    graph.BuildParameterInfo();
                }
            }
        }

        new void OnDisable()
        {
            VisualEffect effect = ((VisualEffect)targets[0]);
            // Check if the component is attach in the editor. If So do not call base.OnDisable() because we don't want to reset the playrate or pause
            VFXViewWindow window = VFXViewWindow.currentWindow;
            if (window == null || window.graphView == null || window.graphView.attachedComponent != effect)
            {
                base.OnDisable();
            }
            else
            {
                OnDisableWithoutResetting();
            }

            m_ContextsPerComponent.Clear();
            EditMode.editModeStarted -= OnEditModeStart;
            EditMode.editModeEnded -= OnEditModeEnd;
        }

        public override void OnInspectorGUI()
        {
            m_GizmoableParameters.Clear();
            base.OnInspectorGUI();
        }

        void OnEditModeStart(IToolModeOwner owner, EditMode.SceneViewEditMode mode)
        {
            if (mode == EditMode.SceneViewEditMode.Collider && owner == (IToolModeOwner)this)
                OnEditStart();
        }

        void OnEditModeEnd(IToolModeOwner owner)
        {
            if (owner == (IToolModeOwner)this)
                OnEditEnd();
        }

        protected override void AssetField(VisualEffectResource resource)
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(m_VisualEffectAsset, Contents.assetPath);

                bool saveEnabled = GUI.enabled;
                if (m_VisualEffectAsset.objectReferenceValue == null && !m_VisualEffectAsset.hasMultipleDifferentValues)
                {
                    GUI.enabled = saveEnabled;
                    if (GUILayout.Button(Contents.createAsset, EditorStyles.miniButton, Styles.MiniButtonWidth))
                    {
                        string filePath = EditorUtility.SaveFilePanelInProject("", "New Graph", "vfx", "Create new VisualEffect Graph");
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            VisualEffectAssetEditorUtility.CreateTemplateAsset(filePath);
                            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(filePath);
                            m_VisualEffectAsset.objectReferenceValue = asset;
                            serializedObject.ApplyModifiedProperties();

                            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
                            window.LoadAsset(asset, targets.Length > 1 ? null : target as VisualEffect);
                        }
                    }
                }
                else
                {
                    GUI.enabled = saveEnabled && !m_VisualEffectAsset.hasMultipleDifferentValues && m_VisualEffectAsset.objectReferenceValue != null && resource != null; // Enabled state will be kept for all content until the end of the inspectorGUI.
                    if (GUILayout.Button(Contents.openEditor, EditorStyles.miniButton, Styles.MiniButtonWidth))
                    {
                        VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
                        var asset = m_VisualEffectAsset.objectReferenceValue as VisualEffectAsset;
                        window.LoadAsset(asset, targets.Length > 1 ? null : target as VisualEffect);
                    }
                }
                GUI.enabled = saveEnabled;
            }
        }

        protected override void EditorModeInspectorButton()
        {
            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Collider,
                "Show Property Gizmos",
                EditorGUIUtility.IconContent("EditCollider"),
                this
            );
        }

        VFXParameter GetParameter(string name, VisualEffectResource resource)
        {
            VisualEffect effect = (VisualEffect)target;

            if (effect.visualEffectAsset == null)
                return null;

            VFXGraph graph = resource.graph as VFXGraph;
            if (graph == null)
                return null;

            var parameter = graph.children.OfType<VFXParameter>().FirstOrDefault(t => t.exposedName == name && t.exposed == true);
            if (parameter == null)
                return null;

            return parameter;
        }

        VFXParameter m_GizmoedParameter;

        List<VFXParameter> m_GizmoableParameters = new List<VFXParameter>();

        protected override void EmptyLineControl(string name, string tooltip, int depth, VisualEffectResource resource)
        {
            if (depth != 1)
            {
                base.EmptyLineControl(name, tooltip, depth, resource);
                return;
            }

            VFXParameter parameter = GetParameter(name, resource);

            if (!VFXGizmoUtility.HasGizmo(parameter.type))
            {
                base.EmptyLineControl(name, tooltip, depth, resource);
                return;
            }

            if (m_EditJustStarted && m_GizmoedParameter == null)
            {
                m_EditJustStarted = false;
                m_GizmoedParameter = parameter;
            }
            m_GizmoableParameters.Add(parameter);
            if (!m_GizmoDisplayed)
            {
                base.EmptyLineControl(name, tooltip, depth, resource);
                return;
            }

            GUILayout.BeginHorizontal();


            GUILayout.Space(overrideWidth);
            // Make the label half the width to make the tooltip
            EditorGUILayout.LabelField(GetGUIContent(name, tooltip), EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));

            GUILayout.FlexibleSpace();

            // Toggle Button
            EditorGUI.BeginChangeCheck();
            bool result = GUILayout.Toggle(m_GizmoedParameter == parameter, new GUIContent("Edit Gizmo"), EditorStyles.miniButton);

            if (EditorGUI.EndChangeCheck() && result)
            {
                m_GizmoedParameter = parameter;
            }

            //GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        bool m_EditJustStarted;
        bool m_GizmoDisplayed;
        void OnEditStart()
        {
            m_GizmoDisplayed = true;
            m_EditJustStarted = true;
        }

        void OnEditEnd()
        {
            m_EditJustStarted = false;
            m_GizmoedParameter = null;
            m_GizmoDisplayed = false;
        }

        struct ContextAndGizmo
        {
            public GizmoContext context;
            public VFXGizmo gizmo;
        }

        protected override void PropertyOverrideChanged()
        {
            foreach (var context in m_ContextsPerComponent.Values.Select(t => t.context))
            {
                context.Unprepare();
            }
        }

        Dictionary<VisualEffect, ContextAndGizmo> m_ContextsPerComponent = new Dictionary<VisualEffect, ContextAndGizmo>();

        ContextAndGizmo GetGizmo()
        {
            ContextAndGizmo context;
            if (!m_ContextsPerComponent.TryGetValue((VisualEffect)target, out context))
            {
                context.context = new GizmoContext(new SerializedObject(target), m_GizmoedParameter);
                context.gizmo = VFXGizmoUtility.CreateGizmoInstance(context.context);
                m_ContextsPerComponent.Add((VisualEffect)target, context);
            }
            else
            {
                var prevType = context.context.portType;
                context.context.SetParameter(m_GizmoedParameter);
                if (context.context.portType != prevType)
                {
                    context.gizmo = VFXGizmoUtility.CreateGizmoInstance(context.context);
                    m_ContextsPerComponent[(VisualEffect)target] = context;
                }
            }

            return context;
        }

        protected override void OnSceneViewGUI(SceneView sv)
        {
            base.OnSceneViewGUI(sv);

            if (m_GizmoDisplayed && m_GizmoedParameter != null && m_GizmoableParameters.Count > 0 && ((VisualEffect)target).visualEffectAsset != null)
            {
                ContextAndGizmo context = GetGizmo();

                VFXGizmoUtility.Draw(context.context, (VisualEffect)target, context.gizmo);
            }
        }

        class GizmoContext : VFXGizmoUtility.Context
        {
            public GizmoContext(SerializedObject obj, VFXParameter parameter)
            {
                m_SerializedObject = obj;
                m_Parameter = parameter;
                m_VFXPropertySheet = m_SerializedObject.FindProperty("m_PropertySheet");
            }

            public override System.Type portType
            {
                get {return m_Parameter.type; }
            }

            public override VFXCoordinateSpace space
            {
                get
                {
                    return m_Parameter.outputSlots[0].space;
                }
            }
            public override bool spaceLocalByDefault
            {
                get { return true; }
            }

            public List<object> m_Stack = new List<object>();

            public override object value
            {
                get
                {
                    m_SerializedObject.Update();
                    m_Stack.Clear();

                    foreach (var cmd in m_ValueCmdList)
                    {
                        cmd(m_Stack);
                    }


                    return m_Stack[0];
                }
            }

            SerializedObject m_SerializedObject;
            SerializedProperty m_VFXPropertySheet;

            public override VFXGizmo.IProperty<T> RegisterProperty<T>(string memberPath)
            {
                var cmdList = new List<Action<List<object>, object>>();
                bool succeeded = BuildPropertyValue<T>(cmdList, m_Parameter.type, m_Parameter.exposedName, memberPath.Split(new char[] {separator[0]}, StringSplitOptions.RemoveEmptyEntries), 0);
                if (succeeded)
                {
                    return new Property<T>(m_SerializedObject, cmdList);
                }

                return VFXGizmoUtility.NullProperty<T>.defaultProperty;
            }

            void AddNewValue(List<object> l, object o, SerializedProperty vfxField, string propertyPath, string[] memberPath, int depth)
            {
                vfxField.InsertArrayElementAtIndex(vfxField.arraySize);
                var newEntry = vfxField.GetArrayElementAtIndex(vfxField.arraySize - 1);
                newEntry.FindPropertyRelative("m_Overridden").boolValue = true;

                var valueProperty = newEntry.FindPropertyRelative("m_Value");

                VFXSlot slot = m_Parameter.outputSlots[0];
                for (int i = 0; i < memberPath.Length && i < depth; ++i)
                {
                    slot = slot.children.First(t => t.name == memberPath[i]);
                }

                l.Add(slot.value); // find the default value which is in the parameter.
                newEntry.FindPropertyRelative("m_Name").stringValue = propertyPath;

                Unprepare(); // if we set the value we'll have to regenerate the cmdList for next time.
            }

            bool BuildPropertyValue<T>(List<Action<List<object>, object>> cmdList, Type type, string propertyPath, string[] memberPath, int depth, FieldInfo specialSpacableVector3CaseField = null)
            {
                string field = VisualEffectSerializationUtility.GetTypeField(type);

                if (field != null)
                {
                    var vfxField = m_VFXPropertySheet.FindPropertyRelative(field + ".m_Array");
                    if (vfxField == null)
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
                        cmdList.Add((l, o) => overrideProperty.boolValue = true);
                    }
                    else if (vfxField != null)
                    {
                        cmdList.Add((l, o) =>
                        {
                            AddNewValue(l, o, vfxField, propertyPath, memberPath, depth);
                        });

                        if (depth < memberPath.Length)
                        {
                            if (!BuildPropertySubValue<T>(cmdList, type, memberPath, depth))
                                return false;
                        }
                        cmdList.Add((l, o) =>
                        {
                            SetObjectValue(vfxField.GetArrayElementAtIndex(vfxField.arraySize - 1).FindPropertyRelative("m_Value"), l[l.Count - 1]);
                        });

                        return true;
                    }

                    if (depth < memberPath.Length)
                    {
                        cmdList.Add((l, o) => l.Add(GetObjectValue(property)));
                        if (!BuildPropertySubValue<T>(cmdList, type, memberPath, depth))
                            return false;
                        cmdList.Add((l, o) => SetObjectValue(property, l[l.Count - 1]));

                        return true;
                    }
                    else
                    {
                        var currentValue = GetObjectValue(property);
                        if (specialSpacableVector3CaseField != null)
                        {
                            cmdList.Add(
                                (l, o) => {
                                    object vector3Property = specialSpacableVector3CaseField.GetValue(o);
                                    SetObjectValue(property, vector3Property);
                                });
                        }
                        else
                        {
                            if (!typeof(T).IsAssignableFrom(currentValue.GetType()))
                            {
                                return false;
                            }

                            cmdList.Add((l, o) => SetObjectValue(property, o));
                        }
                        return true;
                    }
                }
                else if (depth < memberPath.Length)
                {
                    FieldInfo subField = type.GetField(memberPath[depth]);
                    if (subField == null)
                        return false;
                    return BuildPropertyValue<T>(cmdList, subField.FieldType, propertyPath + "_" + memberPath[depth], memberPath, depth + 1);
                }
                else if (typeof(Position) == type || typeof(Vector) == type || typeof(DirectionType) == type)
                {
                    if (typeof(T) != type)
                    {
                        return false;
                    }

                    FieldInfo vector3Field = type.GetFields(BindingFlags.Instance | BindingFlags.Public).First(t => t.FieldType == typeof(Vector3));
                    string name = vector3Field.Name;
                    return BuildPropertyValue<T>(cmdList, typeof(Vector3), propertyPath + "_" + name, new string[] {name}, 1, vector3Field);
                }
                Debug.LogError("Setting A value across multiple property is not yet supported");

                return false;
            }

            bool BuildPropertySubValue<T>(List<Action<List<object>, object>> cmdList, Type type, string[] memberPath, int depth)
            {
                FieldInfo subField = type.GetField(memberPath[depth]);
                if (subField == null)
                    return false;

                depth++;
                if (depth < memberPath.Length)
                {
                    cmdList.Add((l, o) => l.Add(subField.GetValue(l[l.Count - 1])));
                    BuildPropertySubValue<T>(cmdList, type, memberPath, depth);
                    cmdList.Add((l, o) => subField.SetValue(l[l.Count - 2], l[l.Count - 1]));
                    cmdList.Add((l, o) => l.RemoveAt(l.Count - 1));
                }
                else
                {
                    if (subField.FieldType != typeof(T))
                        return false;
                    cmdList.Add((l, o) => subField.SetValue(l[l.Count - 1], o));
                }

                return true;
            }

            public void SetParameter(VFXParameter parameter)
            {
                if (parameter != m_Parameter)
                {
                    Unprepare();
                    m_Parameter = parameter;
                }
            }

            List<Action<List<object>>> m_ValueCmdList = new List<Action<List<object>>>();

            protected override void InternalPrepare()
            {
                m_ValueCmdList.Clear();
                m_Stack.Clear();

                m_ValueCmdList.Add(o => o.Add(m_Parameter.value));

                BuildValue(m_ValueCmdList, portType, m_Parameter.exposedName);
            }

            void BuildValue(List<Action<List<object>>> cmdList, Type type, string propertyPath)
            {
                string field = VisualEffectSerializationUtility.GetTypeField(type);
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
                        var overrideProperty = property.FindPropertyRelative("m_Overridden");
                        property = property.FindPropertyRelative("m_Value");
                        cmdList.Add(o => { if (overrideProperty.boolValue) PushProperty(o, property); });
                    }
                }
                else
                {
                    foreach (var fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (fieldInfo.FieldType == typeof(VFXCoordinateSpace))
                            continue;
                        cmdList.Add(o =>
                        {
                            Push(o, fieldInfo);
                        });
                        BuildValue(cmdList, fieldInfo.FieldType, propertyPath + "_" + fieldInfo.Name);
                        cmdList.Add(o =>
                            Pop(o, fieldInfo)
                        );
                    }
                }
            }

            void PushProperty(List<object> stack, SerializedProperty property)
            {
                stack[stack.Count - 1] = GetObjectValue(property);
            }

            void Push(List<object> stack, FieldInfo fieldInfo)
            {
                object prev = stack[stack.Count - 1];
                stack.Add(fieldInfo.GetValue(prev));
            }

            void Pop(List<object> stack, FieldInfo fieldInfo)
            {
                fieldInfo.SetValue(stack[stack.Count - 2], stack[stack.Count - 1]);
                stack.RemoveAt(stack.Count - 1);
            }

            class Property<T> : VFXGizmo.IProperty<T>
            {
                public Property(SerializedObject serilializedObject, List<Action<List<object>, object>> cmdlist)
                {
                    m_SerializedObject = serilializedObject;
                    m_CmdList = cmdlist;
                }

                public bool isEditable { get {return true; } }


                List<Action<List<object>, object>> m_CmdList;
                List<object> m_Stack = new List<object>();

                SerializedObject m_SerializedObject;

                public void SetValue(T value)
                {
                    m_Stack.Clear();
                    foreach (var cmd in m_CmdList)
                    {
                        cmd(m_Stack, value);
                    }
                    m_SerializedObject.ApplyModifiedProperties();
                }
            }

            VFXParameter m_Parameter;
        }


        bool HasFrameBounds()
        {
            return targets.Length == 1;
        }

        //Callback used by scene view on 'F' shortcut.
        Bounds OnGetFrameBounds()
        {
            return GetWorldBoundsOfTarget(targets[0]);
        }

        internal override Bounds GetWorldBoundsOfTarget(UnityObject targetObject)
        {
            if (m_GizmoDisplayed && m_GizmoedParameter != null)
            {
                ContextAndGizmo context = GetGizmo();

                Bounds result = VFXGizmoUtility.GetGizmoBounds(context.context, (VisualEffect)target, context.gizmo);

                return result;
            }

            return base.GetWorldBoundsOfTarget(targetObject);
        }

        protected override void SceneViewGUICallback(UnityObject tar, SceneView sceneView)
        {
            base.SceneViewGUICallback(tar, sceneView);
            if (m_GizmoableParameters.Count > 0)
            {
                int current = m_GizmoDisplayed ? m_GizmoableParameters.IndexOf(m_GizmoedParameter) : -1;
                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Gizmos", GUILayout.Width(45));
                int result = EditorGUILayout.Popup(current, m_GizmoableParameters.Select(t => t.exposedName).ToArray(), GUILayout.Width(140));
                if (EditorGUI.EndChangeCheck() && result != current)
                {
                    m_GizmoedParameter = m_GizmoableParameters[result];
                    if (!m_GizmoDisplayed)
                    {
                        m_GizmoDisplayed = true;
                        EditMode.ChangeEditMode(EditMode.SceneViewEditMode.Collider, this);
                    }
                    Repaint();
                }

                bool saveEnabled = GUI.enabled;
                GUI.enabled = saveEnabled && m_GizmoedParameter != null;
                if (GUILayout.Button(VFXSlotContainerEditor.Contents.gizmoFrame, VFXSlotContainerEditor.Styles.frameButtonStyle, GUILayout.Width(19), GUILayout.Height(18)))
                {
                    if (m_GizmoDisplayed && m_GizmoedParameter != null)
                    {
                        ContextAndGizmo context = GetGizmo();

                        context.gizmo.currentSpace = context.context.space;
                        context.gizmo.spaceLocalByDefault = context.context.spaceLocalByDefault;
                        context.gizmo.component = (VisualEffect)target;
                        Bounds bounds = context.gizmo.CallGetGizmoBounds(context.context.value);
                        sceneView.Frame(bounds, false);
                    }
                }
                GUI.enabled = saveEnabled;
                GUILayout.EndHorizontal();
            }
        }
    }
}
