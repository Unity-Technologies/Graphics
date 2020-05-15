using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEditor.VFX;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.Experimental.VFX.Utility
{
    [CustomEditor(typeof(VFXPropertyBinder))]
    class VFXPropertyBinderEditor : Editor
    {
        ReorderableList m_List;
        SerializedProperty m_Elements;
        SerializedProperty m_Component;
        SerializedProperty m_ExecuteInEditor;

        GenericMenu m_Menu;
        VFXBinderEditor m_ElementEditor;

        static readonly Color validColor = new Color(0.5f, 1.0f, 0.2f);
        static readonly Color invalidColor = new Color(1.0f, 0.5f, 0.2f);
        static readonly Color errorColor = new Color(1.0f, 0.2f, 0.2f);


        static class Styles
        {
            public static GUIStyle labelStyle;
            static Styles()
            {
                labelStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset(20, 0, 2, 0), richText = true};
            }
        }

        private void OnEnable()
        {
            BuildMenu();
            m_Elements = serializedObject.FindProperty("m_Bindings");
            m_Component = serializedObject.FindProperty("m_VisualEffect");
            m_ExecuteInEditor = serializedObject.FindProperty("m_ExecuteInEditor");

            m_List = new ReorderableList(serializedObject, m_Elements, false, true, true, true);
            m_List.drawHeaderCallback = DrawHeader;
            m_List.drawElementCallback = DrawElement;
            m_List.onRemoveCallback = RemoveElement;
            m_List.onAddCallback = AddElement;
            m_List.onSelectCallback = SelectElement;
        }

        private void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_ExecuteInEditor);
            EditorGUILayout.Space();
            m_List.DoLayoutList();
            EditorGUILayout.Space();
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
            if (m_ElementEditor != null)
            {
                EditorGUI.BeginChangeCheck();

                var fieldAttribute = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

                var binding = m_ElementEditor.serializedObject.targetObject;
                var type = binding.GetType();
                var fields = type.GetFields(fieldAttribute);

                foreach (var field in fields)
                {
                    var property = m_ElementEditor.serializedObject.FindProperty(field.Name);

                    if (property == null) continue;

                    using (new GUILayout.HorizontalScope())
                    {
                        var attrib = field.GetCustomAttributes(true).OfType<VFXPropertyBindingAttribute>().FirstOrDefault<VFXPropertyBindingAttribute>();
                        if (attrib != null)
                        {
                            var parameter = property.FindPropertyRelative("m_Name");
                            string parm = parameter.stringValue;
                            parm = EditorGUILayout.TextField(ObjectNames.NicifyVariableName(property.name), parm);

                            if (parm != parameter.stringValue)
                            {
                                parameter.stringValue = parm;
                                serializedObject.ApplyModifiedProperties();
                            }

                            if (GUILayout.Button("v", EditorStyles.toolbarButton, GUILayout.Width(14)))
                                CheckTypeMenu(property, attrib, (m_Component.objectReferenceValue as VisualEffect).visualEffectAsset);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(property, true);
                        }
                    }
                }

                if (EditorGUI.EndChangeCheck())
                    m_ElementEditor.serializedObject.ApplyModifiedProperties();

                var component = (m_Component.objectReferenceValue as VisualEffect);
                bool valid = (binding as VFXBinderBase).IsValid(component);
                if (!valid)
                {
                    EditorGUILayout.HelpBox("This binding is not correctly configured, please ensure Property is valid and/or objects are not null", MessageType.Warning);
                }
            }
        }

        private class MenuPropertySetName
        {
            public SerializedProperty property;
            public string value;
        }

        public void CheckTypeMenu(SerializedProperty property, VFXPropertyBindingAttribute attribute, VisualEffectAsset asset)
        {
            GenericMenu menu = new GenericMenu();
            var parameters = (asset.GetResource().graph as UnityEditor.VFX.VFXGraph).children.OfType<UnityEditor.VFX.VFXParameter>();
            foreach (var param in parameters)
            {
                string typeName = param.type.ToString();
                if (attribute.EditorTypes.Contains(typeName))
                {
                    MenuPropertySetName set = new MenuPropertySetName
                    {
                        property = property,
                        value = param.exposedName
                    };
                    menu.AddItem(new GUIContent(param.exposedName), false, SetFieldName, set);
                }
            }

            menu.ShowAsContext();
        }

        public void SetFieldName(object o)
        {
            var set = o as MenuPropertySetName;
            set.property.FindPropertyRelative("m_Name").stringValue = set.value;
            m_ElementEditor.serializedObject.ApplyModifiedProperties();
        }

        private static Type[] s_ConcreteBinders = null;
        public void BuildMenu()
        {
            m_Menu = new GenericMenu();
            if (s_ConcreteBinders == null)
                s_ConcreteBinders = VFXLibrary.FindConcreteSubclasses(typeof(VFXBinderBase), typeof(VFXBinderAttribute)).ToArray();

            foreach (var type in s_ConcreteBinders)
            {
                string name = type.ToString();
                var attrib = type.GetCustomAttributes(true).OfType<VFXBinderAttribute>().First();
                name = attrib.MenuPath;

                m_Menu.AddItem(new GUIContent(name), false, AddBinding, type);
            }
        }

        public void AddBinding(object type)
        {
            Type t = type as Type;
            var obj = (serializedObject.targetObject as VFXPropertyBinder).gameObject;
            Undo.AddComponent(obj, t);
        }

        public void SelectElement(ReorderableList list)
        {
            UpdateSelection(list.index);
        }

        public void UpdateSelection(int selected)
        {
            if (selected >= 0)
            {
                Editor editor = null;
                CreateCachedEditor(m_Elements.GetArrayElementAtIndex(selected).objectReferenceValue, typeof(VFXBinderEditor), ref editor);
                m_ElementEditor = editor as VFXBinderEditor;
            }
            else
                m_ElementEditor = null;
        }

        public void AddElement(ReorderableList list)
        {
            m_Menu.ShowAsContext();
        }

        public void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var target = m_Elements.GetArrayElementAtIndex(index).objectReferenceValue as VFXBinderBase;
            Rect iconRect = new Rect(rect.xMin + 4, rect.yMin + 4, 8, 8);

            if (target != null)
            {
                var element = target.ToString();

                GUI.Label(rect, new GUIContent(element), Styles.labelStyle);

                var component = (m_Component.objectReferenceValue as VisualEffect);
                bool valid = target.IsValid(component);


                EditorGUI.DrawRect(iconRect, valid ? validColor : invalidColor);
            }
            else
            {
                EditorGUI.DrawRect(iconRect, errorColor);
                GUI.Label(rect, "<color=red>(Missing or Null Property Binder)</color>", Styles.labelStyle);
            }
        }

        public void RemoveElement(ReorderableList list)
        {
            int index = m_List.index;
            var element = m_Elements.GetArrayElementAtIndex(index).objectReferenceValue;
            if (element != null)
            {
                Undo.DestroyObjectImmediate(element);
                m_Elements.DeleteArrayElementAtIndex(index); // Delete object reference
            }
            else
            {
                Undo.RecordObject(serializedObject.targetObject, "Remove null entry");
            }
            m_Elements.DeleteArrayElementAtIndex(index); // Remove list entry
            UpdateSelection(-1);
        }

        public void DrawHeader(Rect rect)
        {
            GUI.Label(rect, "Property Bindings");
        }
    }
}
