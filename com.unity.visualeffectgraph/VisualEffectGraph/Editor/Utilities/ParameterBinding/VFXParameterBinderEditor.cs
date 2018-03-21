using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(VFXParameterBinder))]
public class VFXParameterBinderEditor : Editor
{
    ReorderableList m_List;
    SerializedProperty m_Elements;
    SerializedProperty m_Component;
    SerializedProperty m_ExecuteInEditor;

    GenericMenu m_Menu;
    Editor m_ElementEditor;

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

        EditorGUILayout.PropertyField(m_ExecuteInEditor);
        EditorGUILayout.Space();
        m_List.DoLayoutList();
        EditorGUILayout.Space();
        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();
        if (m_ElementEditor != null)
        {
            EditorGUI.BeginChangeCheck();
            //m_ElementEditor.DrawDefaultInspector();
            var target = m_ElementEditor.serializedObject.targetObject;
            var type = target.GetType();
            var fields = type.GetFields();

            foreach(var field in fields)
            {
                var property = m_ElementEditor.serializedObject.FindProperty(field.Name);

                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property);

                    var attrib = field.GetCustomAttributes(true).OfType<VFXBindingAttribute>().FirstOrDefault<VFXBindingAttribute>();
                    if (attrib != null)
                    {
                        if (GUILayout.Button("v", EditorStyles.miniButton, GUILayout.Width(14)))
                            CheckTypeMenu(property, attrib, (m_Component.objectReferenceValue as VisualEffect).visualEffectAsset);
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
                m_ElementEditor.serializedObject.ApplyModifiedProperties();
        }
    }

    private class MenuPropertySetName
    {
        public SerializedProperty property;
        public string value;
    }

    public void CheckTypeMenu(SerializedProperty property, VFXBindingAttribute attribute, VisualEffectAsset asset)
    {
        GenericMenu menu = new GenericMenu();
        var parameters = (asset.graph as UnityEditor.VFX.VFXGraph).children.OfType<UnityEditor.VFX.VFXParameter>();
        foreach(var param in parameters)
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
        set.property.stringValue = set.value;
        m_ElementEditor.serializedObject.ApplyModifiedProperties();
    }

    public void BuildMenu()
    {
        m_Menu = new GenericMenu();

        List<Type> relevantTypes = new List<Type>();

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type t in assembly.GetTypes())
            {
                if (t.BaseType == typeof(VFXBindingBase))
                    relevantTypes.Add(t);
            }
        }
        foreach (Type type in relevantTypes)
            m_Menu.AddItem(new GUIContent(type.ToString()), false, AddBinding, type);
    }

    public void AddBinding(object type)
    {
        Type t = type as Type;
        var obj = (m_SerializedObject.targetObject as VFXParameterBinder).gameObject;
        Undo.AddComponent(obj, t);
    }

    public void SelectElement(ReorderableList list)
    {
        UpdateSelection(list.index);
    }

    public void UpdateSelection(int selected)
    {
        if (selected >= 0)
            CreateCachedEditor(m_Elements.GetArrayElementAtIndex(selected).objectReferenceValue, typeof(Editor), ref m_ElementEditor);
        else
            m_ElementEditor = null;
    }

    public void AddElement(ReorderableList list)
    {
        m_Menu.ShowAsContext();
    }

    public void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = (m_Elements.GetArrayElementAtIndex(index).objectReferenceValue).ToString();
        GUI.Label(rect, new GUIContent(element));
    }

    public void RemoveElement(ReorderableList list)
    {
        int index = m_List.index;
        var element = m_Elements.GetArrayElementAtIndex(index).objectReferenceValue;
        Undo.DestroyObjectImmediate(element);
        m_Elements.DeleteArrayElementAtIndex(index);
        m_Elements.DeleteArrayElementAtIndex(index);
        UpdateSelection(-1);
    }

    public void DrawHeader(Rect rect)
    {
        GUI.Label(rect, "Parameter Bindings");
    }
}
